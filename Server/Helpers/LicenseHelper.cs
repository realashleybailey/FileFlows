using FileFlows.Server.Controllers;

namespace FileFlows.ServerShared.Helpers;

/// <summary>
/// Helper for licensing
/// </summary>
class LicensingHelper
{
    /// <summary>
    /// Checks if the user is licensed for a feature
    /// </summary>
    /// <param name="feature">the feature to check</param>
    /// <returns>true if licensed, otherwise false</returns>
    internal static bool IsLicensed(LicenseFlags feature)
    {
        var settings = new SettingsController().Get().Result;
        var license = License.FromCode(settings?.LicenseCode);
        if (license == null)
            return false;
        if (license.ExpirationDateUtc < DateTime.UtcNow)
            return false;
        return (license.Flags & feature) == feature;
    }
}