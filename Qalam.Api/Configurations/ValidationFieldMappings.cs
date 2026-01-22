using Qalam.Core.Resources.Authentication;
using Qalam.Core.Resources.Shared;

namespace Qalam.Api.Configurations;

public static class ValidationFieldMappings
{
	// Convention: FieldName + "Required" = ResourceKey
	// Example: DocumentNumber -> DocumentNumberRequired
	private static readonly Dictionary<string, (Type ResourceType, string ResourceKey)> FieldMap = new()
	{
		// Teacher Registration Fields
		["DocumentNumber"] = (typeof(AuthenticationResources), AuthenticationResourcesKeys.DocumentNumberRequired),
		["IdentityDocumentFile"] = (typeof(AuthenticationResources), AuthenticationResourcesKeys.IdentityDocumentFileRequired),
		["IssuingCountryCode"] = (typeof(AuthenticationResources), AuthenticationResourcesKeys.IssuingCountryRequiredForPassport),
		["File"] = (typeof(AuthenticationResources), AuthenticationResourcesKeys.CertificateFileRequired),
		["Certificates"] = (typeof(AuthenticationResources), AuthenticationResourcesKeys.CertificatesRequired),

		// Add more fields as needed...
		// Future: Auto-discover from resource files
	};

	public static bool TryGetMapping(string fieldName, out Type resourceType, out string resourceKey)
	{
		if (FieldMap.TryGetValue(fieldName, out var mapping))
		{
			resourceType = mapping.ResourceType;
			resourceKey = mapping.ResourceKey;
			return true;
		}

		resourceType = null!;
		resourceKey = null!;
		return false;
	}
}
