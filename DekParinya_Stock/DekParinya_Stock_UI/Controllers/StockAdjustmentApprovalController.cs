using DekParinya_Stock_UI.Models;
using DekParinya_Stock_UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DekParinya_Stock_UI.Controllers;

public class StockAdjustmentApprovalController : AppControllerBase
{
    private readonly IDataStore _store;

    public StockAdjustmentApprovalController(IDataStore store)
    {
        _store = store;
    }

    [HttpGet]
    public IActionResult Index(string? keyword, string? status)
    {
        status ??= nameof(DocStatus.Pending);
        var all = _store.GetAdjustments();
        IEnumerable<StockAdjustment> q = all;

        if (Enum.TryParse<DocStatus>(status, out var parsedStatus))
            q = q.Where(a => a.Status == parsedStatus);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            q = q.Where(a => a.DocNo.Contains(k, StringComparison.OrdinalIgnoreCase)
                          || a.Requester.Contains(k, StringComparison.OrdinalIgnoreCase)
                          || a.Reason.Contains(k, StringComparison.OrdinalIgnoreCase));
        }

        ViewBag.Keyword = keyword;
        ViewBag.Status = status;
        ViewBag.Counts = all.GroupBy(a => a.Status).ToDictionary(g => g.Key, g => g.Count());
        ViewBag.Total = all.Count;
        return View(q.ToList());
    }

    [HttpGet]
    public IActionResult Details(int id)
    {
        var a = _store.GetAdjustment(id);
        return a is null ? NotFound() : View(a);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Approve(int id, string? remark)
    {
        // ต้นแบบ: ยังไม่ปรับยอดสต๊อกจริงตอน Approve
        var a = _store.GetAdjustment(id);
        if (a is null) return NotFound();
        if (_store.SetAdjustmentStatus(id, DocStatus.Approved, CurrentUserName, Trim200(remark)))
            Success($"อนุมัติใบปรับปรุงสต๊อก {a.DocNo} เรียบร้อยแล้ว");
        else
            Error("เอกสารนี้ไม่ได้อยู่ในสถานะรออนุมัติ");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Reject(int id, string? remark)
    {
        var a = _store.GetAdjustment(id);
        if (a is null) return NotFound();
        if (string.IsNullOrWhiteSpace(remark))
        {
            Error("กรุณาระบุเหตุผลที่ไม่อนุมัติ");
            return RedirectToAction(nameof(Details), new { id });
        }
        if (_store.SetAdjustmentStatus(id, DocStatus.Rejected, CurrentUserName, Trim200(remark)))
            Success($"ไม่อนุมัติใบปรับปรุงสต๊อก {a.DocNo} แล้ว");
        else
            Error("เอกสารนี้ไม่ได้อยู่ในสถานะรออนุมัติ");
        return RedirectToAction(nameof(Details), new { id });
    }

    private static string? Trim200(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : (s.Length > 200 ? s[..200] : s).Trim();
}
