using System;
using System.Collections.Generic;

namespace Portal_BE.Models.Entities;

public partial class CongViec
{
    public long Id { get; set; }

    public string TieuDe { get; set; } = null!;

    public string? MoTa { get; set; }

    public DateOnly? NgayBatDau { get; set; }

    public DateOnly? HanHoanThanh { get; set; }

    public string? TrangThai { get; set; }

    public long? IdNguoiGiao { get; set; }

    public long? IdNguoiNhan { get; set; }

    public long? IdDuAn { get; set; }

    public virtual DuAn? IdDuAnNavigation { get; set; }

    public virtual NhanVien? IdNguoiGiaoNavigation { get; set; }

    public virtual NhanVien? IdNguoiNhanNavigation { get; set; }
}
