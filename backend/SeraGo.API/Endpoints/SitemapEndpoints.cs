using System.Text;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using SeraGo.Core.Domain.Entities;
using SeraGo.Core.Domain.Enums;
using SeraGo.Infrastructure.Context;

namespace SeraGo.API.Endpoints;

/// <summary>
/// Public XML Sitemap endpoint for search engine crawlers (Google Search Console, Bing, Yandex).
/// Streams a standard XML sitemap indexing the site's public static landing pages and all
/// active, published job postings.
/// </summary>
public static class SitemapEndpoints
{
    public static IEndpointRouteBuilder MapSitemapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/sitemap.xml", async (ApplicationDbContext db, HttpContext context, IConfiguration config) =>
        {
            var baseUrl = (config["Frontend:BaseUrl"] ?? "https://serago.pro.et").TrimEnd('/');
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

            var now = DateTimeOffset.UtcNow;

            // Fetch active, published jobs that have not passed their deadline
            var jobs = await db.Jobs
                .AsNoTracking()
                .Where(j => j.Status == JobStatus.Published 
                         && j.IsActive 
                         && (j.Deadline == null || j.Deadline >= now))
                .Select(j => new 
                {
                    j.Id,
                    LastMod = j.UpdatedAt > j.CreatedAt ? j.UpdatedAt : j.CreatedAt
                })
                .OrderByDescending(j => j.LastMod)
                .Take(50000) // Standard single-sitemap URL cap
                .ToListAsync();

            var urlElements = new List<XElement>
            {
                // Static public pages
                new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}/"),
                    new XElement(ns + "changefreq", "daily"),
                    new XElement(ns + "priority", "1.0")),
                new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}/jobs"),
                    new XElement(ns + "changefreq", "hourly"),
                    new XElement(ns + "priority", "0.9")),
                new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}/about"),
                    new XElement(ns + "changefreq", "monthly"),
                    new XElement(ns + "priority", "0.5")),
                new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}/terms"),
                    new XElement(ns + "changefreq", "monthly"),
                    new XElement(ns + "priority", "0.3")),
                new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}/privacy"),
                    new XElement(ns + "changefreq", "monthly"),
                    new XElement(ns + "priority", "0.3"))
            };

            // Dynamic Job URLs
            foreach (var job in jobs)
            {
                urlElements.Add(new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}/dashboard/talent/jobs/{job.Id}"),
                    new XElement(ns + "lastmod", job.LastMod.ToString("yyyy-MM-ddTHH:mm:sszzz")),
                    new XElement(ns + "changefreq", "daily"),
                    new XElement(ns + "priority", "0.8")));
            }

            var sitemap = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement(ns + "urlset", urlElements)
            );

            // Set Cache-Control header to 1 hour
            context.Response.Headers.CacheControl = "public, max-age=3600";
            return Results.Content(sitemap.ToString(SaveOptions.DisableFormatting), "application/xml; charset=utf-8", Encoding.UTF8);
        })
        .WithName("GetSitemapXml")
        .WithTags("SEO")
        .ExcludeFromDescription();

        return app;
    }
}
