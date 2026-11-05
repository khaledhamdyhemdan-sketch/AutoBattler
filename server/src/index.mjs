import { createServer } from 'node:http';
import pg from 'pg';
import { createHandler } from './app.mjs';

const { Pool } = pg;
const databaseUrl = process.env.DATABASE_URL;
const pepper = process.env.SESSION_PEPPER;
const port = Number(process.env.PORT ?? 8080);
const host = process.env.HOST ?? '127.0.0.1';
const production = process.env.NODE_ENV === 'production';

if (!databaseUrl || !pepper || pepper.length < 32) {
  process.stderr.write(
    'DATABASE_URL and SESSION_PEPPER (32+ characters) are required.\n'
  );
  process.exitCode = 1;
} else if (!Number.isInteger(port) || port < 1 || port > 65535) {
  process.stderr.write('PORT must be a valid TCP port.\n');
  process.exitCode = 1;
} else {
  // Development-only guest token issuance. Never bypass provider/attestation
  // checks when enabling real player account registration.
  const devGuest = !production &&
    process.env.ENABLE_DEV_GUEST_AUTH === 'true' &&
    (host === '127.0.0.1' || host === '::1');
  const pool = new Pool({
    connectionString: databaseUrl,
    max: 10,
    idleTimeoutMillis: 30000,
    connectionTimeoutMillis: 5000
  });
  const app = createServer(createHandler({ pool, pepper, devGuest }));
  app.requestTimeout = 15000;
  app.headersTimeout = 10000;
  app.listen(port, host, () => {
    process.stdout.write(
      '[Chronicles] HTTP server listening at ' + host + ':' + port +
      '. Reverse-proxy HTTPS required for remote access.\n'
    );
  });

  const shutdown = () => {
    app.close(async () => {
      await pool.end();
      process.exit(0);
    });
  };
  process.once('SIGINT', shutdown);
  process.once('SIGTERM', shutdown);
}
