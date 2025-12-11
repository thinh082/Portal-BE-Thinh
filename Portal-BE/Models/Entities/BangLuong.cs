using System;
using System.Collections.Generic;

namespace Portal_BE.Models.Entities;

public partial class BangLuong
{
    public long Id { get; set; }

    public long IdNhanVien { get; set; }

    public int Thang { get; set; }

    public int Nam { get; set; }

    public decimal? LuongCoBan { get; set; }

    public int? SoNgayCong { get; set; }

    public decimal? Thuong { get; set; }

    public decimal? PhuCap { get; set; }

    public decimal? KhauTru { get; set; }

    public decimal? TongLuong { get; set; }

    public DateTime? NgayTinhLuong { get; set; }

    public double? Bhxh { get; set; }

    public double? ThueTncn { get; set; }

    public decimal? TamUng { get; set; }

    public virtual NhanVien IdNhanVienNavigation { get; set; } = null!;
}
