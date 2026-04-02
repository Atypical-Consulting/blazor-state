using BlazorStatePlus.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorStatePlus.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="StateManager"/> and its dependencies.
        /// Call this in your <c>Program.cs</c> to enable the library.
        ///
        /// <example>
        /// <code>
        /// builder.Services.AddBlazorStatePlus();
        /// </code>
        /// </example>
        /// </summary>
        public IServiceCollection AddBlazorStatePlus()
        {
            services.AddMemoryCache();
            services.AddScoped<StateManager>();
            return services;
        }
    }
}
