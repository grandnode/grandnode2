namespace Grand.Domain.Stores
{
    public class DomainHost : SubBaseEntity
    {
        public string HostName { get; set; }
        public string Url { get; set; }
        public bool Primary { get; set; }

    }
}
