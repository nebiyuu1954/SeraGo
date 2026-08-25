using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeraGo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobViewIpAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobViews_JobId_UserId",
                table: "JobViews");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "JobViews",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "JobViews",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7377), new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7381) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7434), new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7435) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7438), new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7439) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7441), new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7441) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7442), new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7442) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000006"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7444), new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7444) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000007"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7445), new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7445) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000008"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7448), new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7448) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000009"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7450), new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7450) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000a"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7452), new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7452) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000b"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7454), new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7455) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000c"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7456), new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7457) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7459), new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7459) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7461), new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7461) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7463), new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7463) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000010"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7464), new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7464) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000011"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7466), new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7467) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000012"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7468), new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7469) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000013"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7472), new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7472) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000014"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7474), new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7474) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000015"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7475), new DateTime(2026, 8, 23, 11, 8, 14, 468, DateTimeKind.Utc).AddTicks(7475) });

            migrationBuilder.CreateIndex(
                name: "IX_JobViews_JobId_IpAddress",
                table: "JobViews",
                columns: new[] { "JobId", "IpAddress" });

            migrationBuilder.CreateIndex(
                name: "IX_JobViews_JobId_UserId",
                table: "JobViews",
                columns: new[] { "JobId", "UserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobViews_JobId_IpAddress",
                table: "JobViews");

            migrationBuilder.DropIndex(
                name: "IX_JobViews_JobId_UserId",
                table: "JobViews");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "JobViews");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "JobViews",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6159), new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6163) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6184), new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6184) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6187), new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6188) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6189), new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6190) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6191), new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6192) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000006"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6193), new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6193) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000007"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6195), new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6195) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000008"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6197), new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6197) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000009"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6200), new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6200) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000a"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6202), new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6202) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000b"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6205), new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6205) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000c"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6207), new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6207) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6209), new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6210) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6212), new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6212) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6213), new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6213) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000010"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6215), new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6215) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000011"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6216), new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6216) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000012"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6219), new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6219) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000013"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6221), new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6221) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000014"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6223), new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6223) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000015"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6224), new DateTime(2026, 8, 23, 11, 4, 36, 233, DateTimeKind.Utc).AddTicks(6225) });

            migrationBuilder.CreateIndex(
                name: "IX_JobViews_JobId_UserId",
                table: "JobViews",
                columns: new[] { "JobId", "UserId" },
                unique: true);
        }
    }
}
