# CarFit — Gap Analysis

Maps every functional and non-functional requirement in `docs/REQUIREMENTS.md` against the current codebase as documented in `docs/PROJECT_OVERVIEW.md`. No code changes were made.

**Status legend**
- **DONE** — meets the requirement (may need light polish, but is functionally there).
- **PARTIAL** — partly built or the wiring exists but it's broken, half-wired, or wrong shape.
- **MISSING** — no code addresses this requirement.
- **CONFLICT** — the spec contradicts the framework / current architecture; flagged for a decision before implementation.

File:line citations point at the most relevant location, not the only one.

---

## FR-1 — Authentication & Account Management

| ID | Requirement | Status | Current state in code | What's needed |
|---|---|---|---|---|
| FR-1.1 | Registration (name, email, password; unique email; hashed) | PARTIAL | `Areas/Identity/Pages/Account/Register.cshtml.cs:71-102` collects email + password (no `Name`). Identity enforces unique email when configured; password hashed via Identity (PBKDF2). | Add `Name` field to InputModel and persist via custom `ApplicationUser` (or a one-to-one profile row); ensure `RequireUniqueEmail = true`. |
| FR-1.2 | Login / Logout, secure sessions | DONE | `Areas/Identity/Pages/Account/Login.cshtml.cs:104-138`, `Logout.cshtml.cs`. Cookie auth via `AddDefaultIdentity`. | Tighten cookie flags (`Secure`, `HttpOnly`, `SameSite`) and idle timeout (see NFR Session). |
| FR-1.3 | Password reset via email link, 30-min expiry | MISSING | No `/Account/ForgotPassword*` Razor pages scaffolded. No `IEmailSender` implementation registered. | Scaffold ForgotPassword / ResetPassword pages, register an `IEmailSender`, set `DataProtectionTokenProviderOptions.TokenLifespan = 30m`. |
| FR-1.4 | Profile settings page (view + update name/email/preferences) | MISSING | No `ManageController` / profile-edit page. `Areas/Identity/Pages/Account/Manage/*` was not scaffolded. | Scaffold Identity `Manage` pages or build a custom `ProfileController` view. |
| FR-1.5 | Role-based access: Buyer, Dealer, Admin | PARTIAL / CONFLICT | Roles seeded are `Admin, Seller, Buyer` (`Program.cs:53`). Spec calls them `Buyer, Dealer, Admin`. `SellerController` uses `[Authorize(Roles="Seller,Dealer")]` (`Areas/Seller/Controllers/SellerController.cs:9`) but `Dealer` is never seeded. Role-based redirect exists at `Controllers/HomeController.cs:18-29`. | **Decision**: rename `Seller` → `Dealer` everywhere (spec wording) or keep `Seller` as the role name and treat "Dealer" as a subscription concept (FR-7.6). Recommend: keep role `Seller` internally for consistency with existing tables, surface as "Dealer" in UI. Then seed `Dealer` is no longer needed. |

---

## FR-2 — Profile Questionnaire

