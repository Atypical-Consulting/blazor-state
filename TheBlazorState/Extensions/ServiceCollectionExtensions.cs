using TheBlazorState.Services;
using Microsoft.Extensions.DependencyInjection;

namespace TheBlazorState.Extensions;

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
        /// builder.Services.AddTheBlazorState();
        /// </code>
        /// </example>
        /// </summary>
        public IServiceCollection AddTheBlazorState()
        {
            services.AddMemoryCache();
            services.AddScoped<StateManager>();
            return services;
        }
    }
}
