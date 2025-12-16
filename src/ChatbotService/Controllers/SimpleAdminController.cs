using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ChatbotService.Models;

namespace ChatbotService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SimpleAdminController : ControllerBase
{
    private readonly ILogger<SimpleAdminController> _logger;

    public SimpleAdminController(ILogger<SimpleAdminController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get admin dashboard
    /// </summary>
    [HttpGet("dashboard")]
    [AllowAnonymous]
    public ActionResult GetDashboard()
    {
        var html = "<!DOCTYPE html><html><head><title>ChatBot Admin</title></head><body><h1>ChatBot Administration</h1><p>Management interface for training and configuration.</p><div><h2>Features</h2><ul><li>Model Training</li><li>Data Management</li><li>Performance Monitoring</li></ul></div></body></html>";
        return Content(html, "text/html");
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public ActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}