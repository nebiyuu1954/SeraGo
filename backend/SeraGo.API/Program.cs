using Aufy.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SeraGo.API.Endpoints;
using SeraGo.API.Extensions;
using SeraGo.Infrastructure;
using SeraGo.Infrastructure.Context;
using SeraGo.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithJwt();

builder.Services.AddInfrastructure(builder.Configuration); // SQLite DbContext + AuthSeeder
builder.Services.SetupAufy(builder.Configuration);         // Aufy: Identity + JWT + custom signup
builder.Services.Configure<IdentityOptions>(options =>
{
    // MVP: users can sign in right after signup. Flip to true once real email
    // sending (SendGrid/SMTP) is wired up in production.
    options.SignIn.RequireConfirmedEmail = false;
});

var app = builder.Build();

// Apply pending migrations and seed roles + admin account on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<AuthSeeder>().SeedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapAufyEndpoints();      // /api/auth/* and /api/account/* (login, signup, refresh, me, ...)
app.MapProfileEndpoints();   // GET/PUT /api/account/profile — the user's own profile
app.MapAccountEndpoints();   // POST /api/account/deactivate, DELETE /api/account
app.MapForgotPasswordEndpoint(); // POST /api/account/password/forgot — fixed replacement for Aufy 1.0.0's (500s on unknown emails)

app.MapGet("/", () => Results.Ok(new { service = "SeraGo API", docs = "/swagger" }));

app.Run();
