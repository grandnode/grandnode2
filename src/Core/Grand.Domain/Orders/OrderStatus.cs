namespace Grand.Domain.Orders
{
    public class OrderStatus : BaseEntity
    {
        public int StatusId { get; set; }
        public string Name { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsSystem { get; set; }
    }
}
