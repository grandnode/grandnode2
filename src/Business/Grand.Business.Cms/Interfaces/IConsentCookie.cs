namespace Grand.Business.Cms.Interfaces
{
    public interface IConsentCookie
    {
        string SystemName { get; }
        bool AllowToDisable { get; }
        bool? DefaultState { get; }
        int DisplayOrder { get; }
        Task<string> Name();
        Task<string> FullDescription();
    }
}
