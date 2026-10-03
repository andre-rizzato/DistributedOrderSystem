# CPM Conversion Report

- Scope: 12 SDK-style projects (11 in the solution and standalone CustomerService).
- Central entries: 42; direct PackageReferences versionless: 89.
- Baseline and post-conversion clean builds succeeded for the solution and CustomerService.
- Package comparison: 90 entries before, 90 after.
- Added packages: 0; removed packages: 0; VersionOverride entries: 0.

## Changes

| Package | Project | Before | After | Status |
| --- | --- | --- | --- | --- |
| StackExchange.Redis | E:/WorkFiles/Repos/DistributedOrderSystem/src/ChatbotService/ChatbotService.csproj | 2.8.16 | 2.10.1 | User-approved alignment |
| StackExchange.Redis | E:/WorkFiles/Repos/DistributedOrderSystem/src/CustomerService/CustomerService.csproj | 2.9.32 | 2.10.1 | User-approved alignment |
| Swashbuckle.AspNetCore | E:/WorkFiles/Repos/DistributedOrderSystem/src/InventoryService/InventoryService.csproj | 6.8.1 | 7.2.0 | User-approved alignment |
| StackExchange.Redis | E:/WorkFiles/Repos/DistributedOrderSystem/src/NotificationService/NotificationService.csproj | 2.8.16 | 2.10.1 | User-approved alignment |
| Swashbuckle.AspNetCore | E:/WorkFiles/Repos/DistributedOrderSystem/src/NotificationService/NotificationService.csproj | 6.8.0 | 7.2.0 | User-approved alignment |
| Swashbuckle.AspNetCore | E:/WorkFiles/Repos/DistributedOrderSystem/src/OrderService/OrderService.csproj | 6.8.1 | 7.2.0 | User-approved alignment |
| StackExchange.Redis | E:/WorkFiles/Repos/DistributedOrderSystem/src/ProductService/ProductService.csproj | 2.9.32 | 2.10.1 | User-approved alignment |
| StackExchange.Redis | E:/WorkFiles/Repos/DistributedOrderSystem/src/UserService/UserService.csproj | 2.9.32 | 2.10.1 | User-approved alignment |

## Unchanged

