using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;

namespace PhongNguyenPuppy_MVC.Data;

[Table("MaGiamGia")]
public partial class MaGiamGia
{
    public int MaGg { get; set; }
    public string Code { get; set; } = null!;
    public double GiaTri { get; set; }
    public bool LoaiGiam { get; set; }
    public DateTime? HanSuDung { get; set; }
    public int? SoLuongToiDa { get; set; }
    public int SoLuongDaDung { get; set; }
    public bool TrangThai { get; set; }
    public string? MoTa { get; set; }
    public string? MaNv { get; set; }
    public virtual NhanVien? MaNvNavigation { get; set; }
}