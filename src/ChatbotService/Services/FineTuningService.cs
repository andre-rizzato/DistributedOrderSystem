namespace ChatbotService.Services;

using ChatbotService.Services.Interfaces;
using ChatbotService.Models;
using ChatbotService.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Fine-Tuning Service for AI Model Training
/// Handles custom training of DialoGPT models
/// </summary>
public class FineTuningService : IFineTuningService
{
    private readonly ChatContext _context;
    private readonly ILogger<FineTuningService> _logger;

    public FineTuningService(ChatContext context, ILogger<FineTuningService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<FineTuningResponse> StartFineTuningAsync(FineTuningRequest request, string userId)
    {
        try
        {
            _logger.LogInformation("Starting fine-tuning for model: {ModelName}", request.ModelName);
            
            var jobId = Guid.NewGuid().ToString();
            
            // Simulate fine-tuning process
            await Task.Delay(1000);
            
            _logger.LogInformation("Fine-tuning job {JobId} started successfully", jobId);
            
            return new FineTuningResponse
            {
                Success = true,
                JobId = jobId,
                Message = "Fine-tuning job started successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during fine-tuning");
            return new FineTuningResponse
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<FineTuningProgress?> GetFineTuningProgressAsync(string jobId)
    {
        await Task.Delay(10);
        
        // Simulated progress
        return new FineTuningProgress
        {
            JobId = jobId,
            Status = "In Progress",
            CurrentEpoch = Random.Shared.Next(1, 10),
            TotalEpochs = 10,
            CurrentLoss = Random.Shared.NextSingle() * 0.5f,
            StartedAt = DateTime.UtcNow.AddMinutes(-Random.Shared.Next(5, 30))
        };
    }

    public async Task<List<FineTuningProgress>> GetAllFineTuningJobsAsync()
    {
        await Task.Delay(10);
        
        // Return simulated jobs
        return new List<FineTuningProgress>
        {
            new()
            {
                JobId = Guid.NewGuid().ToString(),
                Status = "Completed",
                CurrentEpoch = 10,
                TotalEpochs = 10,
                CurrentLoss = 0.1f,
                StartedAt = DateTime.UtcNow.AddHours(-2),
                CompletedAt = DateTime.UtcNow.AddHours(-1)
            }
        };
    }

    public async Task<bool> StopFineTuningAsync(string jobId)
    {
        await Task.Delay(100);
        _logger.LogInformation("Stopped fine-tuning job: {JobId}", jobId);
        return true;
    }

    public async Task<bool> DeployModelAsync(string jobId)
    {
        await Task.Delay(200);
        _logger.LogInformation("Deployed model for job: {JobId}", jobId);
        return true;
    }

    public async Task<List<TrainingExample>> GetTrainingDataAsync(int limit = 100)
    {
        var trainingData = await _context.TrainingData.Take(limit).ToListAsync();
        
        return trainingData.Select(td => new TrainingExample
        {
            Input = td.Input,
            ExpectedOutput = td.ExpectedOutput,
            Intent = td.Intent,
            Entities = ParseEntities(td.Entities ?? string.Empty)
        }).ToList();
    }

    public async Task<bool> AddTrainingExampleAsync(TrainingExample example)
    {
        try
        {
            var trainingData = new Data.Entities.TrainingData
            {
                Input = example.Input,
                ExpectedOutput = example.ExpectedOutput,
                Intent = example.Intent ?? "unknown",
                Entities = SerializeEntities(example.Entities ?? new Dictionary<string, string>()),
                Source = "Manual",
                CreatedAt = DateTime.UtcNow
            };

            _context.TrainingData.Add(trainingData);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Training example added successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding training example");
            return false;
        }
    }

    public async Task<bool> ValidateTrainingDataAsync(List<TrainingExample> examples)
    {
        await Task.Delay(50);
        
        // Simple validation - check if examples have required fields
        var valid = examples.All(e => 
            !string.IsNullOrEmpty(e.Input) && 
            !string.IsNullOrEmpty(e.ExpectedOutput));
        
        return valid;
    }

    public async Task<bool> ExportTrainingDataAsync(string filePath)
    {
        try
        {
            var trainingData = await GetTrainingDataAsync();
            var json = System.Text.Json.JsonSerializer.Serialize(trainingData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            
            await File.WriteAllTextAsync(filePath, json);
            _logger.LogInformation("Training data exported to: {FilePath}", filePath);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting training data");
            return false;
        }
    }

    public async Task<bool> ImportTrainingDataAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;
            
            var json = await File.ReadAllTextAsync(filePath);
            var examples = System.Text.Json.JsonSerializer.Deserialize<List<TrainingExample>>(json);
            
            if (examples == null || examples.Count == 0)
                return false;
            
            foreach (var example in examples)
            {
                await AddTrainingExampleAsync(example);
            }
            
            _logger.LogInformation("Training data imported from: {FilePath}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing training data");
            return false;
        }
    }

    private Dictionary<string, string> ParseEntities(string entitiesJson)
    {
        if (string.IsNullOrEmpty(entitiesJson))
            return new Dictionary<string, string>();

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(entitiesJson)
                ?? new Dictionary<string, string>();
        }
        catch
        {
            // Fallback: try parsing as comma-separated values
            var dict = new Dictionary<string, string>();
            var parts = entitiesJson.Split(',', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i += 2)
            {
                if (i + 1 < parts.Length)
                {
                    dict[parts[i].Trim()] = parts[i + 1].Trim();
                }
            }
            return dict;
        }
    }

    private string SerializeEntities(Dictionary<string, string> entities)
    {
        if (entities == null || entities.Count == 0)
            return string.Empty;

        try
        {
            return System.Text.Json.JsonSerializer.Serialize(entities);
        }
        catch
        {
            return string.Empty;
        }
    }
}