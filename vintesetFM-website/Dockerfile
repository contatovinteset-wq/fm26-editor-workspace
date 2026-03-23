FROM node:20-alpine

WORKDIR /app

# Instalar pacotes base (necessário dependendo do Prisma e dependências OS)
RUN apk add --no-cache openssl

# Copiar apenas os arquivos de dependência pra cache
COPY package*.json ./
COPY prisma ./prisma/

# Instala as libs
RUN npm install

# Gerar o cliente Prisma
RUN npx prisma generate

# Copiar todo o código fonte
COPY . .

# Fazer a compilação de produção (Gera a pasta /dist para o front-end)
RUN npm run build

# Expõe a porta que o app enxerga
EXPOSE 3000

# Variáveis default importantes (Serão sobrescritas pelo Coolify)
ENV NODE_ENV=production
ENV PORT=3000

# Executar a aplicação
CMD ["npm", "start"]
