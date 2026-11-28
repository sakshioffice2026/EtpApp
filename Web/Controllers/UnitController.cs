using EPCApp.Business.Contracts;
using EPCApp.Business.Explain;
using EPCApp.Model.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

public class UnitController : Controller
{
    private readonly IProposalService _proposals;

    public UnitController(IProposalService proposals)
    {
        _proposals = proposals;
    }

    [HttpGet]
    public async Task<IActionResult> Detail(long proposalId, string unit, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(unit)) return NotFound();

        var proposal = await _proposals.GetByIdAsync(proposalId, cancellationToken);
        if (proposal is null || !proposal.ProjectId.HasValue) return NotFound();

        var result = await _proposals.GetResultAsync(proposalId, cancellationToken);
        if (result is null) return NotFound();

        var unitName = result.TrainOperations
            .FirstOrDefault(x => string.Equals(x, unit, StringComparison.OrdinalIgnoreCase));
        if (unitName is null) return NotFound();

        var model = new UnitDetailViewModel
        {
            ProposalId = result.ProposalId,
            ProposalNo = result.ProposalNo,
            ProjectName = result.ProjectName,
            ClientName = result.ClientName,
            PlantTypeName = result.PlantTypeName,
            PlantController = result.PlantTypeName switch
            {
                "STP" => "Stp",
                "ZLD" => "Zld",
                _ => "Etp"
            },
            UnitTypeName = unitName,
            Purpose = UnitCalculationExplainer.Purpose(unitName)
        };

        var equipment = result.Equipment
            .Where(x => string.Equals(x.UnitTypeName, unitName, StringComparison.OrdinalIgnoreCase));

        foreach (var item in equipment)
        {
            var explanation = UnitCalculationExplainer.Explain(unitName, item.Parameters);

            model.Sections.Add(new UnitEquipmentSection
            {
                Tag = item.TagNo,
                Description = item.Description,
                MaterialName = item.MaterialName,
                Parameters = item.Parameters,
                Inputs = explanation.Inputs,
                Steps = explanation.Steps,
                BoqLines = result.BoqLines
                    .Where(b => string.Equals(b.TagNo, item.TagNo, StringComparison.OrdinalIgnoreCase))
                    .ToList()
            });
        }

        return View(model);
    }
}
