using EPCApp.Business.Contracts;
using EPCApp.Database.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Web.Controllers;

public class ProposalController : Controller
{
    private readonly IProposalService _proposals;

    public ProposalController(IProposalService proposals)
    {
        _proposals = proposals;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? status, CancellationToken cancellationToken)
    {
        var model = await _proposals.GetListAsync(status, cancellationToken);

        ViewBag.StatusTypes = Enum.GetValues<ProposalStatus>()
            .Select(x => new SelectListItem(x.ToString(), ((int)x).ToString()))
            .ToList();

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken)
    {
        var model = await _proposals.GetDetailAsync(id, cancellationToken);
        if (model is null) return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Recalculate(long id, CancellationToken cancellationToken)
    {
        try
        {
            await _proposals.RecalculateAsync(id, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(long id, string? remarks, CancellationToken cancellationToken)
    {
        await _proposals.SendAsync(id, remarks, cancellationToken);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(long id, string? remarks, CancellationToken cancellationToken)
    {
        try
        {
            var projectId = await _proposals.AcceptAsync(id, remarks, cancellationToken);
            return RedirectToAction("Details", "Project", new { id = projectId });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(long id, string? remarks, CancellationToken cancellationToken)
    {
        await _proposals.RejectAsync(id, remarks, cancellationToken);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Revise(long id, string? remarks, CancellationToken cancellationToken)
    {
        var revised = await _proposals.ReviseAsync(id, remarks, cancellationToken);
        return RedirectToAction(nameof(Details), new { id = revised.Id });
    }
}
