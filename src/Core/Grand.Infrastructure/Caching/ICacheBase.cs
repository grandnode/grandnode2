namespace Grand.Infrastructure.Caching
{
    /// <summary>
    /// Cache manager interface
    /// </summary>
    public interface ICacheBase: IDisposable
    {
        Task<T> GetAsync<T>(string key, Func<Task<T>> acquire);
        Task<T> GetAsync<T>(string key, Func<Task<T>> acquire, int cacheTime);
        T Get<T>(string key, Func<T> acquire);
        T Get<T>(string key, Func<T> acquire, int cacheTime);        
        Task RemoveAsync(string key, bool publisher = true);
        Task RemoveByPrefix(string prefix, bool publisher = true);        
        Task Clear(bool publisher = true);
    }
}
