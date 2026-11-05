using DekParinya_Stock_UI.Models;
using DekParinya_Stock_UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DekParinya_Stock_UI.Controllers;

public class UsersController : AppControllerBase
{
    private readonly IDataStore _store;

    public UsersController(IDataStore store)
    {
        _store = store;
    }

    [HttpGet]
    public IActionResult Index(string? keyword, string? role)
    {
        IEnumerable<AppUser> q = _store.GetUsers();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            q = q.Where(u => u.FullName.Contains(k, StringComparison.OrdinalIgnoreCase)
                          || u.Email.Contains(k, StringComparison.OrdinalIgnoreCase)
                          || u.Department.Contains(k, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(role)) q = q.Where(u => u.Role == role);

        ViewBag.Keyword = keyword;
        ViewBag.Role = role;
        return View(q.ToList());
    }

    [HttpGet]
    public IActionResult Create() => View(new AppUser());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(AppUser model)
    {
        if (_store.UserEmailExists(model.Email))
            ModelState.AddModelError(nameof(model.Email), "อีเมลนี้มีอยู่ในระบบแล้ว");
        if (!ModelState.IsValid) return View(model);

        _store.AddUser(model);
        Success($"เพิ่มผู้ใช้ {model.FullName} เรียบร้อยแล้ว");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var u = _store.GetUser(id);
        return u is null ? NotFound() : View(u);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, AppUser model)
    {
        model.Id = id;
        if (_store.UserEmailExists(model.Email, id))
            ModelState.AddModelError(nameof(model.Email), "อีเมลนี้มีอยู่ในระบบแล้ว");
        if (!ModelState.IsValid) return View(model);

        if (!_store.UpdateUser(model)) return NotFound();
        Success($"บันทึกข้อมูลผู้ใช้ {model.FullName} เรียบร้อยแล้ว");
        return RedirectToAction(nameof(Index));
    }
}
