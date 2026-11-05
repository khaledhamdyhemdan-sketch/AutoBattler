# Chronicles — Game Server Baseline

> **Development only.** This folder is not deployed and is **not** a production
> identity, economy, PvP or guild service.

## Requirements

- Node.js 22+
- PostgreSQL 15+ with a separately managed database
- HTTPS/TLS termination and a secret manager for production

## Available HTTP endpoints

| Method | Path | Behavior |
|---|---|---|
| GET | `/healthz` | Process health, no secret data |
| GET | `/readyz` | Runs a database readiness query |
| GET | `/v1/capabilities` | Declares supported vs unavailable features |
| GET | `/v1/me` | Authenticated account and read-only wallet snapshot |
| GET | `/v1/heroes` | Authenticated read-only hero records |
| POST | `/v1/auth/logout` | Revokes an authenticated session |
| POST | `/v1/auth/guest` | **Developer-only**, local-loopback testing; production-disabled |

Unsupported `/v1/*` routes return HTTP 501 and do **not** grant coins, summon
UR heroes, mutate PvP results or create guilds. Server messages are in English.

## Local development (not public deployment)

1. Inspect `sql/001_initial_schema.sql` and apply it to a disposable PostgreSQL database.
2. Set `DATABASE_URL` in your environment to the disposable DB connection string.
3. Generate a strong, random `SESSION_PEPPER` (32+ characters, never check it into Git).
4. Install dependencies in `server` with `npm ci` **only if a lockfile exists**; otherwise use `npm install` and review/commit an appropriate lockfile.
5. Execute `npm run check` and `npm test`, then `npm start`.
6. Default listener is `127.0.0.1:8080`; `PORT` and `HOST` can be set explicitly.

`ENABLE_DEV_GUEST_AUTH=true` is honored **only when NODE_ENV is not
production and HOST is localhost/loopback**. Requests must also originate from
the loopback interface. This is for development tests only: it is not a
production device attestation or social-login system.

No database credentials, signing keys, server tokens or real player data
should be shared in commits.

## Tests and known limitations

`npm test` currently runs **mocked HTTP/security tests**. GitHub Actions runs
these tests and a lightweight Unity source audit on every branch commit.
Successful results do **not** mean PostgreSQL migrations, multiplayer,
Unity compilation or an Android APK have been tested. Deployment requires:

- Real database integration tests, migrations and backups
- Verified Google/Apple provider linking and account deletion
- Server-authoritative inventory/economy with idempotent ledger
- Certified summons/pity and anti-abuse/rate limits
- Trusted PvP battle verification, guild membership transaction tests
- TLS, privacy/support flows, metrics, security and load testing
- Unity client integration and Android device tests

See `API_CONTRACT_V1.md` for the intended future API boundaries.
