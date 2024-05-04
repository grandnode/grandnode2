using System.ComponentModel.DataAnnotations;

namespace Grand.Api.DTOs.Catalog;

public class ProductCollectionDto
{
    [Key] public string CollectionId { get; set; }

    public bool IsFeaturedProduct { get; set; }
}