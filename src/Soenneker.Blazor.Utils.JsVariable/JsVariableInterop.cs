using Soenneker.Asyncs.Locks;
using Microsoft.JSInterop;
using Soenneker.Atomics.ValueBools;
using Soenneker.Blazor.Utils.Ids;
using Soenneker.Blazor.Utils.JsVariable.Abstract;
using Soenneker.Blazor.Utils.ModuleImport.Abstract;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Blazor.Utils.JsVariable;

public sealed class JsVariableInterop : IJsVariableInterop
{
    private const string _modulePath = "./_content/Soenneker.Blazor.Utils.JsVariable/js/jsvariableinterop.js";

    private readonly IModuleImportUtil _moduleImportUtil;
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly CancellationToken _lifetimeToken;
    private ValueAtomicBool _disposed;
    private readonly AsyncLock _operationsGate = new();
    private int _activeOperations;
    private TaskCompletionSource? _operationsDrained;
    private object?[]? _availabilityArguments;
    private object?[]? _waitArguments;
    private InvocationCancellation? _invocationCancellation;
    private static readonly object _defaultDelay = 16;

    public JsVariableInterop(IModuleImportUtil moduleImportUtil)
    {
        _lifetimeToken = _lifetimeCancellation.Token;
        _moduleImportUtil = moduleImportUtil ?? throw new ArgumentNullException(nameof(moduleImportUtil));
    }

    public async ValueTask<bool> IsVariableAvailable(string variableName, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed.Value, this);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateVariableName(variableName);

