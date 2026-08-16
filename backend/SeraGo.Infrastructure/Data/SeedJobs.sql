-- ============================================================================
-- SeraGo — development seed: 10 published jobs
-- ============================================================================
-- Inserts 10 jobs straight into the Jobs table with Status = Published (2) and
-- IsActive = true, so they are immediately visible to Talent users on the
-- dashboard. This bypasses the draft -> pending review -> approval flow for
-- as long as the admin UI is not implemented.
--
-- Run it against the local database, e.g.:
--     psql "Host=localhost;Database=serago;Username=postgres;Password=postgres" -f SeedJobs.sql
-- (or paste it into your SQL tool / Neon console).
--
-- Notes:
--   * PostedByUserId is resolved to the bootstrap admin (Admin:Email in
--     appsettings.Development.json). Change the email if you want another
--     existing user to own these postings.
--   * gen_random_uuid() needs Postgres 13+ (fine on Neon and modern local).
--   * Re-running duplicates rows (no unique constraint on Title) — truncate
--     first if you want a clean slate:  TRUNCATE "Jobs" RESTART IDENTITY;
-- ============================================================================

INSERT INTO "Jobs"
    ("Id", "Title", "Description", "Company", "Location", "JobType",
     "Url", "Salary", "PublishedAt", "Deadline", "Status", "IsActive",
     "PostedByUserId", "SubmittedAt", "ApprovedAt", "RejectedAt",
     "RejectionReason", "CreatedAt", "UpdatedAt")
