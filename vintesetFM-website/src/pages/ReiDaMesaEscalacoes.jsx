import React, { useEffect, useMemo, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { ClipboardList, ArrowLeft, Crown, ImagePlus, Trash2, Flame, ThumbsDown, ClipboardPaste } from 'lucide-react';
import { rdmFetch, useRdmBase } from '../services/reidamesa';
import { useAuth } from '../context/AuthContext';

const Face = ({ uniqueId, name, size = 28 }) => {
  const [err, setErr] = useState(false);
  if (!uniqueId || err) {
    return <div className="rounded-full bg-black/60 border border-white/15 flex items-center justify-center font-bold text-gray-400 text-[10px]" style={{ width: size, height: size }}>{(name || '?').charAt(0).toUpperCase()}</div>;
  }
  return <img src={`https://sortitoutsi.b-cdn.net/uploads/face/face_${uniqueId}.png`} alt={name} onError={() => setErr(true)} className="rounded-full object-cover border border-white/15 bg-black/60" style={{ width: size, height: size }} />;
};

const PlayerCell = ({ player, isCaptain, danger }) => {
  if (!player) return <span className="text-gray-600 text-xs">—</span>;
  return (
    <div className="flex items-center gap-2 min-w-0">
      <Face uniqueId={player.uniqueId} name={player.name} />
      <span className={`text-sm truncate ${danger ? 'text-red-400' : 'text-white'}`}>{player.name}</span>
      {isCaptain && <Crown size={13} className="text-accent shrink-0" title="Capitão" />}
    </div>
  );
};

const AggCard = ({ icon: Icon, label, color, agg, suffix }) => (
  <div className="bg-gray-900 border border-white/10 rounded-2xl p-4 flex items-center gap-3">
    <div className="shrink-0 w-10 h-10 rounded-xl flex items-center justify-center" style={{ backgroundColor: `${color}1f`, color }}>
      <Icon size={20} />
    </div>
    {agg?.player ? (
      <div className="flex items-center gap-2 min-w-0">
        <Face uniqueId={agg.player.uniqueId} name={agg.player.name} size={36} />
        <div className="min-w-0">
          <div className="text-[10px] font-bold uppercase tracking-widest text-gray-500">{label}</div>
          <div className="font-bold text-white truncate">{agg.player.name}</div>
          <div className="text-xs font-mono" style={{ color }}>{agg.count}x {suffix}</div>
        </div>
      </div>
    ) : (
      <div>
        <div className="text-[10px] font-bold uppercase tracking-widest text-gray-500">{label}</div>
        <div className="text-gray-600 text-sm">Sem dados</div>
      </div>
    )}
  </div>
);

// Comprime a imagem no navegador (downscale + JPEG) antes de mandar como base64.
const compressImage = (file) => new Promise((resolve, reject) => {
  const reader = new FileReader();
  reader.onerror = reject;
  reader.onload = () => {
    const img = new Image();
    img.onerror = reject;
    img.onload = () => {
      const maxW = 1200;
      const scale = Math.min(1, maxW / img.width);
      const canvas = document.createElement('canvas');
      canvas.width = Math.round(img.width * scale);
      canvas.height = Math.round(img.height * scale);
      canvas.getContext('2d').drawImage(img, 0, 0, canvas.width, canvas.height);
      resolve(canvas.toDataURL('image/jpeg', 0.75));
    };
    img.src = reader.result;
  };
  reader.readAsDataURL(file);
});

const ReiDaMesaEscalacoes = () => {
  const base = useRdmBase();
  const { user } = useAuth();
  const fileRef = useRef(null);
  const [rows, setRows] = useState([]);
  const [print, setPrint] = useState(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);

  const roles = user?.roles || [];
  const canManage = roles.includes('OWNER') || roles.includes('ADMIN') || roles.includes('ADMIN_GERACAO') || roles.includes('CREATOR');

  const load = () => {
    Promise.all([
      rdmFetch('/api/reidamesa/escalacoes').then((r) => r.json()).catch(() => []),
      rdmFetch('/api/reidamesa/lineup-print').then((r) => r.json()).catch(() => ({ image: null })),
    ]).then(([escalacoes, lp]) => {
      setRows(Array.isArray(escalacoes) ? escalacoes : []);
      setPrint(lp?.image || null);
      setLoading(false);
    });
  };
  useEffect(() => { load(); }, []);

  const uploadImage = async (file) => {
    if (!file) return;
    setBusy(true);
    try {
      const image = await compressImage(file);
      const res = await rdmFetch('/api/reidamesa/lineup-print', {
        method: 'POST', credentials: 'include',
        headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ image }),
      });
      if (!res.ok) { const d = await res.json().catch(() => ({})); throw new Error(d.error || 'Falha ao subir'); }
      setPrint(image);
    } catch (err) { alert(err.message); }
    finally { setBusy(false); if (fileRef.current) fileRef.current.value = ''; }
  };
  const onPickFile = (e) => uploadImage(e.target.files?.[0]);

  // Colar com Ctrl+V: pega a imagem do clipboard e sobe direto (sem salvar arquivo).
  useEffect(() => {
    if (!canManage) return;
    const onPaste = (e) => {
      const items = e.clipboardData?.items || [];
      for (const it of items) {
        if (it.type && it.type.startsWith('image/')) {
          const file = it.getAsFile();
          if (file) { e.preventDefault(); uploadImage(file); break; }
        }
      }
    };
    window.addEventListener('paste', onPaste);
    return () => window.removeEventListener('paste', onPaste);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [canManage]);

  const removePrint = async () => {
    setBusy(true);
    try {
      await rdmFetch('/api/reidamesa/lineup-print', {
        method: 'POST', credentials: 'include',
        headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ image: null }),
      });
      setPrint(null);
    } finally { setBusy(false); }
  };

  // Agregados desta rodada, direto da tabela já carregada (sem chamada extra).
  const aggregates = useMemo(() => {
    const top = (counts, players) => {
      let id = null;
      for (const k of Object.keys(counts)) if (id === null || counts[k] > counts[id]) id = k;
      return id ? { player: players[id], count: counts[id] } : null;
    };
    const tit = {}, titP = {}, bag = {}, bagP = {}, cap = {}, capP = {};
    for (const r of rows) {
      for (const p of [r.def, r.mei, r.ata]) if (p?.id) { tit[p.id] = (tit[p.id] || 0) + 1; titP[p.id] = p; }
      if (r.bagre?.id) { bag[r.bagre.id] = (bag[r.bagre.id] || 0) + 1; bagP[r.bagre.id] = r.bagre; }
      if (r.capitaoId) {
        const cp = [r.def, r.mei, r.ata].find((p) => p?.id === r.capitaoId);
        if (cp) { cap[r.capitaoId] = (cap[r.capitaoId] || 0) + 1; capP[r.capitaoId] = cp; }
      }
    }
    return { maisEscalado: top(tit, titP), bagre: top(bag, bagP), capitao: top(cap, capP) };
  }, [rows]);

  return (
    <div className="w-full min-h-screen bg-bgDark text-white pt-24 pb-16">
      <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex flex-col md:flex-row justify-between items-start md:items-center mb-8 gap-4">
          <div>
            <h1 className="text-3xl font-black uppercase tracking-tight flex items-center gap-3">
              <ClipboardList className="text-accent" size={32} /> Escalações da Rodada
            </h1>
            <p className="text-gray-400 mt-2">Quem escalou o quê nesta rodada + o time do criador.</p>
          </div>
          <Link to={base} className="px-6 py-2 text-sm font-bold uppercase tracking-widest text-gray-400 hover:text-white border border-white/10 bg-black/50 rounded flex items-center gap-2">
            <ArrowLeft size={16} /> Voltar
          </Link>
        </div>

        {/* Print do criador */}
        <div className="bg-gray-900 border border-white/10 rounded-2xl p-4 mb-6">
          <div className="flex items-center justify-between mb-3">
            <h2 className="font-black uppercase tracking-tight text-white text-sm">Escalação do criador (print)</h2>
            {canManage && (
              <div className="flex gap-2">
                <input ref={fileRef} type="file" accept="image/*" onChange={onPickFile} className="hidden" />
                <button onClick={() => fileRef.current?.click()} disabled={busy} className="px-3 py-1.5 rounded-lg text-xs font-bold uppercase tracking-widest bg-accent text-black hover:brightness-110 disabled:opacity-50 flex items-center gap-1.5">
                  <ImagePlus size={14} /> {print ? 'Trocar' : 'Subir arquivo'}
                </button>
                {print && (
                  <button onClick={removePrint} disabled={busy} className="px-3 py-1.5 rounded-lg text-xs font-bold uppercase tracking-widest bg-red-500/10 border border-red-500/30 text-red-400 hover:bg-red-500/20 disabled:opacity-50 flex items-center gap-1.5">
                    <Trash2 size={14} /> Remover
                  </button>
                )}
              </div>
            )}
          </div>
          {print ? (
            <img src={print} alt="Escalação do criador" className="w-full rounded-lg border border-white/10" />
          ) : canManage ? (
            <button
              onClick={() => fileRef.current?.click()}
              disabled={busy}
              className="w-full text-center py-10 text-gray-400 text-sm border border-dashed border-accent/30 rounded-lg hover:bg-accent/5 transition-colors flex flex-col items-center gap-2"
            >
              <ClipboardPaste size={26} className="text-accent" />
              {busy ? 'Enviando…' : <><b className="text-white">Cole o print com Ctrl+V</b><span>ou clique para escolher um arquivo</span></>}
            </button>
          ) : (
            <div className="text-center py-10 text-gray-600 text-sm border border-dashed border-white/10 rounded-lg">
              O criador ainda não subiu o print da escalação.
            </div>
          )}
        </div>

        {/* Agregados da rodada */}
        {rows.length > 0 && (
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-6">
            <AggCard icon={Flame} label="Mais escalado" color="#f59e0b" agg={aggregates.maisEscalado} suffix="escalado" />
            <AggCard icon={Crown} label="Capitão favorito" color="#a855f7" agg={aggregates.capitao} suffix="capitão" />
            <AggCard icon={ThumbsDown} label="Bagre mais escalado" color="#ef4444" agg={aggregates.bagre} suffix="apostado" />
          </div>
        )}

        {/* Tabela de escalações */}
        <div className="bg-gray-900 border border-white/10 rounded-2xl overflow-hidden">
          <div className="px-4 py-3 border-b border-white/10 flex items-center justify-between">
            <h2 className="font-black uppercase tracking-tight text-white text-sm">Times da galera ({rows.length})</h2>
          </div>
          {loading ? (
            <div className="p-12 flex justify-center"><span className="w-8 h-8 border-4 border-accent border-t-transparent rounded-full animate-spin" /></div>
          ) : rows.length === 0 ? (
            <div className="p-12 text-center text-gray-500">Ninguém escalou ainda nesta rodada.</div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left whitespace-nowrap">
                <thead className="bg-black/40 text-gray-500 text-[11px] font-bold uppercase tracking-widest border-b border-white/5">
                  <tr>
                    <th className="px-4 py-3">Manager</th>
                    <th className="px-4 py-3">Defensor</th>
                    <th className="px-4 py-3">Meio</th>
                    <th className="px-4 py-3">Ataque</th>
                    <th className="px-4 py-3">Bagre</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-white/5">
                  {rows.map((r, i) => (
                    <tr key={i} className="hover:bg-white/5">
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-2">
                          {r.avatar ? <img src={r.avatar} alt="" className="w-7 h-7 rounded-full object-cover border border-white/10" /> : <div className="w-7 h-7 rounded-full bg-white/10 flex items-center justify-center text-[10px] font-bold text-gray-400">{r.manager.charAt(0).toUpperCase()}</div>}
                          <span className="font-bold text-white text-sm">{r.manager}</span>
                        </div>
                      </td>
                      <td className="px-4 py-3"><PlayerCell player={r.def} isCaptain={r.capitaoId && r.def?.id === r.capitaoId} /></td>
                      <td className="px-4 py-3"><PlayerCell player={r.mei} isCaptain={r.capitaoId && r.mei?.id === r.capitaoId} /></td>
                      <td className="px-4 py-3"><PlayerCell player={r.ata} isCaptain={r.capitaoId && r.ata?.id === r.capitaoId} /></td>
                      <td className="px-4 py-3"><PlayerCell player={r.bagre} danger /></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default ReiDaMesaEscalacoes;