| ID | Requirement | Status | Current state in code | What's needed |
|---|---|---|---|---|
| FR-2.1 | Multi-step form, back/forward navigation | MISSING | `Areas/Buyer/Views/Dashboard/CreateProfile.cshtml` is a single-page form (POST handler at `Areas/Buyer/Controllers/DashboardController.cs:103`). No step state. | Build a multi-step wizard (session-backed or hidden fields); progress indicator (NFR Usability). |
| FR-2.2 | Age input | DONE | `UserProfile.Age` (`Models/UserProfile.cs:14`), column `age` (`Migrations/20260528205536_InitialBaseline.cs:123`). | — |
| FR-2.3 | Marital status (Single/Married) | DONE | `UserProfile.MaritalStatus` (`Models/UserProfile.cs:16`). | Constrain to enum in UI. |
| FR-2.4 | Children Y/N + count | DONE | `UserProfile.HasKids`, `KidsCount` (`Models/UserProfile.cs:18-20`). | — |
| FR-2.5 | Car purpose (Work / University / Family / Travel) | PARTIAL | `UserProfile.Purpose` exists (free-text `nvarchar(100)`). Default `"Commuting"` set in code (`Areas/Buyer/Controllers/DashboardController.cs:115`). | Constrain to the four enumerated values; use an enum/select list. |
| FR-2.6 | Trip type (Short/Long) | MISSING | No `TripType` column on `UserProfile`. | Add `TripType` column + migration; pass to recommendation engine. |
| FR-2.7 | Budget range + Cash/Installments + months | PARTIAL | `BudgetMin`, `BudgetMax`, `PaymentMethod` exist. Number of installment months not modeled. | Add `InstallmentMonths int?`. |
| FR-2.8 | New / Used / No-Preference + transmission + size | PARTIAL | `TransmissionPref`, `SizePref` exist. No `Condition` (New/Used/Any) field. | Add `ConditionPref` column + migration. |
| FR-2.9 | Save profile, update anytime | PARTIAL | Profile can be created (`CreateProfile` POST). No update/edit action. Multiple profiles supported via `activeProfileId` (`Areas/Buyer/Controllers/DashboardController.cs:22`). | Add `EditProfile` GET/POST. Add server-side validation. |

---

## FR-3 — Car Database & Listings

| ID | Requirement | Status | Current state in code | What's needed |
|---|---|---|---|---|
| FR-3.1 | New-car listing creation (full spec fields) | PARTIAL | `Areas/Seller/Controllers/InventoryController.cs:41-63` `AddCar` POST creates `Car` + `CarListing`. **Does NOT set `SellerId`** (line 50-57). No image upload. Admin lacks a dedicated AddCar page (only CSV BulkLoader). | Set `SellerId` from current user (after Identity↔Seller link is established). Add image upload (FR-3.4). Add Admin AddCar view. |
| FR-3.2 | Used-car listing includes inspection report | PARTIAL | `InspectionReport` model + 1-1 FK to `Car` exists (`Models/InspectionReport.cs`, `Models/CarFitDbContext.cs:185-188`). No UI lets a Seller attach one; admin lacks an inspection-report editor. | Add inspection-report create/edit form linked to listing flow. Required when listing's `Car.Type == "Used"`. |
| FR-3.3 | Dealers edit / deactivate / delete OWN listings; admin manages all | MISSING | `Areas/Seller/Controllers/InventoryController.cs:21-29` lists **every** seller's listings (no `Where(SellerId == …)`). No Edit / Delete / Deactivate actions. Admin has no listing-management screen beyond CSV. | Implement per-seller filtering once ownership link exists. Add Edit/Delete/Deactivate endpoints. Add Admin listings CRUD. |
| FR-3.4 | Image upload, 3–15 photos per listing | MISSING | `Car.Images` is a single `nvarchar(max)` string. No upload pipeline, no validation. | New `CarImage` table (CarId, Url, SortOrder) or store as JSON; upload action; min/max enforcement; WebP conversion (NFR Performance). |
| FR-3.5 | Listing status: Active / Sold / Pending Review; only Active shown in recommendations | PARTIAL | `CarListing.Available` (bool) only models a binary. `vw_AvailableCarDetails` filters by `available = 1` (implicit). | Add `Status` enum column (`Active`/`Sold`/`Pending`); replace `Available` boolean usage; update view + filter logic. |
| FR-3.6 | Manual search & filter | PARTIAL | `Controllers/InventoryController.cs:18-28` (`/Inventory/Search`) lists available listings, no filter parameters. | Add filter form (make, model, year, price range, type, transmission). Server-side query + pagination (NFR Scalability). |

---

## FR-4 — Inspection Report System

