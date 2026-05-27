# Monsoon Masala API

.NET 8 clean-architecture Web API for nested menus, dishes, media uploads, PostgreSQL persistence, Cloudinary storage, and JWT admin auth with refresh tokens.

## Key Endpoints

- `GET /api/public/menu` - anonymous UI endpoint returning active menus with active dishes and media.
- `POST /api/admin/auth/users` - creates the first admin-style user from username/password.
- `POST /api/admin/auth/login` - returns JWT access and refresh tokens.
- `POST /api/admin/auth/refresh` - rotates refresh token and returns a new token pair.
- `POST /api/admin/auth/logout` - protected logout; revokes refresh tokens and bumps token version so existing access tokens are rejected.
- `POST /api/admin/auth/reset-password` - protected password reset; changes password, revokes all refresh tokens, and invalidates current JWTs.
- `POST /api/admin/menus` - protected admin menu creation.
- `POST /api/admin/dishes` - protected multipart dish creation with mass media upload via form files.
- `POST /api/admin/dishes/fix-order` - protected repair endpoint to normalize dish order values; accepts optional `menuId` query parameter.
- `GET /health` and `HEAD /health` - anonymous health endpoint for Render or uptime checks.

Swagger is enabled in production at `/swagger`.

## Render Migration Options

Preferred for Render free plan with Docker: set `RUN_MIGRATIONS=true` so the container entrypoint runs `dotnet ef database update` before the API is allowed to stay up.

If using Render shell/build hooks with the SDK available, run:

```bash
bash scripts/render-migrate.sh
```

## Production Environment Variables

- `ASPNETCORE_ENVIRONMENT=Production`
- `PORT=10000`
- `RUN_MIGRATIONS=true`
- `MIGRATION_MAX_ATTEMPTS=5`
- `ConnectionStrings__DefaultConnection` - PostgreSQL connection string. Use this or `DATABASE_URL`.
- `DATABASE_URL` - Render-style `postgres://user:password@host:port/database`; supported as a fallback.
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__Secret` - use at least 32 random characters.
- `Jwt__AccessTokenMinutes`
- `Jwt__RefreshTokenDays`
- `Cloudinary__CloudName`
- `Cloudinary__ApiKey`
- `Cloudinary__ApiSecret`
- `Cloudinary__Folder`
- `CORS_ALLOWED_ORIGINS` - comma-separated frontend URLs, for example `https://your-ui.onrender.com,http://localhost:5173`.
- Remove old `RUN_MIGRATIONS_ON_STARTUP` if it exists; Docker uses `RUN_MIGRATIONS`.

## Docker

```bash
docker build -t monsoon-masala-api .
docker run -p 10000:10000 --env-file .env monsoon-masala-api
```
