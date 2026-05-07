# GPURent spec gap checklist

Tracks remaining work to fully satisfy `CloudCompute-spec.pdf`. Grouped by spec section. Check items off as PRs land.

## 5.1 Account Management

- [x] **Account deletion** — add `DeleteAccountAsync` to `IProfileService` + `ProfileController` action + confirm-modal view. Soft vs hard delete TBD; if soft, set `IsActive = false` and anonymize PII while preserving rental/credit history (FKs are `Restrict`).
- [x] **Suspension enforcement on rent path** — gate `RentalService.CreateAsync` on `Renter.IsActive`; return form-level error if suspended.
- [x] **Suspension enforcement on listing path** — gate `GpuService.CreateAsync` on `Owner.IsActive`; same treatment.

## 5.2 GPU Listing Module (Owner)

- [x] **Add `Unavailable` to `GpuStatus` enum** — spec lists Available / Unavailable / Maintenance as the owner-controllable toggle states. Add the enum value, EF migration, and wire it into the toggle UI alongside Maintenance.
- [x] **Owner Earnings Dashboard** — new `OwnerEarningsService` + `Owner/Earnings` view. Must show:
  - Total credits earned all-time (sum of `CreditTransaction` where `Type == RentalEarning` and `UserId == owner`)
  - Earnings per GPU (group by `Rental.GpuId`, sum `OwnerEarnings`)
  - Pending payouts (sum `OwnerEarnings` from `Rental.Status == Active` for this owner)
  - Earnings chart, last 7 / 30 days (Chart.js, daily buckets)
- [x] **Surface earnings in nav** for verified owners.

## 5.3 GPU Rental Module (Renter)

- [ ] **Catalog sorting** — add `Sort` enum to `GpuCatalogFilter` (PriceAsc, PriceDesc, RatingDesc, Newest). Replace hardcoded `OrderByDescending(CreatedAt)` in `GpuService.GetCatalogAsync`.
- [ ] **Catalog filtering** — add to filter VM and apply in service:
  - GPU model (multi-select dropdown of distinct models)
  - Price range (min/max credits per hour)
  - VRAM (min GB)
  - Availability (Available only / include all)
- [ ] **Catalog pagination** — 12 GPUs per page; add `Page` + `PageSize` to filter VM, return `TotalCount` + `TotalPages` in `GpuCatalogViewModel`, add Bootstrap pagination partial.
- [ ] **Lazy-loaded GPU images** — add `loading="lazy"` to `<img>` in catalog cards and detail page.
- [ ] **Extend rental** — `RentalService.ExtendAsync(renterId, rentalId, additionalHours)`:
  - Validate active rental, balance covers extra cost, total duration ≤ 168h
  - Wrap in DB transaction: deduct credits, log `RentalCharge`, log owner `RentalEarning` (90/10), bump `EndTime` and `DurationHours` and `TotalCost`/`PlatformFee`/`OwnerEarnings`
  - Notify renter + owner
  - Add "Extend" button + modal on Active Rentals view
- [ ] **Prorated early termination refund** — extend `RentalService.TerminateAsync`:
  - Compute used hours = `ceil((now - StartTime).TotalHours)` (or per-minute proration — pick and document)
  - `refund = TotalCost - (usedHours * PricePerHour)`; clamp ≥ 0
  - Inside the existing transaction: credit renter, debit owner (or only refund the unused share of `OwnerEarnings`), insert `CreditTransaction` rows of type `Refund` for renter and `Revoke` for owner with `RelatedRentalId`
  - Update `Rental.TotalCost` / `OwnerEarnings` / `PlatformFee` to reflect actual usage
  - Cover with a unit/integration test if a test project exists
- [ ] **Rental history search by reference number** — extend the search predicate in `RentalService.GetHistoryAsync` to also match `ReferenceNumber`.
- [ ] **Rental history date-range filter** — add `DateFrom` / `DateTo` to `RentalHistoryFilterViewModel` and apply on `StartTime`.
- [ ] **Rental history GPU model filter** — spec calls this out separately from search; add `GpuModel` filter.

## 5.4 Review & Rating System

- [ ] **Reviews on owner profile** — add a "Reviews" tab/section to the public profile view aggregating reviews across the owner's GPUs.
- [ ] **Admin delete review** — `IReviewService.DeleteAsync(adminId, reviewId, reason)` callable only from admin controller; add a "reported" flag or just expose delete on the admin reviews list. Owners must not have a delete path.

## 5.5 Admin Dashboard

- [ ] **Force-terminate any rental** — admin-scoped variant of terminate (no `RenterId` check). Reuse the prorated-refund logic from the renter path so credits balance.
- [ ] **Flag inappropriate listings** — add `IsFlagged` + `FlagReason` to `Gpu`, admin action to flag, surface flagged listings in moderation queue.
- [ ] **User activity log** — extend the per-user admin view beyond credit transactions: include rentals (as renter and as owner) and listings created, ordered by timestamp.
- [ ] **Analytics: daily/weekly rental trends** — Chart.js line chart, last 30 days of rental counts.
- [ ] **Analytics: platform fee revenue** — sum `Rental.PlatformFee` across all rentals; display alongside total owner earnings.
- [ ] **Analytics: most rented GPUs** — top N list by rental count.

## 5.6 Notifications

All six notification types are implemented. No work here unless we add new flows (e.g., extension confirmation should also notify).

## Cross-cutting

- [ ] **Update `CHANGELOG.md`** per change, following Keep a Changelog format.
- [ ] **EF migrations** for schema changes (Unavailable enum value, IsFlagged on Gpu, any new fields on Rental for refund accounting).
- [ ] **Smoke-test the demo script** end-to-end after merging — landing → register → browse (with new sort/filter/paginate) → detail → rent → active rentals (with extend) → terminate (with refund) → owner earnings → admin grant + analytics → history → profile.
