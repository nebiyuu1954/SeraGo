-- ============================================================================
-- SeraGo — development seed: 10 jobs from the source websites
-- ============================================================================
-- Inserts 10 published jobs with SourceName/SourceUrl/ExternalId set, so the
-- talent dashboard shows each job's source website (brand logo + "via X"
-- link). Run after the AddJobSourceFields migration has been applied.
--
-- Run it against the local database, e.g.:
--     psql "Host=localhost;Database=serago;Username=postgres;Password=postgres" -f SeedScrapedJobs.sql
-- (or paste it into your SQL tool / Neon console).
--
-- Notes:
--   * PostedByUserId resolves to the bootstrap admin (Admin:Email in
--     appsettings.Development.json). Change the email if yours differs.
--   * (SourceName, ExternalId) has a UNIQUE index — re-running this file
--     errors on the duplicates, so it is safe to re-apply.
--   * gen_random_uuid() needs Postgres 13+ (fine on Neon and modern local).
-- ============================================================================

INSERT INTO "Jobs"
    ("Id", "Title", "Description", "Company", "Location", "JobType",
     "Url", "Salary", "PublishedAt", "Deadline", "Status", "IsActive",
     "PostedByUserId", "SubmittedAt", "ApprovedAt", "RejectedAt",
     "RejectionReason", "CreatedAt", "UpdatedAt",
     "SourceName", "SourceUrl", "ExternalId", "CompanyLogoUrl",
     "SectorName", "ExperienceLevel")
