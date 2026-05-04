# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
