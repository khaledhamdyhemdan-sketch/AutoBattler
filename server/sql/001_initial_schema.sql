-- Mythic Chronicles server storage baseline (PostgreSQL 15+)
-- Server-authoritative data model only. No production deployment claimed.
-- Run by controlled migration tooling; back up production data before future changes.
BEGIN;
CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS game_accounts (
    account_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    display_name varchar(40) NOT NULL DEFAULT 'Traveler',
    preferred_language varchar(8) NOT NULL DEFAULT 'en-US',
    guest boolean NOT NULL DEFAULT true,
    disabled_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    last_seen_at timestamptz NOT NULL DEFAULT now(),
    CHECK (char_length(trim(display_name)) >= 2)
);

CREATE TABLE IF NOT EXISTS account_identities (
    provider varchar(32) NOT NULL,
    provider_subject varchar(255) NOT NULL,
    account_id uuid NOT NULL REFERENCES game_accounts(account_id) ON DELETE CASCADE,
    linked_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (provider, provider_subject),
    UNIQUE (account_id, provider)
);

CREATE TABLE IF NOT EXISTS game_sessions (
    token_hash bytea PRIMARY KEY,
    account_id uuid NOT NULL REFERENCES game_accounts(account_id) ON DELETE CASCADE,
    expires_at timestamptz NOT NULL,
    revoked_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_game_sessions_account
    ON game_sessions(account_id, expires_at DESC);

CREATE TABLE IF NOT EXISTS game_wallets (
    account_id uuid PRIMARY KEY REFERENCES game_accounts(account_id) ON DELETE CASCADE,
    gold bigint NOT NULL DEFAULT 2500 CHECK (gold >= 0),
    gems bigint NOT NULL DEFAULT 350 CHECK (gems >= 0),
    summon_tickets bigint NOT NULL DEFAULT 3 CHECK (summon_tickets >= 0),
    astral_seals bigint NOT NULL DEFAULT 0 CHECK (astral_seals >= 0),
    forge_dust bigint NOT NULL DEFAULT 0 CHECK (forge_dust >= 0),
    revision bigint NOT NULL DEFAULT 0 CHECK (revision >= 0)
);

-- Every economic operation must have a server-generated receipt and caller
-- idempotency key. Apply wallet changes in the SAME PostgreSQL transaction.
CREATE TABLE IF NOT EXISTS wallet_transactions (
    transaction_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES game_accounts(account_id),
    idempotency_key varchar(96) NOT NULL,
    action varchar(48) NOT NULL,
    delta_gold bigint NOT NULL DEFAULT 0,
    delta_gems bigint NOT NULL DEFAULT 0,
    delta_tickets bigint NOT NULL DEFAULT 0,
    delta_seals bigint NOT NULL DEFAULT 0,
    metadata jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (account_id, idempotency_key),
    CHECK (length(idempotency_key) >= 8)
);
CREATE INDEX IF NOT EXISTS ix_wallet_transactions_account
    ON wallet_transactions(account_id, created_at DESC);

CREATE TABLE IF NOT EXISTS owned_heroes (
    account_id uuid NOT NULL REFERENCES game_accounts(account_id) ON DELETE CASCADE,
    hero_id varchar(48) NOT NULL,
    level integer NOT NULL DEFAULT 1 CHECK (level BETWEEN 1 AND 300),
    stars integer NOT NULL DEFAULT 1 CHECK (stars BETWEEN 1 AND 20),
    fragments integer NOT NULL DEFAULT 0 CHECK (fragments >= 0),
    signature_rank integer NOT NULL DEFAULT 0 CHECK (signature_rank BETWEEN 0 AND 5),
    PRIMARY KEY (account_id, hero_id)
);

CREATE TABLE IF NOT EXISTS campaign_progress (
    account_id uuid PRIMARY KEY REFERENCES game_accounts(account_id) ON DELETE CASCADE,
    last_cleared_stage integer NOT NULL DEFAULT 0
        CHECK (last_cleared_stage BETWEEN 0 AND 60),
    profile_revision bigint NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS unique_reward_claims (
    account_id uuid NOT NULL REFERENCES game_accounts(account_id) ON DELETE CASCADE,
    reward_key varchar(96) NOT NULL,
    wallet_transaction_id uuid REFERENCES wallet_transactions(transaction_id),
    created_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (account_id, reward_key)
);

CREATE TABLE IF NOT EXISTS summon_draw_receipts (
    draw_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES game_accounts(account_id),
    banner_id varchar(64) NOT NULL,
    batch_id uuid NOT NULL,
    draw_index integer NOT NULL CHECK (draw_index BETWEEN 0 AND 9),
    hero_id varchar(48) NOT NULL,
    rarity varchar(8) NOT NULL CHECK (rarity IN ('R','SR','SSR','UR')),
    pity_before integer NOT NULL CHECK (pity_before >= 0),
    guaranteed boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (account_id, batch_id, draw_index)
);
CREATE INDEX IF NOT EXISTS ix_summon_receipts_history
    ON summon_draw_receipts(account_id, created_at DESC);

CREATE TABLE IF NOT EXISTS arena_seasons (
    season_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name varchar(80) NOT NULL,
    start_at timestamptz NOT NULL,
    ends_at timestamptz NOT NULL,
    CHECK (ends_at > start_at)
);

CREATE TABLE IF NOT EXISTS arena_profiles (
    account_id uuid PRIMARY KEY REFERENCES game_accounts(account_id) ON DELETE CASCADE,
    season_id uuid REFERENCES arena_seasons(season_id),
    rating integer NOT NULL DEFAULT 1000 CHECK (rating BETWEEN 0 AND 5000),
    defense_hero_ids jsonb NOT NULL DEFAULT '[]'::jsonb,
    wins integer NOT NULL DEFAULT 0 CHECK (wins >= 0),
    losses integer NOT NULL DEFAULT 0 CHECK (losses >= 0),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CHECK (jsonb_typeof(defense_hero_ids) = 'array'),
    CHECK (jsonb_array_length(defense_hero_ids) <= 5)
);

-- Client-submitted battle results must NEVER flip pending to resolved by
-- themselves. A trusted battle evaluator must determine and record outcome.
CREATE TABLE IF NOT EXISTS arena_matches (
    match_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    season_id uuid NOT NULL REFERENCES arena_seasons(season_id),
    attacker_id uuid NOT NULL REFERENCES game_accounts(account_id),
    defender_id uuid NOT NULL REFERENCES game_accounts(account_id),
    status varchar(16) NOT NULL DEFAULT 'pending'
        CHECK (status IN ('pending','verified','void')),
    winner_id uuid REFERENCES game_accounts(account_id),
    server_battle_receipt varchar(120),
    created_at timestamptz NOT NULL DEFAULT now(),
    resolved_at timestamptz,
    CHECK (attacker_id <> defender_id),
    CHECK (winner_id IS NULL OR winner_id IN (attacker_id, defender_id)),
    CHECK (status <> 'verified' OR
        (winner_id IS NOT NULL AND resolved_at IS NOT NULL AND
         server_battle_receipt IS NOT NULL))
);
CREATE INDEX IF NOT EXISTS ix_arena_match_attacker
    ON arena_matches(attacker_id, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_arena_match_defender
    ON arena_matches(defender_id, created_at DESC);

CREATE TABLE IF NOT EXISTS game_guilds (
    guild_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name varchar(38) NOT NULL,
    description varchar(240) NOT NULL DEFAULT '',
    owner_id uuid NOT NULL REFERENCES game_accounts(account_id),
    created_at timestamptz NOT NULL DEFAULT now(),
    CHECK (length(trim(name)) BETWEEN 3 AND 38)
);
CREATE UNIQUE INDEX IF NOT EXISTS uq_guild_name_casefold
    ON game_guilds(lower(name));

CREATE TABLE IF NOT EXISTS guild_members (
    account_id uuid PRIMARY KEY REFERENCES game_accounts(account_id) ON DELETE CASCADE,
    guild_id uuid NOT NULL REFERENCES game_guilds(guild_id) ON DELETE CASCADE,
    guild_role varchar(16) NOT NULL DEFAULT 'member'
        CHECK (guild_role IN ('owner','officer','member')),
    joined_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_guild_members_guild
    ON guild_members(guild_id, joined_at ASC);

CREATE TABLE IF NOT EXISTS guild_raid_receipts (
    guild_id uuid NOT NULL REFERENCES game_guilds(guild_id) ON DELETE CASCADE,
    account_id uuid NOT NULL REFERENCES game_accounts(account_id),
    raid_week varchar(10) NOT NULL,
    attempt_index integer NOT NULL CHECK (attempt_index BETWEEN 1 AND 3),
    server_damage bigint NOT NULL CHECK (server_damage >= 0),
    battle_receipt varchar(120) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (guild_id, account_id, raid_week, attempt_index)
);

COMMIT;
