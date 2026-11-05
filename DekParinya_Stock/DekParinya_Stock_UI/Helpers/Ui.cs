using System.Globalization;
using DekParinya_Stock_UI.Models;
using Microsoft.AspNetCore.Html;

namespace DekParinya_Stock_UI.Helpers;

public static class Ui
{
    private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");

    public static IHtmlContent StatusBadge(DocStatus status) => status switch
    {
        DocStatus.Approved => new HtmlString("<span class=\"badge-status status-approved\">Approved</span>"),
        DocStatus.Rejected => new HtmlString("<span class=\"badge-status status-rejected\">Rejected</span>"),
        _ => new HtmlString("<span class=\"badge-status status-pending\">Pending</span>")
    };

    public static IHtmlContent StockBadge(StockState state) => state switch
    {
        StockState.Out => new HtmlString("<span class=\"badge-status status-rejected\">หมด</span>"),
        StockState.Low => new HtmlString("<span class=\"badge-status status-pending\">ใกล้หมด</span>"),
        _ => new HtmlString("<span class=\"badge-status status-normal\">ปกติ</span>")
    };

    public static IHtmlContent AdjustTypeBadge(AdjustmentType type) => type == AdjustmentType.Increase
        ? new HtmlString("<span class=\"adj-type adj-inc\"><i class=\"bi bi-plus-circle\"></i> Increase</span>")
        : new HtmlString("<span class=\"adj-type adj-dec\"><i class=\"bi bi-dash-circle\"></i> Decrease</span>");

    public static string Date(DateTime d) => d.ToString("dd/MM/yyyy", En);
    public static string Date(DateTime? d) => d.HasValue ? Date(d.Value) : "-";
    public static string DateTime(DateTime? d) => d.HasValue ? d.Value.ToString("dd/MM/yyyy HH:mm", En) : "-";
    public static string Money(decimal v) => "฿" + v.ToString("N2", En);
    public static string Num(int v) => v.ToString("N0", En);
    public static string Num(decimal v) => v.ToString("N0", En);
    public static string Qty(decimal v) => v.ToString("#,##0.##", En);
    public static string Iso(DateTime d) => d.ToString("yyyy-MM-dd", En);
    public static string Iso(DateTime? d) => d.HasValue ? Iso(d.Value) : string.Empty;
}
