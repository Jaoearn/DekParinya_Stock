using DekParinya_Stock_UI.Models;
using DekParinya_Stock_UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DekParinya_Stock_UI.Controllers;

public class ProductsController : AppControllerBase
{
    private readonly IDataStore _store;

    public ProductsController(IDataStore store)
    {
        _store = store;
    }

    // ===================== Product Group =====================

    [HttpGet]
    public IActionResult ProductGroup(string? keyword, string? status)
    {
        IEnumerable<ProductGroup> q = _store.GetGroups();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            q = q.Where(g => g.Code.Contains(k, StringComparison.OrdinalIgnoreCase)
                          || g.Name.Contains(k, StringComparison.OrdinalIgnoreCase)
                          || (g.Description ?? "").Contains(k, StringComparison.OrdinalIgnoreCase));
        }
        if (status == "active") q = q.Where(g => g.IsActive);
        if (status == "inactive") q = q.Where(g => !g.IsActive);

        ViewBag.Keyword = keyword;
        ViewBag.Status = status;
        return View(q.ToList());
    }

    [HttpGet]
    public IActionResult CreateProductGroups() => View(new ProductGroup());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateProductGroups(ProductGroup model)
    {
        if (_store.GroupCodeExists(model.Code))
            ModelState.AddModelError(nameof(model.Code), "รหัสกลุ่มสินค้านี้มีอยู่แล้ว");
        if (!ModelState.IsValid) return View(model);

        _store.AddGroup(model);
        Success($"เพิ่มกลุ่มสินค้า {model.Name} เรียบร้อยแล้ว");
        return RedirectToAction(nameof(ProductGroup));
    }

    [HttpGet]
    public IActionResult EditProductGroup(int id)
    {
        var g = _store.GetGroup(id);
        return g is null ? NotFound() : View(g);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditProductGroup(int id, ProductGroup model)
    {
        model.Id = id;
        if (_store.GroupCodeExists(model.Code, id))
            ModelState.AddModelError(nameof(model.Code), "รหัสกลุ่มสินค้านี้มีอยู่แล้ว");
        if (!ModelState.IsValid) return View(model);

        if (!_store.UpdateGroup(model)) return NotFound();
        Success($"บันทึกกลุ่มสินค้า {model.Name} เรียบร้อยแล้ว");
        return RedirectToAction(nameof(ProductGroup));
    }

    [HttpGet]
    public IActionResult DeleteProductGroup(int id)
    {
        var g = _store.GetGroup(id);
        if (g is null) return NotFound();
        ViewBag.ProductCount = _store.GetProducts().Count(p => p.GroupId == id);
        return View(g);
    }

    [HttpPost, ActionName("DeleteProductGroup")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteProductGroupConfirmed(int id)
    {
        var g = _store.GetGroup(id);
        if (_store.DeleteGroup(id, out var groupError))
        {
            Success($"ลบกลุ่มสินค้า {g?.Name} เรียบร้อยแล้ว");
            return RedirectToAction(nameof(ProductGroup));
        }
        Error(groupError ?? "ไม่สามารถลบได้");
        return RedirectToAction(nameof(DeleteProductGroup), new { id });
    }

    // ===================== Product List =====================

    [HttpGet]
    public IActionResult ProductList(string? keyword, int? groupId, string? stock)
    {
        IEnumerable<Product> q = _store.GetProducts();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            q = q.Where(p => p.Code.Contains(k, StringComparison.OrdinalIgnoreCase)
                          || p.Name.Contains(k, StringComparison.OrdinalIgnoreCase)
                          || (p.Vendor ?? "").Contains(k, StringComparison.OrdinalIgnoreCase));
        }
        if (groupId is > 0) q = q.Where(p => p.GroupId == groupId);
        q = stock switch
        {
            "Normal" => q.Where(p => p.State == StockState.Normal),
            "Low" => q.Where(p => p.State == StockState.Low),
            "Out" => q.Where(p => p.State == StockState.Out),
            _ => q
        };

        ViewBag.Keyword = keyword;
        ViewBag.GroupId = groupId;
        ViewBag.Stock = stock;
        return View(q.ToList());
    }

    [HttpGet]
    public IActionResult CreateProductList(int? groupId)
    {
        var next = _store.GetProducts().Count + 1;
        return View(new Product
        {
            Code = $"P{next:0000}",
            GroupId = groupId ?? 0,
            Uom = Lookups.Uoms[0],
            ReorderLevel = 20
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateProductList(Product model)
    {
        if (_store.ProductCodeExists(model.Code))
            ModelState.AddModelError(nameof(model.Code), "รหัสสินค้านี้มีอยู่แล้ว");
        if (!Lookups.Uoms.Contains(model.Uom))
            ModelState.AddModelError(nameof(model.Uom), "หน่วยนับไม่ถูกต้อง");
        if (!ModelState.IsValid) return View(model);

        _store.AddProduct(model);
        Success($"เพิ่มสินค้า {model.Name} เรียบร้อยแล้ว");
        return RedirectToAction(nameof(ProductList));
    }

    [HttpGet]
    public IActionResult EditProductList(int id)
    {
        var p = _store.GetProduct(id);
        return p is null ? NotFound() : View(p);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditProductList(int id, Product model)
    {
        model.Id = id;
        if (_store.ProductCodeExists(model.Code, id))
            ModelState.AddModelError(nameof(model.Code), "รหัสสินค้านี้มีอยู่แล้ว");
        if (!Lookups.Uoms.Contains(model.Uom))
            ModelState.AddModelError(nameof(model.Uom), "หน่วยนับไม่ถูกต้อง");
        if (!ModelState.IsValid) return View(model);

        if (!_store.UpdateProduct(model)) return NotFound();
        Success($"บันทึกสินค้า {model.Name} เรียบร้อยแล้ว");
        return RedirectToAction(nameof(ProductList));
    }

    [HttpGet]
    public IActionResult DeleteProductList(int id)
    {
        var p = _store.GetProduct(id);
        return p is null ? NotFound() : View(p);
    }

    [HttpPost, ActionName("DeleteProductList")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteProductListConfirmed(int id)
    {
        var p = _store.GetProduct(id);
        if (_store.DeleteProduct(id, out var productError))
        {
            Success($"ลบสินค้า {p?.Name} เรียบร้อยแล้ว");
            return RedirectToAction(nameof(ProductList));
        }
        Error(productError ?? "ไม่สามารถลบได้");
        return RedirectToAction(nameof(ProductList));
    }
}
