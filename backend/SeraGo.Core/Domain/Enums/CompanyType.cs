namespace SeraGo.Core.Domain.Enums;

/// <summary>
/// Legal / organisational type of the recruiter's company.
/// </summary>
public enum CompanyType
{
    /// <summary>Publicly traded company.</summary>
    Public = 0,

    /// <summary>Privately held company.</summary>
    Private = 1,

    /// <summary>Non-profit / NGO.</summary>
    NonProfit = 2,

    /// <summary>Government agency or state-owned enterprise.</summary>
    Government = 3,

    /// <summary>Early-stage startup.</summary>
    Startup = 4,

    /// <summary>Sole proprietorship or freelance.</summary>
    SoleProprietorship = 5,

    /// <summary>Partnership.</summary>
    Partnership = 6
}
