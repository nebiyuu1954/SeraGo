using System.Text;
using SeraGo.Core.Domain.Entities;
using Telegram.Bot.Types.ReplyMarkups;

namespace SeraGo.API.Services;

public record BuiltTelegramMessage(
    string Text,
    InlineKeyboardMarkup? Keyboard,
    string? BannerUrl);

/// <summary>
/// Assembles engaging, beautifully formatted HTML message cards and inline buttons
/// for Telegram Channel broadcasts and personalized Bot DMs.
/// </summary>
public sealed class TelegramMessageBuilder
{
    private readonly TelegramOptions _options;

    public TelegramMessageBuilder(TelegramOptions options)
    {
        _options = options;
    }

    private static readonly string[] CareerTips = new[]
    {
        "💡 <b>Career Tip:</b> Recruiters spend an average of 7 seconds on the initial CV scan. Put your strongest technical skills right at the top!",
        "💡 <b>Pro Tip:</b> Tailor your CV keywords to the job description — automated ATS scanners look for direct skill matches.",
        "💡 <b>Market Fact:</b> Over 70% of hiring managers contact applicants within the first 48 hours of posting. Early applications win interviews!",
        "💡 <b>Career Tip:</b> Keep your SeraGo profile up to date with your latest work experience so our AI matching engine scores you at the top.",
        "💡 <b>Did You Know?</b> SeraGo aggregates jobs from 5 major Ethiopian platforms every 15 minutes so you never miss a new role.",
        "💡 <b>Pro Tip:</b> Even if you match 70% of a job's requirements, apply! Most companies hire for potential, enthusiasm, and culture fit."
    };

    private static string GetRandomCareerTip() =>
        CareerTips[Random.Shared.Next(CareerTips.Length)];

    // ── Good Morning ────────────────────────────────────────────────────────

    public BuiltTelegramMessage BuildMorningChannel(DailyJobSummary stats)
    {
        var dateDisplay = stats.PeriodStartEat.ToString("dddd, MMMM dd");
        var sb = new StringBuilder();
        sb.AppendLine("☀️ <b>Good Morning, Ethiopia!</b>");
        sb.AppendLine();
        sb.AppendLine($"Here is your morning job market pulse for <b>{dateDisplay}</b>:");
        sb.AppendLine();

        if (stats.TotalJobs > 0)
        {
            sb.AppendLine($"📊 <b>{stats.TotalJobs} new job{(stats.TotalJobs == 1 ? "" : "s")}</b> were published yesterday!");
            sb.AppendLine();

            if (stats.TopSectors.Count > 0)
            {
                sb.AppendLine("🏆 <b>Top Hiring Sectors:</b>");
                foreach (var s in stats.TopSectors)
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

        sb.AppendLine(GetRandomCareerTip());
        sb.AppendLine();
        sb.AppendLine("Get personalized alerts directly in our bot: @My_Serago_Bot");

        var url = $"{_options.JobLinkBase}?from=tg_channel_morning";
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithUrl("🔍 Browse Today's Jobs on SeraGo", url) }
        });

        return new BuiltTelegramMessage(sb.ToString(), keyboard, null);
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
            sb.AppendLine($"Yesterday, <b>{userStats.MatchingJobsCount} new job{(userStats.MatchingJobsCount == 1 ? "" : "s")}</b> were published in your sector{(userStats.SectorNames.Count > 1 ? "s" : "")}:");
            sb.AppendLine($"🏷️ <b>{sectorsDisplay}</b>");
            sb.AppendLine();

