import React, { useEffect, useState } from 'react';
import { Link, Navigate } from 'react-router-dom';
import { Crown, Save, Settings, ExternalLink, CheckCircle, AlertCircle, Share2, Copy, Check, MessageCircle, Send, UploadCloud, Unlock, Lock, MonitorPlay, BarChart3, Trophy, GraduationCap } from 'lucide-react';
import { useAuth } from '../context/AuthContext';

// 🎓 Banho de loja: passo a passo de como o criador opera o Rei da Mesa.
const TUTORIAL_STEPS = [
  { icon: UploadCloud, title: 'Suba seu elenco', text: 'No Painel, aba Plantel, suba o arquivo exportado do FM26 (plugin de export). Isso cria os jogadores que a galera vai poder escalar.' },
  { icon: Unlock, title: 'Abra o mercado', text: 'Clique em "Abrir Mercado" no Painel. Isso inicia uma nova rodada e libera os viewers a montarem o time (3 titulares + bagre + capitão).' },
  { icon: Share2, title: 'Divulgue o link', text: 'Cole o link do seu Rei da Mesa no chat da sua live (botões de compartilhar aqui em cima). Quanto mais gente escalar, mais animada a disputa.' },
  { icon: MonitorPlay, title: 'Coloque o overlay no OBS', text: 'Copie o link do overlay no Painel e adicione como Fonte de Navegador no OBS. As escalações da galera aparecem ao vivo na tela.' },
  { icon: Lock, title: 'Feche o mercado ao começar a partida', text: 'Assim ninguém escala depois que a bola rola. Nesse momento abre a votação do "Craque do Jogo" pra galera.' },
  { icon: BarChart3, title: 'Suba o resultado', text: 'No fim do jogo, exporte as estatísticas no FM26 e suba no Painel. Confira o preview e processe — o sistema calcula a pontuação de cada jogador automaticamente.' },
  { icon: Trophy, title: 'Acompanhe o ranking', text: 'Ranking Geral (soma das rodadas) + Rei da Mesa da rodada (maior pontuador da live). Repita a cada partida e veja a comunidade subir no ranking!' },
];

const CreatorTutorial = () => (
  <div className="mt-6 bg-gray-900 border border-white/10 rounded-2xl p-6">
    <div className="flex items-center gap-2 mb-1">
      <GraduationCap size={20} className="text-accent" />
      <h3 className="font-black uppercase tracking-tight text-white">Como usar seu Rei da Mesa</h3>
    </div>
    <p className="text-gray-400 text-sm mb-6">O ciclo de cada partida, do começo ao fim. Em pouco tempo vira rotina. 👇</p>

    <ol className="space-y-4">
      {TUTORIAL_STEPS.map((s, i) => {
        const Icon = s.icon;
        return (
          <li key={i} className="flex gap-4">
            <div className="flex flex-col items-center">
              <div className="w-10 h-10 rounded-full bg-accent/15 border border-accent/30 flex items-center justify-center text-accent font-black">{i + 1}</div>
              {i < TUTORIAL_STEPS.length - 1 && <div className="w-px flex-1 bg-white/10 my-1" />}
            </div>
            <div className="pb-2">
              <div className="flex items-center gap-2 text-white font-bold">
                <Icon size={16} className="text-accent" /> {s.title}
              </div>
              <p className="text-gray-400 text-sm mt-1 leading-relaxed">{s.text}</p>
            </div>
          </li>
        );
      })}
    </ol>
  </div>
);

// 𝕏 não existe no lucide — glifo inline.
const XGlyph = ({ size = 16 }) => (
  <span style={{ fontSize: size, fontWeight: 900, lineHeight: 1 }}>𝕏</span>
);

// Caixa de compartilhamento do link do Rei da Mesa do criador.
const ShareBox = ({ url, name }) => {
  const [copied, setCopied] = useState(false);
  const enc = encodeURIComponent;
  const text = `🏆 Entra no Rei da Mesa do ${name}! Escale seu time e dispute o ranking:`;

  const copy = async () => {
    try { await navigator.clipboard.writeText(url); } catch { /* noop */ }
    setCopied(true);
    setTimeout(() => setCopied(false), 2500);
  };

  const nativeShare = async () => {
    try { await navigator.share({ title: `Rei da Mesa do ${name}`, text, url }); } catch { /* cancelado */ }
  };

  const buttons = [
    { label: 'WhatsApp', color: '#25D366', href: `https://wa.me/?text=${enc(`${text} ${url}`)}`, icon: <MessageCircle size={18} /> },
    { label: 'Telegram', color: '#229ED9', href: `https://t.me/share/url?url=${enc(url)}&text=${enc(text)}`, icon: <Send size={18} /> },
    { label: 'X', color: '#1d9bf0', href: `https://twitter.com/intent/tweet?text=${enc(text)}&url=${enc(url)}`, icon: <XGlyph size={15} /> },
  ];

  return (
    <div className="bg-gradient-to-br from-accent/10 to-transparent border border-accent/30 rounded-2xl p-5 mb-6">
      <div className="flex items-center gap-2 mb-3">
        <Share2 size={18} className="text-accent" />
        <h3 className="font-black uppercase tracking-tight text-white">Divulgue na sua live</h3>
      </div>

      {/* Link + copiar */}
      <div className="flex items-stretch gap-2 mb-4">
        <div className="flex-1 bg-black/50 border border-white/10 rounded-lg px-3 py-2.5 text-sm text-gray-300 truncate flex items-center">
          {url}
        </div>
        <button
          onClick={copy}
          className={`px-4 rounded-lg font-bold uppercase text-xs tracking-widest flex items-center gap-2 transition-all ${copied ? 'bg-green-500 text-black' : 'bg-accent text-black hover:brightness-110'}`}
        >
          {copied ? <><Check size={15} /> Copiado</> : <><Copy size={15} /> Copiar</>}
        </button>
      </div>

      {/* Botões de compartilhamento */}
      <div className="flex flex-wrap gap-2">
        {buttons.map((b) => (
          <a
            key={b.label}
            href={b.href}
            target="_blank"
            rel="noopener noreferrer"
            className="flex items-center gap-2 px-4 py-2.5 rounded-lg font-bold uppercase text-xs tracking-widest text-white transition-transform hover:scale-105"
            style={{ backgroundColor: b.color }}
          >
            {b.icon} {b.label}
          </a>
        ))}
        {typeof navigator !== 'undefined' && navigator.share && (
          <button
            onClick={nativeShare}
            className="flex items-center gap-2 px-4 py-2.5 rounded-lg font-bold uppercase text-xs tracking-widest bg-white/10 border border-white/20 text-white hover:bg-white/20 transition"
          >
            <Share2 size={16} /> Mais
          </button>
        )}
      </div>
    </div>
  );
};

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

        {/* Compartilhar: só faz sentido quando o canal está ativo (link público funciona). */}
        {creator.isActive ? (
          <ShareBox url={`${window.location.origin}/reidamesa/c/${creator.slug}`} name={creator.name} />
        ) : (
          <div className="mb-6 px-4 py-3 rounded-lg border border-white/10 bg-white/5 text-gray-400 text-sm flex items-center gap-2">
            <Share2 size={16} className="text-gray-500" /> Ative seu canal (salve com nome + 1 plataforma) para liberar o link de compartilhamento.
          </div>
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

        {creator.isActive && <CreatorTutorial />}
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
