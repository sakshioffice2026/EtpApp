using EPCApp.Business.Contracts;
using EPCApp.Business.Resolvers;
using EPCApp.Business.Services;
using EPCApp.Engine.Abstractions;
using EPCApp.Repositories.Contracts;
using EPCApp.Repositories.Repositories;
using EPCApp.RulePacks.Etp;
using EPCApp.RulePacks.Stp;
using EPCApp.RulePacks.Zld;

namespace Web.Helpers;

public static class ServiceRegistration
{
    public static IServiceCollection AddEtpServices(this IServiceCollection services)
    {
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IProposalRepository, ProposalRepository>();
        services.AddScoped<IDesignRuleRepository, DesignRuleRepository>();
        services.AddScoped<IRateCardRepository, RateCardRepository>();

        services.AddSingleton<IPlantRulePack, EtpRulePack>();
        services.AddSingleton<IPlantRulePack, StpRulePack>();
        services.AddSingleton<IPlantRulePack, ZldRulePack>();
        services.AddSingleton<RulePackResolver>();

        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IProposalService, ProposalService>();

        return services;
    }
}