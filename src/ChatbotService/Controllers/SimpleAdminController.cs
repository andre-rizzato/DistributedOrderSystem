using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ChatbotService.Models;
using ChatbotService.Services.Interfaces;
using ChatbotService.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatbotService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SimpleAdminController : ControllerBase
{
    private readonly ILogger<SimpleAdminController> _logger;
    private readonly ChatContext _context;
    private readonly INLPService _nlpService;

    public SimpleAdminController(
        ILogger<SimpleAdminController> logger,
        ChatContext context,
        INLPService nlpService)
    {
        _logger = logger;
        _context = context;
        _nlpService = nlpService;
    }

    /// <summary>
    /// Get comprehensive admin dashboard with AI capabilities
    /// </summary>
    [HttpGet]
    [HttpGet("dashboard")]
    [AllowAnonymous]
    public ActionResult GetDashboard()
    {
        // Redirect to the comprehensive AI dashboard
        return Redirect("/api/aidashboard");
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public ActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow, model = "DialoGPT-small" });
    }
}