using System;
using System.Collections.Generic;

namespace Portal_BE.Models.Entities;

public partial class CuocTroChuyen
{
    public int Id { get; set; }

    public string? TenCuocTroChuyen { get; set; }

    public bool? LaNhom { get; set; }

    public DateTime? NgayTao { get; set; }

    public virtual ICollection<TinNhan> TinNhans { get; set; } = new List<TinNhan>();
}
