# src\NotificationService\NotificationService.csproj

[← Back to the assessment index](../../assessment.md)

## Project Info

- **Current Target Framework:** net9.0
- **Proposed Target Framework:** net10.0
- **SDK-style**: True
- **Project Kind:** AspNetCore
- **Dependencies**: 1
- **Dependants**: 0
- **Number of Files**: 21
- **Number of Files with Incidents**: 2
- **Lines of Code**: 7300
- **Estimated LOC to modify**: 8+ (at least 0,1% of the project)

## Related Projects

**Depends on (1)** — projects this one references:

- [e:\WorkFiles\Repos\DistributedOrderSystem\src\Shared\Shared.csproj](../projects/Shared.md)

## Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph current["NotificationService.csproj"]
        MAIN["<b>📦&nbsp;NotificationService.csproj</b><br/><small>net9.0</small>"]
        click MAIN "../projects/NotificationService.md"
    end
    subgraph downstream["Dependencies (1)"]
        P5["<b>📦&nbsp;Shared.csproj</b><br/><small>net9.0</small>"]
        click P5 "../projects/Shared.md"
    end
    MAIN --> P5

```

## API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 6 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 2 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 7822 |  |
| ***Total APIs Analyzed*** | ***7830*** |  |

## NuGet Package Issues

| Package | Current Version | Suggested Version | Severity | Issue |
| :--- | :---: | :---: | :---: | :--- |
| FluentValidation.AspNetCore | 11.3.0 | — | 🔵 Optional | NuGet package is deprecated |
| MailKit | 4.7.1.1 | 4.18.1 | 🔵 Optional | NuGet package contains security vulnerability |
| Microsoft.AspNetCore.OpenApi | 9.0.0 | 10.0.12 | 🟡 Potential | NuGet package upgrade is recommended |
| Microsoft.EntityFrameworkCore | 9.0.0 | 10.0.12 | 🟡 Potential | NuGet package upgrade is recommended |
| Microsoft.EntityFrameworkCore.Design | 9.0.0 | 10.0.12 | 🟡 Potential | NuGet package upgrade is recommended |
| Microsoft.EntityFrameworkCore.Tools | 9.0.0 | 10.0.12 | 🟡 Potential | NuGet package upgrade is recommended |
| Microsoft.Extensions.Logging.Console | 9.0.0 | 10.0.12 | 🟡 Potential | NuGet package upgrade is recommended |
| MimeKit | 4.7.1 | 4.18.1 | 🔵 Optional | NuGet package contains security vulnerability |
| Newtonsoft.Json | 13.0.3 | 13.0.4 | 🟡 Potential | NuGet package upgrade is recommended |

Every project affected by these packages, and the versions the repository settles on: [aggregate NuGet packages](../nuget/aggregate-packages.md).

