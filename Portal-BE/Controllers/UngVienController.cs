using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal_BE.Models.Entities;

namespace Portal_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UngVienController : ControllerBase
    {
        private readonly ThinhContext _context;
        private readonly int IdChucNang = 6; // UngVien

        public UngVienController(ThinhContext context)
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

            long roleId = await GetRoleId(userId);
            if (!await CheckPermission(roleId, "Xuat"))
            {
                return Unauthorized(new { statusCode = 401, message = "Không có quyền xem danh sách ứng viên" });
            }

            var today = DateOnly.FromDateTime(DateTime.Now);
            
            var list = await _context.UngViens
                .Where(u => u.NgayNopHoSo.HasValue && 
                           DateOnly.FromDateTime(u.NgayNopHoSo.Value) == today &&
                           (u.TrangThaiHienTai == null || u.TrangThaiHienTai != 3))
                .OrderByDescending(u => u.NgayNopHoSo)
                .Select(u => new
                {
                    u.Id,
                    u.HoTen,
                    u.Email,
                    u.SoDienThoai,
                    u.ViTriUngTuyen,
                    u.DuongDanCv,
                    u.LinkPortfolio,
                    u.TrangThaiHienTai,
                    u.NgayNopHoSo
                })
                .ToListAsync();

            return Ok(new { statusCode = 200, message = "Lấy danh sách ứng viên thành công", data = list });
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll()
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            long roleId = await GetRoleId(userId);
            if (!await CheckPermission(roleId, "Xuat"))
            {
                return Unauthorized(new { statusCode = 401, message = "Không có quyền xem danh sách ứng viên" });
            }
            var today = DateOnly.FromDateTime(DateTime.Now);
            var list = await _context.UngViens
                .Where(u => u.NgayNopHoSo.HasValue &&
                           DateOnly.FromDateTime(u.NgayNopHoSo.Value) == today &&
                           (u.TrangThaiHienTai == null || u.TrangThaiHienTai != 3))
                .Select(u => new
                {
                    u.Id,
                    u.HoTen,
                    u.Email,
                    u.SoDienThoai,
                    u.ViTriUngTuyen,
                    u.DuongDanCv,
                    u.LinkPortfolio,
                    u.TrangThaiHienTai,
                    u.NgayNopHoSo,
                    IdDanhGia = u.LichSuDanhGia
                        .OrderByDescending(l => l.NgayDanhGia)
                        .Select(l => (int?)l.Id)
                        .FirstOrDefault() ?? 0
                })
                .OrderByDescending(u => u.NgayNopHoSo)
                .ToListAsync();

            return Ok(new { statusCode = 200, message = "Lấy danh sách ứng viên thành công", data = list });
        }

        [HttpGet("get-lich-su-danh-gia/{id}")]
        public async Task<IActionResult> GetLichSuDanhGiaById(int id)
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            long roleId = await GetRoleId(userId);
            if (!await CheckPermission(roleId, "Xuat"))
            {
                return Unauthorized(new { statusCode = 401, message = "Không có quyền xem lịch sử đánh giá" });
            }

            var lichSuDanhGia = await _context.LichSuDanhGia
                .Include(l => l.IdUngVienNavigation)
                .Where(l => l.Id == id)
                .Select(l => new
                {
                    l.Id,
                    l.IdUngVien,
                    TenUngVien = l.IdUngVienNavigation.HoTen,
                    l.MaNguoiDanhGia,
                    l.VongPhongVan,
                    l.NhanXetChuyenMon,
                    l.DiemSo,
                    l.KetQua,
                    l.NgayDanhGia
                })
                .FirstOrDefaultAsync();

            if (lichSuDanhGia == null)
            {
                return Ok(new { statusCode = 404, message = "Không tìm thấy lịch sử đánh giá" });
            }

            return Ok(new { statusCode = 200, message = "Lấy chi tiết lịch sử đánh giá thành công", data = lichSuDanhGia });
        }

        [HttpPost("update-ung-vien")]
        public async Task<IActionResult> UpdateUngVien([FromBody] UpdateUngVienRequest request)
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            long roleId = await GetRoleId(userId);
            if (!await CheckPermission(roleId, "Sua"))
            {
                return Unauthorized(new { statusCode = 401, message = "Không có quyền cập nhật ứng viên" });
            }

            if (request == null)
            {
                return Ok(new { statusCode = 400, message = "Dữ liệu không hợp lệ" });
            }

            // Kiểm tra nếu id = 0 hoặc không tồn tại thì thêm mới
            if (request.Id == 0)
            {
                // Thêm mới ứng viên
                var ungVien = new UngVien
                {
                    HoTen = request.HoTen,
                    Email = request.Email,
                    SoDienThoai = request.SoDienThoai,
                    ViTriUngTuyen = request.ViTriUngTuyen,
                    DuongDanCv = request.DuongDanCv,
                    LinkPortfolio = request.LinkPortfolio,
                    TrangThaiHienTai = request.TrangThaiHienTai,
                    NgayNopHoSo = request.NgayNopHoSo ?? DateTime.Now
                };

                _context.UngViens.Add(ungVien);
                await _context.SaveChangesAsync();

                return Ok(new { statusCode = 200, message = "Thêm mới ứng viên thành công", data = ungVien.Id });
            }
            else
            {
                // Kiểm tra xem ứng viên có tồn tại không
                var ungVien = await _context.UngViens.FindAsync(request.Id);
                if (ungVien == null)
                {
                    // Nếu không tồn tại thì thêm mới
                    ungVien = new UngVien
                    {
                        HoTen = request.HoTen,
                        Email = request.Email,
                        SoDienThoai = request.SoDienThoai,
                        ViTriUngTuyen = request.ViTriUngTuyen,
                        DuongDanCv = request.DuongDanCv,
                        LinkPortfolio = request.LinkPortfolio,
                        TrangThaiHienTai = request.TrangThaiHienTai,
                        NgayNopHoSo = request.NgayNopHoSo ?? DateTime.Now
                    };

                    _context.UngViens.Add(ungVien);
                    await _context.SaveChangesAsync();

                    return Ok(new { statusCode = 200, message = "Thêm mới ứng viên thành công", data = ungVien.Id });
                }
                else
                {
                    // Cập nhật ứng viên
                    ungVien.HoTen = request.HoTen ?? ungVien.HoTen;
                    ungVien.Email = request.Email ?? ungVien.Email;
                    ungVien.SoDienThoai = request.SoDienThoai ?? ungVien.SoDienThoai;
                    ungVien.ViTriUngTuyen = request.ViTriUngTuyen ?? ungVien.ViTriUngTuyen;
                    ungVien.DuongDanCv = request.DuongDanCv ?? ungVien.DuongDanCv;
                    ungVien.LinkPortfolio = request.LinkPortfolio ?? ungVien.LinkPortfolio;
                    ungVien.TrangThaiHienTai = request.TrangThaiHienTai ?? ungVien.TrangThaiHienTai;
                    if (request.NgayNopHoSo.HasValue)
                    {
                        ungVien.NgayNopHoSo = request.NgayNopHoSo;
                    }

                    await _context.SaveChangesAsync();

                    return Ok(new { statusCode = 200, message = "Cập nhật ứng viên thành công" });
                }
            }
        }

        [HttpPost("update-lich-su-danh-gia")]
        public async Task<IActionResult> UpdateLichSuDanhGia([FromBody] UpdateLichSuDanhGiaRequest request)
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            long roleId = await GetRoleId(userId);
            if (!await CheckPermission(roleId, "Sua"))
            {
                return Unauthorized(new { statusCode = 401, message = "Không có quyền cập nhật lịch sử đánh giá" });
            }

            if (request == null)
            {
                return Ok(new { statusCode = 400, message = "Dữ liệu không hợp lệ" });
            }

            // Kiểm tra ứng viên có tồn tại không
            var ungVien = await _context.UngViens.FindAsync(request.IdUngVien);
            if (ungVien == null)
            {
                return Ok(new { statusCode = 404, message = "Không tìm thấy ứng viên" });
            }

            // Kiểm tra nếu id = 0 hoặc không tồn tại thì thêm mới
            if (request.Id == 0)
            {
                // Thêm mới lịch sử đánh giá
                var lichSuDanhGia = new LichSuDanhGium
                {
                    IdUngVien = request.IdUngVien,
                    MaNguoiDanhGia = request.MaNguoiDanhGia,
                    VongPhongVan = request.VongPhongVan,
                    NhanXetChuyenMon = request.NhanXetChuyenMon,
                    DiemSo = request.DiemSo,
                    KetQua = request.KetQua,
                    NgayDanhGia = request.NgayDanhGia ?? DateTime.Now
                };

                _context.LichSuDanhGia.Add(lichSuDanhGia);
                await _context.SaveChangesAsync();

                return Ok(new { statusCode = 200, message = "Thêm mới lịch sử đánh giá thành công", data = lichSuDanhGia.Id });
            }
            else
            {
                // Kiểm tra xem lịch sử đánh giá có tồn tại không
                var lichSuDanhGia = await _context.LichSuDanhGia.FindAsync(request.Id);
                if (lichSuDanhGia == null)
                {
                    // Nếu không tồn tại thì thêm mới
                    lichSuDanhGia = new LichSuDanhGium
                    {
                        IdUngVien = request.IdUngVien,
                        MaNguoiDanhGia = request.MaNguoiDanhGia,
                        VongPhongVan = request.VongPhongVan,
                        NhanXetChuyenMon = request.NhanXetChuyenMon,
                        DiemSo = request.DiemSo,
                        KetQua = request.KetQua,
                        NgayDanhGia = request.NgayDanhGia ?? DateTime.Now
                    };

                    _context.LichSuDanhGia.Add(lichSuDanhGia);
                    await _context.SaveChangesAsync();

                    return Ok(new { statusCode = 200, message = "Thêm mới lịch sử đánh giá thành công", data = lichSuDanhGia.Id });
                }
                else
                {
                    // Cập nhật lịch sử đánh giá
                    lichSuDanhGia.IdUngVien = request.IdUngVien;
                    lichSuDanhGia.MaNguoiDanhGia = request.MaNguoiDanhGia ?? lichSuDanhGia.MaNguoiDanhGia;
                    lichSuDanhGia.VongPhongVan = request.VongPhongVan ?? lichSuDanhGia.VongPhongVan;
                    lichSuDanhGia.NhanXetChuyenMon = request.NhanXetChuyenMon ?? lichSuDanhGia.NhanXetChuyenMon;
                    lichSuDanhGia.DiemSo = request.DiemSo ?? lichSuDanhGia.DiemSo;
                    lichSuDanhGia.KetQua = request.KetQua ?? lichSuDanhGia.KetQua;
                    if (request.NgayDanhGia.HasValue)
                    {
                        lichSuDanhGia.NgayDanhGia = request.NgayDanhGia;
                    }

                    await _context.SaveChangesAsync();

                    return Ok(new { statusCode = 200, message = "Cập nhật lịch sử đánh giá thành công" });
                }
            }
        }

        public class UpdateUngVienRequest
        {
            public int Id { get; set; }
            public string? HoTen { get; set; }
            public string? Email { get; set; }
            public string? SoDienThoai { get; set; }
            public string? ViTriUngTuyen { get; set; }
            public string? DuongDanCv { get; set; }
            public string? LinkPortfolio { get; set; }
            public int? TrangThaiHienTai { get; set; }
            public DateTime? NgayNopHoSo { get; set; }
        }

        public class UpdateLichSuDanhGiaRequest
        {
            public int Id { get; set; }
            public int IdUngVien { get; set; }
            public int? MaNguoiDanhGia { get; set; }
            public string? VongPhongVan { get; set; }
            public string? NhanXetChuyenMon { get; set; }
            public double? DiemSo { get; set; }
            public bool? KetQua { get; set; }
            public DateTime? NgayDanhGia { get; set; }
        }
    }
}

