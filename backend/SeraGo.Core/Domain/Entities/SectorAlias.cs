namespace SeraGo.Core.Domain.Entities;

/// <summary>
/// One raw sector name as a source website spells it, mapped to a canonical
/// <see cref="Sector"/>. Aliases are stored normalized (trimmed, lowercase) so
/// lookups are exact. Data-driven: admins add aliases as new sources appear
/// without shipping code.
/// </summary>
public class SectorAlias
{
    public Guid Id { get; set; }

    public Guid SectorId { get; set; }
    public Sector Sector { get; set; } = null!;

    /// <summary>Normalized raw name, e.g. "information technology".</summary>
    public string Alias { get; set; } = string.Empty;
}
