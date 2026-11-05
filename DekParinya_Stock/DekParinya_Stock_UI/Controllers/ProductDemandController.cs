using DekParinya_Stock_UI.Models;
using DekParinya_Stock_UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DekParinya_Stock_UI.Controllers;

/// <summary>
/// ความต้องการสินค้ารายผู้ขาย (Vendor Orders) — ใช้ Views/VendorOrders/*
/// รวมสินค้าใกล้หมด/หมด และยอดเบิกที่รออนุมัติ แยกตามผู้ขาย
/// </summary>
public class ProductDemandController : AppControllerBase
{
    private const string ViewIndex = "~/Views/VendorOrders/Index.cshtml";
    private const string ViewDetails = "~/Views/VendorOrders/Details.cshtml";
    private readonly IDataStore _store;

    public ProductDemandController(IDataStore store)
    {
        _store = store;
    }

    [HttpGet]
    public IActionResult Index(string? keyword)
    {
        var pendingQty = PendingRequisitionQty();
        var rows = _store.GetProducts()
            .Where(p => p.IsActive && !string.IsNullOrWhiteSpace(p.Vendor))
            .GroupBy(p => p.Vendor!)
            .Select(g =>
            {
                var need = g.Where(p => p.State != StockState.Normal).ToList();
                return new VendorDemandRow
                {
                    Vendor = g.Key,
                    ProductCount = g.Count(),
                    LowCount = g.Count(p => p.State == StockState.Low),
                    OutCount = g.Count(p => p.State == StockState.Out),
                    SuggestedQty = need.Sum(p => p.SuggestedOrderQty),
                    EstimatedAmount = need.Sum(p => p.SuggestedOrderQty * p.UnitPrice),
                    PendingRequisitionQty = g.Sum(p => pendingQty.GetValueOrDefault(p.Id))
                };
            });

        if (!string.IsNullOrWhiteSpace(keyword))
            rows = rows.Where(r => r.Vendor.Contains(keyword.Trim(), StringComparison.OrdinalIgnoreCase));

        ViewBag.Keyword = keyword;
        return View(ViewIndex, rows.OrderByDescending(r => r.OutCount + r.LowCount).ThenBy(r => r.Vendor).ToList());
    }

    [HttpGet]
    public IActionResult Details(string vendor)
    {
        if (string.IsNullOrWhiteSpace(vendor)) return RedirectToAction(nameof(Index));
        var products = _store.GetProducts().Where(p => p.Vendor == vendor).OrderBy(p => p.StockQty).ToList();
        if (products.Count == 0) return NotFound();

        ViewBag.Vendor = vendor;
        ViewBag.PendingQty = PendingRequisitionQty();
        return View(ViewDetails, products);
    }

    private Dictionary<int, int> PendingRequisitionQty() =>
        _store.GetRequisitions()
            .Where(r => r.Status == DocStatus.Pending)
            .SelectMany(r => r.Lines)
            .GroupBy(l => l.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Qty));
}