| Package | Project | Framework | Resolved version | Status |
| --- | --- | --- | --- | --- |
| Aspire.Hosting.AppHost | E:/WorkFiles/Repos/DistributedOrderSystem/src/AppHost/AppHost.csproj | net10.0 | 13.5.3 | Unchanged from baseline |
| Microsoft.AspNetCore.Authentication.JwtBearer | E:/WorkFiles/Repos/DistributedOrderSystem/src/ChatbotService/ChatbotService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Microsoft.AspNetCore.OpenApi | E:/WorkFiles/Repos/DistributedOrderSystem/src/ChatbotService/ChatbotService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Microsoft.EntityFrameworkCore | E:/WorkFiles/Repos/DistributedOrderSystem/src/ChatbotService/ChatbotService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Microsoft.EntityFrameworkCore.Tools | E:/WorkFiles/Repos/DistributedOrderSystem/src/ChatbotService/ChatbotService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Microsoft.Extensions.Http | E:/WorkFiles/Repos/DistributedOrderSystem/src/ChatbotService/ChatbotService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Microsoft.ML | E:/WorkFiles/Repos/DistributedOrderSystem/src/ChatbotService/ChatbotService.csproj | net9.0 | 4.0.0 | Unchanged from baseline |
| Microsoft.ML.OnnxRuntime | E:/WorkFiles/Repos/DistributedOrderSystem/src/ChatbotService/ChatbotService.csproj | net9.0 | 1.19.2 | Unchanged from baseline |
| Microsoft.ML.OnnxRuntime.Gpu | E:/WorkFiles/Repos/DistributedOrderSystem/src/ChatbotService/ChatbotService.csproj | net9.0 | 1.19.2 | Unchanged from baseline |
| Microsoft.ML.TensorFlow | E:/WorkFiles/Repos/DistributedOrderSystem/src/ChatbotService/ChatbotService.csproj | net9.0 | 4.0.0 | Unchanged from baseline |
| Microsoft.ML.Tokenizers | E:/WorkFiles/Repos/DistributedOrderSystem/src/ChatbotService/ChatbotService.csproj | net9.0 | 0.22.0-preview.24378.1 | Unchanged from baseline |
| Newtonsoft.Json | E:/WorkFiles/Repos/DistributedOrderSystem/src/ChatbotService/ChatbotService.csproj | net9.0 | 13.0.3 | Unchanged from baseline |
| Npgsql.EntityFrameworkCore.PostgreSQL | E:/WorkFiles/Repos/DistributedOrderSystem/src/ChatbotService/ChatbotService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Scalar.AspNetCore | E:/WorkFiles/Repos/DistributedOrderSystem/src/ChatbotService/ChatbotService.csproj | net9.0 | 2.11.6 | Unchanged from baseline |
| System.IdentityModel.Tokens.Jwt | E:/WorkFiles/Repos/DistributedOrderSystem/src/ChatbotService/ChatbotService.csproj | net9.0 | 8.2.0 | Unchanged from baseline |
| System.Text.Json | E:/WorkFiles/Repos/DistributedOrderSystem/src/ChatbotService/ChatbotService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Microsoft.AspNetCore.OpenApi | E:/WorkFiles/Repos/DistributedOrderSystem/src/CustomerService/CustomerService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Scalar.AspNetCore | E:/WorkFiles/Repos/DistributedOrderSystem/src/CustomerService/CustomerService.csproj | net9.0 | 2.11.6 | Unchanged from baseline |
| AutoMapper.Extensions.Microsoft.DependencyInjection | E:/WorkFiles/Repos/DistributedOrderSystem/src/frontend/customer-facing e-commerce/CustomerWebsite/CustomerWebsite.csproj | net9.0 | 12.0.1 | Unchanged from baseline |
| FluentValidation.AspNetCore | E:/WorkFiles/Repos/DistributedOrderSystem/src/frontend/customer-facing e-commerce/CustomerWebsite/CustomerWebsite.csproj | net9.0 | 11.3.0 | Unchanged from baseline |
| Microsoft.AspNetCore.Authentication.JwtBearer | E:/WorkFiles/Repos/DistributedOrderSystem/src/frontend/customer-facing e-commerce/CustomerWebsite/CustomerWebsite.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Microsoft.AspNetCore.SignalR.Client | E:/WorkFiles/Repos/DistributedOrderSystem/src/frontend/customer-facing e-commerce/CustomerWebsite/CustomerWebsite.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Microsoft.EntityFrameworkCore | E:/WorkFiles/Repos/DistributedOrderSystem/src/frontend/customer-facing e-commerce/CustomerWebsite/CustomerWebsite.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Microsoft.EntityFrameworkCore.SqlServer | E:/WorkFiles/Repos/DistributedOrderSystem/src/frontend/customer-facing e-commerce/CustomerWebsite/CustomerWebsite.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Microsoft.EntityFrameworkCore.Tools | E:/WorkFiles/Repos/DistributedOrderSystem/src/frontend/customer-facing e-commerce/CustomerWebsite/CustomerWebsite.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Microsoft.Extensions.Caching.StackExchangeRedis | E:/WorkFiles/Repos/DistributedOrderSystem/src/frontend/customer-facing e-commerce/CustomerWebsite/CustomerWebsite.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Microsoft.Extensions.Http | E:/WorkFiles/Repos/DistributedOrderSystem/src/frontend/customer-facing e-commerce/CustomerWebsite/CustomerWebsite.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Newtonsoft.Json | E:/WorkFiles/Repos/DistributedOrderSystem/src/frontend/customer-facing e-commerce/CustomerWebsite/CustomerWebsite.csproj | net9.0 | 13.0.3 | Unchanged from baseline |
| RestSharp | E:/WorkFiles/Repos/DistributedOrderSystem/src/frontend/customer-facing e-commerce/CustomerWebsite/CustomerWebsite.csproj | net9.0 | 110.2.0 | Unchanged from baseline |
| Serilog.AspNetCore | E:/WorkFiles/Repos/DistributedOrderSystem/src/frontend/customer-facing e-commerce/CustomerWebsite/CustomerWebsite.csproj | net9.0 | 8.0.0 | Unchanged from baseline |
| Serilog.Sinks.File | E:/WorkFiles/Repos/DistributedOrderSystem/src/frontend/customer-facing e-commerce/CustomerWebsite/CustomerWebsite.csproj | net9.0 | 5.0.0 | Unchanged from baseline |
| SixLabors.ImageSharp.Web | E:/WorkFiles/Repos/DistributedOrderSystem/src/frontend/customer-facing e-commerce/CustomerWebsite/CustomerWebsite.csproj | net9.0 | 3.1.0 | Unchanged from baseline |
| Stripe.net | E:/WorkFiles/Repos/DistributedOrderSystem/src/frontend/customer-facing e-commerce/CustomerWebsite/CustomerWebsite.csproj | net9.0 | 43.12.0 | Unchanged from baseline |
| MediatR | E:/WorkFiles/Repos/DistributedOrderSystem/src/GatewayBff/GatewayBff.csproj | net9.0 | 12.4.1 | Unchanged from baseline |
| Microsoft.AspNetCore.OpenApi | E:/WorkFiles/Repos/DistributedOrderSystem/src/GatewayBff/GatewayBff.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Scalar.AspNetCore | E:/WorkFiles/Repos/DistributedOrderSystem/src/GatewayBff/GatewayBff.csproj | net9.0 | 2.11.6 | Unchanged from baseline |
| Confluent.Kafka | E:/WorkFiles/Repos/DistributedOrderSystem/src/InventoryService/InventoryService.csproj | net9.0 | 2.6.1 | Unchanged from baseline |
| Microsoft.AspNetCore.OpenApi | E:/WorkFiles/Repos/DistributedOrderSystem/src/InventoryService/InventoryService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Microsoft.EntityFrameworkCore.Design | E:/WorkFiles/Repos/DistributedOrderSystem/src/InventoryService/InventoryService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Microsoft.EntityFrameworkCore.Tools | E:/WorkFiles/Repos/DistributedOrderSystem/src/InventoryService/InventoryService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Npgsql.EntityFrameworkCore.PostgreSQL | E:/WorkFiles/Repos/DistributedOrderSystem/src/InventoryService/InventoryService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Scalar.AspNetCore | E:/WorkFiles/Repos/DistributedOrderSystem/src/InventoryService/InventoryService.csproj | net9.0 | 2.11.6 | Unchanged from baseline |
| StackExchange.Redis | E:/WorkFiles/Repos/DistributedOrderSystem/src/InventoryService/InventoryService.csproj | net9.0 | 2.10.1 | Unchanged from baseline |
| FirebaseAdmin | E:/WorkFiles/Repos/DistributedOrderSystem/src/NotificationService/NotificationService.csproj | net9.0 | 3.0.0 | Unchanged from baseline |
| FluentValidation.AspNetCore | E:/WorkFiles/Repos/DistributedOrderSystem/src/NotificationService/NotificationService.csproj | net9.0 | 11.3.0 | Unchanged from baseline |
| Hangfire | E:/WorkFiles/Repos/DistributedOrderSystem/src/NotificationService/NotificationService.csproj | net9.0 | 1.8.16 | Unchanged from baseline |
| Hangfire.PostgreSql | E:/WorkFiles/Repos/DistributedOrderSystem/src/NotificationService/NotificationService.csproj | net9.0 | 1.20.9 | Unchanged from baseline |
| MailKit | E:/WorkFiles/Repos/DistributedOrderSystem/src/NotificationService/NotificationService.csproj | net9.0 | 4.7.1.1 | Unchanged from baseline |
| Microsoft.AspNetCore.OpenApi | E:/WorkFiles/Repos/DistributedOrderSystem/src/NotificationService/NotificationService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Microsoft.EntityFrameworkCore | E:/WorkFiles/Repos/DistributedOrderSystem/src/NotificationService/NotificationService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Microsoft.EntityFrameworkCore.Design | E:/WorkFiles/Repos/DistributedOrderSystem/src/NotificationService/NotificationService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Microsoft.EntityFrameworkCore.Tools | E:/WorkFiles/Repos/DistributedOrderSystem/src/NotificationService/NotificationService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Microsoft.Extensions.Logging.Console | E:/WorkFiles/Repos/DistributedOrderSystem/src/NotificationService/NotificationService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| MimeKit | E:/WorkFiles/Repos/DistributedOrderSystem/src/NotificationService/NotificationService.csproj | net9.0 | 4.7.1 | Unchanged from baseline |
| Newtonsoft.Json | E:/WorkFiles/Repos/DistributedOrderSystem/src/NotificationService/NotificationService.csproj | net9.0 | 13.0.3 | Unchanged from baseline |
| Npgsql.EntityFrameworkCore.PostgreSQL | E:/WorkFiles/Repos/DistributedOrderSystem/src/NotificationService/NotificationService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Scalar.AspNetCore | E:/WorkFiles/Repos/DistributedOrderSystem/src/NotificationService/NotificationService.csproj | net9.0 | 2.11.6 | Unchanged from baseline |
| Twilio | E:/WorkFiles/Repos/DistributedOrderSystem/src/NotificationService/NotificationService.csproj | net9.0 | 7.6.0 | Unchanged from baseline |
| Confluent.Kafka | E:/WorkFiles/Repos/DistributedOrderSystem/src/OrderService/OrderService.csproj | net9.0 | 2.6.1 | Unchanged from baseline |
| Microsoft.AspNetCore.OpenApi | E:/WorkFiles/Repos/DistributedOrderSystem/src/OrderService/OrderService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Microsoft.EntityFrameworkCore | E:/WorkFiles/Repos/DistributedOrderSystem/src/OrderService/OrderService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Microsoft.EntityFrameworkCore.Tools | E:/WorkFiles/Repos/DistributedOrderSystem/src/OrderService/OrderService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Npgsql.EntityFrameworkCore.PostgreSQL | E:/WorkFiles/Repos/DistributedOrderSystem/src/OrderService/OrderService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Scalar.AspNetCore | E:/WorkFiles/Repos/DistributedOrderSystem/src/OrderService/OrderService.csproj | net9.0 | 2.11.6 | Unchanged from baseline |
| Microsoft.AspNetCore.OpenApi | E:/WorkFiles/Repos/DistributedOrderSystem/src/PaymentService/PaymentService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Scalar.AspNetCore | E:/WorkFiles/Repos/DistributedOrderSystem/src/PaymentService/PaymentService.csproj | net9.0 | 2.11.6 | Unchanged from baseline |
| FluentValidation.DependencyInjectionExtensions | E:/WorkFiles/Repos/DistributedOrderSystem/src/ProductService/ProductService.csproj | net9.0 | 11.11.0 | Unchanged from baseline |
| MediatR | E:/WorkFiles/Repos/DistributedOrderSystem/src/ProductService/ProductService.csproj | net9.0 | 12.4.1 | Unchanged from baseline |
| Microsoft.AspNetCore.OpenApi | E:/WorkFiles/Repos/DistributedOrderSystem/src/ProductService/ProductService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Microsoft.EntityFrameworkCore | E:/WorkFiles/Repos/DistributedOrderSystem/src/ProductService/ProductService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Microsoft.EntityFrameworkCore.Tools | E:/WorkFiles/Repos/DistributedOrderSystem/src/ProductService/ProductService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Npgsql.EntityFrameworkCore.PostgreSQL | E:/WorkFiles/Repos/DistributedOrderSystem/src/ProductService/ProductService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Scalar.AspNetCore | E:/WorkFiles/Repos/DistributedOrderSystem/src/ProductService/ProductService.csproj | net9.0 | 2.11.6 | Unchanged from baseline |
| BCrypt.Net-Next | E:/WorkFiles/Repos/DistributedOrderSystem/src/UserService/UserService.csproj | net9.0 | 4.0.3 | Unchanged from baseline |
| Microsoft.AspNetCore.Authentication.Facebook | E:/WorkFiles/Repos/DistributedOrderSystem/src/UserService/UserService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Microsoft.AspNetCore.Authentication.Google | E:/WorkFiles/Repos/DistributedOrderSystem/src/UserService/UserService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Microsoft.AspNetCore.Authentication.JwtBearer | E:/WorkFiles/Repos/DistributedOrderSystem/src/UserService/UserService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | E:/WorkFiles/Repos/DistributedOrderSystem/src/UserService/UserService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Microsoft.EntityFrameworkCore.Tools | E:/WorkFiles/Repos/DistributedOrderSystem/src/UserService/UserService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Npgsql.EntityFrameworkCore.PostgreSQL | E:/WorkFiles/Repos/DistributedOrderSystem/src/UserService/UserService.csproj | net9.0 | 9.0.0 | Unchanged from baseline |
| Scalar.AspNetCore | E:/WorkFiles/Repos/DistributedOrderSystem/src/UserService/UserService.csproj | net9.0 | 2.11.6 | Unchanged from baseline |
| Swashbuckle.AspNetCore | E:/WorkFiles/Repos/DistributedOrderSystem/src/UserService/UserService.csproj | net9.0 | 7.2.0 | Unchanged from baseline |

## Follow-up

- Security upgrades for RestSharp, MailKit, and MimeKit are deferred to the .NET 10 solution-upgrade task; their baseline vulnerabilities remain.
- Deprecated AutoMapper.Extensions.Microsoft.DependencyInjection and FluentValidation.AspNetCore references are deferred to the solution-upgrade task.
- Existing Twilio NU1603 and Firebase SendAllAsync obsolescence warnings remain outside CPM conversion.
