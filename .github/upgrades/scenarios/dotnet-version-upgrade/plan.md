# .NET 10 Upgrade Plan

## Upgrade Options

| Option | Selected | Why |
| ------ | -------- | --- |
| Upgrade Strategy | All-at-Once | The 11-project solution is on modern .NET, has a shallow dependency graph, and fits the strategy's atomic-upgrade range. |
| Package Management | Central Package Management | All projects are SDK-style, there are 11 projects, and no central package props file exists. |
| Unsupported Packages | Resolve Inline | The assessment identifies two deprecated package entries; resolve them in the same upgrade rather than deferring work. |
| Unsupported API Handling | Fix Inline | The assessment identifies 45 binary-incompatible and 20 source-incompatible API occurrences; the selected approach leaves no deferred stubs. |
| Test Coverage | Skip | Test generation is opt-in; no test projects are currently identified in the repository. |

### Selected Strategy

**All-At-Once** — All projects upgraded simultaneously in a single operation.
**Rationale**: 11 projects target .NET 9 or .NET 10, the dependency graph is two levels deep, and the solution has no CI-green constraint identified.

## Project Scope

All 12 SDK-style projects in the requested folder stay in one atomic upgrade group. `CustomerService` is outside the solution file, so it is included as a repository project and validated separately.

- AppHost: `src/AppHost/AppHost.csproj` (already net10.0; retain Aspire 13.5.3)
- ChatbotService: `src/ChatbotService/ChatbotService.csproj`
- CustomerService: `src/CustomerService/CustomerService.csproj` (not included in the solution)
- CustomerWebsite: `src/frontend/customer-facing e-commerce/CustomerWebsite/CustomerWebsite.csproj`
- GatewayBff: `src/GatewayBff/GatewayBff.csproj`
- InventoryService: `src/InventoryService/InventoryService.csproj`
- NotificationService: `src/NotificationService/NotificationService.csproj`
- OrderService: `src/OrderService/OrderService.csproj`
- PaymentService: `src/PaymentService/PaymentService.csproj`
- ProductService: `src/ProductService/ProductService.csproj`
- Shared: `src/Shared/Shared.csproj`
- UserService: `src/UserService/UserService.csproj`

## Tasks

### 01-prerequisites: Verify .NET 10 toolchain

Confirm the .NET 10 SDK and required restore/build tooling are available before changing project files. The environment already has .NET 10 SDKs installed, and the repository has no `global.json`; retain this as a preflight check rather than adding an SDK pin.

**Done when**: The .NET 10 SDK is confirmed usable for the solution and no SDK pin blocks the target.

### 02-central-package-management: Centralize package versions

Introduce Central Package Management for all 12 SDK-style projects in the requested folder by adding `Directory.Packages.props` and moving package version declarations out of project files. The package audit found two direct-version conflicts. By user decision, unify `StackExchange.Redis` at 2.10.1 and `Swashbuckle.AspNetCore` at 7.2.0; other direct package versions are consistent across consumers. `CustomerService` is outside the solution and must still receive the root central package file through normal MSBuild discovery.

**Done when**: All applicable package versions are centrally declared across all 12 projects, project files use versionless `PackageReference` entries, the two agreed versions are unified, and restore succeeds without package-version conflicts.

### 03-upgrade-solution: Upgrade all projects to .NET 10

Retarget the eleven .NET 9 projects in the folder to `net10.0` in the same operation; leave AppHost on its existing `net10.0` target and Aspire 13.5.3. Update .NET-aligned dependencies, resolve the deprecated package entries inline, and patch the assessment's MailKit, MimeKit, and RestSharp advisories. Restore also surfaced ImageSharp advisories, so update ImageSharp.Web to 3.1.5 and pin ImageSharp 3.1.12; align Twilio to its already-resolved 7.6.0. Investigate the 45 binary-incompatible APIs, 20 source-incompatible APIs, and 64 behavioral changes from the solution assessment; also upgrade and build the out-of-solution `CustomerService` project. Make required code changes inline and do not introduce deferred stubs. Keep EF Core and its provider/tooling versions compatible with .NET 10.

**Done when**: All 12 projects target or remain on `net10.0`, package restore succeeds, deprecated and vulnerable packages are resolved, and all identified compile-time API incompatibilities are addressed.

### 04-validation: Build and validate the solution

Restore and build the complete solution and separately build `CustomerService` after the atomic upgrade, resolving compilation errors from framework, package, and API changes in the same bounded pass. Run any existing tests; the repository currently has no test projects, and generated test coverage was explicitly skipped. Review behavioral-change findings, especially HTTP content, URI, configuration binding, authentication/IdentityModel behavior, and the Swashbuckle 6.x-to-7.x update; report areas that still require runtime verification.

**Done when**: `DistributedOrderSystem.sln` and `CustomerService.csproj` build without errors, available tests pass (or are reported as absent), and remaining runtime-only risks are documented. Leave changes uncommitted.
