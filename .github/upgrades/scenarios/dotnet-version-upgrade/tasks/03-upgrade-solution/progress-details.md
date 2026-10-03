# Progress Details

- Retargeted all eleven net9.0 projects to net10.0; AppHost stays on net10.0 with Aspire 13.5.3.
- Upgraded ASP.NET Core and EF Core package families to 10.0.12 and Npgsql.EntityFrameworkCore.PostgreSQL to 10.0.3.
- Updated Newtonsoft.Json to 13.0.4, System.IdentityModel.Tokens.Jwt to 8.19.2, MailKit/MimeKit to 4.18.1, RestSharp to 114.0.0, and ImageSharp.Web/ImageSharp to 3.1.5/3.1.12.
- Aligned Twilio to 7.6.0, its prior resolved version; removed unused deprecated AutoMapper and FluentValidation ASP.NET Core packages.
- Removed framework-provided Microsoft.Extensions.Http, Microsoft.Extensions.Logging.Console, and System.Text.Json references after NU1510.
- Solution build and separate CustomerService build succeeded. Vulnerability audit reports no vulnerable packages. `dotnet test` succeeds; no tests are defined.
- Existing Firebase `SendAllAsync` obsolete warning remains; runtime behavior of HTTP, URI, exception handling, and authentication should still be exercised by application validation.
