// Status "ao vivo" dos criadores (Fase 3e) — best-effort multi-plataforma.
// Twitch: confiável (Helix API). Kick: API pública não-oficial. YouTube: scrape
// best-effort da página /live. Tudo com cache curto (60s) e fallback p/ offline
// em qualquer falha — nunca quebra o diretório.

import axios from 'axios';

const TTL = 60_000;
const cache = new Map(); // key -> { at, live }

function cached(key, fn) {
  const c = cache.get(key);
  if (c && Date.now() - c.at < TTL) return Promise.resolve(c.live);
  return fn()
    .then((live) => { cache.set(key, { at: Date.now(), live: !!live }); return !!live; })
    .catch(() => { cache.set(key, { at: Date.now(), live: false }); return false; });
}

// ───────────── Twitch (Helix) ─────────────
const TWITCH_CLIENT_ID = process.env.TWITCH_CLIENT_ID;
const TWITCH_CLIENT_SECRET = process.env.TWITCH_CLIENT_SECRET;
let twitchToken = process.env.TWITCH_ACCESS_TOKEN || null;

async function twitchAuth() {
  if (twitchToken) return twitchToken;
  if (!TWITCH_CLIENT_ID || !TWITCH_CLIENT_SECRET) return null;
  const r = await axios.post('https://id.twitch.tv/oauth2/token', null, {
    params: { client_id: TWITCH_CLIENT_ID, client_secret: TWITCH_CLIENT_SECRET, grant_type: 'client_credentials' },
    timeout: 5000
  });
  twitchToken = r.data.access_token;
  return twitchToken;
}

async function isTwitchLive(login) {
  if (!TWITCH_CLIENT_ID) return false;
  const call = async (token) => {
    const r = await axios.get(`https://api.twitch.tv/helix/streams?user_login=${encodeURIComponent(login)}`, {
      headers: { 'Client-Id': TWITCH_CLIENT_ID, Authorization: `Bearer ${token}` },
      timeout: 5000
    });
    return Array.isArray(r.data?.data) && r.data.data.length > 0;
  };
  let token = await twitchAuth();
  if (!token) return false;
  try {
    return await call(token);
  } catch (err) {
    if (err.response?.status === 401) { twitchToken = null; token = await twitchAuth(); if (token) return call(token); }
    throw err;
  }
}

// ───────────── Kick (API v2 não-oficial) ─────────────
async function isKickLive(slug) {
  const r = await axios.get(`https://kick.com/api/v2/channels/${encodeURIComponent(slug)}`, {
    timeout: 5000,
    headers: { 'User-Agent': 'Mozilla/5.0', Accept: 'application/json' }
  });
  return !!r.data?.livestream; // livestream != null => ao vivo
}

// ───────────── YouTube (best-effort: página /live) ─────────────
async function isYouTubeLive(url) {
  const base = url.replace(/\/+$/, '');
  const liveUrl = /\/live$/.test(base) ? base : `${base}/live`;
  const r = await axios.get(liveUrl, {
    timeout: 6000,
    headers: { 'User-Agent': 'Mozilla/5.0', 'Accept-Language': 'en-US' },
    maxRedirects: 5
  });
  const html = String(r.data || '');
  // Marcadores típicos de uma transmissão ao vivo em andamento.
  return /"isLiveBroadcast":true/.test(html) || (/hlsManifestUrl/.test(html) && /"isLive":true/.test(html));
}

// ───────────── Parsers de username/slug a partir da URL ─────────────
function twitchLogin(u) { const m = String(u).match(/twitch\.tv\/([A-Za-z0-9_]+)/i); return m ? m[1] : null; }
function kickSlug(u) { const m = String(u).match(/kick\.com\/([A-Za-z0-9_-]+)/i); return m ? m[1] : null; }

// Retorna a lista de plataformas em que o criador está AO VIVO agora.
export async function getLivePlatforms(branding) {
  const pf = (branding && branding.platforms) || {};
  const out = [];
  const jobs = [];
  if (pf.twitch) { const l = twitchLogin(pf.twitch); if (l) jobs.push(cached(`tw:${l}`, () => isTwitchLive(l)).then((v) => { if (v) out.push('twitch'); })); }
  if (pf.kick) { const s = kickSlug(pf.kick); if (s) jobs.push(cached(`kk:${s}`, () => isKickLive(s)).then((v) => { if (v) out.push('kick'); })); }
  if (pf.youtube) { jobs.push(cached(`yt:${pf.youtube}`, () => isYouTubeLive(pf.youtube)).then((v) => { if (v) out.push('youtube'); })); }
  await Promise.all(jobs);
  return out;
}
