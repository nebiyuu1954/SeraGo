using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeraGo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecruiterPrivacyAndCompanyFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CompanyLogoUrl",
                table: "RecruiterProfiles",
                newName: "TwitterUrl");

            migrationBuilder.AddColumn<int>(
                name: "CompanyType",
                table: "RecruiterProfiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyVisibility",
                table: "RecruiterProfiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "RecruiterProfiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "FoundedYear",
                table: "RecruiterProfiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Headquarters",
                table: "RecruiterProfiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsCompanyPrivate",
                table: "RecruiterProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LinkedInUrl",
                table: "RecruiterProfiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "RecruiterProfiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3035), new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3042) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3076), new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3077) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3082), new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3082) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3086), new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3087) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3090), new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3091) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000006"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3094), new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3094) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000007"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3098), new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3098) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000008"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3101), new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3101) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000009"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3104), new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3105) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000a"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3108), new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3108) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000b"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3111), new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3111) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000c"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3115), new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3115) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3118), new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3119) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3122), new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3122) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3125), new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3137) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000010"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3141), new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3141) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000011"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3145), new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3145) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000012"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3148), new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3148) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000013"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3152), new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3152) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000014"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3155), new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3156) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000015"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3158), new DateTime(2026, 8, 26, 9, 26, 1, 431, DateTimeKind.Utc).AddTicks(3158) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyType",
                table: "RecruiterProfiles");

            migrationBuilder.DropColumn(
                name: "CompanyVisibility",
                table: "RecruiterProfiles");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "RecruiterProfiles");

            migrationBuilder.DropColumn(
                name: "FoundedYear",
                table: "RecruiterProfiles");

            migrationBuilder.DropColumn(
                name: "Headquarters",
                table: "RecruiterProfiles");

            migrationBuilder.DropColumn(
                name: "IsCompanyPrivate",
                table: "RecruiterProfiles");

            migrationBuilder.DropColumn(
                name: "LinkedInUrl",
                table: "RecruiterProfiles");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "RecruiterProfiles");

            migrationBuilder.RenameColumn(
                name: "TwitterUrl",
                table: "RecruiterProfiles",
                newName: "CompanyLogoUrl");

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9648), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9652) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9666), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9666) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9669), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9670) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9676), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9676) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9678), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9678) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000006"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9679), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9680) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000007"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9682), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9682) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000008"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9685), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9685) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000009"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9687), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9688) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000a"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9690), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9690) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000b"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9692), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9692) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000c"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9694), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9694) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9696), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9696) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9697), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9697) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9698), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9699) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000010"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9700), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9700) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000011"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9702), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9702) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000012"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9704), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9705) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000013"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9706), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9706) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000014"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9708), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9708) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000015"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9709), new DateTime(2026, 8, 23, 16, 10, 58, 333, DateTimeKind.Utc).AddTicks(9710) });
        }
    }
}
