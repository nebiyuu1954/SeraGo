namespace SeraGo.API.Services;

/// <summary>
/// Obsolete: Replaced by UnifiedJobClassificationService.
/// SectorLabelMappings table has been phased out in favor of the 4-tier pipeline
/// (Gemini Flash -> Groq -> Ollama -> Cosine Similarity).
/// </summary>
[Obsolete("Replaced by UnifiedJobClassificationService")]
public sealed class AiClassificationSaveService
{
}
