using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Order.ExternalOrderApi.Models;

public class ExternalOrderPayload
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("totalElements")]
    public int TotalElements { get; set; }

    [JsonPropertyName("content")]
    public List<ExternalOrderModel> Content { get; set; }
}

public class ExternalOrderModel
{
    [Required]
    [JsonPropertyName("orderNumber")]
    public string? OrderNumber { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    [JsonPropertyName("grossAmount")]
    public double GrossAmount { get; set; }

    [JsonPropertyName("totalDiscount")]
    public double TotalDiscount { get; set; }

    [JsonPropertyName("totalTyDiscount")]
    public double TotalTyDiscount { get; set; }

    [Required]
    [EmailAddress]
    [JsonPropertyName("customerEmail")]
    public string? CustomerEmail { get; set; }

    [JsonPropertyName("customerFirstName")]
    public string? CustomerFirstName { get; set; }

    [JsonPropertyName("customerLastName")]
    public string? CustomerLastName { get; set; }

    [JsonPropertyName("customerId")]
    public long CustomerId { get; set; }

    [JsonPropertyName("taxNumber")]
    public string? TaxNumber { get; set; }

    [JsonPropertyName("tcIdentityNumber")]
    public string? TcIdentityNumber { get; set; }

    [JsonPropertyName("identityNumber")]
    public string? IdentityNumber { get; set; }

    [Required]
    [JsonPropertyName("shipmentAddress")]
    public AddressModel? ShipmentAddress { get; set; }

    [JsonPropertyName("invoiceAddress")]
    public AddressModel? InvoiceAddress { get; set; }

    [Required]
    [JsonPropertyName("lines")]
    public List<ExternalOrderItemModel> Lines { get; set; }

    [JsonPropertyName("orderDate")]
    public long OrderDateTimestamp { get; set; }

    [JsonIgnore]
    public DateTime OrderDate => DateTimeOffset.FromUnixTimeMilliseconds(OrderDateTimestamp).DateTime;
    
    [JsonPropertyName("currencyCode")]
    public string? CurrencyCode { get; set; } = "USD";

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("shipmentPackageStatus")]
    public string? ShipmentPackageStatus { get; set; }

    [JsonPropertyName("id")]
    public long Id { get; set; } // shipmentPackageId

    [JsonPropertyName("cargoTrackingNumber")]
    public long? CargoTrackingNumber { get; set; }

    [JsonPropertyName("paymentMethod")]
    public string? PaymentMethod { get; set; }

    [JsonPropertyName("cargoTrackingLink")]
    public string? CargoTrackingLink { get; set; }

    [JsonPropertyName("cargoProviderName")]
    public string? CargoProviderName { get; set; }

    [JsonPropertyName("deliveryType")]
    public string? DeliveryType { get; set; }

    [JsonPropertyName("totalPrice")]
    public double TotalPrice { get; set; }

    [JsonPropertyName("packageHistories")]
    public List<PackageHistoryModel>? PackageHistories { get; set; }

    [JsonPropertyName("commercial")]
    public bool Commercial { get; set; }

    [JsonPropertyName("giftBoxRequested")]
    public bool GiftBoxRequested { get; set; }
}

public class AddressModel
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [Required]
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [Required]
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("company")]
    public string? Company { get; set; }

    [Required]
    [JsonPropertyName("address1")]
    public string? Address1 { get; set; }

    [JsonPropertyName("address2")]
    public string? Address2 { get; set; }

    [Required]
    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("district")]
    public string? District { get; set; }

    [JsonPropertyName("districtId")]
    public int? DistrictId { get; set; }

    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; set; }

    [Required]
    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("neighborhood")]
    public string? Neighborhood { get; set; }

    [JsonPropertyName("neighborhoodId")]
    public int? NeighborhoodId { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    [JsonPropertyName("fullAddress")]
    public string? FullAddress { get; set; }

    [JsonPropertyName("taxOffice")]
    public string? TaxOffice { get; set; }

    [JsonPropertyName("taxNumber")]
    public string? TaxNumber { get; set; }

    [JsonPropertyName("addressLines")]
    public AddressLinesModel? AddressLines { get; set; }

    [JsonPropertyName("stateName")]
    public string? StateName { get; set; }
}

public class AddressLinesModel
{
    [JsonPropertyName("addressLine1")]
    public string? AddressLine1 { get; set; }

    [JsonPropertyName("addressLine2")]
    public string? AddressLine2 { get; set; }
}

public class PackageHistoryModel
{
    [JsonPropertyName("createdDate")]
    public long CreatedDateTimestamp { get; set; }

    [JsonIgnore]
    public DateTime CreatedDate => DateTimeOffset.FromUnixTimeMilliseconds(CreatedDateTimestamp).DateTime;

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
