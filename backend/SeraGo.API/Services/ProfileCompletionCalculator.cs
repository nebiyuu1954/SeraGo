using SeraGo.Core.Domain.Entities;

namespace SeraGo.API.Services;

/// <summary>
/// Profile-completion scoring, extracted so the user's own
/// <c>GET /api/account/profile</c> and the admin
/// <c>GET /api/admin/users/{id}</c> endpoint can never disagree about someone's
/// percentage. Field lists live here only.
/// </summary>
public static class ProfileCompletionCalculator
{
    public sealed record Result(bool IsComplete, int PercentComplete, List<string> MissingFields);

    /// <summary>Fields the matching engine needs. Links/resume are optional extras.</summary>
    public static Result ForTalent(TalentProfile profile)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(profile.Headline)) missing.Add("headline");
        if (string.IsNullOrWhiteSpace(profile.About)) missing.Add("about");
        if (profile.ExperienceLevel is null) missing.Add("experienceLevel");
        if (profile.YearsOfExperience is null) missing.Add("yearsOfExperience");
        if (profile.DesiredRoles.Count == 0) missing.Add("desiredRoles");
        if (profile.Skills.Count == 0) missing.Add("skills");
        if (profile.DesiredJobTypes.Count == 0) missing.Add("desiredJobTypes");
        if (profile.WorkMode is null) missing.Add("workMode");
        if (profile.Availability is null) missing.Add("availability");

        const int total = 9;
        return Build(missing, total);
    }

    /// <summary>Fields a job post depends on.</summary>
    public static Result ForRecruiter(RecruiterProfile profile)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(profile.CompanyName)) missing.Add("companyName");
        if (string.IsNullOrWhiteSpace(profile.Industry)) missing.Add("industry");
        if (string.IsNullOrWhiteSpace(profile.CompanySize)) missing.Add("companySize");
        if (string.IsNullOrWhiteSpace(profile.WebsiteUrl)) missing.Add("websiteUrl");
        if (string.IsNullOrWhiteSpace(profile.About)) missing.Add("about");
        if (profile.FoundedYear is null) missing.Add("foundedYear");
        if (string.IsNullOrWhiteSpace(profile.Headquarters)) missing.Add("headquarters");
        if (string.IsNullOrWhiteSpace(profile.PhoneNumber)) missing.Add("phoneNumber");
        if (string.IsNullOrWhiteSpace(profile.Email)) missing.Add("email");
        if (profile.CompanyType is null) missing.Add("companyType");

        const int total = 10;
        return Build(missing, total);
    }

    private static Result Build(List<string> missing, int total) =>
        new(missing.Count == 0,
            (int)Math.Round((total - missing.Count) / (double)total * 100),
            missing);
}
