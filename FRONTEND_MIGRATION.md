# Seraphine Flowers Frontend Migration

This file maps the current Seraphine frontend to the new .NET backend so the frontend can stop owning business data and Cloudinary JSON writes.

## Goal

Move mutable business data from frontend or serverless JSON into PostgreSQL behind the backend API:

- customers
- offers
- reviews
- flagged numbers
- promo popup configuration
- visitor analytics
- gallery, trending, and customer-collection metadata

Cloudinary remains the image host. The backend becomes the source of truth for config, metadata, and user or admin actions.

## Keep vs remove

Keep:

- Cloudinary image hosting
- Firebase Phone Auth for customer OTP login and registration
- frontend display components and local UI state

Remove from frontend:

- `/api/customers`
- `/api/reviews`
- `/api/flagged`
- `/api/save-config`
- `/api/save-promo-config`
- `/api/load-promo-config`
- `/api/get-analytics`
- `/api/track-visitor`
- `/api/auth-admin`
- any admin password usage in browser env vars
- Cloudinary JSON writes from browser or serverless functions

## Firebase decision

Firebase is not used for customer data storage.

It is now used only for customer OTP verification:

- frontend uses Firebase Phone Auth to send OTP and confirm it
- frontend reads the Firebase ID token after OTP confirmation
- backend verifies that Firebase ID token using Firebase Admin SDK
- backend creates or logs in the customer and returns backend JWT tokens

Set `Storefront__RequireCustomerOtp=true` only when customer registration or login must require OTP.

## New backend endpoints

### Admin auth

- `POST /api/admin/auth/bootstrap`
  Use once to create the first admin.
- `POST /api/admin/auth/login`
- `POST /api/admin/auth/refresh`
- `POST /api/admin/auth/logout`
- `POST /api/admin/auth/reset-password`
- `POST /api/admin/auth/users`
  Protected. Creates additional admin users.

### Public customer flow

- `GET /api/public/bootstrap?phone={phone}`
  Returns customer snapshot, active promo, approved reviews, and offer config.
- `POST /api/public/customers/register`
  Body: `{ "phone": "...", "name": "...", "firebaseIdToken": "..." }` when OTP is enabled.
- `POST /api/public/customers/lookup`
  Body: `{ "phone": "...", "firebaseIdToken": "..." }` when OTP is enabled.
- `GET /api/public/customers/{phone}`

### Customer OTP authentication

- `POST /api/public/customers/auth/verify-otp`
  Registers a new customer or verifies an existing one with Firebase OTP.
- `POST /api/public/customers/auth/login`
  Logs in an existing customer with Firebase OTP.
- `POST /api/public/customers/auth/refresh`
  Refreshes customer access token using refresh token.
- `POST /api/public/customers/auth/logout`
  Protected. Revokes the refresh token.

Example registration or verification request:

```json
{
  "phone": "9876543210",
  "name": "Customer Name",
  "firebaseIdToken": "eyJhbGc..."
}
```

Example login request:

```json
{
  "phone": "9876543210",
  "firebaseIdToken": "eyJhbGc..."
}
```

Customer auth flow:

1. Frontend sends the phone number to Firebase Phone Auth.
2. Customer enters the OTP in the frontend.
3. Firebase confirms the OTP.
4. Frontend reads the Firebase ID token from the signed-in Firebase user.
5. Frontend sends `phone` plus `firebaseIdToken` to backend.
6. Backend verifies the Firebase ID token with Firebase Admin SDK, checks the phone number match, then creates or logs in the customer and returns backend JWT tokens.

### Public review flow

- `GET /api/public/reviews`
- `POST /api/public/reviews`
  Body: `{ "phone": "...", "name": "...", "rating": 5, "text": "..." }`

### Public safety, promo, media, analytics

- `POST /api/public/flagged/check`
- `GET /api/public/promo`
- `GET /api/public/media/{collectionKey}`
  Use `gallery`, `trending`, or `customercollection`.
- `GET /api/public/cloudinary/{collectionKey}/images`
  Use if you still want direct folder listing through backend.
- `POST /api/public/analytics/visits`

### Admin storefront management

- `GET /api/admin/storefront/customers`
- `POST /api/admin/storefront/customers`
- `POST /api/admin/storefront/customers/{id}/orders/increment`
- `DELETE /api/admin/storefront/customers/{id}`
- `GET /api/admin/storefront/reviews`
- `POST /api/admin/storefront/reviews/{id}/approve`
- `POST /api/admin/storefront/reviews/{id}/reject`
- `DELETE /api/admin/storefront/reviews/{id}`
- `GET /api/admin/storefront/flagged`
- `POST /api/admin/storefront/flagged`
- `DELETE /api/admin/storefront/flagged/{phone}`
- `GET /api/admin/storefront/promo`
- `PUT /api/admin/storefront/promo`
- `GET /api/admin/storefront/media-configs/{collectionKey}`
- `PUT /api/admin/storefront/media-configs/{collectionKey}`
- `GET /api/admin/storefront/analytics`
- `POST /api/admin/storefront/analytics/backfill-places`
- `GET /api/admin/storefront/cloudinary/{collectionKey}/images`

## One-time JSON import endpoints

Run these after backend deploy and admin bootstrap:

- `POST /api/admin/storefront/imports/customers`
- `POST /api/admin/storefront/imports/reviews`
- `POST /api/admin/storefront/imports/flagged`
- `POST /api/admin/storefront/imports/promo`
- `POST /api/admin/storefront/imports/analytics`
- `POST /api/admin/storefront/imports/media-configs/gallery`
- `POST /api/admin/storefront/imports/media-configs/trending`
- `POST /api/admin/storefront/imports/media-configs/customercollection`