VALUES
    -- Afriwork (2)
    (
        gen_random_uuid(),
        'Senior Graphic Designer',
        'Design campaign visuals, social media creatives, and brand assets for a fast-growing agency. You will own the design process from brief to final export and collaborate with copywriters and account managers.',
        'Creative Hub Ethiopia',
        'Addis Ababa, Ethiopia',
        0, -- FullTime
        'https://afriworkjobs.com/jobs/senior-graphic-designer',
        'ETB 40,000 – 60,000 / month',
        '2026-08-12T09:00:00Z',
        '2026-09-18T17:00:00Z',
        2, -- Published
        true,
        (SELECT "Id" FROM "AspNetUsers" WHERE "Email" = 'admin@serago.com' LIMIT 1),
        '2026-08-12T09:00:00Z',
        '2026-08-12T09:00:00Z',
        NULL,
        '',
        '2026-08-12T09:00:00Z',
        '2026-08-12T09:00:00Z',
        'Afriwork',
        'https://afriworkjobs.com/jobs/senior-graphic-designer',
        'afriwork-1001',
        NULL,
        'Design',
        'Senior'
    ),
    (
        gen_random_uuid(),
        'Full-Stack Web Developer',
        'Build and maintain client websites and internal tools end to end. Comfort with Laravel or Django on the back end and modern JavaScript on the front end is required, plus solid database fundamentals.',
        'Addis Software',
        'Addis Ababa, Ethiopia',
        0, -- FullTime
        'https://afriworkjobs.com/jobs/full-stack-web-developer',
        'ETB 75,000 – 100,000 / month',
        '2026-08-11T10:00:00Z',
        '2026-09-25T17:00:00Z',
        2, -- Published
        true,
        (SELECT "Id" FROM "AspNetUsers" WHERE "Email" = 'admin@serago.com' LIMIT 1),
        '2026-08-11T10:00:00Z',
        '2026-08-11T10:00:00Z',
        NULL,
        '',
        '2026-08-11T10:00:00Z',
        '2026-08-11T10:00:00Z',
        'Afriwork',
        'https://afriworkjobs.com/jobs/full-stack-web-developer',
        'afriwork-1002',
        NULL,
        'Technology',
        'Mid'
    ),

    -- EthioJobs (2)
    (
        gen_random_uuid(),
        'Banking Operations Officer',
        'Handle daily branch operations, account maintenance, and customer transactions with accuracy and care. A degree in accounting, finance, or management plus 1+ year in banking operations is preferred.',
        'Dashen Bank',
        'Addis Ababa, Ethiopia',
        0, -- FullTime
        'https://www.ethiojobs.net/jobs/banking-operations-officer',
        'ETB 45,000 – 65,000 / month',
        '2026-08-10T08:30:00Z',
        '2026-09-10T17:00:00Z',
        2, -- Published
        true,
        (SELECT "Id" FROM "AspNetUsers" WHERE "Email" = 'admin@serago.com' LIMIT 1),
        '2026-08-10T08:30:00Z',
        '2026-08-10T08:30:00Z',
        NULL,
        '',
        '2026-08-10T08:30:00Z',
        '2026-08-10T08:30:00Z',
        'EthioJobs',
        'https://www.ethiojobs.net/jobs/banking-operations-officer',
        'ethiojobs-2001',
        NULL,
        'Banking and Insurance',
        'Entry'
    ),
    (
        gen_random_uuid(),
        'Registered Nurse',
        'Provide patient care in a busy private hospital, including medication administration, patient assessment, and family education. BSc in Nursing and a valid license to practice in Ethiopia are required.',
        'Kadisco General Hospital',
        'Addis Ababa, Ethiopia',
        2, -- Contract
        'https://www.ethiojobs.net/jobs/registered-nurse',
        'ETB 30,000 – 40,000 / month',
        '2026-08-09T09:00:00Z',
        '2026-09-05T17:00:00Z',
        2, -- Published
        true,
        (SELECT "Id" FROM "AspNetUsers" WHERE "Email" = 'admin@serago.com' LIMIT 1),
        '2026-08-09T09:00:00Z',
        '2026-08-09T09:00:00Z',
        NULL,
        '',
        '2026-08-09T09:00:00Z',
        '2026-08-09T09:00:00Z',
        'EthioJobs',
        'https://www.ethiojobs.net/jobs/registered-nurse',
        'ethiojobs-2002',
        NULL,
        'Healthcare',
        'Junior'
    ),

    -- GeezJobs (2)
    (
        gen_random_uuid(),
        'Civil Engineer',
        'Supervise construction works, review structural drawings, and ensure quality and safety on site. BSc in Civil Engineering with 5+ years of experience in building projects is required.',
        '4B Trading PLC',
        'Addis Ababa, Ethiopia',
        0, -- FullTime
        'https://geezjobs.com/jobs/civil-engineer',
        'ETB 55,000 – 80,000 / month',
        '2026-08-08T08:00:00Z',
        '2026-09-15T17:00:00Z',
        2, -- Published
        true,
        (SELECT "Id" FROM "AspNetUsers" WHERE "Email" = 'admin@serago.com' LIMIT 1),
        '2026-08-08T08:00:00Z',
        '2026-08-08T08:00:00Z',
        NULL,
        '',
        '2026-08-08T08:00:00Z',
        '2026-08-08T08:00:00Z',
        'GeezJobs',
        'https://geezjobs.com/jobs/civil-engineer',
        'geezjobs-3001',
        NULL,
        'Engineering',
        'Senior'
    ),
    (
        gen_random_uuid(),
        'Sales Manager',
        'Lead a sales team, set targets, and grow the company\u2019s B2B accounts across the region. Proven track record in sales leadership and strong relationship-building skills are essential.',
        'Merkato Traders',
        'Addis Ababa, Ethiopia',
        0, -- FullTime
        'https://geezjobs.com/jobs/sales-manager',
        'ETB 50,000 – 70,000 / month',
        '2026-08-07T10:00:00Z',
        '2026-09-20T17:00:00Z',
        2, -- Published
        true,
        (SELECT "Id" FROM "AspNetUsers" WHERE "Email" = 'admin@serago.com' LIMIT 1),
        '2026-08-07T10:00:00Z',
        '2026-08-07T10:00:00Z',
        NULL,
        '',
        '2026-08-07T10:00:00Z',
        '2026-08-07T10:00:00Z',
        'GeezJobs',
        'https://geezjobs.com/jobs/sales-manager',
        'geezjobs-3002',
        NULL,
        'Sales',
        'Mid'
    ),

    -- Ethiopian Reporter Jobs (2)
    (
        gen_random_uuid(),
        'Journalist / Reporter',
        'Research, write, and file news stories on deadline for a national weekly. Strong Amharic and English writing, sharp news judgement, and willingness to travel within Ethiopia are required.',
        'The Ethiopian Reporter',
        'Addis Ababa, Ethiopia',
        0, -- FullTime
        'https://www.ethiopianreporterjobs.com/jobs/journalist',
        'ETB 25,000 – 40,000 / month',
        '2026-08-06T09:30:00Z',
        '2026-09-08T17:00:00Z',
        2, -- Published
        true,
        (SELECT "Id" FROM "AspNetUsers" WHERE "Email" = 'admin@serago.com' LIMIT 1),
        '2026-08-06T09:30:00Z',
        '2026-08-06T09:30:00Z',
        NULL,
        '',
        '2026-08-06T09:30:00Z',
        '2026-08-06T09:30:00Z',
        'Ethiopian Reporter Jobs',
        'https://www.ethiopianreporterjobs.com/jobs/journalist',
        'reporter-4001',
        NULL,
        'Media',
        'Entry'
    ),
    (
        gen_random_uuid(),
        'HR Officer',
        'Support recruitment, onboarding, payroll liaison, and employee relations at a mid-size firm. BA in management or HR with 2+ years of HR experience and familiarity with Ethiopian labour law is preferred.',
        'Kebede & Associates',
        'Addis Ababa, Ethiopia',
        0, -- FullTime
        'https://www.ethiopianreporterjobs.com/jobs/hr-officer',
        'ETB 30,000 – 45,000 / month',
        '2026-08-05T09:00:00Z',
        '2026-09-12T17:00:00Z',
        2, -- Published
        true,
        (SELECT "Id" FROM "AspNetUsers" WHERE "Email" = 'admin@serago.com' LIMIT 1),
        '2026-08-05T09:00:00Z',
        '2026-08-05T09:00:00Z',
        NULL,
        '',
        '2026-08-05T09:00:00Z',
        '2026-08-05T09:00:00Z',
        'Ethiopian Reporter Jobs',
        'https://www.ethiopianreporterjobs.com/jobs/hr-officer',
        'reporter-4002',
        NULL,
        'Human Resources',
        'Mid'
    ),

    -- HaHuJobs (2)
    (
        gen_random_uuid(),
        'Software Quality Assurance Engineer',
        'Write and execute test plans, automate regression suites, and report defects clearly. Experience with manual and automated testing of web applications and strong attention to detail are required.',
        'TechnoServe Ethiopia',
        'Addis Ababa, Ethiopia',
        0, -- FullTime
        'https://hahu.jobs/jobs/software-qa-engineer',
        'ETB 60,000 – 85,000 / month',
        '2026-08-12T11:00:00Z',
        '2026-09-22T17:00:00Z',
        2, -- Published
        true,
        (SELECT "Id" FROM "AspNetUsers" WHERE "Email" = 'admin@serago.com' LIMIT 1),
        '2026-08-12T11:00:00Z',
        '2026-08-12T11:00:00Z',
        NULL,
        '',
        '2026-08-12T11:00:00Z',
        '2026-08-12T11:00:00Z',
        'HaHuJobs',
        'https://hahu.jobs/jobs/software-qa-engineer',
        'hahujobs-5001',
        NULL,
        'Technology',
        'Mid'
    ),
    (
        gen_random_uuid(),
        'Digital Marketing Specialist',
        'Run paid and organic campaigns across social media and search, analyse performance, and report on ROI. Hands-on experience with Meta and Google Ads plus strong copywriting skills are preferred.',
        'Zemen Digital Agency',
        'Addis Ababa, Ethiopia',
        0, -- FullTime
        'https://hahu.jobs/jobs/digital-marketing-specialist',
        'ETB 35,000 – 50,000 / month',
        '2026-08-11T09:00:00Z',
        '2026-09-18T17:00:00Z',
        2, -- Published
        true,
        (SELECT "Id" FROM "AspNetUsers" WHERE "Email" = 'admin@serago.com' LIMIT 1),
        '2026-08-11T09:00:00Z',
        '2026-08-11T09:00:00Z',
        NULL,
        '',
        '2026-08-11T09:00:00Z',
        '2026-08-11T09:00:00Z',
        'HaHuJobs',
        'https://hahu.jobs/jobs/digital-marketing-specialist',
        'hahujobs-5002',
        NULL,
        'Marketing',
        'Junior'
    );
