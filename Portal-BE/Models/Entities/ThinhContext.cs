using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Portal_BE.Models.Entities;

public partial class ThinhContext : DbContext
{
    public ThinhContext()
    {
    }

    public ThinhContext(DbContextOptions<ThinhContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BangLuong> BangLuongs { get; set; }

    public virtual DbSet<ChamCong> ChamCongs { get; set; }

    public virtual DbSet<ChucNang> ChucNangs { get; set; }

    public virtual DbSet<CongViec> CongViecs { get; set; }

    public virtual DbSet<CuocTroChuyen> CuocTroChuyens { get; set; }

    public virtual DbSet<DuAn> DuAns { get; set; }

    public virtual DbSet<LichSuDanhGium> LichSuDanhGia { get; set; }

    public virtual DbSet<NghiPhep> NghiPheps { get; set; }

    public virtual DbSet<NhanVien> NhanViens { get; set; }

    public virtual DbSet<PhongBan> PhongBans { get; set; }

    public virtual DbSet<Quyen> Quyens { get; set; }

    public virtual DbSet<TaiKhoan> TaiKhoans { get; set; }

    public virtual DbSet<TangCa> TangCas { get; set; }

    public virtual DbSet<TinNhan> TinNhans { get; set; }

    public virtual DbSet<UngVien> UngViens { get; set; }

    public virtual DbSet<VaiTro> VaiTros { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:Connection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BangLuong>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__BangLuon__3214EC075C382735");

            entity.ToTable("BangLuong");

            entity.Property(e => e.Bhxh).HasColumnName("BHXH");
            entity.Property(e => e.KhauTru).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.LuongCoBan).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.NgayTinhLuong)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PhuCap).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TamUng).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ThueTncn).HasColumnName("ThueTNCN");
            entity.Property(e => e.Thuong).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TongLuong).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.IdNhanVienNavigation).WithMany(p => p.BangLuongs)
                .HasForeignKey(d => d.IdNhanVien)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BangLuong__IdNha__45F365D3");
        });

        modelBuilder.Entity<ChamCong>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ChamCong__3214EC0727C9D124");

            entity.ToTable("ChamCong");

            entity.Property(e => e.GhiChu).HasMaxLength(255);
            entity.Property(e => e.HinhThuc).HasMaxLength(50);
            entity.Property(e => e.ViTri).HasMaxLength(255);

            entity.HasOne(d => d.IdNhanVienNavigation).WithMany(p => p.ChamCongs)
                .HasForeignKey(d => d.IdNhanVien)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ChamCong__IdNhan__3D5E1FD2");
        });

        modelBuilder.Entity<ChucNang>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ChucNang__3214EC07894679F5");

            entity.ToTable("ChucNang");

            entity.Property(e => e.MoTa).HasMaxLength(255);
            entity.Property(e => e.TenChucNang).HasMaxLength(100);
        });

        modelBuilder.Entity<CongViec>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CongViec__3214EC073D62B934");

            entity.ToTable("CongViec");

            entity.Property(e => e.TieuDe).HasMaxLength(200);
            entity.Property(e => e.TrangThai).HasMaxLength(50);

            entity.HasOne(d => d.IdDuAnNavigation).WithMany(p => p.CongViecs)
                .HasForeignKey(d => d.IdDuAn)
                .HasConstraintName("CongViec_DuAn");

            entity.HasOne(d => d.IdNguoiGiaoNavigation).WithMany(p => p.CongViecIdNguoiGiaoNavigations)
                .HasForeignKey(d => d.IdNguoiGiao)
                .HasConstraintName("FK__CongViec__IdNguo__48CFD27E");

            entity.HasOne(d => d.IdNguoiNhanNavigation).WithMany(p => p.CongViecIdNguoiNhanNavigations)
                .HasForeignKey(d => d.IdNguoiNhan)
                .HasConstraintName("FK__CongViec__IdNguo__49C3F6B7");
        });

        modelBuilder.Entity<CuocTroChuyen>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CuocTroC__3214EC07FDDA0CB9");

            entity.ToTable("CuocTroChuyen");

            entity.Property(e => e.LaNhom).HasDefaultValue(false);
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TenCuocTroChuyen).HasMaxLength(100);
        });

        modelBuilder.Entity<DuAn>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DuAn__3214EC07328985EC");

            entity.ToTable("DuAn");

            entity.Property(e => e.NgayBatDau).HasColumnType("datetime");
            entity.Property(e => e.NgayKetThuc).HasColumnType("datetime");
            entity.Property(e => e.TenDuAn).HasMaxLength(255);
            entity.Property(e => e.TrangThai).HasMaxLength(100);
        });

        modelBuilder.Entity<LichSuDanhGium>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LichSuDa__3214EC07F23CC387");

            entity.Property(e => e.KetQua).HasDefaultValue(false);
            entity.Property(e => e.NgayDanhGia)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.VongPhongVan).HasMaxLength(50);

            entity.HasOne(d => d.IdUngVienNavigation).WithMany(p => p.LichSuDanhGia)
                .HasForeignKey(d => d.IdUngVien)
                .HasConstraintName("FK_LichSu_UngVien");
        });

        modelBuilder.Entity<NghiPhep>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__NghiPhep__3214EC0782BEF057");

            entity.ToTable("NghiPhep");

            entity.Property(e => e.LyDo).HasMaxLength(255);
            entity.Property(e => e.NgayDuyet).HasColumnType("datetime");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasDefaultValue("Chờ duyệt");

            entity.HasOne(d => d.IdNguoiDuyetNavigation).WithMany(p => p.NghiPhepIdNguoiDuyetNavigations)
                .HasForeignKey(d => d.IdNguoiDuyet)
                .HasConstraintName("FK__NghiPhep__IdNguo__4222D4EF");

            entity.HasOne(d => d.IdNhanVienNavigation).WithMany(p => p.NghiPhepIdNhanVienNavigations)
                .HasForeignKey(d => d.IdNhanVien)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NghiPhep__IdNhan__412EB0B6");
        });

        modelBuilder.Entity<NhanVien>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__NhanVien__3214EC07D71DC3A9");

            entity.ToTable("NhanVien");

            entity.HasIndex(e => e.IdTaiKhoan, "UQ__NhanVien__9A53D3DC3765C1CC").IsUnique();

            entity.Property(e => e.Cccd)
                .HasMaxLength(20)
                .HasColumnName("CCCD");
            entity.Property(e => e.ChucVu).HasMaxLength(100);
            entity.Property(e => e.DiaChi).HasMaxLength(255);
            entity.Property(e => e.GioiTinh).HasMaxLength(10);
            entity.Property(e => e.HoTen).HasMaxLength(150);
            entity.Property(e => e.LuongCoBan).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.IdPhongBanNavigation).WithMany(p => p.NhanViens)
                .HasForeignKey(d => d.IdPhongBan)
                .HasConstraintName("FK__NhanVien__IdPhon__398D8EEE");

            entity.HasOne(d => d.IdTaiKhoanNavigation).WithOne(p => p.NhanVien)
                .HasForeignKey<NhanVien>(d => d.IdTaiKhoan)
                .HasConstraintName("FK__NhanVien__IdTaiK__3A81B327");
        });

        modelBuilder.Entity<PhongBan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PhongBan__3214EC07AE018552");

            entity.ToTable("PhongBan");

            entity.Property(e => e.MoTa).HasMaxLength(255);
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TenPhongBan).HasMaxLength(150);
        });

        modelBuilder.Entity<Quyen>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Quyen__3214EC07AEC62D8D");

            entity.ToTable("Quyen");

            entity.Property(e => e.Sua).HasDefaultValue(false);
            entity.Property(e => e.Them).HasDefaultValue(false);
            entity.Property(e => e.Xoa).HasDefaultValue(false);
            entity.Property(e => e.Xuat).HasDefaultValue(false);

            entity.HasOne(d => d.IdChucNangNavigation).WithMany(p => p.Quyens)
                .HasForeignKey(d => d.IdChucNang)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Quyen__IdChucNan__2D27B809");

            entity.HasOne(d => d.IdVaiTroNavigation).WithMany(p => p.Quyens)
                .HasForeignKey(d => d.IdVaiTro)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Quyen__IdVaiTro__2C3393D0");
        });

        modelBuilder.Entity<TaiKhoan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TaiKhoan__3214EC073766AB31");

            entity.ToTable("TaiKhoan");

            entity.HasIndex(e => e.TenDangNhap, "UQ__TaiKhoan__55F68FC002495097").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.MatKhau).HasMaxLength(255);
            entity.Property(e => e.MoTaKyNang).HasMaxLength(1000);
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SoDienThoai).HasMaxLength(20);
            entity.Property(e => e.TenDangNhap).HasMaxLength(100);
            entity.Property(e => e.TrangThai).HasDefaultValue(true);

            entity.HasOne(d => d.IdVaiTroNavigation).WithMany(p => p.TaiKhoans)
                .HasForeignKey(d => d.IdVaiTro)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TaiKhoan__IdVaiT__32E0915F");
        });

        modelBuilder.Entity<TangCa>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TangCa__3214EC07FAFA62FF");

            entity.ToTable("TangCa");

            entity.Property(e => e.GioBatDau).HasMaxLength(10);
            entity.Property(e => e.GioKetThuc).HasMaxLength(10);
            entity.Property(e => e.LyDoTangCa).HasMaxLength(255);
            entity.Property(e => e.NgayTangCa).HasColumnType("datetime");
            entity.Property(e => e.SoGioLam).HasColumnName("soGioLam");
            entity.Property(e => e.TrangThai).HasMaxLength(50);

            entity.HasOne(d => d.IdNhanVienNavigation).WithMany(p => p.TangCas)
                .HasForeignKey(d => d.IdNhanVien)
                .HasConstraintName("FK_TangCa_NhanVien");
        });

        modelBuilder.Entity<TinNhan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TinNhan__3214EC0734C97582");

            entity.ToTable("TinNhan");

            entity.Property(e => e.DaXem).HasDefaultValue(false);
            entity.Property(e => e.ThoiGianGui)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.IdCuocTroChuyenNavigation).WithMany(p => p.TinNhans)
                .HasForeignKey(d => d.IdCuocTroChuyen)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TinNhan_CuocTroChuyen");

            entity.HasOne(d => d.IdNguoiGuiNavigation).WithMany(p => p.TinNhans)
                .HasForeignKey(d => d.IdNguoiGui)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TinNhan_NhanVien");
        });

        modelBuilder.Entity<UngVien>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UngVien__3214EC07AED92548");

            entity.ToTable("UngVien");

            entity.Property(e => e.DuongDanCv)
                .HasMaxLength(255)
                .HasColumnName("DuongDanCV");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.LinkPortfolio)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.NgayNopHoSo)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TrangThaiHienTai).HasDefaultValue(0);
            entity.Property(e => e.ViTriUngTuyen).HasMaxLength(50);
        });

        modelBuilder.Entity<VaiTro>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__VaiTro__3214EC07CBDCBDAA");

            entity.ToTable("VaiTro");

            entity.Property(e => e.TenVaiTro).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
