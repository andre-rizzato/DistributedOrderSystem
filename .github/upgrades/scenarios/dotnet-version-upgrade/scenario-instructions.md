# .NET Version Upgrade Instructions

## Confirmed Parameters

- Scope: Entire solution
- Solution: `e:\WorkFiles\Repos\DistributedOrderSystem\DistributedOrderSystem.sln`
- Target framework: `net10.0`
- Execution mode: Automatic
- Working branch: `dev` (use current branch)
- Commit strategy: Leave changes uncommitted
- Aspire AppHost: Keep existing .NET 10 / Aspire 13.5.3 configuration

## User Preferences

- Include relevant security fixes identified during assessment.

## Upgrade Options

### Strategy

- Upgrade Strategy: All-at-Once

### Project Structure

- Package Management: Central Package Management
- StackExchange.Redis conflict: unify at 2.10.1
- Swashbuckle.AspNetCore conflict: unify at 7.2.0

### Compatibility

- Unsupported Packages: Resolve Inline (2 incompatible package entries)
- Unsupported API Handling: Fix Inline

### Reliability

- Test Coverage: Skip

## Strategy

**Selected**: All-at-Once
**Rationale**: The solution has 11 modern .NET projects and a shallow dependency graph; the assessment recommends an atomic approach for this project count and upgrade type.

### Execution Constraints

- Update all target frameworks together; do not leave the solution split across .NET 9 and .NET 10 during the upgrade.
- Verify the .NET 10 SDK before editing project targets.
- Establish Central Package Management before updating package versions.
- Resolve flagged API changes, incompatible/deprecated packages, and security vulnerabilities inline; do not leave stubs or deferred resolution tasks.
- Build the solution and run available tests after the full upgrade; leave all changes uncommitted as requested.
