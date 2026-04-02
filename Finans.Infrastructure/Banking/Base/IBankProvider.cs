using System.Net.Http;
using System.Text;

namespace Finans.Infrastructure.Banking.Base
{
    public abstract class BaseBusiness
    {
        private readonly string _username;
        private readonly string _password;
        private readonly HttpClient _httpClient;

        protected BaseBusiness(string username, string password)
        {
            _username = username;
            _password = password;

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Username", _username);
            _httpClient.DefaultRequestHeaders.Add("Password", _password);
        }

        protected async Task<string> PostAsync(string serviceUrl, string soapXml)
        {
            var content = new StringContent(soapXml, Encoding.UTF8, "text/xml");
            var response = await _httpClient.PostAsync(serviceUrl, content);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Servis isteği başarısız oldu: {response.StatusCode}");

            return await response.Content.ReadAsStringAsync();
        }

        protected async Task<T?> PostJsonAsync<T>(string serviceUrl, object request)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(serviceUrl, content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return System.Text.Json.JsonSerializer.Deserialize<T>(responseContent);
        }
    }
}