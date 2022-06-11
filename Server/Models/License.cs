using FileFlows.Server.Helpers;

namespace FileFlows.Server.Models;

class License
{
    public LicenseStatus Status { get; set; }
    public DateTime ExpirationDateUtc { get; set; }
    public LicenseFlags Flags { get; set; }

    public int ProcessingNodes { get; set; }

    internal static License DefaultLicense() => new License
    {
        Status = LicenseStatus.Unlicensed,
        ProcessingNodes = 2
    };

    internal static License FromCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return DefaultLicense();
        try
        {
            const string LicenseResponse_DecryptionKey = "MIIEowIBAAKCAQEAqdurpq85Xtg0haj0LHl//hKBlAFyX4Vsuo2xhIScoYqHUEGoljEZUZdiOe764kiNKOQEqpzbXijoyxGLy4mDtDkEa90L21gbn31mekiIuODdEW9IKmPftrB182hTWYUUg1VlTunkVLVln/ywdf1BfcAHeQn9EcmxwDbqovmMTspKqxrVYgbtEpZZybqbwnyo9m+AKeU+xag3zoAnw2q4OejMLm00BF/uhYDK6uSMQC1Qy15j06KIjY65YT5meDqfkfWsxOEpZ2uCiollFOYsvZPyrNGd6fvKGDD2fKAlvAwSn0PAsMAOUEm+QlZtne4pIADiVnMM9RX7f4OtvVtaXQIDAQABAoIBAAguqc0kwbm85oLNyb1euPivQYi0rSLG1Z8C9lsw3C638p6+GvXpNZQFm9i4l2NRJWOj4EmrtrGJfPVTSg2q+So0WO8tPcX6L5J2Qvp/Bf8J7fxKBQrttrghNf1cuC8mxv8wnOm5QKOH/XZAgOueIIqDNpjxDzzNH3/n5VOme8jLx8PGMCwJWMFK0XdNObD8Dvzv3J9y3X1EVrB/Jm9pcc/2OMieL1kKrJmX3YvA5aXHB6cjbFDL5xwRb9wEOJlx/KlRQQVg7dLVhyMNRXc+gxLPvOR3cbeZ3nb8nGTJeJDSxq1p8+z61wQF5L5Kxche/hZdu1D2rGFOQHUjbrHEJE0CgYEAyFcoChhv9q9RgVwbk26ErIOejcVILIFlMcxbpRoJ/PD4ltXoz7u3UhBT+zsSPRxvT5gPZ6qqEIG5uWlnd1YOdS0KrtSM7K/nBI9IQoN65gTBTMzXSDkuZuZGvs726b4rhvuhDJa6uRyDkKpcsNQkCQ+KUIfnWWYkKq5oJba7S/cCgYEA2QyDI1UkuVtGkWJLL7h1g95abfLOIreV0czMc9uVBCbL+2EyQC44vIwqsRDZkx5/XlvY4bT+o6zAKmwdmCa/QOmWY1SkGBVkDwD6i4mOHnmrV+5icF2OiaSmjtFFOOLteq0SauIWhjEGQgNgoldj6kXVtqVvIUY8rj2yDBrUb0sCgYEAiCYDBelZnbHDmD/6VZVUANFp3TrnM6e0F8Wjum4Zv5YbupYgo5wUl2aVTDT2ziUW2GakgXUQIiunBgRF1mnbZXJ4whucsfVQ8F5XYyxrRwqQOxsyatjBWhjAl0ebsXoVpqQ27JE60DY6iwPb/igNXUL8YoIZjT3G8mKYUJkAbD0CgYBlE+aeNbB8gX1DhzrsZkKTvqDuQvysPkKPCYjNC51B6a9kycbVDLFvXPckrmwkjzdRggRmWBudrX1wRBkkGidG24ElkO06KfwG4LXM9aoxlwesU1+UZH1UrFDEgcBy1Xsyfhbtn4xNwdbgNyJxd7EYEJ2OCUzPeh4YJrMb4AK+MQKBgBXolFhtJWGrKHSfAgXdLMltDErrAO0t0vvQzdZEQExXRhnx3KPfpHMwg5x13bN95aCvodR+136csEOk6UqSuH5xAtZWWkZQE5umBNm1DJs68cERIC6XDishlXkXgAL9qkurmSJ7RK5dcCfZI/uM6BBjs43IqfVUUKmt8Gqbedry";
            string decrypted = LicenseEncryption.Decrypt(LicenseResponse_DecryptionKey, code);
            return JsonSerializer.Deserialize<License>(decrypted);
        }
        catch (Exception)
        {
            return DefaultLicense();
        }
    }
}


/// <summary>
/// License status
/// </summary>
enum LicenseStatus
{
    /// <summary>
    /// Unlicensed, no key
    /// </summary>
    Unlicensed = -1,
    /// <summary>
    /// Invalid license key
    /// </summary>
    Invalid = 0,
    /// <summary>
    /// Valid license
    /// </summary>
    Valid = 1,
    /// <summary>
    /// Expired license
    /// </summary>
    Expired = 2,
    /// <summary>
    /// Revoked license
    /// </summary>
    Revoked = 4
}

/// <summary>
/// License flags
/// </summary>
[Flags]
enum LicenseFlags
{
    /// <summary>
    /// Allowed to use an external database
    /// </summary>
    ExternalDatabase = 1,
    /// <summary>
    /// Allowed to use auto updates
    /// </summary>
    AutoUpdates = 2
}