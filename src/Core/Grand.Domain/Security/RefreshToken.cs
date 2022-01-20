namespace Grand.Domain.Security
{
    public class RefreshToken
    {
        public string RefreshId { get; set; }
        public string Token { get; set; }
        public DateTime ValidTo { get; set; }
        public bool IsActive { get; set; }
    }
}
