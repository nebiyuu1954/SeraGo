using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeraGo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobTelegramChannelFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "TelegramChannelMessageId",
                table: "Jobs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TelegramChannelPostedAt",
                table: "Jobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4732), new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4739) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4793), new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4794) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4804), new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4805) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4816), new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4817) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4824), new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4825) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000006"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4835), new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4836) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000007"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4843), new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4844) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000008"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4851), new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4852) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000009"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4860), new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4861) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000a"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4867), new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4868) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000b"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4875), new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4876) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000c"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4883), new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4884) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4892), new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4893) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4901), new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4902) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4913), new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4914) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000010"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4922), new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4924) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000011"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4931), new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4932) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000012"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4940), new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4941) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000013"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4949), new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4950) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000014"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4957), new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4958) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000015"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4964), new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4965) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000016"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4971), new DateTime(2026, 10, 19, 7, 7, 49, 554, DateTimeKind.Utc).AddTicks(4972) });

            migrationBuilder.CreateIndex(
                name: "IX_jobs_telegram_pending",
                table: "Jobs",
                columns: new[] { "Status", "IsActive", "TelegramChannelPostedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_jobs_telegram_pending",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "TelegramChannelMessageId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "TelegramChannelPostedAt",
                table: "Jobs");

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5803), new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5807) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5834), new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5834) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5838), new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5839) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5840), new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5840) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5842), new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5843) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000006"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5844), new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5844) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000007"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5846), new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5846) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000008"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5848), new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5848) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000009"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5850), new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5851) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000a"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5852), new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5853) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000b"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5855), new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5856) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000c"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5857), new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5857) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5890), new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5890) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5893), new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5893) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5896), new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5897) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000010"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5903), new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5903) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000011"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5905), new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5906) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000012"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5908), new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5908) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000013"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5911), new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5911) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000014"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5913), new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5913) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000015"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5915), new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5915) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000016"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5917), new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5917) });
        }
    }
}
