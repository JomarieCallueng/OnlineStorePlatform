# Contributions

> ⚠️ **Team: you MUST complete the Owner column with real names before submitting.**
> The rubric scores an individual contribution component and flags leftover/placeholder lines.
> Assign each area to the member who actually owns it and keep your commits under your own
> GitHub account, spread across the project period.

This matrix lists what was built, by component. Fill in owners and adjust as your team divides work.

| Area | Component(s) | Owner |
|------|--------------|-------|
| Shared contracts & messaging | `Store.Shared.Events`, `Store.Shared.Common` (RabbitMQ publisher/consumer, correlation, Problem Details, JWT, DB bootstrap) | _TODO_ |
| Identity / Auth | `Identity.API` (register/login, JWT, roles, seed) | _TODO_ |
| Catalog | `Catalog.API` (products, versioned API, contract) | _TODO_ |
| Inventory | `Inventory.API` (stock, reservations, reserve/release consumers) | _TODO_ |
| Order (saga) | `Order.API` (place order, saga orchestration, typed resilient Catalog client) | _TODO_ |
| Payment | `Payment.API` (charge/refund consumers, failure switch) | _TODO_ |
| Gateway & security | `ApiGateway` (Ocelot routes, JWT, rate limiting) | _TODO_ |
| Storefront | `Storefront.UI` (browse, auth, order, admin, failure toggle) | _TODO_ |
| Containerization & DevOps | Dockerfiles, `docker-compose.yml`, health checks | _TODO_ |
| QA & docs | README, OpenAPI contracts, Postman, Evidence | _TODO_ |

## How to split (suggested)

Per the brief: one owner per service (Catalog, Inventory, Order, Payment), with the shared events
project, the gateway, Docker Compose and the Storefront co-owned or rotated. Every member should have
commits that **cross service boundaries** (a contract, a client, an event handler, a compose change),
plus a second hat (integration/DevOps lead and QA/docs lead).

## Commit hygiene checklist

- [ ] Each member's `git config user.email` is their **own** GitHub email.
- [ ] Work merged to `main` via reviewed pull requests (no direct pushes).
- [ ] Small, meaningful commits spread over the period (not one or two bulk commits).
- [ ] Release tagged (e.g. `v1.0`) at submission.
- [ ] This file filled in honestly (no `_TODO_` left).
