using Microsoft.AspNetCore.Mvc;
using System.Net.Http;

namespace Demo_Speech_Recognition_Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        public TokenController()
        {
            var region = "YourServiceRegion";
            var subscriptionKey = "YourSubscriptionKey";

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new System.Uri($"https://{region}.api.cognitive.microsoft.com/sts/v1.0/issueToken");
            _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
        }

        // GET api/token
        [HttpGet]
        public ActionResult<string> Get()
        {
            var body = new StringContent("") as HttpContent;
            var response = _httpClient.PostAsync(string.Empty, body).Result;

            return response.Content.ReadAsStringAsync().Result;
        }
    }
}
