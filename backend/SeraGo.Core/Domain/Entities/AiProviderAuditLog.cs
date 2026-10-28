using System;

namespace SeraGo.Core.Domain.Entities;

/// <summary>
/// Audit record of an AI provider failure, rate-limit, or timeout event
/// that triggered a fallback to a secondary AI provider or lower tier.
/// Enables real-time admin monitoring of provider health and failovers without AI reliance.
/// </summary>
public class AiProviderAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>When the fallback occurred (UTC).</summary>
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>The tier and model that was being attempted (e.g. "Tier 1 (OpenRouter Nemotron 550B)").</summary>
    public string AttemptedTier { get; set; } = string.Empty;

    /// <summary>The provider key name that failed (e.g. "OpenRouter", "Groq", "Gemini", "Ollama").</summary>
    public string FailedProvider { get; set; } = string.Empty;

    /// <summary>HTTP status code if an HTTP response was received (e.g. 429, 503, 401).</summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>Categorized failure reason: "RateLimit", "Timeout", "Auth", "ServerError", "MalformedJson", or "Network".</summary>
    public string ErrorCategory { get; set; } = "Unknown";

    /// <summary>Human-readable error explanation (e.g. "Rate limit exceeded: 6,000 TPM limit" or "Timed out after 30s").</summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>The fallback tier/model that took over (e.g. "Tier 2 (Groq Llama 3.3 70B)").</summary>
    public string FallbackTier { get; set; } = string.Empty;

    /// <summary>Number of jobs in the chunk that were rerouted.</summary>
    public int JobCount { get; set; }

    /// <summary>Whether the fallback tier succeeded in classifying the jobs.</summary>
    public bool WasRecovered { get; set; } = true;

    /// <summary>Preview snippet of job titles affected in this chunk.</summary>
    public string? JobTitlesSnippet { get; set; }
}
