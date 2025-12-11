using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Portal_BE.Models.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Portal_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ThinhContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ThinhContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.TenDangNhap) || string.IsNullOrEmpty(request.MatKhau))
            {
                return Ok(new
                {
                    statusCode = 400,
                    message = "Vui lòng nhập tên đăng nhập và mật khẩu"
                });
            }

            var user = await _context.TaiKhoans
                .Include(t => t.IdVaiTroNavigation)
                .FirstOrDefaultAsync(u => u.TenDangNhap == request.TenDangNhap && u.MatKhau == request.MatKhau);

            if (user == null)
            {
                return Ok(new
                {
                    statusCode = 401,
                    message = "Tên đăng nhập hoặc mật khẩu không chính xác"
                });
            }

            if (user.TrangThai == false)
            {
                return Ok(new
                {
                    statusCode = 403,
                    message = "Tài khoản đã bị khóa"
                });
            }

            // Generate JWT Token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.IdVaiTroNavigation.TenVaiTro),
                    new Claim("Id", user.Id.ToString()) // Custom claim just in case
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new
            {
                statusCode = 200,
                message = "Đăng nhập thành công",
                token = tokenString,
                idTaiKhoan = user.Id,
                role = user.IdVaiTroNavigation.TenVaiTro,
                roleId = user.IdVaiTro
            });
        }

        public class LoginRequest
        {
            public string TenDangNhap { get; set; }
            public string MatKhau { get; set; }
        }
    }
}
