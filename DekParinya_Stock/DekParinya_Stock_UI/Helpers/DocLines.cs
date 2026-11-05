using DekParinya_Stock_UI.Models;
using DekParinya_Stock_UI.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DekParinya_Stock_UI.Helpers;

public static class DocLines
{
    /// <summary>
    /// ตรวจรายการสินค้าในเอกสาร: ต้องมีอย่างน้อย 1 รายการ, Qty > 0, UOM ถูกต้อง
    /// (ไม่ตรวจว่าจำนวนเกินยอดคงเหลือ ตามขอบเขตต้นแบบ)
    /// </summary>
    public static List<(Product Product, DocLineInput Line)> Validate(
        List<DocLineInput>? lines, IDataStore store, ModelStateDictionary modelState)
    {
        var result = new List<(Product, DocLineInput)>();
        var input = (lines ?? new List<DocLineInput>()).Where(l => l.ProductId > 0).ToList();

        if (input.Count == 0)
        {
            modelState.AddModelError(string.Empty, "กรุณาเพิ่มรายการสินค้าอย่างน้อย 1 รายการ");
            return result;
        }

        var lineNo = 0;
        foreach (var line in input)
        {
            lineNo++;
            var product = store.GetProduct(line.ProductId);
            if (product is null)
            {
                modelState.AddModelError(string.Empty, $"รายการที่ {lineNo}: ไม่พบสินค้า");
                continue;
            }
            if (line.Qty <= 0)
                modelState.AddModelError(string.Empty, $"รายการที่ {lineNo} ({product.Name}): จำนวนต้องมากกว่า 0");
            if (string.IsNullOrWhiteSpace(line.Uom) || !Lookups.Uoms.Contains(line.Uom))
                line.Uom = product.Uom;
            result.Add((product, line));
        }
        return result;
    }
}
