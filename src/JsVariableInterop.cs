using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Soenneker.Blazor.Utils.JsVariable.Abstract;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.AsyncSingleton;
using Soenneker.Utils.Delay;

namespace Soenneker.Blazor.Utils.JsVariable;

///<inheritdoc cref="IJsVariableInterop"/>
public sealed class JsVariableInterop : IJsVariableInterop
{
    private readonly IJSRuntime _jsRuntime;
    private readonly AsyncSingleton _scriptInitializer;

    public JsVariableInterop(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;

        _scriptInitializer = new AsyncSingleton(async (token, _) =>
        {
            await _jsRuntime.InvokeVoidAsync("eval", token, """
                                                                window.isVariableAvailable = function (variableName) {
                                                                    return typeof window[variableName] !== 'undefined';
                                                                };
                                                            """)
                            .NoSync();

            return new object();
        });
    }

    public async ValueTask<bool> IsVariableAvailable(string variableName, CancellationToken cancellationToken = default)
    {
        await _scriptInitializer.Init(cancellationToken).NoSync();
        return await _jsRuntime.InvokeAsync<bool>("isVariableAvailable", cancellationToken, variableName).NoSync();
    }

    public async ValueTask WaitForVariable(string variableName, int delay = 100, CancellationToken cancellationToken = default)
    {
        await _scriptInitializer.Init(cancellationToken).NoSync();

        while (!await IsVariableAvailable(variableName, cancellationToken).NoSync())
        {
            await DelayUtil.Delay(delay, null, cancellationToken).NoSync();
        }
    }

    public ValueTask DisposeAsync()
    {
        return _scriptInitializer.DisposeAsync();
    }
}