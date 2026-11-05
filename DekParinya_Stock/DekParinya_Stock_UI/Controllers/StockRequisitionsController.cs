using DekParinya_Stock_UI.Helpers;
using DekParinya_Stock_UI.Models;
using DekParinya_Stock_UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DekParinya_Stock_UI.Controllers;

public class StockRequisitionsController : AppControllerBase
{
    private readonly IDataStore _store;

    public StockRequisitionsController(IDataStore store)
    {
        _store = store;
    }

    [HttpGet]
    public IActionResult Index(string? keyword, DocStatus? status, DateTime? from, DateTime? to)
    {
        IEnumerable<StockRequisition> q = _store.GetRequisitions();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            q = q.Where(r => r.DocNo.Contains(k, StringComparison.OrdinalIgnoreCase)
                          || r.Requester.Contains(k, StringComparison.OrdinalIgnoreCase)
                          || r.Event.Contains(k, StringComparison.OrdinalIgnoreCase)
                          || r.Department.Contains(k, StringComparison.OrdinalIgnoreCase));
        }
        if (status.HasValue) q = q.Where(r => r.Status == status);
        if (from.HasValue) q = q.Where(r => r.RequestDate >= from.Value.Date);
        if (to.HasValue) q = q.Where(r => r.RequestDate <= to.Value.Date);

        ViewBag.Keyword = keyword;
        ViewBag.Status = status;
        ViewBag.From = from;
        ViewBag.To = to;
        return View(q.ToList());
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new RequisitionCreateViewModel
        {
            RequestDate = DateTime.Today,
            Requester = CurrentUserName,
            Lines = new List<DocLineInput> { new() { Qty = 1 } }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(RequisitionCreateViewModel model)
    {
        var lines = DocLines.Validate(model.Lines, _store, ModelState);
        if (!ModelState.IsValid)
        {
            if (model.Lines.Count == 0) model.Lines.Add(new DocLineInput { Qty = 1 });
            return View(model);
        }

        var req = _store.AddRequisition(new StockRequisition
        {
            RequestDate = model.RequestDate.Date,
            Requester = model.Requester.Trim(),
            Department = model.Department,
            Event = model.Event.Trim(),
            Remark = model.Remark,
            Lines = lines.Select(x => new StockRequisitionLine
            {
                ProductId = x.Product.Id,
                ProductCode = x.Product.Code,
                ProductName = x.Product.Name,
                Qty = x.Line.Qty,
                Uom = x.Line.Uom ?? x.Product.Uom,
                Remark = x.Line.Remark
            }).ToList()
        });

        Success($"สร้างใบเบิก {req.DocNo} เรียบร้อยแล้ว (รออนุมัติ)");
        return RedirectToAction(nameof(Details), new { id = req.Id });
    }

    [HttpGet]
    public IActionResult Details(int id)
    {
        var r = _store.GetRequisition(id);
        return r is null ? NotFound() : View(r);
    }
}
