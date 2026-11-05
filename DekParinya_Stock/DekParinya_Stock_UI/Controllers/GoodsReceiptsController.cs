using DekParinya_Stock_UI.Models;
using DekParinya_Stock_UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DekParinya_Stock_UI.Controllers;

/// <summary>GR — รับเข้าวัตถุดิบทำปุ๋ย (ดิน / กาก / ขุย ฯลฯ) พร้อมแนบรูป</summary>
public class GoodsReceiptsController : AppControllerBase
{
    private readonly IDataStore _store;
    private readonly ImageUploadService _images;

    public GoodsReceiptsController(IDataStore store, ImageUploadService images)
    {
        _store = store;
        _images = images;
    }

    [HttpGet]
    public IActionResult Index(string? keyword, string? material, DateTime? from, DateTime? to)
    {
        IEnumerable<GoodsReceipt> q = _store.GetGoodsReceipts();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            q = q.Where(r => r.DocNo.Contains(k, StringComparison.OrdinalIgnoreCase)
                          || r.Supplier.Contains(k, StringComparison.OrdinalIgnoreCase)
                          || (r.RefNo ?? "").Contains(k, StringComparison.OrdinalIgnoreCase)
                          || (r.VehicleNo ?? "").Contains(k, StringComparison.OrdinalIgnoreCase)
                          || r.ReceivedBy.Contains(k, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(material)) q = q.Where(r => r.Lines.Any(l => l.Material == material));
        if (from.HasValue) q = q.Where(r => r.ReceiveDate >= from.Value.Date);
        if (to.HasValue) q = q.Where(r => r.ReceiveDate <= to.Value.Date);

        ViewBag.Keyword = keyword;
        ViewBag.Material = material;
        ViewBag.From = from;
        ViewBag.To = to;
        return View(q.ToList());
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new GoodsReceiptCreateViewModel
        {
            ReceiveDate = DateTime.Today,
            ReceivedBy = CurrentUserName,
            Lines = new List<GoodsReceiptLineInput> { new() { Qty = 1, Unit = RawMaterials.Units[0], Quality = RawMaterials.Qualities[0] } }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(ImageUploadService.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = ImageUploadService.MaxRequestBytes)]
    public async Task<IActionResult> Create(GoodsReceiptCreateViewModel model, List<IFormFile>? images)
    {
        images ??= new List<IFormFile>();
        var lines = ValidateLines(model.Lines);
        foreach (var err in _images.Validate(images, 0)) ModelState.AddModelError(string.Empty, err);

        if (!ModelState.IsValid)
        {
            if (model.Lines.Count == 0) model.Lines.Add(new GoodsReceiptLineInput { Qty = 1 });
            if (images.Count > 0)
                ModelState.AddModelError(string.Empty, "กรุณาเลือกรูปแนบใหม่อีกครั้ง (เบราว์เซอร์ไม่เก็บไฟล์ไว้หลังส่งฟอร์ม)");
            return View(model);
        }

        var gr = _store.AddGoodsReceipt(new GoodsReceipt
        {
            ReceiveDate = model.ReceiveDate.Date,
            Supplier = model.Supplier.Trim(),
            RefNo = model.RefNo?.Trim(),
            VehicleNo = model.VehicleNo?.Trim(),
            ReceivedBy = model.ReceivedBy.Trim(),
            Remark = model.Remark,
            Lines = lines
        });

        if (images.Count > 0)
            _store.AddGoodsReceiptAttachments(gr.Id, await _images.SaveAsync(gr.Id, images));

        Success($"บันทึกรับเข้า {gr.DocNo} เรียบร้อยแล้ว" + (images.Count > 0 ? $" (แนบรูป {images.Count} รูป)" : ""));
        return RedirectToAction(nameof(Details), new { id = gr.Id });
    }

    [HttpGet]
    public IActionResult Details(int id)
    {
        var gr = _store.GetGoodsReceipt(id);
        return gr is null ? NotFound() : View(gr);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(ImageUploadService.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = ImageUploadService.MaxRequestBytes)]
    public async Task<IActionResult> AddImages(int id, List<IFormFile>? images)
    {
        var gr = _store.GetGoodsReceipt(id);
        if (gr is null) return NotFound();
        images ??= new List<IFormFile>();
        if (images.Count == 0)
        {
            Error("กรุณาเลือกรูปที่ต้องการแนบ");
            return RedirectToAction(nameof(Details), new { id });
        }

        var errors = _images.Validate(images, gr.Attachments.Count);
        if (errors.Count > 0)
        {
            Error(string.Join(" / ", errors));
            return RedirectToAction(nameof(Details), new { id });
        }

        _store.AddGoodsReceiptAttachments(id, await _images.SaveAsync(id, images));
        Success($"แนบรูปเพิ่ม {images.Count} รูปเรียบร้อยแล้ว");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteImage(int id, Guid attachmentId)
    {
        var att = _store.RemoveGoodsReceiptAttachment(id, attachmentId);
        if (att is null)
        {
            Error("ไม่พบรูปที่ต้องการลบ");
        }
        else
        {
            _images.Delete(att);
            Success($"ลบรูป {att.FileName} แล้ว");
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    private List<GoodsReceiptLine> ValidateLines(List<GoodsReceiptLineInput>? input)
    {
        var rows = (input ?? new()).Where(l => !string.IsNullOrWhiteSpace(l.Material)).ToList();
        if (rows.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "กรุณาเพิ่มรายการวัตถุดิบอย่างน้อย 1 รายการ");
            return new();
        }

        var result = new List<GoodsReceiptLine>();
        var no = 0;
        foreach (var l in rows)
        {
            no++;
            if (l.Qty <= 0) ModelState.AddModelError(string.Empty, $"รายการที่ {no} ({l.Material}): จำนวนต้องมากกว่า 0");
            var unit = RawMaterials.Units.Contains(l.Unit) ? l.Unit! : RawMaterials.Units[0];
            result.Add(new GoodsReceiptLine
            {
                Material = l.Material!.Trim(),
                Qty = l.Qty,
                Unit = unit,
                Quality = RawMaterials.Qualities.Contains(l.Quality) ? l.Quality : null,
                Remark = l.Remark
            });
        }
        return result;
    }
}
