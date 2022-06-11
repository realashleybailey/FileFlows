using System.Reactive.PlatformServices;
using FileFlows.Server.Controllers;
using FileFlows.Server.Helpers;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Workers;

/// <summary>
/// Worker to validate and refresh the user license
/// </summary>
class LicenseValidatorWorker : Worker
{
    const string LicenseRequest_EncryptionKey = "MIIBCgKCAQEAtMPKGqGr2pYyaMvoxE8d6rlL//Rl7be9AqA4inKvAc0MWmGy6MaiWvX2YHJfaddNSo3CXIgt48KQAUte/+ZM5Nja/cYECPDIS51ragsTfSK/jW5WVsOw8GzZlCV0rcQHQJ+MtNb6lBZD89ffOkQZHAQuC8lh4ptHmnQ3nupnUhlQGOAfnHQSqiDV/BUKcJINQAYMmrVHQJwAm1iXz6xq+dOhzaf+aJ28oRLanEsPcfwZpfkhlxCavMIkQNfIiVJBX89aw4U9yAgMbNhwFr9Zy6lOLyjjHNitOGrgEl1CEgsE04DUQWx2OHmN44rrxv1CQn/vam0G8PHzognbqtw0EwIDAQAB";
    
    /// <summary>
    /// Creates a new instance of the license validator worker
    /// </summary>
    public LicenseValidatorWorker() : base(ScheduleType.Daily, 1)
    {
        Trigger();
    }

    protected override void Execute()
    {
        var result = ValidateLicense(AppSettings.Instance.LicenseEmail, AppSettings.Instance.LicenseKey).Result;
        if (AppSettings.Instance.LicenseCode != result.LicenseCode)
        {
            AppSettings.Instance.LicenseCode = result.LicenseCode;
            AppSettings.Instance.Save();
        }
    }
    
    /// <summary>
    /// Validates a license for a give email and key
    /// </summary>
    /// <param name="licenseEmail">the license email</param>
    /// <param name="licenseKey">the license key</param>
    /// <returns>the validation result of the license</returns>
    internal static async Task<(License License, string LicenseCode)> ValidateLicense(string licenseEmail, string licenseKey)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(licenseEmail) || string.IsNullOrWhiteSpace(licenseKey))
                throw new Exception("Unlicensed");
            
            string json = JsonSerializer.Serialize(new LicenseValidationModel
            {
                Key = licenseKey,
                EmailAddress = licenseEmail
            });

            string requestCode = LicenseEncryption.Encrypt(LicenseRequest_EncryptionKey, json);

            const string licenseUrl = "https://fileflows.com/licensing/validate";
            //const string licenseUrl = "https://localhost:7197/licensing/validate";
            var result = await HttpHelper.Post(licenseUrl, new { Code = requestCode });
            if (result.Success == false)
                throw new Exception("Unlicensed");

            string licenseCode = result.Body;
            var license = License.FromCode(licenseCode);
            return (license, licenseCode);
        }
        catch (Exception ex)
        {
            return (new License
            {
                Status = ex.Message == "Unlicensed" ? LicenseStatus.Unlicensed : LicenseStatus.Invalid,
                ProcessingNodes = 2,
            }, string.Empty);
        }
    }
    
    
    class LicenseValidationModel
    {
        public string EmailAddress { get; set; }
        public string Key { get; set; }
    }
    
}