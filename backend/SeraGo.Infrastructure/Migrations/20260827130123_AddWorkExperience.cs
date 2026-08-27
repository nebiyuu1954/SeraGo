using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeraGo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkExperience : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WorkExperience",
                table: "TalentProfiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9440), new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9444) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9460), new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9461) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9463), new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9463) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9466), new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9466) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9469), new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9469) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000006"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9471), new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9471) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000007"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9473), new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9473) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000008"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9474), new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9475) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000009"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9477), new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9477) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000a"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9480), new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9480) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000b"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9557), new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9557) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000c"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9559), new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9560) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9561), new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9561) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9563), new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9563) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9568), new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9568) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000010"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9570), new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9570) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000011"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9572), new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9572) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000012"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9574), new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9574) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000013"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9575), new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9576) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000014"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9577), new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9577) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000015"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9578), new DateTime(2026, 8, 27, 13, 1, 22, 428, DateTimeKind.Utc).AddTicks(9579) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkExperience",
                table: "TalentProfiles");

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
    }
}
