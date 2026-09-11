using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeraGo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubSectorTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubSectors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubSectorName = table.Column<string>(type: "text", nullable: false),
                    Alias = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubSectors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubSectors_Sectors_SectorId",
                        column: x => x.SectorId,
                        principalTable: "Sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3262), new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3265) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3284), new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3285) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3288), new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3289) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3291), new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3292) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3293), new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3294) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000006"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3295), new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3296) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000007"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3298), new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3298) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000008"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3300), new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3300) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000009"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3303), new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3303) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000a"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3305), new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3305) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000b"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3307), new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3308) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000c"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3309), new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3310) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3311), new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3311) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3316), new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3316) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3364), new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3370) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000010"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3373), new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3373) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000011"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3375), new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3376) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000012"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3377), new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3378) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000013"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3381), new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3381) });

            migrationBuilder.CreateIndex(
                name: "IX_SubSectors_SectorId",
                table: "SubSectors",
                column: "SectorId");

            migrationBuilder.CreateIndex(
                name: "IX_SubSectors_SectorId_Alias",
                table: "SubSectors",
                columns: new[] { "SectorId", "Alias" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubSectors");

            migrationBuilder.DropColumn(
                name: "AiClassification",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "AiClassifiedAt",
                table: "Jobs");
        }
    }
}
