namespace ChatbotService.Models;

public record AuthRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public bool RememberMe { get; init; }
}

public record AuthResponse
{
    public bool Success { get; init; }
    public string Token { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public UserInfo User { get; init; } = new();
    public string? ErrorMessage { get; init; }
}

public record UserInfo
{
    public string Id { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = new();
}

public record UserProfile
{
    public string Id { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime LastLogin { get; init; }
}

public record LoginRequest
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public record RegisterRequest
{
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public record FineTuningRequest
{
    public List<TrainingExample> TrainingData { get; init; } = new();
    public int Epochs { get; init; } = 10;
    public float LearningRate { get; init; } = 0.001f;
    public int BatchSize { get; init; } = 16;
    public bool SaveCheckpoints { get; init; } = true;
    public string? ModelName { get; init; }
}

public record FineTuningResponse
{
    public bool Success { get; init; }
    public string JobId { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public FineTuningProgress? Progress { get; init; }
}

public record FineTuningProgress
{
    public string JobId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int CurrentEpoch { get; init; }
    public int TotalEpochs { get; init; }
    public float CurrentLoss { get; init; }
    public float ValidationAccuracy { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? ErrorMessage { get; init; }
}