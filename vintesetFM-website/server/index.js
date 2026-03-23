import express from 'express';
import cors from 'cors';
import cookieParser from 'cookie-parser';
import passport from './config/passport.js';
import { getTwitchChannelData } from './services/twitchService.js';
import { getYoutubeLatestVideo } from './services/youtubeService.js';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

import authRoutes from './routes/auth.js';
import forumRoutes from './routes/forum.js';
import reiDaMesaRoutes from './routes/reidamesa.js';

const app = express();
const PORT = process.env.PORT || 3000;

// Configuração CORS dinâmica para Cookies JWT
app.use(cors({
  origin: process.env.NODE_ENV === 'production' ? 'https://seu_dominio.com' : 'http://localhost:5173',
  credentials: true,
}));

app.use(express.json());
app.use(cookieParser());
app.use(passport.initialize());

// Rotas MVC
app.use('/api/auth', authRoutes);
app.use('/api/forum', forumRoutes);
app.use('/api/reidamesa', reiDaMesaRoutes);

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

// Servir frontend compilado em ambiente de Produção
if (process.env.NODE_ENV === 'production') {
  app.use(express.static(path.join(__dirname, '../dist')));
  app.get('*', (req, res) => {
    res.sendFile(path.join(__dirname, '../dist/index.html'));
  });
}

app.listen(PORT, () => {
  console.log(`[VintesetFM Backend] Servidor rodando na porta ${PORT}`);
});
