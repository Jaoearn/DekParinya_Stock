using DekParinya_Stock_UI.Models;
using DekParinya_Stock_UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DekParinya_Stock_UI.Controllers;

public class StockRequisitionApprovalController : AppControllerBase
{
    private readonly IDataStore _store;

    public StockRequisitionApprovalController(IDataStore store)
    {
        _store = store;
    }

    /// <param name="status">ค่าเริ่มต้น = Pending, "All" = ทุกสถานะ</param>
    [HttpGet]
    public IActionResult Index(string? keyword, string? status)
    {
        status ??= nameof(DocStatus.Pending);
        var all = _store.GetRequisitions();
        IEnumerable<StockRequisition> q = all;

        if (Enum.TryParse<DocStatus>(status, out var parsedStatus))
            q = q.Where(r => r.Status == parsedStatus);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            q = q.Where(r => r.DocNo.Contains(k, StringComparison.OrdinalIgnoreCase)
                          || r.Requester.Contains(k, StringComparison.OrdinalIgnoreCase)
                          || r.Event.Contains(k, StringComparison.OrdinalIgnoreCase));
        }

        ViewBag.Keyword = keyword;
        ViewBag.Status = status;
        ViewBag.Counts = all.GroupBy(r => r.Status).ToDictionary(g => g.Key, g => g.Count());
        ViewBag.Total = all.Count;
        return View(q.ToList());
    }

    [HttpGet]
    public IActionResult Details(int id)
    {
        var r = _store.GetRequisition(id);
        return r is null ? NotFound() : View(r);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Approve(int id, string? remark)
    {
        // ต้นแบบ: ไม่ตรวจยอดคงเหลือ และยังไม่ตัดสต๊อกตอน Approve
        var r = _store.GetRequisition(id);
        if (r is null) return NotFound();
        if (_store.SetRequisitionStatus(id, DocStatus.Approved, CurrentUserName, Trim200(remark)))
            Success($"อนุมัติใบเบิก {r.DocNo} เรียบร้อยแล้ว");
        else
            Error("เอกสารนี้ไม่ได้อยู่ในสถานะรออนุมัติ");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Reject(int id, string? remark)
    {
        var r = _store.GetRequisition(id);
        if (r is null) return NotFound();
        if (string.IsNullOrWhiteSpace(remark))
        {
            Error("กรุณาระบุเหตุผลที่ไม่อนุมัติ");
            return RedirectToAction(nameof(Details), new { id });
        }
        if (_store.SetRequisitionStatus(id, DocStatus.Rejected, CurrentUserName, Trim200(remark)))
            Success($"ไม่อนุมัติใบเบิก {r.DocNo} แล้ว");
        else
            Error("เอกสารนี้ไม่ได้อยู่ในสถานะรออนุมัติ");
        return RedirectToAction(nameof(Details), new { id });
    }

    private static string? Trim200(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : (s.Length > 200 ? s[..200] : s).Trim();
}
