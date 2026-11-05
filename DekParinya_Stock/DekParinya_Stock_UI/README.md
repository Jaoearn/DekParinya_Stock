# DekParinya_Stock_UI

UI ต้นแบบระบบสต๊อก — ASP.NET Core MVC (.NET 10) + Razor Views ทำงานบนข้อมูล in-memory

## วิธีรัน

```bash
dotnet restore
dotnet run
```

เปิด http://localhost:4433 (ตั้งไว้ใน `Properties/launchSettings.json`)

บัญชีผู้ดูแล (`appsettings.json` → `AdminAccount`)

| Email | Password |
|---|---|
| admin@dekparinya.com | Admin@1234 |

## Stack

- Bootstrap 5.3.3, Bootstrap Icons 1.11.3, Chart.js 4.4.4, SweetAlert2 11.14.5 (CDN jsdelivr)
- Google Fonts: Prompt
- โลโก้: `wwwroot/logo/logo1.jpg` (sidebar, ป้ายหน้า Login, favicon)
- ClosedXML 0.104.2 (Export Excel, ฟอนต์ Tahoma, decimal → double)
- `IDataStore` / `InMemoryDataStore` (singleton, seed ด้วย `Random(42)`)
- Cookie authentication

## โครงสร้าง

| โมดูล | Controller | หมายเหตุ |
|---|---|---|
| Login | Account | ร้านขายปุ๋ย-ดิน: คลิกถุง/พิมพ์รหัส A1–C5 แล้วกด OK → ระเบิด แล้วเติมกลับใน 4 วินาที, LOGIN → ถุงหล่นก่อนเข้าระบบ |
| Dashboard | Home | การ์ดสรุป + กราฟ (Light Blue แกนเดียว) |
| Products | Products | Group / List พร้อม Create, Edit, Delete |
| Stock Requisition | StockRequisitions, StockRequisitionApproval | Add a Requisition, Approve/Reject |
| Purchase Order | PurchaseOrders | Order + Export Excel |
| Vendor Orders | ProductDemand | ใช้ Views/VendorOrders |
| Stock Adjustment | StockAdjustments, StockAdjustmentApproval | |
| Report | Reports | Stock Balance: Print + Export Excel |
| Search | Search | `/Search`, `/Search/Suggest`, Ctrl+K |
| Users | Users | Index, Create, Edit (เมนูอยู่ที่ dropdown ผู้ใช้) |

## ยังไม่อยู่ในขอบเขต

ฐานข้อมูลจริง, `[Authorize]` และสิทธิ์ตามบทบาท, ตัดสต๊อกตอน Approve, บันทึก PO และเลขที่ PO, audit log, pagination, แจ้งเตือน
