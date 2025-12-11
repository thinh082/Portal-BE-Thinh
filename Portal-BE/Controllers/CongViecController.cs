using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal_BE.Models.Entities;

namespace Portal_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CongViecController : ControllerBase
    {
        private readonly ThinhContext _context;
        private readonly int IdChucNang = 5; // CongViec

        public CongViecController(ThinhContext context)
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
        private async Task<string> GetRoleName(long userId)
        {
            var user = await _context.TaiKhoans
                .Include(t => t.IdVaiTroNavigation)
                .FirstOrDefaultAsync(t => t.Id == userId);
            return user?.IdVaiTroNavigation?.TenVaiTro ?? "";
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

        [HttpPost("assign-task")]
        public async Task<IActionResult> AssignTask([FromBody] AssignTaskRequest request)
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            long roleId = await GetRoleId(userId);
            if (!await CheckPermission(roleId, "Them"))
            {
                return Unauthorized(new { statusCode = 401, message = "Không có quyền giao việc" });
            }

            var assigner = await _context.NhanViens.FirstOrDefaultAsync(n => n.IdTaiKhoan == userId);
            if (assigner == null) return Ok(new { statusCode = 400, message = "Người giao việc không hợp lệ" });

            var task = new CongViec
            {
                TieuDe = request.TieuDe,
                MoTa = request.MoTa,
                NgayBatDau = DateOnly.FromDateTime(DateTime.Now),
                HanHoanThanh = request.HanHoanThanh,
                TrangThai = "Mới giao",
                IdNguoiGiao = assigner.Id,
                IdNguoiNhan = request.IdNguoiNhan
            };

            _context.CongViecs.Add(task);
            await _context.SaveChangesAsync();

            return Ok(new { statusCode = 200, message = "Giao việc thành công", data = task.Id });
        }

        [HttpGet("get-my-tasks")]
        public async Task<IActionResult> GetMyTasks()
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            //long roleId = await GetRoleId(userId);
            //if (!await CheckPermission(roleId, "Xuat"))
            //{
            //    return Unauthorized(new { statusCode = 401, message = "Không có quyền xem danh sách công việc" });
            //}

            var nhanVien = await _context.NhanViens.FirstOrDefaultAsync(n => n.IdTaiKhoan == userId);
            if (nhanVien == null) return Ok(new { statusCode = 404, message = "Không tìm thấy thông tin nhân viên" });

            var today = DateOnly.FromDateTime(DateTime.Now);

            var tasks = await _context.CongViecs
                .Include(c => c.IdNguoiGiaoNavigation)
                .Include(c => c.IdDuAnNavigation)
                .Where(c => c.IdNguoiNhan == nhanVien.Id && (c.NgayBatDau == null || c.NgayBatDau >= today))
                .OrderByDescending(c => c.NgayBatDau)
                .Select(c => new 
                {
                    c.Id,
                    c.TieuDe,
                    c.MoTa,
                    c.NgayBatDau,
                    c.HanHoanThanh,
                    c.TrangThai,
                    NguoiGiao = c.IdNguoiGiaoNavigation != null ? c.IdNguoiGiaoNavigation.HoTen : "Unknown",
                    DuAn = c.IdDuAnNavigation != null ? c.IdDuAnNavigation.TenDuAn : null
                })
                .ToListAsync();

            return Ok(new { statusCode = 200, message = "Lấy danh sách công việc thành công", data = tasks });
        }

        [HttpGet("get-all-tasks")]
        public async Task<IActionResult> GetAllTasks()
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            long roleId = await GetRoleId(userId);
            string roleName = await GetRoleName(userId);
            // Check if user has view permission
            if (!await CheckPermission(roleId, "Xuat"))
            {
                return Unauthorized(new { statusCode = 401, message = "Không có quyền xem danh sách công việc" });
            }

            // Check if user has "Quản lý" role
            // Logic: Admin/HR sees all, Employee sees self
            bool isAdminOrHr = roleName.Contains("Admin", StringComparison.OrdinalIgnoreCase) ||
                               roleName.Contains("Nhân sự", StringComparison.OrdinalIgnoreCase);

            if (isAdminOrHr) 
            {
                var tasks = await _context.CongViecs
                .Include(c => c.IdNguoiGiaoNavigation)
                .Include(c => c.IdNguoiNhanNavigation)
                .Include(c => c.IdDuAnNavigation)
                .OrderByDescending(c => c.NgayBatDau)
                .Select(c => new
                {
                    c.Id,
                    c.TieuDe,
                    c.MoTa,
                    c.NgayBatDau,
                    c.HanHoanThanh,
                    c.TrangThai,
                    NguoiGiao = c.IdNguoiGiaoNavigation != null ? c.IdNguoiGiaoNavigation.HoTen : "Unknown",
                    NguoiNhan = c.IdNguoiNhanNavigation != null ? c.IdNguoiNhanNavigation.HoTen : "Unknown",
                    DuAn = c.IdDuAnNavigation != null ? c.IdDuAnNavigation.TenDuAn : null
                })
                .ToListAsync();

                return Ok(new { statusCode = 200, message = "Lấy toàn bộ danh sách công việc thành công", data = tasks });
            }
            else
            {
                return Unauthorized(new { statusCode = 403, message = "Chỉ quản lý mới có quyền xem toàn bộ công việc" });
            }
        }

        [HttpPost("update-status")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusRequest request)
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            long roleId = await GetRoleId(userId);
            // Updating status is technically "Sua" (Edit)
            //if (!await CheckPermission(roleId, "Sua"))
            //{
            //    return Unauthorized(new { statusCode = 401, message = "Không có quyền cập nhật công việc" });
            //}

            var task = await _context.CongViecs.FindAsync(request.Id);
            if (task == null) return Ok(new { statusCode = 404, message = "Không tìm thấy công việc" });

            var nhanVien = await _context.NhanViens.FirstOrDefaultAsync(n => n.IdTaiKhoan == userId);
            
            // Ensure the user is the receiver (or maybe the assigner too?)
            // Requirement: "Employee updates status" -> implies Receiver
            if (nhanVien == null || task.IdNguoiNhan != nhanVien.Id)
            {
                return Unauthorized(new { statusCode = 403, message = "Bạn không được giao công việc này" });
            }

            task.TrangThai = request.TrangThai;
            if (!string.IsNullOrEmpty(request.GhiChu))
            {
                task.MoTa += $" | Update: {request.GhiChu}";
            }

            await _context.SaveChangesAsync();

            return Ok(new { statusCode = 200, message = "Cập nhật trạng thái thành công" });
        }

        public class AssignTaskRequest
        {
            public string TieuDe { get; set; }
            public string? MoTa { get; set; }
            public DateOnly? HanHoanThanh { get; set; }
            public long IdNguoiNhan { get; set; }
        }

        public class UpdateStatusRequest
        {
            public long Id { get; set; }
            public string TrangThai { get; set; }
            public string? GhiChu { get; set; }
        }
    }
}