            sb.AppendLine("🔥 <b>Latest Highlights:</b>");
            foreach (var job in userStats.SampleJobs.Take(3))
            {
                var comp = !string.IsNullOrWhiteSpace(job.Company) ? $" at {Escape(job.Company)}" : "";
                sb.AppendLine($" • <b>{Escape(job.Title)}</b>{comp}");
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

        var url = $"{_options.JobLinkBase}?from=tg_bot_morning";
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithUrl("🎯 View My Matches on SeraGo", url) }
        });

        return new BuiltTelegramMessage(sb.ToString(), keyboard, null);
    }

    // ── Good Afternoon ──────────────────────────────────────────────────────

    public BuiltTelegramMessage BuildAfternoonChannel(DailyJobSummary stats)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🌤️ <b>Good Afternoon! Midday Career Pulse</b>");
        sb.AppendLine();

        if (stats.TotalJobs > 0)
        {
            sb.AppendLine($"⚡ <b>{stats.TotalJobs} fresh job{(stats.TotalJobs == 1 ? "" : "s")}</b> were posted this morning!");
            sb.AppendLine();

            if (stats.TopSectors.Count > 0)
            {
                sb.AppendLine("🔥 <b>Active Right Now:</b>");
                foreach (var s in stats.TopSectors)
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

        sb.AppendLine(GetRandomCareerTip());
        sb.AppendLine();
        sb.AppendLine("Take a midday break, review your CV, and make your move on SeraGo!");

        var url = $"{_options.JobLinkBase}?from=tg_channel_afternoon";
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithUrl("⚡ Explore Fresh Opportunities", url) }
        });

        return new BuiltTelegramMessage(sb.ToString(), keyboard, null);
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
            sb.AppendLine($"Midday check-in: <b>{userStats.MatchingJobsCount} fresh position{(userStats.MatchingJobsCount == 1 ? "" : "s")}</b> were listed this morning in:");
            sb.AppendLine($"🏷️ <b>{sectorsDisplay}</b>");
            sb.AppendLine();

            foreach (var job in userStats.SampleJobs.Take(3))
            {
                var comp = !string.IsNullOrWhiteSpace(job.Company) ? $" · {Escape(job.Company)}" : "";
                sb.AppendLine($" • <b>{Escape(job.Title)}</b>{comp}");
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

        var url = $"{_options.JobLinkBase}?from=tg_bot_afternoon";
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithUrl("🚀 View Midday Listings", url) }
        });

        return new BuiltTelegramMessage(sb.ToString(), keyboard, null);
    }

    // ── Good Night ──────────────────────────────────────────────────────────

    public BuiltTelegramMessage BuildNightChannel(DailyJobSummary stats)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🌙 <b>Good Night & Daily Wrap-Up</b>");
        sb.AppendLine();

        if (stats.TotalJobs > 0)
        {
            sb.AppendLine($"📈 <b>{stats.TotalJobs} total jobs</b> were published today across Ethiopia!");
            sb.AppendLine();

            if (stats.TopSectors.Count > 0)
            {
                sb.AppendLine("📊 <b>Today's Leading Sectors:</b>");
                foreach (var s in stats.TopSectors)
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

        sb.AppendLine("🌟 <i>\"Small daily steps lead to life-changing career milestones.\"</i>");
        sb.AppendLine();
        sb.AppendLine("Rest well tonight, and stay ready for tomorrow's openings! Good night! ✨");

        var url = $"{_options.JobLinkBase}?from=tg_channel_night";
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithUrl("📋 Review Today's Job Catalog", url) }
        });

        return new BuiltTelegramMessage(sb.ToString(), keyboard, null);
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
            sb.AppendLine($"📋 <b>{userStats.MatchingJobsCount} matching job{(userStats.MatchingJobsCount == 1 ? "" : "s")}</b> were posted today!");
            sb.AppendLine();

            foreach (var job in userStats.SampleJobs.Take(4))
            {
                var loc = !string.IsNullOrWhiteSpace(job.Location) ? $" ({Escape(job.Location)})" : "";
                sb.AppendLine($" • <b>{Escape(job.Title)}</b>{loc}");
            }
            sb.AppendLine();
            sb.AppendLine("Bookmark or apply before you sleep so you're first in line tomorrow morning.");
        }
        else
        {
            sb.AppendLine($"That's a wrap for today! No new jobs were added in <b>{sectorsDisplay}</b> today.");
            sb.AppendLine();
            sb.AppendLine("Rest up — our automated scrapers start fresh at dawn! Have a wonderful night! 💤");
        }

        var url = $"{_options.JobLinkBase}?from=tg_bot_night";
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithUrl("📑 Review All Matching Roles", url) }
        });

        return new BuiltTelegramMessage(sb.ToString(), keyboard, null);
    }

    // ── Promotional / Feature Ads ───────────────────────────────────────────

    public BuiltTelegramMessage BuildAdMessage(TelegramScheduledMessage message, string? userName = null)
    {
        if (!string.IsNullOrWhiteSpace(message.CustomTemplate))
        {
            return BuildCustomMessage(message, userName, null, null);
        }

        var sb = new StringBuilder();
        sb.AppendLine("🚀 <b>Why Thousands of Ethiopian Talents Choose SeraGo</b>");
        sb.AppendLine();
        sb.AppendLine("Tired of checking 5 different job boards every single day?");
        sb.AppendLine();
        sb.AppendLine("SeraGo unifies Ethiopia's entire job market in one clean platform:");
        sb.AppendLine(" • 🔄 <b>Automated Sync:</b> Ethiojobs, HaHuJobs, Afriwork, GeezJobs & Reporter synced every 15 mins.");
        sb.AppendLine(" • 🤖 <b>AI Matching:</b> Upload your CV to @My_Serago_Bot and get matched instantly.");
        sb.AppendLine(" • 🔔 <b>Instant Alerts:</b> Personalized job alerts straight to your Telegram DM.");
        sb.AppendLine(" • ⚡ <b>1-Click Apply:</b> Direct ATS and verified employer contacts.");
        sb.AppendLine();
        sb.AppendLine("Level up your career today — it's 100% free for talents!");

        var btnText = !string.IsNullOrWhiteSpace(message.ButtonText) ? message.ButtonText : "🌟 Explore SeraGo Platform";
        var btnUrl = !string.IsNullOrWhiteSpace(message.ButtonUrl) ? message.ButtonUrl : "https://serago.pro.et";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithUrl(btnText, btnUrl) }
        });

        return new BuiltTelegramMessage(sb.ToString(), keyboard, message.BannerUrl);
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
            sectorsListText = string.Join("\n", dailyStats.TopSectors.Select(s => $" • {Escape(s.SectorName)}: {s.JobCount}"));
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

        return new BuiltTelegramMessage(compiled, keyboard, message.BannerUrl);
    }

    private static string Escape(string? text) => string.IsNullOrEmpty(text)
        ? string.Empty
        : text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
