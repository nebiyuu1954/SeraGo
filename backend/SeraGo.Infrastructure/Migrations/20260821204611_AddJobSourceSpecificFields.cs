using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeraGo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobSourceSpecificFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationEmail",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationMethod",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUrl",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AreaName",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompensationAmountCents",
                table: "Jobs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompensationCurrency",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompensationType",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeadlineText",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmploymentText",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExperienceText",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobTime",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobTypeText",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxExperienceYears",
                table: "Jobs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfApplicants",
                table: "Jobs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostedText",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SiteJobType",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceCategories",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceSectors",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubSectorName",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpstreamSource",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9775), new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9780) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9798), new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9798) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9801), new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9801) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9804), new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9804) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9807), new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9808) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000006"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9809), new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9810) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000007"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9812), new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9812) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000008"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9814), new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9814) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000009"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9816), new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9817) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000a"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9819), new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9820) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000b"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9822), new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9823) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000c"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9824), new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9824) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9826), new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9826) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9828), new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9828) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9830), new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9830) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000010"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9835), new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9835) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000011"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9837), new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9837) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000012"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9839), new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9839) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000013"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9840), new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9841) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000014"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9842), new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9843) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000015"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9933), new DateTime(2026, 8, 21, 20, 46, 10, 873, DateTimeKind.Utc).AddTicks(9934) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicationEmail",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ApplicationMethod",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ApplicationUrl",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "AreaName",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "CompensationAmountCents",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "CompensationCurrency",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "CompensationType",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "DeadlineText",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "EmploymentText",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "EntityType",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ExperienceText",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "JobTime",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "JobTypeText",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "MaxExperienceYears",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "NumberOfApplicants",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "PostedText",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "SiteJobType",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "SourceCategories",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "SourceSectors",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "SubSectorName",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "UpstreamSource",
                table: "Jobs");

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(295), new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(299) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(315), new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(315) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(319), new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(319) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(322), new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(323) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(326), new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(326) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000006"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(327), new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(328) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000007"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(329), new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(329) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000008"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(330), new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(331) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000009"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(333), new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(333) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000a"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(335), new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(335) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000b"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(337), new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(338) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000c"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(339), new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(339) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(340), new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(341) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(342), new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(342) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(343), new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(344) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000010"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(346), new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(346) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000011"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(348), new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(348) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000012"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(349), new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(349) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000013"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(351), new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(352) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000014"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(353), new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(353) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000015"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(354), new DateTime(2026, 8, 21, 16, 42, 10, 901, DateTimeKind.Utc).AddTicks(354) });
        }
    }
}
