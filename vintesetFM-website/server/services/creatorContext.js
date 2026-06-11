// Resolver do criador atual do Rei da Mesa.
//
// Hoje o sistema é single-tenant: existe um único Creator (#1 = vinteset),
// criado no backfill da Fase 2b. Como a Fase 2c torna `creatorId` obrigatório
// (NOT NULL) em Player/Round/Squad/PlayerScore/CraqueVote, TODO write precisa
// carimbar o creatorId — e por ora todos apontam pro Creator #1.
//
// Quando o multistream (Fase 3) chegar, este resolver será substituído por
// resolução via request (slug na rota / usuário dono / etc). Manter o ponto
// único aqui deixa essa troca trivial.

let _cachedCreatorId = null;

// Slugs reservadas (colidiriam com rotas / uso interno do Rei da Mesa).
export const RESERVED_SLUGS = new Set(['overlay', 'criadores', 'admin', 'escalar', 'plantel', 'ranking', 'perfil', 'c', 'api']);

// Gera uma slug segura a partir de um texto (nickname/nome).
export function slugify(input) {
  const s = (input || '')
    .toString()
    .normalize('NFKD').replace(/[̀-ͯ]/g, '') // tira acentos
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 30);
  return s || 'criador';
}

// Garante uma slug única no banco (sufixa -2, -3… se preciso) e não-reservada.
async function uniqueSlug(prisma, base) {
  let root = base;
  if (RESERVED_SLUGS.has(root)) root = `${root}-rdm`.slice(0, 30);
  let slug = root;
  let n = 1;
  // eslint-disable-next-line no-await-in-loop
  while (await prisma.creator.findUnique({ where: { slug }, select: { id: true } })) {
    n += 1;
    slug = `${root}-${n}`.slice(0, 30);
  }
  return slug;
}

// Fase 3e: ao conceder o cargo CREATOR, cria o Rei da Mesa do usuário —
// INATIVO. Ele só ativa quando o criador preencher o próprio perfil
// (nome + ≥1 plataforma). Idempotente: não duplica se já existir.
export async function ensureCreatorForUser(prisma, user) {
  const existing = await prisma.creator.findFirst({ where: { ownerId: user.id }, select: { id: true } });
  if (existing) return existing;
  const base = slugify(user.nickname || user.name || `criador-${String(user.id).slice(0, 6)}`);
  const slug = await uniqueSlug(prisma, base);
  return prisma.creator.create({
    data: {
      ownerId: user.id,
      name: user.nickname || user.name || 'Novo criador',
      slug,
      isActive: false
    }
  });
}

// Ao remover o cargo CREATOR, desativa os Rei da Mesa do usuário (preserva dados).
export async function deactivateCreatorsForUser(prisma, userId) {
  await prisma.creator.updateMany({
    where: { ownerId: userId, isActive: true },
    data: { isActive: false }
  });
}

export async function getDefaultCreatorId(prisma) {
  if (_cachedCreatorId) return _cachedCreatorId;
  const creator = await prisma.creator.findFirst({ orderBy: { createdAt: 'asc' } });
  if (!creator) {
    throw new Error('Nenhum Creator configurado no banco (esperado o Creator #1 do backfill da Fase 2b).');
  }
  _cachedCreatorId = creator.id;
  return _cachedCreatorId;
}

// Resolve qual criador este request está operando.
// Fase 3c: lê a slug do header `X-Creator-Slug` (injetado pelo front via
// CreatorContext) ou do query `?creator=`. Sem slug => Creator #1 (vinteset),
// preservando o /reidamesa bare e o overlay atual do OBS.
export async function resolveCreatorId(req, prisma) {
  const slug = (req.headers['x-creator-slug'] || req.query?.creator || '')
    .toString().trim().toLowerCase();

  if (!slug) {
    return getDefaultCreatorId(prisma); // backward compat: vinteset
  }

  const creator = await prisma.creator.findFirst({
    where: { slug, isActive: true },
    select: { id: true }
  });
  if (!creator) {
    const err = new Error(`Criador não encontrado ou inativo: ${slug}`);
    err.code = 'CREATOR_NOT_FOUND';
    throw err;
  }
  return creator.id;
}

// Middleware: injeta req.creatorId em todo request do Rei da Mesa.
// Centraliza a resolução num ponto só — na 3c basta evoluir resolveCreatorId.
export function attachCreatorContext(prisma) {
  return async (req, res, next) => {
    try {
      req.creatorId = await resolveCreatorId(req, prisma);
      next();
    } catch (err) {
      console.error('attachCreatorContext:', err);
      res.status(404).json({ error: 'Criador não encontrado' });
    }
  };
}

// Para testes / cenários onde o creator pode mudar em runtime.
export function _clearCreatorCache() {
  _cachedCreatorId = null;
}
