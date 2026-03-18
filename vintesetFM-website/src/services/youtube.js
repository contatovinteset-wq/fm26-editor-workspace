// src/services/youtube.js
import axios from 'axios';

// URL de desenvolvimento ou produção
const API_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:3000';

const localApi = axios.create({
  baseURL: API_URL
});

export const MOCK_YOUTUBE_VIDEOS = [
  {
    id: "dQw4w9WgXcQ",
    title: "Extraindo Jogadores Escondidos do FM26: O Guia Definitivo",
    thumbnail: "https://i.ytimg.com/vi/dQw4w9WgXcQ/maxresdefault.jpg",
    publishedAt: "2026-03-10T18:00:00Z",
    duration: "14:32"
  }
];

export const getLatestNonLiveVideo = async () => {
  try {
    const response = await localApi.get(`/api/youtube/latest`);
    return response.data || MOCK_YOUTUBE_VIDEOS[0];
  } catch (error) {
    console.error("=== ERRO AO CONECTAR COM BACKEND (YOUTUBE) ===", error.message);
    return MOCK_YOUTUBE_VIDEOS[0];
  }
};
