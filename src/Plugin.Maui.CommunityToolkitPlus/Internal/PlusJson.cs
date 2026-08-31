namespace Plugin.Maui.CommunityToolkitPlus;

sealed class StoredDocument
{
    public int SchemaVersion { get; set; } = 1;
    public int DataVersion { get; set; } = 1;
    public JsonElement Payload { get; set; }
}

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(StoredDocument))]
[JsonSerializable(typeof(IntegrityKeyRecord))]
[JsonSerializable(typeof(RestorationSnapshot))]
[JsonSerializable(typeof(ContributorSnapshot))]
[JsonSerializable(typeof(UpgradeJournalState))]
[JsonSerializable(typeof(UpgradeMigrationState))]
[JsonSerializable(typeof(StartupHealthState))]
[JsonSerializable(typeof(TrustedTimeAnchor))]
[JsonSerializable(typeof(ConsentLedger))]
[JsonSerializable(typeof(ConsentReceiptRecord))]
sealed partial class PlusJsonContext : JsonSerializerContext;
