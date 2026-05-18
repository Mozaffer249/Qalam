using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Qalam.Infrastructure.OpenApi;

/// <summary>
/// Removes the global Bearer requirement from endpoints marked [AllowAnonymous] so Scalar/Swagger "Try it" works without a token.
/// </summary>
public sealed class AllowAnonymousOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var allowAnonymous = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .Any(m => m is IAllowAnonymous);

        if (allowAnonymous)
            operation.Security = new List<OpenApiSecurityRequirement>();
    }
}