| ID | Requirement | Status | Current state in code | What's needed |
|---|---|---|---|---|
| FR-4.1 | Inspection data entry: center, date, chassis 1-4, engine %, gearbox, body, roof, paint | PARTIAL | Schema present (`Models/InspectionReport.cs`). No CRUD UI for admin/dealer. | Build `InspectionReportController` (admin + dealer-owned). |
| FR-4.2 | Chassis status enum per official Jordanian scale | PARTIAL | Columns are `nvarchar(50)` free text (`Migrations/20260528205536_InitialBaseline.cs:163-166`). | Constrain to the 8 chassis terms from the spec via enum / lookup table or check constraint. |
| FR-4.3 | Auto-calc overall score; flag شاصي مقصوص as Risky | MISSING | `OverallScore` and `CalculatedTrustScore` columns exist but nothing computes them. No "Risky" flag. | Service: `IInspectionScoringService` computes overall score from chassis + body + paint + engine. Mark Risky when any chassis = `شاصي مقصوص ومغير` or `خالي قص قلبان`. Persist on save. |
| FR-4.4 | Plain-language display in AR + EN | PARTIAL | `InspectionTermsGlossary` table + admin manager exists (`Areas/Admin/Controllers/AdminController.cs:43-71`). `GlossaryTooltipViewComponent` renders tooltips (`Components/GlossaryTooltipViewComponent.cs`). | Seed glossary with the 11 spec terms; ensure tooltip is used on all inspection-display views. |
| FR-4.5 | Engine %: ≥60 Good, 50-55 Weak, smoke Unsafe; gearbox Good/Knocking | MISSING | `EngineHealthPercent`, `EngineSmoke`, `GearboxStatus` exist as raw fields. No status-derivation logic, no UI rendering. | Add derivation logic in scoring service + helper for views (badge classes). |
| FR-4.6 | CarSeer badge | PARTIAL | `InspectionReport.CarseerAttached` boolean exists. No badge in any view. | Render badge on listing cards/details. |

---

## FR-5 — Recommendation Engine

Critical realities:
- `vw_AvailableCarDetails` view and `GetCarMatchesForUser` stored procedure **are not in source control**. They are referenced at runtime (`Models/CarFitDbContext.cs:346`, `Areas/Buyer/Controllers/DashboardController.cs:54`, `Services/RecommendationService.cs:25`) but no migration creates them. Any fresh database will fail.
- `IRecommendationService` is defined (`Services/RecommendationService.cs:7-28`) but **never registered in DI** and never injected anywhere.
- `AdvisorController.Match` (`Controllers/AdvisorController.cs:22-99`) uses the **first global active profile**, not the current user's, and is anonymous.

| ID | Requirement | Status | Current state in code | What's needed |
|---|---|---|---|---|
| FR-5.1 | Profile-based query (budget, purpose, trip, transmission, size) | PARTIAL | Three different paths exist (Buyer dashboard raw ADO.NET, unused `IRecommendationService`, `AdvisorController.Match`). None query on trip type (column missing). Purpose ignored by `AdvisorController.Match`. Stored proc body is unknown — not in repo. | Consolidate into one service. Port the stored proc into a versioned SQL migration *or* replace with LINQ. Cover all five spec criteria. |
| FR-5.2 | Inspection scoring penalties per chassis term | MISSING | No code computes inspection penalty. Stored proc may, but its definition is not in source. | Implement penalty function in the scoring service; tied to FR-4.3. |
| FR-5.3 | Combined ranking: 50% profile + 30% inspection + 20% budget; top 3-5 | PARTIAL / CONFLICT | `AdvisorController.Match` uses a 40/30/30 split (Budget/Transmission/Family). Spec demands 50/30/20. Threshold ≥50 means returns can exceed top 5. | Implement exact 50/30/20 weights; cap results to top 5. |
| FR-5.4 | No-results: relax filters by 10% budget, notify user | MISSING | No relaxation logic. | Implement fallback pass with `BudgetMax * 1.10`; surface a banner. |
| FR-5.5 | Explanation per recommended car | MISSING | `RecommendedCarViewModel` has no explanation field. | Add `MatchReasons : List<string>` (or rendered HTML/translation keys); populate in scoring service. |

---

## FR-6 — User Actions on Results

