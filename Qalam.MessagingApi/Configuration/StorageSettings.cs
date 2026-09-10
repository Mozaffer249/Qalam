namespace Qalam.MessagingApi.Configuration;

public enum StorageProvider
{
    Alibaba = 0,
    Wasabi = 1,
}

/// <summary>
/// Selects the active object-storage provider for new uploads.
/// Deletes route by URL host so leftover objects on the other provider remain deletable.
/// </summary>
public class StorageSettings
{
    /// <summary>
    /// <c>alibaba</c> (default) or <c>wasabi</c>. Bound from <c>STORAGE_PROVIDER</c> /
    /// <c>StorageSettings__Provider</c>.
    /// </summary>
    public string Provider { get; set; } = "alibaba";

    public StorageProvider ActiveProvider =>
        string.Equals(Provider, "wasabi", StringComparison.OrdinalIgnoreCase)
            ? StorageProvider.Wasabi
            : StorageProvider.Alibaba;
}
