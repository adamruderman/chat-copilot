﻿// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel;

namespace CopilotChat.WebApi.Plugins.NativePlugins.SlideDeckGeneration.Pipeline;

public class SKPipeLineManager
{

    /// <summary>
    /// Invokes a pipeline of functions, running each in order and passing the output from one as the first argument to the next.
    /// </summary>
    /// <param name="functions">The pipeline of functions to invoke.</param>
    /// <param name="kernel">The kernel to use for the operations.</param>
    /// <param name="arguments">The arguments.</param>
    /// <param name="cancellationToken">The cancellation token to monitor for a cancellation request.</param>
    /// <returns></returns>
    public static Task<FunctionResult> InvokePipelineAsync(
        IEnumerable<KernelFunction> functions, Kernel kernel, KernelArguments arguments, CancellationToken cancellationToken) =>
        Pipe(functions).InvokeAsync(kernel, arguments, cancellationToken);

    /// <summary>
    /// Invokes a pipeline of functions, running each in order and passing the output from one as the named argument to the next.
    /// </summary>
    /// <param name="functions">The sequence of functions to invoke, along with the name of the argument to assign to the result of the function's invocation.</param>
    /// <param name="kernel">The kernel to use for the operations.</param>
    /// <param name="arguments">The arguments.</param>
    /// <param name="cancellationToken">The cancellation token to monitor for a cancellation request.</param>
    /// <returns></returns>
    public static Task<FunctionResult> InvokePipelineAsync(
        IEnumerable<(KernelFunction Function, string OutputVariable)> functions, Kernel kernel, KernelArguments arguments, CancellationToken cancellationToken) =>
        Pipe(functions).InvokeAsync(kernel, arguments, cancellationToken);

    /// <summary>
    /// Creates a function whose invocation will invoke each of the supplied functions in sequence.
    /// </summary>
    /// <param name="functions">The pipeline of functions to invoke.</param>
    /// <param name="functionName">The name of the combined operation.</param>
    /// <param name="description">The description of the combined operation.</param>
    /// <returns>The result of the final function.</returns>
    /// <remarks>
    /// The result from one function will be fed into the first argument of the next function.
    /// </remarks>
    public static KernelFunction Pipe(
        IEnumerable<KernelFunction> functions,
        string? functionName = null,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(functions);

        KernelFunction[] funcs = functions.ToArray();
        Array.ForEach(funcs, f => ArgumentNullException.ThrowIfNull(f));

        var funcsAndVars = new (KernelFunction Function, string OutputVariable)[funcs.Length];
        for (int i = 0; i < funcs.Length; i++)
        {
            string p = "";
            if (i < funcs.Length - 1)
            {
                var parameters = funcs[i + 1].Metadata.Parameters;
                if (parameters.Count > 0)
                {
                    p = parameters[0].Name;
                }
            }

            funcsAndVars[i] = (funcs[i], p);
        }

        return Pipe(funcsAndVars, functionName, description);
    }

    /// <summary>
    /// Creates a function whose invocation will invoke each of the supplied functions in sequence.
    /// </summary>
    /// <param name="functions">The pipeline of functions to invoke, along with the name of the argument to assign to the result of the function's invocation.</param>
    /// <param name="functionName">The name of the combined operation.</param>
    /// <param name="description">The description of the combined operation.</param>
    /// <returns>The result of the final function.</returns>
    /// <remarks>
    /// The result from one function will be fed into the first argument of the next function.
    /// </remarks>
    public static KernelFunction Pipe(
        IEnumerable<(KernelFunction Function, string OutputVariable)> functions,
        string? functionName = null,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(functions);

        (KernelFunction Function, string OutputVariable)[] arr = functions.ToArray();
        Array.ForEach(arr, f =>
        {
            ArgumentNullException.ThrowIfNull(f.Function);
            ArgumentNullException.ThrowIfNull(f.OutputVariable);
        });

        return KernelFunctionFactory.CreateFromMethod(async (Kernel kernel, KernelArguments arguments) =>
        {
            FunctionResult? result = null;
            for (int i = 0; i < arr.Length; i++)
            {
                result = await arr[i].Function.InvokeAsync(kernel, arguments).ConfigureAwait(false);
                if (i < arr.Length - 1)
                {
                    arguments[arr[i].OutputVariable] = result.GetValue<object>();
                }
            }

            return result;
        }, functionName, description);
    }

}
