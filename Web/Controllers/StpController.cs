using EPCApp.Business.Contracts;
using EPCApp.Model.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

public class StpController : Controller
{
    private readonly IProjectService _projects;
    private readonly IProposalService _proposals;

    public StpController(IProjectService projects, IProposalService proposals)
    {
        _projects = projects;
        _proposals = proposals;
    }

    [HttpGet]
    public async Task<IActionResult> Intake(long projectId, CancellationToken cancellationToken)
    {
        var project = await _projects.GetByIdAsync(projectId, cancellationToken);
        if (project is null) return NotFound();

        await LoadIntakeAsync(project, cancellationToken);

        return View(new StreamInputDto { ProjectId = projectId, StreamName = "Stream 1" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Intake(StreamInputDto model, CancellationToken cancellationToken)
    {
        if (!model.ProjectId.HasValue) return BadRequest("Project ID is required.");

        var project = await _projects.GetByIdAsync(model.ProjectId.Value, cancellationToken);
        if (project is null) return NotFound();

        if (string.IsNullOrWhiteSpace(model.StreamName))
            ModelState.AddModelError(nameof(model.StreamName), "Stream name is required.");

        if (model.FlowM3PerDay <= 0)
            ModelState.AddModelError(nameof(model.FlowM3PerDay), "Flow must be greater than zero.");

        if (!ModelState.IsValid)
        {
            await LoadIntakeAsync(project, cancellationToken);
            return View(model);
        }

        await _projects.SaveStreamAsync(model, cancellationToken);

        return RedirectToAction(nameof(Intake), new { projectId = model.ProjectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteStream(long id, long projectId, CancellationToken cancellationToken)
    {
        await _projects.DeleteStreamAsync(id, cancellationToken);
        return RedirectToAction(nameof(Intake), new { projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Calculate(long projectId, CancellationToken cancellationToken)
    {
        try
        {
            var proposal = await _proposals.CalculateAsync(projectId, cancellationToken);
            return RedirectToAction(nameof(Result), new { id = proposal.Id });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Intake), new { projectId });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Result(long id, CancellationToken cancellationToken)
    {
        var model = await _proposals.GetResultAsync(id, cancellationToken);
        if (model is null) return NotFound();

        return View(model);
    }

    private async Task LoadIntakeAsync(ProjectDto project, CancellationToken cancellationToken)
    {
        ViewBag.Project = project;
        ViewBag.Streams = await _projects.GetStreamsAsync(project.Id, cancellationToken);
    }
}