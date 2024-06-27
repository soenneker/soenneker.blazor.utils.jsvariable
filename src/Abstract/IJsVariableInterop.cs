using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics.Contracts;
using System;

namespace Soenneker.Blazor.Utils.JsVariable.Abstract;

/// <summary>
/// A Blazor interop library that checks (and waits) for the existence of a JS variable
/// </summary>
public interface IJsVariableInterop : IAsyncDisposable
{
    /// <summary>
    /// Asynchronously checks if a JavaScript variable is available in the global scope.
    /// </summary>
    /// <param name="variableName">The name of the JavaScript variable to check for availability.</param>
    /// <param name="cancellationToken">An optional token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation, containing <c>true</c> if the variable is available; otherwise, <c>false</c>.</returns>
    /// <remarks>This method ensures the necessary JavaScript is injected before checking for the variable.</remarks>
    [Pure]
    ValueTask<bool> IsVariableAvailable(string variableName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously waits until a specified JavaScript variable is available in the global scope.
    /// </summary>
    /// <param name="variableName">The name of the JavaScript variable to wait for.</param>
    /// <param name="delay">The delay in milliseconds between each availability check. The default is 100 milliseconds.</param>
    /// <param name="cancellationToken">An optional token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
    /// <remarks>This method ensures the necessary JavaScript is injected and repeatedly checks for the variable's availability until it becomes available or the operation is canceled.</remarks>
    ValueTask WaitForVariable(string variableName, int delay = 100, CancellationToken cancellationToken = default);
}
