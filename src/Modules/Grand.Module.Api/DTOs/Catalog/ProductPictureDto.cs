using System.ComponentModel.DataAnnotations;

namespace Grand.Module.Api.DTOs.Catalog;

public class ProductPictureDto
{
    [Key] public string PictureId { get; set; }

    public int DisplayOrder { get; set; }
    
    public bool IsDefault { get; set; }
}