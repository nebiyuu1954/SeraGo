using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeraGo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobWorkMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkMode",
                table: "Jobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkMode",
                table: "Jobs");

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7118), new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7122) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7146), new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7147) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7151), new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7152) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7159), new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7159) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7162), new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7163) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000006"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7166), new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7167) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000007"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7171), new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7171) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000008"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7175), new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7176) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000009"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7179), new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7180) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000a"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7183), new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7183) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000b"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7187), new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7187) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000c"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7191), new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7191) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7195), new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7195) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7199), new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7200) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7203), new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7204) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000010"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7207), new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7208) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000011"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7211), new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7212) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000012"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7215), new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7216) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000013"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7219), new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7220) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000014"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7223), new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7223) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000015"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7226), new DateTime(2026, 8, 17, 8, 48, 4, 966, DateTimeKind.Utc).AddTicks(7227) });
        }
    }
}
