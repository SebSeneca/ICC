# Basket API — IMPACT Code Challenge

A Basket API built on top of IMPACT's Code Challenge API. It lets a typical online-shopping
frontend browse a product catalog and manage a shopping basket (add, change quantity, remove,
submit), while the Basket API itself authenticates against the upstream Code Challenge API on the
caller's behalf.

The API is **anonymous to its clients**: no authentication and no email address are ever required
on any of its endpoints. Authentication exists only between this API and the upstream service.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/)

## Running locally

```bash
dotnet run --project BasketSystem.Api
```

The HTTP profile listens on `http://localhost:5204`, the HTTPS profile on `https://localhost:7274`.
In `Development`, the OpenAPI document is served at `/openapi/v1.json`. `BasketSystem.Api/BasketSystem.Api.http`
contains ready-made requests for every endpoint.

On startup the service eagerly warms up the product catalog from the upstream API (a single, slow
call — see *Caching* below), so the first request does not pay that cost.

## Configuration

All settings live under the `CodeChallengeApi` section of `appsettings.json`:

```json
"CodeChallengeApi": {
  "BaseUrl": "https://azfun-impact-code-challenge-api.azurewebsites.net",
  "Email": "seb@challenge.dk",
  "RefreshInterval": "00:04:00"
}
```

- `Email` is only used to obtain an upstream token. The upstream `Login` accepts any address, so a
  template value is sufficient — there is no secret to manage.
- `RefreshInterval` controls how often the background service reloads the cached catalog.

## Endpoints

### Products
| Method | Route | Description |
| --- | --- | --- |
| GET | `/api/products/top` | The top-ranked 100 products (the only products that may be bought). |
| GET | `/api/products?page={p}&pageSize={s}` | Catalog paginated by price ascending. `pageSize` must be 1–1000. |
| GET | `/api/products/cheapest` | The 10 cheapest products across the whole catalog. |

### Baskets
| Method | Route | Description |
| --- | --- | --- |
| POST | `/api/baskets` | Create a new (empty) basket; returns its GUID. |
| GET | `/api/baskets/{id}` | Get a basket by its GUID. |
| POST | `/api/baskets/{id}/items` | Add a product (`{ "productId", "quantity" }`); re-adding increases quantity. |
| PUT | `/api/baskets/{id}/items/{productId}` | Set the quantity of a line (`{ "quantity" }`). |
| DELETE | `/api/baskets/{id}/items/{productId}` | Remove a product from the basket. |
| POST | `/api/baskets/{id}/submit` | Submit the basket, creating an order via the upstream `CreateOrder`. |

Errors are returned as RFC 9457 `ProblemDetails`: validation → `400`, not found → `404`,
business-rule violation (e.g. a non-buyable product) → `422`, upstream failure → `502`.

## Testing

The default test run is fast and fully network-free (the upstream API and catalog are faked):

```bash
dotnet test
```

Integration tests run against the **real** upstream API and are skipped unless opted in:

```powershell
# PowerShell
$env:RUN_INTEGRATION_TESTS=1; dotnet test --filter "Category=Integration"
```

```bash
# bash / Git Bash
RUN_INTEGRATION_TESTS=1 dotnet test --filter "Category=Integration"
```

They verify the live catalog shape, the product endpoints, and a full submit flow end-to-end.

## Architecture & key design decisions

The solution is layered to follow SOLID and keep business logic free of web/HTTP concerns.
Dependencies point inward: **Api → Application → Domain**, with **Infrastructure** implementing the
Application's ports.

| Project | Responsibility |
| --- | --- |
| `BasketSystem.Domain` | Entities and invariants (`Product`, `Basket`, `BasketItem`, `Order`). No dependencies. |
| `BasketSystem.Application` | Use cases, DTOs and ports (`IProductCatalog`, `ICodeChallengeApiClient`, `IBasketRepository`, services). |
| `BasketSystem.Infrastructure` | Upstream HTTP client, token handling, caching, in-memory basket store. |
| `BasketSystem.Api` | Controllers, DI wiring, ProblemDetails error handling. |
| `BasketSystem.Tests` | Unit, end-to-end (in-memory) and opt-in integration tests. |

- **Ranking / buyability.** "Top-ranked" is interpreted as `stars` descending, with `id` ascending
  as a deterministic tie-break. The catalog holds 10,000 products but only 89 are five-star, so the
  top-100 is all five-star products plus the 11 lowest-id four-star ones. This rule is isolated in
  `ProductRanking`, so it is easy to change. Only top-100 products may be added to a basket or
  ordered.
- **Caching.** Upstream `GetAllProducts` returns all 10,000 products in a single, slow (~30s) call.
  The catalog is therefore cached in memory: the background service loads it at startup and reloads
  it every `RefreshInterval`. A single-flight lock ensures concurrent cold requests trigger only one
  upstream call; if a reload fails, the previous catalog keeps being served, so reads never fail
  because of a transient upstream outage.
- **Authentication.** A delegating handler attaches the upstream Bearer token to every request and,
  on a `401`, refreshes the token and retries once. The token is cached and obtained lazily; clients
  never see any of this.
- **Source of truth on submit.** When a basket is submitted, each line's price and name are taken
  from the current catalog (not from client input), and buyability is re-validated.
- **Baskets are in-memory only**, stored in a `ConcurrentDictionary` keyed by GUID — no database, as
  permitted by the challenge.

## Use of AI

AI coding assistance (Claude) was used throughout: to interpret the challenge, sketch the API and
architecture, break the work into user stories (see `UserStories/`), generate boilerplate and tests,
and review the code. The implementation plan and stories were produced collaboratively before coding,
and each story was verified with both automated and manual tests before being committed.
