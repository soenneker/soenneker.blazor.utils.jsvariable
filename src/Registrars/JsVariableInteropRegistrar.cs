using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Blazor.Utils.JsVariable.Abstract;

namespace Soenneker.Blazor.Utils.JsVariable.Registrars;

/// <summary>
/// A Blazor interop library that checks (and waits) for the existance of a JS variable
/// </summary>
public static class JsVariableInteropRegistrar
{
    /// <summary>
    /// Adds <see cref="IJsVariableInterop"/> as a scoped service. <para/>
    /// </summary>
    public static IServiceCollection AddJsVariableInteropAsScoped(this IServiceCollection services)
    {
        services.TryAddScoped<IJsVariableInterop, JsVariableInterop>();

        return services;
    }
}
