# CarFit — Smart Car Recommendation Platform
## Functional & Non-Functional Requirements Specification

| | |
|---|---|
| **Project Name** | CarFit — Car Recommendation System |
| **Technology** | ASP.NET MVC, SQL Server, C# |
| **Target Market** | Jordan — First-time buyers, women buyers, non-technical users |
| **Document Type** | Functional & Non-Functional Requirements Specification |
| **Version** | 1.0 — 2025 |

---

## 1. Project Overview

CarFit is a smart car recommendation web application built for the Jordanian market. Unlike traditional car listing websites, CarFit asks users a set of personal and lifestyle questions and uses their answers to match them with the most suitable car from the database. The platform places special emphasis on the Jordanian car inspection report (ورقة الفحص) for used cars, scoring each car's condition and presenting the results in plain Arabic and English so that any buyer — regardless of technical knowledge — can make a confident, informed decision.

### 1.1 Key Users
- **Buyers** — individuals searching for a car that fits their life and budget
- **Dealers / Sellers** — businesses and individuals who list cars for sale
- **Admin** — platform administrators who manage content, users, and listings

### 1.2 Core Value Proposition
- Recommends the right car based on the user's personal profile, not just price
- Scores and explains ورقة الفحص (the white card) in simple language
- Protects inexperienced buyers from purchasing damaged or overpriced vehicles
- Fully bilingual: Arabic (RTL) and English

---

## 2. Functional Requirements

Priority levels: **High** = must have at launch, **Medium** = important but deferrable, **Low** = future enhancement.

### FR-1: User Authentication & Account Management

| ID | Requirement | Description | Priority |
|---|---|---|---|
| FR-1.1 | User Registration | Users can register with name, email, and password. Email must be unique. Password stored hashed. | High |
| FR-1.2 | User Login / Logout | Registered users log in with email and password. Sessions are maintained securely until logout. | High |
| FR-1.3 | Password Reset | Users can request a password reset via email link with an expiry of 30 minutes. | Medium |
| FR-1.4 | Profile Management | Users can view and update their personal info (name, email, preferences) from a profile settings page. | Medium |
| FR-1.5 | Role-Based Access | System supports three roles: Buyer, Dealer, and Admin. Each role sees different menus and pages. | High |

### FR-2: User Profile Questionnaire

| ID | Requirement | Description | Priority |
|---|---|---|---|
| FR-2.1 | Step-by-Step Form | Profile questionnaire is split into multiple steps (not one long page). User can go back and edit previous answers. | High |
| FR-2.2 | Age Input | User enters their age. System uses age to influence car size and type recommendations. | High |
| FR-2.3 | Marital Status | User selects Single or Married. Affects family-size car recommendations. | High |
| FR-2.4 | Children | User selects Yes/No for kids and enters count. Influences seat count and safety feature weighting. | High |
| FR-2.5 | Car Purpose | User selects from: Work, University, Family use, Travel. Each maps to different car type priorities. | High |
| FR-2.6 | Trip Type | User selects Short (city) or Long (highway/intercity). Affects fuel efficiency and engine size weighting. | High |
| FR-2.7 | Budget & Payment | User enters budget range in JD and selects Cash or Installments. If installments, enters preferred number of months. | High |
| FR-2.8 | Car Preference | User selects New, Used, or No Preference. Also selects preferred transmission (Auto/Manual) and size (Small/Medium/SUV). | High |
| FR-2.9 | Save Profile | User's profile answers are saved to their account and can be updated at any time. | Medium |

### FR-3: Car Database & Listings

| ID | Requirement | Description | Priority |
|---|---|---|---|
| FR-3.1 | New Car Listings | Admin and dealers can add new car listings with: make, model, year, price, engine, fuel type, transmission, seats, size, images. | High |
| FR-3.2 | Used Car Listings | Used car listings include all new car fields plus a linked inspection report (FR-4). | High |
| FR-3.3 | Listing Management | Dealers can edit, deactivate, or delete their own listings. Admin can manage all listings. | High |
| FR-3.4 | Image Upload | Each listing supports multiple image uploads (minimum 3, maximum 15 photos). | Medium |
| FR-3.5 | Listing Status | Listings can be: Active, Sold, or Pending Review. Only Active listings appear in recommendations. | High |
| FR-3.6 | Search & Filter | Users can manually search and filter cars by make, model, year, price range, type (new/used), and transmission. | High |

### FR-4: Inspection Report System (ورقة الفحص)

