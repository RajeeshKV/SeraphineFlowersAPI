# Seraphine Flowers API

.NET 8 clean-architecture backend for the Seraphine Flowers storefront and admin panel. The backend now owns customer registration, offers, reviews, promo configuration, flagged numbers, Cloudinary-backed media metadata, visitor analytics, and admin authentication.

## What moved to the backend

- Admin auth with JWT access and refresh tokens
- Admin user bootstrap and additional admin-user creation
- Normal customer registration and lookup
- Offer calculation based on configurable thresholds
- Review submission and admin moderation
- Flagged-number storage and checks
- Promo popup configuration
- Image metadata/config rows for gallery, trending, and customer collection folders
- Visitor analytics persistence
- One-time import endpoints to migrate current Cloudinary JSON data into PostgreSQL

## Core endpoints

- `POST /api/admin/auth/bootstrap`
- `POST /api/admin/auth/login`
- `POST /api/admin/auth/refresh`
- `POST /api/admin/auth/users`
- `GET /api/public/bootstrap?phone=...`
- `POST /api/public/customers/register`
- `POST /api/public/customers/lookup`
- `POST /api/public/reviews`
- `GET /api/public/reviews`
- `POST /api/public/flagged/check`
- `GET /api/public/promo`
- `GET /api/public/media/{collectionKey}`
- `POST /api/public/analytics/visits`
- `GET /api/admin/storefront/customers`
- `POST /api/admin/storefront/customers`
- `POST /api/admin/storefront/customers/{id}/orders/increment`
- `GET /api/admin/storefront/reviews`
- `POST /api/admin/storefront/reviews/{id}/approve`
- `POST /api/admin/storefront/reviews/{id}/reject`
- `GET /api/admin/storefront/flagged`
- `POST /api/admin/storefront/flagged`
- `GET /api/admin/storefront/promo`
- `PUT /api/admin/storefront/promo`
- `GET /api/admin/storefront/media-configs/{collectionKey}`
- `PUT /api/admin/storefront/media-configs/{collectionKey}`
- `GET /api/admin/storefront/analytics`
- `POST /api/admin/storefront/imports/customers`
- `POST /api/admin/storefront/imports/reviews`
- `POST /api/admin/storefront/imports/flagged`
- `POST /api/admin/storefront/imports/promo`
- `POST /api/admin/storefront/imports/analytics`
- `POST /api/admin/storefront/imports/media-configs/{collectionKey}`

## Configuration

Everything important is env-configurable:

- PostgreSQL connection string
- JWT issuer, audience, secret, token lifetimes
- Cloudinary credentials
- Cloudinary folder names for each storefront collection
- One-time import folder/public ID values
- Welcome and loyalty offer amounts and cadence
- Whether frontend OTP remains required
- Allowed CORS origins

See `.env.example` and `src/MonsoonMasala.Api/appsettings.json`.

## Database migration

The new storefront tables are added in:

- `src/MonsoonMasala.Infrastructure/Persistence/Migrations/20260527054218_AddSeraphineStorefront.cs`

Apply migrations with your normal deployment flow or with `RUN_MIGRATIONS=true`.

## Frontend integration guide

See [FRONTEND_MIGRATION.md](/C:/Personal/SeraphineBackend/SeraphineFlowersAPI/FRONTEND_MIGRATION.md).
