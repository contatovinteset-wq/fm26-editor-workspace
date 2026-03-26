import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { User, Settings, FileText, ArrowRight, Save, LogOut, Crown, ShieldCheck, Twitch } from 'lucide-react';
import { Link, useSearchParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { can } from '../utils/PermissionService';

const DEFAULT_AVATARS = [
  { id: 'manager_classic', path: '/avatars/manager_classic.png?v=3', label: 'Classic' },
  { id: 'manager_modern', path: '/avatars/manager_modern.png?v=3', label: 'Modern' },
  { id: 'tactical_board', path: '/avatars/tactical_board.png?v=3', label: 'Tactical' },
  { id: 'coach_modern', path: '/avatars/coach_modern.png?v=3', label: 'Coach' },
];

const MinhaConta = () => {
  const { user, fetchUser, logout } = useAuth();
  const [searchParams] = useSearchParams();
  const isOnboarding = searchParams.get('onboarding') === 'true';
  
  const [activeTab, setActiveTab] = useState('DADOS');
  const [nickname, setNickname] = useState(user?.nickname || '');
  const [selectedAvatar, setSelectedAvatar] = useState(user?.avatar || '/avatars/manager_classic.png');
  const [isSaved, setIsSaved] = useState(false);
  const [error, setError] = useState('');

  if (!user) return null;

  const isOwner = user.roles?.includes('OWNER');
  const isAdmin = user.roles?.includes('ADMIN');
  const isTwitchUser = !!user.twitchId;

  const hasNicknamePermission = can(user, 'change_nickname');
  
  // Nickname irreversível após definido, a menos que seja ADMIN+
  const isNicknameLocked = user.nickname_defined && !hasNicknamePermission;
  
  // Conta configurada: possui algum método de autenticação persistido
  const isAccountConfigured = !!(user.email || user.googleId || user.twitchId);

  const handleSave = async (e) => {
    e.preventDefault();
    setError('');
    
    if (isNicknameLocked && nickname !== user.nickname) {
      setError(`Segurança: Seu nickname já está definido definitivamente.`);
      return;
    }

    try {
      const res = await fetch('/api/users/profile', {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ nickname, avatar: selectedAvatar })
      });

      const data = await res.json();
      if (!res.ok) throw new Error(data.error || 'Erro ao salvar perfil');

      setIsSaved(true);
      await fetchUser(); 
      setTimeout(() => setIsSaved(false), 3000);
    } catch (err) {
      setError(err.message);
    }
  };

  return (
    <div className="w-full min-h-screen bg-bgDark text-white pt-24 pb-16">
      <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
        
        <div className="flex flex-col md:flex-row gap-8">
          
          {/* Sidebar Left */}
          <div className="w-full md:w-64 flex-shrink-0 space-y-6">
             <div className="bg-gray-900 border border-white/10 rounded-2xl p-6 text-center">
                <div className="w-24 h-24 rounded-full bg-accent/20 border-2 border-accent/50 mx-auto flex items-center justify-center mb-4 relative overflow-hidden group">
                  <img src={user.avatar || '/avatars/manager_classic.png'} alt={user.nickname || 'Manager'} className="w-full h-full object-cover" />
                </div>
                <h2 className="font-bold text-lg flex flex-col items-center gap-1 group">
                  <span className="truncate max-w-full text-white tracking-tight">{user.nickname || 'Novo Manager'}</span>
                  {isOwner && <Crown size={18} className="text-accent drop-shadow-[0_0_8px_rgba(255,215,0,0.5)]" />}
                </h2>
                
                {isOwner && (
                  <span className="inline-block mt-2 px-3 py-1 bg-accent/10 border border-accent/20 text-accent text-[10px] font-black uppercase tracking-widest rounded-full">
                    Selo de Owner
                  </span>
                )}
                {isAdmin && !isOwner && (
                  <span className="inline-block mt-2 px-3 py-1 bg-blue-500/10 border border-blue-500/20 text-blue-400 text-[10px] font-black uppercase tracking-widest rounded-full">
                    Administrador
                  </span>
                )}
             </div>

             <div className="bg-gray-900 border border-white/10 rounded-2xl overflow-hidden py-2 shadow-xl">
                <button 
                  onClick={() => setActiveTab('DADOS')}
                  className={`w-full flex items-center gap-3 px-6 py-3 text-sm font-bold transition-colors ${activeTab === 'DADOS' ? 'bg-white/10 text-white border-r-4 border-accent' : 'text-gray-400 hover:text-white hover:bg-white/5'}`}
                >
                  <Settings size={18} /> Dados da Conta
                </button>
                <button 
                  onClick={() => setActiveTab('UPLOADS')}
                  className={`w-full flex items-center gap-3 px-6 py-3 text-sm font-bold transition-colors ${activeTab === 'UPLOADS' ? 'bg-white/10 text-white border-r-4 border-accent' : 'text-gray-400 hover:text-white hover:bg-white/5'}`}
                >
                  <FileText size={18} /> Meus Tópicos
                </button>
             </div>

             {/* Atalho Jogos Rei da Mesa */}
             <div className="bg-gradient-to-tr from-accent/20 to-transparent border border-accent/20 rounded-2xl p-6 flex flex-col items-center text-center shadow-[0_0_15px_rgba(255,215,0,0.1)] relative overflow-hidden group">
               <div className="absolute -right-4 -bottom-4 opacity-10 rotate-12 group-hover:scale-110 transition-transform"><User size={120} /></div>
               <h3 className="font-black uppercase tracking-tight text-accent mb-2 z-10">Rei da Mesa</h3>
               <p className="text-xs text-gray-300 mb-4 z-10">Acesse seu histórico de rodadas completas no Fantasy Game.</p>
               <Link to="/reidamesa/perfil" className="w-full px-4 py-2 bg-accent text-black font-bold uppercase text-xs tracking-widest rounded-lg hover:bg-accentHover transition-colors z-10 flex items-center justify-center gap-2">
                 Ver Perfil de Jogo <ArrowRight size={14} />
               </Link>
             </div>

             {/* Deslogar */}
             <button 
               onClick={logout}
               className="w-full flex items-center justify-center gap-2 px-6 py-3 border border-red-500/20 bg-red-500/10 text-red-500 hover:bg-red-500/20 rounded-xl font-bold uppercase tracking-widest text-xs transition-colors"
             >
               <LogOut size={16} /> Sair da Conta
             </button>
          </div>

          {/* Main Area */}
          <div className="flex-1">
             {activeTab === 'DADOS' && (
               <motion.div 
                 initial={{ opacity: 0, y: 10 }}
                 animate={{ opacity: 1, y: 0 }}
                 className="bg-gray-900 border border-white/10 rounded-2xl p-8 shadow-2xl"
               >
                  <div className="flex justify-between items-start mb-8">
                    <div>
                      <h3 className="text-2xl font-black uppercase tracking-tight">Informações Pessoais</h3>
                      {isOnboarding && !nickname && <p className="text-accent text-xs font-bold mt-1 animate-pulse">! Por favor, defina seu nickname para continuar.</p>}
                    </div>
                  </div>
                  
                  <form className="space-y-8" onSubmit={handleSave}>
                    {error && (
                      <div className="bg-red-500/10 border border-red-500/20 text-red-500 p-4 rounded-xl text-xs font-bold">
                        {error}
                      </div>
                    )}

                    <div>
                      <label className="block text-xs font-bold text-gray-400 uppercase tracking-widest mb-4 text-center md:text-left">Escolha seu Avatar de Manager</label>
                      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
                        {DEFAULT_AVATARS.map((av) => (
                          <button
                            key={av.id}
                            type="button"
                            onClick={() => setSelectedAvatar(av.path)}
                            className={`relative aspect-square rounded-xl overflow-hidden border-2 transition-all group ${selectedAvatar === av.path ? 'border-accent scale-105 shadow-[0_0_20px_rgba(255,215,0,0.2)]' : 'border-white/5 hover:border-white/20'}`}
                          >
                            <img src={av.path} alt={av.label} className="w-full h-full object-cover" />
                            <div className={`absolute inset-0 flex items-center justify-center transition-opacity ${selectedAvatar === av.path ? 'bg-accent/10 opacity-100' : 'bg-black/40 opacity-0 group-hover:opacity-100'}`}>
                               {selectedAvatar === av.path && <ShieldCheck size={32} className="text-accent" />}
                            </div>
                          </button>
                        ))}
                      </div>
                    </div>

                    <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
                      <div>
                        <label className="block text-xs font-bold text-gray-400 uppercase tracking-widest mb-2">Nickname (Nome de Exibição)</label>
                        <div className="relative">
                          <input 
                            type="text" 
                            disabled={isTwitchUser || isNicknameLocked || !isAccountConfigured}
                            value={nickname}
                            onChange={(e) => setNickname(e.target.value)}
                            className={`w-full bg-black/50 border rounded-xl py-3 px-4 text-sm text-white focus:outline-none transition-colors ${
                              !isAccountConfigured 
                                ? 'opacity-50 cursor-not-allowed border-red-500/50 bg-red-500/5' 
                                : (isTwitchUser || isNicknameLocked) ? 'opacity-50 cursor-not-allowed border-white/5' : (isOnboarding && !nickname ? 'border-accent/50 animate-pulse' : 'border-white/10 focus:border-accent/50')
                            }`}
                            placeholder="Ex: ManagerBrabo"
                            title={isNicknameLocked ? 'Nickname definitivo habilitado. Não é possível alterar.' : !isAccountConfigured ? 'Configure sua conta primeiro para desbloquear' : ''}
                          />
                          {!isAccountConfigured && (
                            <div className="flex items-center gap-1.5 mt-2 text-[10px] text-red-400 font-bold uppercase tracking-wider">
                              <ShieldCheck size={12} /> Defina seu método de autenticação para desbloquear
                            </div>
                          )}
                          {isTwitchUser && (
                            <div className="flex items-center gap-1.5 mt-2 text-[10px] text-purple-400 font-bold uppercase tracking-wider">
                              <Twitch size={12} /> Nickname sincronizado com a Twitch
                            </div>
                          )}
                          {!isTwitchUser && isNicknameLocked && isAccountConfigured && (
                            <div className="flex items-center gap-1.5 mt-2 text-[10px] text-gray-500 font-bold uppercase tracking-wider">
                              <ShieldCheck size={12} /> Nickname Definitivo (Irreversível)
                            </div>
                          )}
                        </div>
                        <p className="text-[10px] text-gray-500 mt-1">Apenas letras, números e underscores (3-15 caracteres).</p>
                      </div>
                      <div>
                        <label className="block text-xs font-bold text-gray-400 uppercase tracking-widest mb-2">Status da Identidade</label>
                        <div className="w-full bg-black/20 border border-white/5 rounded-xl py-3 px-4 text-sm text-gray-400 flex items-center gap-2">
                          <ShieldCheck size={16} className="text-green-500" /> E-mail Protegido & Verificado
                        </div>
                        <p className="text-[10px] text-gray-500 mt-1">Identidade VintesetFM ativa e segura.</p>
                      </div>
                    </div>
                    
                    <div className="pt-4 border-t border-white/10 flex justify-end">
                      <button 
                        type="submit" 
                        disabled={isSaved || (isNicknameLocked && nickname !== user.nickname)}
                        className={`flex items-center justify-center gap-2 font-black uppercase tracking-widest px-8 py-3 rounded-xl transition-all shadow-lg ${isSaved ? 'bg-green-500 text-black' : ((isNicknameLocked && nickname !== user.nickname) ? 'bg-gray-700 text-gray-400 cursor-not-allowed' : 'bg-white hover:bg-gray-200 text-black')}`}
                      >
                        {isSaved ? <Save size={18} /> : null} 
                        {isSaved ? 'Perfil Atualizado!' : 'Salvar Perfil'}
                      </button>
                    </div>
                  </form>
               </motion.div>
             )}

             {activeTab === 'UPLOADS' && (
               <motion.div 
                 initial={{ opacity: 0, y: 10 }}
                 animate={{ opacity: 1, y: 0 }}
                 className="bg-gray-900 border border-white/10 rounded-2xl p-8 shadow-2xl min-h-[500px]"
               >
                 <div className="flex justify-between items-center mb-8 border-b border-white/10 pb-6">
                   <h3 className="text-2xl font-black uppercase tracking-tight">Meus Tópicos (Fórum)</h3>
                   <Link to="/downloads" className="px-4 py-2 bg-accent/10 border border-accent/20 text-accent hover:bg-accent/20 font-bold uppercase tracking-widest text-xs rounded-lg transition-colors">
                     Novo Tópico
                   </Link>
                 </div>

                 <div className="flex flex-col items-center justify-center pt-16 opacity-50 text-center">
                    <FileText size={64} className="text-gray-600 mb-4" />
                    <h4 className="font-bold text-lg mb-2 text-gray-300">Nenhuma publicação ainda</h4>
                    <p className="text-sm text-gray-500 max-w-sm">Você ainda não enviou nenhuma tática, gráfico ou save para a comunidade VintesetFM.</p>
                 </div>
               </motion.div>
             )}

          </div>
        </div>

      </div>
    </div>
  );
};

export default MinhaConta;
