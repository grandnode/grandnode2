using System.Net.Http;
using System.Threading.Tasks;

namespace Grand.Business.System.Utilities
{
    public static class DownloadUrl
    {
        public static async Task<byte[]> DownloadFile(string url)
        {
            using (var client = new HttpClient())
            {
                using (var result = await client.GetAsync(url))
                {
                    if (result.IsSuccessStatusCode)
                    {
                        return await result.Content.ReadAsByteArrayAsync();
                    }
                }
            }
            return null;
        }
    }
}
