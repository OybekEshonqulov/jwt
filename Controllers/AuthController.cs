using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace jwtDocker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto dto)
        {
            // Demo login (odatda DB bilan tekshiriladi)
            if (dto.Username != "admin" || dto.Password != "123")
                return Unauthorized("Login yoki parol noto‘g‘ri!");

            // Token uchun claims
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, dto.Username),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("super_secret_key_1234567890_super_long_key"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "yourapp",
                audience: "yourapp",
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds
            );

            return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        }

        // Himoyalangan endpoint (faqat token bilan kirish mumkin)
        [HttpGet("secure")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult SecureEndpoint()
        {
            return Ok($"Xush kelibsiz, {User.Identity?.Name}!");
        }
    }
}
