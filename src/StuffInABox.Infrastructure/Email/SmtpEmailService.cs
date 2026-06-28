using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using StuffInABox.Application.Common.Interfaces;

namespace StuffInABox.Infrastructure.Email;

/// <summary>
/// Sends transactional email over SMTP via MailKit. Provider-agnostic — point
/// <c>Email:Smtp:*</c> at any SMTP service (Brevo, Resend, Mailtrap, Gmail, …) and swap
/// providers by config alone. Best-effort: a send failure is logged but never thrown, so
/// registration / password reset never break because of email. Selected via
/// <c>Email:Provider=smtp</c>.
/// </summary>
public sealed class SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger) : IEmailService
{
    public Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken ct = default) =>
        SendAsync(toEmail, "Återställ ditt lösenord – StuffInABox",
            Body("Återställ ditt lösenord",
                 "Klicka på knappen nedan för att välja ett nytt lösenord. Länken gäller i en timme.",
                 "Återställ lösenord", resetLink), ct);

    public Task SendEmailVerificationAsync(string toEmail, string verifyLink, CancellationToken ct = default) =>
        SendAsync(toEmail, "Bekräfta din e-postadress – StuffInABox",
            Body("Bekräfta din e-post",
                 "Välkommen! Bekräfta din e-postadress för att låsa upp alla funktioner. Länken gäller i 24 timmar.",
                 "Bekräfta e-post", verifyLink), ct);

    private async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct)
    {
        try
        {
            var host = config["Email:Smtp:Host"];
            if (string.IsNullOrWhiteSpace(host))
            {
                logger.LogWarning("Email:Smtp:Host is not configured — skipping email to {To}.", to);
                return;
            }

            var port = int.TryParse(config["Email:Smtp:Port"], out var p) ? p : 587;
            var user = config["Email:Smtp:User"];
            var pass = config["Email:Smtp:Password"];
            var from = config["Email:Smtp:From"] ?? user ?? "no-reply@stuffinabox.app";
            var fromName = config["Email:Smtp:FromName"] ?? "StuffInABox";
            // TLS on by default (real providers use STARTTLS on 587); only the local test
            // sink turns it off. Auto picks STARTTLS/SSL based on what the server offers.
            var useTls = !bool.TryParse(config["Email:Smtp:UseSsl"], out var ssl) || ssl;

            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(fromName, from));
            msg.To.Add(MailboxAddress.Parse(to));
            msg.Subject = subject;
            msg.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, useTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None, ct);
            if (!string.IsNullOrEmpty(user))
                await client.AuthenticateAsync(user, pass, ct);
            await client.SendAsync(msg, ct);
            await client.DisconnectAsync(true, ct);

            logger.LogInformation("Sent '{Subject}' email to {To} via SMTP {Host}:{Port}.", subject, to, host, port);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send '{Subject}' email to {To}.", subject, to);
        }
    }

    private static string Body(string heading, string intro, string buttonText, string link) =>
        $$"""
        <div style="font-family:system-ui,sans-serif;max-width:480px;margin:0 auto;padding:24px;color:#16181C">
          <h2 style="margin:0 0 12px">{{heading}}</h2>
          <p style="margin:0 0 20px;line-height:1.5;color:#42474E">{{intro}}</p>
          <p style="margin:0 0 24px">
            <a href="{{link}}" style="display:inline-block;background:#2F63E6;color:#fff;text-decoration:none;padding:11px 20px;border-radius:8px;font-weight:600">{{buttonText}}</a>
          </p>
          <p style="margin:0;font-size:12px;color:#8B9098;word-break:break-all">Om knappen inte fungerar, kopiera länken:<br>{{link}}</p>
        </div>
        """;
}
