using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

namespace Bitrix24API.Service
{
    public class ContactService
    {
        private readonly HttpClient _httpClient;
        private string _accessToken;

        public ContactService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public void SetAccessToken(string accessToken)
        {
            _accessToken = accessToken;
        }

        public async Task<JObject> GetContactsAsync()
        {
            var url = "rest/crm.contact.list.json";
            var queryParams = "select[]=NAME&select[]=UF_CRM_1725854291259&select[]=WEB&select[]=EMAIL&select[]=PHONE&select[]=UF_CRM_1725460267871&select[]=UF_CRM_1725460472405";

            var requestUri = $"{url}?{queryParams}&auth={_accessToken}";

            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JObject.Parse(content);
        }
    }

}
