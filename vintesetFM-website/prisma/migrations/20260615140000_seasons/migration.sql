-- Gamificação (G1) — Temporadas + histórico por rodada.
-- Tudo aditivo e nullable; nada destrutivo. Os dados acumulam a partir de agora.

-- AlterTable
ALTER TABLE `Round` ADD COLUMN `seasonId` VARCHAR(191) NULL;

-- CreateTable
CREATE TABLE `Season` (
    `id` VARCHAR(191) NOT NULL,
    `number` INTEGER NOT NULL,
    `name` VARCHAR(191) NULL,
    `isActive` BOOLEAN NOT NULL DEFAULT true,
    `startedAt` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    `endedAt` DATETIME(3) NULL,
    `championId` VARCHAR(191) NULL,
    `championName` VARCHAR(191) NULL,
    `championScore` DOUBLE NULL,
    `creatorId` VARCHAR(191) NOT NULL,

    UNIQUE INDEX `Season_creatorId_number_key`(`creatorId`, `number`),
    INDEX `Season_creatorId_isActive_idx`(`creatorId`, `isActive`),
    PRIMARY KEY (`id`)
) DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- CreateTable
CREATE TABLE `RoundEntry` (
    `id` VARCHAR(191) NOT NULL,
    `roundId` VARCHAR(191) NOT NULL,
    `userId` VARCHAR(191) NOT NULL,
    `seasonId` VARCHAR(191) NULL,
    `score` DOUBLE NOT NULL DEFAULT 0.0,
    `rank` INTEGER NULL,
    `createdAt` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    `creatorId` VARCHAR(191) NOT NULL,

    UNIQUE INDEX `RoundEntry_roundId_userId_key`(`roundId`, `userId`),
    INDEX `RoundEntry_creatorId_seasonId_idx`(`creatorId`, `seasonId`),
    INDEX `RoundEntry_userId_idx`(`userId`),
    PRIMARY KEY (`id`)
) DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- AddForeignKey
ALTER TABLE `Season` ADD CONSTRAINT `Season_creatorId_fkey` FOREIGN KEY (`creatorId`) REFERENCES `Creator`(`id`) ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE `RoundEntry` ADD CONSTRAINT `RoundEntry_creatorId_fkey` FOREIGN KEY (`creatorId`) REFERENCES `Creator`(`id`) ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE `RoundEntry` ADD CONSTRAINT `RoundEntry_roundId_fkey` FOREIGN KEY (`roundId`) REFERENCES `Round`(`id`) ON DELETE RESTRICT ON UPDATE CASCADE;
