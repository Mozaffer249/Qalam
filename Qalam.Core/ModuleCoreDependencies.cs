using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Qalam.Core.Behaviors;
using System.Reflection;

namespace Qalam.Core
{
    public static class ModuleCoreDependencies
    {
        public static IServiceCollection AddCoreDependencies(this IServiceCollection services)
        {
            // Register MediatR
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly()));

            // Register AutoMapper
            services.AddAutoMapper(Assembly.GetExecutingAssembly());

            // Register FluentValidation validators
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            // Register User Identity Behavior (runs before validation)
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UserIdentityBehavior<,>));

            // Register Validation Behavior
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            return services;
        }
    }
}

