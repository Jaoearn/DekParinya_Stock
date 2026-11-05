using System.ComponentModel.DataAnnotations;

namespace DekParinya_Stock_UI.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "กรุณาระบุอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาระบุรหัสผ่าน")]
    [StringLength(200)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class DocLineInput
{
    public int ProductId { get; set; }
    public int Qty { get; set; }

    [StringLength(200)]
    public string? Uom { get; set; }

    [StringLength(200)]
    public string? Remark { get; set; }
}

public class RequisitionCreateViewModel
{
    [Required(ErrorMessage = "กรุณาระบุวันที่")]
    [DataType(DataType.Date)]
    [Display(Name = "Request Date")]
    public DateTime RequestDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "กรุณาระบุผู้ขอเบิก")]
    [StringLength(200)]
    [Display(Name = "Requester")]
    public string Requester { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาเลือกแผนก")]
    [StringLength(200)]
    [Display(Name = "Department")]
    public string Department { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาระบุ Event")]
    [StringLength(200)]
    [Display(Name = "Event")]
    public string Event { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Remark")]
    public string? Remark { get; set; }

    public List<DocLineInput> Lines { get; set; } = new();
}

public class AdjustmentCreateViewModel
{
    [Required(ErrorMessage = "กรุณาระบุวันที่")]
    [DataType(DataType.Date)]
    [Display(Name = "Adjust Date")]
    public DateTime AdjustDate { get; set; } = DateTime.Today;

    [Display(Name = "Adjustment Type")]
    public AdjustmentType Type { get; set; } = AdjustmentType.Decrease;

    [Required(ErrorMessage = "กรุณาเลือกเหตุผล")]
    [StringLength(200)]
    [Display(Name = "Reason")]
    public string Reason { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาระบุผู้ทำรายการ")]
    [StringLength(200)]
    [Display(Name = "Requester")]
    public string Requester { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Remark")]
    public string? Remark { get; set; }

    public List<DocLineInput> Lines { get; set; } = new();
}

public class PurchaseOrderLineInput
{
    public int ProductId { get; set; }
    public int Qty { get; set; }

    [StringLength(200)]
    public string? Uom { get; set; }

    public decimal UnitPrice { get; set; }

    [StringLength(200)]
    public string? Remark { get; set; }
}

public class PurchaseOrderViewModel
{
    [Required(ErrorMessage = "กรุณาระบุผู้ขาย")]
    [StringLength(200)]
    [Display(Name = "Vendor")]
    public string Vendor { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาระบุวันที่สั่งซื้อ")]
    [DataType(DataType.Date)]
    [Display(Name = "Order Date")]
    public DateTime OrderDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "Expected Date")]
    public DateTime? ExpectedDate { get; set; } = DateTime.Today.AddDays(7);

    [StringLength(200)]
    [Display(Name = "Contact Person")]
    public string? ContactName { get; set; }

    [StringLength(200)]
    [Display(Name = "Deliver To")]
    public string? DeliverTo { get; set; } = "DekParinya HQ Warehouse";

    [StringLength(200)]
    [Display(Name = "Remark")]
    public string? Remark { get; set; }

    public List<PurchaseOrderLineInput> Lines { get; set; } = new();
}

public class StockBalanceRow
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Uom { get; set; } = string.Empty;
    public int Stock { get; set; }
    public int Reorder { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Value => Stock * UnitPrice;
    public StockState State { get; set; }
}

public class SearchResultItem
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Icon { get; set; } = "bi-search";
}

public class VendorDemandRow
{
    public string Vendor { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public int LowCount { get; set; }
    public int OutCount { get; set; }
    public int SuggestedQty { get; set; }
    public decimal EstimatedAmount { get; set; }
    public int PendingRequisitionQty { get; set; }
}

public class DashboardViewModel
{
    public int TotalProducts { get; set; }
    public int TotalGroups { get; set; }
    public int TotalStockQty { get; set; }
    public decimal StockValue { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public int PendingRequisitions { get; set; }
    public int PendingAdjustments { get; set; }
    public List<string> GroupLabels { get; set; } = new();
    public List<int> GroupStock { get; set; } = new();
    public List<string> MonthLabels { get; set; } = new();
    public List<int> MonthRequisitionQty { get; set; } = new();
    public List<Product> LowStockItems { get; set; } = new();
    public List<StockRequisition> RecentRequisitions { get; set; } = new();
}
