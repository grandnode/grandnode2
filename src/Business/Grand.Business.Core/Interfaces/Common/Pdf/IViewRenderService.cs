namespace Grand.Business.Core.Interfaces.Common.Pdf
{
    /// <summary>
    /// Allow render rezor view as string
    /// </summary>
    public interface IViewRenderService
    {
        Task<string> RenderToStringAsync<T>(string viewPath, T model);
    }
}
