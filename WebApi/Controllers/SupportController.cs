using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("")]
    public class SupportController : ControllerBase
    {

        [HttpGet("email")]
        public IActionResult GetSupportEmail()
        {
            string supportEmail = "calorauz@gmail.com";
            return Ok(new { email = supportEmail });
        }

        [HttpGet("telegram")]
        public IActionResult GetSupportTelegram()
        {
            string telegramUsername = "https://t.me/calora_support";
            return Ok(new { telegram = telegramUsername });
        }
    }
}
