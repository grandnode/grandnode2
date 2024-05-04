namespace Grand.Domain.Customers;

/// <summary>
///     Represents a Customer History Password
/// </summary>
public class CustomerHistoryPassword : BaseEntity
{
    /// <summary>
    ///     Gets or sets the customer identifier
    /// </summary>
    public string CustomerId { get; set; }

    /// <summary>
    ///     Gets or sets the password
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    ///     Gets or sets the password format identifier
    /// </summary>
    public PasswordFormat PasswordFormatId { get; set; } = PasswordFormat.Clear;

    /// <summary>
    ///     Gets or sets the password salt
    /// </summary>
    public string PasswordSalt { get; set; }
}