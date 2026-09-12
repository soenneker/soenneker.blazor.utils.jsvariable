using System;
using System.Threading;

namespace Soenneker.Blazor.Utils.JsVariable;

internal sealed class InvocationCancellation : IDisposable
{
    private readonly CancellationTokenSource _source = new();
    private CancellationTokenRegistration _lifetimeRegistration;
    private CancellationTokenRegistration _callerRegistration;

    internal CancellationToken Link(CancellationToken lifetime, CancellationToken caller)
    {
        // Register without capturing ExecutionContext, as linked token sources do.
        _lifetimeRegistration = lifetime.UnsafeRegister(static state => ((CancellationTokenSource)state!).Cancel(), _source);
        _callerRegistration = caller.UnsafeRegister(static state => ((CancellationTokenSource)state!).Cancel(), _source);
        return _source.Token;
    }

    internal bool Reset()
    {
        // Dispose registrations before reset: a callback from an old caller must
        // finish before this source can be lent to another invocation.
        _callerRegistration.Dispose();
        _lifetimeRegistration.Dispose();
        _callerRegistration = default;
        _lifetimeRegistration = default;
        if (_source.TryReset())
            return true;
        _source.Dispose();
        return false;
    }

    public void Dispose() => _source.Dispose();
}
