namespace Grand.Domain.Customers;

public class UserApi : BaseEntity
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string PrivateKey { get; set; }
    public bool IsActive { get; set; }
    public string Token { get; set; }
}