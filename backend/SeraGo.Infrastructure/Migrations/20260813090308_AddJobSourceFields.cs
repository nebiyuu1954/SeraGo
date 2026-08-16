using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeraGo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobSourceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompanyLogoUrl",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExperienceLevel",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SectorName",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceName",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_SourceName",
                table: "Jobs",
                column: "SourceName");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_SourceName_ExternalId",
                table: "Jobs",
                columns: new[] { "SourceName", "ExternalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Jobs_SourceName",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_SourceName_ExternalId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "CompanyLogoUrl",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ExperienceLevel",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "SectorName",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "SourceName",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "Jobs");
        }
    }
}