| ID | Requirement | Description | Priority |
|---|---|---|---|
| FR-4.1 | Inspection Data Entry | Admin/dealer enters inspection data for used cars: inspection center, date, chassis 1–4 condition, engine %, gearbox, body, roof, paint/filler. | High |
| FR-4.2 | Chassis Score Enum | Each of the 4 chassis points is rated using the official Jordanian scale: دقة على الرأس، قصعة شنكل، جيد، مضروب ومشغول، مضروب، ضربة رأسية، ضربة على الرأس، شاصي مقصوص ومغير. | High |
| FR-4.3 | Auto Score Calculation | System automatically calculates the overall inspection score (e.g. جيد 7 or جيد 4) based on chassis inputs. Cars with شاصي مقصوص are flagged as Risky. | High |
| FR-4.4 | Plain Language Display | Each inspection term is displayed in simple Arabic and English with an explanation (e.g. مضروب = Heavy chassis damage — price significantly affected). | High |
| FR-4.5 | Engine & Gearbox | Engine health stored as a percentage. System flags: 60%+ = Good, 50–55% = Weak, smoke present = Unsafe. Gearbox: Good or Knocking. | High |
| FR-4.6 | CarSeer Integration | Admin can mark whether a CarSeer report is attached to the listing. This is shown as a badge on the listing card. | Medium |

### FR-5: Recommendation Engine

| ID | Requirement | Description | Priority |
|---|---|---|---|
| FR-5.1 | Profile-Based Query | After the user completes the questionnaire, the system queries the database for cars that match their budget, purpose, trip type, transmission, and size preference. | High |
| FR-5.2 | Inspection Scoring | Used cars are scored based on their ورقة الفحص: جيد chassis = no penalty, minor hits = small penalty, مضروب = high penalty, مقصوص = flagged as Risky. | High |
| FR-5.3 | Combined Ranking | Final ranking combines: profile match score (50%) + inspection quality (30%) + budget fit (20%). Top 3–5 cars are returned. | High |
| FR-5.4 | No Results Handling | If no exact match is found, system relaxes filters (e.g. widens budget by 10%) and notifies the user of the relaxation applied. | Medium |
| FR-5.5 | Result Explanation | Each recommended car shows why it was recommended (e.g. "Matches your family size and budget — inspection score: جيد 7"). | High |

### FR-6: User Actions on Results

| ID | Requirement | Description | Priority |
|---|---|---|---|
| FR-6.1 | Save Car | Logged-in users can save up to 3 cars (free) or unlimited (premium). Saved cars appear in the user's dashboard. | High |
| FR-6.2 | Compare Cars | Users can select 2–3 cars and view a side-by-side comparison of specs, price, and inspection score. | Medium |
| FR-6.3 | Contact Seller | Each listing shows a WhatsApp button and an email button to contact the seller directly. Free users see WhatsApp only. | High |
| FR-6.4 | Book Mechanic | Users can request a mechanic visit for a shortlisted car before purchase. System shows available mechanics by location. | Low |
| FR-6.5 | Share Listing | Users can copy a link to any car listing and share it externally. | Low |

### FR-7: Admin Panel

| ID | Requirement | Description | Priority |
|---|---|---|---|
| FR-7.1 | User Management | Admin can view, activate, deactivate, or delete any user account. | High |
| FR-7.2 | Listing Management | Admin can add, edit, approve, or remove any car listing. Admin can also bulk import listings via CSV. | High |
| FR-7.3 | Inspection Management | Admin can add and edit inspection reports linked to any used car listing. | High |
| FR-7.4 | Glossary Management | Admin can add and edit the inspection terms glossary that explains each فحص term to buyers. | Medium |
| FR-7.5 | Analytics Dashboard | Admin sees: total users, total listings, most recommended cars, top search terms, and monthly new registrations. | Medium |
| FR-7.6 | Dealer Management | Admin can approve or reject dealer registrations and assign subscription tier (Basic, Standard, Premium). | High |

---

## 3. Non-Functional Requirements

Non-functional requirements define the quality standards the system must meet beyond its features. They determine how the system behaves under real-world conditions.

