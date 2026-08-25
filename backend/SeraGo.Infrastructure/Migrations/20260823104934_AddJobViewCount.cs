using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeraGo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobViewCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "Jobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(915), new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(921) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(957), new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(958) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(963), new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(963) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(968), new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(968) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(973), new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(974) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000006"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(977), new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(978) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000007"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(982), new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(982) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000008"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(986), new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(986) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000009"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(990), new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(990) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000a"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(994), new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(995) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000b"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(998), new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(999) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000c"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(1005), new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(1005) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(1009), new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(1009) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(1013), new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(1014) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(1017), new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(1025) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000010"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(1029), new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(1029) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000011"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(1033), new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(1034) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000012"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(1037), new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(1038) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000013"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(1041), new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(1041) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000014"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(1045), new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(1046) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000015"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(1049), new DateTime(2026, 8, 23, 10, 49, 33, 341, DateTimeKind.Utc).AddTicks(1050) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "Jobs");

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6516), new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6519) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6532), new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6533) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6535), new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6535) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6537), new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6537) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6540), new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6540) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000006"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6557), new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6558) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000007"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6559), new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6559) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000008"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6560), new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6560) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000009"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6562), new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6562) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000a"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6563), new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6564) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000b"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6565), new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6566) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000c"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6566), new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6567) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6568), new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6568) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6570), new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6570) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6571), new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6571) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000010"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6572), new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6572) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000011"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6573), new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6573) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000012"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6574), new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6574) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000013"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6575), new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6575) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000014"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6576), new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6576) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000015"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6578), new DateTime(2026, 8, 23, 9, 24, 43, 633, DateTimeKind.Utc).AddTicks(6578) });
        }
    }
}
