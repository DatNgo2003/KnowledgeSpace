using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeSpace.WebPortal.Services
{
    public class BaseApiClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BaseApiClient(IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<T>> GetListAsync<T>(string url, bool requiredLogin = false)
        {
            var client = _httpClientFactory.CreateClient("BackendApi");
            client.BaseAddress = new Uri(_configuration["BackendApiUrl"]);

            if (requiredLogin)
            {
                var token = await _httpContextAccessor.HttpContext.GetTokenAsync("access_token");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();

            // Log để debug
            System.Diagnostics.Debug.WriteLine($"=== API Response Debug ===");
            System.Diagnostics.Debug.WriteLine($"URL: {url}");
            System.Diagnostics.Debug.WriteLine($"Full URL: {client.BaseAddress}{url}");
            System.Diagnostics.Debug.WriteLine($"Status: {response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"Content-Type: {response.Content.Headers.ContentType}");
            System.Diagnostics.Debug.WriteLine($"Body (first 500 chars): {body.Substring(0, Math.Min(500, body.Length))}");

            // Kiểm tra response có thành công không
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"API trả về lỗi {response.StatusCode}: {body}");
            }

            // Kiểm tra xem có phải JSON không
            if (string.IsNullOrWhiteSpace(body) || body.TrimStart().StartsWith("<"))
            {
                var preview = body?.Length > 0 ? body.Substring(0, Math.Min(1000, body.Length)) : "BODY IS NULL OR EMPTY";
                var debugInfo = $@"
===========================================
DEBUG INFORMATION
===========================================
URL: {url}
Full URL: {client.BaseAddress}{url}
Status Code: {response.StatusCode}
Content-Type: {response.Content.Headers.ContentType}
Body Length: {body?.Length ?? 0}
Body Preview (1000 chars):
{preview}
===========================================
";
                throw new Exception($"API trả về HTML thay vì JSON. {debugInfo}");
            }

            try
            {
                var data = JsonConvert.DeserializeObject<List<T>>(body);
                return data;
            }
            catch (JsonException ex)
            {
                throw new Exception($"Không thể parse JSON từ API. Response: {body.Substring(0, Math.Min(500, body.Length))}", ex);
            }
        }

        public async Task<T> GetAsync<T>(string url, bool requiredLogin = false)
        {
            var client = _httpClientFactory.CreateClient("BackendApi");
            client.BaseAddress = new Uri(_configuration["BackendApiUrl"]);

            if (requiredLogin)
            {
                var token = await _httpContextAccessor.HttpContext.GetTokenAsync("access_token");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Log để debug
            var fullUrl = new Uri(client.BaseAddress, url);
            System.Diagnostics.Debug.WriteLine($"Calling API: {fullUrl}");

            var response = await client.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();

            // Kiểm tra response có thành công không
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"API trả về lỗi {response.StatusCode}: {body}");
            }

            // Kiểm tra xem có phải JSON không
            if (string.IsNullOrWhiteSpace(body) || body.TrimStart().StartsWith("<"))
            {
                var preview = body?.Length > 0 ? body.Substring(0, Math.Min(1000, body.Length)) : "BODY IS NULL OR EMPTY";
                throw new Exception($"API trả về HTML. URL: {url}, Status: {response.StatusCode}, Body: {preview}");
            }

            try
            {
                var data = JsonConvert.DeserializeObject<T>(body);
                return data;
            }
            catch (JsonException ex)
            {
                throw new Exception($"Không thể parse JSON từ API. Response: {body.Substring(0, Math.Min(500, body.Length))}", ex);
            }
        }

        public async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest requestContent, bool requiredLogin = true)
        {
            var client = _httpClientFactory.CreateClient("BackendApi");
            client.BaseAddress = new Uri(_configuration["BackendApiUrl"]);

            StringContent httpContent = null;
            if (requestContent != null)
            {
                var json = JsonConvert.SerializeObject(requestContent);
                httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            }

            if (requiredLogin)
            {
                var token = await _httpContextAccessor.HttpContext.GetTokenAsync("access_token");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.PostAsync(url, httpContent);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"API trả về lỗi {response.StatusCode}: {body}");
            }

            // Kiểm tra xem có phải JSON không
            if (string.IsNullOrWhiteSpace(body) || body.TrimStart().StartsWith("<"))
            {
                throw new Exception($"API trả về HTML thay vì JSON. URL: {url}, Status: {response.StatusCode}");
            }

            try
            {
                return JsonConvert.DeserializeObject<TResponse>(body);
            }
            catch (JsonException ex)
            {
                throw new Exception($"Không thể parse JSON từ API. Response: {body.Substring(0, Math.Min(500, body.Length))}", ex);
            }
        }

        public async Task<bool> PutAsync<TRequest, TResponse>(string url, TRequest requestContent, bool requiredLogin = true)
        {
            var client = _httpClientFactory.CreateClient("BackendApi");
            client.BaseAddress = new Uri(_configuration["BackendApiUrl"]);

            HttpContent httpContent = null;
            if (requestContent != null)
            {
                var json = JsonConvert.SerializeObject(requestContent);
                httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            }

            if (requiredLogin)
            {
                var token = await _httpContextAccessor.HttpContext.GetTokenAsync("access_token");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.PutAsync(url, httpContent);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"API trả về lỗi {response.StatusCode}: {body}");
            }

            return true;
        }
    }
}