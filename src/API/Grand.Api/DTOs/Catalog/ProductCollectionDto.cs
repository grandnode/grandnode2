using System.ComponentModel.DataAnnotations;

namespace Grand.Api.DTOs.Catalog
{
    public partial class ProductCollectionDto
    {
        [Key]
        public string CollectionId { get; set; }
        public bool IsFeaturedProduct { get; set; }
    }
}
