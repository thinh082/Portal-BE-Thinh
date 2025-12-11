using System;
using System.Collections.Generic;

namespace Portal_BE.Models.Entities;

public partial class LichSuDanhGium
{
    public int Id { get; set; }

    public int IdUngVien { get; set; }

    public int? MaNguoiDanhGia { get; set; }

    public string? VongPhongVan { get; set; }

    public string? NhanXetChuyenMon { get; set; }

    public double? DiemSo { get; set; }

    public bool? KetQua { get; set; }

    public DateTime? NgayDanhGia { get; set; }

    public virtual UngVien IdUngVienNavigation { get; set; } = null!;
}
