using EPCApp.Business.Contracts;
using EPCApp.Database.Enums;
using EPCApp.Model.DTOs;
using EPCApp.Model.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Web.Controllers;

public class EnquiryController : Controller
{
    private readonly IEnquiryService _enquiries;
    private readonly IProposalService _proposals;

    public EnquiryController(IEnquiryService enquiries, IProposalService proposals)
    {
        _enquiries = enquiries;
        _proposals = proposals;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? plantType, int? status, CancellationToken cancellationToken)
    {
        var rows = await _enquiries.GetAllAsync(plantType, status, cancellationToken);

        var model = new EnquiryListViewModel
        {
            PlantType = plantType,
            Status = status,
            Items = rows.Select(x => new EnquiryListItemViewModel
            {
                Id = x.Id,
                EnquiryNo = x.EnquiryNo,
                ClientName = x.ClientName,
                ProjectName = x.ProjectName,
                PlantType = x.PlantType,
                PlantTypeName = ((PlantType)x.PlantType).ToString().ToUpperInvariant(),
                IndustryTypeName = ((IndustryType)x.IndustryType).ToString(),
                ZldRequired = x.ZldRequired,
                Status = x.Status,
                StatusName = ((EnquiryStatus)x.Status).ToString(),
                CreatedAt = x.CreatedAt
            }).ToList()
        };

        LoadLookups();

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int plantType = (int)PlantType.Etp, CancellationToken cancellationToken = default)
    {
        var model = new EnquiryFormViewModel
        {
            PlantType = plantType,
            IndustryType = (int)IndustryType.Other,
            ZldRequired = plantType == (int)PlantType.Zld,
            EnquiryNo = await _enquiries.GenerateEnquiryNoAsync(plantType, cancellationToken),
            Streams = new List<EnquiryStreamDto> { new() }
        };

        LoadLookups();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EnquiryFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            if (model.Streams.Count == 0) model.Streams.Add(new EnquiryStreamDto());
            LoadLookups();
            return View(model);
        }

        var created = await _enquiries.CreateAsync(ToDto(model), cancellationToken);

        return RedirectToAction(nameof(Details), new { id = created.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        var enquiry = await _enquiries.GetByIdAsync(id, cancellationToken);
        if (enquiry is null) return NotFound();

        var model = ToViewModel(enquiry);
        if (model.Streams.Count == 0) model.Streams.Add(new EnquiryStreamDto());

        LoadLookups();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, EnquiryFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            if (model.Streams.Count == 0) model.Streams.Add(new EnquiryStreamDto());
            LoadLookups();
            return View(model);
        }

        var updated = await _enquiries.UpdateAsync(ToDto(model), cancellationToken);
        if (!updated) return NotFound();

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken)
    {
        var enquiry = await _enquiries.GetByIdAsync(id, cancellationToken);
        if (enquiry is null) return NotFound();

        ViewBag.Proposals = await _proposals.GetByEnquiryAsync(id, cancellationToken);
        ViewBag.PlantTypeName = ((PlantType)enquiry.PlantType).ToString().ToUpperInvariant();
        ViewBag.IndustryTypeName = ((IndustryType)enquiry.IndustryType).ToString();
        ViewBag.StatusName = ((EnquiryStatus)enquiry.Status).ToString();
        ViewBag.CreatedAt = enquiry.CreatedAt;

        return View(ToViewModel(enquiry));
    }

    [HttpGet]
    public IActionResult StreamRow(int index)
    {
        ViewBag.Index = index;
        return PartialView("_StreamRow", new EnquiryStreamDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(long id, CancellationToken cancellationToken)
    {
        await _enquiries.SetStatusAsync(id, (int)EnquiryStatus.Submitted, cancellationToken);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(long id, CancellationToken cancellationToken)
    {
        await _enquiries.SetStatusAsync(id, (int)EnquiryStatus.Closed, cancellationToken);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProposal(long id, CancellationToken cancellationToken)
    {
        var proposal = await _proposals.CreateFromEnquiryAsync(id, cancellationToken);
        return RedirectToAction("Details", "Proposal", new { id = proposal.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await _enquiries.DeleteAsync(id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

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

        ViewBag.StatusTypes = Enum.GetValues<EnquiryStatus>()
            .Select(x => new SelectListItem(x.ToString(), ((int)x).ToString()))
            .ToList();
    }

    private static EnquiryDto ToDto(EnquiryFormViewModel x) => new()
    {
        Id = x.Id,
        EnquiryNo = x.EnquiryNo,
        ClientName = x.ClientName,
        ContactPerson = x.ContactPerson,
        Email = x.Email,
        Phone = x.Phone,
        ProjectName = x.ProjectName,
        PlantType = x.PlantType,
        IndustryType = x.IndustryType,
        ZldRequired = x.ZldRequired,
        SiteLocation = x.SiteLocation,
        Status = x.Status,
        Remarks = x.Remarks,
        Streams = x.Streams
    };

    private static EnquiryFormViewModel ToViewModel(EnquiryDto x) => new()
    {
        Id = x.Id,
        EnquiryNo = x.EnquiryNo,
        ClientName = x.ClientName,
        ContactPerson = x.ContactPerson,
        Email = x.Email,
        Phone = x.Phone,
        ProjectName = x.ProjectName,
        PlantType = x.PlantType,
        IndustryType = x.IndustryType,
        ZldRequired = x.ZldRequired,
        SiteLocation = x.SiteLocation,
        Status = x.Status,
        Remarks = x.Remarks,
        Streams = x.Streams
    };
}