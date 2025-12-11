using System;
using System.Collections.Generic;

namespace Portal_BE.Models.Entities;

public partial class ChamCong
{
    public long Id { get; set; }

    public long IdNhanVien { get; set; }

    public DateOnly Ngay { get; set; }

    public TimeOnly? GioVao { get; set; }

    public TimeOnly? GioRa { get; set; }

    public string? GhiChu { get; set; }

    public string? HinhThuc { get; set; }

    public string? ViTri { get; set; }

    public virtual NhanVien IdNhanVienNavigation { get; set; } = null!;
}
