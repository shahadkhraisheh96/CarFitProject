# CarFit — Implementation Plan

Phased delivery plan to close every gap identified in `docs/GAP_ANALYSIS.md`. Foundation first; nothing depending on a broken foundation gets built until that foundation is in place.

**Rules carried forward from the brief:**
- ASP.NET Core MVC + Areas, EF Core, Bootstrap 5 + jQuery. No React, no AI/ML libraries.
- Controllers thin; logic in services.
- All schema changes via EF Core migrations (only exception: idempotent versioned SQL for the view + stored proc).
- Bilingual AR (RTL) + EN.
- Recommendation engine: rule-based; ranking weights **profile 50% / inspection 30% / budget 20%**.
- Inspection glossary and scoring per the spec verbatim.

Phase dependencies: each phase depends on every earlier phase unless stated otherwise. Phases inside a group can be parallelised only after Phase 0 lands.

---

## Phase 0 — Foundation & Plumbing (BLOCKING)

**Goal:** Stabilise the platform so feature work has a sound base. Land before ANY feature phase. Result is a clean build with no destructive side-effects, a single source of truth for connections, a single user concept, working DI, and source-controlled view + stored proc.

| # | Task | FR/NFR satisfied | Files |
|---|---|---|---|
| 0.1 | Delete `OnConfiguring` override in `CarFitDbContext` (or guard with `if (!optionsBuilder.IsConfigured)`). Move connection string out of source for non-dev (user secrets / env var). | X1, NFR-M3 | `Models/CarFitDbContext.cs:41-42` |
| 0.2 | Refactor `Program.cs` startup seeding into an async `await SeedIdentityAsync(scope)` helper invoked after `app.Build()`. Remove `.Result` / `.Wait()`. | X2, NFR-M1 | `Program.cs:46-86` |
| 0.3 | Move admin seed credentials to user-secrets / `appsettings` + env override. Fail startup in non-Development if not provided. Re-seed only if account missing. | X8, NFR-S1 | `Program.cs:62-86`, `appsettings.json` |
| 0.4 | Harden password policy: `RequiredLength = 10`, `RequireDigit = true`, `RequireUppercase = true`, `RequireNonAlphanumeric = true`, `RequiredUniqueChars = 4`. Update register messages accordingly. | NFR-S1, X9 | `Program.cs:31-36` |
| 0.5 | Decide BCrypt-vs-PBKDF2 (see GAP NFR-S1). Recommended: plug in custom `IPasswordHasher<IdentityUser>` backed by `BCrypt.Net-Next` cost 12; add rehash-on-login fallback so any existing PBKDF2 hash is upgraded transparently. Reference: deferred to Phase 0 only if (b) chosen — otherwise document the deviation and move on. | NFR-S1 (CONFLICT) | `Services/BCryptPasswordHasher.cs` (new), `Program.cs` registration |
| 0.6 | Configure application cookie: `ExpireTimeSpan = 30m`, sliding, `SecurePolicy = Always`, `HttpOnly`, `SameSite = Lax`. | NFR-S5 | `Program.cs` |
| 0.7 | Register `IRecommendationService` in DI (`services.AddScoped<IRecommendationService, RecommendationService>()`). Replace raw ADO in `Buyer/DashboardController` with the service. | X3, FR-5.1 | `Program.cs`, `Services/RecommendationService.cs`, `Areas/Buyer/Controllers/DashboardController.cs:52-88` |
| 0.8 | Bring `vw_AvailableCarDetails` view into source as an idempotent migration (`CREATE OR ALTER VIEW`). Definition reverse-engineered from `VwAvailableCarDetail.cs` columns (Car + CarListing + Seller + InspectionReport join, `available = 1` filter). | X4, FR-5.1, NFR-M3 | `Migrations/{ts}_AddAvailableCarDetailsView.cs` (raw SQL) |
| 0.9 | Bring `GetCarMatchesForUser @ProfileId INT` stored procedure into source as an idempotent migration (`CREATE OR ALTER PROCEDURE`). Outputs match `RecommendedCarViewModel` columns. Initially mirrors the current behaviour — full scoring per FR-5.3 ships in Phase 5. | X4, FR-5.1, NFR-M3 | `Migrations/{ts}_AddGetCarMatchesProc.cs` |
| 0.10 | **Resolve dual-user identity.** Drop legacy `Users` table; migrate `SavedResult.UserId` and `RecommendationLog.UserId` from `int` to `nvarchar(450)` (Identity ID). Drop `User.cs` model, `User` entity from `CarFitDbContext`, FK rebuilds. Data: existing repo has no production data, so migration is a clean recreate of those two tables. | X5, FR-6.1 | `Models/SavedResult.cs`, `Models/RecommendationLog.cs`, `Models/User.cs` (delete), `Models/CarFitDbContext.cs:272-294,206-247`, new migration |
| 0.11 | **Establish Identity↔Seller link.** Add `Seller.IdentityUserId nvarchar(450)` (nullable for legacy rows, indexed). Add `Seller.Email`. Add `Seller.IsApproved bool` and `Seller.Tier nvarchar(20)` (preparing FR-7.6). | X6, FR-3.3, FR-7.6 | `Models/Seller.cs`, `Models/CarFitDbContext.cs:250-270`, new migration |
| 0.12 | Fix `[Area("Seller")]` missing on `SellerController` and rename / merge: keep `InventoryController` as the canonical seller controller; delete `SellerController` once duplicated logic is folded in. | X6, FR-3.3 | `Areas/Seller/Controllers/SellerController.cs` |
| 0.13 | Fix `Inventory.AddCar` POST to set `SellerId` from the current user's `IdentityUserId` → `Seller.Id`. Auto-create a draft `Seller` row on first listing if missing (or block until Dealer onboarding from Phase 1). | X6, FR-3.1 | `Areas/Seller/Controllers/InventoryController.cs:41-63` |
| 0.14 | Fix `Inventory.Index` to filter by current seller. | X6, FR-3.3 | `Areas/Seller/Controllers/InventoryController.cs:21-29` |
| 0.15 | Replace naive CSV split in BulkLoader with `CsvHelper`. Per-row error reporting (don't reject the whole batch). | X7, FR-7.2 | `Areas/Admin/Controllers/AdminController.cs:84-180`, `CarFitProject.csproj` |
| 0.16 | Add `[Authorize(Roles="Buyer")]` to `AdvisorController.Match` and filter `UserProfile` by current user. Plan to retire it in favour of consolidated Buyer dashboard (Phase 5). | X11, NFR-S6 | `Controllers/AdvisorController.cs:22-26` |
| 0.17 | Decide "Dealer" vs "Seller" terminology. Recommended: role identifier stays `Seller` (matches existing DB seed and namespace); UI strings say "Dealer". Document in `README.md`. | X10, FR-1.5 | README, all Razor views (string-level only) |
| 0.18 | Quick wins: remove unused `CarFitDbContext` injection from `InspectionController.Index` (`Controllers/InspectionController.cs:13-22`); verify `TrustServerCertificate` only in dev. | NFR-M1, NFR-S2 | `Controllers/InspectionController.cs`, `Program.cs:14-18` |

**Exit criteria:** project builds; all existing pages still render; new migrations apply on a clean DB and recreate the view + proc; admin account is seeded from configuration; cookie flags, password policy, and authorize attributes are in place; recommendation flow goes through the registered service.

---

## Phase 1 — Auth, Roles, and Dealer Onboarding

**Goal:** Round out the FR-1 set and FR-7.6.

| # | Task | FR | Files |
|---|---|---|---|
| 1.1 | Replace `IdentityUser` with `ApplicationUser : IdentityUser` carrying `FullName`, `IsActive`, `CreatedAt`. Migration adds columns; seeder updated. | FR-1.1, FR-7.1, FR-7.5 | `Models/ApplicationUser.cs` (new), `Data/ApplicationDbContext.cs`, `Program.cs`, new migration |
| 1.2 | Add `Name` to `Register` page; persist to `ApplicationUser.FullName`. | FR-1.1 | `Areas/Identity/Pages/Account/Register.cshtml(.cs)` |
| 1.3 | Scaffold Identity `Manage` pages for profile + change password + change email. | FR-1.4 | `Areas/Identity/Pages/Account/Manage/*` |
| 1.4 | Scaffold ForgotPassword / ResetPassword / ConfirmEmail; implement `IEmailSender` (SMTP via configuration). Set token lifespan 30 min. | FR-1.3 | `Areas/Identity/Pages/Account/ForgotPassword*.cshtml`, `Services/SmtpEmailSender.cs`, `Program.cs` |
| 1.5 | Dealer onboarding: post-register, if user picked "Seller", redirect to `/Seller/Onboarding` to fill `Seller` row (name, phone, email, city, neighborhood). `IsApproved = false`. | FR-7.6 | `Areas/Seller/Controllers/OnboardingController.cs`, view, `Models/Seller.cs` |
| 1.6 | Admin dealer approval queue: list pending sellers, approve / reject + assign tier (`Basic` / `Standard` / `Premium`). | FR-7.6, FR-7.1 | `Areas/Admin/Controllers/DealersController.cs`, view |
| 1.7 | User mgmt: Activate / Deactivate / Delete actions (`IsActive` toggles `LockoutEnd = DateTimeOffset.MaxValue` semantics). | FR-7.1 | `Areas/Admin/Controllers/AdminController.cs` |

**Depends on:** Phase 0.

---

## Phase 2 — Profile Questionnaire (multi-step wizard)

**Goal:** Replace single-page CreateProfile with a multi-step wizard covering FR-2.1 → FR-2.9.

| # | Task | FR | Files |
|---|---|---|---|
| 2.1 | Schema: add `TripType nvarchar(20)`, `ConditionPref nvarchar(20)`, `InstallmentMonths int?` to `UserProfile`. Migration. | FR-2.6, FR-2.7, FR-2.8 | `Models/UserProfile.cs`, `Models/CarFitDbContext.cs:296-340`, new migration |
| 2.2 | New `IUserProfileService` to persist partial-state across steps (session-backed `Dictionary<string,string>` or a draft row in `UserProfiles` with `IsActive = false` until completed). | FR-2.1, FR-2.9, NFR-M1 | `Services/UserProfileService.cs` |
| 2.3 | Wizard controller: 5–6 steps (Basics → Family → Purpose & Trip → Budget & Payment → Preferences → Review). Each step is GET + POST with `[ValidateAntiForgeryToken]`. | FR-2.1 to FR-2.8, NFR-S4 | `Areas/Buyer/Controllers/QuestionnaireController.cs`, views per step |
| 2.4 | Progress bar component (Bootstrap progress) visible on every step. | NFR-U5 | `Views/Shared/Components/StepProgress/*` |
| 2.5 | Edit existing profile: reuse wizard with prefilled values; profile list with rename / activate / delete. | FR-2.9 | same controller |
| 2.6 | Server-side validation: budget range (`BudgetMin < BudgetMax`), age ≥ 18, etc. Inline error rendering in EN/AR. | NFR-U4 | data annotations / `IValidatableObject` |

**Depends on:** Phase 0, Phase 1.

---

## Phase 3 — Listings, Search & Images

**Goal:** FR-3.1 → FR-3.6 end-to-end.

| # | Task | FR | Files |
|---|---|---|---|
| 3.1 | Schema: `CarListing.Status nvarchar(20)` (`Active`/`Sold`/`Pending`); replace boolean `Available` usages. `CarImage` table (`Id`, `CarId`, `Url`, `SortOrder`, `IsPrimary`). Migration. | FR-3.4, FR-3.5 | `Models/CarListing.cs`, `Models/CarImage.cs` (new), `Models/CarFitDbContext.cs`, migration |
| 3.2 | Image upload pipeline: `IImageStorageService` (local storage v1; pluggable for Azure Blob later); WebP conversion via `SixLabors.ImageSharp`; max 200KB after resize; min 3 / max 15 per listing. | FR-3.4, NFR-P4 | `Services/ImageStorageService.cs`, NuGet `SixLabors.ImageSharp` |
| 3.3 | Seller AddCar UI: full Car spec (make, model, year, engine, fuel, transmission, seats, size, body type, mileage, color, options) + multi-image upload + price + payment method. | FR-3.1 | `Areas/Seller/Views/Inventory/AddCar.cshtml`, controller POST |
| 3.4 | Seller EditCar / Deactivate / Delete actions (own listings only). | FR-3.3 | `Areas/Seller/Controllers/InventoryController.cs` |
| 3.5 | Admin AddCar / EditCar / Approve / Remove (any listing). Approve flips `Status` from `Pending` → `Active`. | FR-3.3, FR-7.2 | `Areas/Admin/Controllers/ListingsController.cs` (new) |
| 3.6 | Public search filters on `/Inventory/Search`: make, model, year range, price range, type (New/Used), transmission. LINQ-built query; pagination 12/page. | FR-3.6, NFR-Sc1 | `Controllers/InventoryController.cs`, view |
| 3.7 | Update `vw_AvailableCarDetails` migration to filter on `Status = 'Active'` instead of `Available = 1`. | FR-3.5 | new migration (re-run `CREATE OR ALTER VIEW`) |
| 3.8 | Additional indexes: `Cars.make`, `Cars.model`, `Cars.price`, `Cars.type`. | NFR-Sc2 | migration |

**Depends on:** Phase 0 (ownership + view in source), Phase 1 (dealer approval gates "Pending Review").

---

## Phase 4 — Inspection Reports & Glossary

**Goal:** FR-4.1 → FR-4.6 plus the auto-scoring engine that FR-5.2/5.3 will consume.

| # | Task | FR | Files |
|---|---|---|---|
| 4.1 | Replace free-text `Chassis*Status` columns with constrained enum / lookup table containing the 8 spec terms. Migration with data-conversion step (existing rows are dev data; safe to wipe). | FR-4.2 | `Models/InspectionReport.cs`, migration |
| 4.2 | Seed `InspectionTermsGlossary` with the 11 spec rows (AR term, EN translation, buyer explanation) via migration `HasData` or a one-off SQL migration. | FR-4.4 | migration |
| 4.3 | `IInspectionScoringService`: derives `OverallScore`, `CalculatedTrustScore`, `IsRisky` (any chassis = `شاصي مقصوص ومغير` or `خالي قص قلبان`), engine status (`Good ≥60`, `Weak 50-55`, `Unsafe` if smoke), gearbox status. Pure functions, unit-testable. | FR-4.3, FR-4.5 | `Services/InspectionScoringService.cs` |
| 4.4 | Inspection-report CRUD UI (Admin + owning Dealer) with `GlossaryTooltipViewComponent` on each chassis term. | FR-4.1, FR-4.4, FR-7.3 | `Areas/Admin/Controllers/InspectionReportsController.cs` (new), views |
| 4.5 | CarSeer badge on listing card + detail view when `CarseerAttached`. | FR-4.6 | partial view |
| 4.6 | Listing detail page renders full inspection report with bilingual term tooltips. | FR-4.4 | `Views/Inventory/Detail.cshtml` (new) |

**Depends on:** Phase 0, Phase 3 (listing detail/edit pages).

---

## Phase 5 — Recommendation Engine

**Goal:** FR-5.1 → FR-5.5. Single canonical path. Retire `AdvisorController.Match`.

| # | Task | FR | Files |
|---|---|---|---|
| 5.1 | Rewrite `RecommendationService` to produce the full scoring per spec: budget fit (20%), profile match across purpose/trip/transmission/size/condition (50%), inspection quality from `IInspectionScoringService` (30%). | FR-5.1, FR-5.2, FR-5.3 | `Services/RecommendationService.cs` |
| 5.2 | Decide engine location: keep stored proc OR replace with LINQ. Recommendation: **LINQ in C#** so logic stays testable and version-controlled. Update Phase 0.9 migration to leave a thin proc that just selects from the view; deprecate later. | FR-5.1, NFR-M3 | `Services/RecommendationService.cs`, possibly drop the proc |
| 5.3 | Cap output at top 5. | FR-5.3 | service |
| 5.4 | No-results relaxation: if zero ≥50% matches, retry with `BudgetMax * 1.10`; return banner text `"Budget widened by 10% — no exact matches"`. | FR-5.4 | service + view |
| 5.5 | Per-car `MatchReasons : List<string>` populated by scoring (e.g. `"Matches your family size (7 seats)"`, `"Inspection: جيد"`). | FR-5.5 | `ViewModel/RecommendedCarViewModel.cs`, service |
| 5.6 | Persist each query result to `RecommendationLog` (`UserId string`, `RecommendedCarIds`, `Score`, `CreatedAt`). Powers FR-7.5 "most recommended". | FR-7.5 | service |
| 5.7 | Retire `AdvisorController.Match`: 301-redirect to `/Buyer/Dashboard` or delete. | X11 | `Controllers/AdvisorController.cs` |

**Depends on:** Phase 0 (service registered, dual-user resolved), Phase 2 (questionnaire fields), Phase 3 (Status filter + images for cards), Phase 4 (inspection scoring).

---

## Phase 6 — User Actions on Results

**Goal:** FR-6.1 → FR-6.5.

| # | Task | FR | Files |
|---|---|---|---|
| 6.1 | Save Car: `POST /Buyer/Saved/Toggle?carId=…`; show count badge; enforce 3-free cap. | FR-6.1 | `Areas/Buyer/Controllers/SavedController.cs`, `Services/SavedResultService.cs` |
| 6.2 | Saved Cars page on buyer dashboard. | FR-6.1 | view |
| 6.3 | `ApplicationUser.SubscriptionTier nvarchar(20)` (`Free`/`Premium`). Default `Free`. Gating helper `ISubscriptionService.CanSaveMore(userId)`. | FR-6.1, FR-6.3 | `Models/ApplicationUser.cs`, `Services/SubscriptionService.cs` |
| 6.4 | Contact buttons on listing detail: WhatsApp (everyone) using `https://wa.me/<phone>`; email button visible only when `SubscriptionTier != Free`. | FR-6.3 | partial view |
| 6.5 | Compare page: form takes 2-3 `carId` querystring values; renders side-by-side table of specs + inspection summary. | FR-6.2 | `Controllers/CompareController.cs`, view |
| 6.6 | Mechanic booking: extend `InspectionBooking` with `CarListingId nullable FK` + optional `MechanicId`. Mechanic directory (seed a handful) keyed by city. | FR-6.4 | model, migration, controller, view |
| 6.7 | Share Listing: copy-link JS button on detail page. | FR-6.5 | partial view + JS snippet |

**Depends on:** Phase 0 (SavedResult key migration), Phase 1 (subscription tier on user), Phase 3 (listing detail), Phase 5 (recommendation results render Save buttons).

---

## Phase 7 — Admin (gaps remaining after earlier phases)

**Goal:** FR-7.5 analytics, fix CSV per Phase 0 already, polish.

| # | Task | FR | Files |
|---|---|---|---|
| 7.1 | `SearchLog` table: `Id, Term, FiltersJson, UserId?, CreatedAt`. Logged from `/Inventory/Search`. Index on `CreatedAt`. | FR-7.5 | `Models/SearchLog.cs`, migration, hook in controller |
| 7.2 | Analytics dashboard: total users (Identity), total active listings, top 5 most-recommended (group by `RecommendationLog.RecommendedCarIds`), top 10 search terms (`SearchLog`), monthly new registrations (`ApplicationUser.CreatedAt`). Replace hardcoded Chart.js arrays. | FR-7.5 | `Areas/Admin/Controllers/DashboardController.cs:21-44`, view |
| 7.3 | Verify Admin can edit any inspection report; Admin listings CRUD lands in Phase 3. | FR-7.3 | (covered) |

**Depends on:** Phases 1, 3, 5.

---

## Phase 8 — NFR hardening & polish

**Goal:** finish off remaining non-functional gaps not addressed inline.

| # | Task | NFR | Files |
|---|---|---|---|
| 8.1 | ASP.NET Core Localization: `IStringLocalizer`, `.resx` for EN + AR; `RequestLocalizationOptions`; language-switch action stored in cookie. | NFR-U1, NFR-U4 | `Resources/*.resx`, `Program.cs`, `LanguageController` |
| 8.2 | RTL stylesheet: add Bootstrap RTL bundle (`bootstrap.rtl.min.css`); layout switches `dir` based on culture. | NFR-U1 | `Views/Shared/_Layout.cshtml` |
| 8.3 | `PaginatedList<T>` helper; apply on all listing pages (admin lists, search, saved). | NFR-Sc1 | `Helpers/PaginatedList.cs` |
| 8.4 | XML doc comments on all public service / controller methods. | NFR-M2 | various |
| 8.5 | `README.md` at repo root: setup (connection string via user secrets, admin creds via env vars), migrations, dev URL, backup notes. | NFR-M2, NFR-R2 | `README.md` |
| 8.6 | Friendly error page audit (`Views/Shared/Error.cshtml`); confirm no stack trace in non-dev. | NFR-R3 | view |
| 8.7 | Cookie + HSTS + HTTPS audit checklist; document hosting TLS 1.2 requirement. | NFR-S2 | README |
| 8.8 | Mobile responsiveness audit (320 → 1920). | NFR-U2 | views |
| 8.9 | Lighthouse / load-test pass; document numbers. | NFR-P1, NFR-P2, NFR-P3 | docs |

**Depends on:** all earlier phases.

---

## Phase ordering at a glance

```
Phase 0  →  Phase 1  →  Phase 2 ─┐
              │                  │
              └──────────────────┼──→  Phase 3  →  Phase 4  →  Phase 5  →  Phase 6  →  Phase 7  →  Phase 8
                                 │
                                 └─→ (Phase 2 unblocks Phase 5 along with Phase 4)
```

- Phase 0 is mandatory and blocks everything.
- Phase 1 unblocks dealer ownership in Phase 3 and subscription gating in Phase 6.
- Phase 2 unblocks Phase 5 (recommendation needs the new profile fields).
- Phase 4 unblocks Phase 5 (inspection scoring is an input).
- Phase 3 unblocks Phase 6 (listing detail page hosts Save / Compare / Contact).
- Phase 7 reuses data from 1/3/5.
- Phase 8 finishes the NFR list.

---

## Decisions needed from you before Phase 0 starts

1. **BCrypt vs PBKDF2** (NFR-S1 CONFLICT). Recommend BCrypt-Net-Next at cost 12 with a rehash-on-login wrapper.
2. **Role naming**: keep `Seller` as the role identifier and display "Dealer" in UI? (Recommended.)
3. **Stored proc retention**: bring `GetCarMatchesForUser` into source now (Phase 0) and then deprecate it in Phase 5 in favour of pure LINQ? Or skip the proc entirely and replace immediately? (Recommendation: bring it in as a thin pass-through in Phase 0 so existing code paths don't break, fully replace in Phase 5.)
4. **Legacy `Users` table**: confirm it can be dropped (no production data depends on it).
5. **Email provider for FR-1.3**: SMTP creds via configuration — which SMTP host should `appsettings.json` point at? (Default: leave a placeholder + document in README.)

Awaiting your approval before any code is written.
