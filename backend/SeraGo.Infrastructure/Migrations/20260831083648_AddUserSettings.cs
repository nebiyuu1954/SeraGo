using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeraGo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Settings = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserSettings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1092), new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1097) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1121), new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1121) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1124), new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1124) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1127), new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1127) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1129), new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1129) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000006"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1130), new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1131) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000007"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1136), new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1136) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000008"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1138), new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1138) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000009"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1141), new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1141) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000a"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1143), new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1144) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000b"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1147), new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1147) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000c"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1148), new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1148) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1150), new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1150) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1152), new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1153) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1155), new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1155) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000010"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1156), new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1157) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000011"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1158), new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1159) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000012"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1161), new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1161) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000013"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1162), new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1163) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000014"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1164), new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1164) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000015"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1166), new DateTime(2026, 8, 31, 8, 36, 46, 904, DateTimeKind.Utc).AddTicks(1167) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSettings");

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(652), new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(658) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(679), new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(679) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(683), new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(683) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(687), new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(687) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(690), new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(691) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000006"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(693), new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(693) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000007"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(695), new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(695) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000008"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(750), new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(750) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000009"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(754), new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(754) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000a"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(758), new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(758) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000b"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(761), new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(761) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000c"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(763), new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(764) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(766), new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(766) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(769), new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(770) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(774), new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(775) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000010"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(776), new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(777) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000011"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(779), new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(779) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000012"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(782), new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(782) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000013"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(784), new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(784) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000014"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(786), new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(787) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000015"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(788), new DateTime(2026, 8, 28, 12, 21, 42, 775, DateTimeKind.Utc).AddTicks(789) });
        }
    }
}
