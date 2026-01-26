using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Soenneker.Asyncs.Initializers;
using Soenneker.Blazor.Utils.JsVariable.Abstract;
using Soenneker.Extensions.CancellationTokens;
using Soenneker.Utils.CancellationScopes;
using Soenneker.Utils.Delay;

namespace Soenneker.Blazor.Utils.JsVariable;

/// <inheritdoc cref="IJsVariableInterop"/>
public sealed class JsVariableInterop : IJsVariableInterop
{
    private readonly IJSRuntime _jsRuntime;
    private readonly AsyncInitializer _scriptInitializer;

    private readonly CancellationScope _cancellationScope = new();

    public JsVariableInterop(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        _scriptInitializer = new AsyncInitializer(Initialize);
    }

    private ValueTask Initialize(CancellationToken token)
    {
        return _jsRuntime.InvokeVoidAsync("eval", token, """
                                                            window.isVariableAvailable = function (variableName) {
                                                                return typeof window[variableName] !== 'undefined';
                                                            };
                                                        """);
    }

    private ValueTask<bool> IsVariableAvailableInternal(string variableName, CancellationToken cancellationToken)
    {
        return _jsRuntime.InvokeAsync<bool>("isVariableAvailable", cancellationToken, variableName);
    }

    public async ValueTask<bool> IsVariableAvailable(string variableName, CancellationToken cancellationToken = default)
    {
        var linked = _cancellationScope.CancellationToken.Link(cancellationToken, out var source);

        using (source)
        {
            await _scriptInitializer.Init(linked);
            return await IsVariableAvailableInternal(variableName, linked);
        }
    }

    public async ValueTask WaitForVariable(string variableName, int delay = 100, CancellationToken cancellationToken = default)
    {
        var linked = _cancellationScope.CancellationToken.Link(cancellationToken, out var source);

        using (source)
        {
            await _scriptInitializer.Init(linked);

            while (!await IsVariableAvailableInternal(variableName, linked))
                await DelayUtil.Delay(delay, null, linked);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _scriptInitializer.DisposeAsync();
        await _cancellationScope.DisposeAsync();
    }
}