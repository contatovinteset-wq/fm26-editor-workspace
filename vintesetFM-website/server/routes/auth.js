import express from 'express';
import passport from '../config/passport.js';
import jwt from 'jsonwebtoken';

const router = express.Router();

const generateToken = (user) => {
  // Use a secret do .env, se não houver usa fallback provisório
  const secret = process.env.JWT_SECRET || 'fallback_secret';
  return jwt.sign({ id: user.id, role: user.role, name: user.name, avatar: user.avatar }, secret, {
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
// GOOGLE OAUTH
// ==========================================
router.get('/google', passport.authenticate('google', { scope: ['profile', 'email'] }));

router.get(
  '/google/callback',
  passport.authenticate('google', { session: false, failureRedirect: '/login?error=true' }),
  (req, res) => {
    const token = generateToken(req.user);
    setJWTCookie(res, token);
    res.redirect('/minhaconta');
  }
);

// ==========================================
// TWITCH OAUTH
// ==========================================
router.get('/twitch', passport.authenticate('twitch'));

router.get(
  '/twitch/callback',
  passport.authenticate('twitch', { session: false, failureRedirect: '/login?error=true' }),
  (req, res) => {
    const token = generateToken(req.user);
    setJWTCookie(res, token);
    res.redirect('/minhaconta');
  }
);

// ==========================================
// SESSÃO E LOGOUT
// ==========================================
router.get('/logout', (req, res) => {
  res.clearCookie('jwt');
  res.json({ success: true, message: 'Deslogado com sucesso' });
});

router.get('/me', (req, res) => {
  const token = req.cookies?.jwt;
  if (!token) return res.status(401).json({ user: null });

  try {
    const secret = process.env.JWT_SECRET || 'fallback_secret';
    const decoded = jwt.verify(token, secret);
    res.json({ user: decoded }); 
  } catch (err) {
    res.status(401).json({ user: null });
  }
});

export default router;
