using Microsoft.JSInterop;
using Soenneker.Blazor.Utils.Ids;
using Soenneker.Blazor.Utils.JsVariable.Abstract;
using Soenneker.Blazor.Utils.ModuleImport.Abstract;
using Soenneker.Extensions.CancellationTokens;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.CancellationScopes;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Blazor.Utils.JsVariable;

/// <inheritdoc cref="IJsVariableInterop"/>
public sealed class JsVariableInterop : IJsVariableInterop
{
    private const string _modulePath = "./_content/Soenneker.Blazor.Utils.JsVariable/js/jsvariableinterop.js";

    private readonly IModuleImportUtil _moduleImportUtil;
    private readonly CancellationScope _cancellationScope = new();

    public JsVariableInterop(IModuleImportUtil moduleImportUtil)
    {
        _moduleImportUtil = moduleImportUtil;
    }

    public async ValueTask<bool> IsVariableAvailable(string variableName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(variableName);

        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);

        using (source)
        {
            IJSObjectReference module = await _moduleImportUtil.GetContentModuleReference(_modulePath, linked)
                                                               .NoSync();
            return await module.InvokeAsync<bool>("isVariableAvailable", linked, variableName)
                               .NoSync();
        }
    }

    public async ValueTask WaitForVariable(string variableName, int delay = 16, int? timeout = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(variableName);

        if (delay < 0)
            throw new ArgumentOutOfRangeException(nameof(delay), delay, "Delay must be greater than or equal to 0.");

        if (timeout is < 0)
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout must be greater than or equal to 0.");

        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);
        var operationId = BlazorIdGenerator.New("jsvar-wait");

        using (source)
        {
            IJSObjectReference module = await _moduleImportUtil.GetContentModuleReference(_modulePath, linked).NoSync();

            try
            {
                await module.InvokeVoidAsync("waitForVariable", linked, operationId, variableName, delay, timeout).NoSync();
            }
            catch (OperationCanceledException) when (linked.IsCancellationRequested)
            {
                try
                {
                    _ = await module.InvokeAsync<bool>("cancelWaitForVariable", CancellationToken.None, operationId).NoSync();
                }
                catch
                {
                }

                throw;
            }
            catch (JSException ex) when (timeout.HasValue && ex.Message.Contains("Timed out waiting for JavaScript variable", StringComparison.Ordinal))
            {
                throw new TimeoutException(ex.Message, ex);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _moduleImportUtil.DisposeContentModule(_modulePath)
                               .NoSync();
        await _cancellationScope.DisposeAsync()
                                .NoSync();
    }
}