using System.Text;
using SeraGo.Core.Domain.Entities;
using Telegram.Bot.Types.ReplyMarkups;

namespace SeraGo.API.Services;

public enum TelegramMediaType
{
    None = 0,
    Photo = 1,
    Video = 2
}

public record BuiltTelegramMessage(
    string Text,
    InlineKeyboardMarkup? Keyboard,
    string? MediaPathOrUrl,
    TelegramMediaType MediaType = TelegramMediaType.Photo);

/// <summary>
/// Assembles engaging, beautifully formatted HTML message cards and inline buttons
/// for Telegram Channel broadcasts and personalized Bot DMs.
/// </summary>
public sealed class TelegramMessageBuilder
{
    private readonly TelegramOptions _options;
    private const string LoginUrl = "https://serago.pro.et/login";
    private const string DefaultPhotoPath = "sources/serago.jpg";
    private const string DefaultVideoPath = "sources/SeraGoAD.mp4";
    private const string ShareUrl = "https://t.me/share/url?url=https://serago.pro.et&text=Check%20out%20SeraGo%20-%20Ethiopia%27s%20unified%20job%20platform%20with%20AI%20matching!";

    public TelegramMessageBuilder(TelegramOptions options)
    {
        _options = options;
    }

    // ── Good Morning ────────────────────────────────────────────────────────

