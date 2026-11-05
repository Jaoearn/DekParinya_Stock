using System.Security.Claims;
using DekParinya_Stock_UI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace DekParinya_Stock_UI.Controllers;

public class AccountController : Controller
{
    private readonly IConfiguration _config;

    public AccountController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet]
    public IActionResult Index() => RedirectToAction(nameof(Login));

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return LoginView(new LoginViewModel { ReturnUrl = returnUrl });
    }

    /// <summary>
    /// เลือกหน้า Login: appsettings "LoginPage" = "V2" (ใหม่ ไม่มีโลโก้) หรือ "Classic" (แบบเดิมมีโลโก้)
    /// ดูตัวอย่างได้ด้วย ?ui=classic หรือ ?ui=v2
    /// </summary>
    private IActionResult LoginView(LoginViewModel model)
    {
        var ui = Request.Query["ui"].ToString();
        if (string.IsNullOrEmpty(ui) && Request.HasFormContentType) ui = Request.Form["ui"].ToString();
        if (string.IsNullOrEmpty(ui)) ui = _config["LoginPage"] ?? "Classic";
        ViewData["LoginUi"] = ui;
        return View(ui.Equals("v2", StringComparison.OrdinalIgnoreCase) ? "LoginV2" : "Login", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return LoginView(model);

        var adminEmail = _config["AdminAccount:Email"] ?? string.Empty;
        var adminPassword = _config["AdminAccount:Password"] ?? string.Empty;
        var displayName = _config["AdminAccount:DisplayName"] ?? "Administrator";

        var ok = string.Equals(model.Email.Trim(), adminEmail, StringComparison.OrdinalIgnoreCase)
                 && model.Password == adminPassword;

        if (!ok)
        {
            ModelState.AddModelError(string.Empty, "อีเมลหรือรหัสผ่านไม่ถูกต้อง");
            return LoginView(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, displayName),
            new(ClaimTypes.Email, adminEmail),
            new(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
            });

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
