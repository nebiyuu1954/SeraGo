using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeraGo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileSharedToJobApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ProfileShared",
                table: "JobApplications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileShared",
                table: "JobApplications");

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
    }
}
