using System;

namespace SeraGo.Core.Domain.Entities;

/// <summary>
/// Detailed audit trace of which exact AI model, tier, confidence, and reasoning
/// classified a specific job. Stored in JobClassificationRecords table.
/// </summary>
public class JobClassificationRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Foreign key to the Job entity.</summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// Exact model name that performed the classification:
    /// e.g. "OpenRouter Nemotron", "Groq Llama 3.3 70B", "Google Gemini 2.0 Flash",
    /// "Local Ollama", "Cosine Similarity", "AdminManual".
    /// </summary>
    public string ModelUsed { get; set; } = string.Empty;

    /// <summary>
    /// Tier level and execution details:
    /// e.g. "Tier 1 (NVIDIA Nemotron 3 Ultra 550B via OpenRouter)",
    /// "Tier 2 (Meta Llama 3.3 70B via Groq Cloud)", "Tier 3 (Google Gemini 2.0 Flash)".
    /// </summary>
    public string ModelTier { get; set; } = string.Empty;

    /// <summary>Canonical sector slug assigned.</summary>
    public string? SectorSlug { get; set; }

    /// <summary>Canonical sector name assigned.</summary>
    public string? SectorName { get; set; }

    /// <summary>AI confidence score between 0.0 and 1.0.</summary>
    public double Confidence { get; set; } = 0.95;

    /// <summary>AI explanation and reasoning snippet for this classification decision.</summary>
    public string Reasoning { get; set; } = string.Empty;

    /// <summary>When the classification was executed.</summary>
    public DateTimeOffset ClassifiedAt { get; set; } = DateTimeOffset.UtcNow;
}
