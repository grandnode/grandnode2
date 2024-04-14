using System.Text.Json.Serialization;

namespace Grand.Web.Common.Security.Captcha;

public class GoogleReCaptchaResponse
{
    public bool Success { get; set; }

    [JsonPropertyName("error-codes")] public List<string> ErrorCodes { get; set; } = new();

    public decimal Score { get; set; }
}