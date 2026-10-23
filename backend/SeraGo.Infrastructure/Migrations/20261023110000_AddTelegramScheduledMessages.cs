using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeraGo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramScheduledMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TelegramScheduledMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Target = table.Column<int>(type: "integer", nullable: false),
                    MessageType = table.Column<int>(type: "integer", nullable: false),
                    DaysOfWeek = table.Column<string>(type: "text", nullable: false),
                    SpecificDate = table.Column<string>(type: "text", nullable: true),
                    ScheduledTime = table.Column<string>(type: "text", nullable: false),
                    CustomTemplate = table.Column<string>(type: "text", nullable: true),
                    BannerUrl = table.Column<string>(type: "text", nullable: true),
                    ButtonText = table.Column<string>(type: "text", nullable: true),
                    ButtonUrl = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastRunAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastRunStatus = table.Column<string>(type: "text", nullable: true),
                    LastRunRecipientCount = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramScheduledMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramScheduledMessages_IsActive_ScheduledTime",
                table: "TelegramScheduledMessages",
                columns: new[] { "IsActive", "ScheduledTime" });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramScheduledMessages_MessageType",
                table: "TelegramScheduledMessages",
                column: "MessageType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelegramScheduledMessages");
        }
    }
}