These import from the Cloudinary raw JSON files configured through env vars.

## Frontend file-by-file changes

### `src/utils/customerService.js`

Replace:

- `fetch("/api/customers", ...)`
- admin password usage via `VITE_ADMIN_PASSWORD`
- Cloudinary JSON assumptions

With:

- `POST {backend}/api/public/customers/register`
- `POST {backend}/api/public/customers/lookup`
- `POST {backend}/api/public/customers/auth/verify-otp`
- `POST {backend}/api/public/customers/auth/login`
- `POST {backend}/api/public/customers/auth/refresh`
- `GET {backend}/api/admin/storefront/customers`
- `POST {backend}/api/admin/storefront/customers/{id}/orders/increment`
- `POST {backend}/api/admin/storefront/customers`
- `DELETE {backend}/api/admin/storefront/customers/{id}`

Important:

- the frontend should store customer `id` from backend responses
- the frontend should store backend customer access and refresh tokens when using the OTP login flow
- offer labels should come from backend customer response instead of frontend-only calculation

### `src/utils/reviewService.js`

Replace:

- `/api/reviews`
- admin password query or body values

With:

- `GET {backend}/api/public/reviews`
- `POST {backend}/api/public/reviews`
- `GET {backend}/api/admin/storefront/reviews`
- `POST {backend}/api/admin/storefront/reviews/{id}/approve`
- `POST {backend}/api/admin/storefront/reviews/{id}/reject`
- `DELETE {backend}/api/admin/storefront/reviews/{id}`

### `src/admin/AdminTabs.jsx`

Replace:

- `/api/auth-admin`
- local admin password gate
- `localStorage` auth booleans without refresh flow

With:

- JWT login through `POST {backend}/api/admin/auth/login`
- refresh token handling through `POST {backend}/api/admin/auth/refresh`
- logout through `POST {backend}/api/admin/auth/logout`

### `src/admin/AdminCustomers.jsx`

Replace all customer CRUD calls with `/api/admin/storefront/customers*`.

### `src/admin/AdminReviews.jsx`

Replace moderation calls with `/api/admin/storefront/reviews*`.

### `src/admin/AdminPromoAds.jsx`

Replace:

- `/api/load-promo-config`
- `/api/save-promo-config`
- `/api/list-images`

With:

- `GET {backend}/api/admin/storefront/promo`
- `PUT {backend}/api/admin/storefront/promo`
- `GET {backend}/api/admin/storefront/cloudinary/promoads/images`

### `src/admin/AdminConfigEditor.jsx`

Replace:

- direct Cloudinary config fetch
- `/api/save-config`
- `/api/sync-config`

With:

- `GET {backend}/api/admin/storefront/media-configs/{collectionKey}`
- `PUT {backend}/api/admin/storefront/media-configs/{collectionKey}`
- `GET {backend}/api/admin/storefront/cloudinary/{collectionKey}/images`

Each media config row can include:

- `imageName`
- `displayName`
- `description`
- `amount`
  optional, nullable, admin-configurable
- `sortOrder`

Recommended collection keys:

- `gallery`
- `trending`
- `customercollection`

### `src/utils/useDriveFolderData.js`

Change config-loading source:

- from Cloudinary raw JSON URLs
- to `GET {backend}/api/public/media/{collectionKey}`

If you still want actual image listing via backend:

- use `GET {backend}/api/public/cloudinary/{collectionKey}/images`
- merge the returned Cloudinary assets with metadata rows from `GET /api/public/media/{collectionKey}`

### `src/utils/useFlagCheck.js`

Replace with:

- `POST {backend}/api/public/flagged/check`

### `src/utils/useVisitorTracking.js`

Replace:

- `/api/track-visitor`
- `/api/get-analytics`

With:

- `POST {backend}/api/public/analytics/visits`
- `GET {backend}/api/admin/storefront/analytics`

## Frontend env changes

Remove browser-side secrets:

- `VITE_ADMIN_PASSWORD`
- any Cloudinary API secret exposure

Add:

- `VITE_API_BASE_URL=https://your-backend-url`

Frontend keeps Firebase client config such as:

- `VITE_FIREBASE_API_KEY`
- `VITE_FIREBASE_AUTH_DOMAIN`
- `VITE_FIREBASE_PROJECT_ID`
- `VITE_FIREBASE_APP_ID`

Backend keeps Firebase Admin verification config such as:

- `Storefront__Firebase__Enabled`
- `Storefront__Firebase__ProjectId`
- `Storefront__Firebase__ServiceAccountJsonPath`
- `Storefront__Firebase__ServiceAccountJsonBase64`

## Recommended migration order

1. Deploy backend with env vars and DB connection.
2. Apply the backend migrations.
3. Bootstrap the first admin with `/api/admin/auth/bootstrap`.
4. Call the one-time import endpoints.
5. Switch admin login flow to backend JWT.
6. Switch customer, review, flagged, promo, config, analytics, and media frontend calls to backend URLs.
7. Switch customer OTP flow to Firebase client plus backend verification endpoints.
8. Remove old Vercel serverless files after verification.

## Notes

- Customer-facing and admin-facing GET endpoints are intentionally separated.
- Logged-in and new user experiences can stay nearly the same, but offer logic should now come from backend responses.
- Folder names remain configurable through env vars, including import source folders and live Cloudinary collection folders.
