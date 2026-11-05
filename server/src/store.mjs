import { DatabaseSync } from 'node:sqlite';
import { randomBytes, randomInt, randomUUID, createHash } from 'node:crypto';

const STARTERS = ['sobek', 'bastet', 'set', 'nephthys', 'persephone'];
const DAY_MS = 86400000;
const today = () => new Date().toISOString().slice(0, 10);
const hashed = token => createHash('sha256').update(token).digest('hex');
const reject = (status, message) => { const e = new Error(message); e.status = status; throw e; };

export class GameStore {
  constructor(file = ':memory:') {
    this.db = new DatabaseSync(file);
    this.db.exec('PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;');
    this.db.exec(`
      CREATE TABLE IF NOT EXISTS players(
        id TEXT PRIMARY KEY, name TEXT NOT NULL, created_utc TEXT NOT NULL);
      CREATE TABLE IF NOT EXISTS sessions(
        hash TEXT PRIMARY KEY, player TEXT NOT NULL REFERENCES players(id),
        expires INTEGER NOT NULL);
      CREATE TABLE IF NOT EXISTS owned(
        player TEXT NOT NULL REFERENCES players(id), hero TEXT NOT NULL,
        level INTEGER NOT NULL DEFAULT 1, PRIMARY KEY(player,hero));
      CREATE TABLE IF NOT EXISTS arena(
        player TEXT PRIMARY KEY REFERENCES players(id),
        rating INTEGER NOT NULL DEFAULT 900,
        best INTEGER NOT NULL DEFAULT 900, wins INTEGER NOT NULL DEFAULT 0,
        losses INTEGER NOT NULL DEFAULT 0, date TEXT NOT NULL DEFAULT '',
        used INTEGER NOT NULL DEFAULT 0, defense TEXT NOT NULL DEFAULT '');
      CREATE TABLE IF NOT EXISTS matches(
        id TEXT PRIMARY KEY, attacker TEXT NOT NULL REFERENCES players(id),
        defender TEXT NOT NULL REFERENCES players(id), victor TEXT NOT NULL,
        delta INTEGER NOT NULL, utc TEXT NOT NULL);
      CREATE TABLE IF NOT EXISTS guilds(
        id TEXT PRIMARY KEY, name TEXT NOT NULL COLLATE NOCASE UNIQUE,
        leader TEXT NOT NULL REFERENCES players(id));
      CREATE TABLE IF NOT EXISTS members(
        player TEXT PRIMARY KEY REFERENCES players(id),
        guild TEXT NOT NULL REFERENCES guilds(id), joined TEXT NOT NULL);
    `);
  }
  close() { this.db.close(); }
  tx(callback) {
    this.db.exec('BEGIN IMMEDIATE');
    try { const result = callback(); this.db.exec('COMMIT'); return result; }
    catch (e) { this.db.exec('ROLLBACK'); throw e; }
  }
  grantToken(player) {
    const token = randomBytes(32).toString('base64url');
    const expires = Date.now() + 90 * DAY_MS;
    this.db.prepare('INSERT INTO sessions(hash,player,expires) VALUES(?,?,?)')
      .run(hashed(token), player, expires);
    return { token, expiresUtc: new Date(expires).toISOString() };
  }
  createGuest() {
    return this.tx(() => {
      const id = randomUUID();
      this.db.prepare('INSERT INTO players(id,name,created_utc) VALUES(?,?,?)')
        .run(id, 'Wanderer-' + id.slice(0, 6), new Date().toISOString());
      for (const hero of STARTERS)
        this.db.prepare('INSERT INTO owned(player,hero) VALUES(?,?)').run(id, hero);
      this.db.prepare('INSERT INTO arena(player) VALUES(?)').run(id);
      return { playerId:id, ...this.grantToken(id), profile:this.profile(id) };
    });
  }
  authenticate(token) {
    if (typeof token !== 'string' || !/^[A-Za-z0-9_-]{40,100}$/.test(token))
      reject(401, 'Invalid authentication token.');
    const row = this.db.prepare('SELECT player FROM sessions WHERE hash=? AND expires>?')
      .get(hashed(token), Date.now());
    if (!row) reject(401, 'Invalid or expired authentication token.');
    return row.player;
  }
  refresh(player, oldToken) {
    return this.tx(() => {
      const row = this.db.prepare('SELECT player FROM sessions WHERE hash=? AND player=?')
        .get(hashed(oldToken), player);
      if (!row) reject(401, 'Session expired.');
      const next = this.grantToken(player);
      this.db.prepare('DELETE FROM sessions WHERE hash=?').run(hashed(oldToken));
      return next;
    });
  }
  profile(player) {
    const p = this.db.prepare('SELECT * FROM players WHERE id=?').get(player);
    if (!p) reject(404,'Player not found.');
    const a = this.db.prepare('SELECT * FROM arena WHERE player=?').get(player);
    const heroes = this.db.prepare('SELECT hero AS id,level FROM owned WHERE player=?')
      .all(player);
    const membership = this.db.prepare('SELECT guild FROM members WHERE player=?')
      .get(player);
    return { id:p.id, displayName:p.name, heroes,
      arena:{rating:a.rating,best:a.best,wins:a.wins,losses:a.losses,
        defense:a.defense ? JSON.parse(a.defense):[],
        attemptsRemaining:5-(a.date===today()?a.used:0)},
      guildId:membership?.guild??null };
  }
  defense(player, heroes) {
    if (!Array.isArray(heroes) || heroes.length !== 5 ||
        new Set(heroes).size !== 5 || heroes.some(x=>typeof x!=='string'))
      reject(400,'Choose five distinct heroes.');
    const owned = new Set(this.db.prepare('SELECT hero FROM owned WHERE player=?')
      .all(player).map(x=>x.hero));
    if (heroes.some(x=>!owned.has(x))) reject(403,'Hero not owned.');
    this.db.prepare('UPDATE arena SET defense=? WHERE player=?')
      .run(JSON.stringify(heroes),player);
    return {defense:heroes};
  }
  opponents(player) {
    return this.db.prepare(`SELECT a.player AS id,p.name AS displayName,
      a.rating,a.defense FROM arena a JOIN players p ON p.id=a.player
      WHERE a.player!=? AND a.defense!='' ORDER BY
      ABS(a.rating-(SELECT rating FROM arena WHERE player=?)),a.player LIMIT 10`)
      .all(player,player).map(row=>({...row,defense:JSON.parse(row.defense)}));
  }
  static power(heroes, rating) {
    // Deterministic simple simulator, NOT the full Unity 5v5 combat rules.
    const subtotal=heroes.reduce((sum,id)=>{
      const byte=createHash('sha256').update(id).digest()[0];
      return sum+80+byte%30;
    },0);
    return subtotal+rating*0.025;
  }
  challenge(player, rival) {
    if (typeof rival!=='string'||rival===player) reject(400,'Invalid opponent.');
    return this.tx(()=>{
      const own=this.db.prepare('SELECT * FROM arena WHERE player=?').get(player);
      const opp=this.db.prepare('SELECT * FROM arena WHERE player=?').get(rival);
      if(!own||!opp||!own.defense||!opp.defense)
        reject(400,'Both players must set a five-hero defense.');
      const day=today(), used=own.date===day?own.used:0;
      if(used>=5)reject(429,'Arena daily limit reached.');
      const ownScore=GameStore.power(JSON.parse(own.defense),own.rating);
      const oppScore=GameStore.power(JSON.parse(opp.defense),opp.rating);
      const victory=ownScore+randomInt(-20,21)>=oppScore;
      const next=Math.max(0,own.rating+(victory?22:-12));
      const oppNext=Math.max(0,opp.rating+(victory?-12:22));
      this.db.prepare(`UPDATE arena SET rating=?,best=MAX(best,?),
        wins=wins+?,losses=losses+?,date=?,used=? WHERE player=?`)
        .run(next,next,victory?1:0,victory?0:1,day,used+1,player);
      this.db.prepare(`UPDATE arena SET rating=?,best=MAX(best,?),
        wins=wins+?,losses=losses+? WHERE player=?`)
        .run(oppNext,oppNext,victory?0:1,victory?1:0,rival);
      const matchId=randomUUID();
      this.db.prepare('INSERT INTO matches(id,attacker,defender,victor,delta,utc) VALUES(?,?,?,?,?,?)')
        .run(matchId,player,rival,victory?player:rival,victory?22:-12,new Date().toISOString());
      return {matchId,victory,rating:next,attemptsRemaining:4-used,
        rules:'server_simulation_prototype'};
    });
  }
  guild(id) {
    const g=this.db.prepare('SELECT * FROM guilds WHERE id=?').get(id);
    if(!g)reject(404,'Guild not found.');
    const players=this.db.prepare(`SELECT p.id,p.name FROM members m
      JOIN players p ON p.id=m.player WHERE m.guild=?
      ORDER BY m.joined,p.id`).all(id);
    return {id:g.id,name:g.name,leaderId:g.leader,memberLimit:30,
      members:players.map(p=>({playerId:p.id,displayName:p.name}))};
  }
  guilds() {
    return this.db.prepare(`SELECT g.id,g.name,COUNT(m.player) AS memberCount FROM guilds g
      LEFT JOIN members m ON m.guild=g.id GROUP BY g.id
      ORDER BY memberCount DESC,g.name LIMIT 40`).all();
  }
  createGuild(player,name) {
    if(typeof name!=='string'||!/^[A-Za-z0-9 _-]{3,24}$/.test(name.trim()))
      reject(400,'Guild names must use 3-24 English letters, numbers or spaces.');
    return this.tx(()=>{
      if(this.db.prepare('SELECT 1 FROM members WHERE player=?').get(player))
        reject(409,'Already in a guild.');
      if(this.db.prepare('SELECT 1 FROM guilds WHERE name=?').get(name.trim()))
        reject(409,'Guild name already taken.');
      const id=randomUUID(),guildName=name.trim();
      this.db.prepare('INSERT INTO guilds(id,name,leader) VALUES(?,?,?)')
        .run(id,guildName,player);
      this.db.prepare('INSERT INTO members(player,guild,joined) VALUES(?,?,?)')
        .run(player,id,new Date().toISOString());
      return this.guild(id);
    });
  }
  joinGuild(player,guildId) {
    return this.tx(()=>{
      if(this.db.prepare('SELECT 1 FROM members WHERE player=?').get(player))
        reject(409,'Already in a guild.');
      const guild=this.guild(guildId);
      if(guild.members.length>=30)reject(409,'Guild is full.');
      this.db.prepare('INSERT INTO members(player,guild,joined) VALUES(?,?,?)')
        .run(player,guildId,new Date().toISOString());
      return this.guild(guildId);
    });
  }
  leaveGuild(player) {
    return this.tx(()=>{
      const m=this.db.prepare('SELECT guild FROM members WHERE player=?').get(player);
      if(!m)reject(409,'Not in a guild.');
      const guild=this.guild(m.guild);
      this.db.prepare('DELETE FROM members WHERE player=?').run(player);
      const first=this.db.prepare('SELECT player FROM members WHERE guild=? ORDER BY joined,player LIMIT 1').get(guild.id);
      if(!first)this.db.prepare('DELETE FROM guilds WHERE id=?').run(guild.id);
      else if(guild.leaderId===player)
        this.db.prepare('UPDATE guilds SET leader=? WHERE id=?')
          .run(first.player,guild.id);
      return {left:true,disbanded:!first};
    });
  }
}
