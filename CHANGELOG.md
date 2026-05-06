# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased] - 2026-05-07

### Added
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
- Split `ApplicationUser.FullName` into separate `FirstName` and `LastName` columns (with `FullName` retained as a `[NotMapped]` computed property); EF migration backfills existing rows
- Signup form and `AuthService.SignupAsync` now collect first and last names separately
- Authentication cookie now carries an additional `profile_picture_path` claim and is re-issued automatically when the profile, username, email, or avatar changes
- Sidebar avatar shows the user's uploaded profile picture when available, falling back to initials
- Relocated the theme switcher from a floating top-right button into the new app header action group
- Authenticated main content area now shares the header's surface color for visual continuity with the header

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
