import React, { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { ShieldAlert, CheckCircle, XCircle, Clock, AlertTriangle, User, Eye, Ban } from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { useNavigate } from 'react-router-dom';
import { toast } from 'react-hot-toast';

const ModeratorPanel = () => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState('pending');
  const [topics, setTopics] = useState([]);
  const [loading, setLoading] = useState(true);
  const [rejectReason, setRejectReason] = useState('');
  const [selectedTopicId, setSelectedTopicId] = useState(null);

  const fetchTopics = async (status) => {
    setLoading(true);
    try {
      const res = await fetch(`/api/moderation/${status}`, {
        headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
      });
      if (!res.ok) {
        if (res.status === 403) navigate('/');
        throw new Error('Falha ao buscar fila de moderação');
      }
      const data = await res.json();
      setTopics(data);
    } catch (error) {
      toast.error(error.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchTopics(activeTab);
  }, [activeTab]);

  const handleApprove = async (id) => {
    try {
      const res = await fetch(`/api/moderation/${id}/approve`, {
        method: 'POST',
        headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
      });
      if (!res.ok) throw new Error('Erro ao aprovar tópico');
      toast.success('Tópico aprovado com sucesso!');
      setTopics(topics.filter(t => t.id !== id));
    } catch (error) {
      toast.error(error.message);
    }
  };

  const handleReject = async (e) => {
    e.preventDefault();
    try {
      const res = await fetch(`/api/moderation/${selectedTopicId}/reject`, {
        method: 'POST',
        headers: { 
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('token')}` 
        },
        body: JSON.stringify({ reason: rejectReason })
      });
      if (!res.ok) throw new Error('Erro ao rejeitar tópico');
      toast.success('Tópico rejeitado.');
      setTopics(topics.filter(t => t.id !== selectedTopicId));
      setSelectedTopicId(null);
      setRejectReason('');
    } catch (error) {
      toast.error(error.message);
    }
  };

  return (
    <div className="min-h-screen pt-24 pb-16 px-4 sm:px-6 lg:px-8 max-w-7xl mx-auto text-white">
      {/* Header */}
      <div className="flex flex-col md:flex-row justify-between items-start md:items-center mb-10 gap-6">
        <div>
          <h1 className="text-4xl md:text-5xl font-black text-white uppercase tracking-tighter flex items-center gap-4">
            <ShieldAlert className="text-green-400" size={40} />
            Central de Moderação
          </h1>
          <p className="text-gray-400 mt-2 text-lg">
            Avalie conteúdos retidos na malha fina da Vinteset AI e garanta a pureza da comunidade.
          </p>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex bg-white/5 p-1 rounded-xl mb-8 border border-white/10 w-full md:w-max">
         <button 
           onClick={() => setActiveTab('pending')}
           className={`flex-1 flex items-center justify-center gap-2 px-8 py-3 rounded-lg font-bold uppercase text-sm tracking-wider transition-all ${
             activeTab === 'pending' ? 'bg-primary/40 shadow-inner text-white' : 'text-gray-400 hover:text-white'
           }`}
         >
           <Clock size={16} /> Pendentes
         </button>
         <button 
           onClick={() => setActiveTab('rejected')}
           className={`flex-1 flex items-center justify-center gap-2 px-8 py-3 rounded-lg font-bold uppercase text-sm tracking-wider transition-all ${
             activeTab === 'rejected' ? 'bg-red-500/20 shadow-inner text-red-400' : 'text-gray-400 hover:text-red-400'
           }`}
         >
           <Ban size={16} /> Rejeitados
         </button>
      </div>

      {/* Content */}
      <div className="space-y-4">
        {loading ? (
          <div className="text-center py-20 text-gray-500 font-bold uppercase tracking-widest animate-pulse">
            Sincronizando Fila...
          </div>
        ) : topics.length === 0 ? (
          <div className="text-center py-20 bg-black/40 rounded-xl border border-white/5">
            <CheckCircle size={48} className="mx-auto text-green-500/50 mb-4" />
            <h3 className="text-xl font-bold text-gray-400 uppercase tracking-widest">Fila Limpa!</h3>
            <p className="text-gray-500 mt-2">Nenhum tópico necessita da sua atenção no momento.</p>
          </div>
        ) : (
          topics.map((topic, index) => (
            <motion.div 
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: index * 0.05 }}
              key={topic.id} 
              className="bg-gray-900 border border-white/10 p-6 rounded-xl flex flex-col md:flex-row gap-6 relative"
            >
              <div className="flex-1">
                <div className="flex items-center gap-3 mb-2">
                  <span className="px-2 py-1 bg-white/10 text-[10px] font-bold uppercase tracking-wider rounded">
                    {topic.category}
                  </span>
                  <span className="text-xs text-gray-500 font-mono">
                    {new Date(topic.createdAt).toLocaleString('pt-BR')}
                  </span>
                </div>
                <h3 className="text-xl font-black mb-2">{topic.title}</h3>
                <p className="text-gray-400 text-sm mb-4 line-clamp-3 bg-black/50 p-3 rounded-lg border border-white/5 italic">
                  "{topic.content}"
                </p>
                <div className="flex items-center gap-2 mt-2 bg-yellow-500/10 text-yellow-500/80 px-3 py-2 rounded-lg text-xs font-bold w-max">
                  <AlertTriangle size={14} /> AI Diagnostic: {topic.moderationReason}
                </div>
              </div>

              <div className="flex flex-row md:flex-col items-center justify-center gap-2 border-t md:border-t-0 md:border-l border-white/10 pt-4 md:pt-0 md:pl-6 w-full md:w-48">
                {activeTab === 'pending' ? (
                  <>
                    <button 
                      onClick={() => handleApprove(topic.id)}
                      className="w-full flex items-center justify-center gap-2 bg-green-500/20 hover:bg-green-500/40 text-green-400 font-bold px-4 py-3 rounded-xl transition-colors text-sm uppercase"
                    >
                      <CheckCircle size={16} /> Aprovar
                    </button>
                    <button 
                      onClick={() => setSelectedTopicId(selectedTopicId === topic.id ? null : topic.id)}
                      className="w-full flex items-center justify-center gap-2 bg-red-500/10 hover:bg-red-500/20 text-red-500 font-bold px-4 py-3 rounded-xl transition-colors text-sm uppercase"
                    >
                      <XCircle size={16} /> Barrar
                    </button>
                  </>
                ) : (
                  <button 
                    onClick={() => handleApprove(topic.id)}
                    className="w-full flex items-center justify-center gap-2 bg-accent/20 hover:bg-accent/40 text-accent font-bold px-4 py-3 rounded-xl transition-colors text-sm uppercase"
                  >
                    <CheckCircle size={16} /> Restaurar
                  </button>
                )}
              </div>

              {/* Modal Inline pra Rejeição */}
              <AnimatePresence>
                {selectedTopicId === topic.id && activeTab === 'pending' && (
                  <motion.div 
                    initial={{ opacity: 0, height: 0 }}
                    animate={{ opacity: 1, height: 'auto' }}
                    exit={{ opacity: 0, height: 0 }}
                    className="absolute inset-0 bg-gray-900/95 backdrop-blur-md rounded-xl p-6 flex flex-col justify-center items-center z-10"
                  >
                    <h4 className="text-red-400 font-bold uppercase mb-4 tracking-widest">Motivo da Rejeição</h4>
                    <form onSubmit={handleReject} className="w-full max-w-md flex flex-col gap-3">
                      <input 
                        type="text" 
                        required
                        value={rejectReason}
                        onChange={(e) => setRejectReason(e.target.value)}
                        placeholder="Ex: Conteúdo Tóxico, Spam, Off-Topic..."
                        className="bg-black/50 border border-white/20 p-3 rounded-lg w-full text-white outline-none focus:border-red-500"
                      />
                      <div className="flex gap-2 w-full">
                        <button type="button" onClick={() => setSelectedTopicId(null)} className="flex-1 bg-white/10 hover:bg-white/20 px-4 py-2 rounded-lg font-bold">Cancelar</button>
                        <button type="submit" className="flex-1 bg-red-500 hover:bg-red-600 px-4 py-2 text-white rounded-lg font-bold">Confirmar Ban</button>
                      </div>
                    </form>
                  </motion.div>
                )}
              </AnimatePresence>

            </motion.div>
          ))
        )}
      </div>
    </div>
  );
};

export default ModeratorPanel;
