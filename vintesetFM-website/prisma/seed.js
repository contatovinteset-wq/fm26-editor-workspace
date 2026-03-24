import { PrismaClient } from '@prisma/client';
const prisma = new PrismaClient();

async function main() {
  console.log('Iniciando DB Seed...');

  // Criar categorias padrão do Ranking se não existirem
  console.log('População de categorias mock...');

  // Seed de PERFIS/ROLES básicos a partir do Owner
  const ownerEmail = 'contato@vinteset.com.br'; // Exemplo
  const ownerUser = await prisma.user.findFirst({ where: { email: ownerEmail } });

  if (!ownerUser) {
    console.log(`Nenhum usuário OWNER encontrado com email ${ownerEmail}.`);
    console.log('Caso seja o primeiro deploy, não esqueça de fazer login com sua conta principal e alterar a Role para OWNER manualmente no DB via Prisma Studio.');
  } else if (!ownerUser.roles.includes('OWNER')) {
    await prisma.user.update({
      where: { id: ownerUser.id },
      data: { roles: ['OWNER'] }
    });
    console.log(`Usuário ${ownerEmail} promovido a OWNER.`);
  }

  // Exemplos de Mocks para Admin Downloads (Ajuste o email depois)
  const mockAdminDownloadsEmail = 'admin_downloads@vinteset.com.br';
  const existingAdmin = await prisma.user.findFirst({ where: { email: mockAdminDownloadsEmail } });

  if (!existingAdmin) {
    // Apenas para fins de mock/desenvolvimento
    await prisma.user.create({
      data: {
        email: mockAdminDownloadsEmail,
        name: 'Curador de Downloads',
        nickname: 'AdminDownload',
        roles: ['ADMIN_DOWNLOADS'],
        nickname_defined: true
      }
    });
    console.log('Membro ADMIN_DOWNLOADS criado.');
  }

  console.log('Seed Finalizado com a arquitetura de Permissões base.');
}

main()
  .catch((e) => {
    console.error(e);
    process.exit(1);
  })
  .finally(async () => {
    await prisma.$disconnect();
  });
