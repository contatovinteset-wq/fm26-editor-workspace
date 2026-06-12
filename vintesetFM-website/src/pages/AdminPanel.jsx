import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { motion, AnimatePresence } from 'framer-motion';
import { Settings, Users, ShieldCheck, Search, Shield, Save, X, Edit2, ShieldAlert, Crown, Check, AlertTriangle } from 'lucide-react';
import { getAllRoles, hasPermission, ROLES } from '../config/permissions';
import RoleBadge from '../components/RoleBadge';

const ROLE_COLORS = {
  OWNER: { bg: 'bg-amber-500/20', text: 'text-amber-500', border: 'border-amber-500/50' },
  ADMIN: { bg: 'bg-red-500/20', text: 'text-red-500', border: 'border-red-500/50' },
  ADMIN_DOWNLOADS: { bg: 'bg-cyan-500/20', text: 'text-cyan-500', border: 'border-cyan-500/50' },
  ADMIN_GERACAO: { bg: 'bg-purple-500/20', text: 'text-purple-500', border: 'border-purple-500/50' },
  MODERATOR: { bg: 'bg-emerald-500/20', text: 'text-emerald-500', border: 'border-emerald-500/50' },
  USER: { bg: 'bg-blue-500/20', text: 'text-blue-500', border: 'border-blue-500/50' },
};


