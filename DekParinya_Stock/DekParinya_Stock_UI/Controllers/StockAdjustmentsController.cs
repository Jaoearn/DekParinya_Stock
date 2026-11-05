using DekParinya_Stock_UI.Helpers;
using DekParinya_Stock_UI.Models;
using DekParinya_Stock_UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DekParinya_Stock_UI.Controllers;

public class StockAdjustmentsController : AppControllerBase
{
    private readonly IDataStore _store;

    public StockAdjustmentsController(IDataStore store)
    {
        _store = store;
    }

    [HttpGet]
    public IActionResult Index(string? keyword, DocStatus? status, AdjustmentType? type, DateTime? from, DateTime? to)
    {
        IEnumerable<StockAdjustment> q = _store.GetAdjustments();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            q = q.Where(a => a.DocNo.Contains(k, StringComparison.OrdinalIgnoreCase)
                          || a.Requester.Contains(k, StringComparison.OrdinalIgnoreCase)
                          || a.Reason.Contains(k, StringComparison.OrdinalIgnoreCase));
        }
        if (status.HasValue) q = q.Where(a => a.Status == status);
        if (type.HasValue) q = q.Where(a => a.Type == type);
        if (from.HasValue) q = q.Where(a => a.AdjustDate >= from.Value.Date);
        if (to.HasValue) q = q.Where(a => a.AdjustDate <= to.Value.Date);

        ViewBag.Keyword = keyword;
        ViewBag.Status = status;
        ViewBag.Type = type;
        ViewBag.From = from;
        ViewBag.To = to;
        return View(q.ToList());
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new AdjustmentCreateViewModel
        {
            AdjustDate = DateTime.Today,
            Requester = CurrentUserName,
            Lines = new List<DocLineInput> { new() { Qty = 1 } }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(AdjustmentCreateViewModel model)
    {
        var lines = DocLines.Validate(model.Lines, _store, ModelState);
        if (!ModelState.IsValid)
        {
            if (model.Lines.Count == 0) model.Lines.Add(new DocLineInput { Qty = 1 });
            return View(model);
        }

        var adj = _store.AddAdjustment(new StockAdjustment
        {
            AdjustDate = model.AdjustDate.Date,
            Type = model.Type,
            Reason = model.Reason,
            Requester = model.Requester.Trim(),
            Remark = model.Remark,
            Lines = lines.Select(x => new StockAdjustmentLine
            {
                ProductId = x.Product.Id,
                ProductCode = x.Product.Code,
                ProductName = x.Product.Name,
                SystemQty = x.Product.StockQty,
                Qty = x.Line.Qty,
                Uom = x.Line.Uom ?? x.Product.Uom,
                Remark = x.Line.Remark
            }).ToList()
        });

        Success($"สร้างใบปรับปรุงสต๊อก {adj.DocNo} เรียบร้อยแล้ว (รออนุมัติ)");
        return RedirectToAction(nameof(Details), new { id = adj.Id });
    }

    [HttpGet]
    public IActionResult Details(int id)
    {
        var a = _store.GetAdjustment(id);
        return a is null ? NotFound() : View(a);
    }
}
