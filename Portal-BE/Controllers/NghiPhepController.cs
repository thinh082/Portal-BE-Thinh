using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal_BE.Models.Entities;

namespace Portal_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NghiPhepController : ControllerBase
    {
        private readonly ThinhContext _context;
        private readonly int IdChucNang = 3; // NghiPhep

        public NghiPhepController(ThinhContext context)
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

        [HttpPost("create-request")]
        public async Task<IActionResult> CreateRequest([FromBody] CreateNghiPhepRequest request)
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            // Check if user is an employee
            var nhanVien = await _context.NhanViens.FirstOrDefaultAsync(n => n.IdTaiKhoan == userId);
            if (nhanVien == null)
            {
                return Ok(new { statusCode = 400, message = "Tài khoản không phải là nhân viên" });
            }

            // Optional: Check 'Them' permission, but usually all employees can request leave
            // long roleId = await GetRoleId(userId);
            // if (!await CheckPermission(roleId, "Them")) ...

            var nghiPhep = new NghiPhep
            {
                IdNhanVien = nhanVien.Id,
                NgayBatDau = request.NgayBatDau,
                NgayKetThuc = request.NgayKetThuc,
                LyDo = request.LyDo,
                TrangThai = "Chờ duyệt"
            };

            _context.NghiPheps.Add(nghiPhep);
            await _context.SaveChangesAsync();

            return Ok(new { statusCode = 200, message = "Tạo yêu cầu nghỉ phép thành công", data = nghiPhep.Id });
        }

        [HttpGet("get-list")]
        public async Task<IActionResult> GetList()
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            var today = DateOnly.FromDateTime(DateTime.Now);

            long roleId = await GetRoleId(userId);
            string roleName = await GetRoleName(userId);

            if (!await CheckPermission(roleId, "Xuat"))
            {
                return Unauthorized(new { statusCode = 401, message = "Không có quyền xem danh sách nghỉ phép" });
            }

            bool isAdminOrHr = roleName.Contains("Admin", StringComparison.OrdinalIgnoreCase) || 
                               roleName.Contains("Nhân sự", StringComparison.OrdinalIgnoreCase) ||
                               roleName.Contains("HR", StringComparison.OrdinalIgnoreCase);

            if (isAdminOrHr)
            {
                var list = await _context.NghiPheps
                    .Include(np => np.IdNhanVienNavigation)
                    .Include(np => np.IdNguoiDuyetNavigation)
                    .Where(np => np.NgayBatDau >= today)
                    .OrderByDescending(np => np.NgayBatDau)
                    .Select(np => new 
                    {
                        np.Id,
                        TenNhanVien = np.IdNhanVienNavigation.HoTen,
                        np.NgayBatDau,
                        np.NgayKetThuc,
                        np.LyDo,
                        np.TrangThai,
                        NguoiDuyet = np.IdNguoiDuyetNavigation != null ? np.IdNguoiDuyetNavigation.HoTen : null,
                        np.NgayDuyet
                    })
                    .ToListAsync();
                return Ok(new { statusCode = 200, message = "Lấy danh sách thành công", data = list });
            }
            else
            {
                var nhanVien = await _context.NhanViens.FirstOrDefaultAsync(n => n.IdTaiKhoan == userId);
                if (nhanVien == null) return Ok(new { statusCode = 404, message = "Không tìm thấy thông tin nhân viên" });

                var list = await _context.NghiPheps
                    .Where(np => np.IdNhanVien == nhanVien.Id && np.NgayBatDau >= today)
                    .OrderByDescending(np => np.NgayBatDau)
                    .Select(np => new 
                    {
                        np.Id,
                        np.NgayBatDau,
                        np.NgayKetThuc,
                        np.LyDo,
                        np.TrangThai,
                        NguoiDuyet = np.IdNguoiDuyetNavigation != null ? np.IdNguoiDuyetNavigation.HoTen : null,
                        np.NgayDuyet
                    })
                    .ToListAsync();
                return Ok(new { statusCode = 200, message = "Lấy danh sách thành công", data = list });
            }
        }

        [HttpGet("get-request-employee")]
        public async Task<IActionResult> GetRequestEmployee()
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            var nhanVien = await _context.NhanViens.FirstOrDefaultAsync(n => n.IdTaiKhoan == userId);
            if (nhanVien == null) return Ok(new { statusCode = 404, message = "Không tìm thấy thông tin nhân viên" });

            var list = await _context.NghiPheps
                .Where(np => np.IdNhanVien == nhanVien.Id)
                .OrderByDescending(np => np.NgayBatDau)
                .Select(np => new
                {
                    np.Id,
                    np.NgayBatDau,
                    np.NgayKetThuc,
                    np.LyDo,
                    np.TrangThai,
                    NguoiDuyet = np.IdNguoiDuyetNavigation != null ? np.IdNguoiDuyetNavigation.HoTen : null,
                    np.NgayDuyet
                })
                .ToListAsync();

            return Ok(new { statusCode = 200, message = "Lấy danh sách thành công", data = list });
        }

        [HttpPost("approve-reject")]
        public async Task<IActionResult> ApproveReject([FromBody] ApproveRejectRequest request)
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            long roleId = await GetRoleId(userId);
            if (!await CheckPermission(roleId, "Sua"))
            {
                return Unauthorized(new { statusCode = 401, message = "Không có quyền duyệt nghỉ phép" });
            }

            var nghiPhep = await _context.NghiPheps.FindAsync(request.RequestId);
            if (nghiPhep == null) return Ok(new { statusCode = 404, message = "Không tìm thấy yêu cầu" });

            var approver = await _context.NhanViens.FirstOrDefaultAsync(n => n.IdTaiKhoan == userId);
            if (approver == null) return Ok(new { statusCode = 400, message = "Người duyệt không phải là nhân viên hợp lệ" });

            nghiPhep.TrangThai = request.IsApproved ? "Đã duyệt" : "Từ chối";
            nghiPhep.IdNguoiDuyet = approver.Id;
            nghiPhep.NgayDuyet = DateTime.Now;

            // Append note if provided
            if (!string.IsNullOrEmpty(request.Note))
            {
                nghiPhep.LyDo += $" | Note: {request.Note}";
            }

            await _context.SaveChangesAsync();

            return Ok(new { statusCode = 200, message = "Cập nhật trạng thái thành công" });
        }

        public class CreateNghiPhepRequest
        {
            public DateOnly NgayBatDau { get; set; }
            public DateOnly NgayKetThuc { get; set; }
            public string LyDo { get; set; }
        }

        public class ApproveRejectRequest
        {
            public long RequestId { get; set; }
            public bool IsApproved { get; set; }
            public string? Note { get; set; }
        }
    }
}
