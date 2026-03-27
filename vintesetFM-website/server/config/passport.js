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
      callbackURL: process.env.NODE_ENV === 'production' ? 'https://vintesetfm.cloud/api/auth/google/callback' : 'http://localhost:3000/api/auth/google/callback',
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
      callbackURL: process.env.NODE_ENV === 'production' ? 'https://vintesetfm.cloud/api/auth/twitch/callback' : 'http://localhost:3000/api/auth/twitch/callback',
      scope: 'user:read:email',
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
                // Não sobrescrevemos o nickname da pessoa caso ela já tenha configurado no onboarding
              },
            });
          } else {
             // Caso o email não ache ninguem, tenta criar. Porém O NICKNAME PODE ESTAR USO (ex: "Fallen", se alguém criou manualmente).
             let baseNickname = profile.display_name || profile.login || 'Usuário';
             let finalNickname = baseNickname;
             let suffix = 1;
             let nicknameTaken = true;

             // Checa conflito de Nickname Unique
             while(nicknameTaken) {
                const exist = await prisma.user.findUnique({ where: { nickname: finalNickname }});
                if (exist) {
                   finalNickname = `${baseNickname}${suffix}`;
                   suffix++;
                } else {
                   nicknameTaken = false;
                }
             }

             user = await prisma.user.create({
              data: {
                twitchId: profile.id,
                name: profile.display_name || profile.login || 'Twitch User',
                nickname: finalNickname,
                email: email || null,
                avatar: profile.profile_image_url || null,
              },
            });
          }
        }
        return done(null, user);
      } catch (err) {
        console.error('[Twitch OAuth Error]', err);
        return done(err, null);
      }
    }
  )
);

export default passport;
