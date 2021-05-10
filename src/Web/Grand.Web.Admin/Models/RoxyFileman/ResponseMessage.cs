using Newtonsoft.Json;

namespace Grand.Web.Admin.Models.RoxyFileman
{
    public class ResponseMessage
    {
        [JsonProperty(PropertyName = "res")]
        public string Response { get; set; }

        [JsonProperty(PropertyName = "msg")]
        public string Message { get; set; }
    }
}
