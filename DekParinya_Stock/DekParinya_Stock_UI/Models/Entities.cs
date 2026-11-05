using System.ComponentModel.DataAnnotations;

namespace DekParinya_Stock_UI.Models;

public enum DocStatus { Pending, Approved, Rejected }

public enum StockState { Normal, Low, Out }

public enum AdjustmentType { Increase, Decrease }

public static class Lookups
{
    /// <summary>UOM — 3 แบบ ไม่มีอัตราแปลงหน่วย</summary>
    public static readonly string[] Uoms = { "ขวด", "แพ็ค", "ลัง" };

    public static readonly string[] Departments =
    {
        "Front Desk", "Fitness Floor", "Group Class", "Personal Training",
        "Housekeeping", "Marketing", "Management"
    };

    public static readonly string[] AdjustmentReasons =
    {
        "ตรวจนับสต๊อก", "สินค้าชำรุด", "สินค้าหมดอายุ", "รับคืนสินค้า", "อื่นๆ"
    };

    public static readonly string[] Roles = { "Admin", "Approver", "Staff" };
}

public class ProductGroup
{
    public int Id { get; set; }

    [Required(ErrorMessage = "กรุณาระบุรหัสกลุ่มสินค้า")]
    [StringLength(200)]
    [Display(Name = "Group Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาระบุชื่อกลุ่มสินค้า")]
    [StringLength(200)]
    [Display(Name = "Group Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class Product
{
    public int Id { get; set; }

    [Required(ErrorMessage = "กรุณาระบุรหัสสินค้า")]
    [StringLength(200)]
    [Display(Name = "Product Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาระบุชื่อสินค้า")]
    [StringLength(200)]
    [Display(Name = "Product Name")]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "กรุณาเลือกกลุ่มสินค้า")]
    [Display(Name = "Product Group")]
    public int GroupId { get; set; }

    [Required(ErrorMessage = "กรุณาเลือกหน่วยนับ")]
    [StringLength(200)]
    [Display(Name = "UOM")]
    public string Uom { get; set; } = "ขวด";

    [Range(0, 9_999_999, ErrorMessage = "ราคาต้องไม่ติดลบ")]
    [Display(Name = "Unit Price")]
    public decimal UnitPrice { get; set; }

    [Range(0, 9_999_999, ErrorMessage = "จำนวนต้องไม่ติดลบ")]
    [Display(Name = "Stock Qty")]
    public int StockQty { get; set; }

    [Range(0, 9_999_999, ErrorMessage = "จำนวนต้องไม่ติดลบ")]
    [Display(Name = "Reorder Level")]
    public int ReorderLevel { get; set; }

    [StringLength(200)]
    [Display(Name = "Vendor")]
    public string? Vendor { get; set; }

    [StringLength(200)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public StockState State =>
        StockQty <= 0 ? StockState.Out :
        StockQty <= ReorderLevel ? StockState.Low : StockState.Normal;

    /// <summary>จำนวนแนะนำให้สั่งซื้อ (ต้นแบบ): เติมให้ถึง 2 เท่าของจุดสั่งซื้อ</summary>
    public int SuggestedOrderQty => Math.Max(ReorderLevel * 2 - StockQty, ReorderLevel);
}

public class StockRequisition
{
    public int Id { get; set; }
    public string DocNo { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public string Requester { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Event { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public DocStatus Status { get; set; } = DocStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? ActionBy { get; set; }
    public DateTime? ActionAt { get; set; }
    public string? ActionRemark { get; set; }
    public List<StockRequisitionLine> Lines { get; set; } = new();

    public int TotalQty => Lines.Sum(l => l.Qty);
}

public class StockRequisitionLine
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Qty { get; set; }
    public string Uom { get; set; } = string.Empty;
    public string? Remark { get; set; }
}

public class StockAdjustment
{
    public int Id { get; set; }
    public string DocNo { get; set; } = string.Empty;
    public DateTime AdjustDate { get; set; }
    public AdjustmentType Type { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Requester { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public DocStatus Status { get; set; } = DocStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? ActionBy { get; set; }
    public DateTime? ActionAt { get; set; }
    public string? ActionRemark { get; set; }
    public List<StockAdjustmentLine> Lines { get; set; } = new();

    public int TotalQty => Lines.Sum(l => l.Qty);
}

public class StockAdjustmentLine
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int SystemQty { get; set; }
    public int Qty { get; set; }
    public string Uom { get; set; } = string.Empty;
    public string? Remark { get; set; }
}

public class AppUser
{
    public int Id { get; set; }

    [Required(ErrorMessage = "กรุณาระบุชื่อ-นามสกุล")]
    [StringLength(200)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาระบุอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    [StringLength(200)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาเลือกบทบาท")]
    [StringLength(200)]
    [Display(Name = "Role")]
    public string Role { get; set; } = "Staff";

    [Required(ErrorMessage = "กรุณาเลือกแผนก")]
    [StringLength(200)]
    [Display(Name = "Department")]
    public string Department { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
