using System.ComponentModel.DataAnnotations;

namespace DekParinya_Stock_UI.Models;

/// <summary>วัตถุดิบสำหรับทำปุ๋ย (ดิน / กาก / ขุย ฯลฯ) และหน่วยรับเข้า</summary>
public static class RawMaterials
{
    public static readonly (string Group, string[] Items)[] Groups =
    {
        ("ดิน", new[] { "ดินร่วน", "ดินเหนียว", "ดินดำ", "ดินลูกรัง", "หน้าดิน" }),
        ("กาก", new[] { "กากอ้อย (ฟิลเตอร์เค้ก)", "กากมันสำปะหลัง", "กากกาแฟ", "กากถั่วเหลือง", "กากน้ำตาล (โมลาส)" }),
        ("ขุย / วัสดุปลูก", new[] { "ขุยมะพร้าว", "แกลบดิบ", "แกลบดำ (แกลบเผา)", "ฟางข้าว", "ใบไม้แห้ง" }),
        ("มูลสัตว์", new[] { "มูลไก่", "มูลวัว", "มูลค้างคาว" }),
        ("แร่ / ปรับสภาพดิน", new[] { "ปูนโดโลไมท์", "ยิปซัม", "ร็อคฟอสเฟต" })
    };

    public static IEnumerable<string> All => Groups.SelectMany(g => g.Items);

    /// <summary>หน่วยรับเข้าวัตถุดิบ (แยกจาก UOM สินค้า ขวด/แพ็ค/ลัง)</summary>
    public static readonly string[] Units = { "กิโลกรัม", "ตัน", "กระสอบ", "คิว (ลบ.ม.)" };

    public static readonly string[] Qualities = { "ดี", "ปานกลาง", "ต้องตรวจซ้ำ" };
}

public class GoodsReceipt
{
    public int Id { get; set; }
    public string DocNo { get; set; } = string.Empty;
    public DateTime ReceiveDate { get; set; }
    public string Supplier { get; set; } = string.Empty;
    public string? RefNo { get; set; }
    public string? VehicleNo { get; set; }
    public string ReceivedBy { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<GoodsReceiptLine> Lines { get; set; } = new();
    public List<GoodsReceiptAttachment> Attachments { get; set; } = new();
}

public class GoodsReceiptLine
{
    public string Material { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Quality { get; set; }
    public string? Remark { get; set; }
}

public class GoodsReceiptAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;   // ชื่อไฟล์เดิม
    public string Url { get; set; } = string.Empty;        // path ใน wwwroot
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.Now;
}

public class GoodsReceiptLineInput
{
    [StringLength(200)]
    public string? Material { get; set; }

    public decimal Qty { get; set; }

    [StringLength(200)]
    public string? Unit { get; set; }

    [StringLength(200)]
    public string? Quality { get; set; }

    [StringLength(200)]
    public string? Remark { get; set; }
}

public class GoodsReceiptCreateViewModel
{
    [Required(ErrorMessage = "กรุณาระบุวันที่รับเข้า")]
    [DataType(DataType.Date)]
    [Display(Name = "Receive Date")]
    public DateTime ReceiveDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "กรุณาระบุผู้ส่ง / ซัพพลายเออร์")]
    [StringLength(200)]
    [Display(Name = "Supplier")]
    public string Supplier { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Delivery Note No.")]
    public string? RefNo { get; set; }

    [StringLength(200)]
    [Display(Name = "Vehicle No.")]
    public string? VehicleNo { get; set; }

    [Required(ErrorMessage = "กรุณาระบุผู้รับของ")]
    [StringLength(200)]
    [Display(Name = "Received By")]
    public string ReceivedBy { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Remark")]
    public string? Remark { get; set; }

    public List<GoodsReceiptLineInput> Lines { get; set; } = new();
}
