using Microsoft.Extensions.DependencyInjection;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.InfrastructureBases;
using Qalam.Infrastructure.Repositories;
using Qalam.Infrastructure.Seeder;

namespace Qalam.Infrastructure
{
    public static class ModuleInfrastructureDependencies
    {
        public static IServiceCollection AddInfrastructureDependencies(this IServiceCollection services)
        {
            services.AddTransient<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddTransient(typeof(IGenericRepositoryAsync<>), typeof(GenericRepositoryAsync<>));

            // Database Seeder
            services.AddTransient<DatabaseSeeder>();
            services.AddTransient<RoleSeeder>();
            services.AddTransient<UserSeeder>();

            return services;
        }
    }
}