| ID | Requirement | Status | Current state in code | What's needed |
|---|---|---|---|---|
| FR-6.1 | Save Car; 3 free / unlimited premium | PARTIAL | `SavedResult` table exists with composite PK `(UserId int, CarId int)` (`Models/SavedResult.cs:8-9`). **`UserId` is int — points at legacy `Users` table, not Identity GUID**. No Save endpoint, no premium gating. | Migrate `SavedResult.UserId` to `string` (`AspNetUsers.Id`). Add `SaveCar`/`UnsaveCar` actions. Add `SubscriptionTier` field on user + gating logic. |
| FR-6.2 | Compare 2-3 cars side-by-side | MISSING | No compare controller/view. | New `CompareController` + view; accept up to 3 car IDs. |
| FR-6.3 | Contact seller — WhatsApp (all) + email (premium) | MISSING | `Seller.Phone`, no email field. No contact UI. Premium gating not modeled. | Add WhatsApp link (uses `Seller.Phone`) on listing detail; show email when tier > Basic. Add `Email` to `Seller`. |
| FR-6.4 | Book mechanic | PARTIAL | `InspectionBooking` model + `InspectionController.Book` exists (`Controllers/InspectionController.cs:34-66`). Not tied to a specific shortlisted car; no mechanic directory. | Repurpose `InspectionBooking` or add `MechanicBooking`; attach `CarListingId`; mechanic-by-location lookup table. |
| FR-6.5 | Share listing link | MISSING | No share UI. | Trivial: add copy-link button on listing detail. |

---

## FR-7 — Admin Panel

| ID | Requirement | Status | Current state in code | What's needed |
|---|---|---|---|---|
| FR-7.1 | User mgmt: view / activate / deactivate / delete | PARTIAL | `Areas/Admin/Controllers/AdminController.cs:36-40` `UsersList` view-only. No state-change actions. | Add `ToggleActive`, `Delete` actions; persist `IsActive` on user. |
| FR-7.2 | Listing mgmt: add / edit / approve / remove + CSV import | PARTIAL | CSV import exists (`Areas/Admin/Controllers/AdminController.cs:84-180`) but uses naive `line.Split(',')` (line 112) and **rejects all rows if any error exists** (line 160). No per-listing admin CRUD. No "approve" workflow (ties to FR-3.5 Pending Review). | Replace CSV parser with CsvHelper. Add per-listing CRUD + Approve action. |
| FR-7.3 | Inspection-report mgmt (add/edit per used car) | MISSING | No inspection-report admin CRUD. | New admin `InspectionReportController`. |
| FR-7.4 | Glossary mgmt | DONE | `Areas/Admin/Controllers/AdminController.cs:43-72` `GlossaryManager` + `UpdateGlossaryTerm`. View at `Areas/Admin/Views/Admin/GlossaryManager.cshtml`. | Seed initial 11 terms via migration data seed. |
| FR-7.5 | Analytics: total users, listings, most-recommended, top searches, monthly new regs | PARTIAL | `Areas/Admin/Controllers/DashboardController.cs:21-44` shows total users, available listings, glossary terms + a Chart.js dataset with **hardcoded** months and an array containing `ViewBag.TotalListings` (line 31). No most-recommended, no search-term tracking, no registration time series. | Track `SearchLog`, `RecommendationLog`; aggregate `AspNetUsers.LockoutEnd`/created timestamps (Identity does not store `CreatedAt` by default → add). |
| FR-7.6 | Dealer approval + subscription tiers (Basic/Standard/Premium) | MISSING | No `Dealer` role seeded. No subscription tier on user/seller. `Seller` table has no `IsApproved` flag. | Add `IsApproved` + `Tier` on `Seller` (or on a new `DealerProfile` joined to `AspNetUsers`). Approval action in admin. |

---

## Non-Functional Requirements

### Performance

