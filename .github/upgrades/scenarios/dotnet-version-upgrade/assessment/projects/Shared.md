# src\Shared\Shared.csproj

[← Back to the assessment index](../../assessment.md)

## Project Info

- **Current Target Framework:** net9.0
- **Proposed Target Framework:** net10.0
- **SDK-style**: True
- **Project Kind:** ClassLibrary
- **Dependencies**: 0
- **Dependants**: 4
- **Number of Files**: 2
- **Number of Files with Incidents**: 1
- **Lines of Code**: 88
- **Estimated LOC to modify**: 0+ (at least 0,0% of the project)

## Related Projects

**Depended on by (4)** — projects that reference this one:

- [e:\WorkFiles\Repos\DistributedOrderSystem\src\ChatbotService\ChatbotService.csproj](../projects/ChatbotService.md)
- [e:\WorkFiles\Repos\DistributedOrderSystem\src\InventoryService\InventoryService.csproj](../projects/InventoryService.md)
- [e:\WorkFiles\Repos\DistributedOrderSystem\src\NotificationService\NotificationService.csproj](../projects/NotificationService.md)
- [e:\WorkFiles\Repos\DistributedOrderSystem\src\OrderService\OrderService.csproj](../projects/OrderService.md)

## Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph upstream["Dependants (4)"]
        P1["<b>📦&nbsp;OrderService.csproj</b><br/><small>net9.0</small>"]
        P2["<b>📦&nbsp;InventoryService.csproj</b><br/><small>net9.0</small>"]
        P4["<b>📦&nbsp;NotificationService.csproj</b><br/><small>net9.0</small>"]
        P6["<b>📦&nbsp;ChatbotService.csproj</b><br/><small>net9.0</small>"]
        click P1 "../projects/OrderService.md"
        click P2 "../projects/InventoryService.md"
        click P4 "../projects/NotificationService.md"
        click P6 "../projects/ChatbotService.md"
    end
    subgraph current["Shared.csproj"]
        MAIN["<b>📦&nbsp;Shared.csproj</b><br/><small>net9.0</small>"]
        click MAIN "../projects/Shared.md"
    end
    P1 --> MAIN
    P2 --> MAIN
    P4 --> MAIN
    P6 --> MAIN

```

## API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 63 |  |
| ***Total APIs Analyzed*** | ***63*** |  |

