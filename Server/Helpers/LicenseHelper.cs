using FileFlows.Server.Controllers;

namespace FileFlows.ServerShared.Helpers;

/// <summary>
/// Helper for licensing
/// </summary>
class LicenseHelper
{
    /// <summary>
    /// Checks if the user is licensed for a feature
    /// </summary>
    /// <param name="feature">the feature to check</param>
    /// <param name="licenseCode">[Optional] license code to check against</param>
    /// <returns>true if licensed, otherwise false</returns>
    internal static bool IsLicensed(LicenseFlags feature, string licenseCode = null)
    {
        // only check null here, because on startup before the db is initialize, we may have gotten
        // the license code which could be empty.   if we checked if null or empty here then, that 
        // would fail as it would try to connect to the database which has not been initialized
        if (licenseCode == null)
        {
            try
            {
                var settings = new SettingsController().Get().Result;
                licenseCode = settings?.LicenseCode;
            }
            catch (Exception)
            {
                licenseCode = string.Empty;
            }
        }
        var license = License.FromCode(licenseCode);
        if (license == null)
            return false;
        if (license.ExpirationDateUtc < DateTime.UtcNow)
            return false;
        return (license.Flags & feature) == feature;
    }

    /// <summary>
    /// Gets the amount of nodes this user is licensed for
    /// </summary>
    /// <returns>the amount of nodes this user is licensed for</returns>
    internal static int GetLicensedProcessingNodes()
    {
        var settings = new SettingsController().Get().Result;
        var license = License.FromCode(settings?.LicenseCode);
        if (license == null)
            return 2;
        if (license.ExpirationDateUtc < DateTime.UtcNow)
            return 2;
        if (license.ProcessingNodes < 2)
            return 2;
        return license.ProcessingNodes;
    }
}