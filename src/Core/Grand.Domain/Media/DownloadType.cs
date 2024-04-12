namespace Grand.Domain.Media;

public enum DownloadType
{
    /// <summary>
    ///     None
    /// </summary>
    None = 0,

    /// <summary>
    ///     Product
    /// </summary>
    Product = 10,

    /// <summary>
    ///     Customer
    /// </summary>
    Customer = 20,

    /// <summary>
    ///     Order
    /// </summary>
    Order = 30,

    /// <summary>
    ///     Shipment
    /// </summary>
    Shipment = 40,

    /// <summary>
    ///     Shipment
    /// </summary>
    MerchandiseReturn = 50,

    /// <summary>
    ///     Document
    /// </summary>
    Document = 60,

    /// <summary>
    ///     Course
    /// </summary>
    Course = 70,

    /// <summary>
    ///     Product attribute
    /// </summary>
    ProductAttribute = 80,

    /// <summary>
    ///     Checkout attribute
    /// </summary>
    CheckoutAttribute = 81,

    /// <summary>
    ///     Contact attribute
    /// </summary>
    ContactAttribute = 82,

    /// <summary>
    ///     Message template
    /// </summary>
    MessageTemplate = 90,

    /// <summary>
    ///     Queued email
    /// </summary>
    QueuedEmail = 91
}