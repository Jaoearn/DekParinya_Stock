using DekParinya_Stock_UI.Models;
using DekParinya_Stock_UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DekParinya_Stock_UI.Controllers;

public class SearchController : AppControllerBase
{
    private readonly IDataStore _store;

    private static readonly (string Title, string Keywords, string Controller, string Action, string Icon)[] Pages =
    {
        ("Goods Receipt (GR)", "goods receipt gr รับเข้า รับของ วัตถุดิบ ดิน กาก ขุย", "GoodsReceipts", "Index", "bi-box-arrow-in-down"),
        ("Add GR", "add gr create รับเข้าใหม่", "GoodsReceipts", "Create", "bi-plus-square"),
        ("Dashboard", "dashboard home หน้าหลัก ภาพรวม", "Home", "Index", "bi-grid-1x2"),
        ("Product Group", "product group กลุ่มสินค้า", "Products", "ProductGroup", "bi-collection"),
        ("Product List", "product list สินค้า รายการสินค้า", "Products", "ProductList", "bi-box-seam"),
        ("Stock Requisition", "stock requisition เบิก ใบเบิก", "StockRequisitions", "Index", "bi-clipboard-check"),
        ("Add a Requisition", "add requisition create สร้างใบเบิก", "StockRequisitions", "Create", "bi-plus-square"),
        ("Requisition Approval", "requisition approval อนุมัติ ใบเบิก", "StockRequisitionApproval", "Index", "bi-check2-square"),
        ("Purchase Order", "purchase order po สั่งซื้อ", "PurchaseOrders", "Index", "bi-cart3"),
        ("Vendor Orders", "vendor orders ผู้ขาย ซัพพลายเออร์", "ProductDemand", "Index", "bi-truck"),
        ("Stock Adjustment", "stock adjustment ปรับปรุง ปรับยอด", "StockAdjustments", "Index", "bi-sliders"),
        ("Adjustment Approval", "adjustment approval อนุมัติ ปรับปรุง", "StockAdjustmentApproval", "Index", "bi-check2-square"),
        ("Stock Balance", "stock balance report รายงาน คงเหลือ", "Reports", "StockBalance", "bi-file-earmark-bar-graph"),
        ("Users", "users ผู้ใช้ user", "Users", "Index", "bi-people")
    };

    public SearchController(IDataStore store)
    {
        _store = store;
    }

    [HttpGet]
    public IActionResult Index(string? q)
    {
        ViewBag.Query = q;
        var results = string.IsNullOrWhiteSpace(q) ? new List<SearchResultItem>() : Find(q.Trim(), int.MaxValue);
        return View(results);
    }

    /// <summary>รายการแนะนำขณะพิมพ์ (JSON)</summary>
    [HttpGet]
    public IActionResult Suggest(string? q)
    {
        if (string.IsNullOrWhiteSpace(q)) return Json(Array.Empty<SearchResultItem>());
        var items = Find(q.Trim(), 4)
            .GroupBy(r => r.Type)
            .SelectMany(g => g.Take(4))
            .Take(10)
            .ToList();
        return Json(items);
    }

    private List<SearchResultItem> Find(string k, int perType)
    {
        bool Has(string? s) => !string.IsNullOrEmpty(s) && s.Contains(k, StringComparison.OrdinalIgnoreCase);
        var results = new List<SearchResultItem>();
        var groups = _store.GetGroups();
        var groupNames = groups.ToDictionary(g => g.Id, g => g.Name);

        results.AddRange(Pages.Where(p => Has(p.Title) || Has(p.Keywords)).Take(perType).Select(p => new SearchResultItem
        {
            Type = "Menu", Title = p.Title, Subtitle = "ไปที่หน้า " + p.Title,
            Url = Url.Action(p.Action, p.Controller) ?? "/", Icon = p.Icon
        }));

        results.AddRange(_store.GetProducts().Where(p => Has(p.Code) || Has(p.Name) || Has(p.Vendor)).Take(perType).Select(p => new SearchResultItem
        {
            Type = "Product", Title = p.Name,
            Subtitle = $"{p.Code} · คงเหลือ {p.StockQty:N0} {p.Uom} · {(groupNames.TryGetValue(p.GroupId, out var gn) ? gn : "-")}",
            Url = Url.Action("EditProductList", "Products", new { id = p.Id }) ?? "/", Icon = "bi-box-seam"
        }));

        results.AddRange(groups.Where(g => Has(g.Code) || Has(g.Name)).Take(perType).Select(g => new SearchResultItem
        {
            Type = "Group", Title = g.Name, Subtitle = g.Code,
            Url = Url.Action("ProductList", "Products", new { groupId = g.Id }) ?? "/", Icon = "bi-collection"
        }));

        results.AddRange(_store.GetRequisitions().Where(r => Has(r.DocNo) || Has(r.Requester) || Has(r.Event)).Take(perType).Select(r => new SearchResultItem
        {
            Type = "Requisition", Title = r.DocNo, Subtitle = $"{r.Event} · {r.Requester} · {r.Status}",
            Url = Url.Action("Details", "StockRequisitions", new { id = r.Id }) ?? "/", Icon = "bi-clipboard-check"
        }));

        results.AddRange(_store.GetGoodsReceipts().Where(g => Has(g.DocNo) || Has(g.Supplier) || Has(g.RefNo) || g.Lines.Any(l => Has(l.Material))).Take(perType).Select(g => new SearchResultItem
        {
            Type = "GR", Title = g.DocNo, Subtitle = $"{g.Supplier} · {string.Join(", ", g.Lines.Select(l => l.Material))}",
            Url = Url.Action("Details", "GoodsReceipts", new { id = g.Id }) ?? "/", Icon = "bi-box-arrow-in-down"
        }));

        results.AddRange(_store.GetAdjustments().Where(a => Has(a.DocNo) || Has(a.Requester) || Has(a.Reason)).Take(perType).Select(a => new SearchResultItem
        {
            Type = "Adjustment", Title = a.DocNo, Subtitle = $"{a.Type} · {a.Reason} · {a.Status}",
            Url = Url.Action("Details", "StockAdjustments", new { id = a.Id }) ?? "/", Icon = "bi-sliders"
        }));

        results.AddRange(_store.GetUsers().Where(u => Has(u.FullName) || Has(u.Email)).Take(perType).Select(u => new SearchResultItem
        {
            Type = "User", Title = u.FullName, Subtitle = $"{u.Email} · {u.Role}",
            Url = Url.Action("Edit", "Users", new { id = u.Id }) ?? "/", Icon = "bi-person"
        }));

        return results;
    }
}
