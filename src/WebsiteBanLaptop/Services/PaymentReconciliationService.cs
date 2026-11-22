using Microsoft.EntityFrameworkCore;
using WebsiteBanLaptop.Data;

namespace WebsiteBanLaptop.Services;

public sealed class PaymentReconciliationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<PaymentReconciliationService> _logger;
    public PaymentReconciliationService(AppDbContext db, ILogger<PaymentReconciliationService> logger)
    {
        _db = db; _logger = logger;
    }

    public async Task<PaymentApplyResult> ApplyAsync(PaymentProof proof, CancellationToken token)
    {
        if (proof.AmountVnd <= 0 || string.IsNullOrEmpty(proof.Reference))
            return PaymentApplyResult.WrongAmount;
        await using var transaction = await _db.Database.BeginTransactionAsync(token);
        var order = await _db.Orders.AsNoTracking().Include(x => x.OrderDetails)
            .FirstOrDefaultAsync(x => x.PaymentReference == proof.Reference && x.PaymentMethod == proof.Provider, token);
        if (order == null) return PaymentApplyResult.NotFound;
        if (order.TotalAmount != proof.AmountVnd) return PaymentApplyResult.WrongAmount;
        if (proof.Succeeded is null) return PaymentApplyResult.AwaitingCapture;
        var succeeded = proof.Succeeded.Value;
        if (order.PaymentStatus is "Paid" or "Failed")
            return order.PaymentStatus == (succeeded ? "Paid" : "Failed") &&
                (order.PaymentTransactionId ?? "") == proof.TransactionId
                ? PaymentApplyResult.Duplicate : PaymentApplyResult.ConflictingState;
        if (order.PaymentStatus != "Pending" || order.Status != "Chờ thanh toán")
            return PaymentApplyResult.ConflictingState;
        var newState = succeeded ? "Paid" : "Failed";
        var newOrderStatus = succeeded ? "Chờ xác nhận" : "Đã hủy";
        var affected = await _db.Orders
            .Where(x => x.OrderID == order.OrderID && x.Status == "Chờ thanh toán" && x.PaymentStatus == "Pending")
            .ExecuteUpdateAsync(up => up
                .SetProperty(x => x.PaymentStatus, newState)
                .SetProperty(x => x.Status, newOrderStatus)
                .SetProperty(x => x.PaymentTransactionId, proof.TransactionId)
                .SetProperty(x => x.PaymentUpdatedAtUtc, DateTime.UtcNow), token);
        if (affected != 1) return PaymentApplyResult.Duplicate;
        if (!succeeded)
        {
            foreach (var item in order.OrderDetails)
            {
                await _db.Products.Where(x => x.ProductID == item.ProductID)
                    .ExecuteUpdateAsync(up => up.SetProperty(x => x.Stock, x => x.Stock + item.Quantity), token);
            }
        }
        await transaction.CommitAsync(token);
        _logger.LogInformation("Payment {Provider} reference {Reference} changed to {State}", proof.Provider,
            proof.Reference, newState);
        return PaymentApplyResult.Applied;
    }
}
