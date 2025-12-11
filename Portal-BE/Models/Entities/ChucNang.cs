using System;
using System.Collections.Generic;

namespace Portal_BE.Models.Entities;

public partial class ChucNang
{
    public long Id { get; set; }

    public string TenChucNang { get; set; } = null!;

    public string? MoTa { get; set; }

    public virtual ICollection<Quyen> Quyens { get; set; } = new List<Quyen>();
}
