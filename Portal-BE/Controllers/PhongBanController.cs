using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal_BE.Models.Entities;

namespace Portal_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhongBanController : ControllerBase
    {
        private readonly ThinhContext _context;
        private readonly int IdChucNang = 2; // PhongBan

        public PhongBanController(ThinhContext context)
        {
            _context = context;
        }

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("Id")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out long userId))
            {
                return userId;
            }
            return 0;
        }

        private async Task<long> GetRoleId(long userId)
        {
            var user = await _context.TaiKhoans.FindAsync(userId);
            return user?.IdVaiTro ?? 0;
        }

        private async Task<bool> CheckPermission(long roleId, string action)
        {
            var quyen = await _context.Quyens
                .FirstOrDefaultAsync(q => q.IdVaiTro == roleId && q.IdChucNang == IdChucNang);

            if (quyen == null) return false;

            return action switch
            {
                "Them" => quyen.Them ?? false,
                "Sua" => quyen.Sua ?? false,
                "Xoa" => quyen.Xoa ?? false,
                "Xuat" => quyen.Xuat ?? false,
                _ => false
            };
        }

        [HttpGet("get-list")]
        public async Task<IActionResult> GetList()
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            // All authenticated users can usually see departments, but let's check 'Xuat' just in case
            // Or strictly follow "Accessible by all authenticated users" from requirements
            // Requirement says: "Accessible by all authenticated users"
            // So we might skip strict permission check or assume everyone has 'Xuat' for PhongBan
            
            // Let's stick to the pattern: Check if they have 'Xuat' permission. 
            // If the DB is set up correctly, all roles should have 'Xuat' for PhongBan.
            long roleId = await GetRoleId(userId);
            if (!await CheckPermission(roleId, "Xuat"))
            {
                 // Fallback: If requirement says "All authenticated", maybe we allow it even if permission is missing?
                 // But strict guidelines say "Inside every action... check if Role has permission".
                 // So I will return Unauthorized if no permission found.
                 return Unauthorized(new { statusCode = 401, message = "Không có quyền xem danh sách phòng ban" });
            }

            var list = await _context.PhongBans
                .Select(p => new 
                {
                    p.Id,
                    p.TenPhongBan,
                    p.MoTa,
                    p.NgayTao
                })
                .ToListAsync();

            return Ok(new { statusCode = 200, message = "Lấy danh sách thành công", data = list });
        }

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] AddPhongBanRequest request)
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            long roleId = await GetRoleId(userId);
            if (!await CheckPermission(roleId, "Them"))
            {
                return Unauthorized(new { statusCode = 401, message = "Không có quyền thêm phòng ban" });
            }

            var pb = new PhongBan
            {
                TenPhongBan = request.TenPhongBan,
                MoTa = request.MoTa,
                NgayTao = DateTime.Now
            };

            _context.PhongBans.Add(pb);
            await _context.SaveChangesAsync();

            return Ok(new { statusCode = 200, message = "Thêm phòng ban thành công", data = pb.Id });
        }

        public class AddPhongBanRequest
        {
            public string TenPhongBan { get; set; }
            public string? MoTa { get; set; }
        }
    }
}
