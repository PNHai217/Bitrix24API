using Bitrix24API;
using Bitrix24API.Data;
using Bitrix24API.Models;
using Bitrix24API.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;
namespace Bitrix24API.Controllers
{
    public class ContactController : Controller
    {
        private static readonly string TokenFilePath = "E:\\Bitrix24API\\Bitrix24API\\Bitrix24API\\token.txt";
        private static readonly HttpClient _httpClient = new HttpClient();
        public async Task<IActionResult> Index()
        {
            var tokenData = System.IO.File.ReadAllText(TokenFilePath).Split('\n');
            var accessToken = tokenData[0];
            var contacts = await GetContactsAsync(accessToken);

            return View(contacts);
        }

        public IActionResult Create()
        {
            return View();
        }

        // Create New Contact
        [HttpPost]
        public async Task<IActionResult> Create(Contact model)
        {
            if (ModelState.IsValid)
            {
                // Read token from file
                var tokenData = System.IO.File.ReadAllText(TokenFilePath).Split('\n');
                var accessToken = tokenData[0];
                //Get new token if token expired
                bool result;
                try
                {
                    result = await AddContactAsync(accessToken, model);

                    // If token expired, renew and try again
                    if (!result)
                    {
                        var response = await _httpClient.GetAsync($"https://b24-nz1irc.bitrix24.com/rest/crm.contact.add.json?auth={accessToken}");
                        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            // If token expired, call RenewAccessTokenAsync to get a neww token
                            accessToken = await RenewAccessTokenAsync();
                            tokenData[0] = accessToken;
                            System.IO.File.WriteAllText(TokenFilePath, string.Join("\n", tokenData));
                            result = await AddContactAsync(accessToken, model);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"An error occurred while creating contact: {ex.Message}");
                    return View(model);
                }
                if (result)
                {
                    return RedirectToAction("Index");
                }
                ModelState.AddModelError("", "Failed to create contact.");
            }

            return View(model);
        }
        private async Task<bool> AddContactAsync(string accessToken, Contact model)
        {
            var requestUri = "https://b24-nz1irc.bitrix24.com/rest/crm.contact.add.json";
            var requestBody = new
            {
                fields = new
                {
                    NAME = model.Name,
                    WEB = new[] { new { VALUE = model.Website, VALUE_TYPE = "WORK" } },
                    EMAIL = new[] { new { VALUE = model.Email, VALUE_TYPE = "WORK" } },
                    PHONE = new[] { new { VALUE = model.Phone, VALUE_TYPE = "WORK" } },
                    UF_CRM_1725854291259 = model.Address,
                    UF_CRM_1725460267871 = model.BankName,
                    UF_CRM_1725460472405 = model.AccountNumber
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent(JsonConvert.SerializeObject(requestBody), System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonConvert.DeserializeObject<JObject>(responseContent);

            if (response.IsSuccessStatusCode && jsonResponse["result"] != null)
            {
                return true;
            }
            return false;
        }
        public async Task<IActionResult> Edit(int id)
        {
            // Read token from file
            var tokenData = System.IO.File.ReadAllText(TokenFilePath).Split('\n');
            var accessToken = tokenData[0];
            JObject contactJObject;
            try
            {
                contactJObject = await GetContactByIdAsync(accessToken, id);

                // Check token expired
                if (contactJObject == null)
                {
                    // If token expied, rênw token
                    accessToken = await RenewAccessTokenAsync();
                    tokenData[0] = accessToken;
                    System.IO.File.WriteAllText(TokenFilePath, string.Join("\n", tokenData));
                    contactJObject = await GetContactByIdAsync(accessToken, id);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred while retrieving contact: {ex.Message}");
                return View("Error");
            }


            // Convert JObject to Contact model
            var contact = new Contact
            {
                Id = int.Parse(contactJObject["ID"]?.ToString()),
                Name = contactJObject["NAME"]?.ToString(),
                Website = ExtractValue(contactJObject["WEB"]),
                Email = ExtractValue(contactJObject["EMAIL"]),
                Phone = ExtractValue(contactJObject["PHONE"]),
                Address = ExtractValue(contactJObject["UF_CRM_1725854291259"]),
                BankName = ExtractValue(contactJObject["UF_CRM_1725460267871"]),
                AccountNumber = ExtractValue(contactJObject["UF_CRM_1725460472405"])
            };
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(Contact model)
        {
            if (ModelState.IsValid)
            {
                // Đọc token từ file
                var tokenData = System.IO.File.ReadAllText(TokenFilePath).Split('\n');                
                var accessToken = tokenData[0];

                // Gọi API để cập nhật contact trên Bitrix24
                var result = await UpdateContactAsync(accessToken, model);
                if (result)
                {
                    return RedirectToAction("Index");
                }
                ModelState.AddModelError("", "Failed to update contact.");
            }

            return View(model);
        }
        private async Task<bool> UpdateContactAsync(string accessToken, Contact model)
        {
            var requestUri = "https://b24-nz1irc.bitrix24.com/rest/crm.contact.update.json";
            var requestBody = new
            {
                id = model.Id,
                fields = new
                {
                    NAME = model.Name,
                    WEB = new[] { new { VALUE = model.Website, VALUE_TYPE = "WORK" } },
                    EMAIL = new[] { new { VALUE = model.Email, VALUE_TYPE = "WORK" } },
                    PHONE = new[] { new { VALUE = model.Phone, VALUE_TYPE = "WORK" } },
                    UF_CRM_1725854291259 = model.Address,
                    UF_CRM_1725460267871 = model.BankName,
                    UF_CRM_1725460472405 = model.AccountNumber
                }
            };
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent(JsonConvert.SerializeObject(requestBody), System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        private async Task<JObject> GetContactByIdAsync(string accessToken, int id)
        {
            var requestUri = $"https://b24-nz1irc.bitrix24.com/rest/crm.contact.get.json?ID={id}&auth={accessToken}";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var jsonObject = JObject.Parse(json);

            var contactInfo = new JObject
            {
                ["ID"] = jsonObject["result"]?["ID"]?.ToString(),
                ["NAME"] = jsonObject["result"]?["NAME"]?.ToString(),
                ["WEB"] = ExtractValue(jsonObject["result"]?["WEB"]),
                ["EMAIL"] = ExtractValue(jsonObject["result"]?["EMAIL"]),
                ["PHONE"] = ExtractValue(jsonObject["result"]?["PHONE"]),
                ["UF_CRM_1725854291259"] = ExtractValue(jsonObject["result"]?["UF_CRM_1725854291259"]),
                ["UF_CRM_1725460267871"] = ExtractValue(jsonObject["result"]?["UF_CRM_1725460267871"]),
                ["UF_CRM_1725460472405"] = ExtractValue(jsonObject["result"]?["UF_CRM_1725460472405"])
            };

            return contactInfo;
        }
        // Get Contacts from Bitrix
        private async Task<JObject> GetContactsAsync(string accessToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://b24-nz1irc.bitrix24.com/rest/crm.contact.list.json?select[]=ID&select[]=NAME&select[]" +
                "=UF_CRM_1725854291259&select[]=WEB&select[]=EMAIL&select[]=PHONE&select[]=UF_CRM_1725460267871&select[]=UF_CRM_1725460472405&auth=" + accessToken);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var jsonObject = JObject.Parse(json);

            var contactsArray = new JArray();
            foreach (var contact in jsonObject["result"])
            {
                var contactInfo = new JObject
                {
                    ["ID"] = ExtractValue(contact["ID"]),
                    ["NAME"] = contact["NAME"]?.ToString(),
                    ["WEB"] = ExtractValue(contact["WEB"]),
                    ["EMAIL"] = ExtractValue(contact["EMAIL"]),
                    ["PHONE"] = ExtractValue(contact["PHONE"]),
                    ["UF_CRM_1725854291259"] = ExtractValue(contact["UF_CRM_1725854291259"]),
                    ["UF_CRM_1725460267871"] = ExtractValue(contact["UF_CRM_1725460267871"]),
                    ["UF_CRM_1725460472405"] = ExtractValue(contact["UF_CRM_1725460472405"])
                };
                contactsArray.Add(contactInfo);
            }
            return new JObject { ["contacts"] = contactsArray };
        }
        private string ExtractValue(JToken token)
        {
            if (token.Type == JTokenType.Array && token.HasValues)
            {
                var firstValue = token.First?["VALUE"]?.ToString();
                return firstValue ?? string.Empty;
            }
            return token.ToString();
        }
        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                // Get token from File
                var accessToken = GetAccessToken();
                using var client = new HttpClient();
                var response = await client.PostAsync($"https://b24-nz1irc.bitrix24.com/rest/crm.contact.delete.json?id={id}&auth={accessToken}",new StringContent(""));

                // Check token expire
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    accessToken = await RenewAccessTokenAsync();
                    response = await client.PostAsync($"https://b24-nz1irc.bitrix24.com/rest/crm.contact.delete.json?id={id}&auth={accessToken}",
                        new StringContent(""));
                }
                response.EnsureSuccessStatusCode();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Read token from file
        private string GetAccessToken()
        {
            var tokenData = System.IO.File.ReadAllText(TokenFilePath).Split('\n');
            return tokenData[0];
        }

        // Refresh token
        private async Task<string> RenewAccessTokenAsync()
        {
            var tokenData = System.IO.File.ReadAllText(TokenFilePath).Split('\n');
            var refreshToken = tokenData[1];

            using var client = new HttpClient();
            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("client_id", "local.66d810c7098362.62715092"),
                new KeyValuePair<string, string>("client_secret", "6VyLWqaHQ76l20yPN6qTe7Br3INJaaMNNjP5MeDOvEizdA2y7W"),
                new KeyValuePair<string, string>("refresh_token", refreshToken)
            });

            var response = await client.PostAsync("https://b24-nz1irc.bitrix24.com/oauth/token/", requestContent);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var tokenResponse = JObject.Parse(jsonResponse);

            // Save new token
            System.IO.File.WriteAllText(TokenFilePath, $"{tokenResponse["access_token"]}\n{tokenResponse["refresh_token"]}");

            return tokenResponse["access_token"].ToString();
        }
    }
}
