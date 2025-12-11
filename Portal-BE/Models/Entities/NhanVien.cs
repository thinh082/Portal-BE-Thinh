using System;
using System.Collections.Generic;

namespace Portal_BE.Models.Entities;

public partial class NhanVien
{
    public long Id { get; set; }

    public string HoTen { get; set; } = null!;

    public DateOnly? NgaySinh { get; set; }

    public string? GioiTinh { get; set; }

    public string? DiaChi { get; set; }

    public string? Cccd { get; set; }

    public DateOnly? NgayVaoLam { get; set; }

    public string? ChucVu { get; set; }

    public decimal? LuongCoBan { get; set; }

    public long? IdPhongBan { get; set; }

    public long? IdTaiKhoan { get; set; }

    public virtual ICollection<BangLuong> BangLuongs { get; set; } = new List<BangLuong>();

    public virtual ICollection<ChamCong> ChamCongs { get; set; } = new List<ChamCong>();

    public virtual ICollection<CongViec> CongViecIdNguoiGiaoNavigations { get; set; } = new List<CongViec>();

    public virtual ICollection<CongViec> CongViecIdNguoiNhanNavigations { get; set; } = new List<CongViec>();

    public virtual PhongBan? IdPhongBanNavigation { get; set; }

    public virtual TaiKhoan? IdTaiKhoanNavigation { get; set; }

    public virtual ICollection<NghiPhep> NghiPhepIdNguoiDuyetNavigations { get; set; } = new List<NghiPhep>();

    public virtual ICollection<NghiPhep> NghiPhepIdNhanVienNavigations { get; set; } = new List<NghiPhep>();

    public virtual ICollection<TangCa> TangCas { get; set; } = new List<TangCa>();

    public virtual ICollection<TinNhan> TinNhans { get; set; } = new List<TinNhan>();
}
