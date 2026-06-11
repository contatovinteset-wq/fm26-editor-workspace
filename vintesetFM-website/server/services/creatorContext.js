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
// Fase 3a: ainda single-tenant — sempre o Creator #1 (vinteset).
// Fase 3c: passará a ler a slug de req.params.slug / req.query.creator
//          (rotas /reidamesa/c/:slug) e o painel admin resolverá pelo dono logado.
export async function resolveCreatorId(req, prisma) {
  // const slug = req.params?.slug || req.query?.creator; // <- ligado na Fase 3c
  return getDefaultCreatorId(prisma);
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
