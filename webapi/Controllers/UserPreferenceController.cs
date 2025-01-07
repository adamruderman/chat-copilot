// Copyright (c) Microsoft. All rights reserved.
using Microsoft.AspNetCore.Mvc;
using CopilotChat.WebApi.Auth;
using CopilotChat.WebApi.Services;
using CopilotChat.WebApi.Options;
using Microsoft.Extensions.Options;
using CopilotChat.WebApi.Storage;
using Newtonsoft.Json;
using CopilotChat.WebApi.Models.Storage;

namespace CopilotChat.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class UserPreferenceController : ControllerBase
{
    private readonly ILogger<UserPreferenceController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ServiceOptions _serviceOptions;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAuthInfo _authInfo;
    private readonly SemanticKernelProvider _kernelProvider;
    private readonly UserPreferenceRepository _userPreferenceRepository;
    private readonly IConfiguration _configuration;
    public UserPreferenceController(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<UserPreferenceController> logger, IAuthInfo authInfo, SemanticKernelProvider kernelProvider, IOptions<ServiceOptions> serviceOptions, UserPreferenceRepository userPreferenceRepository, IServiceProvider serviceProvider)
    {
        this._httpClientFactory = httpClientFactory;
        this._serviceOptions = serviceOptions.Value;
        this._configuration = configuration;
        this._serviceProvider = serviceProvider;
        this._kernelProvider = kernelProvider;
        this._userPreferenceRepository = userPreferenceRepository;
        // Injecting AuthInfo to retrieve user information
        this._authInfo = authInfo;
        this._logger = logger;
    }

    // API endpoint to store the current user's model preference
    [HttpPost("SetUserPreference")]
    public async Task<IActionResult> SetUserModel([FromBody] UserPreference userPreference)
    {
#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            // Get the current user's ID using AuthInfo
            userPreference.UserId = this._authInfo.UserId;

            await this._userPreferenceRepository.SaveUserPreferenceAsync(userPreference);
            return this.Ok(JsonConvert.SerializeObject("User preferences saved."));
        }
        catch (Exception ex)
        {
            return this.StatusCode(500, new { message = "Error setting user preferences", error = ex.Message });
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }

    // API endpoint to get the current user's model preference
    [HttpGet("GetUserPreference")]
    public async Task<IActionResult> GetUserPreference()
    {
#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            // Get the current user's ID using AuthInfo
            string userId = this._authInfo.UserId;

            try
            {
                // Retrieve the user's model preference
                var userPreference = await this._userPreferenceRepository.GetUserPreferenceAsync(userId);
                if (userPreference != null)
                {
                    return this.Ok(JsonConvert.SerializeObject(userPreference));
                }
            }
            catch (KeyNotFoundException)
            {
                // User preference not found, proceed to load default
            }

            // Return default preferences
            var defaultPreferences = new UserPreference(userId, false, false, true, false)
            {
                Id = userId
            };

            return this.Ok(JsonConvert.SerializeObject(defaultPreferences));
        }
        catch (Exception ex)
        {
            return this.StatusCode(500, new { message = "Error retrieving user preference", error = ex.Message });
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }

    // API endpoint to fetch the available models from AOAI
    // [HttpGet("GetModels")]
    // public async Task<IActionResult> GetModels()
    // {
    //     try
    //     {
    //         string armBaseUrl = this._serviceOptions.ARMurl;
    //         var resourceGroupName = this._serviceOptions.ResourceGroupName;
    //         var openAiAccountName = this._serviceOptions.AccountName;
    //         var subscriptionId = this._serviceOptions.SubscriptionId;

    //         var options = new DefaultAzureCredentialOptions();
    //         if (this._serviceOptions.GovernmentDeployment)
    //         {
    //             options.AuthorityHost = AzureAuthorityHosts.AzureGovernment;
    //         }
    //         var credential = new DefaultAzureCredential(options);

    //         // Define the scope for ARM API 
    //         var tokenRequestContext = new TokenRequestContext(new[] { $"{armBaseUrl}/.default" });
    //         var token = await credential.GetTokenAsync(tokenRequestContext);

    //         //Create an HttpClient and set the Authorization header
    //         var httpClient = this._httpClientFactory.CreateClient();  // No name needed
    //         httpClient.BaseAddress = new Uri(armBaseUrl);
    //         httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

