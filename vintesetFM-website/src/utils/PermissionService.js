/**
 * PermissionService — shim fino (retrocompatibilidade).
 * A lógica foi unificada na FONTE ÚNICA: src/config/permissions.js.
 * Consumidores antigos (MinhaConta, PermissionGate, server/routes/users.js,
 * tests/) continuam importando `can`, `canManageTarget`, `getUserMaxLevel`,
 * `ROLES`, `PermissionService`, etc. daqui sem alteração.
 */
export * from '../config/permissions.js';
