using DekParinya_Stock_UI.Models;
using DekParinya_Stock_UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DekParinya_Stock_UI.Controllers;

public class PurchaseOrdersController : AppControllerBase
{
    private readonly IDataStore _store;

    public PurchaseOrdersController(IDataStore store)
    {
        _store = store;
    }

    /// <summary>รายการสินค้าที่ควรสั่งซื้อ (ใกล้หมด / หมด)</summary>
    [HttpGet]
    public IActionResult Index(string? vendor, int? groupId, string? scope)
    {
        IEnumerable<Product> q = _store.GetProducts().Where(p => p.IsActive);
        if (scope != "all") q = q.Where(p => p.State != StockState.Normal);
        if (!string.IsNullOrWhiteSpace(vendor)) q = q.Where(p => p.Vendor == vendor);
        if (groupId is > 0) q = q.Where(p => p.GroupId == groupId);

        ViewBag.Vendor = vendor;
        ViewBag.GroupId = groupId;
        ViewBag.Scope = scope;
        return View(q.OrderBy(p => p.Vendor).ThenBy(p => p.StockQty).ToList());
    }

    [HttpGet]
    public IActionResult Order(string? vendor, int[]? productIds)
    {
        var vm = new PurchaseOrderViewModel { Vendor = vendor ?? string.Empty };
        IEnumerable<Product> prefill = Enumerable.Empty<Product>();

        if (productIds is { Length: > 0 })
            prefill = _store.GetProducts().Where(p => productIds.Contains(p.Id));
        else if (!string.IsNullOrWhiteSpace(vendor))
            prefill = _store.GetProducts().Where(p => p.Vendor == vendor && p.IsActive && p.State != StockState.Normal);

        vm.Lines = prefill.Select(p => new PurchaseOrderLineInput
        {
            ProductId = p.Id,
            Qty = p.SuggestedOrderQty,
            Uom = p.Uom,
            UnitPrice = p.UnitPrice
        }).ToList();

        if (string.IsNullOrWhiteSpace(vm.Vendor))
            vm.Vendor = prefill.Select(p => p.Vendor).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
        if (vm.Lines.Count == 0) vm.Lines.Add(new PurchaseOrderLineInput { Qty = 1 });
        return View(vm);
    }

    /// <param name="submitAction">"export" = Export Excel, "create" = สร้าง PO (ต้นแบบ ยังไม่บันทึก)</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Order(PurchaseOrderViewModel model, string? submitAction)
    {
        var lines = new List<(Product Product, PurchaseOrderLineInput Line)>();
        var input = model.Lines.Where(l => l.ProductId > 0).ToList();
        if (input.Count == 0)
            ModelState.AddModelError(string.Empty, "กรุณาเพิ่มรายการสินค้าอย่างน้อย 1 รายการ");

        var no = 0;
        foreach (var l in input)
        {
            no++;
            var p = _store.GetProduct(l.ProductId);
            if (p is null) { ModelState.AddModelError(string.Empty, $"รายการที่ {no}: ไม่พบสินค้า"); continue; }
            if (l.Qty <= 0) ModelState.AddModelError(string.Empty, $"รายการที่ {no} ({p.Name}): จำนวนต้องมากกว่า 0");
            if (l.UnitPrice < 0) ModelState.AddModelError(string.Empty, $"รายการที่ {no} ({p.Name}): ราคาต้องไม่ติดลบ");
            if (string.IsNullOrWhiteSpace(l.Uom) || !Lookups.Uoms.Contains(l.Uom)) l.Uom = p.Uom;
            lines.Add((p, l));
        }

        if (!ModelState.IsValid)
        {
            if (model.Lines.Count == 0) model.Lines.Add(new PurchaseOrderLineInput { Qty = 1 });
            return View(model);
        }

        if (submitAction == "export")
        {
            var bytes = ExcelExportService.PurchaseOrder(model, lines);
            var safeVendor = new string(model.Vendor.Where(char.IsLetterOrDigit).ToArray());
            return File(bytes, ExcelExportService.ContentType, $"PO_{safeVendor}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        // ต้นแบบ: ยังไม่บันทึก PO และยังไม่มีเลขที่ PO
        var total = lines.Sum(x => x.Line.Qty * x.Line.UnitPrice);
        Success($"สร้างใบสั่งซื้อถึง {model.Vendor} จำนวน {lines.Count} รายการ ยอดรวม ฿{total:N2} (ต้นแบบ — ยังไม่บันทึกข้อมูล)");
        return RedirectToAction(nameof(Index));
    }
}
