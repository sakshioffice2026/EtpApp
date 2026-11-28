using EPCApp.Business.Contracts;
using EPCApp.Database.Enums;
using EPCApp.Model.DTOs;
using EPCApp.Model.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Web.Controllers;

public class ProjectController : Controller
{
    private readonly IProjectService _projects;
    private readonly IProposalService _proposals;

    public ProjectController(IProjectService projects, IProposalService proposals)
    {
        _projects = projects;
        _proposals = proposals;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? plantType, CancellationToken cancellationToken)
    {
        var rows = await _projects.GetAllAsync(plantType, cancellationToken);

        ViewBag.PlantType = plantType;

        return View(rows.Select(ToViewModel).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Create(int plantType = (int)PlantType.Etp, CancellationToken cancellationToken = default)
    {
        var model = new ProjectViewModel
        {
            PlantType = plantType,
            IndustryType = (int)IndustryType.Other,
            ZldRequired = plantType == (int)PlantType.Zld,
            ProjectCode = await _projects.GenerateProjectCodeAsync(plantType, cancellationToken)
        };

        LoadLookups();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectViewModel model, CancellationToken cancellationToken)
    {
        ModelState.Remove(nameof(ProjectViewModel.PlantTypeName));
        ModelState.Remove(nameof(ProjectViewModel.IndustryTypeName));

        if (!ModelState.IsValid)
        {
            LoadLookups();
            return View(model);
        }

        var created = await _projects.CreateAsync(new ProjectDto
        {
            ProjectCode = model.ProjectCode,
            Name = model.Name,
            ClientName = model.ClientName,
            PlantType = model.PlantType,
            IndustryType = model.IndustryType,
            ZldRequired = model.ZldRequired,
            SiteLocation = model.SiteLocation,
            Remarks = model.Remarks
        }, cancellationToken);

        var intakeController = created.PlantType == (int)PlantType.Stp ? "Stp"
            : created.PlantType == (int)PlantType.Zld ? "Zld"
            : "Etp";

        return RedirectToAction("Intake", intakeController, new { projectId = created.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken)
    {
        var project = await _projects.GetByIdAsync(id, cancellationToken);
        if (project is null) return NotFound();

        ViewBag.Streams = await _projects.GetStreamsAsync(id, cancellationToken);
        ViewBag.Proposals = await _proposals.GetByProjectAsync(id, cancellationToken);

        return View(ToViewModel(project));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await _projects.DeleteAsync(id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Error() => Problem();

    private void LoadLookups()
    {
        ViewBag.PlantTypes = new List<SelectListItem>
        {
            new("ETP", ((int)PlantType.Etp).ToString()),
            new("STP", ((int)PlantType.Stp).ToString()),
            new("ZLD", ((int)PlantType.Zld).ToString())
        };

        ViewBag.IndustryTypes = Enum.GetValues<IndustryType>()
            .Select(x => new SelectListItem(x.ToString(), ((int)x).ToString()))
            .ToList();
    }

    private static ProjectViewModel ToViewModel(ProjectDto x) => new()
    {
        Id = x.Id,
        ProjectCode = x.ProjectCode,
        Name = x.Name,
        ClientName = x.ClientName,
        PlantType = x.PlantType,
        IndustryType = x.IndustryType,
        ZldRequired = x.ZldRequired,
        SiteLocation = x.SiteLocation,
        Remarks = x.Remarks,
        PlantTypeName = ((PlantType)x.PlantType).ToString().ToUpperInvariant(),
        IndustryTypeName = ((IndustryType)x.IndustryType).ToString()
    };
}
