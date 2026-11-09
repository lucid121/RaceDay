using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.DTOs;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class ParticipantController : Controller
{
    private readonly IApiService _apiService;

    public ParticipantController(IApiService apiService)
    {
        _apiService = apiService;
    }

    private bool IsParticipant() => HttpContext.Session.GetString("Role") == "Participant";

    [HttpGet]
    public async Task<IActionResult> EventDetail(int id)
    {
        var evResponse = await _apiService.GetAsync($"/api/events/{id}");
        if (!evResponse.IsSuccessStatusCode) return NotFound();

        var ev = await evResponse.Content.ReadFromJsonAsync<EventDto>();
        var catResponse = await _apiService.GetAsync($"/api/events/{id}/categories");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryDto>>() ?? new List<CategoryDto>();

        ViewBag.Categories = categories;
        return View(ev);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int categoryId)
    {
        if (!IsParticipant()) return RedirectToAction("Login", "Account");

        var response = await _apiService.PostAsync("/api/enrolments", new EnrolmentRequestDto { CategoryId = categoryId });
        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = "Successfully enrolled in the event!";
            return RedirectToAction(nameof(MyEnrolments));
        }

        var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        TempData["Error"] = error?.GetValueOrDefault("message") ?? "Enrolment failed.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> MyEnrolments()
    {
        if (!IsParticipant()) return RedirectToAction("Login", "Account");

        var response = await _apiService.GetAsync("/api/enrolments/mine");
        var enrolments = await response.Content.ReadFromJsonAsync<List<EnrolmentDto>>() ?? new List<EnrolmentDto>();
        return View(enrolments);
    }

    [HttpGet]
    public async Task<IActionResult> MyResults()
    {
        if (!IsParticipant()) return RedirectToAction("Login", "Account");

        var response = await _apiService.GetAsync("/api/results/mine");
        var results = await response.Content.ReadFromJsonAsync<List<ResultDto>>() ?? new List<ResultDto>();
        return View(results);
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var response = await _apiService.GetAsync("/api/users/me");
        if (!response.IsSuccessStatusCode) return RedirectToAction("Login", "Account");

        var profile = await response.Content.ReadFromJsonAsync<UserResponseDto>();
        return View(profile);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(UpdateProfileDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        var response = await _apiService.PutAsync("/api/users/me", dto);
        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction(nameof(Profile));
        }

        ModelState.AddModelError(string.Empty, "Failed to update profile.");
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadProfilePicture(IFormFile pictureFile)
    {
        if (pictureFile != null)
        {
            var response = await _apiService.PostFileAsync("/api/users/me/picture", pictureFile);
            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Profile picture updated!";
            }
            else
            {
                TempData["Error"] = "Failed to upload image. Must be JPG, PNG, or WEBP under 5MB.";
            }
        }
        return RedirectToAction(nameof(Profile));
    }


}