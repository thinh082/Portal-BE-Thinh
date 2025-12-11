using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal_BE.Models.Entities;

namespace Portal_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ThongKeController : ControllerBase
    {
        private readonly ThinhContext _context;

        public ThongKeController(ThinhContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var salaryByMonth = await _context.BangLuongs
                .GroupBy(b => new { b.Nam, b.Thang })
                .Select(g => new
                {
                    Nam = g.Key.Nam,
                    Thang = g.Key.Thang,
                    TongLuong = g.Sum(x => x.TongLuong ?? 0)
                })
                .OrderBy(g => g.Nam).ThenBy(g => g.Thang)
                .ToListAsync();

            var leaveByMonth = await _context.NghiPheps
                .GroupBy(np => new { Year = np.NgayBatDau.Year, Month = np.NgayBatDau.Month })
                .Select(g => new
                {
                    Nam = g.Key.Year,
                    Thang = g.Key.Month,
                    SoNgayNghi = g.Sum(x => EF.Functions.DateDiffDay(x.NgayBatDau, x.NgayKetThuc) + 1)
                })
                .OrderBy(g => g.Nam).ThenBy(g => g.Thang)
                .ToListAsync();

            var startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-6));
            var gioDungGio = new TimeOnly(8, 30);

            var attendanceLast7Days = await _context.ChamCongs
                .Where(c => c.Ngay >= startDate)
                .GroupBy(c => c.Ngay)
                .Select(g => new
                {
                    Ngay = g.Key,
                    Tong = g.Count(),
                    DungGio = g.Count(c => c.GioVao <= gioDungGio)
                })
                .OrderBy(g => g.Ngay)
                .ToListAsync();

            var taskStatus = await _context.CongViecs
                .GroupBy(c => c.TrangThai)
                .Select(g => new
                {
                    TrangThai = g.Key ?? "Không rõ",
                    SoLuong = g.Count()
                })
                .ToListAsync();

            var overtimeByEmployee = await _context.TangCas
                .Include(t => t.IdNhanVienNavigation)
                .GroupBy(t => t.IdNhanVienNavigation.HoTen)
                .Select(g => new
                {
                    TenNhanVien = g.Key,
                    TongGioOt = g.Sum(x => x.SoGioLam ?? 0)
                })
                .OrderByDescending(g => g.TongGioOt)
                .Take(5)
                .ToListAsync();

            return Ok(new
            {
                statusCode = 200,
                message = "Lấy dữ liệu thống kê dashboard thành công",
                data = new
                {
                    salaryByMonth,
                    leaveByMonth,
                    attendanceLast7Days,
                    taskStatus,
                    overtimeByEmployee
                }
            });
        }
    }
}


