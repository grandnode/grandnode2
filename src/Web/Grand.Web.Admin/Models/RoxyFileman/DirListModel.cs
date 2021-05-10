using Newtonsoft.Json;

namespace Grand.Web.Admin.Models.RoxyFileman
{
    public class DirListModel
    {
        [JsonProperty(PropertyName = "p")]
        public string Folder { get; set; }

        [JsonProperty(PropertyName = "d")]
        public string FolderCount { get; set; }

        [JsonProperty(PropertyName = "f")]
        public string FileCount { get; set; }

    }
}
