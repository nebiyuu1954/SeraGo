using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeraGo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTalentProfileEducationPrivacy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "TalentProfiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CurrentIndustry",
                table: "TalentProfiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CurrentProfession",
                table: "TalentProfiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfBirth",
                table: "TalentProfiles",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EducationHistory",
                table: "TalentProfiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EducationLevel",
                table: "TalentProfiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PreferredLocations",
                table: "TalentProfiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProfileVisibility",
                table: "TalentProfiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProfileSnapshot",
                table: "JobApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MiddleName",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5380), new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5385) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5404), new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5404) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5408), new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5408) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5414), new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5415) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5418), new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5418) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000006"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5420), new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5421) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000007"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5423), new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5423) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000008"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5426), new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5427) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000009"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5429), new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5430) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000a"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5432), new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5432) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000b"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5436), new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5436) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000c"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5439), new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5440) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5442), new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5442) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5445), new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5445) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5448), new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5448) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000010"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5451), new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5451) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000011"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5454), new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5454) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000012"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5458), new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5458) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000013"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5461), new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5461) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000014"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5464), new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5464) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000015"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5467), new DateTime(2026, 8, 23, 15, 24, 14, 684, DateTimeKind.Utc).AddTicks(5467) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "TalentProfiles");

            migrationBuilder.DropColumn(
                name: "CurrentIndustry",
                table: "TalentProfiles");

            migrationBuilder.DropColumn(
                name: "CurrentProfession",
                table: "TalentProfiles");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "TalentProfiles");

            migrationBuilder.DropColumn(
                name: "EducationHistory",
                table: "TalentProfiles");

            migrationBuilder.DropColumn(
                name: "EducationLevel",
                table: "TalentProfiles");

            migrationBuilder.DropColumn(
                name: "PreferredLocations",
                table: "TalentProfiles");

            migrationBuilder.DropColumn(
                name: "ProfileVisibility",
                table: "TalentProfiles");

            migrationBuilder.DropColumn(
                name: "ProfileSnapshot",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "MiddleName",
                table: "AspNetUsers");

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9737), new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9743) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9775), new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9775) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9781), new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9781) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9785), new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9786) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9790), new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9791) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000006"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9795), new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9795) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000007"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9799), new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9799) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000008"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9803), new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9804) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000009"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9809), new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9809) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000a"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9813), new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9814) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000b"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9818), new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9818) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000c"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9825), new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9825) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9829), new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9830) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9834), new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9835) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9838), new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9853) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000010"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9856), new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9856) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000011"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9861), new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9862) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000012"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9924), new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9925) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000013"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9929), new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9929) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000014"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9932), new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9932) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000015"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9936), new DateTime(2026, 8, 23, 15, 7, 2, 967, DateTimeKind.Utc).AddTicks(9937) });
        }
    }
}
