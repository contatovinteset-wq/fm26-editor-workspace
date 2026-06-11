-- Fase 2c — ENFORCE multi-tenant.
-- Torna creatorId NOT NULL (o backfill da 2b garantiu 0 nulls) e troca os
-- uniques globais por uniques compostos por criador.
--
-- ⚠️ ORDEM IMPORTA: os índices compostos sao criados ANTES de dropar os
-- uniques antigos. Squad.userId tem FK pra User (Squad_userId_fkey), e no
-- MySQL nao se pode dropar o unico indice que cobre uma coluna com FK
-- (erro 1553). O indice composto Squad_userId_creatorId_key cobre o userId,
-- entao precisa existir antes do DROP INDEX Squad_userId_key.

-- 1. Solta as FKs de creatorId para permitir o MODIFY da coluna.
ALTER TABLE `Squad` DROP FOREIGN KEY `Squad_creatorId_fkey`;
ALTER TABLE `Player` DROP FOREIGN KEY `Player_creatorId_fkey`;
ALTER TABLE `Round` DROP FOREIGN KEY `Round_creatorId_fkey`;
ALTER TABLE `PlayerScore` DROP FOREIGN KEY `PlayerScore_creatorId_fkey`;
ALTER TABLE `CraqueVote` DROP FOREIGN KEY `CraqueVote_creatorId_fkey`;

-- 2. creatorId vira NOT NULL (backfill 2b ja preencheu tudo).
ALTER TABLE `Squad` MODIFY `creatorId` VARCHAR(191) NOT NULL;
ALTER TABLE `Player` MODIFY `creatorId` VARCHAR(191) NOT NULL;
ALTER TABLE `Round` MODIFY `creatorId` VARCHAR(191) NOT NULL;
ALTER TABLE `PlayerScore` MODIFY `creatorId` VARCHAR(191) NOT NULL;
ALTER TABLE `CraqueVote` MODIFY `creatorId` VARCHAR(191) NOT NULL;

-- 3. Cria os indices unicos compostos ANTES de dropar os antigos.
CREATE UNIQUE INDEX `Squad_userId_creatorId_key` ON `Squad`(`userId`, `creatorId`);
CREATE UNIQUE INDEX `Player_creatorId_uidName_key` ON `Player`(`creatorId`, `uidName`);
CREATE UNIQUE INDEX `Round_creatorId_number_key` ON `Round`(`creatorId`, `number`);

-- 4. Agora pode dropar os uniques globais antigos com seguranca.
DROP INDEX `Squad_userId_key` ON `Squad`;
DROP INDEX `Player_uidName_key` ON `Player`;
DROP INDEX `Round_number_key` ON `Round`;

-- 5. Recoloca as FKs de creatorId.
ALTER TABLE `Squad` ADD CONSTRAINT `Squad_creatorId_fkey` FOREIGN KEY (`creatorId`) REFERENCES `Creator`(`id`) ON DELETE RESTRICT ON UPDATE CASCADE;
ALTER TABLE `Player` ADD CONSTRAINT `Player_creatorId_fkey` FOREIGN KEY (`creatorId`) REFERENCES `Creator`(`id`) ON DELETE RESTRICT ON UPDATE CASCADE;
ALTER TABLE `Round` ADD CONSTRAINT `Round_creatorId_fkey` FOREIGN KEY (`creatorId`) REFERENCES `Creator`(`id`) ON DELETE RESTRICT ON UPDATE CASCADE;
ALTER TABLE `PlayerScore` ADD CONSTRAINT `PlayerScore_creatorId_fkey` FOREIGN KEY (`creatorId`) REFERENCES `Creator`(`id`) ON DELETE RESTRICT ON UPDATE CASCADE;
ALTER TABLE `CraqueVote` ADD CONSTRAINT `CraqueVote_creatorId_fkey` FOREIGN KEY (`creatorId`) REFERENCES `Creator`(`id`) ON DELETE RESTRICT ON UPDATE CASCADE;
