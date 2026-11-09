using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.DTOs;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : Controller
{
    private readonly IApiService _apiService;

    public AccountController(IApiService apiService)
    {
        _apiService = apiService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var response = await _apiService.PostAsync("/api/auth/login", dto);
        if (response.IsSuccessStatusCode)
        {
            var user = await response.Content.ReadFromJsonAsync<UserResponseDto>();
            if (user != null)
            {
                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("Role", user.Role);
                HttpContext.Session.SetString("UserName", $"{user.Name} {user.Surname}");

                TempData["Success"] = $"Welcome back, {user.Name}!";
                return user.Role == "Organiser"
                    ? RedirectToAction("Dashboard", "Organiser")
                    : RedirectToAction("Index", "Home");
            }
        }

        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return View(dto);
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var response = await _apiService.PostAsync("/api/auth/register", dto);
        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = "Account created successfully! Please log in.";
            return RedirectToAction(nameof(Login));
        }

        var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        ModelState.AddModelError(string.Empty, error?.GetValueOrDefault("message") ?? "Registration failed.");
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _apiService.PostAsync<object>("/api/auth/logout", new { });
        HttpContext.Session.Clear();
        TempData["Success"] = "You have been logged out.";
        return RedirectToAction("Index", "Home");
    }
}