| ID | Requirement | Status | Current state | What's needed |
|---|---|---|---|---|
| NFR-P1 | Page < 3s on 4G | UNKNOWN | No perf measurement in repo. | Set up basic Application Insights / lighthouse audit checklist post-feature. |
| NFR-P2 | Recommendation < 2s | UNKNOWN | Buyer dashboard opens raw ADO.NET + executes stored proc. Acceptable if proc is fast. | Add timing log; index `UserProfiles.user_id`. |
| NFR-P3 | ≥ 200 concurrent users | UNKNOWN | — | Load testing pass before launch. |
| NFR-P4 | Images WebP ≤ 200KB | MISSING | No image pipeline (FR-3.4). | Server-side conversion (e.g. `ImageSharp`) on upload. |

### Security

| ID | Requirement | Status | Current state | What's needed |
|---|---|---|---|---|
| NFR-S1 | BCrypt cost ≥ 12 | **CONFLICT** | Identity ships PBKDF2 (`PasswordHasher<IdentityUser>`). Spec demands BCrypt-12. | **Decision required**. Three options: (a) accept PBKDF2 as compliant-equivalent and document it; (b) plug in a custom `IPasswordHasher<IdentityUser>` backed by `BCrypt.Net-Next` at cost 12 (breaks any existing PBKDF2 hash on rotation); (c) document deviation in NFR-S1 and amend the spec. **Recommend (b)** for greenfield, **(a)** with documented justification if any production hashes exist. The current admin seed at `Program.cs:64` would be re-hashed on first startup with the new hasher; existing user hashes need a rotation strategy (rehash on next successful login). |
| NFR-S2 | HTTPS, HTTP→HTTPS redirect, TLS ≥ 1.2 | PARTIAL | `app.UseHttpsRedirection()` (`Program.cs:99`) + `UseHsts` in non-dev (`Program.cs:96`). TLS version controlled by host, not app. | Document hosting TLS requirement; verify in launch settings. |
| NFR-S3 | SQL injection prevention | PARTIAL | All EF Core queries are parameterized. Raw `EXEC GetCarMatchesForUser {0}` (`Services/RecommendationService.cs:25`) uses positional params — safe. Raw ADO command (`Areas/Buyer/Controllers/DashboardController.cs:54`) uses parameter objects — safe. CSV BulkLoader inserts via EF — safe. | OK after stored proc is brought into source and reviewed. |
| NFR-S4 | CSRF anti-forgery on POST | DONE | `[ValidateAntiForgeryToken]` present on all relevant POST actions (`AdminController.cs:51,83`, `Buyer/DashboardController.cs:102`, `InspectionController.cs:33`, `Seller/InventoryController.cs:40`). | Audit any new POSTs added later. |
| NFR-S5 | Session 30-min idle expiry, Secure + HttpOnly | MISSING | No `services.ConfigureApplicationCookie` block in `Program.cs`. | Add cookie config: `ExpireTimeSpan = 30m`, `SlidingExpiration = true`, `Cookie.SecurePolicy = Always`, `Cookie.HttpOnly = true`, `Cookie.SameSite = Lax`. |
| NFR-S6 | All actions `[Authorize]` + role check | PARTIAL | Area controllers are decorated. Root controllers (`Home`, `Inspection`, `Advisor`, `Inventory`) are anonymous. `AdvisorController.Match` should require auth (currently leaks recommendations from a random profile). | Add `[Authorize]` to `AdvisorController.Match` and any action that depends on user context. |

### Usability

| ID | Requirement | Status | Current state | What's needed |
|---|---|---|---|---|
| NFR-U1 | AR (RTL) + EN, switchable from any page | MISSING | No `IStringLocalizer`/`.resx` files. No RTL CSS. Glossary stores `ExplanationAr/En`, but UI is English-only Razor markup. | Add ASP.NET Core localization (RequestLocalization + `.resx`), language-switch action, RTL Bootstrap stylesheet. |
| NFR-U2 | Responsive 320 → 1920 | PARTIAL | Stock Bootstrap layout (`Views/Shared/_Layout.cshtml`). Not audited. | Manual audit + media-query fixes. |
| NFR-U3 | Inspection terms shown with explanation | DONE/PARTIAL | `GlossaryTooltipViewComponent` exists. | Ensure used everywhere inspection terms appear. |
| NFR-U4 | Inline form validation in user's language | PARTIAL | Data annotations + jQuery validation present. Messages currently EN only. | Localize validation messages via `.resx` (depends on NFR-U1). |
| NFR-U5 | Multi-step progress bar | MISSING | No wizard yet (FR-2.1). | Built as part of FR-2.1. |

