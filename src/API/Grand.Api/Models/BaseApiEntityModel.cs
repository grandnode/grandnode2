using System.ComponentModel.DataAnnotations;

namespace Grand.Api.Models
{
    public class BaseApiEntityModel
    {
        [Key]
        public string Id { get; set; }
    }
}
