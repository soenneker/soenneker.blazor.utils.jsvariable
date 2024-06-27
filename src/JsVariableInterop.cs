using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Soenneker.Blazor.Utils.JsVariable.Abstract;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.AsyncSingleton;

namespace Soenneker.Blazor.Utils.JsVariable;

///<inheritdoc cref="IJsVariableInterop"/>
public class JsVariableInterop : IJsVariableInterop
{
    private readonly IJSRuntime _jsRuntime;
    private readonly AsyncSingleton<object> _scriptInitializer;

    public JsVariableInterop(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;

        _scriptInitializer = new AsyncSingleton<object>(async () =>
        {
            await _jsRuntime.InvokeVoidAsync("eval", @"
                window.isVariableAvailable = function (variableName) {
                    return typeof window[variableName] !== 'undefined';
                };
            ");

            return new object();
        });
    }

    public async ValueTask<bool> IsVariableAvailable(string variableName, CancellationToken cancellationToken = default)
    {
        _ = await _scriptInitializer.Get().NoSync();
        return await _jsRuntime.InvokeAsync<bool>("isVariableAvailable", cancellationToken, variableName);
    }

    public async ValueTask WaitForVariable(string variableName, int delay = 100, CancellationToken cancellationToken = default)
    {
        _ = await _scriptInitializer.Get().NoSync();

        while (!await IsVariableAvailable(variableName, cancellationToken).NoSync())
        {
            await Task.Delay(delay, cancellationToken).NoSync();
        }
    }

    public ValueTask DisposeAsync()
    {
        return _scriptInitializer.DisposeAsync();
    }
}