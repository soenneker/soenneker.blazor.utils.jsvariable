using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Soenneker.Asyncs.Initializers;
using Soenneker.Blazor.Utils.JsVariable.Abstract;
using Soenneker.Utils.Delay;

namespace Soenneker.Blazor.Utils.JsVariable;

/// <inheritdoc cref="IJsVariableInterop"/>
public sealed class JsVariableInterop : IJsVariableInterop
{
    private readonly IJSRuntime _jsRuntime;
    private readonly AsyncInitializer _scriptInitializer;

    public JsVariableInterop(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;

        _scriptInitializer = new AsyncInitializer(async token =>
        {
            await _jsRuntime.InvokeVoidAsync("eval", token, """
                                                                window.isVariableAvailable = function (variableName) {
                                                                    return typeof window[variableName] !== 'undefined';
                                                                };
                                                            """);
        });
    }

    public async ValueTask<bool> IsVariableAvailable(string variableName, CancellationToken cancellationToken = default)
    {
        await _scriptInitializer.Init(cancellationToken);
        return await _jsRuntime.InvokeAsync<bool>("isVariableAvailable", cancellationToken, variableName);
    }

    public async ValueTask WaitForVariable(string variableName, int delay = 100, CancellationToken cancellationToken = default)
    {
        await _scriptInitializer.Init(cancellationToken);

        while (!await IsVariableAvailable(variableName, cancellationToken))
            await DelayUtil.Delay(delay, null, cancellationToken);
    }

    public ValueTask DisposeAsync() => _scriptInitializer.DisposeAsync();
}