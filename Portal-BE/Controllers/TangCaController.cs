using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal_BE.Models.Entities;

namespace Portal_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TangCaController : ControllerBase
    {
        private int IdChucNang = 1;
        private readonly ThinhContext _context;
        // Assuming there is a specific function ID for Overtime management if needed for permission checks
        // private readonly int IdChucNang = ...; 

        public TangCaController(ThinhContext context)
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
        private async Task<string> GetRoleName(long userId)
        {
            var user = await _context.TaiKhoans
                .Include(t => t.IdVaiTroNavigation)
                .FirstOrDefaultAsync(t => t.Id == userId);
            return user?.IdVaiTroNavigation?.TenVaiTro ?? "";
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


        // Helper to check permission if needed. For now, we might assume only authorized roles can approve.
        // private async Task<bool> CheckPermission(long roleId, string action) { ... }

        [HttpGet("get-my-ot")]
        public async Task<IActionResult> GetMyOt()
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            var nhanVien = await _context.NhanViens.FirstOrDefaultAsync(n => n.IdTaiKhoan == userId);
            if (nhanVien == null)
            {
                return Ok(new { statusCode = 400, message = "Tài khoản không gắn với nhân viên" });
            }

            var list = await _context.TangCas
                .Where(t => t.IdNhanVien == nhanVien.Id)
                .OrderByDescending(t => t.NgayTangCa)
                .ThenByDescending(t => t.Id)
                .Select(t => new
                {
                    t.Id,
                    t.NgayTangCa,
                    t.GioBatDau,
                    t.GioKetThuc,
                    t.SoGioLam,
                    t.HeSo,
                    t.LyDoTangCa,
                    t.TrangThai
                })
                .ToListAsync();

            return Ok(new { statusCode = 200, message = "Lấy danh sách tăng ca thành công", data = list });
        }

        [HttpPost("request-ot")]
        public async Task<IActionResult> RequestOt([FromBody] RequestOtDto request)
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            var nhanVien = await _context.NhanViens.FirstOrDefaultAsync(n => n.IdTaiKhoan == userId);
            if (nhanVien == null)
            {
                return Ok(new { statusCode = 400, message = "Tài khoản không gắn với nhân viên" });
            }

            var today = DateTime.Now;

            var existing = await _context.TangCas
                .FirstOrDefaultAsync(t => t.IdNhanVien == nhanVien.Id && t.NgayTangCa >= today);

            if (existing != null)
            {
                return Ok(new { statusCode = 400, message = "Bạn đã đăng ký tăng ca cho hôm nay hoặc ngày sau đó" });
            }

            var tangCa = new TangCa
            {
                IdNhanVien = nhanVien.Id,
                NgayTangCa = today,
                GioBatDau = request.GioBatDau,
                GioKetThuc = request.GioKetThuc,
                SoGioLam = request.SoGioLam,
                HeSo = request.HeSo,
                LyDoTangCa = request.LyDoTangCa,
                TrangThai = "Chờ duyệt"
            };

            _context.TangCas.Add(tangCa);
            await _context.SaveChangesAsync();

            return Ok(new { statusCode = 200, message = "Đăng ký tăng ca thành công", data = tangCa.Id });
        }

        [HttpGet("get-all-ot")]
        public async Task<IActionResult> GetAllOt()
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            long roleId = await GetRoleId(userId);
            string roleName = await GetRoleName(userId);

            if (!await CheckPermission(roleId, "Xuat"))
            {
                return Unauthorized(new { statusCode = 401, message = "Không có quyền xem danh sách tăng ca" });
            }

            bool isAdminOrHr = roleName.Contains("Admin", StringComparison.OrdinalIgnoreCase) ||
                               roleName.Contains("Nhân sự", StringComparison.OrdinalIgnoreCase) ||
                               roleName.Contains("HR", StringComparison.OrdinalIgnoreCase) ||
                               roleName.Contains("Quản lý", StringComparison.OrdinalIgnoreCase);

            if (!isAdminOrHr)
            {
                return Unauthorized(new { statusCode = 403, message = "Chỉ quản lý mới có quyền xem danh sách tăng ca" });
            }

            var today = DateTime.Now;

            var list = await _context.TangCas
                .Include(t => t.IdNhanVienNavigation)
                .Where(t => t.NgayTangCa == today)
                .OrderByDescending(t => t.Id)
                .Select(t => new
                {
                    t.Id,
                    t.NgayTangCa,
                    t.GioBatDau,
                    t.GioKetThuc,
                    t.SoGioLam,
                    t.HeSo,
                    t.LyDoTangCa,
                    t.TrangThai,
                    TenNhanVien = t.IdNhanVienNavigation != null ? t.IdNhanVienNavigation.HoTen : null
                })
                .ToListAsync();

            return Ok(new { statusCode = 200, message = "Lấy danh sách tăng ca hôm nay thành công", data = list });
        }

        [HttpPost("status-ot")]
        public async Task<IActionResult> StatusOT([FromBody] int id)
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            // Check if user has permission to approve (e.g., Admin, HR, or Manager)
            // For simplicity, let's assume any authenticated user with a specific role can approve, 
            // or we can reuse the permission logic from other controllers if we knew the Function ID.
            // Let's implement a basic role check for now.
            var user = await _context.TaiKhoans.Include(t => t.IdVaiTroNavigation).FirstOrDefaultAsync(t => t.Id == userId);
            if (user == null) return Unauthorized(new { statusCode = 401, message = "Không tìm thấy thông tin người dùng" });

            long roleId = await GetRoleId(userId);
            string roleName = await GetRoleName(userId);

            if (!await CheckPermission(roleId, "Sua"))
            {
                return Unauthorized(new { statusCode = 401, message = "Không có quyền sửa thông tin" });
            }

            string isAdminOrHr = user.IdVaiTroNavigation.TenVaiTro;
            bool canApprove = isAdminOrHr.Contains("Admin", StringComparison.OrdinalIgnoreCase) ||
                              isAdminOrHr.Contains("Nhân sự", StringComparison.OrdinalIgnoreCase) ||
                              isAdminOrHr.Contains("HR", StringComparison.OrdinalIgnoreCase) ||
                              isAdminOrHr.Contains("Quản lý", StringComparison.OrdinalIgnoreCase);

            if (!canApprove)
            {
                return Unauthorized(new { statusCode = 403, message = "Bạn không có quyền duyệt tăng ca" });
            }

            var tangCa = await _context.TangCas.FindAsync(id);
            if (tangCa == null)
            {
                return Ok(new { statusCode = 404, message = "Không tìm thấy yêu cầu tăng ca" });
            }
            tangCa.TrangThai = "Duyệt";
            
            await _context.SaveChangesAsync();

            return Ok(new { statusCode = 200, message = "Đã duyệt yêu cầu tăng ca" });
        }

        public class RequestOtDto
        {
            public TimeOnly? GioBatDau { get; set; }
            public TimeOnly? GioKetThuc { get; set; }
            public double? SoGioLam { get; set; }
            public double? HeSo { get; set; }
            public string? LyDoTangCa { get; set; }
        }
    }
}
