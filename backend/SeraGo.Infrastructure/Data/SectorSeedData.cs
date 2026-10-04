using SeraGo.Core.Domain.Entities;

namespace SeraGo.Infrastructure.Data;

/// <summary>
/// The canonical sector vocabulary plus every raw sector name seen from the
/// scraped sources (Afriwork, HaHuJobs, EthioJobs) mapped onto it. Written
/// from the scraper database's actual values so the standardization layer
/// covers the real data out of the box. Seed IDs are fixed so migrations and
/// re-seeds stay stable.
/// </summary>
public static class SectorSeedData
{
    public sealed record SectorSeed(string Id, string Name, string Slug, string[] Aliases);

    public static readonly SectorSeed[] Sectors =
    [
        new("a0000000-0000-0000-0000-000000000001", "Technology & IT", "technology-it",
        [
            "Information Technology", "Technology", "Tech", "IT", "ICT",
            "IT Support", "Computer Science and Information Technology",
            "IT, Computer Science and Software Engineering", "Software Design & Development",
            "Data Science & Analytics", "Software", "Computer Science",
            "Software Engineering", "Frontend", "Backend", "CS", "DevOps",
            "Web Development", "Network Administration", "System Administration",
            "Database Administration", "Cybersecurity", "Machine Learning", "Artificial Intelligence"
        ]),
        new("a0000000-0000-0000-0000-000000000002", "Accounting & Finance", "accounting-finance",
        [
            "Accounting & Finance", "Accounting", "Finance", "Accounting and Finance",
            "Economics", "Tax", "Audit", "Bookkeeping", "Accountant", "Financial Analysis"
        ]),
        new("a0000000-0000-0000-0000-000000000003", "Banking & Insurance", "banking-insurance",
        [
            "Banking and Insurance", "Banking", "Insurance", "Insurance and Investment",
        ]),
        new("a0000000-0000-0000-0000-000000000004", "Sales & Marketing", "sales-marketing",
        [
            "Sales & Promotion", "Sales", "Marketing", "Sales and Marketing",
            "Business Sales and Marketing", "Marketing Management", "Digital Marketing",
            "Promotion",
        ]),
        new("a0000000-0000-0000-0000-000000000005", "Healthcare", "healthcare",
        [
            "Healthcare", "Health Care", "Health Care Management", "Public Health",
            "Nursing", "Pharmaceutical", "Pharmacy", "Medicine", "Medical",
            "Psychiatry, Psychology & Social Work",
        ]),
        new("a0000000-0000-0000-0000-000000000006", "Education & Training", "education-training",
        [
            "Teaching & Education", "Education", "Teaching", "Tutoring, Training & Mentorship",
            "Language and Literature", "Education Management", "Tutoring", "Training",
        ]),
        new("a0000000-0000-0000-0000-000000000007", "Engineering & Construction", "engineering-construction",
        [
            "Engineering", "Construction & Civil Engineering", "Civil Engineering",
            "Mechanical & Electrical Engineering", "Chemical & Biomedical Engineering",
            "Environmental, Mining & Energy Engineering", "Manufacturing Engineering",
            "Architectural Engineering", "Automotive Engineering", "Sanitary Engineering",
            "Biomedical Engineering", "Electrical Engineering", "Architecture & Urban Planning",
            "Construction Skilled Worker", "Mechanical Engineering", "Planning",
            "Water and Sanitation", "Automotive", "Aeronautics & Aerospace",
        ]),
        new("a0000000-0000-0000-0000-000000000008", "Human Resources", "human-resources",
        [
            "Human Resource & Talent Management", "Human Resource and Recruitment",
            "Human Resource Administration", "Human Resources", "HR", "Recruitment",
            "Human Resource", "Talent Management",
        ]),
        new("a0000000-0000-0000-0000-000000000009", "Business & Administration", "business-administration",
        [
            "Business", "Business Administration", "Business Administration & Operations",
            "Business and Administration", "Secretarial, Admin and Clerical",
            "Secretarial & Office Management", "Management", "Development and Project Management",
            "Advisory & Consultancy", "Brokerage & Case Closing", "Business Management",
            "Administration", "Office Management", "Secretarial", "Event Management & Organization",
            "Research Services",
        ]),
        new("a0000000-0000-0000-0000-00000000000a", "Manufacturing & Production", "manufacturing-production",
        [
            "Manufacturing & Production", "Manufacturing", "Manufacturing Management",
            "FMCG and Manufacturing", "Woodwork & Carpentry", "Production",
        ]),
        new("a0000000-0000-0000-0000-00000000000b", "Logistics & Transportation", "logistics-transportation",
        [
            "Transportation & Delivery", "Transportation & Logistics", "Transportation Management",
            "Transportation", "Logistics & Supply Chain", "Logistics, Transport and Supply Chain",
            "Logistics", "Warehouse, Supply Chain and Distribution",
        ]),
        new("a0000000-0000-0000-0000-00000000000c", "Procurement & Supply Chain", "procurement-supply-chain",
        [
            "Purchasing & Procurement", "Supply Chain & Purchasing Management",
            "Procurement", "Supply Chain", "Purchasing",
        ]),
        new("a0000000-0000-0000-0000-00000000000d", "Media & Communication", "media-communication",
        [
            "Media & Entertainment", "Media and Communication", "Multimedia Content Production",
            "Documentation & Writing", "Translation & Transcription", "Media",
            "Journalism & Communication", "Journalism", "Communication", "Writing",
        ]),
        new("a0000000-0000-0000-0000-00000000000e", "Design & Creative", "design-creative",
        [
            "Creative Art & Design", "Fashion / Clothing & Textile Design", "Design",
            "Graphic Design", "Creative", "Fashion Design", "Creative Arts",
            "UI/UX Design", "Fashion",
        ]),
        new("a0000000-0000-0000-0000-00000000000f", "Customer Service & Support", "customer-service-support",
        [
            "Customer Service & Care", "Retail & Office Support", "Customer Service",
            "Support", "Call Center", "Reception",
        ]),
        new("a0000000-0000-0000-0000-000000000010", "Hospitality & Tourism", "hospitality-tourism",
        [
            "Hospitality & Tourism", "Food & Drink Preparation / Service", "Hospitality",
            "Tourism", "Chef", "Catering", "Hotel",
        ]),
        new("a0000000-0000-0000-0000-000000000011", "Agriculture & Natural Science", "agriculture-natural-science",
        [
            "Agriculture", "Agricultural Science", "Natural Science", "Natural Sciences",
            "Chemistry", "Physics", "Microbiology", "Mathematics", "Nutrition", "Biology",
            "Veterinary", "Gardening & Landscaping", "Horticulture", "Livestock & Animal Husbandry",
        ]),
        new("a0000000-0000-0000-0000-000000000012", "Legal", "legal",
        [
            "Law & Legal Advocacy", "Legal Services", "Legal", "Law", "Advocacy",
        ]),
        new("a0000000-0000-0000-0000-000000000013", "Social Science & Community", "social-science-community",
        [
            "Social Sciences and Community Service", "Social Science", "Social Work",
            "Community Service", "History", "Sociology", "Psychology",
        ]),
        new("a0000000-0000-0000-0000-000000000014", "Security & Protection", "security-protection",
        [
            "Security & Safety", "Security", "Protection", "Guard", "Safety",
        ]),
        new("a0000000-0000-0000-0000-000000000015", "Skilled & General Labor", "skilled-general-labor",
        [
            "Low and Medium Skilled Worker", "Service Industry Skilled Worker",
            "Janitorial & Office Services", "General Labor", "Cleaner", "Maintenance",
            "Installation & Maintenance", "Labor & Masonry",
        ]),
        new("a0000000-0000-0000-0000-000000000016", "Beauty & Grooming", "beauty-grooming",
        [
            "Beauty & Grooming", "Cosmetics", "Salon", "Hairdresser", "Barber"
        ]),
    ];

    /// <summary>All seeded sectors as EF entities (for HasData).</summary>
    public static List<Sector> ToEntities() =>
        Sectors.Select(s => new Sector
        {
            Id = Guid.Parse(s.Id),
            Name = s.Name,
            Slug = s.Slug,
            IsActive = true,
        }).ToList();

    /// <summary>All seeded aliases as EF entities (for HasData). Each gets a
    /// deterministic id from a global counter so no two collide.</summary>
    public static List<SectorAlias> AliasEntities()
    {
        var list = new List<SectorAlias>();
        var i = 0;
        foreach (var s in Sectors)
        {
            foreach (var alias in s.Aliases)
            {
                list.Add(new SectorAlias
                {
                    Id = Guid.Parse($"b0000000-0000-0000-0000-{i++:x12}"),
                    SectorId = Guid.Parse(s.Id),
                    Alias = Normalize(alias),
                });
            }
        }
        return list;
    }

    /// <summary>Aliases are stored trimmed + lowercase so lookups are exact.</summary>
    public static string Normalize(string value) =>
        value.Trim().ToLowerInvariant();
}
