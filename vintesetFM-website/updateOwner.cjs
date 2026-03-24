const { PrismaClient } = require('@prisma/client');
const prisma = new PrismaClient();

async function main() {
  const user = await prisma.user.updateMany({
    where: { nickname: 'vinteset' },
    data: { role: 'OWNER' }
  });
  console.log('Update result:', user);
}

main().catch(e => console.error(e)).finally(async () => {
  await prisma.$disconnect();
});
