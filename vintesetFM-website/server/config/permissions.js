/**
 * RBAC (backend) — shim fino.
 * A lógica vive na FONTE ÚNICA isomórfica: src/config/permissions.js.
 * Mantido este caminho por retrocompatibilidade dos imports do servidor
 * (middleware/roles.js, routes/admin.js).
 */
export * from '../../src/config/permissions.js';
