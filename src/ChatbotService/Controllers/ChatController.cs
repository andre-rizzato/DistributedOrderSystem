using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ChatbotService.Models;
using ChatbotService.Services.Interfaces;

namespace ChatbotService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatbotService _chatbotService;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatbotService chatbotService,
        IAuthenticationService authService,
        ILogger<ChatController> logger)
    {
        _chatbotService = chatbotService;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Send a message to the chatbot
    /// </summary>
    /// <param name="request">Chat request containing message and session info</param>
    /// <returns>Bot response</returns>
    [HttpPost("message")]
    public async Task<ActionResult<ChatResponse>> SendMessage([FromBody] ChatRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Message cannot be empty");
        }

        try
        {
            var response = await _chatbotService.ProcessMessageAsync(request, ct: ct);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return StatusCode(500, new ChatResponse
            {
                Message = "I'm sorry, I'm having trouble understanding right now. Please try again.",
                Intent = "error",
                Confidence = 0,
                SessionId = request.SessionId ?? string.Empty
            });
        }
    }

    /// <summary>
    /// Get chat session history
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <returns>Chat history</returns>
    [HttpGet("history/{sessionId}")]
    public async Task<ActionResult<ChatHistoryResponse>> GetChatHistory(string sessionId)
    {
        try
        {
            var history = await _chatbotService.GetChatHistoryAsync(sessionId);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chat history for session {SessionId}", sessionId);
            return StatusCode(500, "Error retrieving chat history");
        }
    }

    /// <summary>
    /// Create a new chat session
    /// </summary>
    /// <returns>New session ID</returns>
    [HttpPost("session")]
    public async Task<ActionResult<SessionResponse>> CreateSession()
    {
        try
        {
            var sessionId = await _chatbotService.CreateSessionAsync();
            return Ok(new SessionResponse { SessionId = sessionId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating chat session");
            return StatusCode(500, "Error creating session");
        }
    }

    /// <summary>
    /// End a chat session
    /// </summary>
    /// <param name="sessionId">Session to end</param>
    [HttpDelete("session/{sessionId}")]
    public async Task<ActionResult> EndSession(string sessionId)
    {
        try
        {
            await _chatbotService.EndSessionAsync(sessionId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending session {SessionId}", sessionId);
            return StatusCode(500, "Error ending session");
        }
    }

    /// <summary>
    /// Authenticate user for chat access
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT token</returns>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Username and password are required");
        }

        try
        {
            var response = await _authService.AuthenticateAsync(request);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return Unauthorized(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication");
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                ErrorMessage = "Authentication error occurred"
            });
        }
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <returns>Registration result</returns>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || 
            string.IsNullOrWhiteSpace(request.Password) || 
            string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest("Username, password, and email are required");
        }

        try
        {
            var response = await _authService.RegisterAsync(request);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                ErrorMessage = "Registration error occurred"
            });
        }
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    /// <returns>User profile information</returns>
    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<UserProfile>> GetProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var profile = await _authService.GetUserProfileAsync(userId);
            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile for {UserId}", userId);
            return StatusCode(500, "Error retrieving profile");
        }
    }

    /// <summary>
    /// Get available chat intents
    /// </summary>
    /// <returns>List of supported intents</returns>
    [HttpGet("intents")]
    public async Task<ActionResult<List<string>>> GetIntents()
    {
        try
        {
            var intents = await _chatbotService.GetSupportedIntentsAsync();
            return Ok(intents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving intents");
            return StatusCode(500, "Error retrieving intents");
        }
    }

    /// <summary>
    /// Provide feedback on a bot response
    /// </summary>
    /// <param name="feedback">User feedback</param>
    [HttpPost("feedback")]
    public async Task<ActionResult> ProvideFeedback([FromBody] FeedbackRequest feedback)
    {
        try
        {
            await _chatbotService.StoreFeedbackAsync(feedback);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing feedback");
            return StatusCode(500, "Error storing feedback");
        }
    }

}