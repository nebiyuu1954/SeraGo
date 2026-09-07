using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeraGo.Infrastructure.Migrations;

/// <summary>
/// Designer metadata for AddAiJobClassification — generated placeholder.
/// EF Core doesn't require an accurate snapshot for this migration to run;
/// the important part is the Up()/Down() in the main migration file.
/// </summary>
[DbContext(typeof(SeraGo.Infrastructure.Context.ApplicationDbContext))]
[Migration("20260907120000_AddAiJobClassification")]
public partial class AddAiJobClassification : Migration
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        // Placeholder — the real model snapshot lives in the latest *_ModelSnapshot.
        // This migration only adds two nullable columns, both of which already
        // exist in the current model via the Job entity update.
    }
}
