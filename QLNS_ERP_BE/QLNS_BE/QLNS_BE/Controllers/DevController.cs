using Microsoft.AspNetCore.Mvc;
using QLNS_BE.Security;

namespace QLNS_BE.Controllers
{
    [Route("api/dev")]
    [ApiController]
    public class DevController : ControllerBase
    {
        private readonly PasswordHasher _passwordHasher;

        public DevController(PasswordHasher passwordHasher)
        {
            _passwordHasher = passwordHasher;
        }

        [HttpGet("hash")]
        public IActionResult HashPassword([FromQuery] string password)
        {
            _passwordHasher.CreatePasswordHash(password, out string hash, out string salt);

            return Ok(new
            {
                password,
                hash,
                salt
            });
        }
    }
}
