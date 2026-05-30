# CarFit

ASP.NET Core 10.0 MVC + Razor Pages application for a Jordanian used-car
marketplace. Buyers complete a lifestyle questionnaire and the recommendation
engine ranks cars by profile match (50%) + inspection quality (30%) + budget
fit (20%). Dealers list inventory, admins approve dealers and curate the
catalogue. Bilingual EN / AR with full RTL.

For the requirements spec see `docs/REQUIREMENTS.md`; for the architectural
tour and gap analysis see `docs/PROJECT_OVERVIEW.md`, `docs/GAP_ANALYSIS.md`,
and `docs/IMPLEMENTATION_PLAN.md`.

## Prerequisites

- .NET SDK 10.0
- SQL Server 2019+ (LocalDB or full instance — `.\SQLEXPRESS` works fine)
- `dotnet-ef` global tool: `dotnet tool install --global dotnet-ef`
- A modern browser

## Local setup

1. Restore packages:
   ```
   dotnet restore CarFitProject/CarFitProject/CarFitProject.csproj
   ```

2. Configure the SQL Server connection via **user secrets** so your machine's
   value stays out of the repo. The repo's `appsettings.json` ships a
   placeholder; user secrets override it:
   ```
   cd CarFitProject/CarFitProject/CarFitProject
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.\\SQLEXPRESS;Database=CarFit;Trusted_Connection=True;TrustServerCertificate=True"
   ```
   `TrustServerCertificate=True` is auto-appended in Development only.

3. Configure the seeded admin account. In Development a default is provided in
   `appsettings.Development.json`; in **any other environment** these keys
   must come from user secrets or env vars or the host will refuse to start:
   ```
   dotnet user-secrets set "AdminSeed:Email"    "admin@your.tld"
   dotnet user-secrets set "AdminSeed:Password" "<a-strong-password>"
   ```

4. Apply migrations against **both** DbContexts:
   ```
   dotnet ef database update --context ApplicationDbContext
   dotnet ef database update --context CarFitDbContext
   ```

5. Run:
   ```
   dotnet run --project CarFitProject/CarFitProject/CarFitProject.csproj
   ```
   Default URLs: `https://localhost:7025` and `http://localhost:5113`.

The startup seeder is idempotent and runs on every boot — it creates the
`Admin`, `Dealer`, and `Buyer` roles (and migrates any legacy `"Seller"` role
members to `"Dealer"`), seeds the admin user from configuration, seeds the
11-term inspection glossary from `docs/REQUIREMENTS.md` §5, and seeds 9 sample
mechanics across Amman, Irbid, Zarqa, and Aqaba.

## Two DbContexts, two migration timelines

| Context | Owns | Migrations folder |
|---|---|---|
| `ApplicationDbContext` | Identity tables (`AspNet*`), plus `ApplicationUser.FullName / IsActive / CreatedAt / SubscriptionTier` | `CarFitProject/CarFitProject/Data/Migrations/` |
| `CarFitDbContext` | Domain tables (`Cars`, `CarListings`, `Sellers`, `InspectionReports`, `CarImages`, `Mechanics`, `RecommendationLog`, `SearchLog`, ...) plus the `vw_AvailableCarDetails` view | `CarFitProject/CarFitProject/Migrations/` |

Pulling a new feature branch usually means running **both** updates — the
build will succeed before either is applied, but startup queries will fail.

## Roles and terminology

The Identity roles seeded by the host are `Admin`, `Dealer`, and `Buyer` to
match `docs/REQUIREMENTS.md` §1.1. Folders under `Areas/Seller/` are the
legacy namespace; the dealer-facing controllers and views live there for
historical reasons. UI strings refer to the role as "Dealer" everywhere
(English) / "الوكيل" / "تاجر السيارات" (Arabic).

`Seller.Tier` is the **dealer** subscription concept (Basic / Standard /
Premium) used by FR-7.6. It is unrelated to `ApplicationUser.SubscriptionTier`,
which is the **buyer** plan (Free / Premium) that gates Save-Car capacity and
the email-the-dealer button.

## Authentication & password policy

- Identity's default PBKDF2 password hasher is replaced by BCrypt
  (`BCrypt.Net-Next`) at work factor 12 — see
  `Services/BCryptPasswordHasher.cs`.
