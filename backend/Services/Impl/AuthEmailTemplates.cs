using System.Net;

namespace AdvancedOrderSystem.Services.Impl;

public record EmailContent(string Subject, string HtmlBody, string TextBody);

public static class AuthEmailTemplates
{
    public static EmailContent ConfirmEmail(string fullName, string link) =>
        Build(
            subject: "Confirm your ShoppyAI account",
            greeting: $"Hi {fullName},",
            intro: "Confirm your email address to finish setting up your account.",
            buttonText: "Confirm email",
            link: link,
            footer: "The link expires in 24 hours. If you did not create this account, you can ignore this email.");

    public static EmailContent ResetPassword(string fullName, string link) =>
        Build(
            subject: "Reset your ShoppyAI password",
            greeting: $"Hi {fullName},",
            intro: "We received a request to reset your password.",
            buttonText: "Reset password",
            link: link,
            footer: "The link expires in 1 hour. If you did not request this, you can ignore this email and your password stays unchanged.");

    public static EmailContent PasswordChanged(string fullName) =>
        Build(
            subject: "Your ShoppyAI password was changed",
            greeting: $"Hi {fullName},",
            intro: "Your password was just changed and every other session was signed out.",
            buttonText: null,
            link: null,
            footer: "If this wasn't you, reset your password immediately and contact your administrator.");

    private static EmailContent Build(
        string subject,
        string greeting,
        string intro,
        string? buttonText,
        string? link,
        string footer)
    {
        var button = buttonText is null || link is null
            ? string.Empty
            : $"""
               <p style="margin:24px 0;">
                 <a href="{WebUtility.HtmlEncode(link)}" style="background:#4f46e5;color:#ffffff;padding:12px 20px;border-radius:8px;text-decoration:none;display:inline-block;font-weight:600;">{WebUtility.HtmlEncode(buttonText)}</a>
               </p>
               <p style="color:#64748b;font-size:13px;">Or paste this link into your browser:<br>{WebUtility.HtmlEncode(link)}</p>
               """;

        var htmlBody = $"""
            <div style="font-family:system-ui,-apple-system,'Segoe UI',sans-serif;color:#0f172a;max-width:560px;">
              <h2 style="margin:0 0 16px;">ShoppyAI</h2>
              <p>{WebUtility.HtmlEncode(greeting)}</p>
              <p>{WebUtility.HtmlEncode(intro)}</p>
              {button}
              <hr style="border:none;border-top:1px solid #e2e8f0;margin:24px 0;">
              <p style="color:#64748b;font-size:13px;">{WebUtility.HtmlEncode(footer)}</p>
            </div>
            """;

        var textBody = link is null
            ? $"{greeting}\n\n{intro}\n\n{footer}"
            : $"{greeting}\n\n{intro}\n\n{link}\n\n{footer}";

        return new EmailContent(subject, htmlBody, textBody);
    }
}
