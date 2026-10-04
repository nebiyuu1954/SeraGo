using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SeraGo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNewSectorAliases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RefreshToken",
                table: "AufyRefreshTokens",
                newName: "Token");

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000000c"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "software engineering", new Guid("a0000000-0000-0000-0000-000000000001") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000000d"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "frontend", new Guid("a0000000-0000-0000-0000-000000000001") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000000e"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "backend", new Guid("a0000000-0000-0000-0000-000000000001") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000000f"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "cs", new Guid("a0000000-0000-0000-0000-000000000001") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000010"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "devops", new Guid("a0000000-0000-0000-0000-000000000001") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000011"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "web development", new Guid("a0000000-0000-0000-0000-000000000001") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000012"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "network administration", new Guid("a0000000-0000-0000-0000-000000000001") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000013"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "system administration", new Guid("a0000000-0000-0000-0000-000000000001") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000014"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "database administration", new Guid("a0000000-0000-0000-0000-000000000001") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000015"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "cybersecurity", new Guid("a0000000-0000-0000-0000-000000000001") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000016"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "machine learning", new Guid("a0000000-0000-0000-0000-000000000001") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000017"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "artificial intelligence", new Guid("a0000000-0000-0000-0000-000000000001") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000018"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "accounting & finance", new Guid("a0000000-0000-0000-0000-000000000002") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000019"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "accounting", new Guid("a0000000-0000-0000-0000-000000000002") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000001a"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "finance", new Guid("a0000000-0000-0000-0000-000000000002") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000001b"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "accounting and finance", new Guid("a0000000-0000-0000-0000-000000000002") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000001c"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "economics", new Guid("a0000000-0000-0000-0000-000000000002") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000001d"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "tax", new Guid("a0000000-0000-0000-0000-000000000002") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000001e"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "audit", new Guid("a0000000-0000-0000-0000-000000000002") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000001f"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "bookkeeping", new Guid("a0000000-0000-0000-0000-000000000002") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000020"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "accountant", new Guid("a0000000-0000-0000-0000-000000000002") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000021"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "financial analysis", new Guid("a0000000-0000-0000-0000-000000000002") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000022"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "banking and insurance", new Guid("a0000000-0000-0000-0000-000000000003") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000023"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "banking", new Guid("a0000000-0000-0000-0000-000000000003") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000024"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "insurance", new Guid("a0000000-0000-0000-0000-000000000003") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000025"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "insurance and investment", new Guid("a0000000-0000-0000-0000-000000000003") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000026"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "sales & promotion", new Guid("a0000000-0000-0000-0000-000000000004") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000027"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "sales", new Guid("a0000000-0000-0000-0000-000000000004") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000028"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "marketing", new Guid("a0000000-0000-0000-0000-000000000004") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000029"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "sales and marketing", new Guid("a0000000-0000-0000-0000-000000000004") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000002a"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "business sales and marketing", new Guid("a0000000-0000-0000-0000-000000000004") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000002b"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "marketing management", new Guid("a0000000-0000-0000-0000-000000000004") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000002c"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "digital marketing", new Guid("a0000000-0000-0000-0000-000000000004") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000002d"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "promotion", new Guid("a0000000-0000-0000-0000-000000000004") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000002e"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "healthcare", new Guid("a0000000-0000-0000-0000-000000000005") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000002f"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "health care", new Guid("a0000000-0000-0000-0000-000000000005") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000030"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "health care management", new Guid("a0000000-0000-0000-0000-000000000005") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000031"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "public health", new Guid("a0000000-0000-0000-0000-000000000005") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000032"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "nursing", new Guid("a0000000-0000-0000-0000-000000000005") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000033"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "pharmaceutical", new Guid("a0000000-0000-0000-0000-000000000005") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000034"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "pharmacy", new Guid("a0000000-0000-0000-0000-000000000005") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000035"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "medicine", new Guid("a0000000-0000-0000-0000-000000000005") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000036"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "medical", new Guid("a0000000-0000-0000-0000-000000000005") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000037"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "psychiatry, psychology & social work", new Guid("a0000000-0000-0000-0000-000000000005") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000038"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "teaching & education", new Guid("a0000000-0000-0000-0000-000000000006") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000039"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "education", new Guid("a0000000-0000-0000-0000-000000000006") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000003a"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "teaching", new Guid("a0000000-0000-0000-0000-000000000006") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000003b"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "tutoring, training & mentorship", new Guid("a0000000-0000-0000-0000-000000000006") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000003c"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "language and literature", new Guid("a0000000-0000-0000-0000-000000000006") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000003d"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "education management", new Guid("a0000000-0000-0000-0000-000000000006") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000003e"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "tutoring", new Guid("a0000000-0000-0000-0000-000000000006") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000003f"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "training", new Guid("a0000000-0000-0000-0000-000000000006") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000040"),
                column: "Alias",
                value: "engineering");

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000041"),
                column: "Alias",
                value: "construction & civil engineering");

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000042"),
                column: "Alias",
                value: "civil engineering");

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000043"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "mechanical & electrical engineering", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000044"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "chemical & biomedical engineering", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000045"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "environmental, mining & energy engineering", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000046"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "manufacturing engineering", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000047"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "architectural engineering", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000048"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "automotive engineering", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000049"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "sanitary engineering", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000004a"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "biomedical engineering", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000004b"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "electrical engineering", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000004c"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "architecture & urban planning", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000004d"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "construction skilled worker", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000004e"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "mechanical engineering", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000004f"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "planning", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000050"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "water and sanitation", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000051"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "automotive", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000052"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "aeronautics & aerospace", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000053"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "human resource & talent management", new Guid("a0000000-0000-0000-0000-000000000008") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000054"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "human resource and recruitment", new Guid("a0000000-0000-0000-0000-000000000008") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000055"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "human resource administration", new Guid("a0000000-0000-0000-0000-000000000008") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000056"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "human resources", new Guid("a0000000-0000-0000-0000-000000000008") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000057"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "hr", new Guid("a0000000-0000-0000-0000-000000000008") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000058"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "recruitment", new Guid("a0000000-0000-0000-0000-000000000008") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000059"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "human resource", new Guid("a0000000-0000-0000-0000-000000000008") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000005a"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "talent management", new Guid("a0000000-0000-0000-0000-000000000008") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000005b"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "business", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000005c"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "business administration", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000005d"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "business administration & operations", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000005e"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "business and administration", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000005f"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "secretarial, admin and clerical", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000060"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "secretarial & office management", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000061"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "management", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000062"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "development and project management", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000063"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "advisory & consultancy", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000064"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "brokerage & case closing", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000065"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "business management", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000066"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "administration", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000067"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "office management", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000068"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "secretarial", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000069"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "event management & organization", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000006a"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "research services", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000006b"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "manufacturing & production", new Guid("a0000000-0000-0000-0000-00000000000a") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000006c"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "manufacturing", new Guid("a0000000-0000-0000-0000-00000000000a") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000006d"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "manufacturing management", new Guid("a0000000-0000-0000-0000-00000000000a") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000006e"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "fmcg and manufacturing", new Guid("a0000000-0000-0000-0000-00000000000a") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000006f"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "woodwork & carpentry", new Guid("a0000000-0000-0000-0000-00000000000a") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000070"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "production", new Guid("a0000000-0000-0000-0000-00000000000a") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000071"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "transportation & delivery", new Guid("a0000000-0000-0000-0000-00000000000b") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000072"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "transportation & logistics", new Guid("a0000000-0000-0000-0000-00000000000b") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000073"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "transportation management", new Guid("a0000000-0000-0000-0000-00000000000b") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000074"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "transportation", new Guid("a0000000-0000-0000-0000-00000000000b") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000075"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "logistics & supply chain", new Guid("a0000000-0000-0000-0000-00000000000b") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000076"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "logistics, transport and supply chain", new Guid("a0000000-0000-0000-0000-00000000000b") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000077"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "logistics", new Guid("a0000000-0000-0000-0000-00000000000b") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000078"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "warehouse, supply chain and distribution", new Guid("a0000000-0000-0000-0000-00000000000b") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000079"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "purchasing & procurement", new Guid("a0000000-0000-0000-0000-00000000000c") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000007a"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "supply chain & purchasing management", new Guid("a0000000-0000-0000-0000-00000000000c") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000007b"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "procurement", new Guid("a0000000-0000-0000-0000-00000000000c") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000007c"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "supply chain", new Guid("a0000000-0000-0000-0000-00000000000c") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000007d"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "purchasing", new Guid("a0000000-0000-0000-0000-00000000000c") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000007e"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "media & entertainment", new Guid("a0000000-0000-0000-0000-00000000000d") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000007f"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "media and communication", new Guid("a0000000-0000-0000-0000-00000000000d") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000080"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "multimedia content production", new Guid("a0000000-0000-0000-0000-00000000000d") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000081"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "documentation & writing", new Guid("a0000000-0000-0000-0000-00000000000d") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000082"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "translation & transcription", new Guid("a0000000-0000-0000-0000-00000000000d") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000083"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "media", new Guid("a0000000-0000-0000-0000-00000000000d") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000084"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "journalism & communication", new Guid("a0000000-0000-0000-0000-00000000000d") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000085"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "journalism", new Guid("a0000000-0000-0000-0000-00000000000d") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000086"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "communication", new Guid("a0000000-0000-0000-0000-00000000000d") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000087"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "writing", new Guid("a0000000-0000-0000-0000-00000000000d") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000088"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "creative art & design", new Guid("a0000000-0000-0000-0000-00000000000e") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000089"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "fashion / clothing & textile design", new Guid("a0000000-0000-0000-0000-00000000000e") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000008a"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "design", new Guid("a0000000-0000-0000-0000-00000000000e") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000008b"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "graphic design", new Guid("a0000000-0000-0000-0000-00000000000e") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000008c"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "creative", new Guid("a0000000-0000-0000-0000-00000000000e") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000008d"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "fashion design", new Guid("a0000000-0000-0000-0000-00000000000e") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000008e"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "creative arts", new Guid("a0000000-0000-0000-0000-00000000000e") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000008f"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "ui/ux design", new Guid("a0000000-0000-0000-0000-00000000000e") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000090"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "fashion", new Guid("a0000000-0000-0000-0000-00000000000e") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000091"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "customer service & care", new Guid("a0000000-0000-0000-0000-00000000000f") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000092"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "retail & office support", new Guid("a0000000-0000-0000-0000-00000000000f") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000093"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "customer service", new Guid("a0000000-0000-0000-0000-00000000000f") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000094"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "support", new Guid("a0000000-0000-0000-0000-00000000000f") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000095"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "call center", new Guid("a0000000-0000-0000-0000-00000000000f") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000096"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "reception", new Guid("a0000000-0000-0000-0000-00000000000f") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000097"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "hospitality & tourism", new Guid("a0000000-0000-0000-0000-000000000010") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000098"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "food & drink preparation / service", new Guid("a0000000-0000-0000-0000-000000000010") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000099"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "hospitality", new Guid("a0000000-0000-0000-0000-000000000010") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000009a"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "tourism", new Guid("a0000000-0000-0000-0000-000000000010") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000009b"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "chef", new Guid("a0000000-0000-0000-0000-000000000010") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000009c"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "catering", new Guid("a0000000-0000-0000-0000-000000000010") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000009d"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "hotel", new Guid("a0000000-0000-0000-0000-000000000010") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000009e"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "agriculture", new Guid("a0000000-0000-0000-0000-000000000011") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000009f"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "agricultural science", new Guid("a0000000-0000-0000-0000-000000000011") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000a0"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "natural science", new Guid("a0000000-0000-0000-0000-000000000011") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000a1"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "natural sciences", new Guid("a0000000-0000-0000-0000-000000000011") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000a2"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "chemistry", new Guid("a0000000-0000-0000-0000-000000000011") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000a3"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "physics", new Guid("a0000000-0000-0000-0000-000000000011") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000a4"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "microbiology", new Guid("a0000000-0000-0000-0000-000000000011") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000a5"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "mathematics", new Guid("a0000000-0000-0000-0000-000000000011") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000a6"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "nutrition", new Guid("a0000000-0000-0000-0000-000000000011") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000a7"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "biology", new Guid("a0000000-0000-0000-0000-000000000011") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000a8"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "veterinary", new Guid("a0000000-0000-0000-0000-000000000011") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000a9"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "gardening & landscaping", new Guid("a0000000-0000-0000-0000-000000000011") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000aa"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "horticulture", new Guid("a0000000-0000-0000-0000-000000000011") });

            migrationBuilder.InsertData(
                table: "SectorAliases",
                columns: new[] { "Id", "Alias", "SectorId" },
                values: new object[,]
                {
                    { new Guid("b0000000-0000-0000-0000-0000000000ab"), "livestock & animal husbandry", new Guid("a0000000-0000-0000-0000-000000000011") },
                    { new Guid("b0000000-0000-0000-0000-0000000000ac"), "law & legal advocacy", new Guid("a0000000-0000-0000-0000-000000000012") },
                    { new Guid("b0000000-0000-0000-0000-0000000000ad"), "legal services", new Guid("a0000000-0000-0000-0000-000000000012") },
                    { new Guid("b0000000-0000-0000-0000-0000000000ae"), "legal", new Guid("a0000000-0000-0000-0000-000000000012") },
                    { new Guid("b0000000-0000-0000-0000-0000000000af"), "law", new Guid("a0000000-0000-0000-0000-000000000012") },
                    { new Guid("b0000000-0000-0000-0000-0000000000b0"), "advocacy", new Guid("a0000000-0000-0000-0000-000000000012") },
                    { new Guid("b0000000-0000-0000-0000-0000000000b1"), "social sciences and community service", new Guid("a0000000-0000-0000-0000-000000000013") },
                    { new Guid("b0000000-0000-0000-0000-0000000000b2"), "social science", new Guid("a0000000-0000-0000-0000-000000000013") },
                    { new Guid("b0000000-0000-0000-0000-0000000000b3"), "social work", new Guid("a0000000-0000-0000-0000-000000000013") },
                    { new Guid("b0000000-0000-0000-0000-0000000000b4"), "community service", new Guid("a0000000-0000-0000-0000-000000000013") },
                    { new Guid("b0000000-0000-0000-0000-0000000000b5"), "history", new Guid("a0000000-0000-0000-0000-000000000013") },
                    { new Guid("b0000000-0000-0000-0000-0000000000b6"), "sociology", new Guid("a0000000-0000-0000-0000-000000000013") },
                    { new Guid("b0000000-0000-0000-0000-0000000000b7"), "psychology", new Guid("a0000000-0000-0000-0000-000000000013") },
                    { new Guid("b0000000-0000-0000-0000-0000000000b8"), "security & safety", new Guid("a0000000-0000-0000-0000-000000000014") },
                    { new Guid("b0000000-0000-0000-0000-0000000000b9"), "security", new Guid("a0000000-0000-0000-0000-000000000014") },
                    { new Guid("b0000000-0000-0000-0000-0000000000ba"), "protection", new Guid("a0000000-0000-0000-0000-000000000014") },
                    { new Guid("b0000000-0000-0000-0000-0000000000bb"), "guard", new Guid("a0000000-0000-0000-0000-000000000014") },
                    { new Guid("b0000000-0000-0000-0000-0000000000bc"), "safety", new Guid("a0000000-0000-0000-0000-000000000014") },
                    { new Guid("b0000000-0000-0000-0000-0000000000bd"), "low and medium skilled worker", new Guid("a0000000-0000-0000-0000-000000000015") },
                    { new Guid("b0000000-0000-0000-0000-0000000000be"), "service industry skilled worker", new Guid("a0000000-0000-0000-0000-000000000015") },
                    { new Guid("b0000000-0000-0000-0000-0000000000bf"), "janitorial & office services", new Guid("a0000000-0000-0000-0000-000000000015") },
                    { new Guid("b0000000-0000-0000-0000-0000000000c0"), "general labor", new Guid("a0000000-0000-0000-0000-000000000015") },
                    { new Guid("b0000000-0000-0000-0000-0000000000c1"), "cleaner", new Guid("a0000000-0000-0000-0000-000000000015") },
                    { new Guid("b0000000-0000-0000-0000-0000000000c2"), "maintenance", new Guid("a0000000-0000-0000-0000-000000000015") },
                    { new Guid("b0000000-0000-0000-0000-0000000000c3"), "installation & maintenance", new Guid("a0000000-0000-0000-0000-000000000015") },
                    { new Guid("b0000000-0000-0000-0000-0000000000c4"), "labor & masonry", new Guid("a0000000-0000-0000-0000-000000000015") }
                });

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

            migrationBuilder.InsertData(
                table: "Sectors",
                columns: new[] { "Id", "CreatedAt", "IsActive", "Name", "Slug", "UpdatedAt" },
                values: new object[] { new Guid("a0000000-0000-0000-0000-000000000016"), new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5917), true, "Beauty & Grooming", "beauty-grooming", new DateTime(2026, 10, 4, 17, 32, 21, 797, DateTimeKind.Utc).AddTicks(5917) });

            migrationBuilder.InsertData(
                table: "SectorAliases",
                columns: new[] { "Id", "Alias", "SectorId" },
                values: new object[,]
                {
                    { new Guid("b0000000-0000-0000-0000-0000000000c5"), "beauty & grooming", new Guid("a0000000-0000-0000-0000-000000000016") },
                    { new Guid("b0000000-0000-0000-0000-0000000000c6"), "cosmetics", new Guid("a0000000-0000-0000-0000-000000000016") },
                    { new Guid("b0000000-0000-0000-0000-0000000000c7"), "salon", new Guid("a0000000-0000-0000-0000-000000000016") },
                    { new Guid("b0000000-0000-0000-0000-0000000000c8"), "hairdresser", new Guid("a0000000-0000-0000-0000-000000000016") },
                    { new Guid("b0000000-0000-0000-0000-0000000000c9"), "barber", new Guid("a0000000-0000-0000-0000-000000000016") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000ab"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000ac"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000ad"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000ae"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000af"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000b0"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000b1"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000b2"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000b3"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000b4"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000b5"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000b6"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000b7"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000b8"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000b9"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000ba"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000bb"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000bc"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000bd"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000be"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000bf"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000c0"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000c1"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000c2"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000c3"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000c4"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000c5"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000c6"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000c7"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000c8"));

            migrationBuilder.DeleteData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000c9"));

            migrationBuilder.DeleteData(
                table: "Sectors",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000016"));

            migrationBuilder.RenameColumn(
                name: "Token",
                table: "AufyRefreshTokens",
                newName: "RefreshToken");

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000000c"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "accounting & finance", new Guid("a0000000-0000-0000-0000-000000000002") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000000d"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "accounting", new Guid("a0000000-0000-0000-0000-000000000002") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000000e"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "finance", new Guid("a0000000-0000-0000-0000-000000000002") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000000f"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "accounting and finance", new Guid("a0000000-0000-0000-0000-000000000002") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000010"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "economics", new Guid("a0000000-0000-0000-0000-000000000002") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000011"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "tax", new Guid("a0000000-0000-0000-0000-000000000002") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000012"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "audit", new Guid("a0000000-0000-0000-0000-000000000002") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000013"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "bookkeeping", new Guid("a0000000-0000-0000-0000-000000000002") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000014"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "banking and insurance", new Guid("a0000000-0000-0000-0000-000000000003") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000015"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "banking", new Guid("a0000000-0000-0000-0000-000000000003") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000016"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "insurance", new Guid("a0000000-0000-0000-0000-000000000003") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000017"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "insurance and investment", new Guid("a0000000-0000-0000-0000-000000000003") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000018"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "sales & promotion", new Guid("a0000000-0000-0000-0000-000000000004") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000019"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "sales", new Guid("a0000000-0000-0000-0000-000000000004") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000001a"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "marketing", new Guid("a0000000-0000-0000-0000-000000000004") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000001b"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "sales and marketing", new Guid("a0000000-0000-0000-0000-000000000004") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000001c"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "business sales and marketing", new Guid("a0000000-0000-0000-0000-000000000004") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000001d"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "marketing management", new Guid("a0000000-0000-0000-0000-000000000004") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000001e"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "digital marketing", new Guid("a0000000-0000-0000-0000-000000000004") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000001f"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "promotion", new Guid("a0000000-0000-0000-0000-000000000004") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000020"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "healthcare", new Guid("a0000000-0000-0000-0000-000000000005") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000021"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "health care", new Guid("a0000000-0000-0000-0000-000000000005") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000022"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "health care management", new Guid("a0000000-0000-0000-0000-000000000005") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000023"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "public health", new Guid("a0000000-0000-0000-0000-000000000005") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000024"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "nursing", new Guid("a0000000-0000-0000-0000-000000000005") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000025"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "pharmaceutical", new Guid("a0000000-0000-0000-0000-000000000005") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000026"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "pharmacy", new Guid("a0000000-0000-0000-0000-000000000005") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000027"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "medicine", new Guid("a0000000-0000-0000-0000-000000000005") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000028"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "medical", new Guid("a0000000-0000-0000-0000-000000000005") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000029"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "teaching & education", new Guid("a0000000-0000-0000-0000-000000000006") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000002a"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "education", new Guid("a0000000-0000-0000-0000-000000000006") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000002b"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "teaching", new Guid("a0000000-0000-0000-0000-000000000006") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000002c"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "tutoring, training & mentorship", new Guid("a0000000-0000-0000-0000-000000000006") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000002d"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "language and literature", new Guid("a0000000-0000-0000-0000-000000000006") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000002e"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "education management", new Guid("a0000000-0000-0000-0000-000000000006") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000002f"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "tutoring", new Guid("a0000000-0000-0000-0000-000000000006") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000030"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "training", new Guid("a0000000-0000-0000-0000-000000000006") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000031"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "engineering", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000032"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "construction & civil engineering", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000033"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "civil engineering", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000034"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "mechanical & electrical engineering", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000035"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "chemical & biomedical engineering", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000036"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "environmental, mining & energy engineering", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000037"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "manufacturing engineering", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000038"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "architectural engineering", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000039"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "automotive engineering", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000003a"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "sanitary engineering", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000003b"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "biomedical engineering", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000003c"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "electrical engineering", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000003d"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "architecture & urban planning", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000003e"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "construction skilled worker", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000003f"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "mechanical engineering", new Guid("a0000000-0000-0000-0000-000000000007") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000040"),
                column: "Alias",
                value: "planning");

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000041"),
                column: "Alias",
                value: "water and sanitation");

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000042"),
                column: "Alias",
                value: "automotive");

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000043"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "human resource & talent management", new Guid("a0000000-0000-0000-0000-000000000008") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000044"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "human resource and recruitment", new Guid("a0000000-0000-0000-0000-000000000008") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000045"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "human resource administration", new Guid("a0000000-0000-0000-0000-000000000008") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000046"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "human resources", new Guid("a0000000-0000-0000-0000-000000000008") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000047"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "hr", new Guid("a0000000-0000-0000-0000-000000000008") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000048"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "recruitment", new Guid("a0000000-0000-0000-0000-000000000008") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000049"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "human resource", new Guid("a0000000-0000-0000-0000-000000000008") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000004a"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "talent management", new Guid("a0000000-0000-0000-0000-000000000008") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000004b"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "business", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000004c"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "business administration", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000004d"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "business administration & operations", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000004e"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "business and administration", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000004f"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "secretarial, admin and clerical", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000050"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "secretarial & office management", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000051"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "management", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000052"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "development and project management", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000053"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "advisory & consultancy", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000054"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "brokerage & case closing", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000055"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "business management", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000056"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "administration", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000057"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "office management", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000058"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "secretarial", new Guid("a0000000-0000-0000-0000-000000000009") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000059"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "manufacturing & production", new Guid("a0000000-0000-0000-0000-00000000000a") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000005a"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "manufacturing", new Guid("a0000000-0000-0000-0000-00000000000a") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000005b"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "manufacturing management", new Guid("a0000000-0000-0000-0000-00000000000a") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000005c"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "fmcg and manufacturing", new Guid("a0000000-0000-0000-0000-00000000000a") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000005d"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "woodwork & carpentry", new Guid("a0000000-0000-0000-0000-00000000000a") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000005e"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "production", new Guid("a0000000-0000-0000-0000-00000000000a") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000005f"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "transportation & delivery", new Guid("a0000000-0000-0000-0000-00000000000b") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000060"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "transportation & logistics", new Guid("a0000000-0000-0000-0000-00000000000b") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000061"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "transportation management", new Guid("a0000000-0000-0000-0000-00000000000b") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000062"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "transportation", new Guid("a0000000-0000-0000-0000-00000000000b") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000063"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "logistics & supply chain", new Guid("a0000000-0000-0000-0000-00000000000b") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000064"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "logistics, transport and supply chain", new Guid("a0000000-0000-0000-0000-00000000000b") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000065"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "logistics", new Guid("a0000000-0000-0000-0000-00000000000b") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000066"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "warehouse, supply chain and distribution", new Guid("a0000000-0000-0000-0000-00000000000b") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000067"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "purchasing & procurement", new Guid("a0000000-0000-0000-0000-00000000000c") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000068"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "supply chain & purchasing management", new Guid("a0000000-0000-0000-0000-00000000000c") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000069"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "procurement", new Guid("a0000000-0000-0000-0000-00000000000c") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000006a"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "supply chain", new Guid("a0000000-0000-0000-0000-00000000000c") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000006b"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "purchasing", new Guid("a0000000-0000-0000-0000-00000000000c") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000006c"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "media & entertainment", new Guid("a0000000-0000-0000-0000-00000000000d") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000006d"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "media and communication", new Guid("a0000000-0000-0000-0000-00000000000d") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000006e"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "multimedia content production", new Guid("a0000000-0000-0000-0000-00000000000d") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000006f"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "documentation & writing", new Guid("a0000000-0000-0000-0000-00000000000d") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000070"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "translation & transcription", new Guid("a0000000-0000-0000-0000-00000000000d") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000071"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "media", new Guid("a0000000-0000-0000-0000-00000000000d") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000072"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "journalism", new Guid("a0000000-0000-0000-0000-00000000000d") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000073"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "communication", new Guid("a0000000-0000-0000-0000-00000000000d") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000074"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "writing", new Guid("a0000000-0000-0000-0000-00000000000d") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000075"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "creative art & design", new Guid("a0000000-0000-0000-0000-00000000000e") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000076"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "fashion / clothing & textile design", new Guid("a0000000-0000-0000-0000-00000000000e") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000077"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "design", new Guid("a0000000-0000-0000-0000-00000000000e") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000078"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "graphic design", new Guid("a0000000-0000-0000-0000-00000000000e") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000079"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "creative", new Guid("a0000000-0000-0000-0000-00000000000e") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000007a"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "fashion design", new Guid("a0000000-0000-0000-0000-00000000000e") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000007b"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "creative arts", new Guid("a0000000-0000-0000-0000-00000000000e") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000007c"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "ui/ux design", new Guid("a0000000-0000-0000-0000-00000000000e") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000007d"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "fashion", new Guid("a0000000-0000-0000-0000-00000000000e") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000007e"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "customer service & care", new Guid("a0000000-0000-0000-0000-00000000000f") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000007f"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "retail & office support", new Guid("a0000000-0000-0000-0000-00000000000f") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000080"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "customer service", new Guid("a0000000-0000-0000-0000-00000000000f") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000081"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "support", new Guid("a0000000-0000-0000-0000-00000000000f") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000082"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "call center", new Guid("a0000000-0000-0000-0000-00000000000f") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000083"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "reception", new Guid("a0000000-0000-0000-0000-00000000000f") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000084"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "hospitality & tourism", new Guid("a0000000-0000-0000-0000-000000000010") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000085"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "food & drink preparation / service", new Guid("a0000000-0000-0000-0000-000000000010") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000086"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "hospitality", new Guid("a0000000-0000-0000-0000-000000000010") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000087"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "tourism", new Guid("a0000000-0000-0000-0000-000000000010") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000088"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "chef", new Guid("a0000000-0000-0000-0000-000000000010") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000089"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "catering", new Guid("a0000000-0000-0000-0000-000000000010") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000008a"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "hotel", new Guid("a0000000-0000-0000-0000-000000000010") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000008b"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "agriculture", new Guid("a0000000-0000-0000-0000-000000000011") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000008c"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "agricultural science", new Guid("a0000000-0000-0000-0000-000000000011") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000008d"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "natural science", new Guid("a0000000-0000-0000-0000-000000000011") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000008e"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "natural sciences", new Guid("a0000000-0000-0000-0000-000000000011") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000008f"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "chemistry", new Guid("a0000000-0000-0000-0000-000000000011") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000090"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "physics", new Guid("a0000000-0000-0000-0000-000000000011") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000091"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "microbiology", new Guid("a0000000-0000-0000-0000-000000000011") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000092"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "mathematics", new Guid("a0000000-0000-0000-0000-000000000011") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000093"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "nutrition", new Guid("a0000000-0000-0000-0000-000000000011") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000094"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "biology", new Guid("a0000000-0000-0000-0000-000000000011") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000095"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "law & legal advocacy", new Guid("a0000000-0000-0000-0000-000000000012") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000096"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "legal services", new Guid("a0000000-0000-0000-0000-000000000012") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000097"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "legal", new Guid("a0000000-0000-0000-0000-000000000012") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000098"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "law", new Guid("a0000000-0000-0000-0000-000000000012") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000099"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "advocacy", new Guid("a0000000-0000-0000-0000-000000000012") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000009a"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "social sciences and community service", new Guid("a0000000-0000-0000-0000-000000000013") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000009b"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "social science", new Guid("a0000000-0000-0000-0000-000000000013") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000009c"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "social work", new Guid("a0000000-0000-0000-0000-000000000013") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000009d"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "community service", new Guid("a0000000-0000-0000-0000-000000000013") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000009e"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "history", new Guid("a0000000-0000-0000-0000-000000000013") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-00000000009f"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "sociology", new Guid("a0000000-0000-0000-0000-000000000013") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000a0"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "psychology", new Guid("a0000000-0000-0000-0000-000000000013") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000a1"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "security", new Guid("a0000000-0000-0000-0000-000000000014") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000a2"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "protection", new Guid("a0000000-0000-0000-0000-000000000014") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000a3"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "guard", new Guid("a0000000-0000-0000-0000-000000000014") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000a4"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "safety", new Guid("a0000000-0000-0000-0000-000000000014") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000a5"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "low and medium skilled worker", new Guid("a0000000-0000-0000-0000-000000000015") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000a6"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "service industry skilled worker", new Guid("a0000000-0000-0000-0000-000000000015") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000a7"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "janitorial & office services", new Guid("a0000000-0000-0000-0000-000000000015") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000a8"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "general labor", new Guid("a0000000-0000-0000-0000-000000000015") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000a9"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "cleaner", new Guid("a0000000-0000-0000-0000-000000000015") });

            migrationBuilder.UpdateData(
                table: "SectorAliases",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-0000000000aa"),
                columns: new[] { "Alias", "SectorId" },
                values: new object[] { "maintenance", new Guid("a0000000-0000-0000-0000-000000000015") });

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
    }
}
