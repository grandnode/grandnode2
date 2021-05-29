namespace Grand.Web.Admin.Validators.Common
{
    public static class CommonValid
    {
        public static bool IsCommissionValid(double? commission)
        {
            if (!commission.HasValue)
                return true;

            if (commission < 0 || commission > 100)
                return false;

            return true;
        }

    }
}
