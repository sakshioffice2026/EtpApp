using EtpApp.Business.Contracts;
using EtpApp.Business.Resolvers;
using EtpApp.Business.Services;
using EtpApp.Engine.Abstractions;
using EtpApp.Repositories.Contracts;
using EtpApp.Repositories.Repositories;
using EtpApp.RulePacks.Etp;
using EtpApp.RulePacks.Stp;
using EtpApp.RulePacks.Zld;

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