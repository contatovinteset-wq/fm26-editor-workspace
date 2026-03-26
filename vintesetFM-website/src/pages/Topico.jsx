import React, { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import { Download, Share2, Heart, MessageSquare, ArrowLeft, Tag, Calendar, User, CheckCircle2, Trash2, ExternalLink } from 'lucide-react';
import { Link, useParams, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { toast } from 'react-hot-toast';

const Topico = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  
  const [topic, setTopic] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchTopic = async () => {
      try {
        const res = await fetch(`/api/forum/${id}`);
        if (!res.ok) throw new Error('Falha ao buscar tópico');
        const data = await res.json();
        setTopic(data);
      } catch (err) {
        console.error(err);
      } finally {
        setLoading(false);
      }
    };
    fetchTopic();
  }, [id]);

  const handleDelete = async () => {
    if (!window.confirm('Tem certeza que deseja excluir permanentemente este tópico?')) return;
    
    try {
      const res = await fetch(`/api/forum/${id}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
      });
      if (!res.ok) throw new Error('Erro ao excluir tópico');
      
      toast.success('Tópico excluído com sucesso!');
      navigate('/downloads');
    } catch (err) {
      toast.error(err.message);
    }
  };

  if (loading) {
    return (
      <div className="w-full min-h-screen bg-bgDark text-white pt-24 pb-16 flex items-center justify-center">
        <div className="animate-spin w-12 h-12 border-4 border-accent border-t-transparent rounded-full"></div>
      </div>
    );
  }

  if (!topic) {
    return (
      <div className="w-full min-h-screen bg-bgDark text-white pt-24 pb-16 flex flex-col items-center justify-center">
         <h1 className="text-3xl font-black mb-4">Tópico não encontrado</h1>
         <p className="text-gray-400 mb-8">Nenhum dado real recebido ou tópico inexistente.</p>
         <Link to="/downloads" className="px-6 py-2 bg-accent hover:bg-accentHover transition-colors text-black font-bold uppercase tracking-wider rounded-xl">
           Voltar aos Downloads
         </Link>
      </div>
    );
  }

  const isOwnerOrAdmin = user?.roles?.includes('OWNER') || user?.roles?.includes('ADMIN') || user?.roles?.includes('ADMIN_DOWNLOADS');
  const isAuthor = topic.authorId === user?.id;
  const canDelete = isOwnerOrAdmin || isAuthor;

  return (
    <div className="w-full min-h-screen bg-bgDark text-white pt-24 pb-16">
      <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8">
        
        {/* Nav / Voltar */}
        <div className="mb-8 flex items-center justify-between">
          <Link to="/downloads" className="inline-flex items-center gap-2 text-gray-400 hover:text-white transition-colors">
            <ArrowLeft size={16} /> Voltar para o Fórum
          </Link>
          
          {canDelete && (
            <button 
              onClick={handleDelete}
              className="flex items-center gap-2 bg-red-500/10 hover:bg-red-500/20 text-red-500 px-4 py-2 rounded-lg font-bold text-sm transition-colors"
            >
              <Trash2 size={16} /> Excluir Tópico
            </button>
          )}
        </div>

        {/* Cabelhaço do Tópico */}
        <div className="bg-gray-900 border border-white/10 rounded-t-3xl p-8 relative overflow-hidden">
           <div className="absolute top-0 right-0 py-2 px-10 bg-accent text-black font-black uppercase text-xs tracking-widest shadow-xl rounded-bl-3xl">
             {topic.category}
           </div>

           <h1 className="text-3xl sm:text-4xl font-black tracking-tight mb-4 pr-20">{topic.title}</h1>
           
           <div className="flex flex-wrap items-center gap-6 text-sm text-gray-400 font-mono">
              <div className="flex items-center gap-2 text-white bg-white/5 py-1 pr-3 rounded-full border border-white/10">
                <div className="w-8 h-8 rounded-full bg-accent/20 flex items-center justify-center overflow-hidden">
                  {topic.author?.avatar ? (
                    <img src={topic.author.avatar} alt="Avatar" className="w-full h-full object-cover" />
                  ) : (
                    <User size={14} className="text-accent" />
                  )}
                </div>
                <span className="font-bold">{topic.author?.nickname || 'Usuario'}</span>
                {topic.author?.roles?.includes('OWNER') && <CheckCircle2 size={14} className="text-blue-400" />}
              </div>
              <span className="flex items-center gap-2"><Calendar size={16} /> {new Date(topic.createdAt).toLocaleDateString('pt-BR')}</span>
              <span className="flex items-center gap-2 text-red-400"><Heart size={16} /> {topic.likes || 0} curtidas</span>
           </div>
        </div>

        {/* Corpo do Tópico */}
        <div className="bg-black/40 border-x border-white/10 border-b border-white/5 rounded-b-3xl p-8 shadow-2xl mb-12 relative">
           
           <div className="prose prose-invert max-w-none mb-12 whitespace-pre-line text-gray-300 leading-relaxed text-lg break-words">
             {topic.content}
           </div>

           {/* Call to Action Principal - Se o Link foi colocado na formatação */}
           <div className="w-full bg-gradient-to-r from-accent/20 to-transparent p-1 rounded-2xl border border-accent/20">
             <div className="bg-gray-900 rounded-xl p-8 flex flex-col sm:flex-row items-center justify-between gap-6">
               <div>
                  <h3 className="text-xl font-bold mb-1 flex items-center gap-2"><Tag size={20} className="text-accent" /> Sobre este Arquivo</h3>
                  <p className="text-gray-400 text-sm">Verifique os links deixados pelo autor para proceder com a instalação.</p>
               </div>
               
               <div className="flex gap-3 w-full sm:w-auto">
                 <button className="flex-1 sm:flex-none border border-white/10 hover:bg-white/5 p-4 rounded-xl text-gray-400 hover:text-white transition-colors flex items-center justify-center">
                   <Share2 size={20} />
                 </button>
               </div>
             </div>
           </div>
        </div>

        {/* Seção de Comentários */}
        <div>
           <div className="flex items-center gap-3 mb-6">
              <MessageSquare className="text-gray-500" />
              <h2 className="text-2xl font-black uppercase tracking-tight">Discussão da Comunidade <span className="text-accent">({topic.comments?.length || 0})</span></h2>
           </div>

           <div className="bg-gray-900 border border-white/10 rounded-2xl p-6 mb-8 flex gap-4 opacity-50 pointer-events-none">
              <div className="w-10 h-10 rounded-full bg-white/10 flex-shrink-0 flex items-center justify-center font-bold text-gray-500">
                 <User size={20} />
              </div>
              <div className="w-full">
                 <textarea 
                   placeholder="Módulo de comentários em construção..." 
                   className="w-full bg-black/50 border border-white/10 rounded-xl p-4 text-sm text-white focus:outline-none focus:border-accent/50 min-h-[100px] mb-3 transition-colors"
                   disabled
                 ></textarea>
                 <div className="flex justify-end">
                   <button className="bg-gray-700 text-gray-400 font-bold uppercase tracking-widest text-xs px-6 py-3 rounded-lg shadow-lg cursor-not-allowed">Comentar</button>
                 </div>
              </div>
           </div>

           <div className="space-y-4">
             {topic.comments?.map(comment => (
               <div key={comment.id} className="bg-black/50 border border-white/5 rounded-2xl p-6 flex gap-4 hover:border-white/10 transition-colors">
                  <div className="w-10 h-10 rounded-full bg-primary/20 border border-primary/50 flex-shrink-0 flex items-center justify-center font-bold text-primary overflow-hidden">
                     {comment.author?.avatar ? <img src={comment.author.avatar} alt="Avatar" /> : comment.author?.nickname?.substring(0,2).toUpperCase()}
                  </div>
                  <div>
                    <div className="flex items-center gap-3 mb-2">
                      <span className="font-bold">{comment.author?.nickname || 'Usuario'}</span>
                      <span className="text-[10px] text-gray-500 font-mono">{new Date(comment.createdAt).toLocaleDateString('pt-BR')}</span>
                    </div>
                    <p className="text-gray-300 text-sm leading-relaxed">{comment.content}</p>
                  </div>
               </div>
             ))}
           </div>
        </div>

      </div>
    </div>
  );
};

export default Topico;
