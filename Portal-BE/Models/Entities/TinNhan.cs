using System;
using System.Collections.Generic;

namespace Portal_BE.Models.Entities;

public partial class TinNhan
{
    public long Id { get; set; }

    public int IdCuocTroChuyen { get; set; }

    public long IdNguoiGui { get; set; }

    public string? NoiDung { get; set; }

    public DateTime? ThoiGianGui { get; set; }

    public bool? DaXem { get; set; }

    public virtual CuocTroChuyen IdCuocTroChuyenNavigation { get; set; } = null!;

    public virtual NhanVien IdNguoiGuiNavigation { get; set; } = null!;
}
