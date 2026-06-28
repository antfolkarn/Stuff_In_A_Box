namespace StuffInABox.Application.Common.Interfaces;

/// <summary>
/// Sends transactional email to users. The default implementation logs the message
/// (so flows work in dev without a provider); a real SMTP/SendGrid/Resend/Supabase
/// implementation plugs in behind the <c>Email:Provider</c> flag.
/// </summary>
public interface IEmailService
{
    Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken ct = default);
    Task SendEmailVerificationAsync(string toEmail, string verifyLink, CancellationToken ct = default);
}
