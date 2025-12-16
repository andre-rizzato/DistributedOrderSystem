namespace ChatbotService.Services.Interfaces;

using ChatbotService.Models;

public interface IFineTuningService
{
    Task<FineTuningResponse> StartFineTuningAsync(FineTuningRequest request, string userId);
    Task<FineTuningProgress?> GetFineTuningProgressAsync(string jobId);
    Task<List<FineTuningProgress>> GetAllFineTuningJobsAsync();
    Task<bool> StopFineTuningAsync(string jobId);
    Task<bool> DeployModelAsync(string jobId);
    Task<List<TrainingExample>> GetTrainingDataAsync(int limit = 100);
    Task<bool> AddTrainingExampleAsync(TrainingExample example);
    Task<bool> ValidateTrainingDataAsync(List<TrainingExample> examples);
    Task<bool> ExportTrainingDataAsync(string filePath);
    Task<bool> ImportTrainingDataAsync(string filePath);
}