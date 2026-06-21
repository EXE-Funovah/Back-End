namespace Mascoteach.Service.Exceptions;

public class PaymentLinkRateLimitException : InvalidOperationException
{
    public PaymentLinkRateLimitException(int retryAfterSeconds)
        : base($"You can create at most 3 payment links within 10 minutes. Try again in {retryAfterSeconds} seconds.")
    {
        RetryAfterSeconds = retryAfterSeconds;
    }

    public int RetryAfterSeconds { get; }
}
