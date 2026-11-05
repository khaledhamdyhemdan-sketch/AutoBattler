# Game Server Contract — Accounts, Arena and Guilds (draft v1)

**Status:** PostgreSQL schema exists at `server/sql/001_initial_schema.sql`. **There is no deployed/connected game server or production login yet.** This is an implementation contract, not a claim that PvP or guilds can be played.

## Identity and trust

- The Unity Android app can start locally as Guest for offline PvE; online mode will require a separately issued opaque session token bound to a server account. Account linking via Google/Apple must validate the provider's token **server side** before mapping the identity; never accept a self-asserted email/provider subject.
- Use HTTPS/TLS, secrets from hosting environment only, short-lived sessions/rotation and revoke/logout support. Store only hashes of high-entropy opaque session tokens, never raw tokens.
- All in-game text and errors delivered to the player are **English**. The Unity client sends `Accept-Language: en-US`.
- Device-local saves and clocks are **not trusted** for online economy, UR pity, leaderboard results, purchases, guild raids or rankings. A client cannot upload arbitrary level/hero ownership to turn it into authoritative balances.
- No secrets, keystores or database passwords should be committed to GitHub.

## Implemented public game catalog (offline Alpha)

The following **read-only** endpoints are now implemented and tested without
player authentication. Neither endpoint modifies progression or executes draws.

| Endpoint | Authorization | Behavior |
|---|---|---|
| `GET /v1/catalog/heroes` | Public | English roster of 41 unique heroes, tier, element, origin and role |
| `GET /v1/catalog/banners` | Public | Standard/Astral probabilities, 40/70 hard pity, and explicit `liveSummoning:false` |

Both endpoints source their data from `shared/hero-catalog.json`. CI enforces
that the shared list matches `WorldsCatalog.cs` / `HeroElements.cs`.
The Unity Alpha includes `ChroniclesCatalogClient`, an optional HTTPS-only
probe for those two routes; **no endpoint URL is preconfigured and no player
tokens, currencies, or saves are sent**. Unity Editor can use local loopback
HTTP for isolated testing only. Google/Apple identity, real matchmaking and
server-authoritative summons are still not implemented.

## API proposal (`/v1`)

| Endpoint | Authorization | Behavior |
|---|---|---|
| `POST /auth/guest` | Device attestation + rate limit | Create a guest account and secure token |
| `POST /auth/link` | Session + validated provider assertion | Upgrade/link a guest account without deleting its entitlements |
| `POST /auth/refresh` | Valid refresh credential | Rotate credential and revoke old session |
| `POST /auth/logout` | Session | Revoke current token |
| `GET /me` | Session | Profile, wallet revision and linked identity status |
| `GET /heroes` | Session | Owned hero records, formation and legitimate equipment |
| `GET /banners` | Session | Current rates, eligible pool, pity counters and guarantee rules |
| `POST /summons` | Session + idempotency key | Perform one/ten verified draws in one DB transaction |
| `GET /summons/history` | Session | Immutable personal summon receipt list |
| `POST /battles/start` | Session | Reserve encounter, verify formation and server state |
| `POST /battles/finish` | Session + battle token | Submit turn/event trace; server re-simulates or rejects unsupported results |
| `GET /arena/seasons/current` | Session | Current time window, rating and legitimate player ranking |
| `POST /arena/queue` | Session | Server pairs real players based on authorized snapshots |
| `GET /arena/matches/:id` | Session + participant | Server-verified battle result and signed reward receipt |
| `GET /guilds` | Session | Search publicly visible guilds (paginated) |
| `POST /guilds` | Session + server validation | Create a guild with unique name and enforced quota |
| `POST /guilds/:id/join` | Session | Join at most one guild under lock / capacity limit |
| `POST /guilds/:id/leave` | Session | Leave safely; disallow orphaning owner without succession |
| `GET /guilds/:id` | Session | Visible members, officer roles and weekly raid information |
| `POST /guilds/:id/raid/start` | Session + membership | Reserve eligible daily/weekly attempt |
| `POST /guilds/:id/raid/finish` | Session + server battle receipt | Record verified damage once, credit server reward |

**Do not deploy routes that grant currency, UR units, rank, inventory or rewards until the trusted validation and ledger transaction paths exist.**

## Summon transaction contract

Request: `{ "bannerId": "astral-v1", "count": 10, "idempotencyKey": "<unique client request id>" }`

Server workflow:
1. Validate account session, banner version/rates and UTC availability.
2. `BEGIN`; lock wallet, pity state and relevant inventory rows using `FOR UPDATE`.
3. Check balance and spend eligible tickets/seals, then gems.
4. Use cryptographically secure server RNG; apply real hard pity exactly once; record all individual receipts and duplicate fragment grants.
5. Insert a unique wallet ledger operation with `(account_id, idempotency_key)`. Retrying the same request returns the **same receipt**, never a second draw.
6. Commit and return new balances, hero grants and pity counters. Do not accept the Unity local save's currency fields as truth.

Need an authoritative `banner_pity` table and dedicated transactional implementation before publishing rates or real purchase flows.

## Arena fairness

A training room against NPCs is already implemented in Unity and must remain labeled **Arena Training**. It has local tiers (Bronze → Mythic) and is **not real PvP**.

For public ranked arena:
- Save a valid 5-hero defense snapshot, including allowed ranks and skill/equipment configuration, generated from the server inventory.
- Match compatible rating bands; prevent self-matches or scripted repeated farming. Store matching season, snapshots, deterministic battle seed and unique idempotency key.
- Resolve combat by trusted server logic or validate a deterministic event log, not by blindly trusting `victory=true` from the app.
- Reward and rating changes in one transaction with a server-side season lock and uniqueness constraints.
- Only then expose global/seasonal ratings, rewards, and leaderboard positions.

## Guild safety

- One guild per account, owner/officer/member roles, at most 30 verified members (enforce at database transaction boundary with row lock).
- Name moderation and reporting, capacity and join cooldowns, guild raid attempts per UTC period, audit logs for administrative kicks/promotions.
- Guild chat, if offered, requires moderation, abuse controls, retention and block/report workflows; it is not part of the first server baseline.
- Leader succession/disband must prevent losing valid guild achievements or unrelated account property.

## Database design

Schema `server/sql/001_initial_schema.sql` currently creates accounts, provider links, hashed sessions, wallets and economic ledger, owned heroes, campaign progress, one-time rewards, summon receipts, arena seasons/profiles/matches, guilds/members and raid receipts.

**Schema is not a running API.** Before launch also add migrations for banner pity, item transactions, events, verification/replay payloads, account deletion/privacy policy, retention windows, support/admin audit, observability and anti-abuse enforcement.

## Release gates

1. Choose account provider(s), deploy managed PostgreSQL and real HTTPS backend, configure environment secrets.
2. Implement + test all grant endpoints with concurrency/idempotency integration tests.
3. Wire Unity remote auth and wallet/hero sync with safe offline guest migration.
4. Test PvP outcome verification, season reset, guild membership race conditions, receipt revocation and abuse.
5. Security review, scaling/load tests, privacy/deletion controls, Android network/security tests.

**No backend credentials or production endpoints are required or stored in the repository by this draft.**
