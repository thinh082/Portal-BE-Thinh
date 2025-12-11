using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal_BE.Models.Entities;

namespace Portal_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NhanVienController : ControllerBase
    {
        private readonly ThinhContext _context;
        private readonly int IdChucNang = 1; // NhanVien

        public NhanVienController(ThinhContext context)
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

        [HttpGet("get-list")]
        public async Task<IActionResult> GetList()
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            long roleId = await GetRoleId(userId);
            string roleName = await GetRoleName(userId);

            // Check view permission
            if (!await CheckPermission(roleId, "Xuat"))
            {
                return Unauthorized(new { statusCode = 401, message = "Không có quyền xem danh sách nhân viên" });
            }

            // Logic: Admin/HR sees all, Employee sees self
            bool isAdminOrHr = roleName.Contains("Admin", StringComparison.OrdinalIgnoreCase) ||
                               roleName.Contains("Nhân sự", StringComparison.OrdinalIgnoreCase);

            if (isAdminOrHr)
            {
                var list = await _context.NhanViens
                    .Include(n => n.IdPhongBanNavigation)
                    .Include(n => n.IdTaiKhoanNavigation)
                    .Select(n => new
                    {
                        n.Id,
                        n.HoTen,
                        n.NgaySinh,
                        n.GioiTinh,
                        n.DiaChi,
                        n.Cccd,
                        n.NgayVaoLam,
                        n.ChucVu,
                        n.LuongCoBan,
                        TenPhongBan = n.IdPhongBanNavigation.TenPhongBan,
                        Email = n.IdTaiKhoanNavigation.Email,
                        SoDienThoai = n.IdTaiKhoanNavigation.SoDienThoai,
                        SoNamKinhNghiem = n.IdTaiKhoanNavigation.SoNamKinhNghiem,
                        MoTaKyNang = n.IdTaiKhoanNavigation.MoTaKyNang
                    })
                    .ToListAsync();
                return Ok(new { statusCode = 200, message = "Lấy danh sách thành công", data = list });
            }
            else
            {
                var profile = await _context.NhanViens
                    .Include(n => n.IdPhongBanNavigation)
                    .Include(n => n.IdTaiKhoanNavigation)
                    .Where(n => n.IdTaiKhoan == userId)
                    .Select(n => new
                    {
                        n.Id,
                        n.HoTen,
                        n.NgaySinh,
                        n.GioiTinh,
                        n.DiaChi,
                        n.Cccd,
                        n.NgayVaoLam,
                        n.ChucVu,
                        n.LuongCoBan,
                        TenPhongBan = n.IdPhongBanNavigation.TenPhongBan,
                        Email = n.IdTaiKhoanNavigation.Email,
                        SoDienThoai = n.IdTaiKhoanNavigation.SoDienThoai,
                        SoNamKinhNghiem = n.IdTaiKhoanNavigation.SoNamKinhNghiem,
                        MoTaKyNang = n.IdTaiKhoanNavigation.MoTaKyNang
                    })
                    .FirstOrDefaultAsync();
                
                if (profile == null) return Ok(new { statusCode = 404, message = "Không tìm thấy hồ sơ nhân viên" });

                return Ok(new { statusCode = 200, message = "Lấy thông tin thành công", data = new[] { profile } }); 
            }
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });


            var nv = await _context.NhanViens
                .Include(n => n.IdPhongBanNavigation)
                .Include(n => n.IdTaiKhoanNavigation)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (nv == null) return Ok(new { statusCode = 404, message = "Không tìm thấy nhân viên" });

           

            var data = new
            {
                nv.Id,
                nv.HoTen,
                nv.NgaySinh,
                nv.GioiTinh,
                nv.DiaChi,
                nv.Cccd,
                nv.NgayVaoLam,
                nv.ChucVu,
                nv.LuongCoBan,
                nv.IdPhongBan,
                TenPhongBan = nv.IdPhongBanNavigation?.TenPhongBan,
                Email = nv.IdTaiKhoanNavigation?.Email,
                SoDienThoai = nv.IdTaiKhoanNavigation?.SoDienThoai,
                SoNamKinhNghiem = nv.IdTaiKhoanNavigation?.SoNamKinhNghiem,
                MoTaKyNang = nv.IdTaiKhoanNavigation?.MoTaKyNang
            };

            return Ok(new { statusCode = 200, message = "Lấy thông tin thành công", data = data });
        }

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] AddNhanVienRequest request)
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            long roleId = await GetRoleId(userId);
            if (!await CheckPermission(roleId, "Them"))
            {
                return Unauthorized(new { statusCode = 401, message = "Không có quyền thêm nhân viên" });
            }

            var nv = new NhanVien
            {
                HoTen = request.HoTen,
                NgaySinh = request.NgaySinh,
                GioiTinh = request.GioiTinh,
                DiaChi = request.DiaChi,
                Cccd = request.Cccd,
                NgayVaoLam = request.NgayVaoLam,
                ChucVu = request.ChucVu,
                LuongCoBan = request.LuongCoBan,
                IdPhongBan = request.IdPhongBan,
                IdTaiKhoan = request.IdTaiKhoan 
            };

            _context.NhanViens.Add(nv);
            await _context.SaveChangesAsync();

            return Ok(new { statusCode = 200, message = "Thêm nhân viên thành công", data = nv.Id });
        }

        [HttpPost("update")]
        public async Task<IActionResult> Update([FromBody] UpdateNhanVienRequest request)
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            long roleId = await GetRoleId(userId);
            string roleName = await GetRoleName(userId);

            if (!await CheckPermission(roleId, "Sua"))
            {
                return Unauthorized(new { statusCode = 401, message = "Không có quyền sửa thông tin" });
            }

            var nv = await _context.NhanViens
                .Include(n => n.IdTaiKhoanNavigation)
                .FirstOrDefaultAsync(n => n.Id == request.Id);

            if (nv == null) return Ok(new { statusCode = 404, message = "Không tìm thấy nhân viên" });

            bool isAdminOrHr = roleName.Contains("Admin", StringComparison.OrdinalIgnoreCase) || 
                               roleName.Contains("Nhân sự", StringComparison.OrdinalIgnoreCase) ||
                               roleName.Contains("HR", StringComparison.OrdinalIgnoreCase);

            // Logic: Admin/HR updates all. Employee updates only contact info.
            // Check if user is trying to update someone else's profile while not being Admin/HR
            if (!isAdminOrHr && nv.IdTaiKhoan != userId)
            {
                return Unauthorized(new { statusCode = 403, message = "Bạn chỉ được phép sửa thông tin của chính mình" });
            }

            if (isAdminOrHr)
            {
                // Admin/HR can update everything
                nv.HoTen = request.HoTen ?? nv.HoTen;
                nv.NgaySinh = request.NgaySinh ?? nv.NgaySinh;
                nv.GioiTinh = request.GioiTinh ?? nv.GioiTinh;
                nv.DiaChi = request.DiaChi ?? nv.DiaChi;
                nv.Cccd = request.Cccd ?? nv.Cccd;
                nv.NgayVaoLam = request.NgayVaoLam ?? nv.NgayVaoLam;
                nv.ChucVu = request.ChucVu ?? nv.ChucVu;
                nv.LuongCoBan = request.LuongCoBan ?? nv.LuongCoBan;
                nv.IdPhongBan = request.IdPhongBan ?? nv.IdPhongBan;
                
                // Also update contact info if provided
                if (nv.IdTaiKhoanNavigation != null)
                {
                    nv.IdTaiKhoanNavigation.Email = request.Email ?? nv.IdTaiKhoanNavigation.Email;
                    nv.IdTaiKhoanNavigation.SoDienThoai = request.SoDienThoai ?? nv.IdTaiKhoanNavigation.SoDienThoai;
                }
            }
            else
            {
                // Employee can only update Address, Phone, Email
                nv.DiaChi = request.DiaChi ?? nv.DiaChi;
                if (nv.IdTaiKhoanNavigation != null)
                {
                    nv.IdTaiKhoanNavigation.Email = request.Email ?? nv.IdTaiKhoanNavigation.Email;
                    nv.IdTaiKhoanNavigation.SoDienThoai = request.SoDienThoai ?? nv.IdTaiKhoanNavigation.SoDienThoai;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { statusCode = 200, message = "Cập nhật thành công" });
        }

        [HttpPost("delete")]
        public async Task<IActionResult> Delete([FromBody] DeleteRequest request)
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            long roleId = await GetRoleId(userId);
            if (!await CheckPermission(roleId, "Xoa"))
            {
                return Unauthorized(new { statusCode = 401, message = "Không có quyền xóa nhân viên" });
            }

            var nv = await _context.NhanViens.FindAsync(request.Id);
            if (nv == null) return Ok(new { statusCode = 404, message = "Không tìm thấy nhân viên" });

            _context.NhanViens.Remove(nv);
            await _context.SaveChangesAsync();

            return Ok(new { statusCode = 200, message = "Xóa nhân viên thành công" });
        }

        [HttpPost("add-ot")]
        public async Task<IActionResult> AddOT([FromBody] AddOTDto request)
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            // Optional: Check permissions if needed, or assume self-request is allowed
            // long roleId = await GetRoleId(userId);
            
            // If request.IdNhanVien is null, assume it's for the current user (if linked)
            long targetEmployeeId = request.IdNhanVien ?? 0;

            if (targetEmployeeId == 0)
            {
                 var currentUserNv = await _context.NhanViens.FirstOrDefaultAsync(n => n.IdTaiKhoan == userId);
                 if (currentUserNv != null) targetEmployeeId = currentUserNv.Id;
            }

            if (targetEmployeeId == 0)
            {
                return Ok(new { statusCode = 400, message = "Không xác định được nhân viên" });
            }

            var tangCa = new TangCa
            {
                IdNhanVien = targetEmployeeId,
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

        public class AddNhanVienRequest
        {
            public string HoTen { get; set; }
            public DateOnly? NgaySinh { get; set; }
            public string? GioiTinh { get; set; }
            public string? DiaChi { get; set; }
            public string? Cccd { get; set; }
            public DateOnly? NgayVaoLam { get; set; }
            public string? ChucVu { get; set; }
            public decimal? LuongCoBan { get; set; }
            public long? IdPhongBan { get; set; }
            public long? IdTaiKhoan { get; set; }
        }

        public class UpdateNhanVienRequest
        {
            public long Id { get; set; }
            public string? HoTen { get; set; }
            public DateOnly? NgaySinh { get; set; }
            public string? GioiTinh { get; set; }
            public string? DiaChi { get; set; }
            public string? Cccd { get; set; }
            public DateOnly? NgayVaoLam { get; set; }
            public string? ChucVu { get; set; }
            public decimal? LuongCoBan { get; set; }
            public long? IdPhongBan { get; set; }
            public string? Email { get; set; }
            public string? SoDienThoai { get; set; }
        }

        public class DeleteRequest
        {
            public long Id { get; set; }
        }

        public class AddOTDto
        {
            public long? IdNhanVien { get; set; }
            public TimeOnly? GioBatDau { get; set; }
            public TimeOnly? GioKetThuc { get; set; }
            public double? SoGioLam { get; set; }
            public double? HeSo { get; set; }
            public string? LyDoTangCa { get; set; }
        }
    }
}
