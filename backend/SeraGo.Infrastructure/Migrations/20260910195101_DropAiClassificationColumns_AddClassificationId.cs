using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeraGo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropAiClassificationColumns_AddClassificationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClassificationId",
                table: "Jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "AiClassifiedAt",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "AiClassification",
                table: "Jobs");

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3567), new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3570) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3588), new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3588) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3590), new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3590) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3593), new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3594) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3595), new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3596) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000006"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3597), new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3598) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000007"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3599), new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3600) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000008"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3602), new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3602) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000009"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3605), new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3605) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000a"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3607), new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3607) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000b"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3610), new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3610) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000c"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3612), new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3612) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3617), new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3617) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3620), new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3620) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3622), new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3622) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000010"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3624), new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3624) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000011"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3626), new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3626) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000012"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3628), new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3628) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000013"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3631), new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3631) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000014"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3633), new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3634) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000015"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3635), new DateTime(2026, 9, 10, 19, 51, 0, 567, DateTimeKind.Utc).AddTicks(3636) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClassificationId",
                table: "Jobs");

            migrationBuilder.AddColumn<string>(
                name: "AiClassification",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AiClassifiedAt",
                table: "Jobs",
                type: "timestamp with time zone",
                nullable: true);

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

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000014"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3383), new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3384) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000015"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3388), new DateTime(2026, 9, 8, 7, 11, 45, 396, DateTimeKind.Utc).AddTicks(3388) });
        }
    }
}
