using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SeraGo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSectorsAndJobSector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<Guid>>(
                name: "PreferredSectorIds",
                table: "TalentProfiles",
                type: "uuid[]",
                nullable: false,
                defaultValue: new Guid[0]); // existing rows → empty array = "not configured"

            migrationBuilder.AddColumn<Guid>(
                name: "SectorId",
                table: "Jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Sectors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sectors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SectorAliases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Alias = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectorAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SectorAliases_Sectors_SectorId",
                        column: x => x.SectorId,
                        principalTable: "Sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Sectors",
                columns: new[] { "Id", "CreatedAt", "IsActive", "Name", "Slug", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("a0000000-0000-0000-0000-000000000001"), new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8035), true, "Technology & IT", "technology-it", new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8040) },
                    { new Guid("a0000000-0000-0000-0000-000000000002"), new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8066), true, "Accounting & Finance", "accounting-finance", new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8067) },
                    { new Guid("a0000000-0000-0000-0000-000000000003"), new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8071), true, "Banking & Insurance", "banking-insurance", new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8072) },
                    { new Guid("a0000000-0000-0000-0000-000000000004"), new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8076), true, "Sales & Marketing", "sales-marketing", new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8077) },
                    { new Guid("a0000000-0000-0000-0000-000000000005"), new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8082), true, "Healthcare", "healthcare", new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8082) },
                    { new Guid("a0000000-0000-0000-0000-000000000006"), new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8087), true, "Education & Training", "education-training", new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8088) },
                    { new Guid("a0000000-0000-0000-0000-000000000007"), new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8093), true, "Engineering & Construction", "engineering-construction", new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8094) },
                    { new Guid("a0000000-0000-0000-0000-000000000008"), new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8097), true, "Human Resources", "human-resources", new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8097) },
                    { new Guid("a0000000-0000-0000-0000-000000000009"), new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8101), true, "Business & Administration", "business-administration", new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8102) },
                    { new Guid("a0000000-0000-0000-0000-00000000000a"), new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8105), true, "Manufacturing & Production", "manufacturing-production", new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8106) },
                    { new Guid("a0000000-0000-0000-0000-00000000000b"), new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8110), true, "Logistics & Transportation", "logistics-transportation", new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8111) },
                    { new Guid("a0000000-0000-0000-0000-00000000000c"), new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8115), true, "Procurement & Supply Chain", "procurement-supply-chain", new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8115) },
                    { new Guid("a0000000-0000-0000-0000-00000000000d"), new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8119), true, "Media & Communication", "media-communication", new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8120) },
                    { new Guid("a0000000-0000-0000-0000-00000000000e"), new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8123), true, "Design & Creative", "design-creative", new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8124) },
                    { new Guid("a0000000-0000-0000-0000-00000000000f"), new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8128), true, "Customer Service & Support", "customer-service-support", new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8128) },
                    { new Guid("a0000000-0000-0000-0000-000000000010"), new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8131), true, "Hospitality & Tourism", "hospitality-tourism", new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8132) },
                    { new Guid("a0000000-0000-0000-0000-000000000011"), new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8135), true, "Agriculture & Natural Science", "agriculture-natural-science", new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8136) },
                    { new Guid("a0000000-0000-0000-0000-000000000012"), new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8140), true, "Legal", "legal", new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8140) },
                    { new Guid("a0000000-0000-0000-0000-000000000013"), new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8143), true, "Social Science & Community", "social-science-community", new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8144) },
                    { new Guid("a0000000-0000-0000-0000-000000000014"), new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8146), true, "Security & Protection", "security-protection", new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8147) },
                    { new Guid("a0000000-0000-0000-0000-000000000015"), new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8149), true, "Skilled & General Labor", "skilled-general-labor", new DateTime(2026, 8, 13, 12, 44, 19, 46, DateTimeKind.Utc).AddTicks(8150) }
                });

            migrationBuilder.InsertData(
                table: "SectorAliases",
                columns: new[] { "Id", "Alias", "SectorId" },
                values: new object[,]
                {
                    { new Guid("b0000000-0000-0000-0000-000000000000"), "information technology", new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("b0000000-0000-0000-0000-000000000001"), "technology", new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("b0000000-0000-0000-0000-000000000002"), "tech", new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("b0000000-0000-0000-0000-000000000003"), "it", new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("b0000000-0000-0000-0000-000000000004"), "ict", new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("b0000000-0000-0000-0000-000000000005"), "it support", new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("b0000000-0000-0000-0000-000000000006"), "computer science and information technology", new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("b0000000-0000-0000-0000-000000000007"), "it, computer science and software engineering", new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("b0000000-0000-0000-0000-000000000008"), "software design & development", new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("b0000000-0000-0000-0000-000000000009"), "data science & analytics", new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("b0000000-0000-0000-0000-00000000000a"), "software", new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("b0000000-0000-0000-0000-00000000000b"), "computer science", new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("b0000000-0000-0000-0000-00000000000c"), "accounting & finance", new Guid("a0000000-0000-0000-0000-000000000002") },
                    { new Guid("b0000000-0000-0000-0000-00000000000d"), "accounting", new Guid("a0000000-0000-0000-0000-000000000002") },
                    { new Guid("b0000000-0000-0000-0000-00000000000e"), "finance", new Guid("a0000000-0000-0000-0000-000000000002") },
                    { new Guid("b0000000-0000-0000-0000-00000000000f"), "accounting and finance", new Guid("a0000000-0000-0000-0000-000000000002") },
                    { new Guid("b0000000-0000-0000-0000-000000000010"), "economics", new Guid("a0000000-0000-0000-0000-000000000002") },
                    { new Guid("b0000000-0000-0000-0000-000000000011"), "tax", new Guid("a0000000-0000-0000-0000-000000000002") },
                    { new Guid("b0000000-0000-0000-0000-000000000012"), "audit", new Guid("a0000000-0000-0000-0000-000000000002") },
                    { new Guid("b0000000-0000-0000-0000-000000000013"), "bookkeeping", new Guid("a0000000-0000-0000-0000-000000000002") },
                    { new Guid("b0000000-0000-0000-0000-000000000014"), "banking and insurance", new Guid("a0000000-0000-0000-0000-000000000003") },
                    { new Guid("b0000000-0000-0000-0000-000000000015"), "banking", new Guid("a0000000-0000-0000-0000-000000000003") },
                    { new Guid("b0000000-0000-0000-0000-000000000016"), "insurance", new Guid("a0000000-0000-0000-0000-000000000003") },
                    { new Guid("b0000000-0000-0000-0000-000000000017"), "insurance and investment", new Guid("a0000000-0000-0000-0000-000000000003") },
                    { new Guid("b0000000-0000-0000-0000-000000000018"), "sales & promotion", new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("b0000000-0000-0000-0000-000000000019"), "sales", new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("b0000000-0000-0000-0000-00000000001a"), "marketing", new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("b0000000-0000-0000-0000-00000000001b"), "sales and marketing", new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("b0000000-0000-0000-0000-00000000001c"), "business sales and marketing", new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("b0000000-0000-0000-0000-00000000001d"), "marketing management", new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("b0000000-0000-0000-0000-00000000001e"), "digital marketing", new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("b0000000-0000-0000-0000-00000000001f"), "promotion", new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("b0000000-0000-0000-0000-000000000020"), "healthcare", new Guid("a0000000-0000-0000-0000-000000000005") },
                    { new Guid("b0000000-0000-0000-0000-000000000021"), "health care", new Guid("a0000000-0000-0000-0000-000000000005") },
                    { new Guid("b0000000-0000-0000-0000-000000000022"), "health care management", new Guid("a0000000-0000-0000-0000-000000000005") },
                    { new Guid("b0000000-0000-0000-0000-000000000023"), "public health", new Guid("a0000000-0000-0000-0000-000000000005") },
                    { new Guid("b0000000-0000-0000-0000-000000000024"), "nursing", new Guid("a0000000-0000-0000-0000-000000000005") },
                    { new Guid("b0000000-0000-0000-0000-000000000025"), "pharmaceutical", new Guid("a0000000-0000-0000-0000-000000000005") },
                    { new Guid("b0000000-0000-0000-0000-000000000026"), "pharmacy", new Guid("a0000000-0000-0000-0000-000000000005") },
                    { new Guid("b0000000-0000-0000-0000-000000000027"), "medicine", new Guid("a0000000-0000-0000-0000-000000000005") },
                    { new Guid("b0000000-0000-0000-0000-000000000028"), "medical", new Guid("a0000000-0000-0000-0000-000000000005") },
                    { new Guid("b0000000-0000-0000-0000-000000000029"), "teaching & education", new Guid("a0000000-0000-0000-0000-000000000006") },
                    { new Guid("b0000000-0000-0000-0000-00000000002a"), "education", new Guid("a0000000-0000-0000-0000-000000000006") },
                    { new Guid("b0000000-0000-0000-0000-00000000002b"), "teaching", new Guid("a0000000-0000-0000-0000-000000000006") },
                    { new Guid("b0000000-0000-0000-0000-00000000002c"), "tutoring, training & mentorship", new Guid("a0000000-0000-0000-0000-000000000006") },
                    { new Guid("b0000000-0000-0000-0000-00000000002d"), "language and literature", new Guid("a0000000-0000-0000-0000-000000000006") },
                    { new Guid("b0000000-0000-0000-0000-00000000002e"), "education management", new Guid("a0000000-0000-0000-0000-000000000006") },
                    { new Guid("b0000000-0000-0000-0000-00000000002f"), "tutoring", new Guid("a0000000-0000-0000-0000-000000000006") },
                    { new Guid("b0000000-0000-0000-0000-000000000030"), "training", new Guid("a0000000-0000-0000-0000-000000000006") },
                    { new Guid("b0000000-0000-0000-0000-000000000031"), "engineering", new Guid("a0000000-0000-0000-0000-000000000007") },
                    { new Guid("b0000000-0000-0000-0000-000000000032"), "construction & civil engineering", new Guid("a0000000-0000-0000-0000-000000000007") },
                    { new Guid("b0000000-0000-0000-0000-000000000033"), "civil engineering", new Guid("a0000000-0000-0000-0000-000000000007") },
                    { new Guid("b0000000-0000-0000-0000-000000000034"), "mechanical & electrical engineering", new Guid("a0000000-0000-0000-0000-000000000007") },
                    { new Guid("b0000000-0000-0000-0000-000000000035"), "chemical & biomedical engineering", new Guid("a0000000-0000-0000-0000-000000000007") },
                    { new Guid("b0000000-0000-0000-0000-000000000036"), "environmental, mining & energy engineering", new Guid("a0000000-0000-0000-0000-000000000007") },
                    { new Guid("b0000000-0000-0000-0000-000000000037"), "manufacturing engineering", new Guid("a0000000-0000-0000-0000-000000000007") },
                    { new Guid("b0000000-0000-0000-0000-000000000038"), "architectural engineering", new Guid("a0000000-0000-0000-0000-000000000007") },
                    { new Guid("b0000000-0000-0000-0000-000000000039"), "automotive engineering", new Guid("a0000000-0000-0000-0000-000000000007") },
                    { new Guid("b0000000-0000-0000-0000-00000000003a"), "sanitary engineering", new Guid("a0000000-0000-0000-0000-000000000007") },
                    { new Guid("b0000000-0000-0000-0000-00000000003b"), "biomedical engineering", new Guid("a0000000-0000-0000-0000-000000000007") },
                    { new Guid("b0000000-0000-0000-0000-00000000003c"), "electrical engineering", new Guid("a0000000-0000-0000-0000-000000000007") },
                    { new Guid("b0000000-0000-0000-0000-00000000003d"), "architecture & urban planning", new Guid("a0000000-0000-0000-0000-000000000007") },
                    { new Guid("b0000000-0000-0000-0000-00000000003e"), "construction skilled worker", new Guid("a0000000-0000-0000-0000-000000000007") },
                    { new Guid("b0000000-0000-0000-0000-00000000003f"), "mechanical engineering", new Guid("a0000000-0000-0000-0000-000000000007") },
                    { new Guid("b0000000-0000-0000-0000-000000000040"), "planning", new Guid("a0000000-0000-0000-0000-000000000007") },
                    { new Guid("b0000000-0000-0000-0000-000000000041"), "water and sanitation", new Guid("a0000000-0000-0000-0000-000000000007") },
                    { new Guid("b0000000-0000-0000-0000-000000000042"), "automotive", new Guid("a0000000-0000-0000-0000-000000000007") },
                    { new Guid("b0000000-0000-0000-0000-000000000043"), "human resource & talent management", new Guid("a0000000-0000-0000-0000-000000000008") },
                    { new Guid("b0000000-0000-0000-0000-000000000044"), "human resource and recruitment", new Guid("a0000000-0000-0000-0000-000000000008") },
                    { new Guid("b0000000-0000-0000-0000-000000000045"), "human resource administration", new Guid("a0000000-0000-0000-0000-000000000008") },
                    { new Guid("b0000000-0000-0000-0000-000000000046"), "human resources", new Guid("a0000000-0000-0000-0000-000000000008") },
                    { new Guid("b0000000-0000-0000-0000-000000000047"), "hr", new Guid("a0000000-0000-0000-0000-000000000008") },
                    { new Guid("b0000000-0000-0000-0000-000000000048"), "recruitment", new Guid("a0000000-0000-0000-0000-000000000008") },
                    { new Guid("b0000000-0000-0000-0000-000000000049"), "human resource", new Guid("a0000000-0000-0000-0000-000000000008") },
                    { new Guid("b0000000-0000-0000-0000-00000000004a"), "talent management", new Guid("a0000000-0000-0000-0000-000000000008") },
                    { new Guid("b0000000-0000-0000-0000-00000000004b"), "business", new Guid("a0000000-0000-0000-0000-000000000009") },
                    { new Guid("b0000000-0000-0000-0000-00000000004c"), "business administration", new Guid("a0000000-0000-0000-0000-000000000009") },
                    { new Guid("b0000000-0000-0000-0000-00000000004d"), "business administration & operations", new Guid("a0000000-0000-0000-0000-000000000009") },
                    { new Guid("b0000000-0000-0000-0000-00000000004e"), "business and administration", new Guid("a0000000-0000-0000-0000-000000000009") },
                    { new Guid("b0000000-0000-0000-0000-00000000004f"), "secretarial, admin and clerical", new Guid("a0000000-0000-0000-0000-000000000009") },
                    { new Guid("b0000000-0000-0000-0000-000000000050"), "secretarial & office management", new Guid("a0000000-0000-0000-0000-000000000009") },
                    { new Guid("b0000000-0000-0000-0000-000000000051"), "management", new Guid("a0000000-0000-0000-0000-000000000009") },
                    { new Guid("b0000000-0000-0000-0000-000000000052"), "development and project management", new Guid("a0000000-0000-0000-0000-000000000009") },
                    { new Guid("b0000000-0000-0000-0000-000000000053"), "advisory & consultancy", new Guid("a0000000-0000-0000-0000-000000000009") },
                    { new Guid("b0000000-0000-0000-0000-000000000054"), "brokerage & case closing", new Guid("a0000000-0000-0000-0000-000000000009") },
                    { new Guid("b0000000-0000-0000-0000-000000000055"), "business management", new Guid("a0000000-0000-0000-0000-000000000009") },
                    { new Guid("b0000000-0000-0000-0000-000000000056"), "administration", new Guid("a0000000-0000-0000-0000-000000000009") },
                    { new Guid("b0000000-0000-0000-0000-000000000057"), "office management", new Guid("a0000000-0000-0000-0000-000000000009") },
                    { new Guid("b0000000-0000-0000-0000-000000000058"), "secretarial", new Guid("a0000000-0000-0000-0000-000000000009") },
                    { new Guid("b0000000-0000-0000-0000-000000000059"), "manufacturing & production", new Guid("a0000000-0000-0000-0000-00000000000a") },
                    { new Guid("b0000000-0000-0000-0000-00000000005a"), "manufacturing", new Guid("a0000000-0000-0000-0000-00000000000a") },
                    { new Guid("b0000000-0000-0000-0000-00000000005b"), "manufacturing management", new Guid("a0000000-0000-0000-0000-00000000000a") },
                    { new Guid("b0000000-0000-0000-0000-00000000005c"), "fmcg and manufacturing", new Guid("a0000000-0000-0000-0000-00000000000a") },
                    { new Guid("b0000000-0000-0000-0000-00000000005d"), "woodwork & carpentry", new Guid("a0000000-0000-0000-0000-00000000000a") },
                    { new Guid("b0000000-0000-0000-0000-00000000005e"), "production", new Guid("a0000000-0000-0000-0000-00000000000a") },
                    { new Guid("b0000000-0000-0000-0000-00000000005f"), "transportation & delivery", new Guid("a0000000-0000-0000-0000-00000000000b") },
                    { new Guid("b0000000-0000-0000-0000-000000000060"), "transportation & logistics", new Guid("a0000000-0000-0000-0000-00000000000b") },
                    { new Guid("b0000000-0000-0000-0000-000000000061"), "transportation management", new Guid("a0000000-0000-0000-0000-00000000000b") },
                    { new Guid("b0000000-0000-0000-0000-000000000062"), "transportation", new Guid("a0000000-0000-0000-0000-00000000000b") },
                    { new Guid("b0000000-0000-0000-0000-000000000063"), "logistics & supply chain", new Guid("a0000000-0000-0000-0000-00000000000b") },
                    { new Guid("b0000000-0000-0000-0000-000000000064"), "logistics, transport and supply chain", new Guid("a0000000-0000-0000-0000-00000000000b") },
                    { new Guid("b0000000-0000-0000-0000-000000000065"), "logistics", new Guid("a0000000-0000-0000-0000-00000000000b") },
                    { new Guid("b0000000-0000-0000-0000-000000000066"), "warehouse, supply chain and distribution", new Guid("a0000000-0000-0000-0000-00000000000b") },
                    { new Guid("b0000000-0000-0000-0000-000000000067"), "purchasing & procurement", new Guid("a0000000-0000-0000-0000-00000000000c") },
                    { new Guid("b0000000-0000-0000-0000-000000000068"), "supply chain & purchasing management", new Guid("a0000000-0000-0000-0000-00000000000c") },
                    { new Guid("b0000000-0000-0000-0000-000000000069"), "procurement", new Guid("a0000000-0000-0000-0000-00000000000c") },
                    { new Guid("b0000000-0000-0000-0000-00000000006a"), "supply chain", new Guid("a0000000-0000-0000-0000-00000000000c") },
                    { new Guid("b0000000-0000-0000-0000-00000000006b"), "purchasing", new Guid("a0000000-0000-0000-0000-00000000000c") },
                    { new Guid("b0000000-0000-0000-0000-00000000006c"), "media & entertainment", new Guid("a0000000-0000-0000-0000-00000000000d") },
                    { new Guid("b0000000-0000-0000-0000-00000000006d"), "media and communication", new Guid("a0000000-0000-0000-0000-00000000000d") },
                    { new Guid("b0000000-0000-0000-0000-00000000006e"), "multimedia content production", new Guid("a0000000-0000-0000-0000-00000000000d") },
                    { new Guid("b0000000-0000-0000-0000-00000000006f"), "documentation & writing", new Guid("a0000000-0000-0000-0000-00000000000d") },
                    { new Guid("b0000000-0000-0000-0000-000000000070"), "translation & transcription", new Guid("a0000000-0000-0000-0000-00000000000d") },
                    { new Guid("b0000000-0000-0000-0000-000000000071"), "media", new Guid("a0000000-0000-0000-0000-00000000000d") },
                    { new Guid("b0000000-0000-0000-0000-000000000072"), "journalism", new Guid("a0000000-0000-0000-0000-00000000000d") },
                    { new Guid("b0000000-0000-0000-0000-000000000073"), "communication", new Guid("a0000000-0000-0000-0000-00000000000d") },
                    { new Guid("b0000000-0000-0000-0000-000000000074"), "writing", new Guid("a0000000-0000-0000-0000-00000000000d") },
                    { new Guid("b0000000-0000-0000-0000-000000000075"), "creative art & design", new Guid("a0000000-0000-0000-0000-00000000000e") },
                    { new Guid("b0000000-0000-0000-0000-000000000076"), "fashion / clothing & textile design", new Guid("a0000000-0000-0000-0000-00000000000e") },
                    { new Guid("b0000000-0000-0000-0000-000000000077"), "design", new Guid("a0000000-0000-0000-0000-00000000000e") },
                    { new Guid("b0000000-0000-0000-0000-000000000078"), "graphic design", new Guid("a0000000-0000-0000-0000-00000000000e") },
                    { new Guid("b0000000-0000-0000-0000-000000000079"), "creative", new Guid("a0000000-0000-0000-0000-00000000000e") },
                    { new Guid("b0000000-0000-0000-0000-00000000007a"), "fashion design", new Guid("a0000000-0000-0000-0000-00000000000e") },
                    { new Guid("b0000000-0000-0000-0000-00000000007b"), "creative arts", new Guid("a0000000-0000-0000-0000-00000000000e") },
                    { new Guid("b0000000-0000-0000-0000-00000000007c"), "ui/ux design", new Guid("a0000000-0000-0000-0000-00000000000e") },
                    { new Guid("b0000000-0000-0000-0000-00000000007d"), "fashion", new Guid("a0000000-0000-0000-0000-00000000000e") },
                    { new Guid("b0000000-0000-0000-0000-00000000007e"), "customer service & care", new Guid("a0000000-0000-0000-0000-00000000000f") },
                    { new Guid("b0000000-0000-0000-0000-00000000007f"), "retail & office support", new Guid("a0000000-0000-0000-0000-00000000000f") },
                    { new Guid("b0000000-0000-0000-0000-000000000080"), "customer service", new Guid("a0000000-0000-0000-0000-00000000000f") },
                    { new Guid("b0000000-0000-0000-0000-000000000081"), "support", new Guid("a0000000-0000-0000-0000-00000000000f") },
                    { new Guid("b0000000-0000-0000-0000-000000000082"), "call center", new Guid("a0000000-0000-0000-0000-00000000000f") },
                    { new Guid("b0000000-0000-0000-0000-000000000083"), "reception", new Guid("a0000000-0000-0000-0000-00000000000f") },
                    { new Guid("b0000000-0000-0000-0000-000000000084"), "hospitality & tourism", new Guid("a0000000-0000-0000-0000-000000000010") },
                    { new Guid("b0000000-0000-0000-0000-000000000085"), "food & drink preparation / service", new Guid("a0000000-0000-0000-0000-000000000010") },
                    { new Guid("b0000000-0000-0000-0000-000000000086"), "hospitality", new Guid("a0000000-0000-0000-0000-000000000010") },
                    { new Guid("b0000000-0000-0000-0000-000000000087"), "tourism", new Guid("a0000000-0000-0000-0000-000000000010") },
                    { new Guid("b0000000-0000-0000-0000-000000000088"), "chef", new Guid("a0000000-0000-0000-0000-000000000010") },
                    { new Guid("b0000000-0000-0000-0000-000000000089"), "catering", new Guid("a0000000-0000-0000-0000-000000000010") },
                    { new Guid("b0000000-0000-0000-0000-00000000008a"), "hotel", new Guid("a0000000-0000-0000-0000-000000000010") },
                    { new Guid("b0000000-0000-0000-0000-00000000008b"), "agriculture", new Guid("a0000000-0000-0000-0000-000000000011") },
                    { new Guid("b0000000-0000-0000-0000-00000000008c"), "agricultural science", new Guid("a0000000-0000-0000-0000-000000000011") },
                    { new Guid("b0000000-0000-0000-0000-00000000008d"), "natural science", new Guid("a0000000-0000-0000-0000-000000000011") },
                    { new Guid("b0000000-0000-0000-0000-00000000008e"), "natural sciences", new Guid("a0000000-0000-0000-0000-000000000011") },
                    { new Guid("b0000000-0000-0000-0000-00000000008f"), "chemistry", new Guid("a0000000-0000-0000-0000-000000000011") },
                    { new Guid("b0000000-0000-0000-0000-000000000090"), "physics", new Guid("a0000000-0000-0000-0000-000000000011") },
                    { new Guid("b0000000-0000-0000-0000-000000000091"), "microbiology", new Guid("a0000000-0000-0000-0000-000000000011") },
                    { new Guid("b0000000-0000-0000-0000-000000000092"), "mathematics", new Guid("a0000000-0000-0000-0000-000000000011") },
                    { new Guid("b0000000-0000-0000-0000-000000000093"), "nutrition", new Guid("a0000000-0000-0000-0000-000000000011") },
                    { new Guid("b0000000-0000-0000-0000-000000000094"), "biology", new Guid("a0000000-0000-0000-0000-000000000011") },
                    { new Guid("b0000000-0000-0000-0000-000000000095"), "law & legal advocacy", new Guid("a0000000-0000-0000-0000-000000000012") },
                    { new Guid("b0000000-0000-0000-0000-000000000096"), "legal services", new Guid("a0000000-0000-0000-0000-000000000012") },
                    { new Guid("b0000000-0000-0000-0000-000000000097"), "legal", new Guid("a0000000-0000-0000-0000-000000000012") },
                    { new Guid("b0000000-0000-0000-0000-000000000098"), "law", new Guid("a0000000-0000-0000-0000-000000000012") },
                    { new Guid("b0000000-0000-0000-0000-000000000099"), "advocacy", new Guid("a0000000-0000-0000-0000-000000000012") },
                    { new Guid("b0000000-0000-0000-0000-00000000009a"), "social sciences and community service", new Guid("a0000000-0000-0000-0000-000000000013") },
                    { new Guid("b0000000-0000-0000-0000-00000000009b"), "social science", new Guid("a0000000-0000-0000-0000-000000000013") },
                    { new Guid("b0000000-0000-0000-0000-00000000009c"), "social work", new Guid("a0000000-0000-0000-0000-000000000013") },
                    { new Guid("b0000000-0000-0000-0000-00000000009d"), "community service", new Guid("a0000000-0000-0000-0000-000000000013") },
                    { new Guid("b0000000-0000-0000-0000-00000000009e"), "history", new Guid("a0000000-0000-0000-0000-000000000013") },
                    { new Guid("b0000000-0000-0000-0000-00000000009f"), "sociology", new Guid("a0000000-0000-0000-0000-000000000013") },
                    { new Guid("b0000000-0000-0000-0000-0000000000a0"), "psychology", new Guid("a0000000-0000-0000-0000-000000000013") },
                    { new Guid("b0000000-0000-0000-0000-0000000000a1"), "security", new Guid("a0000000-0000-0000-0000-000000000014") },
                    { new Guid("b0000000-0000-0000-0000-0000000000a2"), "protection", new Guid("a0000000-0000-0000-0000-000000000014") },
                    { new Guid("b0000000-0000-0000-0000-0000000000a3"), "guard", new Guid("a0000000-0000-0000-0000-000000000014") },
                    { new Guid("b0000000-0000-0000-0000-0000000000a4"), "safety", new Guid("a0000000-0000-0000-0000-000000000014") },
                    { new Guid("b0000000-0000-0000-0000-0000000000a5"), "low and medium skilled worker", new Guid("a0000000-0000-0000-0000-000000000015") },
                    { new Guid("b0000000-0000-0000-0000-0000000000a6"), "service industry skilled worker", new Guid("a0000000-0000-0000-0000-000000000015") },
                    { new Guid("b0000000-0000-0000-0000-0000000000a7"), "janitorial & office services", new Guid("a0000000-0000-0000-0000-000000000015") },
                    { new Guid("b0000000-0000-0000-0000-0000000000a8"), "general labor", new Guid("a0000000-0000-0000-0000-000000000015") },
                    { new Guid("b0000000-0000-0000-0000-0000000000a9"), "cleaner", new Guid("a0000000-0000-0000-0000-000000000015") },
                    { new Guid("b0000000-0000-0000-0000-0000000000aa"), "maintenance", new Guid("a0000000-0000-0000-0000-000000000015") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_SectorId",
                table: "Jobs",
                column: "SectorId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_SectorId_Status_IsActive",
                table: "Jobs",
                columns: new[] { "SectorId", "Status", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SectorAliases_Alias",
                table: "SectorAliases",
                column: "Alias",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SectorAliases_SectorId",
                table: "SectorAliases",
                column: "SectorId");

            migrationBuilder.CreateIndex(
                name: "IX_Sectors_IsActive",
                table: "Sectors",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Sectors_Name",
                table: "Sectors",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sectors_Slug",
                table: "Sectors",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Sectors_SectorId",
                table: "Jobs",
                column: "SectorId",
                principalTable: "Sectors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Sectors_SectorId",
                table: "Jobs");

            migrationBuilder.DropTable(
                name: "SectorAliases");

            migrationBuilder.DropTable(
                name: "Sectors");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_SectorId",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_SectorId_Status_IsActive",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "PreferredSectorIds",
                table: "TalentProfiles");

            migrationBuilder.DropColumn(
                name: "SectorId",
                table: "Jobs");
        }
    }
}
