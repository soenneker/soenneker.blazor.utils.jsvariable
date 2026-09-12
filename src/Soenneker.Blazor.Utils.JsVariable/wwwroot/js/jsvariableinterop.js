const pending = new Map();

function resolveParts(parts) {
    if (typeof parts === "string") {
        const descriptor = Object.getOwnPropertyDescriptor(globalThis, parts);
        return descriptor && "value" in descriptor ? descriptor.value : undefined;
    }

    let current = globalThis;

    for (let i = 0; i < parts.length; i++) {
        const part = parts[i];

        if (current == null) {
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

function parseVariableName(variableName) {
    if (typeof variableName !== "string" || variableName.trim().length === 0) {
        throw new Error("JavaScript variable name cannot be null, empty, or whitespace.");
    }

    // Global names are the common case; avoid allocating an array for them.
    if (!variableName.includes(".")) {
        validatePart(variableName);
        return variableName;
    }

    const parts = variableName.split(".");
    for (let i = 0; i < parts.length; i++) {
        validatePart(parts[i]);
    }

    return parts;
}

function validatePart(part) {
    if (part.trim().length === 0) {
        throw new Error("JavaScript variable paths cannot contain empty segments.");
    }
    if (part === "__proto__" || part === "prototype" || part === "constructor") {
        throw new Error("JavaScript variable paths cannot traverse prototype-related properties.");
    }
}

export function isVariableAvailable(variableName) {
    return resolveParts(parseVariableName(variableName)) !== undefined;
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

    const parts = parseVariableName(variableName);
    if (resolveParts(parts) !== undefined) {
        return;
    }

    if (pending.has(operationId)) {
        throw new Error(`A wait operation with id "${operationId}" already exists.`);
    }

    return new Promise((resolvePromise, rejectPromise) => {
        const hasTimeout = timeout != null;
        const deadline = hasTimeout ? performance.now() + timeout : 0;

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

        state.cancel = () => {
            cleanup();
            rejectPromise(new Error(`Waiting for JavaScript variable "${variableName}" was cancelled.`));
        };

        function poll() {
            try {
                if (resolveParts(parts) !== undefined) {
                    cleanup();
                    resolvePromise();
                    return;
                }
                schedule();
            } catch (error) {
                cleanup();
                rejectPromise(error);
            }
        }

        function schedule() {
            const remaining = hasTimeout ? deadline - performance.now() : delay;
            if (remaining <= 0) {
                cleanup();
                rejectPromise(new Error(`Timed out waiting for JavaScript variable "${variableName}".`));
                return;
            }

            state.handle = setTimeout(poll, Math.min(delay, remaining));
        }

        schedule();
    });
}
