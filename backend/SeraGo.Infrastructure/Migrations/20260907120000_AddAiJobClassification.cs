using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeraGo.Infrastructure.Migrations;

/// <summary>
/// Adds AI job-classification fields to the Jobs table:
///   AiClassification  — raw JSON from the Groq classification service (source of truth).
///   AiClassifiedAt    — when the job was last classified by the AI service.
/// Both nullable: existing jobs keep working; only classified jobs get a value.
/// </summary>
public partial class AddAiJobClassification : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AiClassifiedAt",
            table: "Jobs");

        migrationBuilder.DropColumn(
            name: "AiClassification",
            table: "Jobs");
    }
}
