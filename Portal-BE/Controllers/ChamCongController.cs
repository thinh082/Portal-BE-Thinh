using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal_BE.Models.Entities;
using System.Security.Claims;

namespace Portal_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChamCongController : ControllerBase
    {
        private readonly ThinhContext _context;

        public ChamCongController(ThinhContext context)
        {
            _context = context;
        }

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("Id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdClaim, out long userId) ? userId : 0;
        }

        private async Task<NhanVien?> GetNhanVienFromCurrentUser()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return null;
            return await _context.NhanViens.FirstOrDefaultAsync(n => n.IdTaiKhoan == userId);
        }

        [HttpGet("MyToday")]
        public async Task<IActionResult> MyToday()
        {
            var nhanVien = await GetNhanVienFromCurrentUser();
            if (nhanVien == null)
            {
                return Ok(new { statusCode = 400, message = "Tài khoản không gắn với nhân viên" });
            }

            var today = DateOnly.FromDateTime(DateTime.Now);
            var record = await _context.ChamCongs
                .Where(c => c.IdNhanVien == nhanVien.Id && c.Ngay == today)
                .Select(c => new
                {
                    c.Id,
                    c.IdNhanVien,
                    c.Ngay,
                    c.GioVao,
                    c.GioRa,
                    c.HinhThuc,
                    c.GhiChu,
                    c.ViTri
                })
                .FirstOrDefaultAsync();

            return Ok(new
            {
                statusCode = 200,
                message = record != null ? "Đã lấy chấm công hôm nay" : "Chưa có chấm công",
                data = record
            });
        }

        public class CheckRequest
        {
            public string? ViTri { get; set; }
        }

        [HttpPost("CheckIn")]
        public async Task<IActionResult> CheckIn([FromBody] CheckRequest? request)
        {
            var nhanVien = await GetNhanVienFromCurrentUser();
            if (nhanVien == null)
            {
                return Ok(new { statusCode = 400, message = "Tài khoản không gắn với nhân viên" });
            }

            var today = DateOnly.FromDateTime(DateTime.Now);
            var now = TimeOnly.FromDateTime(DateTime.Now);

            var chamCong = await _context.ChamCongs
                .FirstOrDefaultAsync(c => c.IdNhanVien == nhanVien.Id && c.Ngay == today);

            if (chamCong != null && chamCong.GioVao.HasValue)
            {
                return Ok(new { statusCode = 400, message = "Hôm nay đã chấm công" });
            }

            if (chamCong == null)
            {
                chamCong = new ChamCong
                {
                    IdNhanVien = nhanVien.Id,
                    Ngay = today,
                    GioVao = now,
                    HinhThuc = "CheckIn",
                    GhiChu = "Chấm công tự động",
                    ViTri = request?.ViTri
                };

                _context.ChamCongs.Add(chamCong);
            }
            else
            {
                chamCong.GioVao = now;
                chamCong.HinhThuc = "CheckIn";
                if (!string.IsNullOrWhiteSpace(request?.ViTri))
                    chamCong.ViTri = request.ViTri;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                statusCode = 200,
                message = "Chấm công thành công",
                data = new
                {
                    chamCong.Id,
                    chamCong.Ngay,
                    chamCong.GioVao,
                    chamCong.ViTri
                }
            });
        }

        [HttpPost("CheckOut")]
        public async Task<IActionResult> CheckOut([FromBody] CheckRequest? request)
        {
            var nhanVien = await GetNhanVienFromCurrentUser();
            if (nhanVien == null)
            {
                return Ok(new { statusCode = 400, message = "Tài khoản không gắn với nhân viên" });
            }

            var today = DateOnly.FromDateTime(DateTime.Now);
            var now = TimeOnly.FromDateTime(DateTime.Now);

            var chamCong = await _context.ChamCongs
                .FirstOrDefaultAsync(c => c.IdNhanVien == nhanVien.Id && c.Ngay == today);

            if (chamCong == null)
            {
                chamCong = new ChamCong
                {
                    IdNhanVien = nhanVien.Id,
                    Ngay = today,
                    GioRa = now,
                    HinhThuc = "CheckOut",
                    GhiChu = "Auto checkout",
                    ViTri = request?.ViTri
                };
                _context.ChamCongs.Add(chamCong);
            }
            else
            {
                chamCong.GioRa = now;
                chamCong.HinhThuc = "CheckOut";
                if (!string.IsNullOrWhiteSpace(request?.ViTri))
                    chamCong.ViTri = request.ViTri;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                statusCode = 200,
                message = "Check-out thành công",
                data = new
                {
                    chamCong.Id,
                    chamCong.Ngay,
                    chamCong.GioVao,
                    chamCong.GioRa,
                    chamCong.ViTri
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> Upsert([FromBody] ChamCongRequest request)
        {
            if (request == null || request.IdNhanVien <= 0)
            {
                return Ok(new { statusCode = 400, message = "Dữ liệu không hợp lệ" });
            }

            var targetDate = request.Ngay ?? DateOnly.FromDateTime(DateTime.Now);

            var chamCong = await _context.ChamCongs
                .FirstOrDefaultAsync(c => c.IdNhanVien == request.IdNhanVien && c.Ngay == targetDate);

            bool isNew = chamCong == null;

            if (isNew)
            {
                chamCong = new ChamCong
                {
                    IdNhanVien = request.IdNhanVien,
                    Ngay = targetDate
                };
                _context.ChamCongs.Add(chamCong);
            }

            chamCong.GioVao = request.GioVao ?? chamCong.GioVao;
            chamCong.GioRa = request.GioRa ?? chamCong.GioRa;
            chamCong.GhiChu = request.GhiChu ?? chamCong.GhiChu;
            chamCong.HinhThuc = request.HinhThuc ?? chamCong.HinhThuc;
            chamCong.ViTri = request.ViTri ?? chamCong.ViTri;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                statusCode = 200,
                message = isNew ? "Tạo chấm công thành công" : "Cập nhật chấm công thành công",
                data = new
                {
                    chamCong.Id,
                    chamCong.IdNhanVien,
                    chamCong.Ngay,
                    chamCong.GioVao,
                    chamCong.GioRa,
                    chamCong.HinhThuc,
                    chamCong.GhiChu
                }
            });
        }

        public class ChamCongRequest
        {
            public long IdNhanVien { get; set; }
            public DateOnly? Ngay { get; set; }
            public TimeOnly? GioVao { get; set; }
            public TimeOnly? GioRa { get; set; }
            public string? HinhThuc { get; set; }
            public string? GhiChu { get; set; }
            public string? ViTri { get; set; }
        }
    }
}


