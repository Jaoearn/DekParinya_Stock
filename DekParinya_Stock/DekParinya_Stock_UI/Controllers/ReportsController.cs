using DekParinya_Stock_UI.Models;
using DekParinya_Stock_UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DekParinya_Stock_UI.Controllers;

public class ReportsController : AppControllerBase
{
    private readonly IDataStore _store;

    public ReportsController(IDataStore store)
    {
        _store = store;
    }

    public IActionResult Index() => RedirectToAction(nameof(StockBalance));

    [HttpGet]
    public IActionResult StockBalance(string? keyword, int? groupId, string? stock)
    {
        ViewBag.Keyword = keyword;
        ViewBag.GroupId = groupId;
        ViewBag.Stock = stock;
        return View(BuildRows(keyword, groupId, stock));
    }

    [HttpGet]
    public IActionResult ExportStockBalance(string? keyword, int? groupId, string? stock)
    {
        var rows = BuildRows(keyword, groupId, stock);
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(keyword)) parts.Add($"Keyword: {keyword}");
        if (groupId is > 0) parts.Add($"Group: {_store.GetGroup(groupId.Value)?.Name}");
        if (!string.IsNullOrWhiteSpace(stock)) parts.Add($"Status: {stock}");
        var filterText = parts.Count > 0 ? "| " + string.Join(", ", parts) : "";

        var bytes = ExcelExportService.StockBalance(rows, filterText);
        return File(bytes, ExcelExportService.ContentType, $"StockBalance_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }

    private List<StockBalanceRow> BuildRows(string? keyword, int? groupId, string? stock)
    {
        var groups = _store.GetGroups().ToDictionary(g => g.Id, g => g.Name);
        IEnumerable<Product> q = _store.GetProducts();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            q = q.Where(p => p.Code.Contains(k, StringComparison.OrdinalIgnoreCase)
                          || p.Name.Contains(k, StringComparison.OrdinalIgnoreCase));
        }
        if (groupId is > 0) q = q.Where(p => p.GroupId == groupId);
        q = stock switch
        {
            "Normal" => q.Where(p => p.State == StockState.Normal),
            "Low" => q.Where(p => p.State == StockState.Low),
            "Out" => q.Where(p => p.State == StockState.Out),
            _ => q
        };

        return q.Select(p => new StockBalanceRow
        {
            Code = p.Code,
            Name = p.Name,
            Group = groups.TryGetValue(p.GroupId, out var groupName) ? groupName : "-",
            Uom = p.Uom,
            Stock = p.StockQty,
            Reorder = p.ReorderLevel,
            UnitPrice = p.UnitPrice,
            State = p.State
        }).OrderBy(r => r.Group).ThenBy(r => r.Code).ToList();
    }
}
