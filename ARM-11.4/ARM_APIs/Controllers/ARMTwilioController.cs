using Microsoft.AspNetCore.Mvc;
using Twilio.Exceptions;
using Twilio.Types;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using static ARM_APIs.Controllers.ARMTwilioController;

namespace ARM_APIs.Controllers
{
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [ApiController]
    public class ARMTwilioController : ControllerBase
    {
        [HttpPost]
        [Route("ARMSendWhatsappMessage")]
        public IActionResult SendWhatsappMessage(WhatsappMessage whatsapp)
        {
            try
            {
                TwilioClient.Init("AC6644ec818d30b81758398cd0063f61f6", "8d3666c6c054e3a24b3480a636cffaa7");

                // Split phone numbers by comma
                string[] phoneNumbers = whatsapp.To.Split(",");

                // Send message to each phone number
                foreach (string phoneNumber in phoneNumbers)
                {
                    MessageResource.Create(
                        to: new PhoneNumber("whatsapp:" + phoneNumber),
                        from: new PhoneNumber("whatsapp:+14155238886"),
                        body: whatsapp.Message
                    );
                }

                return Ok(new { success = true, message = "Message sent successfully." });
            }
            catch (TwilioException ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to send message. Error: " + ex.Message });
            }
        }

        public class WhatsappMessage
        {
            public string To { get; set; }
            public string Message { get; set; }
        }
    }
}
