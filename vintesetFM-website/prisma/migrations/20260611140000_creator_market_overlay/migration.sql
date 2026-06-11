-- Fase 3b — mercado e overlay POR CRIADOR.
-- Escrita à mão: envolve troca de PK (ReiDaMesaState singleton -> 1 por criador)
-- e backfill, que o migrate diff não gera sozinho. Preserva os dados atuais
-- migrando-os para o Creator #1.

-- ============================================================
-- ReiDaMesaState: de singleton (id=1) para 1 linha por criador
-- ============================================================
-- 1. Adiciona creatorId (nullable) e migra a linha existente pro Creator #1,
--    preservando o isMarketOpen atual.
ALTER TABLE `ReiDaMesaState` ADD COLUMN `creatorId` VARCHAR(191) NULL;
UPDATE `ReiDaMesaState` SET `creatorId` = (SELECT `id` FROM `Creator` ORDER BY `createdAt` ASC LIMIT 1);

-- 2. Troca a PK de `id` para `creatorId`.
ALTER TABLE `ReiDaMesaState` DROP PRIMARY KEY;
ALTER TABLE `ReiDaMesaState` MODIFY `creatorId` VARCHAR(191) NOT NULL;
ALTER TABLE `ReiDaMesaState` DROP COLUMN `id`;
ALTER TABLE `ReiDaMesaState` ADD PRIMARY KEY (`creatorId`);

-- 3. FK para Creator.
ALTER TABLE `ReiDaMesaState` ADD CONSTRAINT `ReiDaMesaState_creatorId_fkey` FOREIGN KEY (`creatorId`) REFERENCES `Creator`(`id`) ON DELETE RESTRICT ON UPDATE CASCADE;

-- ============================================================
-- OverlayEvent: adiciona creatorId (backfill pro Creator #1)
-- ============================================================
ALTER TABLE `OverlayEvent` ADD COLUMN `creatorId` VARCHAR(191) NULL;
UPDATE `OverlayEvent` SET `creatorId` = (SELECT `id` FROM `Creator` ORDER BY `createdAt` ASC LIMIT 1) WHERE `creatorId` IS NULL;
ALTER TABLE `OverlayEvent` MODIFY `creatorId` VARCHAR(191) NOT NULL;

-- Troca o índice antigo (só createdAt) pelo composto (creatorId, createdAt).
DROP INDEX `OverlayEvent_createdAt_idx` ON `OverlayEvent`;
CREATE INDEX `OverlayEvent_creatorId_createdAt_idx` ON `OverlayEvent`(`creatorId`, `createdAt`);

ALTER TABLE `OverlayEvent` ADD CONSTRAINT `OverlayEvent_creatorId_fkey` FOREIGN KEY (`creatorId`) REFERENCES `Creator`(`id`) ON DELETE RESTRICT ON UPDATE CASCADE;
