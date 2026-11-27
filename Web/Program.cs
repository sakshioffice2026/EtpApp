using EPCApp.Business.Contracts;
using EPCApp.Business.Resolvers;
using EPCApp.Business.Services;
using EPCApp.Database.Context;
using EPCApp.Engine.Abstractions;
using EPCApp.Repositories.Contracts;
using EPCApp.Repositories.Repositories;
using EPCApp.RulePacks.Etp;
using EPCApp.RulePacks.Stp;
using EPCApp.RulePacks.Zld;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options
        .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
        .UseSnakeCaseNamingConvention());

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IEnquiryRepository, EnquiryRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IDesignBasisLoader, DesignBasisLoader>();
builder.Services.AddScoped<IProposalRepository, ProposalRepository>();
builder.Services.AddScoped<IDesignRuleRepository, DesignRuleRepository>();
builder.Services.AddScoped<IRateCardRepository, RateCardRepository>();

builder.Services.AddSingleton<IPlantRulePack, EtpRulePack>();
builder.Services.AddSingleton<IPlantRulePack, StpRulePack>();
builder.Services.AddSingleton<IPlantRulePack, ZldRulePack>();
builder.Services.AddSingleton<RulePackResolver>();

builder.Services.AddScoped<IEnquiryService, EnquiryService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IProposalService, ProposalService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Project/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Project}/{action=Index}/{id?}");

app.Run();