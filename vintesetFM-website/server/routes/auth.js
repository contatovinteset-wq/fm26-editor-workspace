import express from 'express';
import passport from '../config/passport.js';
import jwt from 'jsonwebtoken';
import { PrismaClient } from '@prisma/client';
import rateLimit from 'express-rate-limit';
import bcrypt from 'bcrypt';

const prisma = new PrismaClient();
const router = express.Router();

const loginLimiter = rateLimit({
  windowMs: 15 * 60 * 1000, // 15 minutos
  max: 20, // Limita cada IP a 20 requisições de login por janela
  message: 'Excesso de tentativas de login, tente novamente mais tarde.'
});

const generateToken = (user) => {
  const secret = process.env.JWT_SECRET || 'fallback_secret';
  return jwt.sign({ id: user.id, roles: user.roles }, secret, {
    expiresIn: '7d',
  });
};

const setJWTCookie = (res, token) => {
  res.cookie('jwt', token, { 
    httpOnly: true, 
    secure: process.env.NODE_ENV === 'production',
    sameSite: 'lax',
    maxAge: 7 * 24 * 60 * 60 * 1000 // 7 dias
  });
};

// ==========================================
// LOCAL AUTH (Registro / Login)
// ==========================================

router.post('/register', async (req, res) => {
  try {
    const { email, password, nickname } = req.body;
    if (!email || !password || !nickname) {
      return res.status(400).json({ error: 'Preencha todos os campos obrigatórios.' });
    }

    const existingUser = await prisma.user.findFirst({
      where: {
        OR: [{ email }, { nickname }]
      }
    });

    if (existingUser) {
      return res.status(400).json({ error: 'Email ou Nickname já está em uso.' });
    }

    const hashedPassword = await bcrypt.hash(password, 10);

    const user = await prisma.user.create({
      data: {
        email,
        nickname,
        password: hashedPassword,
        name: nickname,
      }
    });

    const token = generateToken(user);
    setJWTCookie(res, token);
    
    res.status(201).json({ success: true, user });
  } catch (error) {
    console.error('[Register Error]', error);
    res.status(500).json({ error: 'Erro interno ao criar a conta.' });
  }
});

router.post('/login', loginLimiter, async (req, res) => {
  try {
    const { email, password } = req.body;
    if (!email || !password) {
      return res.status(400).json({ error: 'Preencha email e senha.' });
    }

    const user = await prisma.user.findUnique({ where: { email } });
    if (!user || !user.password) {
      return res.status(401).json({ error: 'Credenciais inválidas.' });
    }

    const match = await bcrypt.compare(password, user.password);
    if (!match) {
      return res.status(401).json({ error: 'Credenciais inválidas.' });
    }

    const token = generateToken(user);
    setJWTCookie(res, token);

    res.json({ success: true, user });
  } catch (error) {
    console.error('[Login Error]', error);
    res.status(500).json({ error: 'Erro interno ao fazer login.' });
  }
});

// ==========================================
// GOOGLE OAUTH
// ==========================================
router.get('/google', loginLimiter, passport.authenticate('google', { scope: ['profile', 'email'] }));

router.get(
  '/google/callback',
  passport.authenticate('google', { session: false, failureRedirect: '/login?error=true' }),
  (req, res) => {
    const token = generateToken(req.user);
    setJWTCookie(res, token);
    const clientUrl = (req.hostname === 'localhost' || req.hostname === '127.0.0.1') ? 'http://localhost:5173' : '';
    
    // Se não tiver nickname, manda definir um
    if (!req.user.nickname) {
      return res.redirect(`${clientUrl}/minhaconta?onboarding=true`);
    }
    
    res.redirect(`${clientUrl}/minhaconta`);
  }
);

// ==========================================
// TWITCH OAUTH
// ==========================================
router.get('/twitch', loginLimiter, passport.authenticate('twitch'));

router.get(
  '/twitch/callback',
  passport.authenticate('twitch', { session: false, failureRedirect: '/login?error=true' }),
  (req, res) => {
    const token = generateToken(req.user);
    setJWTCookie(res, token);
    const clientUrl = (req.hostname === 'localhost' || req.hostname === '127.0.0.1') ? 'http://localhost:5173' : '';
    
    if (!req.user.nickname) {
      return res.redirect(`${clientUrl}/minhaconta?onboarding=true`);
    }
    
    res.redirect(`${clientUrl}/minhaconta`);
  }
);

// ==========================================
// SESSÃO E LOGOUT
// ==========================================
router.get('/logout', (req, res) => {
  res.clearCookie('jwt');
  res.json({ success: true, message: 'Deslogado com sucesso' });
});

router.get('/me', async (req, res) => {
  const token = req.cookies?.jwt;
  if (!token) return res.status(401).json({ user: null });

  try {
    const secret = process.env.JWT_SECRET || 'fallback_secret';
    const decoded = jwt.verify(token, secret);
    
    // Buscar dados atualizados e setar lastActiveAt
    const user = await prisma.user.update({
      where: { id: decoded.id },
      data: { lastActiveAt: new Date() }
    });

    if (!user) return res.status(401).json({ user: null });

    res.json({ user }); 
  } catch (err) {
    res.status(401).json({ user: null });
  }
});

export default router;
