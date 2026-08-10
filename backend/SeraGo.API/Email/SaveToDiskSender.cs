using FluentEmail.Core;
using FluentEmail.Core.Interfaces;
using FluentEmail.Core.Models;

namespace SeraGo.API.Email;

/// <summary>
/// Dev-only ISender that writes every email to a folder on disk (no SMTP needed).
/// Enabled when "FluentEmail:SaveEmailsOnDisk" is set — remove it and configure
/// SMTP (FluentEmail:Smtp*) to send real emails.
/// </summary>
public class SaveToDiskSender(string directory) : ISender
{
    public SendResponse Send(IFluentEmail email, CancellationToken? cancellationToken = null)
    {
        var file = WriteToDisk(email.Data);
        return new SendResponse { MessageId = file };
    }

    public async Task<SendResponse> SendAsync(IFluentEmail email, CancellationToken? cancellationToken = null)
    {
        var file = await WriteToDiskAsync(email.Data, cancellationToken ?? CancellationToken.None);
        return new SendResponse { MessageId = file };
    }

    private string WriteToDisk(EmailData data)
    {
        var (file, content) = BuildFile(data);
        File.WriteAllText(file, content);
        return file;
    }

    private async Task<string> WriteToDiskAsync(EmailData data, CancellationToken ct)
    {
        var (file, content) = BuildFile(data);
        await File.WriteAllTextAsync(file, content, ct);
        return file;
    }

    private (string file, string content) BuildFile(EmailData data)
    {
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
        var to = data.ToAddresses.FirstOrDefault()?.EmailAddress ?? "unknown";
        var file = Path.Combine(directory, $"{stamp}-{Sanitize(to)}.eml");
        return (file, BuildEml(data));
    }

    private static string BuildEml(EmailData data)
    {
        var from = data.FromAddress is null ? "(no from)" : $"{data.FromAddress.Name} <{data.FromAddress.EmailAddress}>";
        var to = string.Join(", ", data.ToAddresses.Select(a => a.EmailAddress));
        return
            $"From: {from}\n" +
            $"To: {to}\n" +
            $"Subject: {data.Subject}\n" +
            $"{new string('-', 60)}\n" +
            data.Body +
            "\n";
    }

    private static string Sanitize(string value) =>
        string.Concat(value.Where(char.IsLetterOrDigit)).ToLowerInvariant();
}
