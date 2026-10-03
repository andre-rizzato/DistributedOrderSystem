# 03-upgrade-solution: Upgrade all projects to .NET 10

## Research Findings

- The folder contains 12 SDK-style projects. Eleven were `net9.0`; AppHost was already `net10.0` with Aspire 13.5.3.
- `CustomerService` is outside the solution and was included explicitly.
- The solution assessment flags 45 binary-incompatible APIs, 20 source-incompatible APIs, and 64 behavioral changes across the solution. It points to configuration/option binding, MediatR registration, JWT APIs, HTTP content, URI handling, and exception handling. No .NET 9 framework targets or legacy project formats were present after the prior CPM task.
- Unused deprecated `AutoMapper.Extensions.Microsoft.DependencyInjection` and `FluentValidation.AspNetCore` references had no matching source usages and were removed. Microsoft.Extensions.Http, Microsoft.Extensions.Logging.Console, and System.Text.Json explicit references were removed after .NET 10 restore reported them as framework-provided (NU1510).
- EF Core and ASP.NET Core packages were assessed for .NET 10; Npgsql EF Core provider 10.0.3 was verified as supported for net10.0.
- Restore exposed dependency floors Newtonsoft.Json >=13.0.4 and System.IdentityModel.Tokens.Jwt >=8.19.2 through updated .NET 10 packages; central versions were aligned accordingly.
- Restore also surfaced ImageSharp advisories. The final security resolution uses ImageSharp.Web 3.1.5 with an explicit ImageSharp 3.1.12 pin, avoiding ImageSharp.Web 4.x's license-key requirement. Twilio was aligned from declared 7.5.2 to the pre-existing restore resolution 7.6.0.

## Execution Results

- Retargeted all eleven `net9.0` projects to `net10.0`; AppHost remains on `net10.0` / Aspire 13.5.3.
- Updated central ASP.NET Core, EF Core, JSON, Npgsql, authentication, and security package versions. Removed the unused deprecated package references.
- Solution restore and build succeeded. The out-of-solution CustomerService restore/build also succeeded.
- `dotnet list package --vulnerable --include-transitive` reports no vulnerable packages for any solution project or CustomerService.
- `dotnet test DistributedOrderSystem.sln` succeeded; the repository has no test projects, so no tests were discovered.
- An earlier build showed the pre-existing Firebase `SendAllAsync` obsolete warning. No Firebase behavior changes were included in this framework upgrade; runtime checks for HTTP, URI, exception handling, and auth behavior remain appropriate.

**Done when**: All 12 projects target or remain on `net10.0`, package restore succeeds, deprecated and vulnerable packages are resolved, and all identified compile-time API incompatibilities are addressed.