- **Password policy**: minimum 10 characters; at least one digit, one
  uppercase letter, one non-alphanumeric character; at least 4 unique
  characters.
- **Session cookie**: 30-minute sliding expiry, `HttpOnly`, `Secure=Always`,
  `SameSite=Lax`.
- **Password-reset / email-confirmation tokens**: 30-minute lifespan
  (`DataProtectionTokenProviderOptions`).
- HTTPS redirect + HSTS are enabled in non-Development environments
  (`app.UseHttpsRedirection()` + `app.UseHsts()`).
- Production hosting must terminate TLS 1.2 or higher — the framework can
  redirect HTTP → HTTPS but the actual TLS handshake happens at the host
  (IIS, Kestrel-direct, or a reverse proxy). Configure the host accordingly.

## Email (password reset / confirmation)

`IEmailSender` is registered per environment:

- **Development** uses `LoggingEmailSender`. It writes the email body
  (including the reset link) to the application logger so you can click
  through without a live SMTP server. Look in console output for `[Dev email]`.
- **Non-Development** uses `SmtpEmailSender`. Configure the `EmailSettings`
  block — in `appsettings.{Environment}.json`, user secrets, or environment
  variables. **Never** commit real credentials.

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

`appsettings.json` ships with a blank placeholder.

## Internationalization

Cultures: `en` (default) and `ar`. Culture is sticky via the standard
ASP.NET Core localization cookie set by `/Language/Set?culture=ar`. RTL
flips automatically — `<html dir="rtl">` is set and the RTL Bootstrap
bundle is loaded when the culture is `ar`. Translations live in
`Resources/SharedResource.{en|ar}.resx`. Data-annotation messages on view
models resolve through the same shared file.

User-facing flows are bilingual: nav/chrome, Home, the questionnaire wizard
and its validation messages, Buyer dashboard / Saved / Compare / Profiles,
Search, and Listing Detail. Admin / Identity scaffolded / Dealer-area screens
remain English in the current pass.

## Ops notes

- **Database backups**: production deployments should configure SQL Server to
  run **daily backups** with at least **30-day retention** (NFR-R2). Either a
  SQL Server Agent maintenance plan or, on Azure SQL, the built-in PITR with a
  ≥ 30-day retention policy.
- **Uploaded images**: stored under `wwwroot/uploads/cars/{carId}/` as
  ImageSharp-encoded WebP capped at ≤ 200 KB. Excluded from git via
  `.gitignore`. Treat this directory as user data — include it in the backup
  story or migrate to Azure Blob via `IImageStorageService`.
- **Health**: the host requires SQL Server connectivity at startup for the
  seeder to run. Failures fall through to the standard ASP.NET Core error
  pipeline.

## Operational & Unverified Requirements

These requirements are real but are **not** enforced by application code in
this build — they are operational concerns or quality bars that need a live
environment to measure. Document them here so they don't get lost.

- **NFR-R1 — 99.5% uptime (≤ ~3.6 h downtime/month)**: hosting responsibility.
  Achieved through the Azure App Service SLA / IIS health monitoring / load
  balancer configuration of whichever environment runs the app.
- **NFR-R2 — Daily database backups, 30-day retention**: hosting
  responsibility (see Ops notes above). Configure SQL Server Agent or Azure
  SQL PITR with ≥ 30-day retention.
- **NFR-P1 — Page load < 3 s on 4G**: not formally measured in this build.
  Validate with Lighthouse against a representative deployment under
  4G-throttled network conditions before launch.
- **NFR-P2 — Recommendation response < 2 s**: the recommendation service is
  LINQ-based with indexed columns and a deterministic top-5 cut, but
  end-to-end timing has not been benchmarked. Add timing instrumentation
  (Application Insights or `ILogger` scope) before launch.
- **NFR-P3 — 200 concurrent users**: not load-tested. Run a k6 / Azure Load
  Testing scenario against staging before public launch.
- **NFR-U2 — Responsive 320 px → 1920 px**: the layout uses stock Bootstrap 5
  responsive utilities but has not had a formal cross-viewport audit. Walk
  the questionnaire wizard, Search, Detail, and Saved screens at
  320 / 768 / 1024 / 1440 / 1920 before launch.

## Status

Phases 0a → 8a have landed. Phase 8b (this README, audits, XML docs,
pagination polish) wraps the project. See `docs/IMPLEMENTATION_PLAN.md`
for the full phase log.
