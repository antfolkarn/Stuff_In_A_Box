using Microsoft.Extensions.Logging;
using StuffInABox.Application.Common.Interfaces;

namespace StuffInABox.Infrastructure.Email;

/// <summary>
/// Default email "sender" that just logs the message. Lets password-reset (and other
/// email flows) work end-to-end without a configured provider — the reset link shows
/// up in the logs. Swap for a real provider in production via <c>Email:Provider</c>.
/// </summary>
public sealed class LoggingEmailService(ILogger<LoggingEmailService> logger) : IEmailService
{
    public Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[EMAIL/dev] Password reset for {Email}. No email provider configured — reset link: {ResetLink}",
            toEmail, resetLink);
        return Task.CompletedTask;
    }

    public Task SendEmailVerificationAsync(string toEmail, string verifyLink, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[EMAIL/dev] Email verification for {Email}. No email provider configured — verify link: {VerifyLink}",
            toEmail, verifyLink);
        return Task.CompletedTask;
    }
}
