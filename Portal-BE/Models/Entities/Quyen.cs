using System;
using System.Collections.Generic;

namespace Portal_BE.Models.Entities;

public partial class Quyen
{
    public long Id { get; set; }

    public long IdVaiTro { get; set; }

    public long IdChucNang { get; set; }

    public bool? Them { get; set; }

    public bool? Xoa { get; set; }

    public bool? Sua { get; set; }

    public bool? Xuat { get; set; }

    public virtual ChucNang IdChucNangNavigation { get; set; } = null!;

    public virtual VaiTro IdVaiTroNavigation { get; set; } = null!;
}
