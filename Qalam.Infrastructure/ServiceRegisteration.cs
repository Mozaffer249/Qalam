using System;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Qalam.Data.AppMetaData;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure
{
    public static class ServiceRegisteration
    {
        public static IServiceCollection AddServiceRegisteration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddIdentity<User, Role>(option =>
               {
                   // Password settings.
                   option.Password.RequireDigit = true;
                   option.Password.RequireLowercase = true;
                   option.Password.RequireNonAlphanumeric = true;
                   option.Password.RequireUppercase = true;
                   option.Password.RequiredLength = 6;
                   option.Password.RequiredUniqueChars = 1;

                   // Lockout settings.
                   option.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                   option.Lockout.MaxFailedAccessAttempts = 5;
                   option.Lockout.AllowedForNewUsers = true;

                   // User settings.
                   option.User.AllowedUserNameCharacters =
                   "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                   option.User.RequireUniqueEmail = false;  // Allow phone-only registration
                   option.SignIn.RequireConfirmedEmail = true;
                   

               }).AddEntityFrameworkStores<ApplicationDBContext>().AddDefaultTokenProviders();
            //JWT Authentication
            var jwtSettings = new JwtSettings();
            var emailSettings = new EmailSettings();
            var rateLimitSettings = new RateLimitSettings();
            configuration.GetSection("jwtSettings").Bind(jwtSettings);
            configuration.GetSection("EmailSettings").Bind(emailSettings);
            configuration.GetSection("RateLimiting").Bind(rateLimitSettings);

            services.AddSingleton(jwtSettings);
            services.AddSingleton(emailSettings);
            services.Configure<RateLimitSettings>(configuration.GetSection("RateLimiting"));
            services.Configure<PasswordPolicySettings>(configuration.GetSection("PasswordPolicy"));

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
           .AddJwtBearer(x =>
           {
               x.RequireHttpsMetadata = true; // Changed to true for production security
               x.SaveToken = true;
               x.TokenValidationParameters = new TokenValidationParameters
               {
                   ValidateIssuer = jwtSettings.ValidateIssuer,
                   ValidIssuers = new[] { jwtSettings.Issuer },
                   ValidateIssuerSigningKey = jwtSettings.ValidateIssuerSigningKey,
                   IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings.Secret)),
                   ValidAudience = jwtSettings.Audience,
                   ValidateAudience = jwtSettings.ValidateAudience,
                   ValidateLifetime = jwtSettings.ValidateLifeTime,
                   ClockSkew = TimeSpan.Zero, // Remove default 5-minute tolerance
               };
           });
            //Swagger Gen
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Qalam API",
                    Version = "v1",
                    Description = "Qalam Education Platform API - Manages education content, Quran studies, and teaching configurations",
                    Contact = new OpenApiContact
                    {
                        Name = "Qalam Support",
                        Email = "support@qalam.com"
                    }
                });
                c.EnableAnnotations();

                c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme (Example: 'Bearer 12345abcdef')",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = JwtBearerDefaults.AuthenticationScheme
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = JwtBearerDefaults.AuthenticationScheme
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                // XML Documentation
                var xmlFile = "Qalam.Api.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });

            services.AddAuthorization(option =>
            {
                // Admin policy - requires Admin or SuperAdmin role
                option.AddPolicy(Roles.AdminPolicy, policy =>
                {
                    policy.RequireRole(Roles.Admin, Roles.SuperAdmin);
                });

                // SuperAdmin policy - requires SuperAdmin role only
                option.AddPolicy(Roles.SuperAdminPolicy, policy =>
                {
                    policy.RequireRole(Roles.SuperAdmin);
                });

                // Teacher policy - requires Teacher role
                option.AddPolicy(Roles.TeacherPolicy, policy =>
                {
                    policy.RequireRole(Roles.Teacher);
                });

                // Teacher or Admin policy - for content management
                option.AddPolicy(Roles.TeacherOrAdminPolicy, policy =>
                {
                    policy.RequireRole(Roles.Teacher, Roles.Admin, Roles.SuperAdmin);
                });
            });

            return services;
        }
    }
}