    //         //Get the Azure OpenAI account resource
    //         string accountUrl = $"{armBaseUrl}/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.CognitiveServices/accounts/{openAiAccountName}?api-version=2023-05-01";
    //         var accountResponse = await httpClient.GetAsync(accountUrl);

    //         if (!accountResponse.IsSuccessStatusCode)
    //         {
    //             this._logger.LogError($"Failed to retrieve Azure OpenAI account. Status Code: {accountResponse.StatusCode}");
    //             string accountErrorContent = await accountResponse.Content.ReadAsStringAsync();
    //             this._logger.LogError($"Error details: {accountErrorContent}");
    //             return this.StatusCode(500, new { message = "Error retrieving models", error = accountResponse.Content });
    //         }

    //         this._logger.LogInformation("Successfully retrieved Azure OpenAI account.");

    //         // Step 4: Get the list of deployments for the Azure OpenAI account
    //         string deploymentsUrl = $"{armBaseUrl}/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.CognitiveServices/accounts/{openAiAccountName}/deployments?api-version=2023-05-01";
    //         var deploymentsResponse = await httpClient.GetAsync(deploymentsUrl);

    //         if (!deploymentsResponse.IsSuccessStatusCode)
    //         {
    //             this._logger.LogError($"Failed to retrieve deployments. Status Code: {deploymentsResponse.StatusCode}");
    //             string deploymentsErrorContent = await deploymentsResponse.Content.ReadAsStringAsync();
    //             this._logger.LogError($"Error details: {deploymentsErrorContent}");
    //             return this.StatusCode(500, new { message = "Error retrieving models", error = accountResponse.Content });
    //         }

    //         string deploymentsContent = await deploymentsResponse.Content.ReadAsStringAsync();
    //         using JsonDocument jsonDoc = JsonDocument.Parse(deploymentsContent);
    //         var modelNames = new List<string>();

    //         foreach (var element in jsonDoc.RootElement.GetProperty("value").EnumerateArray())
    //         {
    //             string deploymentName = element.GetProperty("name").GetString();
    //             modelNames.Add(deploymentName);
    //         }

    //         return this.Ok(modelNames);
    //     }
    //     catch (Exception ex)
    //     {
    //         return this.StatusCode(500, new { message = "Error retrieving models", error = ex.Message });
    //     }
    // }

    // // API endpoint to store the current user's model preference
    // [HttpPost("SetUserModel")]
    // public async Task<IActionResult> SetUserModel([FromBody] string modelName)
    // {
    //     try
    //     {
    //         // Get the current user's ID using AuthInfo
    //         string userId = this._authInfo.UserId;

    //         // Create or update the user's model preference
    //         var chatPreference = new ChatPreference(userId, modelName);

    //         await this._chatPreferenceRepository.SaveUserPreferenceAsync(chatPreference);

    //         // Update the kernel deployment for this session (existing UpdateAzureAIDeployment method)
    //         this._kernelProvider.UpdateAzureAIDeployment(modelName);

    //         return this.Ok(JsonConvert.SerializeObject("User model preference saved and deployment updated."));
    //     }
    //     catch (Exception ex)
    //     {
    //         return this.StatusCode(500, new { message = "Error setting user model", error = ex.Message });
    //     }
    // }

    // // API endpoint to get the current user's model preference
    // [HttpGet("GetUserModel")]
    // public async Task<IActionResult> GetUserModel()
    // {
    //     try
    //     {
    //         // Get the current user's ID using AuthInfo
    //         string userId = this._authInfo.UserId;

    //         try
    //         {
    //             // Retrieve the user's model preference
    //             var userPreference = await this._chatPreferenceRepository.GetUserPreferenceAsync(userId);
    //             if (userPreference != null)
    //             {
    //                 return this.Ok(JsonConvert.SerializeObject(userPreference.ModelName));
    //             }
    //         }
    //         catch (KeyNotFoundException)
    //         {
    //             // User preference not found, proceed to load default
    //         }

    //         // If no preference is found, return the default value from settings
    //         var memoryOptions = this._serviceProvider?.GetRequiredService<IOptions<KernelMemoryConfig>>().Value;
    //         var azureAIOptions = memoryOptions.GetServiceConfig<AzureOpenAIConfig>(this._configuration, "AzureOpenAIText");

    //         return this.Ok(JsonConvert.SerializeObject(azureAIOptions.Deployment));
    //     }
    //     catch (Exception ex)
    //     {
    //         return this.StatusCode(500, new { message = "Error retrieving user model", error = ex.Message });
    //     }
    // }
}
