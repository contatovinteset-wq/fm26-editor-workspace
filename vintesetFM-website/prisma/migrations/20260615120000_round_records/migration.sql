-- Fase 6 (Sala de Troféus — recordes): campeão e maior pontuação por rodada.
-- Aditivo e nullable; os recordes acumulam a partir de agora.
ALTER TABLE `Round` ADD COLUMN `championId` VARCHAR(191) NULL,
    ADD COLUMN `championName` VARCHAR(191) NULL,
    ADD COLUMN `topScore` DOUBLE NULL;
