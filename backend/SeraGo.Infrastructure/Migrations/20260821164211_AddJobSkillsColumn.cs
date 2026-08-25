using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeraGo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobSkillsColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Skills",
                table: "Jobs",
                type: "text",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Skills",
                table: "Jobs");

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(3771), new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(3777) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4049), new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4050) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4056), new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4057) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4061), new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4062) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4069), new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4069) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000006"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4073), new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4074) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000007"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4078), new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4079) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000008"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4083), new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4084) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000009"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4088), new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4088) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000a"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4092), new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4093) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000b"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4097), new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4097) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000c"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4101), new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4101) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4105), new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4105) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4110), new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4110) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4114), new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4114) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000010"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4118), new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4119) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000011"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4122), new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4123) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000012"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4126), new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4127) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000013"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4131), new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4132) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000014"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4137), new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4138) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000015"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4143), new DateTime(2026, 8, 17, 11, 38, 40, 215, DateTimeKind.Utc).AddTicks(4144) });
        }
    }
}
