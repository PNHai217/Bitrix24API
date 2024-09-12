using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;
using System.Threading.Tasks;
using System.Net.Http;
using Bitrix24API.Models;
using Newtonsoft.Json.Linq;
using System.IO;
using Azure.Core;

namespace Bitrix24API.Controllers
{
    public class OAuthController : Controller
    {
        private static readonly string TokenFilePath = "E:\\Bitrix24API\\Bitrix24API\\Bitrix24API\\token.txt";
        private static readonly string ClientId = "local.66d810c7098362.62715092";
        private static readonly string ClientSecret = "6VyLWqaHQ76l20yPN6qTe7Br3INJaaMNNjP5MeDOvEizdA2y7W";
        private static readonly string RedirectUri = "https://a55e-116-96-47-183.ngrok-free.app/OAuth/handler";

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Authorize()
        {
            string authUrl = $"https://b24-nz1irc.bitrix24.com/oauth/authorize/?client_id={ClientId}&response_type=code&redirect_uri={RedirectUri}";
            return Redirect(authUrl);
        }

        // After recive code, get token
        [HttpGet]
        public async Task<IActionResult> Callback(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest("Authorization code not provided.");
            }

            var tokenResponse = await GetAccessTokenAsync(code);
            if (tokenResponse != null)
            {
                SaveToken(tokenResponse);
                return RedirectToAction("Index", "Contact");
            }

            return BadRequest("Failed to retrieve token.");
        }

        // Get token
        private async Task<JObject> GetAccessTokenAsync(string code)
        {
            string Uri = "https://bx-oauth2.aasc.com.vn/bx/oauth2_man";
            using var client = new HttpClient();
            var requestBody = new FormUrlEncodedContent(new[]
            {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("client_id", ClientId),
            new KeyValuePair<string, string>("client_secret", ClientSecret),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", Uri)
        });

            var response = await client.PostAsync("https://b24-nz1irc.bitrix24.com/oauth/token/", requestBody);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JObject.Parse(json);
        }
        [HttpGet]
        public async Task<IActionResult> Handler(string code, string state, string domain, string member_id, string scope, string server_domain)
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest("Authorization code not provided.");
            }

            var tokenResponse = await GetAccessTokenAsync(code);
            if (tokenResponse != null)
            {
                SaveToken(tokenResponse);
                return RedirectToAction("Index", "Contact");
            }

            return BadRequest("Failed to retrieve token.");
        }
        private async Task<string> RefreshAccessTokenAsync()
        {
            var refreshToken = GetRefreshToken(); // Get Refresh token
            var requestBody = new FormUrlEncodedContent(new[]
            {
        new KeyValuePair<string, string>("grant_type", "refresh_token"),
        new KeyValuePair<string, string>("client_id", ClientId),
        new KeyValuePair<string, string>("client_secret", ClientSecret),
        new KeyValuePair<string, string>("refresh_token", refreshToken)
        });

            using var client = new HttpClient();
            var response = await client.PostAsync("https://bx-oauth2.aasc.com.vn/bx/oauth2_token", requestBody);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JObject.Parse(json);

            SaveToken(tokenResponse); // Save neww token to file
            return tokenResponse["access_token"].ToString();
        }

        private string GetRefreshToken()
        {
            var tokenData = System.IO.File.ReadAllText(TokenFilePath).Split('\n');
            return tokenData[1]; // Get Refresh token
        }
        // Save token to file
        private void SaveToken(JObject tokenResponse)
        {
            var accessToken = tokenResponse["access_token"]?.ToString();
            var refreshToken = tokenResponse["refresh_token"]?.ToString();
            var expiry = DateTime.UtcNow.AddSeconds((int?)tokenResponse["expires_in"] ?? 0).ToString("o");

            var tokenData = $"{accessToken}\n{refreshToken}\n{expiry}";
            System.IO.File.WriteAllText(TokenFilePath, tokenData);
        }
    }
}
