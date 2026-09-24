using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Models;
using NotificationService.Models.Requests;
using NotificationService.Models.Responses;
using NotificationService.Services;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NotificationService.Services.Implementations;

/// <summary>
/// Implementation of the notification template management service
/// </summary>
public class NotificationTemplateService : INotificationTemplateService
{
    private readonly NotificationContext _context;
    private readonly ILogger<NotificationTemplateService> _logger;

    public NotificationTemplateService(
        NotificationContext context,
        ILogger<NotificationTemplateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<NotificationTemplate?> GetTemplateAsync(string templateName, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.NotificationTemplates
                .FirstOrDefaultAsync(t => t.Name == templateName && t.IsActive, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template {TemplateName}", templateName);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<List<NotificationTemplate>> GetTemplatesByTypeAsync(NotificationType type, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.NotificationTemplates
                .Where(t => t.Type == type && t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving templates for type {Type}", type);
            return new List<NotificationTemplate>();
        }
    }

    /// <inheritdoc/>
    public async Task<RenderedTemplate> RenderTemplateAsync(string templateName, Dictionary<string, string> variables, CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await GetTemplateAsync(templateName, cancellationToken);
            if (template == null)
            {
                throw new InvalidOperationException($"Template '{templateName}' not found");
            }

            // Render subject
            var subject = ReplaceVariables(template.SubjectTemplate, variables);

            // Render content
            var content = ReplaceVariables(template.ContentTemplate, variables);

            // Render HTML content if present
            string? htmlContent = null;
            if (!string.IsNullOrEmpty(template.HtmlTemplate))
            {
                htmlContent = ReplaceVariables(template.HtmlTemplate, variables);
            }

            return new RenderedTemplate
            {
                Subject = subject,
                Content = content,
                HtmlContent = htmlContent
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering template {TemplateName}", templateName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<NotificationTemplate> CreateTemplateAsync(NotificationTemplate template, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check that a template with the same name doesn't already exist
            var existingTemplate = await _context.NotificationTemplates
                .FirstOrDefaultAsync(t => t.Name == template.Name, cancellationToken);

            if (existingTemplate != null)
            {
                throw new InvalidOperationException($"Template '{template.Name}' already exists");
            }

            // Validate variables
            var validationResult = ValidateTemplateVariables(template);
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException($"Invalid template: {string.Join(", ", validationResult.Errors)}");
            }

            template.CreatedAt = DateTime.UtcNow;
            template.UpdatedAt = DateTime.UtcNow;

            _context.NotificationTemplates.Add(template);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Template created: {TemplateName}", template.Name);
            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template {TemplateName}", template.Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<NotificationTemplate?> UpdateTemplateAsync(string templateName, NotificationTemplate template, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingTemplate = await _context.NotificationTemplates
                .FirstOrDefaultAsync(t => t.Name == templateName, cancellationToken);

            if (existingTemplate == null)
            {
                return null;
            }

            // Validate variables
            var validationResult = ValidateTemplateVariables(template);
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException($"Invalid template: {string.Join(", ", validationResult.Errors)}");
            }

            // Update fields
            existingTemplate.Description = template.Description;
            existingTemplate.Type = template.Type;
            existingTemplate.SubjectTemplate = template.SubjectTemplate;
            existingTemplate.ContentTemplate = template.ContentTemplate;
            existingTemplate.HtmlTemplate = template.HtmlTemplate;
            existingTemplate.Variables = template.Variables;
            existingTemplate.IsActive = template.IsActive;
            existingTemplate.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Template updated: {TemplateName}", templateName);
            return existingTemplate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template {TemplateName}", templateName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteTemplateAsync(string templateName, CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await _context.NotificationTemplates
                .FirstOrDefaultAsync(t => t.Name == templateName, cancellationToken);

            if (template == null)
            {
                return false;
            }

            _context.NotificationTemplates.Remove(template);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Template deleted: {TemplateName}", templateName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template {TemplateName}", templateName);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateTemplateVariablesAsync(string templateName, Dictionary<string, string> variables)
    {
        try
        {
            var template = await GetTemplateAsync(templateName);
            if (template == null)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Errors = { $"Template '{templateName}' not found" }
                };
            }

            return ValidateVariablesAgainstTemplate(template, variables);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating variables for template {TemplateName}", templateName);
            return new ValidationResult
            {
                IsValid = false,
                Errors = { $"Error during validation: {ex.Message}" }
            };
        }
    }

    /// <summary>
    /// Replaces variables in the template text
    /// </summary>
    private static string ReplaceVariables(string template, Dictionary<string, string> variables)
    {
        var result = template;

        foreach (var variable in variables)
        {
            var placeholder = "{" + variable.Key + "}";
            result = result.Replace(placeholder, variable.Value ?? string.Empty);
        }

        return result;
    }

    /// <summary>
    /// Validates a template during creation/update
    /// </summary>
    private static ValidationResult ValidateTemplateVariables(NotificationTemplate template)
    {
        var result = new ValidationResult { IsValid = true };

        try
        {
            // Check that the templates aren't empty
            if (string.IsNullOrWhiteSpace(template.SubjectTemplate))
            {
                result.Errors.Add("Subject template cannot be empty");
            }

            if (string.IsNullOrWhiteSpace(template.ContentTemplate))
            {
                result.Errors.Add("Content template cannot be empty");
            }

            // Extract variables from the templates
            var subjectVariables = ExtractVariables(template.SubjectTemplate);
            var contentVariables = ExtractVariables(template.ContentTemplate);
            var htmlVariables = ExtractVariables(template.HtmlTemplate ?? string.Empty);

            var allVariables = subjectVariables
                .Union(contentVariables)
                .Union(htmlVariables)
                .Distinct()
                .ToList();

            // Validate the JSON format of the defined variables
            if (!string.IsNullOrEmpty(template.Variables))
            {
                try
                {
                    var definedVariables = JsonSerializer.Deserialize<Dictionary<string, string>>(template.Variables);

                    // Check that all used variables are defined
                    var missingVariables = allVariables.Where(v => !definedVariables!.ContainsKey(v)).ToList();
                    if (missingVariables.Any())
                    {
                        result.MissingVariables.AddRange(missingVariables);
                        result.Errors.Add($"Undefined variables: {string.Join(", ", missingVariables)}");
                    }
                }
                catch (JsonException)
                {
                    result.Errors.Add("Invalid JSON format for variables");
                }
            }
            else if (allVariables.Any())
            {
                result.Errors.Add("Template contains variables but no JSON definition was provided");
            }

            result.IsValid = !result.Errors.Any();
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"Error during validation: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Validates the provided variables against a template
    /// </summary>
    private static ValidationResult ValidateVariablesAgainstTemplate(NotificationTemplate template, Dictionary<string, string> variables)
    {
        var result = new ValidationResult { IsValid = true };

        try
        {
            // Extract variables required by the template
            var subjectVariables = ExtractVariables(template.SubjectTemplate);
            var contentVariables = ExtractVariables(template.ContentTemplate);
            var htmlVariables = ExtractVariables(template.HtmlTemplate ?? string.Empty);

            var requiredVariables = subjectVariables
                .Union(contentVariables)
                .Union(htmlVariables)
                .Distinct()
                .ToList();

            // Check for missing variables
            var missingVariables = requiredVariables.Where(v => !variables.ContainsKey(v)).ToList();
            if (missingVariables.Any())
            {
                result.MissingVariables.AddRange(missingVariables);
                result.Errors.Add($"Missing variables: {string.Join(", ", missingVariables)}");
            }

            result.IsValid = !result.Errors.Any();
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"Error during validation: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Extracts variables from a template text using regex
    /// </summary>
    private static List<string> ExtractVariables(string template)
    {
        if (string.IsNullOrEmpty(template))
            return new List<string>();

        var regex = new Regex(@"\{([^}]+)\}", RegexOptions.Compiled);
        var matches = regex.Matches(template);

        return matches.Cast<Match>()
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();
    }
}
