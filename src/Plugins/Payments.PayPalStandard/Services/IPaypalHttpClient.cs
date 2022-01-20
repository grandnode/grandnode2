namespace Payments.PayPalStandard.Services
{
    public interface IPaypalHttpClient
    {
        Task<(bool success, Dictionary<string, string> values)> VerifyIpn(string formString);
        Task<(bool status, Dictionary<string, string> values, string response)> GetPdtDetails(string tx);
    }
}
