using System;

namespace Grand.Business.Catalog.Utilities
{
    public class VatResponse
    {
        public string CountryCode { get; set; }
        public string VatNumber { get; set; }
        public bool Valid { get; set; }
        public DateTime RequestDate { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
    }
}
