import express from 'express';
import cors from 'cors';
import cookieParser from 'cookie-parser';
import helmet from 'helmet';
import passport from './config/passport.js';
import { getTwitchChannelData } from './services/twitchService.js';
import { getYoutubeLatestVideo } from './services/youtubeService.js';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

import authRoutes from './routes/auth.js';
import forumRoutes from './routes/forum.js';
import moderationRoutes from './routes/moderation.js';
import reiDaMesaRoutes from './routes/reidamesa.js';
import adminRoutes from './routes/admin.js';
import userRoutes from './routes/users.js';
import boardRoutes from './routes/board.js';

import fs from 'fs';

const app = express();
app.set('trust proxy', 1);

// Garante que a pasta de uploads exista
const uploadsDir = path.join(__dirname, 'uploads');
if (!fs.existsSync(uploadsDir)) {
  fs.mkdirSync(uploadsDir, { recursive: true });
}

const PORT = process.env.PORT || 3000;

app.use(helmet({
  contentSecurityPolicy: false,
  crossOriginResourcePolicy: { policy: "cross-origin" }
}));

app.use(cors({
  origin: process.env.NODE_ENV === 'production' ? ['https://vintesetfm.com.br', 'https://www.vintesetfm.com.br', 'https://vintesetfm.cloud'] : 'http://localhost:5173',
  credentials: true,
}));

app.use('/uploads', express.static(uploadsDir));
app.use(express.json({ limit: '10mb' }));
app.use(cookieParser());
app.use(passport.initialize());

// Rotas MVC
app.use('/api/auth', authRoutes);
app.use('/api/users', userRoutes);
app.use('/api/forum', forumRoutes);
app.use('/api/moderation', moderationRoutes);
app.use('/api/reidamesa', reiDaMesaRoutes);
app.use('/api/admin', adminRoutes);
app.use('/api/board', boardRoutes);

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
  app.get(/.*/, (req, res) => {
    res.sendFile(path.join(__dirname, '../dist/index.html'));
  });
}

app.listen(PORT, () => {
  console.log(`[VintesetFM Backend] Servidor rodando na porta ${PORT}`);
});
