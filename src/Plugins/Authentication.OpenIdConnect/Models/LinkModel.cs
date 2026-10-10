namespace Authentication.OpenIdConnect.Models;

public class LinkModel
{
    public string Email { get; set; }
    public string LoginUrl { get; set; }
    public string CompleteUrl { get; set; }
    public bool SignedIn { get; set; }
}
