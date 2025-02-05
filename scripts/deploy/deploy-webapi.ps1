<#
.SYNOPSIS
Deploy Chat Copilot application to Azure
#>

param(
    [Parameter(Mandatory)]
    [ValidateSet("AzureCloud", "AzureUSGovernment")]
    [string]
    # Azure cloud environment name
    $Environment = "AzureCloud",

    [Parameter(Mandatory)]
    [string]
    # Subscription to which to make the deployment
    $Subscription,

    [Parameter(Mandatory)]
    [string]
    # Resource group to which to make the deployment
    $ResourceGroupName,
    
    [string]
    # Name of the previously deployed Azure deployment 
    $DeploymentName,

    [string]
    # Name of the web app deployment slot
    $DeploymentSlot,

    [string]
    $PackageFilePath = "$PSScriptRoot/out/webapi.zip",

    [switch]
    # Don't attempt to add our URIs in frontend app registration's redirect URIs
    $SkipAppRegistration,
    
    [switch]
    # Don't attempt to add our URIs in CORS origins for our plugins
    $SkipCorsRegistration,

    [string]
    # Web API name
    $WebApi,

    [string]
    # Web API URL
    $WebApiUrl
)

# Ensure $PackageFilePath exists
if (!(Test-Path $PackageFilePath)) {
    Write-Error "Package file '$PackageFilePath' does not exist. Have you run 'package-webapi.ps1' yet?"
    exit 1
}

if ($Environment -eq "AzureUSGovernment") {
    $suffix = "us"
}
else {
    $suffix = "com"
}

Write-Host "Setting Azure cloud environment to $Environment..."
az cloud set --name $Environment
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

az account show --output none
if ($LASTEXITCODE -ne 0) {
    Write-Host "Log into your Azure account"
    az login --output none
}

az account set -s $Subscription
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if (-not $WebApi -or -not $WebApiUrl) {
    if (-not $DeploymentName) {
        Write-Error "Either DeploymentName or both WebApi and WebApiUrl must be provided."
        exit 1
    }

    Write-Host "Getting Azure WebApp resource name..."
    $deployment = $(az deployment group show --name $DeploymentName --resource-group $ResourceGroupName --output json | ConvertFrom-Json)
    $WebApiUrl = $deployment.properties.outputs.webapiUrl.value
    $WebApi = $deployment.properties.outputs.webapiName.value
    $pluginNames = $deployment.properties.outputs.pluginNames.value

    if ($null -eq $WebApi) {
        Write-Error "Could not get Azure WebApp resource name from deployment output."
        exit 1
    }
}

Write-Host "Azure WebApp name: $WebApi"

Write-Host "Configuring Azure WebApp to run from package..."
az webapp config appsettings set --resource-group $ResourceGroupName --name $WebApi --settings WEBSITE_RUN_FROM_PACKAGE="1" --output none
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

# Set up deployment command as a string
$azWebAppCommand = "az webapp deployment source config-zip --resource-group $ResourceGroupName --name $WebApi --src $PackageFilePath"

# Check if DeploymentSlot parameter was passed
$origins = @("$WebApiUrl")
if ($DeploymentSlot) {
    Write-Host "Checking if slot $DeploymentSlot exists for '$WebApi'..."
    $azWebAppCommand += " --slot $DeploymentSlot"
    $slotInfo = az webapp deployment slot list --resource-group $ResourceGroupName --name $WebApi | ConvertFrom-JSON
    $availableSlots = $slotInfo | Select-Object -Property Name
    $origins = $slotInfo | Select-Object -Property defaultHostName
    $slotExists = false

    foreach ($slot in $availableSlots) { 
        if ($slot.name -eq $DeploymentSlot) { 
            # Deployment slot was found we dont need to create it
            $slotExists = true
        } 
    }

    # Web App deployment slot does not exist, create it
    if (!$slotExists) {
        Write-Host "Deployment slot $DeploymentSlot does not exist, creating..."
        az webapp deployment slot create --slot $DeploymentSlot --resource-group $ResourceGroupName --name $WebApi --output none
        $origins = az webapp deployment slot list --resource-group $ResourceGroupName --name $WebApi | ConvertFrom-JSON | Select-Object -Property defaultHostName
    }
}

Write-Host "Deploying '$PackageFilePath' to Azure WebApp '$WebApi'..."

# Invoke the command string
Invoke-Expression $azWebAppCommand
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if (-Not $SkipAppRegistration) {
    $webapiSettings = $(az webapp config appsettings list --name $WebApi --resource-group $ResourceGroupName | ConvertFrom-JSON)
    $frontendClientId = ($webapiSettings | Where-Object -Property name -EQ -Value Frontend:AadClientId).value
    $objectId = (az ad app show --id $frontendClientId | ConvertFrom-Json).id
    $redirectUris = (az rest --method GET --uri "https://graph.microsoft.$suffix/v1.0/applications/$objectId" --headers 'Content-Type=application/json' | ConvertFrom-Json).spa.redirectUris
    $needToUpdateRegistration = $false

    foreach ($address in $origins) {
        $origin = "https://$address"
        Write-Host "Ensuring '$origin' is included in AAD app registration's redirect URIs..."

        if ($redirectUris -notcontains "$origin") {
            $redirectUris += "$origin"
            $needToUpdateRegistration = $true
        }
    }

    if ($needToUpdateRegistration) {
        $body = "{spa:{redirectUris:["
        foreach ($uri in $redirectUris) {
            $body += "'$uri',"
        }
        $body += "]}}"

        az rest `
            --method PATCH `
            --uri "https://graph.microsoft.$suffix/v1.0/applications/$objectId" `
            --headers 'Content-Type=application/json' `
            --body $body
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Failed to update AAD app registration - Use -SkipAppRegistration switch to skip this step"
            exit $LASTEXITCODE
        }
    }
}

if (-Not $SkipCorsRegistration) {
    foreach ($pluginName in $pluginNames) {
        $allowedOrigins = $((az webapp cors show --name $pluginName --resource-group $ResourceGroupName --subscription $Subscription | ConvertFrom-Json).allowedOrigins)
        foreach ($address in $origins) {
            $origin = "https://$address"
            Write-Host "Ensuring '$origin' is included in CORS origins for plugin '$pluginName'..."
            if (-not $allowedOrigins -contains $origin) {
                az webapp cors add --name $pluginName --resource-group $ResourceGroupName --subscription $Subscription --allowed-origins $origin
                if ($LASTEXITCODE -ne 0) {
                    Write-Host "Failed to update plugin CORS URIs - Use -SkipCorsRegistration switch to skip this step"
                    exit $LASTEXITCODE
                }
            }
        }
    }
}

Write-Host "To verify your deployment, go to 'https://$WebApiUrl/' in your browser."