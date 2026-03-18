import express from 'express';
import cors from 'cors';
import { getTwitchChannelData } from './services/twitchService.js';
import { getYoutubeLatestVideo } from './services/youtubeService.js';

const app = express();
const PORT = process.env.PORT || 3000;

app.use(cors());
app.use(express.json());

// Rota Twitch
app.get('/api/twitch/channel', async (req, res) => {
  try {
    const data = await getTwitchChannelData();
    res.json(data);
  } catch (error) {
    res.status(500).json({ error: 'Erro ao buscar dados da Twitch.', details: error.message });
  }
});

// Rota YouTube
app.get('/api/youtube/latest', async (req, res) => {
  try {
    const data = await getYoutubeLatestVideo();
    res.json(data);
  } catch (error) {
    res.status(500).json({ error: 'Erro ao buscar dados do YouTube.', details: error.message });
  }
});

app.listen(PORT, () => {
  console.log(`[VintesetFM Backend] Servidor rodando na porta ${PORT}`);
});
