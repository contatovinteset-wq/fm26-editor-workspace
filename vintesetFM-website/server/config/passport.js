import passport from 'passport';
import { Strategy as GoogleStrategy } from 'passport-google-oauth20';
import { Strategy as TwitchStrategy } from 'passport-twitch-new';
import { PrismaClient } from '@prisma/client';

const prisma = new PrismaClient();

// Google OAuth
passport.use(
  new GoogleStrategy(
    {
      clientID: process.env.GOOGLE_CLIENT_ID || 'placeholder',
      clientSecret: process.env.GOOGLE_CLIENT_SECRET || 'placeholder',
      callbackURL: process.env.NODE_ENV === 'production' ? 'https://vintesetfm.com.br/api/auth/google/callback' : 'http://localhost:3000/api/auth/google/callback',
    },
    async (accessToken, refreshToken, profile, done) => {
      try {
        let user = await prisma.user.findUnique({
          where: { googleId: profile.id },
        });

        if (!user) {
          const email = profile.emails?.[0]?.value;
          if (email) {
            user = await prisma.user.findUnique({ where: { email } });
          }

          if (user) {
            user = await prisma.user.update({
              where: { id: user.id },
              data: { googleId: profile.id },
            });
          } else {
            user = await prisma.user.create({
              data: {
                googleId: profile.id,
                name: profile.displayName,
                nickname: null, // Forçar onboarding para Google
                email: email,
                avatar: profile.photos?.[0]?.value,
              },
            });
          }
        }
        return done(null, user);
      } catch (err) {
        return done(err, null);
      }
    }
  )
);

// Twitch OAuth
passport.use(
  new TwitchStrategy(
    {
      clientID: process.env.TWITCH_CLIENT_ID || 'placeholder',
      clientSecret: process.env.TWITCH_CLIENT_SECRET || 'placeholder',
      callbackURL: process.env.NODE_ENV === 'production' ? 'https://vintesetfm.com.br/api/auth/twitch/callback' : 'http://localhost:3000/api/auth/twitch/callback',
      scope: 'user_read',
    },
    async (accessToken, refreshToken, profile, done) => {
      try {
        let user = await prisma.user.findUnique({
          where: { twitchId: profile.id },
        });

        if (!user) {
          const email = profile.email;
          if (email) {
             user = await prisma.user.findUnique({ where: { email } });
          }
          if (user) {
            user = await prisma.user.update({
              where: { id: user.id },
              data: { 
                twitchId: profile.id,
                nickname: profile.display_name || profile.login 
              },
            });
          } else {
             user = await prisma.user.create({
              data: {
                twitchId: profile.id,
                name: profile.display_name || profile.login,
                nickname: profile.display_name || profile.login, // Twitch já tem nick
                email: email,
                avatar: profile.profile_image_url,
              },
            });
          }
        }
        return done(null, user);
      } catch (err) {
        return done(err, null);
      }
    }
  )
);

export default passport;
