import axios from 'axios';

// URL de desenvolvimento ou produção
const API_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:3000';

const localApi = axios.create({
  baseURL: API_URL
});

// Fallback ultra-seguro garantindo contexto do vintesetFM
export const getFallbackVideo = () => {
  return {
    id: "aX9-JXXO3W8", // Placeholder atualizado para canal correto
    title: "Extraindo Jogadores Escondidos do FM26: O Guia Definitivo",
    thumbnail: "https://images.unsplash.com/photo-1542751371-adc38448a05e?q=80&w=1200&auto=format&fit=crop", // generic FM
    publishedAt: new Date().toISOString(),
    duration: "14:32"
  };
};

export const getLatestNonLiveVideo = async () => {
  try {
    const response = await localApi.get(`/api/youtube/latest`);
    const validVideo = response.data;
    if (validVideo && validVideo.id) {
      // Salva em localStorage como contingência "O último que pegou mas precisa ser do meu canal vintesetFM"
      try {
        localStorage.setItem('vinteset_last_video', JSON.stringify(validVideo));
      } catch (e) {}
      return validVideo;
    }
  } catch (error) {
    console.warn("=== YOUTUBE API UNAVAILABLE, FALLING BACK TO CACHE ===", error.message);
  }

  // Tratativa: Se a API falhar, resgatar o último vídeo válido salvo no cache
  try {
    const cached = localStorage.getItem('vinteset_last_video');
    if (cached) {
      return JSON.parse(cached);
    }
  } catch (e) {}

  // Se o cache estiver vazio e a API fora, usar fallback seguro
  return getFallbackVideo();
};
