import axios from 'axios';
import dotenv from 'dotenv';
dotenv.config();

const TWITCH_CLIENT_ID = process.env.TWITCH_CLIENT_ID;
const TWITCH_CLIENT_SECRET = process.env.TWITCH_CLIENT_SECRET;
const TARGET_USERNAME = 'vinteset';

let appAccessToken = process.env.TWITCH_ACCESS_TOKEN || null;

/**
 * Função para gerar um novo App Access Token usando Client Credentials
 */
const generateNewToken = async () => {
  try {
    const response = await axios.post('https://id.twitch.tv/oauth2/token', null, {
      params: {
        client_id: TWITCH_CLIENT_ID,
        client_secret: TWITCH_CLIENT_SECRET,
        grant_type: 'client_credentials'
      }
    });
    
    appAccessToken = response.data.access_token;
    console.log('[Twitch Service] Token renovado com sucesso!');
    return appAccessToken;
  } catch (error) {
    console.error('[Twitch Service] Erro ao gerar novo token:', error.response?.data || error.message);
    throw new Error('Falha ao autenticar na Twitch');
  }
};

/**
 * Retorna os dados do canal (Live ou Último VOD)
 */
export const getTwitchChannelData = async () => {
  if (!TWITCH_CLIENT_ID) throw new Error('TWITCH_CLIENT_ID não configurado no .env');

  // Se não temos token em memória, tentamos gerar um primeiro
  if (!appAccessToken && TWITCH_CLIENT_SECRET) {
    await generateNewToken();
  }

  const fetchWithToken = async (token) => {
    const api = axios.create({
      baseURL: 'https://api.twitch.tv/helix',
      headers: {
        'Client-Id': TWITCH_CLIENT_ID,
        'Authorization': `Bearer ${token}`
      }
    });

    // Passo 1: Verifica Streams (Live)
    const streamRes = await api.get(`/streams?user_login=${TARGET_USERNAME}`);
    if (streamRes.data.data.length > 0) {
      return { isLive: true, data: streamRes.data.data[0] };
    }

    // Passo 2: Se não estiver Live, pega o ID do Usuário para buscar vídeos
    const userRes = await api.get(`/users?login=${TARGET_USERNAME}`);
    if (userRes.data.data.length === 0) throw new Error('Usuário vinteset não encontrado na Twitch');
    const userId = userRes.data.data[0].id;

    // Passo 3: Pega o último VOD (Archive)
    const videoRes = await api.get(`/videos?user_id=${userId}&first=1&type=archive`);
    return {
      isLive: false,
      data: null,
      lastVod: videoRes.data.data.length > 0 ? videoRes.data.data[0] : null
    };
  };

  try {
    return await fetchWithToken(appAccessToken);
  } catch (error) {
    // Se o erro for 401 Unauthorized, o token expirou. Renovamos e tentamos DE NOVO 1 ÚNICA VEZ.
    if (error.response?.status === 401 && TWITCH_CLIENT_SECRET) {
      console.warn('[Twitch Service] Token expirado ou inválido (401). Renovando...');
      const newToken = await generateNewToken();
      return await fetchWithToken(newToken);
    }
    
    // Se não for 401 ou não tem secret pra renovar, joga o erro pra cima
    console.error('[Twitch Service] Erro na API:', error.response?.data || error.message);
    throw error;
  }
};
