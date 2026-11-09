using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.DTOs;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : Controller
{
    private readonly IApiService _apiService;

    public HomeController(IApiService apiService)
    {
        _apiService = apiService;
    }

    /// <summary>
    /// Displays upcoming events for public browsing.
    /// Supports filtering by event type (Run, Walk, Cycle).
    /// </summary>
    public async Task<IActionResult> Index(string? eventType)
    {
        var path = string.IsNullOrWhiteSpace(eventType)
            ? "/api/events?upcomingOnly=true"
            : $"/api/events?upcomingOnly=true&eventType={eventType}";

        var response = await _apiService.GetAsync(path);

        if (!response.IsSuccessStatusCode)
        {
            ViewBag.ErrorMessage = "Unable to load events right now. Please try again later.";
            return View(new List<EventDto>());
        }

        var events = await response.Content.ReadFromJsonAsync<List<EventDto>>() ?? new List<EventDto>();
        ViewBag.CurrentFilter = eventType;
        return View(events);
    }

    /// <summary>
    /// Friendly custom error handler page to catch unhandled errors gracefully.
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}