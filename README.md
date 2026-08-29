[![](https://img.shields.io/nuget/v/soenneker.blazor.utils.jsvariable.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.blazor.utils.jsvariable/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.blazor.utils.jsvariable/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.blazor.utils.jsvariable/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.blazor.utils.jsvariable.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.blazor.utils.jsvariable/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.blazor.utils.jsvariable/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.blazor.utils.jsvariable/actions/workflows/codeql.yml)

# Soenneker.Blazor.Utils.JsVariable

A Blazor interop library that checks (and waits) for the existence of a JS variable.

## Install

```bash
dotnet add package Soenneker.Blazor.Utils.JsVariable
```

## Quick start

```csharp
using Soenneker.Blazor.Utils.JsVariable.Registrars;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
var result = services.AddJsVariableInteropAsScoped();
```

Adds `IJsVariableInterop` as a scoped service.

## What you get

- `IJsVariableInterop` — A Blazor interop library that checks (and waits) for the existence of a JS variable.
- `JsVariableInteropRegistrar` — Registers the Blazor interop service that checks for a JavaScript variable and can wait for it to appear.

## API at a glance

| API | What it does | Result / important behavior |
| --- | --- | --- |
| `IJsVariableInterop.IsVariableAvailable(variableName, cancellationToken)` | Asynchronously checks if a JavaScript variable is available in the global scope. | A `ValueTask{TResult}` that represents the asynchronous operation, containing `true` if the variable is available; otherwise, `false`. |
| `IJsVariableInterop.WaitForVariable(variableName, delay, timeout, cancellationToken)` | Asynchronously waits until a specified JavaScript variable is available in the global scope. | A `ValueTask` that represents the asynchronous operation. |
| `JsVariableInteropRegistrar.AddJsVariableInteropAsScoped(services)` | Adds `IJsVariableInterop` as a scoped service. | The same service collection, so additional registrations can be chained. |

## Important behavior

- `IJsVariableInterop.IsVariableAvailable(variableName, cancellationToken)`: This method ensures the necessary JavaScript is injected before checking for the variable.
- `IJsVariableInterop.WaitForVariable(variableName, delay, timeout, cancellationToken)`: This method ensures the necessary JavaScript is injected and repeatedly checks for the variable's availability until it becomes available or the operation is canceled.

## Practical notes

- Cancellation stops pending work; it does not undo work that has already completed.
- Dispose instances you own when their scope ends so held resources can be released.