### Reliability

| ID | Requirement | Status | Current state | What's needed |
|---|---|---|---|---|
| NFR-R1 | 99.5% uptime | OPS | — | Hosting/ops concern, document in README. |
| NFR-R2 | Daily backups, 30-day retention | OPS | — | Document SQL Server maintenance plan / Azure backup config. |
| NFR-R3 | Friendly error page (no stack traces) | DONE | `app.UseExceptionHandler("/Home/Error")` non-dev (`Program.cs:95`). `HomeController.Error` returns `ErrorViewModel`. | Verify `Views/Shared/Error.cshtml` does not leak stack details. |

### Maintainability

| ID | Requirement | Status | Current state | What's needed |
|---|---|---|---|---|
| NFR-M1 | Business logic in services, thin controllers | PARTIAL | `Areas/Buyer/Controllers/DashboardController.cs:52-88` opens its own DbCommand. `AdvisorController.Match` runs scoring inline in the controller. | Move recommendation, inspection scoring, image processing, etc. into services. |
| NFR-M2 | XML doc comments, README | MISSING | No XML doc comments. No README at repo root. | Add `///` summaries on public methods; write `README.md`. |
| NFR-M3 | All schema via EF migrations | PARTIAL | Domain schema is migrated. `vw_AvailableCarDetails` + `GetCarMatchesForUser` are **not** in migrations. | Add idempotent SQL migrations for view + proc (the only acceptable raw-SQL migrations). |

### Scalability

| ID | Requirement | Status | Current state | What's needed |
|---|---|---|---|---|
| NFR-Sc1 | 12-per-page pagination | MISSING | All list queries `.ToListAsync()` with no skip/take. | Add pagination helper / `PaginatedList<T>` pattern. |
| NFR-Sc2 | Indexes on make/model/price/type/chassis/user_id FKs | PARTIAL | Existing indexes: `IX_Cars_Matching (transmission,body_type,year)`, `IX_CarListings_Availability`, `IX_CarListings_car_id`, `IX_CarListings_seller_id`, `IX_Users_Email`, `UQ__Users__Email`, `IX_RecommendationLog_user_id`, `IX_SavedResults_car_id` (`Migrations/20260528205536_InitialBaseline.cs:262-301`). Missing: indexes on `Cars.make`/`model`/`price`/`type`, chassis-score derived column. | Add missing indexes in a migration. |
| NFR-Sc3 | Azure App Service / IIS + SQL Server | OK | Standard ASP.NET Core 10.0 web project deploys to either. | Provide deployment doc. |

### Compatibility

| ID | Requirement | Status | Current state | What's needed |
|---|---|---|---|---|
| NFR-C1 | Browser support | OK | Stock Bootstrap 5 + jQuery work on listed browsers. | No action. |
| NFR-C2 | ASP.NET MVC 5 or Core 6+ | OK / NOTE | Built on .NET 10.0 (`CarFitProject.csproj:4`). Newer than spec floor, satisfies "Core 6+". | No action. |

---

## Cross-cutting issues that block multiple FRs

These are foundational gaps. Listed once here, then referenced from the implementation plan.

