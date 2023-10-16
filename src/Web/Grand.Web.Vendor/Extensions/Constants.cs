namespace Grand.Web.Vendor.Extensions
{
    public static class Constants
    {
        public const string AreaVendor = "Vendor";

        public static string Layout_Vendor => $"~/Areas/{AreaVendor}/Views/Shared/_VendorLayout.cshtml";
        public static string Layout_VendorLogin => $"~/Areas/{AreaVendor}/Views/Shared/_VendorLoginLayout.cshtml";
        public static string WwwRoot { get; set; } = "/_content/Grand.Web.Vendor";

    }
}
