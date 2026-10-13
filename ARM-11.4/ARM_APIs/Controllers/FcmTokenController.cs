using ARM_APIs.Interface;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

[Route("api/v{version:apiVersion}")]
[ApiVersion("1")]
[ApiController]
public class FcmTokenController : ControllerBase
{
    private readonly IFirebase _firebaseTokenService;

    public FcmTokenController(IFirebase firebaseTokenService)
    {
        _firebaseTokenService = firebaseTokenService;
    }

    [HttpGet("GetFCMAccessToken")]
    public async Task<IActionResult> GetFCMAccessToken()
    {
        var accessToken = await _firebaseTokenService.GetAccessTokenAsync();
        return Ok(accessToken);
    }

    [HttpPost("SendFCMNotification")]
    public async Task<IActionResult> SendFCMNotification(FCMRequest fcmRequest)
    {
        try
        {
            var accessToken = await _firebaseTokenService.GetAccessTokenAsync();

            if (fcmRequest.message?.token == null || !fcmRequest.message.token.Any())
            {
                return BadRequest(new { message = "At least one token is required." });
            }

            var fcmUrl = "https://fcm.googleapis.com/v1/projects/axperthybrid/messages:send";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Store results for each token
                var results = new List<object>();

                foreach (var token in fcmRequest.message.token)
                {
                    var jsonRequest = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        message = new
                        {
                            token = token,
                            data = fcmRequest.message.data
                        }
                    });

                    var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(fcmUrl, content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    results.Add(new
                    {
                        message = responseContent
                    });
                }

                return Ok(new
                {
                    message = "Notifications processed",
                    results
                });
            }
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, new
            {
                Error = ex.Message
            });
        }
    }


}
public class FCMRequest
{
    public FCMMessage message { get; set; }
}

public class FCMMessage
{
    public List<string> token { get; set; }
    public object data { get; set; }
}

