using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeraGo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobRefreshedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RefreshedAt",
                table: "Jobs",
                type: "timestamp with time zone",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefreshedAt",
                table: "Jobs");

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9844), new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9847) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9865), new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9866) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9868), new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9868) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9870), new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9871) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9901), new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9901) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000006"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9904), new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9904) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000007"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9906), new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9906) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000008"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9908), new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9909) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000009"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9910), new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9910) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000a"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9911), new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9912) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000b"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9913), new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9913) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000c"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9915), new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9916) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9918), new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9918) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9919), new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9919) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9921), new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9921) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000010"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9922), new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9923) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000011"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9924), new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9924) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000012"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9927), new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9927) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000013"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9928), new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9928) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000014"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9930), new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9930) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000015"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9931), new DateTime(2026, 8, 16, 9, 3, 22, 115, DateTimeKind.Utc).AddTicks(9932) });
        }
    }
}
