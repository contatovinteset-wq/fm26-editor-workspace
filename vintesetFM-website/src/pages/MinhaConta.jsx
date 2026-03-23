import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { User, Settings, Image as ImageIcon, FileText, ArrowRight, Save, LogOut } from 'lucide-react';
import { Link } from 'react-router-dom';

const MinhaConta = () => {
  const [activeTab, setActiveTab] = useState('DADOS'); // 'DADOS', 'UPLOADS'
  
  // Mock para exemplo de edições salvas
  const [isSaved, setIsSaved] = useState(false);

  return (
    <div className="w-full min-h-screen bg-bgDark text-white pt-24 pb-16">
      <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
        
        <div className="flex flex-col md:flex-row gap-8">
          
          {/* Sidebar Left */}
          <div className="w-full md:w-64 flex-shrink-0 space-y-6">
             <div className="bg-gray-900 border border-white/10 rounded-2xl p-6 text-center">
                <div className="w-24 h-24 rounded-full bg-accent/20 border-2 border-accent/50 mx-auto flex items-center justify-center mb-4 relative overflow-hidden group">
                  <span className="text-2xl font-black text-accent drop-shadow-lg">MB</span>
                  <div className="absolute inset-0 bg-black/60 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity cursor-pointer">
                    <ImageIcon size={20} />
                  </div>
                </div>
                <h2 className="font-bold text-lg">ManagerBrabo</h2>
                <p className="text-xs text-gray-400 font-mono mt-1">manager@vinteset.com</p>
             </div>

             <div className="bg-gray-900 border border-white/10 rounded-2xl overflow-hidden py-2">
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
             <button className="w-full flex items-center justify-center gap-2 px-6 py-3 border border-red-500/20 bg-red-500/10 text-red-500 hover:bg-red-500/20 rounded-xl font-bold uppercase tracking-widest text-xs transition-colors">
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
                  <h3 className="text-2xl font-black uppercase tracking-tight mb-8">Informações Pessoais</h3>
                  
                  <form className="space-y-6" onSubmit={(e) => { e.preventDefault(); setIsSaved(true); setTimeout(()=>setIsSaved(false), 3000)}}>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                      <div>
                        <label className="block text-xs font-bold text-gray-400 uppercase tracking-widest mb-2">Nome de Exibição (Nickname)</label>
                        <input type="text" defaultValue="ManagerBrabo" className="w-full bg-black/50 border border-white/10 rounded-xl py-3 px-4 text-sm text-white focus:outline-none focus:border-accent/50 transition-colors" />
                      </div>
                      <div>
                        <label className="block text-xs font-bold text-gray-400 uppercase tracking-widest mb-2">E-mail</label>
                        <input type="email" defaultValue="manager@vinteset.com" disabled className="w-full bg-black/20 border border-white/5 rounded-xl py-3 px-4 text-sm text-gray-500 cursor-not-allowed" />
                        <p className="text-[10px] text-gray-500 mt-1">E-mail vinculado não pode ser alterado diretamente.</p>
                      </div>
                    </div>
                    
                    <div>
                      <label className="block text-xs font-bold text-gray-400 uppercase tracking-widest mb-2">Mini-Biografia</label>
                      <textarea rows="4" className="w-full bg-black/50 border border-white/10 rounded-xl py-3 px-4 text-sm text-white focus:outline-none focus:border-accent/50 transition-colors" placeholder="Fale um pouco sobre o seu estilo como Manager..."></textarea>
                    </div>

                    <div className="pt-4 border-t border-white/10 flex justify-end">
                      <button type="submit" className={`flex items-center justify-center gap-2 font-black uppercase tracking-widest px-8 py-3 rounded-xl transition-all shadow-lg ${isSaved ? 'bg-green-500 text-black' : 'bg-white hover:bg-gray-200 text-black'}`}>
                        {isSaved ? <Save size={18} /> : null} {isSaved ? 'Dados Salvos!' : 'Salvar Alterações'}
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
