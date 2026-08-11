using System.Text;
using System.Text.Json;
using FluentEmail.Core;
using FluentEmail.Core.Interfaces;
using FluentEmail.Core.Models;
using Microsoft.Extensions.Logging;

namespace SeraGo.API.Email;

/// <summary>
/// <see cref="ISender"/> that relays FluentEmail messages through EmailJS's
/// REST API (https://api.emailjs.com/api/v1.0/email/send).
///
/// EmailJS works purely over HTTPS to a cloud API, so it keeps working in
/// regions where SMTP/SendGrid are blocked or unreachable (e.g. Ethiopia) —
/// the email is finally delivered by whatever provider you connect to EmailJS
/// (Gmail, Outlook, ...).
///
/// It is a pass-through relay: the existing backend templates (Aufy's embedded
/// HTML confirmation + forgot-password emails) still produce the subject and
/// body; EmailJS just delivers them. The EmailJS template must be created as:
///
///   Subject:   {{subject}}
///   To Email:  {{to_email}}
///   Content:   {{{message_html}}}
///
/// (triple braces render the HTML body without escaping).
/// </summary>
public class EmailJsSender(
    EmailJsSenderOptions options,
    IHttpClientFactory httpClientFactory,
    ILogger<EmailJsSender> logger) : ISender
{
    private const string SendEndpoint = "https://api.emailjs.com/api/v1.0/email/send";

    public SendResponse Send(IFluentEmail email, CancellationToken? cancellationToken = null)
        => SendAsync(email, cancellationToken).GetAwaiter().GetResult();

    public async Task<SendResponse> SendAsync(IFluentEmail email, CancellationToken? cancellationToken = null)
    {
        var data = email.Data;
        var to = data.ToAddresses.FirstOrDefault();
        if (to is null)
        {
            return new SendResponse { ErrorMessages = ["Email has no recipient address."] };
        }

        var payload = new Dictionary<string, object>
        {
            ["service_id"] = options.ServiceId,
            ["template_id"] = options.TemplateId,
            ["user_id"] = options.PublicKey,
            ["template_params"] = new Dictionary<string, string>
            {
                ["subject"] = data.Subject ?? string.Empty,
                ["message_html"] = data.Body ?? string.Empty,
                ["to_email"] = to.EmailAddress,
                ["to_name"] = to.Name ?? to.EmailAddress,
                ["from_name"] = data.FromAddress?.Name ?? options.FromName ?? string.Empty,
                ["from_email"] = data.FromAddress?.EmailAddress ?? options.FromEmail ?? string.Empty,
            },
        };
        // Only send the access token when one is configured — an empty string
        // can be rejected by accounts with private-key restriction enabled.
        if (!string.IsNullOrWhiteSpace(options.PrivateKey))
        {
            payload["accessToken"] = options.PrivateKey;
        }

        try
        {
            using var client = httpClientFactory.CreateClient("EmailJs");
            using var request = new HttpRequestMessage(HttpMethod.Post, SendEndpoint)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"),
            };
            using var response = await client.SendAsync(request, cancellationToken ?? CancellationToken.None);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Email sent via EmailJS to {To}", to.EmailAddress);
                return new SendResponse { MessageId = to.EmailAddress };
            }

            var errorBody = await response.Content
                .ReadAsStringAsync(cancellationToken ?? CancellationToken.None);
            logger.LogError("EmailJS send failed ({Status}): {Error}",
                (int)response.StatusCode, errorBody);
            return new SendResponse
            {
                ErrorMessages = [$"EmailJS returned {(int)response.StatusCode}: {errorBody}"],
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "EmailJS send threw for {To}", to.EmailAddress);
            return new SendResponse { ErrorMessages = [ex.Message] };
        }
    }
}

/// <summary>Configuration for <see cref="EmailJsSender"/>.</summary>
public sealed class EmailJsSenderOptions
{
    public string ServiceId { get; set; } = string.Empty;
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>EmailJS account Public Key (the REST API's <c>user_id</c>).</summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>EmailJS account Private Key / REST API token (optional for testing).</summary>
    public string PrivateKey { get; set; } = string.Empty;

    public string FromName { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
}
