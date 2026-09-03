using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeraGo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MoveForYouSectorsToSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Data backfill: copy each talent's first 2 saved sector ids from
            // TalentProfiles.PreferredSectorIds (uuid[]) into their UserSettings
            // JSON blob under "forYou".sectorIds, then the column is dropped below.
            // Existing settings rows: merge the category in (unless already present).
            migrationBuilder.Sql("""
                UPDATE "UserSettings" s
                SET "Settings" = jsonb_set(
                        s."Settings"::jsonb,
                        '{forYou}',
                        jsonb_build_object('sectorIds',
                            (SELECT COALESCE(jsonb_agg(elem), '[]'::jsonb)
                             FROM unnest(t."PreferredSectorIds"[1:2]) AS e(elem))),
                        true)::text,
                    "UpdatedAt" = now()
                FROM "TalentProfiles" t
                WHERE t."UserId" = s."UserId"
                  AND cardinality(t."PreferredSectorIds") > 0
                  AND NOT (s."Settings"::jsonb ? 'forYou');
                """);

            // Talents without a settings row yet: create one carrying the sectors.
            migrationBuilder.Sql("""
                INSERT INTO "UserSettings" ("UserId", "Settings", "Version", "CreatedAt", "UpdatedAt")
                SELECT t."UserId",
                       jsonb_build_object('forYou',
                           jsonb_build_object('sectorIds',
                               (SELECT COALESCE(jsonb_agg(elem), '[]'::jsonb)
                                FROM unnest(t."PreferredSectorIds"[1:2]) AS e(elem))))::text,
                       1, now(), now()
                FROM "TalentProfiles" t
                WHERE cardinality(t."PreferredSectorIds") > 0
                  AND NOT EXISTS (SELECT 1 FROM "UserSettings" s WHERE s."UserId" = t."UserId");
                """);

            migrationBuilder.DropColumn(
                name: "PreferredSectorIds",
                table: "TalentProfiles");

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(2999), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3002) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3016), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3016) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3023), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3023) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3025), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3025) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3027), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3027) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000006"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3028), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3028) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000007"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3030), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3030) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000008"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3032), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3032) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000009"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3034), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3034) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000a"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3036), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3036) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000b"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3038), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3039) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000c"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3039), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3040) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3041), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3041) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3042), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3042) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-00000000000f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3043), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3043) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000010"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3044), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3044) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000011"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3046), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3046) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000012"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3048), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3048) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000013"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3049), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3049) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000014"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3050), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3050) });

            migrationBuilder.UpdateData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000015"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3051), new DateTime(2026, 9, 3, 8, 11, 19, 495, DateTimeKind.Utc).AddTicks(3052) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<Guid>>(
                name: "PreferredSectorIds",
                table: "TalentProfiles",
                type: "uuid[]",
                nullable: false,
                defaultValue: new Guid[0]);

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
        }
    }
}
