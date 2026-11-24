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
               <p style="margin:24px 0 16px;">
                 <a href="{WebUtility.HtmlEncode(link)}" style="display:inline-block;background-color:#b93815;color:#ffffff;padding:12px 22px;border-radius:8px;text-decoration:none;font-weight:700;">{WebUtility.HtmlEncode(buttonText)}</a>
               </p>
               <p style="margin:0;font-size:13px;color:#57534e;">
                 Button not working? <a href="{WebUtility.HtmlEncode(link)}" style="color:#b93815;text-decoration:underline;">Open the link in your browser</a>.
               </p>
               """;

        var htmlBody = $"""
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background-color:#fff4f1;font-family:Arial,sans-serif;">
              <tr>
                <td align="center" style="padding:24px 12px;">
                  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:560px;background-color:#ffffff;border:1px solid #ffe4dd;border-radius:12px;">
                    <tr>
                      <td style="padding:20px 24px;background-color:#ffe4dd;color:#b93815;font-size:18px;font-weight:700;border-radius:12px 12px 0 0;">
                        ShoppyAI
                      </td>
                    </tr>
                    <tr>
                      <td style="padding:24px;color:#1c1917;font-size:16px;line-height:1.5;">
                        <h1 style="margin:0 0 16px;font-size:24px;line-height:1.25;">{WebUtility.HtmlEncode(subject)}</h1>
                        <p style="margin:0 0 12px;">{WebUtility.HtmlEncode(greeting)}</p>
                        <p style="margin:0;">{WebUtility.HtmlEncode(intro)}</p>
                        {button}
                      </td>
                    </tr>
                    <tr>
                      <td style="padding:16px 24px;border-top:1px solid #ffe4dd;color:#57534e;font-size:13px;line-height:1.5;">
                        {WebUtility.HtmlEncode(footer)}
                      </td>
                    </tr>
                  </table>
                </td>
              </tr>
            </table>
            """;

        var textBody = link is null
            ? $"{greeting}\n\n{intro}\n\n{footer}"
            : $"{greeting}\n\n{intro}\n\n{link}\n\n{footer}";

        return new EmailContent(subject, htmlBody, textBody);
    }
}
