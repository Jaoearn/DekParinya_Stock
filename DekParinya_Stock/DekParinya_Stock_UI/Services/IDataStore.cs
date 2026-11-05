using DekParinya_Stock_UI.Models;

namespace DekParinya_Stock_UI.Services;

public interface IDataStore
{
    // Product groups
    IReadOnlyList<ProductGroup> GetGroups();
    ProductGroup? GetGroup(int id);
    bool GroupCodeExists(string code, int? excludeId = null);
    ProductGroup AddGroup(ProductGroup group);
    bool UpdateGroup(ProductGroup group);
    bool DeleteGroup(int id, out string? error);

    // Products
    IReadOnlyList<Product> GetProducts();
    Product? GetProduct(int id);
    bool ProductCodeExists(string code, int? excludeId = null);
    Product AddProduct(Product product);
    bool UpdateProduct(Product product);
    bool DeleteProduct(int id, out string? error);
    IReadOnlyList<string> GetVendors();

    // Stock requisitions
    IReadOnlyList<StockRequisition> GetRequisitions();
    StockRequisition? GetRequisition(int id);
    StockRequisition AddRequisition(StockRequisition requisition);
    bool SetRequisitionStatus(int id, DocStatus status, string actionBy, string? remark);

    // Stock adjustments
    IReadOnlyList<StockAdjustment> GetAdjustments();
    StockAdjustment? GetAdjustment(int id);
    StockAdjustment AddAdjustment(StockAdjustment adjustment);
    bool SetAdjustmentStatus(int id, DocStatus status, string actionBy, string? remark);

    // Goods receipts (GR) — รับเข้าวัตถุดิบ ดิน / กาก / ขุย
    IReadOnlyList<GoodsReceipt> GetGoodsReceipts();
    GoodsReceipt? GetGoodsReceipt(int id);
    GoodsReceipt AddGoodsReceipt(GoodsReceipt receipt);
    void AddGoodsReceiptAttachments(int id, IEnumerable<GoodsReceiptAttachment> attachments);
    GoodsReceiptAttachment? RemoveGoodsReceiptAttachment(int id, Guid attachmentId);
    IReadOnlyList<string> GetGrSuppliers();

    // Users
    IReadOnlyList<AppUser> GetUsers();
    AppUser? GetUser(int id);
    bool UserEmailExists(string email, int? excludeId = null);
    AppUser AddUser(AppUser user);
    bool UpdateUser(AppUser user);
}
