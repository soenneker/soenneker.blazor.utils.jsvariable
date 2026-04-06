const waiters = new Map();

function validateVariableName(variableName) {
    if (typeof variableName !== 'string' || variableName.trim().length === 0) {
        throw new Error('variableName must be a non-empty string.');
    }
}

function resolveVariable(variableName) {
    validateVariableName(variableName);

    const parts = variableName.split('.');
    let current = globalThis;

    for (const part of parts) {
        if (current == null || !(part in current)) {
            return undefined;
        }

        current = current[part];
    }

    return current;
}

export function isVariableAvailable(variableName) {
    return typeof resolveVariable(variableName) !== 'undefined';
}

export function cancelWaitForVariable(operationId) {
    if (typeof operationId !== 'string' || operationId.length === 0) {
        throw new Error('operationId must be a non-empty string.');
    }

    const waiter = waiters.get(operationId);

    if (!waiter) {
        return false;
    }

    waiter.cancelled = true;

    if (waiter.timeoutHandle != null) {
        globalThis.clearTimeout(waiter.timeoutHandle);
        waiter.timeoutHandle = null;
    }

    waiters.delete(operationId);
    return true;
}

export async function waitForVariable(operationId, variableName, delay = 16, timeout = null) {
    validateVariableName(variableName);

    if (typeof operationId !== 'string' || operationId.length === 0) {
        throw new Error('operationId must be a non-empty string.');
    }

    delay = Number.isFinite(delay) && delay >= 0 ? delay : 16;
    timeout = Number.isFinite(timeout) && timeout >= 0 ? timeout : null;

    if (typeof resolveVariable(variableName) !== 'undefined') {
        return;
    }

    if (waiters.has(operationId)) {
        throw new Error(`A wait operation with id "${operationId}" already exists.`);
    }

    const startedAt = Date.now();

    await new Promise((resolve, reject) => {
        const state = {
            cancelled: false,
            timeoutHandle: null
        };

        waiters.set(operationId, state);

        const cleanup = () => {
            if (state.timeoutHandle != null) {
                globalThis.clearTimeout(state.timeoutHandle);
                state.timeoutHandle = null;
            }

            waiters.delete(operationId);
        };

        const poll = () => {
            if (state.cancelled) {
                cleanup();
                reject(new Error(`Waiting for JavaScript variable "${variableName}" was cancelled.`));
                return;
            }

            if (typeof resolveVariable(variableName) !== 'undefined') {
                cleanup();
                resolve();
                return;
            }

            if (timeout != null && (Date.now() - startedAt) >= timeout) {
                cleanup();
                reject(new Error(`Timed out waiting for JavaScript variable "${variableName}".`));
                return;
            }

            state.timeoutHandle = globalThis.setTimeout(poll, delay);
        };

        poll();
    });
}