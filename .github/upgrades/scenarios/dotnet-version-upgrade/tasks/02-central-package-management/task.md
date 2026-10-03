# 02-central-package-management: Centralize package versions

## Objective

Introduce root-level NuGet Central Package Management for every SDK-style project in the requested folder while honoring the user's package-version choices.

## Research Findings

- The folder contains 12 `.csproj` projects. Eleven are in `DistributedOrderSystem.sln`; `src/CustomerService/CustomerService.csproj` is outside the solution and is included explicitly.
- No `Directory.Packages.props`, `Directory.Build.props`, `Directory.Build.targets`, or NuGet config was found at the repository scope.
- The solution baseline build succeeds. The separate `CustomerService` baseline build succeeds.
- Direct package references are defined in project files. The dependency tool confirmed the 11 solution projects; it cannot inspect the out-of-solution project with this solution, so its three references were confirmed directly from its `.csproj`.
- Direct version conflicts found by parsing every project file:
  - `StackExchange.Redis`: ChatbotService and NotificationService 2.8.16; CustomerService, ProductService, and UserService 2.9.32; InventoryService 2.10.1.
  - `Swashbuckle.AspNetCore`: NotificationService 6.8.0; InventoryService and OrderService 6.8.1; UserService 7.2.0.
- User chose `StackExchange.Redis` 2.10.1 and `Swashbuckle.AspNetCore` 7.2.0 as the unified versions. Verify the Swashbuckle 6.x-to-7.x changes during the solution upgrade.
- Baseline warnings: RestSharp 110.2.0, MailKit 4.7.1.1, and MimeKit 4.7.1 have known moderate vulnerabilities; NotificationService resolves Twilio 7.5.2 to 7.6.0 with NU1603; Firebase `SendAllAsync` is obsolete.

## Execution Results

- Added root `Directory.Packages.props` with CPM enabled and 42 unique central package versions.
- Converted 89 direct PackageReference items across the project files to versionless references.
- Both user-approved package alignments were applied. Snapshot comparison: eight resolved-version changes, 82 unchanged entries, no added/removed packages, and no VersionOverride entries.
- Clean solution build and separate CustomerService build succeeded before and after conversion. Baseline and post-conversion package JSON and binlogs, plus `convert-to-cpm.md`, are in this task folder.
- Existing security, Twilio NU1603, and Firebase obsolescence warnings remain assigned to the .NET 10 upgrade task.

**Done when**: All folder projects use the root central package file, package resolution has no unexpected changes, and clean builds pass for the solution and standalone project.
