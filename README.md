[![](https://img.shields.io/nuget/v/soenneker.blazor.utils.jsvariable.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.blazor.utils.jsvariable/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.blazor.utils.jsvariable/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.blazor.utils.jsvariable/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.blazor.utils.jsvariable.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.blazor.utils.jsvariable/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.blazor.utils.jsvariable/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.blazor.utils.jsvariable/actions/workflows/codeql.yml)

# Soenneker.Blazor.Utils.JsVariable

A scoped Blazor interop service for checking whether a `globalThis` property exists and waiting for a script-created global to become available.

Use it when integrating a third-party script that exposes readiness only by assigning a global object. Prefer the script’s documented load event or initialization promise when one exists.

## Installation

```bash
dotnet add package Soenneker.Blazor.Utils.JsVariable
```

```csharp
using Soenneker.Blazor.Utils.JsVariable.Registrars;

builder.Services.AddJsVariableInteropAsScoped();
```

```razor
@using Soenneker.Blazor.Utils.JsVariable.Abstract
@inject IJsVariableInterop JsVariables
```

The checks require browser interop, so call them after interactive rendering rather than during server prerendering.

## Check once

```csharp
bool mapsReady = await JsVariables.IsVariableAvailable("google.maps");
```

Paths are dot-separated and resolved from `globalThis`. Every segment must be an own data property. Prototype traversal and accessor execution are deliberately excluded, so inherited properties and getter-only globals are reported as unavailable.

A property whose value is `null` counts as available. A missing property or one whose value is JavaScript `undefined` does not.

## Wait with a timeout

```csharp
try
{
    await JsVariables.WaitForVariable(
        "google.maps",
        delay: 25,
        timeout: 5_000,
        cancellationToken);

    // The global existed at the end of the wait. Use the owning SDK here.
}
catch (TimeoutException)
{
    // The script did not expose the global within five seconds.
}
```

`delay` and `timeout` are milliseconds. Delay must be greater than zero; timeout can be zero for an immediate check. With no timeout, the wait continues until the value appears, cancellation is requested, or the scoped service is disposed.

Each wait has independent timer state. Cancellation clears its browser timer, settles the pending JavaScript promise, and propagates `OperationCanceledException`. A timeout is surfaced as `TimeoutException`.

Availability is only a point-in-time observation. Another script can replace or remove the property immediately afterward, so the subsequent SDK call must still handle its own errors.

## Path and trust guidance

Use paths defined by the application or SDK documentation. Empty path segments and `__proto__`, `prototype`, or `constructor` segments are rejected.

Do not use global-variable availability as proof that a script is trustworthy, that a user is authorized, or that an SDK response is safe. Any script running in the page can create or replace globals, and values received from JavaScript still require validation.
