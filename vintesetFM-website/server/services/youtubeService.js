import axios from 'axios';
import dotenv from 'dotenv';
dotenv.config();

const YOUTUBE_API_KEY = process.env.YOUTUBE_API_KEY;
const YOUTUBE_CHANNEL_ID = process.env.YOUTUBE_CHANNEL_ID || 'UCYj8T4zLqL9V0eI5iK2N1vQ'; 

export const getYoutubeLatestVideo = async () => {
  if (!YOUTUBE_API_KEY) throw new Error('YOUTUBE_API_KEY não configurada no .env');

  try {
    const api = axios.create({
      baseURL: 'https://www.googleapis.com/youtube/v3'
    });

    const res = await api.get('/search', {
      params: {
        key: YOUTUBE_API_KEY,
        channelId: YOUTUBE_CHANNEL_ID,
        part: 'snippet',
        order: 'date',
        maxResults: 6, // Margem garantida pedida p/ order: date
        type: 'video'
      }
    });

    if (res.data.items.length === 0) {
      return null;
    }

    const videoIds = res.data.items.map(item => item.id.videoId).join(',');
    
    // Segunda chamada para pegar detalhes dos vídeos (para filtrar ex-lives e pegar duração de verdade)
    const videosRes = await api.get('/videos', {
      params: {
        key: YOUTUBE_API_KEY,
        id: videoIds,
        part: 'snippet,contentDetails,liveStreamingDetails'
      }
    });

    // Filtra os vídeos reais (que NÃO possuem liveStreamingDetails, ou seja, nunca foram lives/estreias)
    const realVideos = videosRes.data.items.filter(video => 
      !video.liveStreamingDetails
    );

    if (realVideos.length === 0) {
       return null;
    }

    const latestVideo = realVideos[0];

    // Formatar duração do formato ISO 8601 (Ex: PT10M3S) para MM:SS
    const parseDuration = (isoStr) => {
      const match = isoStr.match(/PT(\d+H)?(\d+M)?(\d+S)?/);
      if (!match) return "00:00";
      const hours = (parseInt(match[1]) || 0);
      const minutes = (parseInt(match[2]) || 0);
      const seconds = (parseInt(match[3]) || 0);
      
      let res = '';
      if (hours > 0) res += `${hours}:`;
      res += `${hours > 0 ? minutes.toString().padStart(2, '0') : minutes}:${seconds.toString().padStart(2, '0')}`;
      return res;
    };

    return {
      id: latestVideo.id,
      title: latestVideo.snippet.title,
      thumbnail: latestVideo.snippet.thumbnails.high.url,
      publishedAt: latestVideo.snippet.publishedAt,
      duration: parseDuration(latestVideo.contentDetails?.duration || "")
    };

  } catch (error) {
    console.error('[YouTube Service] Erro:', error.response?.data || error.message);
    throw error;
  }
};
