function resolveVariable(variableName) {
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

export async function waitForVariable(variableName, delay = 16, timeout = null) {
    if (isVariableAvailable(variableName)) {
        return;
    }

    const startedAt = Date.now();

    await new Promise((resolve, reject) => {
        const poll = () => {
            if (isVariableAvailable(variableName)) {
                resolve();
                return;
            }

            if (timeout != null && Date.now() - startedAt >= timeout) {
                reject(new Error(`Timed out waiting for JavaScript variable "${variableName}"`));
                return;
            }

            globalThis.setTimeout(poll, delay);
        };

        poll();
    });
}
