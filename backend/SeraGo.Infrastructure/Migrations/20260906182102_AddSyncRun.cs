using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SeraGo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncRun : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RanAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Inserted = table.Column<int>(type: "integer", nullable: false),
                    Updated = table.Column<int>(type: "integer", nullable: false),
                    Unchanged = table.Column<int>(type: "integer", nullable: false),
                    Uncategorized = table.Column<int>(type: "integer", nullable: false),
                    Deactivated = table.Column<int>(type: "integer", nullable: false),
                    UnknownSectors = table.Column<string>(type: "text", nullable: true),
                    TriggeredBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncRuns", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2572), new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2577) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2606), new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2607) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2612), new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2613) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2618), new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2619) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2622), new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2623) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000006"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2628), new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2629) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000007"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2633), new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2633) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000008"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2636), new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2637) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000009"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2641), new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2641) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000a"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2645), new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2645) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000b"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2649), new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2650) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000c"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2653), new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2654) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2657), new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2658) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2661), new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2662) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2665), new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2666) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000010"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2669), new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2670) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000011"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2673), new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2674) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000012"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2678), new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2678) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000013"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2682), new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2683) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000014"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2686), new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2686) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000015"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2690), new DateTime(2026, 9, 6, 18, 21, 0, 723, DateTimeKind.Utc).AddTicks(2691) });

            migrationBuilder.CreateIndex(
                name: "IX_SyncRuns_RanAt",
                table: "SyncRuns",
                column: "RanAt",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncRuns");

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(2999), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3002) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3016), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3016) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3023), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3023) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3025), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3025) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3027), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3027) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000006"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3028), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3028) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000007"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3030), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3030) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000008"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3032), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3032) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000009"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3034), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3034) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000a"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3036), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3036) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000b"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3038), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3039) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000c"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3039), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3040) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3041), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3041) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3042), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3042) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3043), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3043) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000010"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3044), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3044) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000011"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3046), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3046) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000012"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3048), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3048) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000013"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3049), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3049) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000014"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3050), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3050) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000015"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3051), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3052) });
        }
    }
}
