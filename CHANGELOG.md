# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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