import React, { useEffect, useState } from 'react';
import { Link, Navigate } from 'react-router-dom';
import { Crown, Save, Settings, ExternalLink, CheckCircle, AlertCircle, Tv } from 'lucide-react';
import { useAuth } from '../context/AuthContext';

// Perfil self-service do criador (Fase 3e). Porta de entrada do CREATOR:
// carrega o /my-creator (mesmo inativo), deixa preencher nome + logo + links
// de plataforma e ATIVA quando tiver nome + ≥1 plataforma.
const PLATFORMS = [
  { key: 'twitch', label: 'Twitch', ph: 'https://twitch.tv/seucanal' },
  { key: 'kick', label: 'Kick', ph: 'https://kick.com/seucanal' },
  { key: 'youtube', label: 'YouTube', ph: 'https://youtube.com/@seucanal' },
];

const ReiDaMesaPerfilCriador = () => {
  const { user, isLoading } = useAuth();
  const [creator, setCreator] = useState(null);
  const [notFound, setNotFound] = useState(false);
  const [name, setName] = useState('');
  const [logo, setLogo] = useState('');
  const [links, setLinks] = useState({ twitch: '', kick: '', youtube: '' });
  const [busy, setBusy] = useState(false);
  const [msg, setMsg] = useState(null);

  useEffect(() => {
    fetch('/api/reidamesa/my-creator', { credentials: 'include' })
      .then((r) => (r.ok ? r.json() : Promise.reject(r)))
      .then((c) => {
        setCreator(c);
        setName(c.name || '');
        setLogo(c.branding?.logo || '');
        setLinks({
          twitch: c.branding?.platforms?.twitch || '',
          kick: c.branding?.platforms?.kick || '',
          youtube: c.branding?.platforms?.youtube || '',
        });
      })
      .catch(() => setNotFound(true));
  }, []);

  const canManage = user?.roles?.includes('CREATOR') || user?.roles?.includes('OWNER');
  if (!isLoading && !canManage) return <Navigate to="/reidamesa" replace />;

  const hasPlatform = !!(links.twitch || links.kick || links.youtube);
  const willActivate = !!name.trim() && hasPlatform;

  const save = async () => {
    setBusy(true); setMsg(null);
    try {
      const platforms = {};
      for (const p of PLATFORMS) if (links[p.key].trim()) platforms[p.key] = links[p.key].trim();
      const branding = { ...(logo.trim() ? { logo: logo.trim() } : {}), platforms };
      const res = await fetch(`/api/reidamesa/creator/${creator.slug}`, {
        method: 'PATCH',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name: name.trim(), branding }),
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.error || 'Falha ao salvar');
      setCreator((prev) => ({ ...prev, ...data }));
      setMsg({ type: 'ok', text: data.isActive ? 'Perfil salvo! Teu Rei da Mesa está ATIVO e no diretório. 🎉' : 'Perfil salvo, mas ainda INATIVO — falta nome + pelo menos 1 plataforma.' });
    } catch (err) {
      setMsg({ type: 'err', text: err.message });
    } finally {
      setBusy(false);
    }
  };

  if (notFound) {
    return (
      <div className="w-full min-h-screen bg-bgDark text-white pt-24 pb-16">
        <div className="max-w-xl mx-auto px-4 text-center">
          <Crown className="mx-auto text-gray-600 mb-4" size={40} />
          <h1 className="text-2xl font-black uppercase">Você ainda não tem um Rei da Mesa</h1>
          <p className="text-gray-400 mt-2">Fale com a administração do vinteset para receber o cargo de Criador.</p>
        </div>
      </div>
    );
  }
  if (!creator) {
    return <div className="w-full min-h-screen bg-bgDark flex items-center justify-center"><span className="w-10 h-10 border-4 border-accent border-t-transparent rounded-full animate-spin" /></div>;
  }

  return (
    <div className="w-full min-h-screen bg-bgDark text-white pt-24 pb-16">
      <div className="max-w-2xl mx-auto px-4 sm:px-6 lg:px-8">
        <h1 className="text-3xl font-black uppercase tracking-tight flex items-center gap-3 mb-2">
          <Crown className="text-accent" size={30} /> Meu Canal
        </h1>
        <p className="text-gray-400 mb-6">Configure o seu Rei da Mesa. Ele fica visível no diretório quando tiver <b>nome + pelo menos 1 plataforma</b>.</p>

        {/* Status */}
        <div className={`mb-6 px-4 py-3 rounded-lg border text-sm font-bold flex items-center gap-2 ${creator.isActive ? 'bg-green-500/10 border-green-500/30 text-green-400' : 'bg-yellow-500/10 border-yellow-500/30 text-yellow-400'}`}>
          {creator.isActive ? <CheckCircle size={16} /> : <AlertCircle size={16} />}
          {creator.isActive ? 'ATIVO — aparece no diretório' : 'INATIVO — preencha e salve para ativar'}
        </div>

        {msg && (
          <div className={`mb-6 px-4 py-3 rounded-lg border text-sm font-bold ${msg.type === 'ok' ? 'bg-green-500/10 border-green-500/30 text-green-400' : 'bg-red-500/10 border-red-500/30 text-red-400'}`}>{msg.text}</div>
        )}

        <div className="bg-gray-900 border border-white/10 rounded-2xl p-6 space-y-4">
          <Field label="Nome de exibição *" value={name} onChange={setName} placeholder="Como teu Rei da Mesa aparece" />
          <Field label="Logo/avatar (URL)" value={logo} onChange={setLogo} placeholder="https://... (imagem .png/.jpg)" />
          <div>
            <span className="text-xs font-bold uppercase tracking-widest text-gray-400">Plataformas (cole o link de onde você faz live)</span>
            <div className="mt-2 space-y-3">
              {PLATFORMS.map((p) => (
                <div key={p.key} className="flex items-center gap-3">
                  <span className="w-16 text-xs font-bold uppercase text-gray-400">{p.label}</span>
                  <input
                    value={links[p.key]}
                    onChange={(e) => setLinks({ ...links, [p.key]: e.target.value })}
                    placeholder={p.ph}
                    className="flex-1 bg-black/50 text-white rounded-lg px-3 py-2 border border-white/10 focus:border-accent focus:outline-none text-sm"
                  />
                </div>
              ))}
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-3 pt-2">
            <button onClick={save} disabled={busy || !name.trim()} className="px-6 py-2.5 rounded-lg font-bold uppercase text-xs tracking-widest bg-accent text-black hover:brightness-110 transition disabled:opacity-50 flex items-center gap-2">
              <Save size={16} /> Salvar {willActivate ? '& Ativar' : ''}
            </button>
            <Link to={`/reidamesa/c/${creator.slug}/admin`} className="px-4 py-2.5 rounded-lg font-bold uppercase text-xs tracking-widest bg-white/10 hover:bg-white/20 border border-white/20 transition flex items-center gap-2">
              <Settings size={15} /> Painel do Rei da Mesa
            </Link>
            {creator.isActive && (
              <Link to={`/reidamesa/c/${creator.slug}`} className="px-4 py-2.5 rounded-lg font-bold uppercase text-xs tracking-widest text-gray-300 hover:text-white border border-white/10 transition flex items-center gap-2">
                <ExternalLink size={15} /> Ver página pública
              </Link>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

const Field = ({ label, value, onChange, placeholder }) => (
  <label className="block">
    <span className="text-xs font-bold uppercase tracking-widest text-gray-400">{label}</span>
    <input value={value} onChange={(e) => onChange(e.target.value)} placeholder={placeholder} className="mt-1 w-full bg-black/50 text-white rounded-lg px-3 py-2 border border-white/10 focus:border-accent focus:outline-none text-sm" />
  </label>
);

export default ReiDaMesaPerfilCriador;
