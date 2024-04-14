namespace Grand.Web.Admin.Validators.Common;

public static class CommonValid
{
    public static bool IsCommissionValid(double? commission)
    {
        switch (commission)
        {
            case null:
                return true;
            case < 0:
            case > 100:
                return false;
            default:
                return true;
        }
    }
}