using DekParinya_Stock_UI.Models;

namespace DekParinya_Stock_UI.Services;

/// <summary>
/// ที่เก็บข้อมูลในหน่วยความจำ (Singleton) สำหรับ UI ต้นแบบ — ข้อมูลหายเมื่อรีสตาร์ทแอป
/// Seed ข้อมูลตัวอย่างด้วย Random(42) เพื่อให้ได้ผลลัพธ์เหมือนเดิมทุกครั้ง
/// </summary>
public class InMemoryDataStore : IDataStore
{
    private readonly object _lock = new();
    private readonly List<ProductGroup> _groups = new();
    private readonly List<Product> _products = new();
    private readonly List<StockRequisition> _requisitions = new();
    private readonly List<StockAdjustment> _adjustments = new();
    private readonly List<AppUser> _users = new();
    private readonly List<GoodsReceipt> _receipts = new();
    private int _grSeq;
    private int _reqSeq;
    private int _adjSeq;

    public InMemoryDataStore()
    {
        Seed();
    }

    #region Product groups

    public IReadOnlyList<ProductGroup> GetGroups()
    {
        lock (_lock) return _groups.OrderBy(g => g.Code).ToList();
    }

    public ProductGroup? GetGroup(int id)
    {
        lock (_lock) return _groups.FirstOrDefault(g => g.Id == id);
    }

