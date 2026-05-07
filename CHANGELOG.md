# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased] - 2026-05-07

### Added
- My Reviews page at `/reviews/mine`, with a sidebar entry and read-only review cards showing the reviewed GPU, owner, rating, comment, review date, receipt link, and GPU detail link
- Rental review submission from Rental History: completed, unreviewed rentals now open an inline review modal with star rating buttons and optional comments, backed by renter/completed/one-review-per-rental validation and `ReviewReceived` owner notifications
- Expired-rental lifecycle sync service that marks elapsed active rentals as `Completed`, restores the GPU to `Available`, and emits completion notifications; Active Rentals, Rental History, rental receipts, owner Rented GPUs, and the background notifier all use the same lifecycle path
- Active status filter on Rental History so renters can include active rentals in the same history table/filter workflow
- Rental receipt view linked from Rental History, with renter-scoped receipt lookup and a dedicated receipt card showing payment, GPU, status, start/end timestamps, platform fee, owner earnings, and total paid
- Active rental early termination flow with a confirmation modal, renter ownership/status validation, `TerminatedEarly` rental updates, GPU availability restoration, and no refund/reversal transactions
- `NotificationType.RentalTerminated` with distinct notification list/dashboard icon and badge styling so terminated rentals are not categorized as completed
- `NotificationType.VerificationApproved` / `NotificationType.VerificationRejected` with matching message templates and icon/badge classes; admin approval or rejection on `/admin/verifications` now stages an in-app notification (linking to `/verification`) inside the same `SaveChangesAsync` as the status change, and admin reviewer notes are passed through to the notification message when present
- `NotificationType.Welcome` plus an initial-credit notification staged during signup: every new account now gets a welcome notification and a "received 500 credits" notification committed atomically with the user insert and credit ledger row, both linking to `/dashboard` (Welcome's timestamp is bumped one tick so it sorts above the credit notification on `/notifications`)
- Member-facing GPU catalog, detail, rental confirmation, and active-rentals flow
- Transactional rental creation with upfront renter charge, owner earnings after platform fee, credit ledger rows, GPU status updates, and rental notifications
- `NotificationType.CreditRevoked` with `CreditRevokedFormat` template plus matching icon/badge classes; admin credit revocations now stage an in-app notification (linking to `/dashboard`) inside the same transaction as the ledger entry, mirroring the existing grant flow
- `/admin/searchmembers` JSON endpoint backing a server-side, debounced, top-20 search of non-admin users (by name/username/email, with current credit balance) used by the redesigned admin credit pickers
- Member dashboard overview backed by `IDashboardService` / `DashboardService`, showing credit balance, active rental count, current-month spend, lifetime compute hours, active rental previews, and recent notification previews
- Dashboard-specific view models and `DashboardConstants` preview limits so the page can integrate with rentals, GPU browsing, and notifications without depending on those modules' UI models
- `RentalExpiryNotifier` background hosted service that polls every 2 minutes for active rentals ending within 1 hour and stages a `RentalExpiring` notification linking the renter to `/rentals/active`; an `ExpiryNotifiedAt` stamp on `Rental` (added via the `AddRentalExpiryNotifiedAt` EF migration) makes the dispatch idempotent so no rental is warned twice
- In-app notifications page at `/notifications` showing the signed-in user's most recent 100 notifications as cards with type-specific icons, status badges, relative timestamps, an "Open" action that marks-as-read and redirects to the notification's link, per-row mark-as-read, and a header-level "Mark all as read" action
- Notifications bell in the authenticated app header with an unread-count badge that links to `/notifications`; rendered via the new `NotificationBell` view component so the count refreshes on every authenticated page load
- Automatic notification creation on admin approval/rejection of a GPU listing (linking the owner to `/gpus/mine`) and on admin credit grants — single or bulk — linking the recipient to `/dashboard`; each notification is staged before the existing `SaveChangesAsync`/transaction so it commits atomically with the source action
- `INotificationService` / `NotificationService` with a staging-only `Create`, a paged read query, mark-as-read / mark-all-as-read (single-statement EF Core 8 `ExecuteUpdateAsync`), and a click-through `Open` action that uses `Url.IsLocalUrl` to reject non-local link redirects
- `Constants/NotificationConstants.cs` for status TempData keys, route targets, message format strings, and `Notification` length limits
- Owner-side "My Listings" page at `/gpus/mine` that shows each of the signed-in user's GPUs as a card with photo, model, status badge (PENDING REVIEW / LIVE / RENTED / PAUSED / REJECTED), specs, rental count, average rating, and (for rejected listings) the admin's rejection reason
- Inline owner actions on `/gpus/mine`: an Available toggle that flips a listing between `Available` and `Maintenance` (`POST /gpus/{id}/toggle-status`), an edit shortcut, and a delete action (`POST /gpus/{id}/delete`) that refuses listings with rental or review history and cleans up the photo file from disk on success
- "Edit Listing" page at `/gpus/{id}/edit` that reuses the List a GPU form fields for hardware, pricing, description, and an optional photo replacement (the previous photo is deleted only after the database update succeeds)
- Empty-state CTA on `/gpus/mine` for owners with no listings yet, plus a "+ New Listing" header button that links straight to `/gpus/create`
- Admin dashboard at `/admin/dashboard` with at-a-glance cards (pending verifications, pending listings, active/suspended users, credits in circulation) and a recent admin credit-activity table
- Admin user management at `/admin/users` with name/username/email search, role/status/verification filters, paged list, and `/admin/users/{id}` detail view showing recent credit transactions and owned GPUs
- Admin user actions: suspend/reactivate (with self-action and admin-on-admin guards) and a manual `IsOwnerVerified` toggle that bypasses the verification queue
- Admin credit management at `/admin/credits`: paged ledger with user/type/date filters, single-user grant form, bulk member grant (active-only by default, wrapped in a DB transaction), and revoke form (rejects amounts that would drive a balance below zero); all writes log a `CreditTransaction` with `AdminId`/`Reason`
- Admin listing moderation at `/admin/listings`: status-grouped queue (with counts) and `/admin/listings/{id}` detail view; approve flips `Pending → Available`, reject flips `Pending → Rejected` and stores the new `Gpu.RejectionReason`
- Admin platform analytics at `/admin/analytics`: total/active/suspended users, credits in circulation, total credits granted/revoked, listing breakdown by status, and top earners by `RentalEarning` credit transactions
- `IAdminDashboardService`, `IAdminUserService`, `IAdminCreditService`, `IAdminListingService`, `IAdminAnalyticsService` implementations and corresponding view models under `ViewModels/Admin/`
- `Constants/AdminConstants.cs` for routes, TempData keys, validation limits (1–100 000 credits, 5–500 char reasons), and admin status messages
- Role-aware admin nav links (Dashboard / Users / Credits / Listings / Verifications / Analytics) on `_Navigation.cshtml`, shown only to users in the `Admin` role
- Owner-verification ID image upload: `/verification` form now accepts a JPG/PNG/WebP photo (max 4 MB) saved to `wwwroot/uploads/verification/`, with the image rendered in `/admin/verifications` table thumbnails and inside the approve/reject modals; new `OwnerVerificationRequest.IdentityImagePath` and `Gpu.RejectionReason` columns persisted via the `AddVerificationIdImageAndGpuRejectionReason` EF migration
- Owner verification request flow at `/verification`: authenticated members submit a justification (30–2000 chars) that admins review at `/admin/verifications`, with approve/reject modals that capture optional reviewer notes; approval flips `ApplicationUser.IsOwnerVerified`
- "List a GPU" form at `/gpus/create` with Hardware (brand, model, VRAM, CUDA cores), Pricing & Availability (price per hour, min rental hours, description), single photo upload, and an Approval Checklist sidebar; submissions are persisted as `Pending` GPUs awaiting admin approval
- GPU photo upload pipeline (`wwwroot/uploads/gpus/`) reusing the avatar pattern, with JPG/PNG/WebP allowlist and 5 MB size cap
- `IVerificationService` / `VerificationService` and `IGpuService` / `GpuService` returning the existing `ServiceResult` shape, plus `Constants/VerificationConstants.cs` and `Constants/GpuConstants.cs` for messages, file limits, and TempData keys
- `OwnerVerificationRequest` entity (with `OwnerVerificationStatus` enum) and `Gpu.MinRentalHours` column, persisted via the `AddOwnerVerificationAndGpuMinRentalHours` EF migration
- Locked-state CTA on `/gpus/create` for unverified members that surfaces "Become a verified owner" with state-aware messaging (no prior request, pending review, or previously rejected)
- Profile page with avatar upload, editable Personal Information form (first name, last name, username, email, bio), and Change Password form, replacing the placeholder
- Profile picture upload pipeline (`wwwroot/uploads/profiles/`) with extension and 2 MB size validation, automatic cleanup of replaced files, and a `profile_picture_path` claim that lets the sidebar render the uploaded image
- `IProfileService` / `ProfileService` for profile updates, password changes, and avatar uploads, returning the existing `ServiceResult` shape
- Global app header on authenticated pages with a search input, notifications icon button, theme switcher, and Sign Out button that triggers the existing logout confirmation modal

### Changed
- Review UX now stays in Rental History instead of navigating to a separate full-page review form; invalid modal submissions return to history with TempData feedback
- Active rental countdown pages now refresh shortly after a timer reaches zero so completed rentals disappear from active views without waiting for the background polling interval
- Standardized remaining rental/listing price labels and admin picker balance snippets to credits instead of mixed currency wording
- Active Rentals now updates each running rental timer and usage progress live in the browser, replaces the disabled termination placeholder with an actionable CTA, and clamps expired cards at `00:00:00` / `100% used`
- Rental History now renders persisted rental rows with status filters and an enabled Receipt action instead of the placeholder history page
- GPU catalog cards are now fully clickable, with clearer owner, spec, rating, and per-hour pricing presentation
- Unread notification cards now use a slimmer left border treatment
- Render the Type column on `/admin/credits` as color-coded Bootstrap badges (Initial/Admin grant/Rental charge/Rental earning/Refund/Revoke) using the existing `bg-{variant}-subtle text-{variant}` style, with friendlier spaced labels
- Polished GPU catalog, detail, create, and edit listing pages, including owner-specific listing actions and cleaner upload controls
- Restricted member-facing pages to member accounts so admin sessions cannot use member listing or rental workflows
- Rebuilt `/admin/grantcredits` and `/admin/revokecredits` as a stepped flow: search-and-pick a user (each result row shows their current credit balance), then reveal Amount and Reason. The Selected User panel shows the chosen user's current balance and a live "new balance after" preview that recomputes on every keystroke; revoke also surfaces an "Amount exceeds the user's current balance" warning that mirrors the server-side check. Deep links from `/admin/users/{id}` (`?userId=...`) and validation re-renders both land directly on the second step
- Replaced the flat per-page-button pager on `/admin/credits` with a windowed pagination (`‹ Prev | 1 … current±2 … last | Next ›`) plus a "Showing X–Y of Z transactions" counter; Prev/Next are disabled at the edges and all filter state is preserved across page links
- Replaced the placeholder member dashboard with a responsive card-based overview UI that preserves the authenticated app shell theme and links out to Browse GPUs, Active Rentals, and Notifications
- Signup now writes an `Initial` `CreditTransaction` for the 500-credit welcome balance so every user's ledger has an opening row that matches `ApplicationUser.CreditBalance`; the user insert and ledger row commit in the same `SaveChangesAsync` call
- Replaced the static notifications bell `<button>` in `_AppHeader.cshtml` with the new `NotificationBell` view component, and added a Notifications link in the authenticated sidebar Platform nav between History and Profile
- Split `ApplicationUser.FullName` into separate `FirstName` and `LastName` columns (with `FullName` retained as a `[NotMapped]` computed property); EF migration backfills existing rows
- Signup form and `AuthService.SignupAsync` now collect first and last names separately
- Authentication cookie now carries an additional `profile_picture_path` claim and is re-issued automatically when the profile, username, email, or avatar changes
- Sidebar avatar shows the user's uploaded profile picture when available, falling back to initials
- Relocated the theme switcher from a floating top-right button into the new app header action group
- Authenticated main content area now shares the header's surface color for visual continuity with the header
- Active Rentals cards now align column edges across rows by laying the row out as a 3-track CSS grid (`320px / 1fr / auto`) at xl widths, so Time Left, progress bars, dates, and the terminate button share the same x-axis regardless of owner-name length; long owner names truncate with ellipsis inside the fixed left column

### Fixed
- Duplicate `NotificationType` enum value where `RentalTerminated` and `VerificationApproved` both equaled `8`, which made later switch arms in `NotificationItemViewModel` (icon, badge label, badge css) unreachable and broke the build with `CS8510`; reassigned `RentalTerminated` to `11` while keeping `VerificationApproved`, `VerificationRejected`, and `Welcome` on their existing values

## [Unreleased] - 2026-05-06

### Added
- Authenticated app shell with left sidebar (Dashboard, Browse GPUs, Active Rentals, History, Profile, My Listings, List a GPU) and a slide-in offcanvas drawer on mobile
- Placeholder pages for browsing GPUs, owner listings, listing a GPU, active rentals, rental history, and member profile
- Docker Compose dev configuration for SQL Server 2022 local database

### Changed
- Member dashboard moved onto the new app shell with a welcome headline and summary cards for active rentals, listings, and credit balance

## [Unreleased] - 2026-05-04

### Added
- Backend authentication with signup, login, logout, access-denied handling, and cookie-based sessions
- Restored member login and signup pages with shared authentication layout, model-state alerts, auth styling, and logo asset
- Member dashboard page protected by authentication
- Admin login and dashboard pages protected by admin role authorization
- Authentication ViewModels for email/username login and signup server-side validation
- EF Core migration adding password hash storage to users
- EF Core migration adding user role storage to users
- Development admin seeder for local admin account provisioning
- User secrets configuration for local project settings

### Changed
- Refactored authentication logic out of `AuthController` into `AuthService`
- Reworked member authentication to support email or username with password
- Updated signup to collect and validate unique usernames before redirecting to login without automatically signing in
- Replaced the custom password hasher with ASP.NET Core Identity password hashing
- Added role-aware member and admin login flows with clean admin, member, and access-denied redirects
- Added development-only automatic migrations before admin seeding on startup
- Updated the development admin seeder to support a configured username and fail on identity conflicts
- Configured authentication services and middleware in the application startup flow

### Removed
- Custom authentication password hasher service and interface

## [Unreleased] - 2026-05-03

### Added
- Landing page with hero content, marketplace metrics, how-it-works steps, credit system overview, FAQ accordion, call-to-action section, navigation, and footer
- Landing page visual polish including a scoped gradient background, dark-mode accordion colors, and filled card backgrounds for metric and process cards
- Bootstrap color mode toggle with light, dark, and auto options persisted in local storage
- Bootstrap Icons static assets through LibMan
- Placeholder login and signup pages with a shared authentication layout

### Changed
- Updated Bootstrap static assets from 5.1 to 5.3 to support color mode theming
- Moved MVC and routing service setup into `ServiceExtensions`
- Configured routing to generate lowercase URLs and query strings
- Updated the default route to use the landing page as the application entry point

### Removed
- Unused default MVC template home controller, home views, privacy view, and scoped layout stylesheet

## [Unreleased] - 2026-05-02

### Added
- EF Core `AppDbContext` table mappings, relationship configuration, and uniqueness constraints for the core marketplace schema
- Initial EF Core migration for the CloudCompute database
- DBML database ERD source for rendering the schema in dbdiagram.io
- Updated .gitignore file

## [0.3.0] - 2026-05-01

### Added
- Core marketplace domain models for users, GPU listings, rentals, credit transactions, reviews, and notifications
- Domain enums for GPU status, rental status, credit transaction types, and notification types
- Entity relationship documentation with Mermaid ERD, relationship summary, and business rules

## [0.2.0] - 2026-04-23

### Added
- Entity Framework Core package references to project file
- README.md with project details

## [0.1.0] - 2026-04-23

### Added
- Initial project setup using ASP.NET Core MVC (.NET 8)
- Home controller with default Index and Privacy actions
- Shared layout with Bootstrap 5, jQuery, and jQuery Validation
- Error view model and error handling middleware
- Static web assets (CSS, JS, favicon)