const AdminPanel = () => {
  const { user, isLoading } = useAuth();
  const [users, setUsers] = useState([]);
  const [loadingUsers, setLoadingUsers] = useState(true);
  const [search, setSearch] = useState('');
  const [stats, setStats] = useState({ total: 0, counts: {} });
  const [editingUser, setEditingUser] = useState(null);
  const [editRoles, setEditRoles] = useState([]);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState(null);

  // Parse das roles do usuário logado
  let userRoles = user?.roles || [];
  if (typeof userRoles === 'string') {
    try { userRoles = JSON.parse(userRoles); } catch { userRoles = [userRoles]; }
  }

  const canManageRoles = hasPermission(userRoles, 'admin:manage_roles');
  const allRoles = getAllRoles();

  // Contadores dos cards (1 query leve no servidor).
  useEffect(() => {
    fetch('/api/admin/users/stats', { credentials: 'include' })
      .then((r) => (r.ok ? r.json() : null))
      .then((d) => { if (d) setStats({ total: d.total || 0, counts: d.counts || {} }); })
      .catch(() => {});
  }, []);

  // Busca no servidor com debounce (300ms). search vazio = 30 mais recentes.
  useEffect(() => {
    const t = setTimeout(() => fetchUsers(search), 300);
    return () => clearTimeout(t);
  }, [search]);

  const fetchUsers = async (q = '') => {
    setLoadingUsers(true);
    try {
      const res = await fetch(`/api/admin/users?q=${encodeURIComponent(q)}`, { credentials: 'include' });
      const data = await res.json();
      if (res.ok) {
        setUsers(data.users || []);
      } else {
        console.error('API Error:', data.error);
        setMessage({ type: 'error', text: data.error || 'A API retornou um erro ao buscar usuários.' });
      }
    } catch (err) {
      console.error('Erro ao buscar usuários:', err);
      setMessage({ type: 'error', text: 'Sua conexão falhou ou a API colapsou (Erro 500).' });
    } finally {
      setLoadingUsers(false);
    }
  };

  const startEditing = (targetUser) => {
    let roles = targetUser.roles;
    if (typeof roles === 'string') {
      try { roles = JSON.parse(roles); } catch { roles = [roles]; }
    }
    setEditingUser(targetUser);
    setEditRoles([...roles]);
  };

  const toggleRole = (role) => {
    setEditRoles(prev => {
      if (prev.includes(role)) {
        // Não permitir remover a última role
        if (prev.length <= 1) return prev;
        return prev.filter(r => r !== role);
      }
      return [...prev, role];
    });
  };

  const saveRoles = async () => {
    if (!editingUser) return;
    setSaving(true);
    setMessage(null);

    try {
      const res = await fetch(`/api/admin/users/${editingUser.id}/roles`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ roles: editRoles }),
      });

      const data = await res.json();
      if (res.ok) {
        setMessage({ type: 'success', text: `Roles de ${editingUser.name} atualizadas!` });
        setUsers(prev => prev.map(u => u.id === editingUser.id ? { ...u, roles: editRoles } : u));
        setEditingUser(null);
      } else {
        setMessage({ type: 'error', text: data.error || 'Erro ao salvar.' });
      }
    } catch (err) {
      setMessage({ type: 'error', text: 'Erro de conexão.' });
    } finally {
      setSaving(false);
    }
  };

  // A busca agora é feita no servidor; 'users' já vem filtrado e limitado.
  const filtered = users;

  if (isLoading) {
    return (
      <div className="w-full min-h-screen bg-bgDark flex items-center justify-center">
        <div className="w-10 h-10 border-4 border-accent border-t-transparent rounded-full animate-spin"></div>
      </div>
    );
  }

  return (
    <div className="w-full min-h-screen bg-bgDark pt-24 pb-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-5xl mx-auto">
        {/* Header */}
        <motion.div
          initial={{ y: -20, opacity: 0 }}
          animate={{ y: 0, opacity: 1 }}
          className="mb-8"
        >
          <div className="flex items-center gap-3 mb-2">
            <div className="p-2 bg-accent/20 rounded-lg">
              <Shield className="text-accent" size={28} />
            </div>
            <h1 className="text-3xl font-black text-white uppercase tracking-tight">Painel Admin</h1>
          </div>
          <p className="text-gray-400 ml-12">Gerencie os cargos e permissões dos usuários do sistema.</p>
        </motion.div>

        {/* Messages */}
        <AnimatePresence>
          {message && (
            <motion.div
              initial={{ y: -10, opacity: 0 }}
              animate={{ y: 0, opacity: 1 }}
              exit={{ y: -10, opacity: 0 }}
              className={`mb-6 p-4 rounded-xl border ${message.type === 'success' ? 'bg-green-500/10 border-green-500/30 text-green-400' : 'bg-red-500/10 border-red-500/30 text-red-400'}`}
            >
              <div className="flex items-center justify-between">
                <span className="font-medium">{message.text}</span>
                <button onClick={() => setMessage(null)} className="hover:opacity-70"><X size={16} /></button>
              </div>
            </motion.div>
          )}
        </AnimatePresence>

        {/* Stats */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-8">
          {Object.entries(ROLES).map(([key, name]) => {
            const count = stats.counts[name] || 0;
            const colors = ROLE_COLORS[name] || ROLE_COLORS.USER;
            return (
              <motion.div
                key={key}
                initial={{ scale: 0.9, opacity: 0 }}
                animate={{ scale: 1, opacity: 1 }}
                transition={{ delay: Object.keys(ROLES).indexOf(key) * 0.1 }}
                className={`p-4 rounded-xl border ${colors.border} ${colors.bg} backdrop-blur-sm`}
              >
                <div className={`text-2xl font-black ${colors.text}`}>{count}</div>
                <div className="text-sm text-gray-400 font-medium uppercase tracking-wider">{name === 'USER' ? 'Membros' : name + 's'}</div>
              </motion.div>
            );
          })}
        </div>

        {/* Search */}
        <div className="relative mb-6">
          <Search className="absolute left-4 top-1/2 -translate-y-1/2 text-gray-500" size={18} />
          <input
            type="text"
            placeholder="Buscar por nome ou email..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="w-full pl-12 pr-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-accent/50 focus:ring-1 focus:ring-accent/30 transition-all"
          />
        </div>

        {/* Users Table */}
        <div className="bg-white/5 border border-white/10 rounded-2xl overflow-hidden backdrop-blur-sm">
          <div className="grid grid-cols-12 gap-4 p-4 border-b border-white/10 text-xs font-bold text-gray-500 uppercase tracking-widest">
            <div className="col-span-5 sm:col-span-4">Usuário</div>
            <div className="col-span-4 sm:col-span-4">Roles</div>
            <div className="col-span-3 sm:col-span-2 hidden sm:block">Desde</div>
            <div className="col-span-3 sm:col-span-2 text-right">Ações</div>
          </div>

          {loadingUsers ? (
            <div className="p-12 flex justify-center">
              <span className="w-8 h-8 border-4 border-accent border-t-transparent rounded-full animate-spin" />
            </div>
          ) : filtered.length === 0 ? (
            <div className="p-12 text-center text-gray-500">
              <Users size={32} className="mx-auto mb-3 opacity-40" />
              <p>{search ? 'Nenhum usuário encontrado.' : 'Nenhum usuário recente.'}</p>
            </div>
          ) : null}

          {filtered.map((u, i) => {
            let roles = u.roles;
            if (typeof roles === 'string') try { roles = JSON.parse(roles); } catch { roles = []; }
            if (!Array.isArray(roles)) roles = ['USER'];

            return (
              <motion.div
                key={u.id}
                initial={{ x: -10, opacity: 0 }}
                animate={{ x: 0, opacity: 1 }}
                transition={{ delay: i * 0.03 }}
                className="grid grid-cols-12 gap-4 p-4 border-b border-white/5 hover:bg-white/5 transition-colors items-center"
              >
                <div className="col-span-5 sm:col-span-4 flex items-center gap-3">
                  {u.avatar ? (
                    <img src={u.avatar} alt="" className="w-8 h-8 rounded-full object-cover ring-2 ring-white/10" />
                  ) : (
                    <div className="w-8 h-8 rounded-full bg-white/10 flex items-center justify-center text-xs font-bold text-gray-400">
                      {(u.nickname || u.name || 'U')[0].toUpperCase()}
                    </div>
                  )}
                  <div className="truncate">
                    <div className="text-white text-sm font-semibold truncate">{u.nickname || u.name || 'Sem nome'}</div>
                    <div className="text-gray-500 text-xs truncate">{u.email || '—'}</div>
                  </div>
                </div>

                <div className="col-span-4 sm:col-span-4 flex flex-wrap gap-1">
                  {roles.map(role => <RoleBadge key={role} role={role} small />)}
                </div>

                <div className="col-span-3 sm:col-span-2 hidden sm:block text-gray-500 text-xs">
                  {new Date(u.createdAt).toLocaleDateString('pt-BR')}
                </div>

                <div className="col-span-3 sm:col-span-2 text-right">
                  {canManageRoles && (
                    <button
                      onClick={() => startEditing(u)}
                      className="px-3 py-1.5 text-xs font-bold text-accent bg-accent/10 border border-accent/20 rounded-lg hover:bg-accent/20 transition-all uppercase tracking-wider"
                    >
                      Editar
                    </button>
                  )}
                </div>
              </motion.div>
            );
          })}
        </div>

        {/* Edit Modal */}
        <AnimatePresence>
          {editingUser && (
            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="fixed inset-0 bg-black/70 backdrop-blur-sm z-50 flex items-center justify-center p-4"
              onClick={() => setEditingUser(null)}
            >
              <motion.div
                initial={{ scale: 0.9, y: 20 }}
                animate={{ scale: 1, y: 0 }}
                exit={{ scale: 0.9, y: 20 }}
                className="bg-[#1a1a2e] border border-white/10 rounded-2xl p-6 w-full max-w-md shadow-2xl"
                onClick={e => e.stopPropagation()}
              >
                <div className="flex items-center gap-3 mb-6">
                  {editingUser.avatar ? (
                    <img src={editingUser.avatar} alt="" className="w-12 h-12 rounded-full ring-2 ring-accent/30" />
                  ) : (
                    <div className="w-12 h-12 rounded-full bg-accent/20 flex items-center justify-center text-accent font-bold text-lg">
                      {(editingUser.nickname || editingUser.name || 'U')[0].toUpperCase()}
                    </div>
                  )}
                  <div>
                    <h3 className="text-white font-bold text-lg">{editingUser.nickname || editingUser.name || 'Sem nome'}</h3>
                    <p className="text-gray-500 text-sm">{editingUser.email || '—'}</p>
                  </div>
                </div>

                <h4 className="text-gray-400 text-xs font-bold uppercase tracking-widest mb-3">Selecionar Roles</h4>

                <div className="space-y-2 mb-6">
                  {allRoles.map(({ name, level }) => {
                    const isActive = editRoles.includes(name);
                    const colors = ROLE_COLORS[name] || ROLE_COLORS.USER;
                    // Não permitir dar OWNER para outro user (a menos que você seja OWNER)
                    const disabled = name === 'OWNER' && !userRoles.includes('OWNER');

                    return (
                      <button
                        key={name}
                        onClick={() => !disabled && toggleRole(name)}
                        disabled={disabled}
                        className={`w-full flex items-center justify-between p-3 rounded-xl border transition-all ${
                          isActive
                            ? `${colors.bg} ${colors.border} ${colors.text}`
                            : 'bg-white/5 border-white/10 text-gray-500 hover:border-white/20'
                        } ${disabled ? 'opacity-30 cursor-not-allowed' : 'cursor-pointer'}`}
                      >
                        <div className="flex items-center gap-3">
                          {name === 'OWNER' && <Crown size={16} />}
                          {name === 'ADMIN' && <Shield size={16} />}
                          {name === 'MODERATOR' && <Shield size={14} />}
                          {name === 'USER' && <Users size={14} />}
                          <span className="font-bold text-sm uppercase tracking-wider">{name}</span>
                          <span className="text-xs opacity-50">Nível {level}</span>
                        </div>
                        {isActive && <Check size={16} />}
                      </button>
                    );
                  })}
                </div>

                {editRoles.length === 0 && (
                  <div className="flex items-center gap-2 text-amber-400 text-sm mb-4">
                    <AlertTriangle size={14} />
                    <span>Selecione pelo menos uma role.</span>
                  </div>
                )}

                <div className="flex gap-3">
                  <button
                    onClick={() => setEditingUser(null)}
                    className="flex-1 py-2.5 text-sm font-bold text-gray-400 bg-white/5 border border-white/10 rounded-xl hover:bg-white/10 transition-all"
                  >
                    Cancelar
                  </button>
                  <button
                    onClick={saveRoles}
                    disabled={saving || editRoles.length === 0}
                    className="flex-1 py-2.5 text-sm font-black text-black bg-accent rounded-xl hover:bg-accentHover transition-all disabled:opacity-50 disabled:cursor-not-allowed uppercase tracking-wider"
                  >
                    {saving ? 'Salvando...' : 'Salvar'}
                  </button>
                </div>
              </motion.div>
            </motion.div>
          )}
        </AnimatePresence>
      </div>
    </div>
  );
};

export default AdminPanel;
