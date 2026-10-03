# Projects and dependencies analysis

This document provides a comprehensive overview of the projects and their dependencies in the context of upgrading to .NETCoreApp,Version=v10.0.

Detailed findings live alongside this file in `assessment/`. This page is the index: read it first, then open only the documents you need.

## Table of Contents

- [Executive Summary](#executive-summary)
  - [Highlevel Metrics](#highlevel-metrics)
  - [Projects Compatibility](#projects-compatibility)
  - [Package Compatibility](#package-compatibility)
  - [API Compatibility](#api-compatibility)
- [Top API Migration Challenges](#top-api-migration-challenges)
  - [Technologies and Features](#technologies-and-features)
  - [Most Frequent API Issues](#most-frequent-api-issues)
- [Detailed Reports](#detailed-reports)
  - [Projects Relationship Graph](assessment/project-graph.md)
  - [Aggregate NuGet packages details](assessment/nuget/aggregate-packages.md)
  - [Most Frequent API Issues (complete list)](assessment/api-issues/most-frequent-api-issues.md)
  - [Project Details](#project-details)

## Executive Summary

### Highlevel Metrics

| Metric | Count | Status |
| :--- | :---: | :--- |
| Total Projects | 11 | 10 require upgrade |
| Total NuGet Packages | 306 | 20 need upgrade |
| Total Code Files | 218 |  |
| Total Code Files with Incidents | 31 |  |
| Total Lines of Code | 25725 |  |
| Total Number of Issues | 182 |  |
| Proposed Target Framework | net10.0 |  |
| Estimated LOC to modify | 129+ | at least 0,5% of codebase |

### Projects Compatibility

| Project | Target Framework | Difficulty | Test Coverage | Package Issues | API Issues | Binding Issues | Est. LOC Impact | Description |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :--- |
| [src\AppHost\AppHost.csproj](assessment/projects/AppHost.md) | net10.0 | ✅ None | — | 0 | 0 | 0 |  | DotNetCoreApp, Sdk Style = True |
| [src\ChatbotService\ChatbotService.csproj](assessment/projects/ChatbotService.md) | net9.0 | 🟢 Low | 🧪 Recommended | 7 | 27 | 0 | 27+ | AspNetCore, Sdk Style = True |
| [src\frontend\customer-facing e-commerce\CustomerWebsite\CustomerWebsite.csproj](assessment/projects/CustomerWebsite.md) | net9.0 | 🟢 Low | 🧪 Recommended | 11 | 22 | 0 | 22+ | AspNetCore, Sdk Style = True |
| [src\GatewayBff\GatewayBff.csproj](assessment/projects/GatewayBff.md) | net9.0 | 🟢 Low | 🧪 Recommended | 1 | 35 | 0 | 35+ | AspNetCore, Sdk Style = True |
| [src\InventoryService\InventoryService.csproj](assessment/projects/InventoryService.md) | net9.0 | 🟢 Low | 🧪 Recommended | 3 | 2 | 0 | 2+ | AspNetCore, Sdk Style = True |
| [src\NotificationService\NotificationService.csproj](assessment/projects/NotificationService.md) | net9.0 | 🟢 Low | 🧪 Recommended | 9 | 8 | 0 | 8+ | AspNetCore, Sdk Style = True |
| [src\OrderService\OrderService.csproj](assessment/projects/OrderService.md) | net9.0 | 🟢 Low | 🧪 Recommended | 3 | 1 | 0 | 1+ | AspNetCore, Sdk Style = True |
| [src\PaymentService\PaymentService.csproj](assessment/projects/PaymentService.md) | net9.0 | 🟢 Low | — | 1 | 0 | 0 |  | AspNetCore, Sdk Style = True |
| [src\ProductService\ProductService.csproj](assessment/projects/ProductService.md) | net9.0 | 🟢 Low | 🧪 Recommended | 3 | 2 | 0 | 2+ | AspNetCore, Sdk Style = True |
| [src\Shared\Shared.csproj](assessment/projects/Shared.md) | net9.0 | 🟢 Low | — | 0 | 0 | 0 |  | ClassLibrary, Sdk Style = True |
| [src\UserService\UserService.csproj](assessment/projects/UserService.md) | net9.0 | 🟢 Low | 🧪 Recommended | 5 | 32 | 0 | 32+ | AspNetCore, Sdk Style = True |

🧪 **Test Coverage** — projects risky enough to add behavior-locking tests before upgrading, to catch regressions the upgrade may introduce. Requires the **dotnet-test** plugin.

### Package Compatibility

| Status | Count | Percentage |
| :--- | :---: | :---: |
| ✅ Compatible | 286 | 93,5% |
| ⚠️ Incompatible | 2 | 0,7% |
| 🔄 Upgrade Recommended | 18 | 5,9% |
| ***Total NuGet Packages*** | ***306*** | ***100%*** |

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 45 | High - Require code changes |
| 🟡 Source Incompatible | 20 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 64 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 32252 |  |
| ***Total APIs Analyzed*** | ***32381*** |  |

## Top API Migration Challenges

### Technologies and Features

| Technology | Issues | Percentage | Migration Path |
| :--- | :---: | :---: | :--- |
| IdentityModel & Claims-based Security | 18 | 14,0% | Windows Identity Foundation (WIF), SAML, and claims-based authentication APIs that have been replaced by modern identity libraries. WIF was the original identity framework for .NET Framework. Migrate to Microsoft.IdentityModel.* packages (modern identity stack). |

### Most Frequent API Issues

| API | Count | Percentage | Category |
| :--- | :---: | :---: | :--- |
| T:System.Net.Http.HttpContent | 30 | 23,3% | Behavioral Change |
| T:System.Uri | 22 | 17,1% | Behavioral Change |
| M:Microsoft.Extensions.DependencyInjection.OptionsConfigurationServiceCollectionExtensions.Configure''1(Microsoft.Extensions.DependencyInjection.IServiceCollection,Microsoft.Extensions.Configuration.IConfiguration) | 14 | 10,9% | Binary Incompatible |
| M:Microsoft.Extensions.Configuration.ConfigurationBinder.GetValue''1(Microsoft.Extensions.Configuration.IConfiguration,System.String) | 10 | 7,8% | Binary Incompatible |
| M:System.Uri.#ctor(System.String) | 6 | 4,7% | Behavioral Change |
| F:Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme | 3 | 2,3% | Source Incompatible |
| M:Microsoft.Extensions.Logging.ConsoleLoggerExtensions.AddConsole(Microsoft.Extensions.Logging.ILoggingBuilder) | 3 | 2,3% | Behavioral Change |
| M:System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.#ctor | 3 | 2,3% | Binary Incompatible |
| T:Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults | 3 | 2,3% | Source Incompatible |
| T:System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames | 3 | 2,3% | Binary Incompatible |

The table above is the top 10. See [the complete list](assessment/api-issues/most-frequent-api-issues.md) for every affected API.

## Detailed Reports

- [Projects Relationship Graph](assessment/project-graph.md)
- [Aggregate NuGet packages details](assessment/nuget/aggregate-packages.md)
- [Most Frequent API Issues (complete list)](assessment/api-issues/most-frequent-api-issues.md)

### Project Details

- [src\AppHost\AppHost.csproj](assessment/projects/AppHost.md)
- [src\ChatbotService\ChatbotService.csproj](assessment/projects/ChatbotService.md)
- [src\frontend\customer-facing e-commerce\CustomerWebsite\CustomerWebsite.csproj](assessment/projects/CustomerWebsite.md)
- [src\GatewayBff\GatewayBff.csproj](assessment/projects/GatewayBff.md)
- [src\InventoryService\InventoryService.csproj](assessment/projects/InventoryService.md)
- [src\NotificationService\NotificationService.csproj](assessment/projects/NotificationService.md)
- [src\OrderService\OrderService.csproj](assessment/projects/OrderService.md)
- [src\PaymentService\PaymentService.csproj](assessment/projects/PaymentService.md)
- [src\ProductService\ProductService.csproj](assessment/projects/ProductService.md)
- [src\Shared\Shared.csproj](assessment/projects/Shared.md)
- [src\UserService\UserService.csproj](assessment/projects/UserService.md)


