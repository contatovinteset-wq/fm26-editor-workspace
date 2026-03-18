// src/services/twitch.js
import axios from 'axios';

// URL de desenvolvimento ou produção da sua própria API
const API_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:3000';

const localApi = axios.create({
  baseURL: API_URL
});

const MOCK_LIVE_DATA = {
  isLive: true,
  data: {
    user_name: "vinteset",
    title: "LANÇAMENTO OVERHAUL FM26 + 10h de Gameplay 🔴 AO VIVO",
    viewer_count: 1450,
    thumbnail_url: "https://static-cdn.jtvnw.net/previews-ttv/live_user_vinteset-{width}x{height}.jpg",
    game_name: "Football Manager 2026",
    type: "live"
  }
};

/**
 * Verifica se o canal está ao vivo batendo no próprio Servidor (Node)
 */
export const checkChannelLive = async () => {
  try {
    const response = await localApi.get(`/api/twitch/channel`);
    return response.data;
  } catch (error) {
    console.error("=== ERRO AO CONECTAR COM BACKEND (TWITCH) ===", error.message);
    // Em caso de API inteira fora do ar, não quebramos o site, voltamos MOCK de segurança ou offline puro
    return MOCK_LIVE_DATA; 
  }
};
