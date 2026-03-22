using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Soenneker.Blazor.Utils.JsVariable.Abstract;
using Soenneker.Blazor.Utils.ModuleImport.Abstract;
using Soenneker.Extensions.CancellationTokens;
using Soenneker.Utils.CancellationScopes;

namespace Soenneker.Blazor.Utils.JsVariable;

/// <inheritdoc cref="IJsVariableInterop"/>
public sealed class JsVariableInterop : IJsVariableInterop
{
    private const string _modulePath = "Soenneker.Blazor.Utils.JsVariable/js/jsvariableinterop.js";

    private readonly IModuleImportUtil _moduleImportUtil;
    private readonly CancellationScope _cancellationScope = new();

    public JsVariableInterop(IModuleImportUtil moduleImportUtil)
    {
        _moduleImportUtil = moduleImportUtil;
    }

    public async ValueTask<bool> IsVariableAvailable(string variableName, CancellationToken cancellationToken = default)
    {
        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);

        using (source)
        {
            IJSObjectReference module = await _moduleImportUtil.Import(_modulePath, linked);
            return await module.InvokeAsync<bool>("isVariableAvailable", linked, variableName);
        }
    }

    public async ValueTask WaitForVariable(string variableName, int delay = 16, int? timeout = null, CancellationToken cancellationToken = default)
    {
        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);

        using (source)
        {
            IJSObjectReference module = await _moduleImportUtil.Import(_modulePath, linked);
            await module.InvokeVoidAsync("waitForVariable", linked, variableName, delay, timeout);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _moduleImportUtil.DisposeModule(_modulePath);
        await _cancellationScope.DisposeAsync();
    }
}