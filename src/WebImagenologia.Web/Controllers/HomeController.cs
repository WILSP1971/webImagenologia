using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebImagenologia.Web.Extensions;
using WebImagenologia.Web.Models;
using WebImagenologia.Web.Models.Domain;

namespace WebImagenologia.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction("Login", "Account");
        }

        ViewBag.DisplayName = User.GetDisplayName();
        ViewBag.UserRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Usuario";
        return View();
    }

    [Authorize]
    public IActionResult AccesoDenegado()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