| Category | Requirement | Metric / Standard |
|---|---|---|
| Performance | Page load time | All pages load within 3 seconds on a standard 4G connection |
| Performance | Recommendation response time | Results returned within 2 seconds of form submission |
| Performance | Concurrent users | System supports at least 200 simultaneous users without degradation |
| Performance | Image optimization | All listing images served in WebP format, max 200KB each |
| Security | Password hashing | All passwords stored using BCrypt with minimum cost factor of 12 |
| Security | HTTPS enforcement | All traffic served over HTTPS/TLS 1.2 or higher. HTTP redirects to HTTPS |
| Security | SQL injection prevention | All database queries use parameterized statements. No raw SQL with user input |
| Security | CSRF protection | All POST forms include anti-forgery tokens (ASP.NET AntiForgeryToken) |
| Security | Session management | Sessions expire after 30 minutes of inactivity. Secure and HttpOnly cookie flags set |
| Security | Role authorization | All controller actions decorated with [Authorize] and role checks. Unauthenticated access redirected |
| Usability | Bilingual support | Full Arabic (RTL) and English (LTR) support. User can switch language from any page |
| Usability | Mobile responsive | UI renders correctly on screens from 320px (mobile) to 1920px (desktop) |
| Usability | Accessible language | All inspection terms shown with plain-language explanation. No jargon without definition |
| Usability | Form validation feedback | All form errors shown inline next to the relevant field in the user's selected language |
| Usability | Step indicator | Multi-step questionnaire shows a progress bar so users know how many steps remain |
| Reliability | Uptime target | System targets 99.5% uptime per month (approx. 3.6 hours max downtime/month) |
| Reliability | Data backup | Automated daily database backups retained for 30 days |
| Reliability | Graceful error handling | All unhandled exceptions return a friendly error page (no raw stack traces shown to users) |
| Maintainability | MVC separation | Business logic in services layer. Controllers thin. No business logic in views |
| Maintainability | Code documentation | All public methods and classes have XML doc comments. README covers setup and deployment |
| Maintainability | Database migrations | All schema changes managed via Entity Framework Migrations (no manual SQL scripts) |
| Scalability | Pagination | All listing pages paginated at 12 items per page. No unlimited result sets returned |
| Scalability | Database indexing | Indexes on: car make/model, price, type (new/used), chassis score, user_id foreign keys |
| Scalability | Hosting environment | Deployable to Azure App Service or any Windows Server with IIS and SQL Server |
| Compatibility | Browser support | Supports Chrome 100+, Firefox 100+, Safari 15+, Edge 100+ |
| Compatibility | Framework | Built on ASP.NET MVC 5 (.NET Framework 4.8) or ASP.NET Core MVC 6+ |

---

## 4. Constraints & Assumptions

### 4.1 Technical Constraints
- The system is built exclusively using ASP.NET MVC with C# as the server-side language.
- The database is Microsoft SQL Server. Entity Framework is used as the ORM.
- Frontend uses HTML5, CSS3, Bootstrap 5, and jQuery. No separate frontend framework (e.g. React) is used.
- File storage for car images uses local server storage or Azure Blob Storage.
- No third-party AI/ML library is used in v1.0. The recommendation engine is rule-based scoring logic.

### 4.2 Business Constraints
- The platform is scoped to the Jordanian car market only (v1.0).
- All prices are displayed in Jordanian Dinar (JD). No multi-currency support in v1.0.
- Inspection report data must be entered manually by admin or dealer. OCR scanning is a future feature.
- The platform does not process payments directly. Installment calculations are informational only.

### 4.3 Assumptions
- Dealers will be onboarded manually and trained to enter inspection report data accurately.
- Users are assumed to have basic smartphone or computer literacy.
- Inspection terminology used follows the standard Jordanian market conventions as documented in this specification.
- Internet connectivity is assumed. No offline mode is required in v1.0.

---

## 5. Inspection Term Glossary

The following table defines all official Jordanian car inspection terms used in the system. Each term is stored in the `InspectionTermsGlossary` database table and displayed to buyers in plain language.

| Arabic Term | English Translation | Buyer Explanation |
|---|---|---|
| جيد | Good | No damage. No penalty on score or price. |
| قصعة شنكل | Loading dent | Very minor dent from shipping. Near-zero effect on safety or value. |
| دقة على الرأس | Light head tap | Minor front/rear hit. Chassis bent ~1cm. Minimal impact on price. |
| ضربة على الرأس | Head hit | Front/rear hit. Chassis bent 3–5cm. Small price impact. |
| ضربة رأسية | Direct head blow | Chassis bent 5–10cm. Slight safety concern. Tens of JD price reduction. |
| مضروب | Damaged | Heavy accident. Chassis badly bent. Needs repair. Hundreds of JD reduction per chassis. |
| مضروب ومشغول | Damaged & repaired | Was badly damaged but repaired. Safety still a concern. Significant price reduction. |
| شاصي مقصوص ومغير | Chassis cut & replaced | Chassis was cut and replaced. FLAGGED as Risky. Requires official technical inspection. |
| خالي قص قلبان | Complete write-off | All 4 chassis points damaged. Car is considered structurally compromised. |
| دخان أزرق/أبيض | Blue/white smoke | Engine problem. Indicates oil or coolant burn. Expensive to fix. |
| طقطقة أكس | Knocking axle | Gearbox/axle joint issue. Sign of wear. Cost to repair varies by model. |

---

*CarFit v1.0 — Requirements Specification Document*
*Built with ASP.NET MVC for the Jordanian car market • All inspection terminology based on official Jordanian vehicle inspection standards*
