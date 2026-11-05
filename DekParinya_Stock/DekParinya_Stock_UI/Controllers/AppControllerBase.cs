using Microsoft.AspNetCore.Mvc;

namespace DekParinya_Stock_UI.Controllers;

/// <summary>Base controller: helper ทั่วไปของทุกโมดูล (ยังไม่ใส่ [Authorize] ตามขอบเขตต้นแบบ)</summary>
public abstract class AppControllerBase : Controller
{
    protected string CurrentUserName =>
        User.Identity?.IsAuthenticated == true ? (User.Identity?.Name ?? "Administrator") : "Administrator";

    protected void Success(string message) => TempData["Success"] = message;
    protected void Error(string message) => TempData["Error"] = message;
}
