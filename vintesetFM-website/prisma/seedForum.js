import { PrismaClient } from '@prisma/client';

const prisma = new PrismaClient();

const defaultCategories = [
  { slug: 'taticas', name: 'Táticas', description: 'Discuta estratégias, formações e compartilhe suas táticas vencedoras.' },
  { slug: 'dicas', name: 'Dicas', description: 'Ajuda, tutoriais e dicas úteis sobre o jogo.' },
  { slug: 'discussoes', name: 'Discussões', description: 'Papo geral sobre Football Manager e futebol na vida real.' },
  { slug: 'diario-de-save', name: 'Diário de Save', description: 'Conte a história do seu save, desafios e conquistas.' }
];

async function main() {
  console.log('🌱 Inicializando seed das categorias do Fórum...');
  
  for (const cat of defaultCategories) {
    await prisma.forumCategory.upsert({
      where: { slug: cat.slug },
      update: {},
      create: cat
    });
  }
  
  console.log('✅ Categorias do Fórum criadas/verificadas com sucesso!');
}

main()
  .catch((e) => {
    console.error(e);
    process.exit(1);
  })
  .finally(async () => {
    await prisma.$disconnect();
  });
