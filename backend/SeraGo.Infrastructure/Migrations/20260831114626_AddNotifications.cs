using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeraGo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Data = table.Column<string>(type: "text", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EmailSent = table.Column<bool>(type: "boolean", nullable: false),
                    EmailSentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EmailBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    TelegramSent = table.Column<bool>(type: "boolean", nullable: false),
                    TelegramSentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_AspNetUsers_UserId",
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
                values: new object[] { new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3135), new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3140) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3171), new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3171) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3178), new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3179) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3183), new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3183) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3187), new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3188) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000006"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3191), new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3191) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000007"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3303), new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3303) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000008"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3307), new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3308) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000009"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3313), new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3313) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000a"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3316), new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3317) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000b"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3320), new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3321) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000c"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3324), new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3324) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3330), new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3330) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3334), new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3334) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3337), new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3344) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000010"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3347), new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3348) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000011"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3351), new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3352) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000012"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3355), new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3356) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000013"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3359), new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3359) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000014"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3362), new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3363) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000015"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3366), new DateTime(2026, 8, 31, 11, 46, 26, 122, DateTimeKind.Utc).AddTicks(3367) });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_created_at",
                table: "Notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_email_batch",
                table: "Notifications",
                columns: new[] { "EmailBatchId", "UserId" },
                filter: "\"EmailBatchId\" IS NOT NULL AND \"EmailSent\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_user_unread",
                table: "Notifications",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true },
                filter: "\"IsRead\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");

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
    }
}