        CancellationToken lifetime = EnterOperation(false, cancellationToken, out object?[] arguments, out InvocationCancellation? cancellation);
        try
        {
            CancellationToken linked = cancellation?.Link(lifetime, cancellationToken) ?? lifetime;

            IJSObjectReference module = await _moduleImportUtil.GetContentModuleReference(_modulePath, linked)
                                                               .ConfigureAwait(false);
            arguments[0] = variableName;
            if (module is IJSInProcessObjectReference synchronous)
            {
                linked.ThrowIfCancellationRequested();
                return synchronous.Invoke<bool>("isVariableAvailable", arguments);
            }
            return await module.InvokeAsync<bool>("isVariableAvailable", linked, arguments).ConfigureAwait(false);
        }
        finally
        {
            ExitOperation(false, arguments, true, cancellation);
        }
    }

    public async ValueTask WaitForVariable(string variableName, int delay = 16, int? timeout = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed.Value, this);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateVariableName(variableName);

        if (delay <= 0)
            throw new ArgumentOutOfRangeException(nameof(delay), delay, "Delay must be greater than 0.");

        if (timeout is < 0)
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout must be greater than or equal to 0.");

        CancellationToken lifetime = EnterOperation(true, cancellationToken, out object?[] arguments, out InvocationCancellation? cancellation);
        bool completed = false;
        try
        {
            CancellationToken linked = cancellation?.Link(lifetime, cancellationToken) ?? lifetime;

            IJSObjectReference module = await _moduleImportUtil.GetContentModuleReference(_modulePath, linked).ConfigureAwait(false);
            linked.ThrowIfCancellationRequested();
            if (module is IJSInProcessObjectReference synchronous && IsAvailableSynchronously(synchronous, variableName))
            {
                completed = true;
                return;
            }
            arguments[1] = variableName;
            if ((int)arguments[2]! != delay)
                arguments[2] = delay;
            if ((int?)arguments[3] != timeout)
                arguments[3] = timeout;
            var operationId = (string)arguments[0]!;

            try
            {
                await module.InvokeVoidAsync("waitForVariable", linked, arguments).ConfigureAwait(false);
                completed = true;
            }
            catch (OperationCanceledException) when (linked.IsCancellationRequested)
            {
                try
                {
                    _ = await module.InvokeAsync<bool>("cancelWaitForVariable", CancellationToken.None, operationId).ConfigureAwait(false);
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
        finally
        {
            ExitOperation(true, arguments, completed, cancellation);
        }
    }

    private async ValueTask CancelLifetime()
    {
        try
        {
            await _lifetimeCancellation.CancelAsync().ConfigureAwait(false);
        }
        catch
        {
            // A cancellation callback must not prevent reference cleanup.
        }
        finally
        {
            _lifetimeCancellation.Dispose();
        }
    }

    private bool IsAvailableSynchronously(IJSInProcessObjectReference module, string variableName)
    {
        object?[] arguments;
        using (_operationsGate.LockSync())
        {
            arguments = _availabilityArguments ?? new object?[1];
            _availabilityArguments = null;
        }
        try
        {
            arguments[0] = variableName;
            return module.Invoke<bool>("isVariableAvailable", arguments);
        }
        finally
        {
            arguments[0] = null;
            using (_operationsGate.LockSync())
                _availabilityArguments ??= arguments;
        }
    }

    public async ValueTask DisposeAsync()
    {
        Task drained;
        using (await _operationsGate.Lock().ConfigureAwait(false))
        {
            if (!_disposed.TrySetTrue())
                return;
            drained = _activeOperations == 0 ? Task.CompletedTask
                : (_operationsDrained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)).Task;
        }

        await CancelLifetime();
        // Wait for cancellation cleanup in JS before releasing its module reference.
        await drained.ConfigureAwait(false);
        _availabilityArguments = null;
        _waitArguments = null;
        _invocationCancellation?.Dispose();
        _invocationCancellation = null;
        await _moduleImportUtil.DisposeContentModule(_modulePath)
                               .ConfigureAwait(false);
    }

    private CancellationToken EnterOperation(bool waiting, CancellationToken caller, out object?[] arguments,
        out InvocationCancellation? cancellation)
    {
        using (_operationsGate.LockSync())
        {
            ObjectDisposedException.ThrowIf(_disposed.Value, this);
            CancellationToken lifetime = _lifetimeToken;
            if (waiting)
            {
                arguments = _waitArguments ?? [BlazorIdGenerator.New("jsvar-wait"), null, _defaultDelay, null];
                _waitArguments = null;
            }
            else
            {
                arguments = _availabilityArguments ?? new object?[1];
                _availabilityArguments = null;
            }
            cancellation = null;
            if (caller.CanBeCanceled && caller != lifetime)
            {
                cancellation = _invocationCancellation ?? new InvocationCancellation();
                _invocationCancellation = null;
            }
            _activeOperations++;
            return lifetime;
        }
    }

    private void ExitOperation(bool waiting, object?[] arguments, bool reusable, InvocationCancellation? cancellation)
    {
        arguments[waiting ? 1 : 0] = null;
        bool reusableCancellation = cancellation?.Reset() == true;
        using (_operationsGate.LockSync())
        {
            if (reusableCancellation)
            {
                if (_invocationCancellation is null)
                    _invocationCancellation = cancellation;
                else
                    cancellation!.Dispose();
            }
            // Reuse wait IDs only after JS acknowledges successful completion.
            // Cancellation/failure may leave an older invocation in transit.
            if (reusable)
            {
                if (waiting)
                    _waitArguments ??= arguments;
                else
                    _availabilityArguments ??= arguments;
            }
            if (--_activeOperations == 0)
                _operationsDrained?.TrySetResult();
        }
    }

    private static void ValidateVariableName(string variableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(variableName);

        ReadOnlySpan<char> path = variableName.AsSpan();
        foreach (Range range in path.Split('.'))
        {
            ReadOnlySpan<char> segment = path[range];
            if (segment.IsWhiteSpace())
                throw new ArgumentException("JavaScript variable paths cannot contain empty segments.", nameof(variableName));

            if (segment.SequenceEqual("__proto__") || segment.SequenceEqual("prototype") || segment.SequenceEqual("constructor"))
                throw new ArgumentException("JavaScript variable paths cannot traverse prototype-related properties.", nameof(variableName));
        }
    }
}
