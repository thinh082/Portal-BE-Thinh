using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal_BE.Models.Entities;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Drawing;

namespace Portal_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BangLuongController : ControllerBase
    {
        private readonly ThinhContext _context;
        private readonly int IdChucNang = 4; // BangLuong

        public BangLuongController(ThinhContext context)
        {
            _context = context;
        }

        // Helper to get current user ID from header (Simulated Auth)
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

        [HttpPost("calculate")]
        public async Task<IActionResult> Calculate([FromBody] CalculateRequest request)
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            long roleId = await GetRoleId(userId);
            if (!await CheckPermission(roleId, "Them")) // Assuming Calculate requires 'Add' permission
            {
                return Unauthorized(new { statusCode = 401, message = "Không có quyền thực hiện chức năng này" });
            }

            if (request.Thang <= 0 || request.Thang > 12 || request.Nam <= 0)
            {
                return Ok(new { statusCode = 400, message = "Tháng hoặc năm không hợp lệ" });
            }

            // Logic: Calculate salary for all employees based on ChamCong
            var nhanViens = await _context.NhanViens.ToListAsync();
            int count = 0;

            foreach (var nv in nhanViens)
            {
                // Check if salary already exists for this month
                var existing = await _context.BangLuongs
                    .FirstOrDefaultAsync(b => b.IdNhanVien == nv.Id && b.Thang == request.Thang && b.Nam == request.Nam);

                if (existing != null) continue; // Skip if already calculated

                // Get ChamCong
                var chamCongs = await _context.ChamCongs
                    .Where(c => c.IdNhanVien == nv.Id && 
                                c.Ngay.Month == request.Thang && 
                                c.Ngay.Year == request.Nam)
                    .ToListAsync();

                // Simple calculation logic
                int soNgayCong = chamCongs.Count; // Assuming 1 record = 1 day or check HinhThuc
                decimal luongCoBan = nv.LuongCoBan ?? 0;
                decimal thuong = 0; // Logic for bonus can be added
                decimal phuCap = 0; // Logic for allowance
                decimal khauTru = 0; // Logic for deduction

                // Formula: (LuongCoBan / 26) * SoNgayCong
                decimal tongLuong = (luongCoBan / 26) * soNgayCong + thuong + phuCap - khauTru;

                var bangLuong = new BangLuong
                {
                    IdNhanVien = nv.Id,
                    Thang = request.Thang,
                    Nam = request.Nam,
                    LuongCoBan = luongCoBan,
                    SoNgayCong = soNgayCong,
                    Thuong = thuong,
                    PhuCap = phuCap,
                    KhauTru = khauTru,
                    TongLuong = tongLuong,
                    NgayTinhLuong = DateTime.Now
                };

                _context.BangLuongs.Add(bangLuong);
                count++;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                statusCode = 200,
                message = $"Đã tính lương thành công cho {count} nhân viên",
                data = new { processedCount = count }
            });
        }

        [HttpGet("my-salary")]
        public async Task<IActionResult> GetMySalary()
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            // Check if user is an employee
            var nhanVien = await _context.NhanViens.FirstOrDefaultAsync(n => n.IdTaiKhoan == userId);
            if (nhanVien == null)
            {
                return Ok(new { statusCode = 400, message = "Tài khoản không liên kết với nhân viên nào" });
            }

            // Get salary history
            var salaries = await _context.BangLuongs
                .Where(b => b.IdNhanVien == nhanVien.Id)
                .OrderByDescending(b => b.Nam).ThenByDescending(b => b.Thang)
                .Select(b => new 
                {
                    b.Id,
                    b.Thang,
                    b.Nam,
                    b.LuongCoBan,
                    b.SoNgayCong,
                    b.Thuong,
                    b.PhuCap,
                    b.KhauTru,
                    b.TongLuong,
                    b.NgayTinhLuong
                })
                .ToListAsync();

            return Ok(new
            {
                statusCode = 200,
                message = "Lấy dữ liệu lương thành công",
                data = salaries
            });
        }

        [HttpGet("get-all-salary")]
        public async Task<IActionResult> GetAllSalary()
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            long roleId = await GetRoleId(userId);
            string roleName = await GetRoleName(userId);

            // Check view permission
            if (!await CheckPermission(roleId, "Xuat"))
            {
                return Unauthorized(new { statusCode = 401, message = "Không có quyền xem danh sách lương" });
            }

            // Logic: Admin/HR sees all, Employee sees self
            bool isAdminOrHr = roleName.Contains("Admin", StringComparison.OrdinalIgnoreCase) ||
                               roleName.Contains("Nhân sự", StringComparison.OrdinalIgnoreCase);

            if (isAdminOrHr)
            {
                var today = DateTime.Today;

                var salaries = await _context.BangLuongs
                    .Include(b => b.IdNhanVienNavigation)
                    .OrderByDescending(b => b.Nam).ThenByDescending(b => b.Thang)
                    .Select(b => new
                    {
                        b.Id,
                        b.Thang,
                        b.Nam,
                        b.LuongCoBan,
                        b.SoNgayCong,
                        b.Thuong,
                        b.PhuCap,
                        b.KhauTru,
                        b.TongLuong,
                        b.NgayTinhLuong,
                        NhanVien = b.IdNhanVienNavigation != null ? b.IdNhanVienNavigation.HoTen : "Unknown"
                    })
                    .ToListAsync();

                return Ok(new
                {
                    statusCode = 200,
                    message = "Lấy toàn bộ danh sách lương thành công",
                    data = salaries
                });
            }
            else
            {
                return Unauthorized(new { statusCode = 403, message = "Chỉ quản lý mới có quyền xem toàn bộ lương" });
            }
        }

        [HttpGet("export-excel")]
        public async Task<IActionResult> ExportExcel(int? thang = null, int? nam = null)
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            long roleId = await GetRoleId(userId);
            string roleName = await GetRoleName(userId);

            if (!await CheckPermission(roleId, "Xuat"))
            {
                return Unauthorized(new { statusCode = 401, message = "Không có quyền xuất Excel" });
            }

            bool isAdminOrHr = roleName.Contains("Admin", StringComparison.OrdinalIgnoreCase) ||
                               roleName.Contains("Nhân sự", StringComparison.OrdinalIgnoreCase);

            if (!isAdminOrHr)
            {
                return Unauthorized(new { statusCode = 403, message = "Chỉ quản lý mới có quyền xuất Excel" });
            }

            var query = _context.BangLuongs
                .Include(b => b.IdNhanVienNavigation)
                .AsQueryable();

            if (thang.HasValue) query = query.Where(b => b.Thang == thang.Value);
            if (nam.HasValue) query = query.Where(b => b.Nam == nam.Value);

            var salaries = await query
                .OrderByDescending(b => b.Nam).ThenByDescending(b => b.Thang)
                .ToListAsync();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Bảng Lương");

                // Header
                worksheet.Cells[1, 1].Value = "STT";
                worksheet.Cells[1, 2].Value = "Nhân Viên";
                worksheet.Cells[1, 3].Value = "Tháng";
                worksheet.Cells[1, 4].Value = "Năm";
                worksheet.Cells[1, 5].Value = "Lương Cơ Bản";
                worksheet.Cells[1, 6].Value = "Số Ngày Công";
                worksheet.Cells[1, 7].Value = "Thưởng";
                worksheet.Cells[1, 8].Value = "Phụ Cấp";
                worksheet.Cells[1, 9].Value = "Khấu Trừ";
                worksheet.Cells[1, 10].Value = "Tổng Lương";
                worksheet.Cells[1, 11].Value = "Ngày Tính Lương";

                // Style header
                var headerRange = worksheet.Cells[1, 1, 1, 11];
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(59, 130, 246));
                headerRange.Style.Font.Color.SetColor(Color.White);
                headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Data
                int row = 2;
                int stt = 1;
                foreach (var salary in salaries)
                {
                    worksheet.Cells[row, 1].Value = stt++;
                    worksheet.Cells[row, 2].Value = salary.IdNhanVienNavigation?.HoTen ?? "Unknown";
                    worksheet.Cells[row, 3].Value = salary.Thang;
                    worksheet.Cells[row, 4].Value = salary.Nam;
                    worksheet.Cells[row, 5].Value = salary.LuongCoBan ?? 0;
                    worksheet.Cells[row, 6].Value = salary.SoNgayCong ?? 0;
                    worksheet.Cells[row, 7].Value = salary.Thuong ?? 0;
                    worksheet.Cells[row, 8].Value = salary.PhuCap ?? 0;
                    worksheet.Cells[row, 9].Value = salary.KhauTru ?? 0;
                    worksheet.Cells[row, 10].Value = salary.TongLuong ?? 0;
                    worksheet.Cells[row, 11].Value = salary.NgayTinhLuong?.ToString("dd/MM/yyyy") ?? "";

                    // Format số tiền
                    worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[row, 7].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[row, 8].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[row, 9].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[row, 10].Style.Numberformat.Format = "#,##0";

                    row++;
                }

                // Auto fit columns
                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"BangLuong_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        [HttpGet("export-pdf")]
        public async Task<IActionResult> ExportPdf(int? thang = null, int? nam = null)
        {
            //long userId = GetCurrentUserId();
            long userId = 2;
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            var nhanVien = await _context.NhanViens
                .Include(n => n.IdPhongBanNavigation)
                .Include(n => n.IdTaiKhoanNavigation)
                .FirstOrDefaultAsync(n => n.Id == userId);

            if (nhanVien == null)
            {
                return Ok(new { statusCode = 404, message = "Không tìm thấy nhân viên" });
            }

            var queryPdf = _context.BangLuongs
                .Where(b => b.IdNhanVien == userId)
                .AsQueryable();

            if (thang.HasValue) queryPdf = queryPdf.Where(b => b.Thang == thang.Value);
            if (nam.HasValue) queryPdf = queryPdf.Where(b => b.Nam == nam.Value);

            var salaries = await queryPdf
                .OrderByDescending(b => b.Nam).ThenByDescending(b => b.Thang)
                .ToListAsync();

            QuestPDF.Settings.License = LicenseType.Community;

            const float Cm = 28.3465f; // 1 cm in points

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2 * Cm);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                    page.Header()
                        .Column(col =>
                        {
                            col.Item()
                                .AlignCenter()
                                .Text("BẢNG LƯƠNG NHÂN VIÊN")
                                .FontFamily("Arial")
                                .SemiBold().FontSize(16);
                        });

                    page.Content()
                        .PaddingVertical(1 * Cm)
                        .Column(column =>
                        {
                            column.Spacing(0.5f * Cm);

                            // Thông tin nhân viên
                            column.Item().Background(Colors.Grey.Lighten3).Padding(10).Column(info =>
                            {
                                info.Item().Text($"Họ tên: {nhanVien.HoTen}").FontFamily("Arial").SemiBold();
                                info.Item().Text($"Phòng ban: {nhanVien.IdPhongBanNavigation?.TenPhongBan ?? "N/A"}").FontFamily("Arial");
                                info.Item().Text($"Chức vụ: {nhanVien.ChucVu ?? "N/A"}").FontFamily("Arial");
                                info.Item().Text($"Email: {nhanVien.IdTaiKhoanNavigation?.Email ?? "N/A"}").FontFamily("Arial");
                            });

                            // Bảng lương
                            if (salaries.Any())
                            {
                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn(2);
                                    });

                                    // Header
                                    table.Header(header =>
                                    {
                                        header.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Tháng/Năm").FontFamily("Arial").Bold().FontColor(Colors.White);
                                        header.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Ngày công").FontFamily("Arial").Bold().FontColor(Colors.White);
                                        header.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Lương cơ bản").FontFamily("Arial").Bold().FontColor(Colors.White);
                                        header.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Tổng lương").FontFamily("Arial").Bold().FontColor(Colors.White);
                                    });

                                    // Rows
                                    foreach (var salary in salaries)
                                    {
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{salary.Thang}/{salary.Nam}").FontFamily("Arial");
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{salary.SoNgayCong ?? 0}").FontFamily("Arial");
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{salary.LuongCoBan ?? 0:N0} VNĐ").FontFamily("Arial");
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{salary.TongLuong ?? 0:N0} VNĐ").FontFamily("Arial").SemiBold();
                                    }
                                });
                            }
                            else
                            {
                                column.Item().Text("Chưa có dữ liệu lương").FontFamily("Arial");
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Ngày xuất: ").FontFamily("Arial");
                            x.Span($"{DateTime.Now:dd/MM/yyyy HH:mm}").FontFamily("Arial").SemiBold();
                        });
                });
            });

            var stream = new MemoryStream();
            document.GeneratePdf(stream);
            stream.Position = 0;

            var safeName = (nhanVien.HoTen ?? "NhanVien").Replace(" ", "_");
            var fileName = $"BangLuong_{safeName}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(stream.ToArray(), "application/pdf", fileName);
        }

        [HttpPost("import-excel")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportExcel([FromForm] IFormFile file)
        {
            long userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { statusCode = 401, message = "Chưa đăng nhập" });

            long roleId = await GetRoleId(userId);
            if (!await CheckPermission(roleId, "Them"))
            {
                return Unauthorized(new { statusCode = 401, message = "Không có quyền thực hiện chức năng này" });
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { statusCode = 400, message = "Vui lòng chọn file Excel" });
            }

            // Check file extension
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".xlsx" && extension != ".xls")
            {
                return BadRequest(new { statusCode = 400, message = "File phải là định dạng Excel (.xlsx hoặc .xls)" });
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            int successCount = 0;
            int errorCount = 0;
            var errors = new List<string>();

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        if (worksheet == null)
                        {
                            return BadRequest(new { statusCode = 400, message = "File Excel không hợp lệ hoặc không có dữ liệu" });
                        }

                        // Get all employees for lookup
                        var nhanViens = await _context.NhanViens.ToListAsync();
                        var nhanVienDict = nhanViens.ToDictionary(nv => nv.HoTen?.Trim().ToLower() ?? "", nv => nv);

                        // Try to detect header row (look for "Nhân viên" or "Tên" or "Employee" in first row)
                        int startRow = 2; // Default start from row 2
                        var firstRowValue = worksheet.Cells[1, 2]?.Value?.ToString()?.ToLower() ?? "";
                        if (firstRowValue.Contains("nhân viên") || firstRowValue.Contains("tên") || firstRowValue.Contains("employee") || firstRowValue.Contains("stt"))
                        {
                            startRow = 2; // Row 1 is header
                        }
                        else
                        {
                            startRow = 1; // No header, start from row 1
                        }
                        
                        int maxRow = worksheet.Dimension?.End.Row ?? 0;

                        for (int row = startRow; row <= maxRow; row++)
                        {
                            try
                            {
                                // Read data from Excel
                                // Try to detect column structure: check if first column is STT (number)
                                int colOffset = 0;
                                var firstCell = worksheet.Cells[row, 1]?.Value?.ToString()?.Trim();
                                if (row > startRow && !string.IsNullOrWhiteSpace(firstCell) && int.TryParse(firstCell, out _))
                                {
                                    colOffset = 1; // First column is STT, offset by 1
                                }
                                
                                var nhanVienName = worksheet.Cells[row, 1 + colOffset]?.Value?.ToString()?.Trim();
                                var thangStr = worksheet.Cells[row, 2 + colOffset]?.Value?.ToString()?.Trim();
                                var namStr = worksheet.Cells[row, 3 + colOffset]?.Value?.ToString()?.Trim();
                                var luongCoBanStr = worksheet.Cells[row, 4 + colOffset]?.Value?.ToString()?.Trim();
                                var soNgayCongStr = worksheet.Cells[row, 5 + colOffset]?.Value?.ToString()?.Trim();
                                var thuongStr = worksheet.Cells[row, 6 + colOffset]?.Value?.ToString()?.Trim();
                                var phuCapStr = worksheet.Cells[row, 7 + colOffset]?.Value?.ToString()?.Trim();
                                var khauTruStr = worksheet.Cells[row, 8 + colOffset]?.Value?.ToString()?.Trim();
                                var tongLuongStr = worksheet.Cells[row, 9 + colOffset]?.Value?.ToString()?.Trim();
                                
                                // Fallback: Try original format (with STT column) if name is empty
                                if (string.IsNullOrWhiteSpace(nhanVienName))
                                {
                                    nhanVienName = worksheet.Cells[row, 2]?.Value?.ToString()?.Trim();
                                    thangStr = worksheet.Cells[row, 3]?.Value?.ToString()?.Trim();
                                    namStr = worksheet.Cells[row, 4]?.Value?.ToString()?.Trim();
                                    luongCoBanStr = worksheet.Cells[row, 5]?.Value?.ToString()?.Trim();
                                    soNgayCongStr = worksheet.Cells[row, 6]?.Value?.ToString()?.Trim();
                                    thuongStr = worksheet.Cells[row, 7]?.Value?.ToString()?.Trim();
                                    phuCapStr = worksheet.Cells[row, 8]?.Value?.ToString()?.Trim();
                                    khauTruStr = worksheet.Cells[row, 9]?.Value?.ToString()?.Trim();
                                    tongLuongStr = worksheet.Cells[row, 10]?.Value?.ToString()?.Trim();
                                }

                                // Skip empty rows
                                if (string.IsNullOrWhiteSpace(nhanVienName) && string.IsNullOrWhiteSpace(thangStr))
                                {
                                    continue;
                                }

                                // Validate required fields
                                if (string.IsNullOrWhiteSpace(nhanVienName))
                                {
                                    errors.Add($"Dòng {row}: Thiếu tên nhân viên");
                                    errorCount++;
                                    continue;
                                }

                                // Find employee
                                var nhanVienKey = nhanVienName.ToLower();
                                if (!nhanVienDict.ContainsKey(nhanVienKey))
                                {
                                    errors.Add($"Dòng {row}: Không tìm thấy nhân viên '{nhanVienName}'");
                                    errorCount++;
                                    continue;
                                }

                                var nhanVien = nhanVienDict[nhanVienKey];

                                // Parse month and year
                                if (!int.TryParse(thangStr, out int thang) || thang < 1 || thang > 12)
                                {
                                    errors.Add($"Dòng {row}: Tháng không hợp lệ ({thangStr})");
                                    errorCount++;
                                    continue;
                                }

                                if (!int.TryParse(namStr, out int nam) || nam < 2000 || nam > 2100)
                                {
                                    errors.Add($"Dòng {row}: Năm không hợp lệ ({namStr})");
                                    errorCount++;
                                    continue;
                                }

                                // Check if salary already exists
                                var existing = await _context.BangLuongs
                                    .FirstOrDefaultAsync(b => b.IdNhanVien == nhanVien.Id && b.Thang == thang && b.Nam == nam);

                                if (existing != null)
                                {
                                    // Update existing record
                                    existing.LuongCoBan = ParseDecimal(luongCoBanStr) ?? nhanVien.LuongCoBan ?? 0;
                                    existing.SoNgayCong = ParseInt(soNgayCongStr);
                                    existing.Thuong = ParseDecimal(thuongStr) ?? 0;
                                    existing.PhuCap = ParseDecimal(phuCapStr) ?? 0;
                                    existing.KhauTru = ParseDecimal(khauTruStr) ?? 0;
                                    existing.TongLuong = ParseDecimal(tongLuongStr) ?? 
                                        (existing.LuongCoBan ?? 0) + (existing.Thuong ?? 0) + (existing.PhuCap ?? 0) - (existing.KhauTru ?? 0);
                                    existing.NgayTinhLuong = DateTime.Now;
                                    successCount++;
                                }
                                else
                                {
                                    // Create new record
                                    var luongCoBan = ParseDecimal(luongCoBanStr) ?? nhanVien.LuongCoBan ?? 0;
                                    var soNgayCong = ParseInt(soNgayCongStr);
                                    var thuong = ParseDecimal(thuongStr) ?? 0;
                                    var phuCap = ParseDecimal(phuCapStr) ?? 0;
                                    var khauTru = ParseDecimal(khauTruStr) ?? 0;
                                    var tongLuong = ParseDecimal(tongLuongStr) ?? (luongCoBan + thuong + phuCap - khauTru);

                                    var bangLuong = new BangLuong
                                    {
                                        IdNhanVien = nhanVien.Id,
                                        Thang = thang,
                                        Nam = nam,
                                        LuongCoBan = luongCoBan,
                                        SoNgayCong = soNgayCong,
                                        Thuong = thuong,
                                        PhuCap = phuCap,
                                        KhauTru = khauTru,
                                        TongLuong = tongLuong,
                                        NgayTinhLuong = DateTime.Now
                                    };

                                    _context.BangLuongs.Add(bangLuong);
                                    successCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Dòng {row}: Lỗi xử lý - {ex.Message}");
                                errorCount++;
                            }
                        }

                        await _context.SaveChangesAsync();
                    }
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = $"Import thành công {successCount} bản ghi. Lỗi: {errorCount}",
                    data = new
                    {
                        successCount,
                        errorCount,
                        errors = errors.Take(20).ToList() // Limit to first 20 errors
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = $"Lỗi khi xử lý file Excel: {ex.Message}",
                    data = new { successCount, errorCount, errors }
                });
            }
        }

        private decimal? ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (decimal.TryParse(value.Replace(",", "").Replace(".", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator), out decimal result))
            {
                return result;
            }
            return null;
        }

        private int? ParseInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (int.TryParse(value, out int result))
            {
                return result;
            }
            return null;
        }

        public class CalculateRequest
        {
            public int Thang { get; set; }
            public int Nam { get; set; }
        }
    }
}
