using System;
using System.Collections.Generic;

namespace Portal_BE.Models.Entities;

public partial class VaiTro
{
    public long Id { get; set; }

    public string TenVaiTro { get; set; } = null!;

    public virtual ICollection<Quyen> Quyens { get; set; } = new List<Quyen>();

    public virtual ICollection<TaiKhoan> TaiKhoans { get; set; } = new List<TaiKhoan>();
}
