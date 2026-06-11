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

// Para testes / cenários onde o creator pode mudar em runtime.
export function _clearCreatorCache() {
  _cachedCreatorId = null;
}
