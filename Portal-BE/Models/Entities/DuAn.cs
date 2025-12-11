using System;
using System.Collections.Generic;

namespace Portal_BE.Models.Entities;

public partial class DuAn
{
    public long Id { get; set; }

    public string? TenDuAn { get; set; }

    public DateTime? NgayBatDau { get; set; }

    public DateTime? NgayKetThuc { get; set; }

    public string? TrangThai { get; set; }

    public virtual ICollection<CongViec> CongViecs { get; set; } = new List<CongViec>();
}