VALUES
    (
        gen_random_uuid(),
        'Senior Flutter Developer',
        'We are looking for a senior Flutter developer to own our flagship mobile app. You will build cross-platform features end to end, mentor two mid-level engineers, and work closely with design on a polished, high-performance experience. 5+ years of mobile experience with a strong Dart/Flutter portfolio is required.',
        'Addis Tech Solutions',
        'Addis Ababa, Ethiopia',
        0, -- FullTime
        'https://example.com/apply/senior-flutter-developer',
        'ETB 90,000 – 120,000 / month',
        '2026-08-12T09:00:00Z',
        '2026-09-20T17:00:00Z',
        2, -- Published
        true,
        (SELECT "Id" FROM "AspNetUsers" WHERE "Email" = 'admin@serago.com' LIMIT 1),
        '2026-08-12T09:00:00Z',
        '2026-08-12T09:00:00Z',
        NULL,
        '',
        '2026-08-12T09:00:00Z',
        '2026-08-12T09:00:00Z'
    ),
    (
        gen_random_uuid(),
        'Frontend Engineer (React)',
        'Join our product team building a modern web platform used by thousands of daily users. You will work with React, TypeScript, and Tailwind to ship clean, accessible UI. We value ownership, clear communication, and attention to detail.',
        'FinPay Ethiopia',
        'Remote',
        0, -- FullTime
        'https://example.com/apply/frontend-engineer-react',
        'ETB 70,000 – 95,000 / month',
        '2026-08-11T08:30:00Z',
        '2026-09-15T17:00:00Z',
        2, -- Published
        true,
        (SELECT "Id" FROM "AspNetUsers" WHERE "Email" = 'admin@serago.com' LIMIT 1),
        '2026-08-11T08:30:00Z',
        '2026-08-11T08:30:00Z',
        NULL,
        '',
        '2026-08-11T08:30:00Z',
        '2026-08-11T08:30:00Z'
    ),
    (
        gen_random_uuid(),
        'Backend Engineer (.NET / Node.js)',
        'We are scaling our core services and need a backend engineer comfortable with .NET or Node.js, PostgreSQL, and REST APIs. You will design data models, build resilient endpoints, and keep our platform fast and reliable.',
        'SeraGo',
        'Addis Ababa, Ethiopia',
        0, -- FullTime
        'https://example.com/apply/backend-engineer',
        'ETB 80,000 – 110,000 / month',
        '2026-08-10T10:00:00Z',
        '2026-09-30T17:00:00Z',
        2, -- Published
        true,
        (SELECT "Id" FROM "AspNetUsers" WHERE "Email" = 'admin@serago.com' LIMIT 1),
        '2026-08-10T10:00:00Z',
        '2026-08-10T10:00:00Z',
        NULL,
        '',
        '2026-08-10T10:00:00Z',
        '2026-08-10T10:00:00Z'
    ),
    (
        gen_random_uuid(),
        'Data Analyst',
        'Turn raw data into decisions. You will own reporting dashboards, run analyses on user behaviour, and partner with product and finance teams. Strong SQL and Excel skills are a must; familiarity with Python or Power BI is a plus.',
        'Habesha Bank',
        'Addis Ababa, Ethiopia',
        0, -- FullTime
        'https://example.com/apply/data-analyst',
        'ETB 55,000 – 75,000 / month',
        '2026-08-09T09:00:00Z',
        '2026-09-10T17:00:00Z',
        2, -- Published
        true,
        (SELECT "Id" FROM "AspNetUsers" WHERE "Email" = 'admin@serago.com' LIMIT 1),
        '2026-08-09T09:00:00Z',
        '2026-08-09T09:00:00Z',
        NULL,
        '',
        '2026-08-09T09:00:00Z',
        '2026-08-09T09:00:00Z'
    ),
    (
        gen_random_uuid(),
        'UI/UX Designer',
        'We are looking for a designer who cares about the details. You will craft intuitive product flows, maintain our design system, and collaborate daily with engineers. A portfolio showing end-to-end product design is required.',
        'Craft Digital Studio',
        'Remote',
        2, -- Contract
        'https://example.com/apply/ui-ux-designer',
        'ETB 60,000 – 80,000 / month',
        '2026-08-08T08:00:00Z',
        '2026-09-05T17:00:00Z',
        2, -- Published
        true,
        (SELECT "Id" FROM "AspNetUsers" WHERE "Email" = 'admin@serago.com' LIMIT 1),
        '2026-08-08T08:00:00Z',
        '2026-08-08T08:00:00Z',
        NULL,
        '',
        '2026-08-08T08:00:00Z',
        '2026-08-08T08:00:00Z'
    ),
    (
        gen_random_uuid(),
        'Marketing Officer',
        'Help us grow our brand across Ethiopia. You will plan campaigns, manage social media channels, and track performance against clear targets. We need a creative, organized marketer with 2+ years of experience and strong Amharic and English writing skills.',
        'GreenLeaf Agro',
        'Addis Ababa, Ethiopia',
        0, -- FullTime
        'https://example.com/apply/marketing-officer',
        'ETB 30,000 – 45,000 / month',
        '2026-08-07T09:30:00Z',
        '2026-09-25T17:00:00Z',
        2, -- Published
        true,
        (SELECT "Id" FROM "AspNetUsers" WHERE "Email" = 'admin@serago.com' LIMIT 1),
        '2026-08-07T09:30:00Z',
        '2026-08-07T09:30:00Z',
        NULL,
        '',
        '2026-08-07T09:30:00Z',
        '2026-08-07T09:30:00Z'
    ),
    (
        gen_random_uuid(),
        'Accountant',
        'Join our finance team to manage day-to-day bookkeeping, reconciliations, tax filings, and monthly reporting. You should be a certified accountant (or nearly finished) with 2+ years of experience and strong knowledge of Ethiopian tax law.',
        'Zemen Consulting',
        'Addis Ababa, Ethiopia',
        0, -- FullTime
        'https://example.com/apply/accountant',
        'ETB 35,000 – 50,000 / month',
        '2026-08-06T09:00:00Z',
        '2026-09-12T17:00:00Z',
        2, -- Published
        true,
        (SELECT "Id" FROM "AspNetUsers" WHERE "Email" = 'admin@serago.com' LIMIT 1),
        '2026-08-06T09:00:00Z',
        '2026-08-06T09:00:00Z',
        NULL,
        '',
        '2026-08-06T09:00:00Z',
        '2026-08-06T09:00:00Z'
    ),
    (
        gen_random_uuid(),
        'Customer Success Representative',
        'Be the friendly first line of support for our customers. You will answer questions over phone and chat, onboard new clients, and escalate technical issues. Excellent communication in Amharic and English and a calm, helpful attitude matter more than experience.',
        'Yegna Logistics',
        'Addis Ababa, Ethiopia',
        1, -- PartTime
        'https://example.com/apply/customer-success',
        'ETB 20,000 – 28,000 / month',
        '2026-08-05T10:30:00Z',
        '2026-09-08T17:00:00Z',
        2, -- Published
        true,
        (SELECT "Id" FROM "AspNetUsers" WHERE "Email" = 'admin@serago.com' LIMIT 1),
        '2026-08-05T10:30:00Z',
        '2026-08-05T10:30:00Z',
        NULL,
        '',
        '2026-08-05T10:30:00Z',
        '2026-08-05T10:30:00Z'
    ),
    (
        gen_random_uuid(),
        'Freelance Content Writer',
        'We publish daily content on careers, tech, and business in Ethiopia. You will pitch topics, research, and write engaging articles in English (Amharic a plus). Fully remote and flexible — paid per article with a monthly retainer option after a trial month.',
        'SeraGo Media',
        'Remote',
        6, -- Freelance
        'https://example.com/apply/content-writer',
        'ETB 15,000 – 25,000 / month',
        '2026-08-12T11:00:00Z',
        '2026-09-18T17:00:00Z',
        2, -- Published
        true,
        (SELECT "Id" FROM "AspNetUsers" WHERE "Email" = 'admin@serago.com' LIMIT 1),
        '2026-08-12T11:00:00Z',
        '2026-08-12T11:00:00Z',
        NULL,
        '',
        '2026-08-12T11:00:00Z',
        '2026-08-12T11:00:00Z'
    ),
    (
        gen_random_uuid(),
        'IT Support Specialist',
        'Keep our office running. You will set up and maintain computers and networks, manage user accounts, and help colleagues with everyday IT issues. A hands-on attitude and 1+ years of IT support experience are what we are looking for.',
        'Blue Nile University',
        'Addis Ababa, Ethiopia',
        0, -- FullTime
        'https://example.com/apply/it-support',
        'ETB 25,000 – 35,000 / month',
        '2026-08-04T08:30:00Z',
        '2026-09-22T17:00:00Z',
        2, -- Published
        true,
        (SELECT "Id" FROM "AspNetUsers" WHERE "Email" = 'admin@serago.com' LIMIT 1),
        '2026-08-04T08:30:00Z',
        '2026-08-04T08:30:00Z',
        NULL,
        '',
        '2026-08-04T08:30:00Z',
        '2026-08-04T08:30:00Z'
    );