| # | Issue | FRs affected | Notes |
|---|---|---|---|
| X1 | Hardcoded connection string in `Models/CarFitDbContext.cs:42` shadows DI. | NFR-S2, NFR-M3 | Delete `OnConfiguring` or guard with `if (!optionsBuilder.IsConfigured)`. |
| X2 | Sync-over-async in `Program.cs:56-83` startup seeding (`.Result`, `.Wait()`). | NFR-R3 indirectly | Refactor to async-local `SeedAsync()` invoked once at startup. |
| X3 | `IRecommendationService` declared but not in DI. | FR-5.1-5.5 | Either register and use, or delete. |
| X4 | `vw_AvailableCarDetails` and `GetCarMatchesForUser` not in source. | FR-5.1-5.5, FR-3.5, FR-3.6 | Bring into migrations as idempotent SQL or replace with LINQ. |
| X5 | Dual user identity: legacy `Users` (int) vs `AspNetUsers` (GUID). `SavedResult.UserId` + `RecommendationLog.UserId` are ints; `UserProfile.UserId` is the Identity string. | FR-1.5, FR-2.9, FR-5, FR-6.1 | **Decision**: drop legacy `Users` entirely; migrate `SavedResult` + `RecommendationLog` keys to `nvarchar(450)` Identity IDs. |
| X6 | Seller ownership: `AddCar` doesn't set `SellerId` (`Areas/Seller/Controllers/InventoryController.cs:50-57`); no Identity↔Seller link; `Inventory.Index` returns all listings (line 21-29); `SellerController` missing `[Area("Seller")]` (`Areas/Seller/Controllers/SellerController.cs:10`); `SellerController.Index` compares int `SellerId.ToString()` to GUID `userId` (line 26). | FR-3.1, FR-3.3, FR-7.6 | Add `IdentityUserId nvarchar(450)` FK on `Seller`; populate on dealer onboarding; filter queries by it; fix `[Area]` attribute. |
| X7 | CSV BulkLoader naive split + abort-on-any-error semantics. | FR-7.2 | Replace with `CsvHelper`; per-row error reporting. |
| X8 | Admin password seeded in source (`Program.cs:63-64`). | NFR-S1 | Move to user secrets / env vars; fail startup if missing in non-dev. |
| X9 | Weak password policy (6 chars, no digit/upper/symbol, `Program.cs:31-36`). | NFR-S1 | Strengthen to 10+ with mixed-case + digit + symbol; pair with BCrypt decision in NFR-S1. |
| X10 | Spec uses "Dealer", code uses "Seller". | FR-1.5, FR-7.6 | Keep `Seller` as the role identifier (DB lock-in), display "Dealer" in UI text. Document in README. |
| X11 | Anonymous `AdvisorController.Match` reads first global active `UserProfile`. | NFR-S6, FR-5.1 | Add `[Authorize(Roles="Buyer")]` and filter by current user — or delete in favor of consolidated Buyer dashboard. |

---

## Summary counts

| Module | Done | Partial | Missing | Conflict |
|---|---|---|---|---|
| FR-1 Auth | 1 | 2 | 2 | 1 (Dealer naming) |
| FR-2 Questionnaire | 3 | 4 | 2 | — |
| FR-3 Listings | 0 | 5 | 1 | — |
| FR-4 Inspection | 0 | 3 | 3 | — |
| FR-5 Recommendation | 0 | 2 | 3 | 1 (50/30/20 weights vs current 40/30/30) |
| FR-6 User actions | 0 | 2 | 3 | — |
| FR-7 Admin | 1 | 3 | 2 | — |
| NFR Performance | 0 | 0 | 1 | 3 unknown |
| NFR Security | 1 | 3 | 1 | 1 (BCrypt vs PBKDF2) |
| NFR Usability | 0 | 3 | 2 | — |
| NFR Reliability | 1 | 0 | 0 | 2 ops |
| NFR Maintainability | 0 | 2 | 1 | — |
| NFR Scalability | 0 | 1 | 1 | 1 ok |
| NFR Compatibility | 2 | 0 | 0 | — |

**Headline gaps that gate launch (must-have, P0):**
1. Foundation fixes X1–X11 (see implementation plan Phase 0).
2. Real recommendation engine with versioned view/proc and the spec's 50/30/20 weights.
3. Inspection scoring service + UI for entry & display with glossary tooltips.
4. Dealer ownership chain (Identity↔Seller, AddCar, Edit, list filtering, approval).
5. Multi-step questionnaire wizard.
6. Save Car flow on the Identity user side.
7. Bilingual AR/EN with RTL.
8. Session + cookie + password-policy hardening; BCrypt decision.
