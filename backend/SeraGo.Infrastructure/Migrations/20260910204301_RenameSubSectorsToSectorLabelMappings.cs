using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeraGo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameSubSectorsToSectorLabelMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "SubSectors",
                newName: "SectorLabelMappings");

            migrationBuilder.RenameColumn(
                name: "SubSectorName",
                table: "SectorLabelMappings",
                newName: "SectorName");

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3047), new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3054) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3100), new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3101) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3110), new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3111) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3121), new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3122) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3130), new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3131) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000006"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3139), new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3140) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000007"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3149), new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3150) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000008"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3157), new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3158) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000009"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3168), new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3169) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000a"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3177), new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3178) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000b"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3186), new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3187) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000c"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3195), new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3196) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3205), new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3206) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3214), new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3215) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3223), new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3235) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000010"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3243), new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3245) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000011"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3253), new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3255) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000012"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3262), new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3263) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000013"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3272), new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3273) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000014"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3281), new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3282) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000015"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3289), new DateTime(2026, 9, 10, 20, 42, 58, 914, DateTimeKind.Utc).AddTicks(3290) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SectorName",
                table: "SectorLabelMappings",
                newName: "SubSectorName");

            migrationBuilder.RenameTable(
                name: "SectorLabelMappings",
                newName: "SubSectors");

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
    }
}