    public BuiltTelegramMessage BuildMorningChannel(DailyJobSummary stats)
    {
        var dateDisplay = stats.PeriodStartEat.ToString("dddd, MMMM dd");
        var sb = new StringBuilder();
        sb.AppendLine("☀️ <b>Good Morning, Everyone!</b>");
        sb.AppendLine();
        sb.AppendLine($"Here is your morning job report for <b>{dateDisplay}</b>:");
        sb.AppendLine();

        if (stats.TotalJobs > 0)
        {
            sb.AppendLine($"📊 <b>Over +{stats.TotalJobs} new jobs</b> were published yesterday!");
            sb.AppendLine();

            if (stats.TopSectors.Count > 0)
            {
                sb.AppendLine("🏆 <b>Top Hiring Sectors:</b>");
                foreach (var s in stats.TopSectors.Take(10))
                {
                    sb.AppendLine($" • <b>{Escape(s.SectorName)}</b> — {s.JobCount} {(s.JobCount == 1 ? "role" : "roles")}");
                }
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("📊 New listings are coming in right now through our morning scraper sync!");
            sb.AppendLine();
        }

        sb.AppendLine("💡 <b>Pro Tip:</b> Go to the website and Run AI matching to see which job fits you from yesterdays listing");
        sb.AppendLine();
        sb.AppendLine("Get personalized alerts directly in our bot: @My_Serago_Bot");

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithUrl("🔍 Browse Jobs on SeraGo", LoginUrl) }
        });

        return new BuiltTelegramMessage(sb.ToString().TrimEnd(), keyboard, DefaultPhotoPath, TelegramMediaType.Photo);
    }

    public BuiltTelegramMessage BuildMorningBotDm(string userName, UserSectorJobSummary userStats)
    {
        var nameDisplay = !string.IsNullOrWhiteSpace(userName) ? Escape(userName) : "there";
        var sb = new StringBuilder();
        sb.AppendLine($"☀️ <b>Good Morning, {nameDisplay}!</b>");
        sb.AppendLine();

        var sectorsDisplay = userStats.SectorNames.Count > 0
            ? string.Join(", ", userStats.SectorNames.Select(Escape))
            : "Your Target Sectors";

        if (userStats.MatchingJobsCount > 0)
        {
            sb.AppendLine($"Yesterday, <b>{userStats.MatchingJobsCount} new job{(userStats.MatchingJobsCount == 1 ? "" : "s")}</b> were published in your sector:");
            sb.AppendLine($"🏷️ <b>{sectorsDisplay}</b>");
            sb.AppendLine();

            sb.AppendLine("🔥 <b>Latest yesterdays listing:</b>");
            const int maxCaptionLength = 950;
            var listed = 0;
            foreach (var job in userStats.SampleJobs)
            {
                var comp = !string.IsNullOrWhiteSpace(job.Company) ? $" at {Escape(job.Company)}" : "";
                var line = $" • <b>{Escape(job.Title)}</b>{comp}\n";
                if (sb.Length + line.Length > maxCaptionLength)
                {
                    var remaining = userStats.MatchingJobsCount - listed;
                    if (remaining > 0)
                    {
                        sb.AppendLine($"<i>...and {remaining} more jobs on the website!</i>");
                    }
                    break;
                }
                sb.Append(line);
                listed++;
            }
            sb.AppendLine();
            sb.AppendLine("Make sure to review these opportunities early today!");
        }
        else
        {
            sb.AppendLine($"No new postings landed in <b>{sectorsDisplay}</b> yesterday, but our scraper syncs 5 Ethiopian sources every 15 minutes.");
            sb.AppendLine();
            sb.AppendLine("We will notify you immediately the moment a matching role is found today! 🚀");
        }

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithUrl("🔍 Browse Jobs on SeraGo", LoginUrl) }
        });

        return new BuiltTelegramMessage(sb.ToString().TrimEnd(), keyboard, DefaultPhotoPath, TelegramMediaType.Photo);
    }

    // ── Good Afternoon ──────────────────────────────────────────────────────

    public BuiltTelegramMessage BuildAfternoonChannel(DailyJobSummary stats)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🌤️ <b>Good Afternoon Everyone!</b>");
        sb.AppendLine();

        if (stats.TotalJobs > 0)
        {
            sb.AppendLine($"⚡ <b>{stats.TotalJobs} fresh jobs</b> were posted this morning!");
            sb.AppendLine();

            if (stats.TopSectors.Count > 0)
            {
                sb.AppendLine("🔥 <b>Active Right Now:</b>");
                foreach (var s in stats.TopSectors.Take(10))
                {
                    sb.AppendLine($" • <b>{Escape(s.SectorName)}</b> ({s.JobCount})");
                }
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("⚡ The midday market is active with fresh applications and recruiter reviews.");
            sb.AppendLine();
        }

        sb.AppendLine("💡 <b>Pro Tip:</b> Keep your SeraGo profile up to date with your latest work experience so our AI matching engine scores you at the top.");
        sb.AppendLine();
        sb.AppendLine("Get personalized alerts directly in our bot: @My_Serago_Bot");

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithUrl("⚡ Explore Morning Jobs", LoginUrl) }
        });

        return new BuiltTelegramMessage(sb.ToString().TrimEnd(), keyboard, DefaultPhotoPath, TelegramMediaType.Photo);
    }

    public BuiltTelegramMessage BuildAfternoonBotDm(string userName, UserSectorJobSummary userStats)
    {
        var nameDisplay = !string.IsNullOrWhiteSpace(userName) ? Escape(userName) : "there";
        var sb = new StringBuilder();
        sb.AppendLine($"🌤️ <b>Good Afternoon, {nameDisplay}!</b>");
        sb.AppendLine();

        var sectorsDisplay = userStats.SectorNames.Count > 0
            ? string.Join(", ", userStats.SectorNames.Select(Escape))
            : "Your Target Sectors";

        if (userStats.MatchingJobsCount > 0)
        {
            sb.AppendLine($"<b>{userStats.MatchingJobsCount} fresh positions</b> were listed this morning in:");
            sb.AppendLine($"🏷️ <b>{sectorsDisplay}</b>");
            sb.AppendLine();

            const int maxCaptionLength = 950;
            var listed = 0;
            foreach (var job in userStats.SampleJobs)
            {
                var comp = !string.IsNullOrWhiteSpace(job.Company) ? $" · {Escape(job.Company)}" : "";
                var line = $" • <b>{Escape(job.Title)}</b>{comp}\n";
                if (sb.Length + line.Length > maxCaptionLength)
                {
                    var remaining = userStats.MatchingJobsCount - listed;
                    if (remaining > 0)
                    {
                        sb.AppendLine($"<i>...and {remaining} more fresh jobs on the website!</i>");
                    }
                    break;
                }
                sb.Append(line);
                listed++;
            }
            sb.AppendLine();
            sb.AppendLine("Check them out while they are still brand new!");
        }
        else
        {
            sb.AppendLine($"Your sectors (<b>{sectorsDisplay}</b>) are quiet this afternoon, but applications are actively being reviewed across the platform.");
            sb.AppendLine();
            sb.AppendLine("Check back or explore related sectors to widen your horizon!");
        }

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithUrl("⚡ View Morning Jobs", LoginUrl) }
        });

        return new BuiltTelegramMessage(sb.ToString().TrimEnd(), keyboard, DefaultPhotoPath, TelegramMediaType.Photo);
    }

    // ── Good Night ──────────────────────────────────────────────────────────

    public BuiltTelegramMessage BuildNightChannel(DailyJobSummary stats)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<b>Daily Wrap-Up report</b>");
        sb.AppendLine();

        if (stats.TotalJobs > 0)
        {
            sb.AppendLine($"📈 <b>{stats.TotalJobs} total jobs</b> were published today across Ethiopia!");
            sb.AppendLine();

            if (stats.TopSectors.Count > 0)
            {
                sb.AppendLine("📊 <b>Today's Leading Sectors:</b>");
                foreach (var s in stats.TopSectors.Take(10))
                {
                    sb.AppendLine($" • <b>{Escape(s.SectorName)}</b> — {s.JobCount} {(s.JobCount == 1 ? "listing" : "listings")}");
                }
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("📈 Today's job round-up has concluded across all our connected sources.");
            sb.AppendLine();
        }

        sb.AppendLine("💡 <b>Pro Tip:</b> Go to the website and Run AI matching to see which job fits you from todays listing");
        sb.AppendLine();
        sb.AppendLine("Get personalized alerts directly in our bot: @My_Serago_Bot");
        sb.AppendLine();
        sb.AppendLine("Good night Everyone! ✨");

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithUrl("📋 Review Today's Job Catalog", LoginUrl) }
        });

        return new BuiltTelegramMessage(sb.ToString().TrimEnd(), keyboard, DefaultPhotoPath, TelegramMediaType.Photo);
    }

    public BuiltTelegramMessage BuildNightBotDm(string userName, UserSectorJobSummary userStats)
    {
        var nameDisplay = !string.IsNullOrWhiteSpace(userName) ? Escape(userName) : "there";
        var sb = new StringBuilder();
        sb.AppendLine($"🌙 <b>Evening Report for {nameDisplay}</b>");
        sb.AppendLine();

        var sectorsDisplay = userStats.SectorNames.Count > 0
            ? string.Join(", ", userStats.SectorNames.Select(Escape))
            : "Your Target Sectors";

        if (userStats.MatchingJobsCount > 0)
        {
            sb.AppendLine($"Here is your complete daily recap in <b>{sectorsDisplay}</b>:");
            sb.AppendLine($"📋 <b>{userStats.MatchingJobsCount} matching jobs</b> were posted today!");
            sb.AppendLine();

            const int maxCaptionLength = 950;
            var listed = 0;
            foreach (var job in userStats.SampleJobs)
            {
                var loc = !string.IsNullOrWhiteSpace(job.Company)
                    ? $" ({Escape(job.Company)})"
                    : (!string.IsNullOrWhiteSpace(job.Location) ? $" ({Escape(job.Location)})" : "");
                var line = $" • <b>{Escape(job.Title)}</b>{loc}\n";
                if (sb.Length + line.Length > maxCaptionLength)
                {
                    var remaining = userStats.MatchingJobsCount - listed;
                    if (remaining > 0)
                    {
                        sb.AppendLine($"<i>...and {remaining} more jobs on the website!</i>");
                    }
                    break;
                }
                sb.Append(line);
                listed++;
            }
            sb.AppendLine();
            sb.AppendLine("Make sure to review these opportunities from today!");
        }
        else
        {
            sb.AppendLine($"That's a wrap for today! No new jobs were added in <b>{sectorsDisplay}</b> today.");
            sb.AppendLine();
            sb.AppendLine("Rest up — our automated scrapers start fresh at dawn! Have a wonderful night! 💤");
        }

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithUrl("🌙 Review Today's Jobs", LoginUrl) }
        });

        return new BuiltTelegramMessage(sb.ToString().TrimEnd(), keyboard, DefaultPhotoPath, TelegramMediaType.Photo);
    }

    // ── Promotional / Feature Ads ───────────────────────────────────────────

    public BuiltTelegramMessage BuildAdMessage(TelegramScheduledMessage message, string? userName = null)
    {
        if (!string.IsNullOrWhiteSpace(message.CustomTemplate))
        {
            return BuildCustomMessage(message, userName, null, null);
        }

        var sb = new StringBuilder();
        sb.AppendLine("🚀 <b>Why Thousands of Ethiopian job seekers Choose SeraGo</b>");
        sb.AppendLine();
        sb.AppendLine("Tired of checking multiple different job boards every single day?");
        sb.AppendLine();
        sb.AppendLine("SeraGo unifies Ethiopia's entire job market in one clean platform:");
        sb.AppendLine(" • 🔄 <b>Automated Sync:</b> Ethiojobs, HaHuJobs, Afriwork, GeezJobs & Reporter jobs synced and tailored to your profile.");
        sb.AppendLine(" • 🤖 <b>AI Matching:</b> Upload your CV to our website and get matched instantly.");
        sb.AppendLine(" • 🔔 <b>Instant Alerts:</b> Personalized job alerts straight to your Telegram DM with our bot @My_Serago_Bot.");
        sb.AppendLine(" • ⚡️ <b>1-Click Apply:</b> Direct ATS and verified employer contacts.");
        sb.AppendLine();
        sb.AppendLine("Level up your career today — it's 100% free for everyone to use!");
        sb.AppendLine();
        sb.AppendLine("Serago Team");

        var btnText = !string.IsNullOrWhiteSpace(message.ButtonText) ? message.ButtonText : "🚀 Visit SeraGo Website";
        var btnUrl = !string.IsNullOrWhiteSpace(message.ButtonUrl) ? message.ButtonUrl : LoginUrl;

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithUrl(btnText, btnUrl) },
            new[] { InlineKeyboardButton.WithUrl("👥 Share to friends", ShareUrl) }
        });

        var videoPath = !string.IsNullOrWhiteSpace(message.BannerUrl) ? message.BannerUrl : DefaultVideoPath;
        return new BuiltTelegramMessage(sb.ToString().TrimEnd(), keyboard, videoPath, TelegramMediaType.Video);
    }

    // ── Custom Template Compiler ────────────────────────────────────────────

    public BuiltTelegramMessage BuildCustomMessage(
        TelegramScheduledMessage message,
        string? userName,
        DailyJobSummary? dailyStats,
        UserSectorJobSummary? userStats)
    {
        var template = message.CustomTemplate ?? message.Title;
        var nowEat = TelegramStatsService.NowEat;

        var sectorsListText = "";
        if (dailyStats?.TopSectors != null && dailyStats.TopSectors.Count > 0)
        {
            sectorsListText = string.Join("\n", dailyStats.TopSectors.Take(10).Select(s => $" • {Escape(s.SectorName)}: {s.JobCount}"));
        }

        var userSectorsText = userStats?.SectorNames != null && userStats.SectorNames.Count > 0
            ? string.Join(", ", userStats.SectorNames.Select(Escape))
            : "General";

        var compiled = template
            .Replace("{user_name}", !string.IsNullOrWhiteSpace(userName) ? Escape(userName) : "there")
            .Replace("{date}", nowEat.ToString("dddd, MMMM dd"))
            .Replace("{time}", nowEat.ToString("HH:mm"))
            .Replace("{yesterday_count}", (dailyStats?.TotalJobs ?? 0).ToString())
            .Replace("{morning_count}", (dailyStats?.TotalJobs ?? 0).ToString())
            .Replace("{today_count}", (dailyStats?.TotalJobs ?? 0).ToString())
            .Replace("{top_sectors}", sectorsListText)
            .Replace("{user_sectors}", userSectorsText)
            .Replace("{user_sector_jobs_count}", (userStats?.MatchingJobsCount ?? 0).ToString())
            .Replace("{bot_username}", "@My_Serago_Bot")
            .Replace("{serago_url}", "https://serago.pro.et");

        InlineKeyboardMarkup? keyboard = null;
        if (!string.IsNullOrWhiteSpace(message.ButtonText) && !string.IsNullOrWhiteSpace(message.ButtonUrl))
        {
            keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithUrl(message.ButtonText, message.ButtonUrl) }
            });
        }

        var mediaType = !string.IsNullOrWhiteSpace(message.BannerUrl) && message.BannerUrl.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
            ? TelegramMediaType.Video
            : (!string.IsNullOrWhiteSpace(message.BannerUrl) ? TelegramMediaType.Photo : TelegramMediaType.None);

        return new BuiltTelegramMessage(compiled, keyboard, message.BannerUrl, mediaType);
    }

    private static string Escape(string? text) => string.IsNullOrEmpty(text)
        ? string.Empty
        : text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
