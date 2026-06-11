import React, { useEffect, useState } from 'react';
import { Navigate, Link } from 'react-router-dom';
import { Users, Save, ArrowLeft, Crown, Trash2, Power, Info } from 'lucide-react';
import { useAuth } from '../context/AuthContext';

// Gestão de criadores (Fase 3e) — só OWNER. A CRIAÇÃO agora é pelo cargo:
// dê "Criador" a um usuário no Painel ADMIN e o Rei da Mesa dele nasce inativo.
// Aqui o OWNER vê todos, edita branding, desativa/reativa e exclui os vazios.
const PLATFORMS = [
  { key: 'twitch', label: 'Twitch' },
  { key: 'kick', label: 'Kick' },
  { key: 'youtube', label: 'YouTube' },
];

const ReiDaMesaCriadoresAdmin = () => {
  const { user, isLoading } = useAuth();
  const [creators, setCreators] = useState([]);
  const [busy, setBusy] = useState(false);
  const [msg, setMsg] = useState(null);

  const load = () => {
    fetch('/api/reidamesa/creators/all', { credentials: 'include' })
      .then((r) => (r.ok ? r.json() : Promise.reject(r)))
      .then((d) => setCreators(Array.isArray(d) ? d : []))
      .catch(console.error);
  };
  useEffect(() => { load(); }, []);

  if (!isLoading && !user?.roles?.includes('OWNER')) return <Navigate to="/reidamesa" replace />;

  const patch = async (c, body, okText) => {
    setBusy(true); setMsg(null);
    try {
      const res = await fetch(`/api/reidamesa/creator/${c.slug}`, {
        method: 'PATCH', credentials: 'include',
        headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body),
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.error || 'Falha');
      setMsg({ type: 'ok', text: okText || `"${c.name}" atualizado.` });
      load();
    } catch (err) { setMsg({ type: 'err', text: err.message }); }
    finally { setBusy(false); }
  };

  const remove = async (c) => {
    if (!window.confirm(`Excluir o Rei da Mesa "${c.name}"? Só funciona se ele não tiver dados de jogo.`)) return;
    setBusy(true); setMsg(null);
    try {
      const res = await fetch(`/api/reidamesa/creator/${c.slug}`, { method: 'DELETE', credentials: 'include' });
      const data = await res.json();
      if (!res.ok) throw new Error(data.error || 'Falha ao excluir');
      setMsg({ type: 'ok', text: `"${c.name}" excluído.` });
      load();
    } catch (err) { setMsg({ type: 'err', text: err.message }); }
    finally { setBusy(false); }
  };

  return (
    <div className="w-full min-h-screen bg-bgDark text-white pt-24 pb-16">
      <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between items-center mb-6 gap-4">
          <h1 className="text-3xl font-black uppercase tracking-tight flex items-center gap-3">
            <Users className="text-accent" size={32} /> Gerenciar Criadores
          </h1>
          <Link to="/reidamesa/criadores" className="px-4 py-2 text-sm font-bold uppercase tracking-widest text-gray-400 hover:text-white border border-white/10 bg-black/50 rounded flex items-center gap-2">
            <ArrowLeft size={16} /> Diretório
          </Link>
        </div>

        <div className="mb-6 px-4 py-3 rounded-lg border border-blue-500/30 bg-blue-500/10 text-blue-300 text-sm flex items-start gap-2">
          <Info size={16} className="mt-0.5 shrink-0" />
          <span>Para <b>adicionar</b> um criador, dê o cargo <b>Criador</b> a um usuário no <Link to="/admin" className="underline font-bold">Painel ADMIN</Link>. O Rei da Mesa dele nasce inativo e ativa quando ele preencher o perfil em <b>Meu Canal</b>.</span>
        </div>

        {msg && (
          <div className={`mb-6 px-4 py-3 rounded-lg border text-sm font-bold ${msg.type === 'ok' ? 'bg-green-500/10 border-green-500/30 text-green-400' : 'bg-red-500/10 border-red-500/30 text-red-400'}`}>{msg.text}</div>
        )}

        <h2 className="text-lg font-black uppercase tracking-tight mb-4">Criadores ({creators.length})</h2>
        <div className="space-y-4">
          {creators.map((c) => (
            <CreatorRow key={c.slug} creator={c} busy={busy} onSave={patch} onRemove={remove} />
          ))}
        </div>
      </div>
    </div>
  );
};

