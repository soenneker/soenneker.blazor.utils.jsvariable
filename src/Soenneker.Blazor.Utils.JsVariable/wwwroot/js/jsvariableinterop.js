const pending = new Map();

function resolveParts(parts) {
    let current = globalThis;

    for (let i = 0; i < parts.length; i++) {
        const part = parts[i];

        if (current == null || !(part in current)) {
            return undefined;
        }

        current = current[part];
    }

    return current;
}

function resolveVariable(variableName) {
    const dotIndex = variableName.indexOf(".");

    if (dotIndex === -1) {
        return globalThis[variableName];
    }

    return resolveParts(variableName.split("."));
}

export function isVariableAvailable(variableName) {
    return resolveVariable(variableName) !== undefined;
}

export function cancelWaitForVariable(operationId) {
    const state = pending.get(operationId);

    if (state === undefined) {
        return false;
    }

    state.cancelled = true;

    const handle = state.handle;

    if (handle !== 0) {
        clearTimeout(handle);
        state.handle = 0;
    }

    pending.delete(operationId);
    return true;
}

export function waitForVariable(operationId, variableName, delay, timeout) {
    const dotIndex = variableName.indexOf(".");
    const parts = dotIndex === -1 ? null : variableName.split(".");

    if ((parts === null ? globalThis[variableName] : resolveParts(parts)) !== undefined) {
        return Promise.resolve();
    }

    if (pending.has(operationId)) {
        throw new Error(`A wait operation with id "${operationId}" already exists.`);
    }

    return new Promise((resolvePromise, rejectPromise) => {
        const hasTimeout = timeout != null;
        const started = hasTimeout ? Date.now() : 0;

        const state = {
            cancelled: false,
            handle: 0
        };

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
            return (parts === null ? globalThis[variableName] : resolveParts(parts)) !== undefined;
        }

        function poll() {
            if (state.cancelled) {
                rejectPromise(new Error(`Waiting for JavaScript variable "${variableName}" was cancelled.`));
                return;
            }

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