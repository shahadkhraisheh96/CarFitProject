# CarFit

ASP.NET Core 10.0 MVC + Razor Pages application for a Jordanian used-car marketplace. See `docs/REQUIREMENTS.md` for the spec, `docs/PROJECT_OVERVIEW.md` for an architectural tour, `docs/GAP_ANALYSIS.md` for the requirements-vs-code delta, and `docs/IMPLEMENTATION_PLAN.md` for the phased delivery plan.

## Running locally

1. Restore packages: `dotnet restore CarFitProject/CarFitProject/CarFitProject.csproj`.
2. Provide the SQL Server connection string via `appsettings.Development.json` or user secrets (`ConnectionStrings:DefaultConnection`). The Development connection string may include `TrustServerCertificate=True` for local SQL Server certificates; non-Development environments must use a properly signed certificate.
3. Provide the seeded admin credentials. In Development they default from `appsettings.Development.json` (`AdminSeed:Email`, `AdminSeed:Password`). In any other environment those keys must be present (user secrets, environment variables, or your secret store) or the host will fail to start.
4. Apply migrations:
   ```
   dotnet ef database update --context ApplicationDbContext
   dotnet ef database update --context CarFitDbContext
   ```
5. Run: `dotnet run --project CarFitProject/CarFitProject/CarFitProject.csproj`.

## Roles and terminology

The Identity roles seeded by the host are `Admin`, `Dealer`, and `Buyer` — matching the spec wording in `docs/REQUIREMENTS.md` §1.1. The folder layout in `Areas/Seller/` is the legacy namespace and is kept as-is — Dealer-facing controllers and views live there. UI strings refer to the role as "Dealer".

## Password hashing

The application overrides Identity's default PBKDF2 password hasher with BCrypt (`BCrypt.Net-Next`) at work factor 12 — see `Services/BCryptPasswordHasher.cs`. Password policy: minimum 10 characters with at least one digit, one uppercase letter, one non-alphanumeric character, and at least 4 unique characters.

## Email (password reset / confirmation)

Password-reset and email-confirmation tokens expire after 30 minutes.

In **Development** an `IEmailSender` is registered that writes the email body — including the reset link — to the logger, so resets are clickable without a live SMTP server. Look in the console output for `[Dev email]`.

In any **non-Development** environment the SMTP sender is registered instead. Configure the `EmailSettings` section (in `appsettings.{Environment}.json`, user secrets, or environment variables — never commit real credentials):

```json
"EmailSettings": {
  "Host": "smtp.example.com",
  "Port": 587,
  "User": "no-reply@example.com",
  "Password": "<smtp-password>",
  "From": "no-reply@example.com",
  "EnableSsl": true
}
```

`appsettings.json` ships with a blank placeholder block.

## Status

Phase 0a, 0b, and 1a of the implementation plan have landed. Phase 1b (account features — FullName on register, Manage page, password reset email) follows. See `docs/IMPLEMENTATION_PLAN.md`.
