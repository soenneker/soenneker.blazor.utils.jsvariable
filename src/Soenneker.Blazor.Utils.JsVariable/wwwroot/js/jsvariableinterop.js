const pending = new Map();

function resolveParts(parts) {
    let current = globalThis;

    for (let i = 0; i < parts.length; i++) {
        const part = parts[i];

        if (current == null || part === "__proto__" || part === "prototype" || part === "constructor") {
            return undefined;
        }

        const descriptor = Object.getOwnPropertyDescriptor(Object(current), part);

        if (descriptor === undefined || !("value" in descriptor)) {
            return undefined;
        }

        current = descriptor.value;
    }

    return current;
}

function resolveVariable(variableName) {
    if (typeof variableName !== "string" || variableName.trim().length === 0) {
        throw new Error("JavaScript variable name cannot be null, empty, or whitespace.");
    }

    const parts = variableName.split(".");

    if (parts.some(part => part.trim().length === 0)) {
        throw new Error("JavaScript variable paths cannot contain empty segments.");
    }

    return resolveParts(parts);
}

export function isVariableAvailable(variableName) {
    return resolveVariable(variableName) !== undefined;
}

export function cancelWaitForVariable(operationId) {
    const state = pending.get(operationId);

    if (state === undefined) {
        return false;
    }

    state.cancel();
    return true;
}

export function waitForVariable(operationId, variableName, delay, timeout) {
    if (!Number.isInteger(delay) || delay <= 0) {
        throw new Error("Delay must be a positive integer.");
    }

    if (timeout != null && (!Number.isInteger(timeout) || timeout < 0)) {
        throw new Error("Timeout must be a non-negative integer.");
    }

    if (resolveVariable(variableName) !== undefined) {
        return Promise.resolve();
    }

    if (pending.has(operationId)) {
        throw new Error(`A wait operation with id "${operationId}" already exists.`);
    }

    return new Promise((resolvePromise, rejectPromise) => {
        const hasTimeout = timeout != null;
        const started = hasTimeout ? Date.now() : 0;

        const state = { handle: 0, cancel: null };

        pending.set(operationId, state);

        function cleanup() {
            const handle = state.handle;

            if (handle !== 0) {
                clearTimeout(handle);
                state.handle = 0;
            }

            pending.delete(operationId);
        }

        function isAvailable() {
            return resolveVariable(variableName) !== undefined;
        }

        state.cancel = () => {
            cleanup();
            rejectPromise(new Error(`Waiting for JavaScript variable "${variableName}" was cancelled.`));
        };

        function poll() {
            if (isAvailable()) {
                cleanup();
                resolvePromise();
                return;
            }

            if (hasTimeout && (Date.now() - started) >= timeout) {
                cleanup();
                rejectPromise(new Error(`Timed out waiting for JavaScript variable "${variableName}".`));
                return;
            }

            state.handle = setTimeout(poll, delay);
        }

        poll();
    });
}
