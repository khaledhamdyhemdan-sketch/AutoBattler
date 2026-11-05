import { createHmac, randomBytes } from 'node:crypto';

const safeGuestName = /^[A-Za-z0-9][A-Za-z0-9 ._-]{1,38}[A-Za-z0-9]$/;
const safeGuildName = /^[A-Za-z0-9][A-Za-z0-9 ._-]{1,36}[A-Za-z0-9]$/;

export function guestName(value) {
  if (typeof value !== 'string') return 'Traveler';
  const clean = value.trim().replace(/\s+/g, ' ');
  return safeGuestName.test(clean) && clean.length <= 40 ? clean : null;
}

export function guildName(value) {
  if (typeof value !== 'string') return null;
  const clean = value.trim().replace(/\s+/g, ' ');
  return safeGuildName.test(clean) && clean.length >= 3 &&
    clean.length <= 38 ? clean : null;
}

export function guildDescription(value) {
  if (value == null || value === '') return '';
  if (typeof value !== 'string' || value.length > 240 ||
      /[\u0000-\u001f]/.test(value)) return null;
  return value.trim();
}

export function newSessionToken() {
  return randomBytes(32).toString('base64url');
}

export function hashedSession(token, pepper) {
  if (typeof pepper !== 'string' || pepper.length < 32)
    throw new Error('Session pepper must be at least 32 characters');
  if (typeof token !== 'string' || token.length < 32 || token.length > 160)
    return null;
  return createHmac('sha256', pepper).update(token).digest();
}

export function bearerToken(header) {
  if (typeof header !== 'string') return null;
  const match = /^Bearer ([A-Za-z0-9_-]{32,160})$/.exec(header);
  return match ? match[1] : null;
}

export function validUuid(value) {
  return typeof value === 'string' &&
    /^[a-f0-9]{8}-[a-f0-9]{4}-[1-8][a-f0-9]{3}-[89ab][a-f0-9]{3}-[a-f0-9]{12}$/i.test(value);
}

export function readJson(req, maxBytes = 16384) {
  return new Promise((resolve, reject) => {
    const contentType = (req.headers['content-type'] || '').split(';')[0].trim();
    if (contentType !== 'application/json')
      return reject(Object.assign(new Error('Content-Type must be application/json'), { status:415 }));
    const chunks = [];
    let bytes = 0;
    req.on('data', chunk => {
      bytes += chunk.length;
      if (bytes > maxBytes) {
        reject(Object.assign(new Error('JSON body too large'), { status:413 }));
        req.destroy();
        return;
      }
      chunks.push(chunk);
    });
    req.on('error', reject);
    req.on('end', () => {
      if (bytes > maxBytes) return;
      try {
        const parsed = JSON.parse(Buffer.concat(chunks).toString('utf8'));
        if (parsed == null || Array.isArray(parsed) || typeof parsed !== 'object')
          throw new Error('Expected JSON object');
        resolve(parsed);
      } catch (e) {
        reject(Object.assign(new Error('Invalid JSON object'), { status:400 }));
      }
    });
  });
}
