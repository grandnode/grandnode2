namespace Grand.Business.Core.Utilities.Marketing
{
    public class SerializeCustomerReminderHistory
    {
        public string Id { get; set; }
        public string CustomerReminderId { get; set; }
        public string CustomerId { get; set; }
        public DateTime SendDate { get; set; }
        public int Level { get; set; }
        public string OrderId { get; set; }
    }
}
