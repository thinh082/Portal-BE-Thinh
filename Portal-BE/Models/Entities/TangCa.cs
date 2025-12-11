using System;
using System.Collections.Generic;

namespace Portal_BE.Models.Entities;

public partial class TangCa
{
    public int Id { get; set; }

    public long? IdNhanVien { get; set; }

    public TimeOnly? GioBatDau { get; set; }

    public TimeOnly? GioKetThuc { get; set; }

    public double? SoGioLam { get; set; }

    public double? HeSo { get; set; }

    public string? LyDoTangCa { get; set; }

    public string? TrangThai { get; set; }

    public DateTime? NgayTangCa { get; set; }

    public virtual NhanVien? IdNhanVienNavigation { get; set; }
}
