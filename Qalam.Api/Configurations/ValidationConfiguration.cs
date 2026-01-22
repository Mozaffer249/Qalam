using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Authentication;
using Qalam.Core.Resources.Shared;
using System.Net;

namespace Qalam.Api.Configurations;

public static class ValidationConfiguration
{
	public static IServiceCollection ConfigureValidationBehavior(this IServiceCollection services)
	{
		services.Configure<ApiBehaviorOptions>(options =>
		{
			options.InvalidModelStateResponseFactory = context =>
			{
				// Get localizers from DI
				var sharedLocalizer = context.HttpContext.RequestServices
					.GetRequiredService<IStringLocalizer<SharedResources>>();
				var authLocalizer = context.HttpContext.RequestServices
					.GetRequiredService<IStringLocalizer<AuthenticationResources>>();

				// Format errors - translate default model binding messages
				var errors = context.ModelState
					.Where(e => e.Value?.Errors.Count > 0)
					.SelectMany(e => e.Value!.Errors.Select(er =>
					{
						var errorMessage = er.ErrorMessage;

						// Check if it's a default "field is required" message
						if (string.IsNullOrEmpty(errorMessage) ||
							errorMessage.Contains("field is required", StringComparison.OrdinalIgnoreCase))
						{
							return GetSpecificRequiredMessage(e.Key, authLocalizer, sharedLocalizer);
						}

						return errorMessage;
					}))
					.ToList();

				var response = new Response<string>
				{
					StatusCode = HttpStatusCode.BadRequest,
					Succeeded = false,
					Message = sharedLocalizer[SharedResourcesKeys.ValidationFailed],
					Errors = errors,
					Data = null!
				};

				return new BadRequestObjectResult(response);
			};
		});

		return services;
	}

	private static string GetSpecificRequiredMessage(
		string fieldKey,
		IStringLocalizer<AuthenticationResources> authLocalizer,
		IStringLocalizer<SharedResources> sharedLocalizer)
	{
		var cleanFieldName = fieldKey;
		if (fieldKey.Contains('['))
		{
			var parts = fieldKey.Split('.');
			cleanFieldName = parts.Length > 1 ? parts[^1] : fieldKey.Split('[')[0];
		}

		// Try to get from registry first
		if (ValidationFieldMappings.TryGetMapping(cleanFieldName, out var resourceType, out var resourceKey))
		{
			// Get the appropriate localizer based on resource type
			if (resourceType == typeof(AuthenticationResources))
			{
				return authLocalizer[resourceKey].Value;
			}
			// Add more resource types as needed
		}

		// Fallback: Try convention-based lookup (FieldName + "Required")
		var conventionKey = $"{cleanFieldName}Required";
		var conventionValue = authLocalizer[conventionKey];
		if (!conventionValue.ResourceNotFound)
		{
			return conventionValue.Value;
		}

		// Final fallback: Generic message
		return sharedLocalizer[SharedResourcesKeys.FieldRequired, cleanFieldName].Value;
	}
}
