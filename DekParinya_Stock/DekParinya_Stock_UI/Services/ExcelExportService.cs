using ClosedXML.Excel;
using DekParinya_Stock_UI.Models;

namespace DekParinya_Stock_UI.Services;

/// <summary>
/// Export Excel ด้วย ClosedXML — ค่า decimal ต้อง cast เป็น double ก่อนใส่ลง cell, ฟอนต์ใช้ Tahoma
/// </summary>
public static class ExcelExportService
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static readonly XLColor Red = XLColor.FromHtml("#E31936");
    private static readonly XLColor DarkGrey = XLColor.FromHtml("#333F48");
    private static readonly XLColor LightGrey = XLColor.FromHtml("#F1F2F2");
    private static readonly XLColor Amber = XLColor.FromHtml("#FFF3D6");
    private static readonly XLColor RedTint = XLColor.FromHtml("#FDE3E7");

    private static IXLWorksheet NewSheet(XLWorkbook wb, string name)
    {
        var ws = wb.Worksheets.Add(name);
        ws.Style.Font.FontName = "Tahoma";
        ws.Style.Font.FontSize = 10;
        return ws;
    }

    private static void StyleHeader(IXLRange header)
    {
        header.Style.Font.Bold = true;
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Fill.BackgroundColor = DarkGrey;
        header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    private static void StyleTable(IXLRange range)
    {
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.OutsideBorderColor = XLColor.FromHtml("#C4C6C7");
        range.Style.Border.InsideBorderColor = XLColor.FromHtml("#C4C6C7");
    }

    private static byte[] Save(XLWorkbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public static byte[] StockBalance(IReadOnlyList<StockBalanceRow> rows, string filterText)
    {
        using var wb = new XLWorkbook();
        var ws = NewSheet(wb, "Stock Balance");

        ws.Cell(1, 1).Value = "DekParinya — Stock Balance Report";
        ws.Range(1, 1, 1, 10).Merge();
        ws.Cell(1, 1).Style.Font.FontSize = 16;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontColor = Red;

        ws.Cell(2, 1).Value = $"พิมพ์เมื่อ: {DateTime.Now:dd/MM/yyyy HH:mm}   {filterText}";
        ws.Range(2, 1, 2, 10).Merge();
        ws.Cell(2, 1).Style.Font.FontColor = DarkGrey;

        var headers = new[] { "#", "Product Code", "Product Name", "Group", "UOM", "Stock Qty", "Reorder Level", "Unit Price", "Stock Value", "Status" };
        const int headerRow = 4;
        for (var c = 0; c < headers.Length; c++) ws.Cell(headerRow, c + 1).Value = headers[c];
        StyleHeader(ws.Range(headerRow, 1, headerRow, headers.Length));

        var r = headerRow + 1;
        var i = 1;
        foreach (var row in rows)
        {
            ws.Cell(r, 1).Value = i++;
            ws.Cell(r, 2).Value = row.Code;
            ws.Cell(r, 3).Value = row.Name;
            ws.Cell(r, 4).Value = row.Group;
            ws.Cell(r, 5).Value = row.Uom;
            ws.Cell(r, 6).Value = row.Stock;
            ws.Cell(r, 7).Value = row.Reorder;
            ws.Cell(r, 8).Value = (double)row.UnitPrice;
            ws.Cell(r, 9).Value = (double)row.Value;
            ws.Cell(r, 10).Value = row.State switch
            {
                StockState.Out => "หมด",
                StockState.Low => "ใกล้หมด",
                _ => "ปกติ"
            };
            if (row.State == StockState.Low) ws.Range(r, 1, r, 10).Style.Fill.BackgroundColor = Amber;
            if (row.State == StockState.Out) ws.Range(r, 1, r, 10).Style.Fill.BackgroundColor = RedTint;
            r++;
        }

        // Totals
        ws.Cell(r, 1).Value = "รวม";
        ws.Range(r, 1, r, 5).Merge();
        ws.Cell(r, 6).Value = rows.Sum(x => x.Stock);
        ws.Cell(r, 9).Value = (double)rows.Sum(x => x.Value);
        var totalRange = ws.Range(r, 1, r, 10);
        totalRange.Style.Font.Bold = true;
        totalRange.Style.Fill.BackgroundColor = LightGrey;

        StyleTable(ws.Range(headerRow, 1, r, headers.Length));
        ws.Range(headerRow + 1, 6, r, 7).Style.NumberFormat.Format = "#,##0";
        ws.Range(headerRow + 1, 8, r, 9).Style.NumberFormat.Format = "#,##0.00";
        ws.Range(headerRow + 1, 1, r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range(headerRow + 1, 10, r, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Columns().AdjustToContents();
        ws.Column(3).Width = Math.Max(ws.Column(3).Width, 34);
        ws.SheetView.FreezeRows(headerRow);
        return Save(wb);
    }

    public static byte[] PurchaseOrder(PurchaseOrderViewModel po, IReadOnlyList<(Product Product, PurchaseOrderLineInput Line)> lines)
    {
        using var wb = new XLWorkbook();
        var ws = NewSheet(wb, "Purchase Order");

        ws.Cell(1, 1).Value = "PURCHASE ORDER";
        ws.Range(1, 1, 1, 7).Merge();
        ws.Cell(1, 1).Style.Font.FontSize = 18;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontColor = Red;

        ws.Cell(2, 1).Value = "DekParinya Stock — เอกสารต้นแบบ (ยังไม่มีเลขที่ PO)";
        ws.Range(2, 1, 2, 7).Merge();
        ws.Cell(2, 1).Style.Font.FontColor = DarkGrey;

        var info = new (string Label, string Value)[]
        {
            ("Vendor", po.Vendor),
            ("Order Date", po.OrderDate.ToString("dd/MM/yyyy")),
            ("Expected Date", po.ExpectedDate?.ToString("dd/MM/yyyy") ?? "-"),
            ("Contact Person", po.ContactName ?? "-"),
            ("Deliver To", po.DeliverTo ?? "-"),
            ("Remark", po.Remark ?? "-")
        };
        var r = 4;
        foreach (var (label, value) in info)
        {
            ws.Cell(r, 1).Value = label;
            ws.Cell(r, 1).Style.Font.Bold = true;
            ws.Cell(r, 2).Value = value;
            ws.Range(r, 2, r, 7).Merge();
            r++;
        }

        r++;
        var headerRow = r;
        var headers = new[] { "#", "Product Code", "Product Name", "UOM", "Qty", "Unit Price", "Amount" };
        for (var c = 0; c < headers.Length; c++) ws.Cell(headerRow, c + 1).Value = headers[c];
        StyleHeader(ws.Range(headerRow, 1, headerRow, headers.Length));

        r = headerRow + 1;
        var i = 1;
        decimal total = 0;
        foreach (var (product, line) in lines)
        {
            var amount = line.Qty * line.UnitPrice;
            total += amount;
            ws.Cell(r, 1).Value = i++;
            ws.Cell(r, 2).Value = product.Code;
            ws.Cell(r, 3).Value = product.Name + (string.IsNullOrWhiteSpace(line.Remark) ? "" : $" ({line.Remark})");
            ws.Cell(r, 4).Value = line.Uom ?? product.Uom;
            ws.Cell(r, 5).Value = line.Qty;
            ws.Cell(r, 6).Value = (double)line.UnitPrice;
            ws.Cell(r, 7).Value = (double)amount;
            r++;
        }

        var vat = Math.Round(total * 0.07m, 2);
        var summary = new (string Label, decimal Value)[] { ("Subtotal", total), ("VAT 7%", vat), ("Grand Total", total + vat) };
        var firstSummaryRow = r;
        foreach (var (label, value) in summary)
        {
            ws.Cell(r, 6).Value = label;
            ws.Cell(r, 6).Style.Font.Bold = true;
            ws.Cell(r, 7).Value = (double)value;
            r++;
        }
        ws.Range(r - 1, 6, r - 1, 7).Style.Font.FontColor = Red;
        ws.Range(r - 1, 6, r - 1, 7).Style.Fill.BackgroundColor = LightGrey;

        StyleTable(ws.Range(headerRow, 1, firstSummaryRow - 1, headers.Length));
        StyleTable(ws.Range(firstSummaryRow, 6, r - 1, 7));
        ws.Range(headerRow + 1, 5, r, 5).Style.NumberFormat.Format = "#,##0";
        ws.Range(headerRow + 1, 6, r, 7).Style.NumberFormat.Format = "#,##0.00";
        ws.Range(headerRow + 1, 1, r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        r += 2;
        ws.Cell(r, 2).Value = "ผู้สั่งซื้อ ____________________";
        ws.Cell(r, 6).Value = "ผู้อนุมัติ ____________________";

        ws.Columns().AdjustToContents();
        ws.Column(1).Width = 16;
        ws.Column(3).Width = Math.Max(ws.Column(3).Width, 36);
        return Save(wb);
    }
}
