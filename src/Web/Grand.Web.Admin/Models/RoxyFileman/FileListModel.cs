using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Models.RoxyFileman
{
    public class FileListModel
    {
        [JsonProperty(PropertyName = "p")]
        public string Path { get; set; }

        [JsonProperty(PropertyName = "t")]
        public string LastWriteTime { get; set; }

        [JsonProperty(PropertyName = "s")]
        public string Length { get; set; }

        [JsonProperty(PropertyName = "w")]
        public string Width { get; set; }

        [JsonProperty(PropertyName = "h")]
        public string Height { get; set; }
    }
}
