# Projects Relationship Graph

[← Back to the assessment index](../assessment.md)

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart LR
    P1["<b>📦&nbsp;OrderService.csproj</b><br/><small>net9.0</small>"]
    P2["<b>📦&nbsp;InventoryService.csproj</b><br/><small>net9.0</small>"]
    P3["<b>📦&nbsp;PaymentService.csproj</b><br/><small>net9.0</small>"]
    P4["<b>📦&nbsp;NotificationService.csproj</b><br/><small>net9.0</small>"]
    P5["<b>📦&nbsp;Shared.csproj</b><br/><small>net9.0</small>"]
    P6["<b>📦&nbsp;ChatbotService.csproj</b><br/><small>net9.0</small>"]
    P7["<b>📦&nbsp;CustomerWebsite.csproj</b><br/><small>net9.0</small>"]
    P8["<b>📦&nbsp;ProductService.csproj</b><br/><small>net9.0</small>"]
    P9["<b>📦&nbsp;GatewayBff.csproj</b><br/><small>net9.0</small>"]
    P10["<b>📦&nbsp;UserService.csproj</b><br/><small>net9.0</small>"]
    P11["<b>📦&nbsp;AppHost.csproj</b><br/><small>net10.0</small>"]
    P1 --> P5
    P2 --> P5
    P4 --> P5
    P6 --> P5
    click P1 "projects/OrderService.md"
    click P2 "projects/InventoryService.md"
    click P3 "projects/PaymentService.md"
    click P4 "projects/NotificationService.md"
    click P5 "projects/Shared.md"
    click P6 "projects/ChatbotService.md"
    click P7 "projects/CustomerWebsite.md"
    click P8 "projects/ProductService.md"
    click P9 "projects/GatewayBff.md"
    click P10 "projects/UserService.md"
    click P11 "projects/AppHost.md"

```

