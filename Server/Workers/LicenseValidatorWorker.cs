using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Workers;

/// <summary>
/// Worker to validate and refresh the user license
/// </summary>
class LicenseValidatorWorker : Worker
{
    /// <summary>
    /// Creates a new instance of the license validator worker
    /// </summary>
    public LicenseValidatorWorker() : base(ScheduleType.Daily, 1)
    {
        Trigger();
    }

    protected override void Execute()
    {
        var controller = new SettingsController();
        var settings = controller.Get().Result;
        ValidateLicense(settings.LicenseEmail, settings.LicenseKey);

        if (string.IsNullOrEmpty(settings.LicenseCode))
        {
        }
        //controller.UpdateLicenseCode(string.Empty);
    }
    
    /// <summary>
    /// Validates a license for a give email and key
    /// </summary>
    /// <param name="licenseEmail">the license email</param>
    /// <param name="licenseKey">the license key</param>
    /// <returns>the validation result of the license</returns>
    internal static async Task<LicenseValidationResult> ValidateLicense(string licenseEmail, string licenseKey)
    {

        const string licenseUrl = "https://fileflows.com/licensing/validate";
        if (string.IsNullOrWhiteSpace(licenseEmail) || string.IsNullOrWhiteSpace(licenseKey))
        {
            return new LicenseValidationResult
            {
                Status = LicenseStatus.Unlicensed,
                ProcessNodes = 2,
            };
        }

        string json = System.Text.Json.JsonSerializer.Serialize(new LicenseValidationModel
        {

        });
        
        HttpHelper.Post<LicenseValidationResult>
    }
    
    
    class LicenseValidationModel
    {
        public string EmailAddress { get; set; }
        public string Key { get; set; }
    }
    
}
class LicenseValidationResult
{
    public LicenseStatus Status { get; set; }
    public DateTime ExpirationDateUtc { get; set; }
    public LicenseFlags Flags { get; set; }

    public int ProcessNodes { get; set; }

    internal string GenerateCode()
    {
        
    }
}