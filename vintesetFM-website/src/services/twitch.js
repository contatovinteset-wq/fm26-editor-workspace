// src/services/twitch.js
import axios from 'axios';

// URL de desenvolvimento ou produção (usar rota relativa em Produção)
const API_URL = import.meta.env.PROD ? '' : (import.meta.env.VITE_API_BASE_URL || 'http://localhost:3000');

const localApi = axios.create({
  baseURL: API_URL
});

const OFFLINE_FALLBACK = {
  isLive: false,
  data: null,
  lastVod: null
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
    // Em caso de API inteira fora do ar, retornamos offline para não mostrar Mocks falsos vazando
    return OFFLINE_FALLBACK; 
  }
};
