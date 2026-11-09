using System.Net.Http.Headers;

namespace RaceDay.Web.Services;

public interface IApiService
{
    Task<HttpResponseMessage> GetAsync(string path);
    Task<HttpResponseMessage> PostAsync<T>(string path, T data);
    Task<HttpResponseMessage> PutAsync<T>(string path, T data);
    Task<HttpResponseMessage> DeleteAsync(string path);
    Task<HttpResponseMessage> PostFileAsync(string path, IFormFile file);
}

public class ApiService : IApiService
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiService(IHttpClientFactory clientFactory, IHttpContextAccessor httpContextAccessor)
    {
        _clientFactory = clientFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    private HttpClient GetClient()
    {
        var client = _clientFactory.CreateClient("RaceDayApi");
        var context = _httpContextAccessor.HttpContext;
        if (context != null && context.Request.Headers.ContainsKey("Cookie"))
        {
            client.DefaultRequestHeaders.Add("Cookie", context.Request.Headers["Cookie"].AsEnumerable());
        }
        return client;
    }

    public async Task<HttpResponseMessage> GetAsync(string path) => await GetClient().GetAsync(path);
    public async Task<HttpResponseMessage> PostAsync<T>(string path, T data) => await GetClient().PostAsJsonAsync(path, data);
    public async Task<HttpResponseMessage> PutAsync<T>(string path, T data) => await GetClient().PutAsJsonAsync(path, data);
    public async Task<HttpResponseMessage> DeleteAsync(string path) => await GetClient().DeleteAsync(path);

    public async Task<HttpResponseMessage> PostFileAsync(string path, IFormFile file)
    {
        var client = GetClient();
        using var content = new MultipartFormDataContent();
        using var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.FileName);

        return await client.PostAsync(path, content);
    }
}