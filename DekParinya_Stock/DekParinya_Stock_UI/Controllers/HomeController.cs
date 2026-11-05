using System.Globalization;
using DekParinya_Stock_UI.Models;
using DekParinya_Stock_UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DekParinya_Stock_UI.Controllers;

public class HomeController : AppControllerBase
{
    private readonly IDataStore _store;

    public HomeController(IDataStore store)
    {
        _store = store;
    }

    public IActionResult Index()
    {
        var products = _store.GetProducts();
        var groups = _store.GetGroups();
        var requisitions = _store.GetRequisitions();
        var adjustments = _store.GetAdjustments();

        var vm = new DashboardViewModel
        {
            TotalProducts = products.Count,
            TotalGroups = groups.Count,
            TotalStockQty = products.Sum(p => p.StockQty),
            StockValue = products.Sum(p => p.StockQty * p.UnitPrice),
            LowStockCount = products.Count(p => p.State == StockState.Low),
            OutOfStockCount = products.Count(p => p.State == StockState.Out),
            PendingRequisitions = requisitions.Count(r => r.Status == DocStatus.Pending),
            PendingAdjustments = adjustments.Count(a => a.Status == DocStatus.Pending),
            LowStockItems = products.Where(p => p.State != StockState.Normal)
                .OrderBy(p => p.StockQty).Take(6).ToList(),
            RecentRequisitions = requisitions.Take(6).ToList()
        };

        foreach (var g in groups)
        {
            vm.GroupLabels.Add(g.Name);
            vm.GroupStock.Add(products.Where(p => p.GroupId == g.Id).Sum(p => p.StockQty));
        }

        var en = CultureInfo.GetCultureInfo("en-US");
        var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-5);
        for (var i = 0; i < 6; i++)
        {
            var m = start.AddMonths(i);
            vm.MonthLabels.Add(m.ToString("MMM yy", en));
            vm.MonthRequisitionQty.Add(requisitions
                .Where(r => r.RequestDate.Year == m.Year && r.RequestDate.Month == m.Month && r.Status != DocStatus.Rejected)
                .Sum(r => r.TotalQty));
        }

        return View(vm);
    }

    public IActionResult Error() => View();
}
