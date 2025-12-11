using System;
using System.Collections.Generic;

namespace Portal_BE.Models.Entities;

public partial class UngVien
{
    public int Id { get; set; }

    public string HoTen { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? SoDienThoai { get; set; }

    public string? ViTriUngTuyen { get; set; }

    public string? DuongDanCv { get; set; }

    public string? LinkPortfolio { get; set; }

    public int? TrangThaiHienTai { get; set; }

    public DateTime? NgayNopHoSo { get; set; }

    public virtual ICollection<LichSuDanhGium> LichSuDanhGia { get; set; } = new List<LichSuDanhGium>();
}
