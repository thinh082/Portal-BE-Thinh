using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal_BE.Models.Entities;

namespace Portal_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DuAnController : ControllerBase
    {
        private readonly ThinhContext _context;

        public DuAnController(ThinhContext context)
        {
            _context = context;
        }

        [HttpGet("get-list")]
        public async Task<IActionResult> GetList()
        {
            var list = await _context.DuAns
                .OrderByDescending(d => d.NgayBatDau)
                .Select(d => new
                {
                    d.Id,
                    d.TenDuAn,
                    d.NgayBatDau,
                    d.NgayKetThuc,
                    d.TrangThai
                })
                .ToListAsync();

            return Ok(new { statusCode = 200, message = "Lấy danh sách dự án thành công", data = list });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var duAn = await _context.DuAns.FindAsync(id);
            if (duAn == null) return Ok(new { statusCode = 404, message = "Không tìm thấy dự án" });

            return Ok(new { statusCode = 200, data = duAn });
        }

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] DuAnRequest request)
        {
            var duAn = new DuAn
            {
                TenDuAn = request.TenDuAn,
                NgayBatDau = request.NgayBatDau,
                NgayKetThuc = request.NgayKetThuc,
                TrangThai = request.TrangThai ?? "Đang thực hiện"
            };
            _context.DuAns.Add(duAn);
            await _context.SaveChangesAsync();
            return Ok(new { statusCode = 200, message = "Thêm dự án thành công", data = duAn.Id });
        }

        [HttpPost("update")]
        public async Task<IActionResult> Update([FromBody] DuAnRequest request)
        {
            var duAn = await _context.DuAns.FindAsync(request.Id);
            if (duAn == null) return Ok(new { statusCode = 404, message = "Không tìm thấy dự án" });

            duAn.TenDuAn = request.TenDuAn ?? duAn.TenDuAn;
            duAn.NgayBatDau = request.NgayBatDau ?? duAn.NgayBatDau;
            duAn.NgayKetThuc = request.NgayKetThuc ?? duAn.NgayKetThuc;
            duAn.TrangThai = request.TrangThai ?? duAn.TrangThai;

            await _context.SaveChangesAsync();
            return Ok(new { statusCode = 200, message = "Cập nhật dự án thành công" });
        }

        [HttpPost("delete")]
        public async Task<IActionResult> Delete([FromBody] IdRequest request)
        {
            var duAn = await _context.DuAns.FindAsync(request.Id);
            if (duAn == null) return Ok(new { statusCode = 404, message = "Không tìm thấy dự án" });

            _context.DuAns.Remove(duAn);
            await _context.SaveChangesAsync();
            return Ok(new { statusCode = 200, message = "Xóa dự án thành công" });
        }

        public class DuAnRequest
        {
            public long Id { get; set; }
            public string? TenDuAn { get; set; }
            public DateTime? NgayBatDau { get; set; }
            public DateTime? NgayKetThuc { get; set; }
            public string? TrangThai { get; set; }
        }

        public class IdRequest
        {
            public long Id { get; set; }
        }
    }
}


