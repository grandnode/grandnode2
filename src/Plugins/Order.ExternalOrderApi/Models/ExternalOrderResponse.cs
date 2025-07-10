using System;

namespace Order.ExternalOrderApi.Models;

/// <summary>
/// Represents the response returned after processing an external order
/// </summary>
public class ExternalOrderResponse
{
    /// <summary>
    /// Gets or sets whether the operation was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Gets or sets the ID of the created order (if successful)
    /// </summary>
    public string OrderId { get; set; }
    
    /// <summary>
    /// Gets or sets the order number of the created order (if successful)
    /// </summary>
    public int? OrderNumber { get; set; }
}
