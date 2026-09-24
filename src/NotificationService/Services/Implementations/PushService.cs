using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NotificationService.Configuration;
using NotificationService.Data;
using NotificationService.Models;
using NotificationService.Models.Requests;
using NotificationService.Models.Responses;
using NotificationService.Services;

namespace NotificationService.Services.Implementations;

/// <summary>
/// Push service implementation using Firebase Cloud Messaging
/// </summary>
public class FirebasePushService : IPushService
{
    private readonly PushNotificationSettings _settings;
    private readonly ILogger<FirebasePushService> _logger;
    private readonly NotificationContext _context;
    private readonly FirebaseMessaging _messaging;

    public FirebasePushService(
        IOptions<PushNotificationSettings> settings,
        ILogger<FirebasePushService> logger,
        NotificationContext context)
    {
        _settings = settings.Value;
        _logger = logger;
        _context = context;

        // Initialize Firebase if not already done
        if (FirebaseApp.DefaultInstance == null)
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromJson(_settings.ServiceAccountJson)
            });
        }

        _messaging = FirebaseMessaging.DefaultInstance;
    }

    /// <inheritdoc/>
    public async Task<NotificationResponse> SendPushNotificationAsync(SendPushNotificationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new Message
            {
                Token = request.DeviceToken,
                Notification = new FirebaseAdmin.Messaging.Notification
                {
                    Title = request.Title,
                    Body = request.Body,
                    ImageUrl = request.ImageUrl
                },
                Data = request.Data ?? new Dictionary<string, string>(),
                Android = CreateAndroidConfig(request),
                Apns = CreateApnsConfig(request)
            };

            var response = await _messaging.SendAsync(message, cancellationToken);

            _logger.LogInformation("Push notification sent successfully. Response: {Response}", response);

            return new NotificationResponse
            {
                Success = true,
                MessageId = response,
                ExternalId = response,
                Status = "sent"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending push notification to token {Token}", request.DeviceToken);
            return NotificationResponse.CreateError($"Push send error: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<BulkNotificationResponse> SendBulkPushNotificationAsync(List<SendPushNotificationRequest> requests, CancellationToken cancellationToken = default)
    {
        var response = new BulkNotificationResponse
        {
            TotalRequests = requests.Count
        };

        // Group by similar content to optimize sending
        var messages = requests.Select(request => new Message
        {
            Token = request.DeviceToken,
            Notification = new FirebaseAdmin.Messaging.Notification
            {
                Title = request.Title,
                Body = request.Body,
                ImageUrl = request.ImageUrl
            },
            Data = request.Data ?? new Dictionary<string, string>(),
            Android = CreateAndroidConfig(request),
            Apns = CreateApnsConfig(request)
        }).ToList();

        try
        {
            var batchResponse = await _messaging.SendAllAsync(messages, cancellationToken);

            for (int i = 0; i < requests.Count; i++)
            {
                var result = batchResponse.Responses[i];
                if (result.IsSuccess)
                {
                    response.SuccessCount++;
                    response.Results.Add(new BulkNotificationResult
                    {
                        Recipient = requests[i].DeviceToken,
                        Success = true,
                        MessageId = result.MessageId,
                        ExternalId = result.MessageId
                    });
                }
                else
                {
                    response.FailureCount++;
                    response.Results.Add(new BulkNotificationResult
                    {
                        Recipient = requests[i].DeviceToken,
                        Success = false,
                        Error = result.Exception?.Message ?? "Unknown error"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk push notifications");
            // If the whole batch fails
            response.FailureCount = response.TotalRequests;
            foreach (var request in requests)
            {
                response.Results.Add(new BulkNotificationResult
                {
                    Recipient = request.DeviceToken,
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        response.Success = response.SuccessCount > 0;
        return response;
    }

    /// <inheritdoc/>
    public async Task<NotificationResponse> SendToUserAsync(string userId, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all of the user's device tokens
            var deviceTokens = await GetUserDeviceTokensAsync(userId, cancellationToken);

            if (!deviceTokens.Any())
            {
                _logger.LogWarning("No device token found for user {UserId}", userId);
                return NotificationResponse.CreateError("No device registered for the user");
            }

            var requests = deviceTokens.Select(token => new SendPushNotificationRequest
            {
                Target = token,
                DeviceToken = token,
                Title = title,
                Body = body,
                Data = data
            }).ToList();

            var bulkResponse = await SendBulkPushNotificationAsync(requests, cancellationToken);

            return new NotificationResponse
            {
                Success = bulkResponse.Success,
                MessageId = $"user_{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}",
                Status = bulkResponse.Success ? "sent" : "failed",
                Error = bulkResponse.Success ? null : $"Send failed for {bulkResponse.FailureCount} devices"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending push notification to user {UserId}", userId);
            return NotificationResponse.CreateError($"User push send error: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<NotificationResponse> SendToTopicAsync(string topic, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new Message
            {
                Topic = topic,
                Notification = new FirebaseAdmin.Messaging.Notification
                {
                    Title = title,
                    Body = body
                },
                Data = data ?? new Dictionary<string, string>()
            };

            var response = await _messaging.SendAsync(message, cancellationToken);

            _logger.LogInformation("Push notification sent to topic {Topic}. Response: {Response}", topic, response);

            return new NotificationResponse
            {
                Success = true,
                MessageId = response,
                ExternalId = response,
                Status = "sent"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending push notification to topic {Topic}", topic);
            return NotificationResponse.CreateError($"Topic push send error: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RegisterDeviceTokenAsync(string userId, string deviceToken, string platform, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check whether the token already exists
            var existingToken = await _context.Database
                .SqlQuery<string>($"SELECT DeviceToken FROM UserDeviceTokens WHERE UserId = {userId} AND DeviceToken = {deviceToken}")
                .FirstOrDefaultAsync(cancellationToken);

            if (existingToken == null)
            {
                // Insert new token
                await _context.Database.ExecuteSqlAsync(
                    $@"INSERT INTO UserDeviceTokens (UserId, DeviceToken, Platform, RegisteredAt, IsActive)
                       VALUES ({userId}, {deviceToken}, {platform}, {DateTime.UtcNow}, 1)",
                    cancellationToken);

                _logger.LogInformation("Device token registered for user {UserId}: {Token}", userId, deviceToken);
            }
            else
            {
                // Update timestamp
                await _context.Database.ExecuteSqlAsync(
                    $@"UPDATE UserDeviceTokens
                       SET RegisteredAt = {DateTime.UtcNow}, IsActive = 1
                       WHERE UserId = {userId} AND DeviceToken = {deviceToken}",
                    cancellationToken);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering device token for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Gets device tokens for a user
    /// </summary>
    private async Task<List<string>> GetUserDeviceTokensAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            return await _context.Database
                .SqlQuery<string>($"SELECT DeviceToken FROM UserDeviceTokens WHERE UserId = {userId} AND IsActive = 1")
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving device tokens for user {UserId}", userId);
            return new List<string>();
        }
    }

    /// <summary>
    /// Builds the Android-specific configuration
    /// </summary>
    private AndroidConfig CreateAndroidConfig(SendPushNotificationRequest request)
    {
        return new AndroidConfig
        {
            Priority = Priority.High,
            Notification = new AndroidNotification
            {
                Title = request.Title,
                Body = request.Body,
                Icon = request.Icon ?? "ic_notification",
                Color = "#FF0000",
                Sound = request.Sound ?? "default",
                ClickAction = request.ClickAction
            }
        };
    }

    /// <summary>
    /// Builds the iOS-specific configuration
    /// </summary>
    private ApnsConfig CreateApnsConfig(SendPushNotificationRequest request)
    {
        var aps = new Aps
        {
            Alert = new ApsAlert
            {
                Title = request.Title,
                Body = request.Body
            },
            Badge = request.Badge,
            Sound = request.Sound ?? "default"
        };

        return new ApnsConfig
        {
            Aps = aps,
            Headers = new Dictionary<string, string>
            {
                ["apns-priority"] = "10"
            }
        };
    }
}

/// <summary>
/// Mock implementation of the Push service for testing
/// </summary>
public class MockPushService : IPushService
{
    private readonly ILogger<MockPushService> _logger;
    private readonly Dictionary<string, List<string>> _userTokens = new();

    public MockPushService(ILogger<MockPushService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<NotificationResponse> SendPushNotificationAsync(SendPushNotificationRequest request, CancellationToken cancellationToken = default)
    {
        var messageId = Guid.NewGuid().ToString();

        _logger.LogInformation("MOCK PUSH sent to token {Token}\nTitle: {Title}\nMessage: {Body}",
            request.DeviceToken, request.Title, request.Body);

        return Task.FromResult(new NotificationResponse
        {
            Success = true,
            MessageId = messageId,
            ExternalId = messageId,
            Status = "sent"
        });
    }

    /// <inheritdoc/>
    public async Task<BulkNotificationResponse> SendBulkPushNotificationAsync(List<SendPushNotificationRequest> requests, CancellationToken cancellationToken = default)
    {
        var response = new BulkNotificationResponse
        {
            TotalRequests = requests.Count,
            SuccessCount = requests.Count,
            FailureCount = 0
        };

        foreach (var request in requests)
        {
            var result = await SendPushNotificationAsync(request, cancellationToken);
            response.Results.Add(new BulkNotificationResult
            {
                Recipient = request.DeviceToken,
                Success = result.Success,
                MessageId = result.MessageId,
                ExternalId = result.ExternalId
            });
        }

        response.Success = true;
        return response;
    }

    /// <inheritdoc/>
    public Task<NotificationResponse> SendToUserAsync(string userId, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default)
    {
        var tokenCount = _userTokens.ContainsKey(userId) ? _userTokens[userId].Count : 0;

        _logger.LogInformation("MOCK PUSH sent to user {UserId} ({TokenCount} devices)\nTitle: {Title}\nMessage: {Body}",
            userId, tokenCount, title, body);

        return Task.FromResult(new NotificationResponse
        {
            Success = true,
            MessageId = $"user_{userId}_{Guid.NewGuid()}",
            Status = "sent"
        });
    }

    /// <inheritdoc/>
    public Task<NotificationResponse> SendToTopicAsync(string topic, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MOCK PUSH sent to topic {Topic}\nTitle: {Title}\nMessage: {Body}",
            topic, title, body);

        return Task.FromResult(new NotificationResponse
        {
            Success = true,
            MessageId = $"topic_{topic}_{Guid.NewGuid()}",
            Status = "sent"
        });
    }

    /// <inheritdoc/>
    public Task<bool> RegisterDeviceTokenAsync(string userId, string deviceToken, string platform, CancellationToken cancellationToken = default)
    {
        if (!_userTokens.ContainsKey(userId))
        {
            _userTokens[userId] = new List<string>();
        }

        if (!_userTokens[userId].Contains(deviceToken))
        {
            _userTokens[userId].Add(deviceToken);
        }

        _logger.LogInformation("MOCK token registered for user {UserId}: {Token} ({Platform})",
            userId, deviceToken, platform);

        return Task.FromResult(true);
    }
}
