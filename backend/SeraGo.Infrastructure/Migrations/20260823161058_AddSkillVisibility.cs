using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeraGo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SkillVisibility",
                table: "TalentProfiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9648), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9652) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9666), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9666) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9669), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9670) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9676), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9676) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9678), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9678) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000006"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9679), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9680) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000007"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9682), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9682) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000008"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9685), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9685) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000009"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9687), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9688) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000a"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9690), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9690) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000b"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9692), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9692) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000c"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9694), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9694) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9696), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9696) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9697), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9697) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9698), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9699) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000010"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9700), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9700) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000011"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9702), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9702) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000012"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9704), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9705) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000013"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9706), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9706) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000014"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9708), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9708) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000015"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9709), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9710) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SkillVisibility",
                table: "TalentProfiles");

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
    }
}
