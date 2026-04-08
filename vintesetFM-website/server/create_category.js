import { PrismaClient } from '@prisma/client';

const prisma = new PrismaClient();

async function main() {
  const category = await prisma.forumCategory.upsert({
    where: { slug: 'duvidas-suporte' },
    update: {},
    create: {
      slug: 'duvidas-suporte',
      name: 'Dúvidas/Suporte',
      description: 'Espaço reservado para dúvidas, suporte técnico e ajuda geral com o jogo e as ferramentas.',
      icon: 'HelpCircle'
    }
  });
  console.log('Categoria criada/verificada:', category);
}

main()
  .catch(e => console.error(e))
  .finally(async () => {
    await prisma.$disconnect();
  });
