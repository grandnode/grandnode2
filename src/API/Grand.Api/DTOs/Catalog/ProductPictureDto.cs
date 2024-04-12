using System.ComponentModel.DataAnnotations;

namespace Grand.Api.DTOs.Catalog;

public class ProductPictureDto
{
    [Key] public string PictureId { get; set; }

    public int DisplayOrder { get; set; }
}