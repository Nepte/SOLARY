namespace SOLARY.Services
{
    public interface IApiService
    {
        string BaseUrl { get; }
        HttpClient HttpClient { get; }

        Task<T?> GetAsync<T>(string endpoint);
        Task<T?> PostAsync<T>(string endpoint, object data);
        Task<T?> PutAsync<T>(string endpoint, object data);
        Task<T?> DeleteAsync<T>(string endpoint);
    }
}
