using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Models;
using NotificationService.Services;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NotificationService.Services.Implementations;

/// <summary>
/// Implementazione del servizio per la gestione dei template di notifica
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
            _logger.LogError(ex, "Errore durante il recupero del template {TemplateName}", templateName);
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
            _logger.LogError(ex, "Errore durante il recupero dei template per tipo {Type}", type);
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
                throw new InvalidOperationException($"Template '{templateName}' non trovato");
            }

            // Renderizza subject
            var subject = ReplaceVariables(template.SubjectTemplate, variables);
            
            // Renderizza content
            var content = ReplaceVariables(template.ContentTemplate, variables);
            
            // Renderizza HTML content se presente
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
            _logger.LogError(ex, "Errore durante il rendering del template {TemplateName}", templateName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<NotificationTemplate> CreateTemplateAsync(NotificationTemplate template, CancellationToken cancellationToken = default)
    {
        try
        {
            // Verifica che non esista già un template con lo stesso nome
            var existingTemplate = await _context.NotificationTemplates
                .FirstOrDefaultAsync(t => t.Name == template.Name, cancellationToken);

            if (existingTemplate != null)
            {
                throw new InvalidOperationException($"Template '{template.Name}' esiste già");
            }

            // Valida variabili
            var validationResult = ValidateTemplateVariables(template);
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException($"Template non valido: {string.Join(", ", validationResult.Errors)}");
            }

            template.CreatedAt = DateTime.UtcNow;
            template.UpdatedAt = DateTime.UtcNow;

            _context.NotificationTemplates.Add(template);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Template creato: {TemplateName}", template.Name);
            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la creazione del template {TemplateName}", template.Name);
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

            // Valida variabili
            var validationResult = ValidateTemplateVariables(template);
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException($"Template non valido: {string.Join(", ", validationResult.Errors)}");
            }

            // Aggiorna campi
            existingTemplate.Description = template.Description;
            existingTemplate.Type = template.Type;
            existingTemplate.SubjectTemplate = template.SubjectTemplate;
            existingTemplate.ContentTemplate = template.ContentTemplate;
            existingTemplate.HtmlTemplate = template.HtmlTemplate;
            existingTemplate.Variables = template.Variables;
            existingTemplate.IsActive = template.IsActive;
            existingTemplate.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Template aggiornato: {TemplateName}", templateName);
            return existingTemplate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'aggiornamento del template {TemplateName}", templateName);
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

            _logger.LogInformation("Template eliminato: {TemplateName}", templateName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'eliminazione del template {TemplateName}", templateName);
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
                    Errors = { $"Template '{templateName}' non trovato" }
                };
            }

            return ValidateVariablesAgainstTemplate(template, variables);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la validazione delle variabili per template {TemplateName}", templateName);
            return new ValidationResult
            {
                IsValid = false,
                Errors = { $"Errore durante la validazione: {ex.Message}" }
            };
        }
    }

    /// <summary>
    /// Sostituisce le variabili nel testo del template
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
    /// Valida un template durante creazione/aggiornamento
    /// </summary>
    private static ValidationResult ValidateTemplateVariables(NotificationTemplate template)
    {
        var result = new ValidationResult { IsValid = true };

        try
        {
            // Verifica che i template non siano vuoti
            if (string.IsNullOrWhiteSpace(template.SubjectTemplate))
            {
                result.Errors.Add("Subject template non può essere vuoto");
            }

            if (string.IsNullOrWhiteSpace(template.ContentTemplate))
            {
                result.Errors.Add("Content template non può essere vuoto");
            }

            // Estrai variabili dai template
            var subjectVariables = ExtractVariables(template.SubjectTemplate);
            var contentVariables = ExtractVariables(template.ContentTemplate);
            var htmlVariables = ExtractVariables(template.HtmlTemplate ?? string.Empty);

            var allVariables = subjectVariables
                .Union(contentVariables)
                .Union(htmlVariables)
                .Distinct()
                .ToList();

            // Valida formato JSON delle variabili definite
            if (!string.IsNullOrEmpty(template.Variables))
            {
                try
                {
                    var definedVariables = JsonSerializer.Deserialize<Dictionary<string, string>>(template.Variables);
                    
                    // Verifica che tutte le variabili usate siano definite
                    var missingVariables = allVariables.Where(v => !definedVariables!.ContainsKey(v)).ToList();
                    if (missingVariables.Any())
                    {
                        result.MissingVariables.AddRange(missingVariables);
                        result.Errors.Add($"Variabili non definite: {string.Join(", ", missingVariables)}");
                    }
                }
                catch (JsonException)
                {
                    result.Errors.Add("Formato JSON non valido per le variabili");
                }
            }
            else if (allVariables.Any())
            {
                result.Errors.Add("Template contiene variabili ma nessuna definizione JSON fornita");
            }

            result.IsValid = !result.Errors.Any();
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"Errore durante la validazione: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Valida le variabili fornite contro un template
    /// </summary>
    private static ValidationResult ValidateVariablesAgainstTemplate(NotificationTemplate template, Dictionary<string, string> variables)
    {
        var result = new ValidationResult { IsValid = true };

        try
        {
            // Estrai variabili richieste dal template
            var subjectVariables = ExtractVariables(template.SubjectTemplate);
            var contentVariables = ExtractVariables(template.ContentTemplate);
            var htmlVariables = ExtractVariables(template.HtmlTemplate ?? string.Empty);

            var requiredVariables = subjectVariables
                .Union(contentVariables)
                .Union(htmlVariables)
                .Distinct()
                .ToList();

            // Verifica variabili mancanti
            var missingVariables = requiredVariables.Where(v => !variables.ContainsKey(v)).ToList();
            if (missingVariables.Any())
            {
                result.MissingVariables.AddRange(missingVariables);
                result.Errors.Add($"Variabili mancanti: {string.Join(", ", missingVariables)}");
            }

            result.IsValid = !result.Errors.Any();
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"Errore durante la validazione: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Estrae le variabili da un testo template usando regex
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