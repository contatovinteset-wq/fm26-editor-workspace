import assert from 'assert';
import { can, canManageTarget, getUserMaxLevel, ROLES } from '../src/utils/PermissionService.js';

// Mocks de Usuários
const ownerUser = { roles: [ROLES.OWNER] };
const adminUser = { roles: [ROLES.ADMIN] };
const adminDownloadsUser = { roles: [ROLES.ADMIN_DOWNLOADS] };
const standardUser = { roles: [ROLES.USER] };

try {
  console.log('Iniciando Testes Unitários de Permissão (Parte 1)...');

  // Teste 1: Ações globais irreversiveis (Mudar Nickname de outro)
  assert.strictEqual(can(ownerUser, 'change_nickname'), true, 'Owner deve poder mudar nickname');
  assert.strictEqual(can(adminUser, 'change_nickname'), true, 'Admin deve poder mudar nickname (moderação)');
  assert.strictEqual(can(standardUser, 'change_nickname'), false, 'User comum não tem a permissão bruta de mudar nickname irrestritamente');

  // Teste 2: Aprovação de Downloads
  assert.strictEqual(can(adminDownloadsUser, 'approve_download'), true, 'Admin de Downloads deve poder aprovar um arquivo');
  assert.strictEqual(can(standardUser, 'approve_download'), false, 'Usuário comum não pode aprovar downloads');

  // Teste 3: Hierarquia e Delegação
  assert.strictEqual(canManageTarget(ownerUser.roles, adminUser.roles), true, 'Owner pode gerenciar Admin');
  assert.strictEqual(canManageTarget(adminUser.roles, standardUser.roles), true, 'Admin pode gerenciar User');
  assert.strictEqual(canManageTarget(adminUser.roles, ownerUser.roles), false, 'Admin NÃO pode gerenciar Owner');
  assert.strictEqual(canManageTarget(adminDownloadsUser.roles, adminUser.roles), false, 'Admin específico NÃO pode gerenciar Admin geral');

  // Teste 4: Verificação de Nível Máximo
  assert.strictEqual(getUserMaxLevel([ROLES.USER, ROLES.ADMIN_DOWNLOADS]), 60, 'O nivel máximo deve ser 60 (Admin Downloads)');

  console.log('✅ Todos os testes críticos de permissão passaram com sucesso!');
} catch (error) {
  console.error('❌ Falha nos testes de permissão:', error.message);
  process.exit(1);
}