const Field = ({ label, value, onChange, placeholder }) => (
  <label className="block">
    <span className="text-[10px] font-bold uppercase tracking-widest text-gray-500">{label}</span>
    <input value={value} onChange={(e) => onChange(e.target.value)} placeholder={placeholder} className="mt-1 w-full bg-black/50 text-white rounded-lg px-3 py-2 border border-white/10 focus:border-accent focus:outline-none text-sm" />
  </label>
);

const CreatorRow = ({ creator, busy, onSave, onRemove }) => {
  const [logo, setLogo] = useState(creator.branding?.logo || '');
  const [links, setLinks] = useState({
    twitch: creator.branding?.platforms?.twitch || '',
    kick: creator.branding?.platforms?.kick || '',
    youtube: creator.branding?.platforms?.youtube || '',
  });

  const saveBranding = () => {
    const platforms = {};
    for (const p of PLATFORMS) if (links[p.key].trim()) platforms[p.key] = links[p.key].trim();
    onSave(creator, { branding: { ...(logo.trim() ? { logo: logo.trim() } : {}), platforms } });
  };

  return (
    <div className={`bg-gray-900 border rounded-xl p-4 ${creator.isActive ? 'border-white/10' : 'border-yellow-500/20'}`}>
      <div className="flex items-center justify-between gap-3 mb-3">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-full bg-black border border-white/20 flex items-center justify-center overflow-hidden">
            {logo ? <img src={logo} alt="" className="w-full h-full object-cover" onError={(e) => { e.target.style.display = 'none'; }} /> : <Crown className="text-accent" size={18} />}
          </div>
          <div>
            <div className="font-bold text-white flex items-center gap-2">
              {creator.name}
              <span className={`text-[9px] px-1.5 py-0.5 rounded font-black uppercase tracking-widest ${creator.isActive ? 'bg-green-500/15 text-green-400 border border-green-500/30' : 'bg-yellow-500/15 text-yellow-400 border border-yellow-500/30'}`}>
                {creator.isActive ? 'Ativo' : 'Inativo'}
              </span>
            </div>
            <div className="text-xs text-gray-500">/{creator.slug} · dono: {creator.ownerName}</div>
          </div>
        </div>
        <div className="flex items-center gap-2">
          {creator.isActive && creator.slug !== 'vinteset' && (
            <button onClick={() => onSave(creator, { isActive: false }, `"${creator.name}" desativado.`)} disabled={busy} title="Desativar" className="px-3 py-2 rounded-lg text-xs font-bold uppercase bg-white/5 hover:bg-white/10 border border-white/10 text-gray-300 disabled:opacity-50 flex items-center gap-1">
              <Power size={13} /> Desativar
            </button>
          )}
          {creator.slug !== 'vinteset' && (
            <button onClick={() => onRemove(creator)} disabled={busy} title="Excluir (só vazios)" className="px-3 py-2 rounded-lg text-xs font-bold uppercase bg-red-500/10 hover:bg-red-500/20 border border-red-500/30 text-red-400 disabled:opacity-50 flex items-center gap-1">
              <Trash2 size={13} /> Excluir
            </button>
          )}
        </div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <Field label="Logo (URL)" value={logo} onChange={setLogo} placeholder="https://..." />
        {PLATFORMS.map((p) => (
          <Field key={p.key} label={p.label} value={links[p.key]} onChange={(v) => setLinks({ ...links, [p.key]: v })} placeholder={`Link do ${p.label}`} />
        ))}
      </div>
      <div className="mt-3">
        <button onClick={saveBranding} disabled={busy} className="px-4 py-2 rounded-lg font-bold uppercase text-xs tracking-widest bg-white/10 hover:bg-white/20 border border-white/20 transition disabled:opacity-50 flex items-center gap-2">
          <Save size={14} /> Salvar branding
        </button>
      </div>
    </div>
  );
};

export default ReiDaMesaCriadoresAdmin;
