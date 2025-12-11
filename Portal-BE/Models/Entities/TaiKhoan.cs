using System;
using System.Collections.Generic;

namespace Portal_BE.Models.Entities;

public partial class TaiKhoan
{
    public long Id { get; set; }

    public string TenDangNhap { get; set; } = null!;

    public string MatKhau { get; set; } = null!;

    public string? Email { get; set; }

    public string? SoDienThoai { get; set; }

    public long IdVaiTro { get; set; }

    public bool? TrangThai { get; set; }

    public DateTime? NgayTao { get; set; }

    public int? SoNamKinhNghiem { get; set; }

    public string? MoTaKyNang { get; set; }

    public virtual VaiTro IdVaiTroNavigation { get; set; } = null!;

    public virtual NhanVien? NhanVien { get; set; }
}
