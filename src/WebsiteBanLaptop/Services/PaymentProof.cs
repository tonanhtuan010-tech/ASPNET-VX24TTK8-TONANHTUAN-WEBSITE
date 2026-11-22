namespace WebsiteBanLaptop.Services;

public sealed record PaymentProof(string Provider, string Reference, long AmountVnd,
    bool? Succeeded, string TransactionId);

public enum PaymentApplyResult
{
    Applied, Duplicate, NotFound, WrongAmount, ConflictingState, AwaitingCapture
}
