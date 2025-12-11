using System;
using System.Collections.Generic;

namespace Portal_BE.Models.Entities;

public partial class NghiPhep
{
    public long Id { get; set; }

    public long IdNhanVien { get; set; }

    public DateOnly NgayBatDau { get; set; }

    public DateOnly NgayKetThuc { get; set; }

    public string? LyDo { get; set; }

    public string? TrangThai { get; set; }

    public long? IdNguoiDuyet { get; set; }

    public DateTime? NgayDuyet { get; set; }

    public virtual NhanVien? IdNguoiDuyetNavigation { get; set; }

    public virtual NhanVien IdNhanVienNavigation { get; set; } = null!;
}
