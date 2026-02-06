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
    public async Task<ActionResult<ChatResponse>> SendMessage([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Message cannot be empty");
        }

        try
        {
            var response = await _chatbotService.ProcessMessageAsync(request);
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

    /// <summary>
    /// Get simple chat interface
    /// </summary>
    [HttpGet("interface")]
    public ActionResult GetChatInterface()
    {
        var html = GetChatInterfaceHtml();
        return Content(html, "text/html");
    }

    private static string GetChatInterfaceHtml()
    {
        return @"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Chatbot Interface</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 0; padding: 20px; background: #f5f5f5; }
        .chat-container { max-width: 800px; margin: 0 auto; background: white; border-radius: 10px; box-shadow: 0 0 10px rgba(0,0,0,0.1); }
        .chat-header { background: #007bff; color: white; padding: 20px; border-radius: 10px 10px 0 0; }
        .chat-messages { height: 400px; overflow-y: auto; padding: 20px; border-bottom: 1px solid #eee; }
        .chat-input { display: flex; padding: 20px; }
        .chat-input input { flex: 1; padding: 10px; border: 1px solid #ddd; border-radius: 5px; margin-right: 10px; }
        .chat-input button { padding: 10px 20px; background: #007bff; color: white; border: none; border-radius: 5px; cursor: pointer; }
        .message { margin-bottom: 15px; }
        .user-message { text-align: right; }
        .bot-message { text-align: left; }
        .message-bubble { display: inline-block; padding: 10px 15px; border-radius: 15px; max-width: 70%; }
        .user-message .message-bubble { background: #007bff; color: white; }
        .bot-message .message-bubble { background: #e9ecef; color: #333; }
    </style>
</head>
<body>
    <div class='chat-container'>
        <div class='chat-header'>
            <h1>🤖 Chatbot Assistant</h1>
            <p>Ask me anything about orders, products, or get help with your account!</p>
        </div>
        
        <div id='messages' class='chat-messages'>
            <div class='message bot-message'>
                <div class='message-bubble'>
                    Hello! I'm your virtual assistant. How can I help you today?
                </div>
            </div>
        </div>
        
        <div class='chat-input'>
            <input type='text' id='messageInput' placeholder='Type your message...' onkeypress='handleKeyPress(event)'>
            <button onclick='sendMessage()'>Send</button>
        </div>
    </div>
    
    <script>
        let sessionId = 'session_' + Math.random().toString(36).substring(2, 15);
        
        function handleKeyPress(event) {
            if (event.key === 'Enter') {
                sendMessage();
            }
        }
        
        async function sendMessage() {
            const input = document.getElementById('messageInput');
            const message = input.value.trim();
            
            if (!message) return;
            
            // Add user message to chat
            addMessage(message, 'user');
            input.value = '';
            
            try {
                // Send to bot
                const response = await fetch('/api/chat/message', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ message: message, sessionId: sessionId })
                });
                
                const data = await response.json();
                addMessage(data.message || 'Sorry, I didn\\'t understand that.', 'bot');
            } catch (error) {
                addMessage('Sorry, I\\'m having trouble connecting. Please try again.', 'bot');
            }
        }
        
        function addMessage(message, sender) {
            const messagesDiv = document.getElementById('messages');
            const messageDiv = document.createElement('div');
            messageDiv.className = 'message ' + sender + '-message';
            
            const bubble = document.createElement('div');
            bubble.className = 'message-bubble';
            bubble.textContent = message;
            
            messageDiv.appendChild(bubble);
            messagesDiv.appendChild(messageDiv);
            messagesDiv.scrollTop = messagesDiv.scrollHeight;
        }
    </script>
</body>
</html>";
    }
}