    public bool GroupCodeExists(string code, int? excludeId = null)
    {
        lock (_lock)
            return _groups.Any(g => g.Id != excludeId &&
                string.Equals(g.Code, code?.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public ProductGroup AddGroup(ProductGroup group)
    {
        lock (_lock)
        {
            group.Id = _groups.Count == 0 ? 1 : _groups.Max(g => g.Id) + 1;
            group.Code = group.Code.Trim();
            group.CreatedAt = DateTime.Now;
            _groups.Add(group);
            return group;
        }
    }

    public bool UpdateGroup(ProductGroup group)
    {
        lock (_lock)
        {
            var existing = _groups.FirstOrDefault(g => g.Id == group.Id);
            if (existing is null) return false;
            existing.Code = group.Code.Trim();
            existing.Name = group.Name;
            existing.Description = group.Description;
            existing.IsActive = group.IsActive;
            return true;
        }
    }

    public bool DeleteGroup(int id, out string? error)
    {
        lock (_lock)
        {
            error = null;
            var existing = _groups.FirstOrDefault(g => g.Id == id);
            if (existing is null) { error = "ไม่พบกลุ่มสินค้า"; return false; }
            var used = _products.Count(p => p.GroupId == id);
            if (used > 0)
            {
                error = $"ไม่สามารถลบได้ เนื่องจากมีสินค้าในกลุ่มนี้ {used} รายการ";
                return false;
            }
            _groups.Remove(existing);
            return true;
        }
    }

    #endregion

    #region Products

    public IReadOnlyList<Product> GetProducts()
    {
        lock (_lock) return _products.OrderBy(p => p.Code).ToList();
    }

    public Product? GetProduct(int id)
    {
        lock (_lock) return _products.FirstOrDefault(p => p.Id == id);
    }

    public bool ProductCodeExists(string code, int? excludeId = null)
    {
        lock (_lock)
            return _products.Any(p => p.Id != excludeId &&
                string.Equals(p.Code, code?.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public Product AddProduct(Product product)
    {
        lock (_lock)
        {
            product.Id = _products.Count == 0 ? 1 : _products.Max(p => p.Id) + 1;
            product.Code = product.Code.Trim();
            product.UpdatedAt = DateTime.Now;
            _products.Add(product);
            return product;
        }
    }

    public bool UpdateProduct(Product product)
    {
        lock (_lock)
        {
            var e = _products.FirstOrDefault(p => p.Id == product.Id);
            if (e is null) return false;
            e.Code = product.Code.Trim();
            e.Name = product.Name;
            e.GroupId = product.GroupId;
            e.Uom = product.Uom;
            e.UnitPrice = product.UnitPrice;
            e.StockQty = product.StockQty;
            e.ReorderLevel = product.ReorderLevel;
            e.Vendor = product.Vendor;
            e.Description = product.Description;
            e.IsActive = product.IsActive;
            e.UpdatedAt = DateTime.Now;
            return true;
        }
    }

    public bool DeleteProduct(int id, out string? error)
    {
        lock (_lock)
        {
            error = null;
            var e = _products.FirstOrDefault(p => p.Id == id);
            if (e is null) { error = "ไม่พบสินค้า"; return false; }
            _products.Remove(e);
            return true;
        }
    }

    public IReadOnlyList<string> GetVendors()
    {
        lock (_lock)
            return _products.Select(p => p.Vendor)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!)
                .Distinct()
                .OrderBy(v => v)
                .ToList();
    }

    #endregion

    #region Requisitions

    public IReadOnlyList<StockRequisition> GetRequisitions()
    {
        lock (_lock) return _requisitions.OrderByDescending(r => r.RequestDate).ThenByDescending(r => r.Id).ToList();
    }

    public StockRequisition? GetRequisition(int id)
    {
        lock (_lock) return _requisitions.FirstOrDefault(r => r.Id == id);
    }

    public StockRequisition AddRequisition(StockRequisition requisition)
    {
        lock (_lock)
        {
            requisition.Id = _requisitions.Count == 0 ? 1 : _requisitions.Max(r => r.Id) + 1;
            requisition.DocNo = $"REQ-{requisition.RequestDate:yyMM}-{++_reqSeq:0000}";
            requisition.Status = DocStatus.Pending;
            requisition.CreatedAt = DateTime.Now;
            _requisitions.Add(requisition);
            return requisition;
        }
    }

    public bool SetRequisitionStatus(int id, DocStatus status, string actionBy, string? remark)
    {
        lock (_lock)
        {
            var r = _requisitions.FirstOrDefault(x => x.Id == id);
            if (r is null || r.Status != DocStatus.Pending) return false;
            // ต้นแบบ: ยังไม่ตัดสต๊อกตอน Approve
            r.Status = status;
            r.ActionBy = actionBy;
            r.ActionAt = DateTime.Now;
            r.ActionRemark = remark;
            return true;
        }
    }

    #endregion

    #region Adjustments

    public IReadOnlyList<StockAdjustment> GetAdjustments()
    {
        lock (_lock) return _adjustments.OrderByDescending(a => a.AdjustDate).ThenByDescending(a => a.Id).ToList();
    }

    public StockAdjustment? GetAdjustment(int id)
    {
        lock (_lock) return _adjustments.FirstOrDefault(a => a.Id == id);
    }

    public StockAdjustment AddAdjustment(StockAdjustment adjustment)
    {
        lock (_lock)
        {
            adjustment.Id = _adjustments.Count == 0 ? 1 : _adjustments.Max(a => a.Id) + 1;
            adjustment.DocNo = $"ADJ-{adjustment.AdjustDate:yyMM}-{++_adjSeq:0000}";
            adjustment.Status = DocStatus.Pending;
            adjustment.CreatedAt = DateTime.Now;
            _adjustments.Add(adjustment);
            return adjustment;
        }
    }

    public bool SetAdjustmentStatus(int id, DocStatus status, string actionBy, string? remark)
    {
        lock (_lock)
        {
            var a = _adjustments.FirstOrDefault(x => x.Id == id);
            if (a is null || a.Status != DocStatus.Pending) return false;
            a.Status = status;
            a.ActionBy = actionBy;
            a.ActionAt = DateTime.Now;
            a.ActionRemark = remark;
            return true;
        }
    }

    #endregion

    #region Goods receipts

    public IReadOnlyList<GoodsReceipt> GetGoodsReceipts()
    {
        lock (_lock) return _receipts.OrderByDescending(r => r.ReceiveDate).ThenByDescending(r => r.Id).ToList();
    }

    public GoodsReceipt? GetGoodsReceipt(int id)
    {
        lock (_lock) return _receipts.FirstOrDefault(r => r.Id == id);
    }

    public GoodsReceipt AddGoodsReceipt(GoodsReceipt receipt)
    {
        lock (_lock)
        {
            receipt.Id = _receipts.Count == 0 ? 1 : _receipts.Max(r => r.Id) + 1;
            receipt.DocNo = $"GR-{receipt.ReceiveDate:yyMM}-{++_grSeq:0000}";
            receipt.CreatedAt = DateTime.Now;
            _receipts.Add(receipt);
            return receipt;
        }
    }

    public void AddGoodsReceiptAttachments(int id, IEnumerable<GoodsReceiptAttachment> attachments)
    {
        lock (_lock) _receipts.FirstOrDefault(r => r.Id == id)?.Attachments.AddRange(attachments);
    }

    public GoodsReceiptAttachment? RemoveGoodsReceiptAttachment(int id, Guid attachmentId)
    {
        lock (_lock)
        {
            var r = _receipts.FirstOrDefault(x => x.Id == id);
            var att = r?.Attachments.FirstOrDefault(a => a.Id == attachmentId);
            if (att is not null) r!.Attachments.Remove(att);
            return att;
        }
    }

    public IReadOnlyList<string> GetGrSuppliers()
    {
        lock (_lock) return _receipts.Select(r => r.Supplier).Distinct().OrderBy(s => s).ToList();
    }

    #endregion

    #region Users

    public IReadOnlyList<AppUser> GetUsers()
    {
        lock (_lock) return _users.OrderBy(u => u.FullName).ToList();
    }

    public AppUser? GetUser(int id)
    {
        lock (_lock) return _users.FirstOrDefault(u => u.Id == id);
    }

    public bool UserEmailExists(string email, int? excludeId = null)
    {
        lock (_lock)
            return _users.Any(u => u.Id != excludeId &&
                string.Equals(u.Email, email?.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public AppUser AddUser(AppUser user)
    {
        lock (_lock)
        {
            user.Id = _users.Count == 0 ? 1 : _users.Max(u => u.Id) + 1;
            user.Email = user.Email.Trim();
            user.CreatedAt = DateTime.Now;
            _users.Add(user);
            return user;
        }
    }

    public bool UpdateUser(AppUser user)
    {
        lock (_lock)
        {
            var e = _users.FirstOrDefault(u => u.Id == user.Id);
            if (e is null) return false;
            e.FullName = user.FullName;
            e.Email = user.Email.Trim();
            e.Role = user.Role;
            e.Department = user.Department;
            e.Phone = user.Phone;
            e.IsActive = user.IsActive;
            return true;
        }
    }

    #endregion

    #region Seed

    private void Seed()
    {
        var rnd = new Random(42);
        var today = DateTime.Today;

        // ---- Groups
        var groupSeed = new (string Code, string Name, string Desc)[]
        {
            ("GRP-BEV", "เครื่องดื่ม", "น้ำดื่ม เกลือแร่ และเครื่องดื่มพร้อมดื่ม"),
            ("GRP-SUP", "อาหารเสริม", "โปรตีนและผลิตภัณฑ์เสริมอาหาร"),
            ("GRP-SNK", "ขนม/ของว่าง", "ของว่างเพื่อสุขภาพ"),
            ("GRP-ACC", "อุปกรณ์/ของใช้", "ผ้าเย็น ขวดเชค และของใช้สำหรับสมาชิก"),
            ("GRP-CLN", "อุปกรณ์ทำความสะอาด", "น้ำยาและอุปกรณ์ทำความสะอาด")
        };
        foreach (var g in groupSeed)
        {
            _groups.Add(new ProductGroup
            {
                Id = _groups.Count + 1,
                Code = g.Code,
                Name = g.Name,
                Description = g.Desc,
                IsActive = true,
                CreatedAt = today.AddDays(-rnd.Next(200, 400))
            });
        }

        // ---- Products
        const string vWater = "Crystal Water Co., Ltd.";
        const string vNutri = "Sport Nutrition Thailand";
        const string vSnack = "Healthy Snack Supply";
        const string vGear = "Gym Gear Trading";
        const string vClean = "CleanPro Distribution";

        var productSeed = new (string Name, int Group, string Uom, decimal Price, string Vendor)[]
        {
            ("น้ำดื่ม 600 มล.", 1, "ขวด", 7m, vWater),
            ("น้ำดื่ม 1.5 ลิตร", 1, "ขวด", 14m, vWater),
            ("น้ำดื่ม 600 มล. (แพ็ค 12)", 1, "แพ็ค", 72m, vWater),
            ("เครื่องดื่มเกลือแร่ 500 มล.", 1, "ขวด", 18m, vWater),
            ("Electrolyte Lemon 350 มล.", 1, "ขวด", 25m, vNutri),
            ("Energy Drink 250 มล.", 1, "ลัง", 360m, vNutri),
            ("น้ำมะพร้าว 100% 330 มล.", 1, "ขวด", 35m, vSnack),
            ("Cold Brew Coffee 250 มล.", 1, "ขวด", 45m, vSnack),
            ("Whey Protein RTD 350 มล.", 2, "ขวด", 79m, vNutri),
            ("BCAA ซอง (แพ็ค 10)", 2, "แพ็ค", 290m, vNutri),
            ("Protein Bar (แพ็ค 6)", 2, "แพ็ค", 330m, vNutri),
            ("Creatine Shot 60 มล.", 2, "ลัง", 1_200m, vNutri),
            ("อัลมอนด์อบ (แพ็ค 10)", 3, "แพ็ค", 250m, vSnack),
            ("Granola Bar (แพ็ค 12)", 3, "แพ็ค", 216m, vSnack),
            ("กล้วยอบกรอบ (ลัง 24)", 3, "ลัง", 480m, vSnack),
            ("ผ้าเย็น (แพ็ค 50)", 4, "แพ็ค", 175m, vGear),
            ("ขวดเชคโปรตีน 600 มล.", 4, "ขวด", 120m, vGear),
            ("ผ้าขนหนูออกกำลังกาย (แพ็ค 10)", 4, "แพ็ค", 450m, vGear),
            ("ถุงขยะสำหรับล็อกเกอร์ (ลัง)", 4, "ลัง", 390m, vGear),
            ("สเปรย์ฆ่าเชื้อ 500 มล.", 5, "ขวด", 89m, vClean),
            ("เจลล้างมือ 1 ลิตร", 5, "ขวด", 159m, vClean),
            ("กระดาษเช็ดมือ (แพ็ค 6)", 5, "แพ็ค", 199m, vClean),
            ("น้ำยาถูพื้น (ลัง 4 แกลลอน)", 5, "ลัง", 640m, vClean),
            ("น้ำยาเช็ดอุปกรณ์ 750 มล.", 5, "ขวด", 129m, vClean)
        };

        for (var i = 0; i < productSeed.Length; i++)
        {
            var s = productSeed[i];
            var reorder = rnd.Next(20, 80);
            int stock;
            // ทำให้มีสินค้าใกล้หมด/หมด ปนอยู่บ้าง
            var bucket = rnd.Next(0, 10);
            if (bucket == 0) stock = 0;
            else if (bucket <= 2) stock = rnd.Next(1, reorder + 1);
            else stock = rnd.Next(reorder + 5, 400);

            _products.Add(new Product
            {
                Id = i + 1,
                Code = $"P{(i + 1):0000}",
                Name = s.Name,
                GroupId = s.Group,
                Uom = s.Uom,
                UnitPrice = s.Price,
                StockQty = stock,
                ReorderLevel = reorder,
                Vendor = s.Vendor,
                Description = null,
                IsActive = rnd.Next(0, 20) != 0,
                UpdatedAt = today.AddDays(-rnd.Next(0, 30))
            });
        }

        // ---- Users
        var userSeed = new (string Name, string Email, string Role, string Dept)[]
        {
            ("Administrator", "admin@dekparinya.com", "Admin", "Management"),
            ("สมชาย ใจดี", "somchai@dekparinya.com", "Approver", "Management"),
            ("วิภาวรรณ ศรีสุข", "wipawan@dekparinya.com", "Staff", "Front Desk"),
            ("ธนากร แก้วมณี", "thanakorn@dekparinya.com", "Staff", "Fitness Floor"),
            ("ณัฐริกา พูลผล", "nattarika@dekparinya.com", "Staff", "Group Class"),
            ("ปิยะพงษ์ อินทร์แก้ว", "piyapong@dekparinya.com", "Staff", "Housekeeping"),
            ("กมลชนก รุ่งเรือง", "kamonchanok@dekparinya.com", "Approver", "Marketing")
        };
        foreach (var u in userSeed)
        {
            _users.Add(new AppUser
            {
                Id = _users.Count + 1,
                FullName = u.Name,
                Email = u.Email,
                Role = u.Role,
                Department = u.Dept,
                Phone = $"08{rnd.Next(1, 10)}-{rnd.Next(100, 999)}-{rnd.Next(1000, 9999)}",
                IsActive = true,
                CreatedAt = today.AddDays(-rnd.Next(30, 300))
            });
        }

        var requesters = _users.Where(u => u.Role == "Staff").ToList();
        var approvers = _users.Where(u => u.Role != "Staff").Select(u => u.FullName).ToList();

        // ---- Requisitions
        var events = new[]
        {
            "Group Class: Body Combat", "Member Day ประจำเดือน", "Open House สาขาใหม่",
            "Bootcamp Weekend", "Corporate Wellness Program", "เบิกใช้ประจำวัน Front Desk",
            "Fitness Challenge 30 วัน", "Yoga Morning Session", "งานแนะนำสมาชิกใหม่"
        };

        var reqDates = Enumerable.Range(0, 22).Select(_ => today.AddDays(-rnd.Next(0, 170))).OrderBy(d => d).ToList();
        foreach (var date in reqDates)
        {
            var who = requesters[rnd.Next(requesters.Count)];
            var req = new StockRequisition
            {
                Id = _requisitions.Count + 1,
                RequestDate = date,
                Requester = who.FullName,
                Department = who.Department,
                Event = events[rnd.Next(events.Length)],
                Remark = rnd.Next(0, 3) == 0 ? "ต้องการภายในวันนี้" : null,
                CreatedAt = date.AddHours(rnd.Next(8, 18))
            };
            req.DocNo = $"REQ-{date:yyMM}-{++_reqSeq:0000}";
            req.Lines = RandomLines(rnd, rnd.Next(1, 5))
                .Select(p => new StockRequisitionLine
                {
                    ProductId = p.Id, ProductCode = p.Code, ProductName = p.Name,
                    Qty = rnd.Next(1, 30), Uom = p.Uom
                }).ToList();

            var age = (today - date).TotalDays;
            var roll = rnd.Next(0, 10);
            req.Status = age < 10 && roll < 7 ? DocStatus.Pending : roll < 8 ? DocStatus.Approved : roll < 9 ? DocStatus.Rejected : DocStatus.Pending;
            if (req.Status != DocStatus.Pending)
            {
                req.ActionBy = approvers[rnd.Next(approvers.Count)];
                req.ActionAt = date.AddDays(rnd.Next(0, 3)).AddHours(rnd.Next(9, 18));
                req.ActionRemark = req.Status == DocStatus.Rejected ? "จำนวนที่ขอเกินความจำเป็น" : "อนุมัติ";
            }
            _requisitions.Add(req);
        }

        // ---- Adjustments
        var adjDates = Enumerable.Range(0, 12).Select(_ => today.AddDays(-rnd.Next(0, 120))).OrderBy(d => d).ToList();
        foreach (var date in adjDates)
        {
            var who = requesters[rnd.Next(requesters.Count)];
            var type = rnd.Next(0, 3) == 0 ? AdjustmentType.Increase : AdjustmentType.Decrease;
            var adj = new StockAdjustment
            {
                Id = _adjustments.Count + 1,
                AdjustDate = date,
                Type = type,
                Reason = type == AdjustmentType.Increase
                    ? (rnd.Next(0, 2) == 0 ? "ตรวจนับสต๊อก" : "รับคืนสินค้า")
                    : Lookups.AdjustmentReasons[rnd.Next(0, 3)],
                Requester = who.FullName,
                Remark = null,
                CreatedAt = date.AddHours(rnd.Next(8, 18))
            };
            adj.DocNo = $"ADJ-{date:yyMM}-{++_adjSeq:0000}";
            adj.Lines = RandomLines(rnd, rnd.Next(1, 4))
                .Select(p => new StockAdjustmentLine
                {
                    ProductId = p.Id, ProductCode = p.Code, ProductName = p.Name,
                    SystemQty = p.StockQty, Qty = rnd.Next(1, 12), Uom = p.Uom
                }).ToList();

            var roll = rnd.Next(0, 10);
            adj.Status = (today - date).TotalDays < 14 && roll < 7 ? DocStatus.Pending : roll < 8 ? DocStatus.Approved : DocStatus.Rejected;
            if (adj.Status != DocStatus.Pending)
            {
                adj.ActionBy = approvers[rnd.Next(approvers.Count)];
                adj.ActionAt = date.AddDays(rnd.Next(0, 3)).AddHours(rnd.Next(9, 18));
                adj.ActionRemark = adj.Status == DocStatus.Rejected ? "กรุณาตรวจนับซ้ำ" : "อนุมัติ";
            }
            _adjustments.Add(adj);
        }

        SeedGoodsReceipts(requesters.Select(u => u.FullName).ToList());
    }

    private void SeedGoodsReceipts(List<string> staff)
    {
        // ใช้ Random(42) แยกต่างหาก เพื่อไม่ให้ข้อมูลตัวอย่างชุดเดิมเปลี่ยน
        var rnd = new Random(42);
        var today = DateTime.Today;
        var suppliers = new[]
        {
            "หจก. ดินดีเกษตรภัณฑ์", "โรงงานน้ำตาลมิตรผล (กากอ้อย)", "สหกรณ์มะพร้าวบ้านแพ้ว",
            "ฟาร์มไก่ไข่ทองคำ", "บจก. ปุ๋ยอินทรีย์ไทย", "ลานมันสำปะหลังโคราช"
        };
        var materials = RawMaterials.All.ToArray();
        var dates = Enumerable.Range(0, 10).Select(_ => today.AddDays(-rnd.Next(0, 60))).OrderBy(d => d).ToList();
        foreach (var date in dates)
        {
            var gr = new GoodsReceipt
            {
                Id = _receipts.Count + 1,
                ReceiveDate = date,
                Supplier = suppliers[rnd.Next(suppliers.Length)],
                RefNo = $"DN{rnd.Next(10000, 99999)}",
                VehicleNo = $"{rnd.Next(70, 99)}-{rnd.Next(1000, 9999)}",
                ReceivedBy = staff[rnd.Next(staff.Count)],
                CreatedAt = date.AddHours(rnd.Next(7, 17))
            };
            gr.DocNo = $"GR-{date:yyMM}-{++_grSeq:0000}";
            var count = rnd.Next(1, 4);
            foreach (var m in materials.OrderBy(_ => rnd.Next()).Take(count))
            {
                var unit = RawMaterials.Units[rnd.Next(RawMaterials.Units.Length)];
                gr.Lines.Add(new GoodsReceiptLine
                {
                    Material = m,
                    Unit = unit,
                    Qty = unit == "ตัน" ? rnd.Next(2, 20) : unit == "กิโลกรัม" ? rnd.Next(100, 3000) : rnd.Next(10, 200),
                    Quality = RawMaterials.Qualities[rnd.Next(0, 10) < 8 ? 0 : rnd.Next(1, 3)]
                });
            }
            _receipts.Add(gr);
        }
    }

    private List<Product> RandomLines(Random rnd, int count) =>
        _products.OrderBy(_ => rnd.Next()).Take(count).ToList();

    #endregion
}
