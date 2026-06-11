-- Fase 2b — BACKFILL multi-tenant (migration de DADOS, escrita à mão).
-- Cria o "Creator #1" (o Rei da Mesa atual = vinteset) vinculado ao OWNER do site
-- e carimba creatorId em todas as linhas já existentes. Roda uma única vez.

-- 1. Cria o Creator #1 com dono = usuário OWNER do site.
INSERT INTO `Creator` (`id`, `ownerId`, `name`, `slug`, `isActive`, `createdAt`, `updatedAt`)
SELECT UUID(), u.`id`, 'vinteset', 'vinteset', true, NOW(3), NOW(3)
FROM `User` u
WHERE JSON_CONTAINS(u.`roles`, '"OWNER"')
ORDER BY u.`createdAt` ASC
LIMIT 1;

-- 2. Vincula tudo que já existe ao Creator #1.
UPDATE `Player`      SET `creatorId` = (SELECT `id` FROM `Creator` ORDER BY `createdAt` ASC LIMIT 1) WHERE `creatorId` IS NULL;
UPDATE `Round`       SET `creatorId` = (SELECT `id` FROM `Creator` ORDER BY `createdAt` ASC LIMIT 1) WHERE `creatorId` IS NULL;
UPDATE `Squad`       SET `creatorId` = (SELECT `id` FROM `Creator` ORDER BY `createdAt` ASC LIMIT 1) WHERE `creatorId` IS NULL;
UPDATE `PlayerScore` SET `creatorId` = (SELECT `id` FROM `Creator` ORDER BY `createdAt` ASC LIMIT 1) WHERE `creatorId` IS NULL;
UPDATE `CraqueVote`  SET `creatorId` = (SELECT `id` FROM `Creator` ORDER BY `createdAt` ASC LIMIT 1) WHERE `creatorId` IS NULL;
