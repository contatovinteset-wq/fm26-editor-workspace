import React, { useEffect, useState } from 'react';
import { Navigate, Link } from 'react-router-dom';
import { Users, Plus, Save, ExternalLink, ArrowLeft, Crown } from 'lucide-react';
import { useAuth } from '../context/AuthContext';

// Gestão de criadores do Rei da Mesa (Fase 3d) — só OWNER.
// Cria novos criadores (nome, slug, dono por email, logo, link da live) e
// edita o branding/ativação dos existentes.
const ReiDaMesaCriadoresAdmin = () => {
  const { user, isLoading } = useAuth();
  const [creators, setCreators] = useState([]);
  const [form, setForm] = useState({ name: '', slug: '', ownerEmail: '', logo: '', liveUrl: '' });
  const [busy, setBusy] = useState(false);
  const [msg, setMsg] = useState(null);

  const load = () => {
    fetch('/api/reidamesa/creators')
      .then((r) => r.json())
      .then((d) => setCreators(Array.isArray(d) ? d : []))
      .catch(console.error);
  };
  useEffect(() => { load(); }, []);

  if (!isLoading && !user?.roles?.includes('OWNER')) {
    return <Navigate to="/reidamesa" replace />;
  }

  const createCreator = async (e) => {
    e.preventDefault();
    setBusy(true); setMsg(null);
    try {
      const branding = {};
      if (form.logo.trim()) branding.logo = form.logo.trim();
      if (form.liveUrl.trim()) branding.liveUrl = form.liveUrl.trim();
      const res = await fetch('/api/reidamesa/creators', {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: form.name,
          slug: form.slug,
          ownerEmail: form.ownerEmail || undefined,
          branding
        })
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.error || 'Falha ao criar');
      setMsg({ type: 'ok', text: `Criador "${data.name}" criado! Cargo CREATOR concedido ao dono.` });
      setForm({ name: '', slug: '', ownerEmail: '', logo: '', liveUrl: '' });
      load();
    } catch (err) {
      setMsg({ type: 'err', text: err.message });
    } finally {
      setBusy(false);
    }
  };

  const saveBranding = async (c, patch) => {
    setBusy(true); setMsg(null);
    try {
      const res = await fetch(`/api/reidamesa/creator/${c.slug}`, {
        method: 'PATCH',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(patch)
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.error || 'Falha ao salvar');
      setMsg({ type: 'ok', text: `"${c.name}" atualizado.` });
      load();
    } catch (err) {
      setMsg({ type: 'err', text: err.message });
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="w-full min-h-screen bg-bgDark text-white pt-24 pb-16">
      <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8">

        <div className="flex justify-between items-center mb-8 gap-4">
          <h1 className="text-3xl font-black uppercase tracking-tight flex items-center gap-3">
            <Users className="text-accent" size={32} /> Gerenciar Criadores
          </h1>
          <Link to="/reidamesa/criadores" className="px-4 py-2 text-sm font-bold uppercase tracking-widest text-gray-400 hover:text-white border border-white/10 bg-black/50 rounded flex items-center gap-2">
            <ArrowLeft size={16} /> Diretório
          </Link>
        </div>

        {msg && (
          <div className={`mb-6 px-4 py-3 rounded-lg border text-sm font-bold ${msg.type === 'ok' ? 'bg-green-500/10 border-green-500/30 text-green-400' : 'bg-red-500/10 border-red-500/30 text-red-400'}`}>
            {msg.text}
          </div>
        )}

        {/* Form de criação */}
        <form onSubmit={createCreator} className="bg-gray-900 border border-white/10 rounded-2xl p-6 mb-10">
          <h2 className="text-lg font-black uppercase tracking-tight mb-4 flex items-center gap-2"><Plus size={18} className="text-accent" /> Novo criador</h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Field label="Nome de exibição *" value={form.name} onChange={(v) => setForm({ ...form, name: v })} placeholder="Ex: Canal do Fulano" />
            <Field label="Slug (URL) *" value={form.slug} onChange={(v) => setForm({ ...form, slug: v.toLowerCase() })} placeholder="ex: fulano (a-z, 0-9, hífen)" />
            <Field label="Email do dono (opcional)" value={form.ownerEmail} onChange={(v) => setForm({ ...form, ownerEmail: v })} placeholder="deixe vazio = você. Ganha cargo CREATOR." />
            <Field label="Logo/avatar (URL)" value={form.logo} onChange={(v) => setForm({ ...form, logo: v })} placeholder="https://..." />
            <Field label="Link da live (Twitch/YouTube)" value={form.liveUrl} onChange={(v) => setForm({ ...form, liveUrl: v })} placeholder="https://twitch.tv/..." />
          </div>
          <button type="submit" disabled={busy} className="mt-5 px-6 py-2.5 rounded-lg font-bold uppercase text-xs tracking-widest bg-accent text-black hover:brightness-110 transition disabled:opacity-50 flex items-center gap-2">
            <Plus size={16} /> Criar criador
          </button>
        </form>

        {/* Lista de criadores */}
        <h2 className="text-lg font-black uppercase tracking-tight mb-4">Criadores ({creators.length})</h2>
        <div className="space-y-4">
          {creators.map((c) => (
            <CreatorRow key={c.slug} creator={c} busy={busy} onSave={saveBranding} />
          ))}
        </div>
      </div>
    </div>
  );
};

const Field = ({ label, value, onChange, placeholder }) => (
  <label className="block">
    <span className="text-xs font-bold uppercase tracking-widest text-gray-400">{label}</span>
    <input
      value={value}
      onChange={(e) => onChange(e.target.value)}
      placeholder={placeholder}
      className="mt-1 w-full bg-black/50 text-white rounded-lg px-3 py-2 border border-white/10 focus:border-accent focus:outline-none text-sm"
    />
  </label>
);

const CreatorRow = ({ creator, busy, onSave }) => {
  const [logo, setLogo] = useState(creator.branding?.logo || '');
  const [liveUrl, setLiveUrl] = useState(creator.branding?.liveUrl || '');

  return (
    <div className="bg-gray-900 border border-white/10 rounded-xl p-4 flex flex-col sm:flex-row sm:items-end gap-4">
      <div className="flex items-center gap-3 sm:w-48">
        <div className="w-12 h-12 rounded-full bg-black border border-white/20 flex items-center justify-center overflow-hidden">
          {logo ? <img src={logo} alt={creator.name} className="w-full h-full object-cover" onError={(e) => { e.target.style.display = 'none'; }} /> : <Crown className="text-accent" size={20} />}
        </div>
        <div>
          <div className="font-bold text-white">{creator.name}</div>
          <div className="text-xs text-gray-500">/{creator.slug}</div>
        </div>
      </div>

      <div className="flex-1 grid grid-cols-1 sm:grid-cols-2 gap-3">
        <Field label="Logo (URL)" value={logo} onChange={setLogo} placeholder="https://..." />
        <Field label="Link da live" value={liveUrl} onChange={setLiveUrl} placeholder="https://..." />
      </div>

      <button
        onClick={() => onSave(creator, { branding: { ...(logo ? { logo } : {}), ...(liveUrl ? { liveUrl } : {}) } })}
        disabled={busy}
        className="px-4 py-2 rounded-lg font-bold uppercase text-xs tracking-widest bg-white/10 hover:bg-white/20 border border-white/20 transition disabled:opacity-50 flex items-center justify-center gap-2"
      >
        <Save size={14} /> Salvar
      </button>
    </div>
  );
};

export default ReiDaMesaCriadoresAdmin;
