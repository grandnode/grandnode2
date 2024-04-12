using System.ComponentModel.DataAnnotations;

namespace Grand.Api.DTOs.Catalog;

public class ProductCategoryDto
{
    [Key] public string CategoryId { get; set; }

    public bool IsFeaturedProduct { get; set; }
}