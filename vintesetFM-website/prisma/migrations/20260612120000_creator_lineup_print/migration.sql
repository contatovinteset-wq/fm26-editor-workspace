-- Fase 6: print da escalação do criador (base64 no banco). Aditivo e nullable.
ALTER TABLE `Creator` ADD COLUMN `lineupPrint` LONGTEXT NULL,
    ADD COLUMN `lineupPrintAt` DATETIME(3) NULL;
