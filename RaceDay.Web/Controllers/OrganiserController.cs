using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.DTOs;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class OrganiserController : Controller
{
    private readonly IApiService _apiService;

    public OrganiserController(IApiService apiService)
    {
        _apiService = apiService;
    }

    private bool IsOrganiser() => HttpContext.Session.GetString("Role") == "Organiser";

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        if (!IsOrganiser()) return RedirectToAction("Login", "Account");

        var response = await _apiService.GetAsync("/api/events/mine");
        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = "Unable to load dashboard events.";
            return View(new List<EventDto>());
        }

        var events = await response.Content.ReadFromJsonAsync<List<EventDto>>() ?? new List<EventDto>();
        return View(events);
    }

    [HttpGet]
    public async Task<IActionResult> Events()
    {
        if (!IsOrganiser()) return RedirectToAction("Login", "Account");

        var response = await _apiService.GetAsync("/api/events/mine");
        var events = await response.Content.ReadFromJsonAsync<List<EventDto>>() ?? new List<EventDto>();
        return View(events);
    }

    [HttpGet]
    public IActionResult CreateEvent()
    {
        if (!IsOrganiser()) return RedirectToAction("Login", "Account");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventRequestDto dto, IFormFile? bannerFile)
    {
        if (!IsOrganiser()) return RedirectToAction("Login", "Account");
        if (!ModelState.IsValid) return View(dto);

        var response = await _apiService.PostAsync("/api/events", dto);
        if (response.IsSuccessStatusCode)
        {
            var created = await response.Content.ReadFromJsonAsync<EventDto>();
            if (bannerFile != null && created != null)
            {
                await _apiService.PostFileAsync($"/api/events/{created.EventId}/banner", bannerFile);
            }

            TempData["Success"] = "Event created successfully!";
            return RedirectToAction(nameof(Events));
        }

        ModelState.AddModelError(string.Empty, "Failed to create event.");
        return View(dto);
    }

    [HttpGet]
    public async Task<IActionResult> Categories(int eventId)
    {
        if (!IsOrganiser()) return RedirectToAction("Login", "Account");

        var response = await _apiService.GetAsync($"/api/events/{eventId}/categories");
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>() ?? new List<CategoryDto>();
        ViewBag.EventId = eventId;
        return View(categories);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(int eventId, CategoryRequestDto dto)
    {
        if (!IsOrganiser()) return RedirectToAction("Login", "Account");
        if (!ModelState.IsValid) return RedirectToAction(nameof(Categories), new { eventId });

        var response = await _apiService.PostAsync($"/api/events/{eventId}/categories", dto);
        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = "Category added successfully!";
        }
        else
        {
            TempData["Error"] = "Failed to add category.";
        }

        return RedirectToAction(nameof(Categories), new { eventId });
    }

    [HttpGet]
    public async Task<IActionResult> Enrolments(int eventId)
    {
        if (!IsOrganiser()) return RedirectToAction("Login", "Account");

        var response = await _apiService.GetAsync($"/api/events/{eventId}/enrolments");
        var enrolments = await response.Content.ReadFromJsonAsync<List<EnrolmentDto>>() ?? new List<EnrolmentDto>();
        ViewBag.EventId = eventId;
        return View(enrolments);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateEnrolmentStatus(int enrolmentId, int eventId, string status)
    {
        if (!IsOrganiser()) return RedirectToAction("Login", "Account");

        var response = await _apiService.PutAsync($"/api/enrolments/{enrolmentId}/status", new UpdateEnrolmentStatusDto { Status = status });
        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = "Enrolment status updated!";
        }
        else
        {
            TempData["Error"] = "Failed to update status.";
        }

        return RedirectToAction(nameof(Enrolments), new { eventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveResult(int enrolmentId, int eventId, ResultRequestDto dto)
    {
        if (!IsOrganiser()) return RedirectToAction("Login", "Account");

        var response = await _apiService.PutAsync($"/api/enrolments/{enrolmentId}/result", dto);
        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = "Race result captured successfully!";
        }
        else
        {
            TempData["Error"] = "Failed to capture result. Verify finish time formatting.";
        }

        return RedirectToAction(nameof(Enrolments), new { eventId });
    }


}