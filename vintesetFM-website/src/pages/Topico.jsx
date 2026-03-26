import React, { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import { Download, Share2, Heart, MessageSquare, ArrowLeft, Tag, Calendar, User, CheckCircle2, Trash2, ExternalLink, Check, ShieldAlert, XCircle } from 'lucide-react';
import { Link, useParams, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { toast } from 'react-hot-toast';

const Topico = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  
  const [topic, setTopic] = useState(null);
  const [loading, setLoading] = useState(true);
  const [likesCount, setLikesCount] = useState(0);
  const [commentContent, setCommentContent] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isCopied, setIsCopied] = useState(false);

  const renderTextWithLinks = (text) => {
    if (!text) return null;
    const urlRegex = /(https?:\/\/[^\s]+)/g;
    return text.split(urlRegex).map((part, i) => {
      if (part.match(urlRegex)) {
        return (
          <a key={i} href={part} target="_blank" rel="noopener noreferrer" className="text-blue-400 hover:text-blue-300 underline underline-offset-2 break-all">
            {part}
          </a>
        );
      }
      return part;
    });
  };

  useEffect(() => {
    const fetchTopic = async () => {
      try {
        const res = await fetch(`/api/forum/${id}`);
        if (!res.ok) throw new Error('Falha ao buscar tópico');
        const data = await res.json();
        setTopic(data);
        setLikesCount(data._count?.likes || 0);
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

  const handleLike = async () => {
    if (!user) {
      toast.error("Você precisa estar logado para curtir.");
      return;
    }

    try {
      const res = await fetch(`/api/forum/${id}/like`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
      });
      if (!res.ok) throw new Error('Erro ao registrar curtida');
      
      const data = await res.json();
      setLikesCount(data.likesCount);
      toast.success(data.liked ? "Tópico curtido!" : "Curtida removida.");
    } catch (err) {
      toast.error(err.message);
    }
  };

  const handleComment = async () => {
    if (!user) {
      toast.error("Você precisa estar logado para comentar.");
      return;
    }
    if (!commentContent.trim()) {
      toast.error("O comentário não pode estar vazio.");
      return;
    }

    setIsSubmitting(true);
    try {
      const res = await fetch(`/api/forum/${id}/comment`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        body: JSON.stringify({ content: commentContent })
      });
      if (!res.ok) throw new Error('Erro ao publicar comentário');
      
      const newComment = await res.json();
      setTopic(prev => ({
        ...prev,
        comments: [...(prev.comments || []), newComment]
      }));
      setCommentContent('');
      toast.success("Comentário publicado!");
    } catch (err) {
      toast.error(err.message);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleShare = () => {
    navigator.clipboard.writeText(window.location.href);
    toast.success('Link copiado para a área de transferência!');
    setIsCopied(true);
    setTimeout(() => setIsCopied(false), 2000);
  };

  const handleDeleteComment = async (commentId) => {
    if (!window.confirm('Deseja excluir este comentário?')) return;
    try {
      const res = await fetch(`/api/forum/comment/${commentId}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
      });
      if (!res.ok) throw new Error('Erro ao excluir comentário');
      
      setTopic(prev => ({
        ...prev,
        comments: prev.comments.filter(c => c.id !== commentId)
      }));
      toast.success('Comentário excluído!');
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

  if (!user) {
    return (
      <div className="w-full min-h-screen bg-bgDark text-white pt-24 pb-16 flex flex-col items-center justify-center px-4 text-center">
         <div className="w-20 h-20 bg-accent/10 rounded-full flex items-center justify-center mb-6">
           <Download size={40} className="text-accent" />
         </div>
         <h1 className="text-3xl md:text-5xl font-black mb-4 uppercase tracking-tighter">Acesso Restrito</h1>
         <p className="text-gray-400 mb-8 max-w-lg text-lg">
           Você precisa fazer login na plataforma para visualizar este tópico e baixar os arquivos anexados.
         </p>
         <button onClick={() => navigate('/minha-conta')} className="px-8 py-4 bg-accent hover:bg-accentHover transition-colors text-black font-black uppercase tracking-widest rounded-xl shadow-[0_0_20px_rgba(255,215,0,0.3)]">
           Fazer Login Agora
         </button>
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

        {/* Banners de Moderação */}
        {topic.status === 'PENDING' && (
           <div className="bg-yellow-500/10 border border-yellow-500/30 text-yellow-500 p-4 rounded-xl mb-6 flex items-start gap-4 shadow-lg">
             <ShieldAlert size={24} className="mt-1 flex-shrink-0" />
             <div>
               <h4 className="font-bold text-lg uppercase tracking-tight">Post em Análise</h4>
               <p className="text-sm opacity-80 mt-1">
                 Este tópico foi retido pela malha-fina e aguarda aprovação de um moderador. Apenas você e a moderação podem visualizá-lo.
               </p>
             </div>
           </div>
        )}

        {topic.status === 'REJECTED' && (
           <div className="bg-red-500/10 border border-red-500/30 text-red-500 p-4 rounded-xl mb-6 flex items-start gap-4 shadow-lg">
             <XCircle size={24} className="mt-1 flex-shrink-0" />
             <div>
               <h4 className="font-bold text-lg uppercase tracking-tight">Post Reprovado</h4>
               <p className="text-sm opacity-80 mt-1 mb-2">
                 Este conteúdo violou as diretrizes da comunidade ou foi considerado inadequado.
               </p>
               <div className="bg-black/40 p-2 rounded border border-red-500/20 inline-block font-mono text-xs">
                 Razão: {topic.moderationReason}
               </div>
             </div>
           </div>
        )}

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
              <button onClick={handleLike} className="flex items-center gap-2 text-red-400 hover:text-red-300 transition-colors bg-red-400/10 px-3 py-1 rounded-lg">
                <Heart size={16} /> {likesCount} curtidas
              </button>
           </div>
        </div>

        {/* Corpo do Tópico */}
        <div className="bg-black/40 border-x border-white/10 border-b border-white/5 rounded-b-3xl p-8 shadow-2xl mb-12 relative">
           
           <div className="prose prose-invert max-w-none mb-12 whitespace-pre-line text-gray-300 leading-relaxed text-lg break-words">
             {renderTextWithLinks(topic.content)}
           </div>

           {/* Call to Action Principal - Se o Link foi colocado na formatação */}
           <div className="w-full bg-gradient-to-r from-accent/20 to-transparent p-1 rounded-2xl border border-accent/20">
             <div className="bg-gray-900 rounded-xl p-8 flex flex-col sm:flex-row items-center justify-between gap-6">
               <div>
                  <h3 className="text-xl font-bold mb-1 flex items-center gap-2"><Tag size={20} className="text-accent" /> Sobre este Arquivo</h3>
                  <p className="text-gray-400 text-sm">Verifique os links deixados pelo autor para proceder com a instalação.</p>
               </div>
               
               <div className="flex gap-3 w-full sm:w-auto">
                 <button onClick={handleShare} className="flex-1 sm:flex-none border border-white/10 hover:bg-white/5 px-6 py-4 sm:p-4 rounded-xl text-gray-400 hover:text-white transition-colors flex items-center justify-center gap-2 font-bold uppercase text-xs tracking-widest">
                   {isCopied ? (
                     <>
                       <Check size={20} className="text-green-400" />
                       <span className="text-green-400">Copiado</span>
                     </>
                   ) : (
                     <>
                        <Share2 size={20} />
                        <span className="sm:hidden">Compartilhar</span>
                     </>
                   )}
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

           <div className={`bg-gray-900 border border-white/10 rounded-2xl p-6 mb-8 flex flex-col sm:flex-row gap-4 ${!user ? 'opacity-50 pointer-events-none' : ''}`}>
              <div className="hidden sm:flex w-10 h-10 rounded-full bg-white/10 flex-shrink-0 items-center justify-center font-bold text-gray-500 overflow-hidden">
                 {user?.avatar ? <img src={user.avatar} alt="Seu Avatar" /> : <User size={20} />}
              </div>
              <div className="w-full">
                 {!user && <p className="text-red-400 text-xs mb-2 font-bold uppercase">Faça login para comentar</p>}
                 <textarea 
                   value={commentContent}
                   onChange={(e) => setCommentContent(e.target.value)}
                   placeholder="Adicione à discussão..." 
                   className="w-full bg-black/50 border border-white/10 rounded-xl p-4 text-sm text-white focus:outline-none focus:border-accent/50 min-h-[100px] mb-3 transition-colors resize-y"
                   disabled={!user || isSubmitting}
                 ></textarea>
                 <div className="flex justify-end">
                   <button 
                     onClick={handleComment} 
                     disabled={!user || isSubmitting || !commentContent.trim()}
                     className="bg-accent hover:bg-accentHover text-black font-bold uppercase tracking-widest text-xs px-6 py-3 rounded-lg shadow-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                   >
                     {isSubmitting ? 'Enviando...' : 'Comentar'}
                   </button>
                 </div>
              </div>
           </div>

           <div className="space-y-4">
             {topic.comments?.length > 0 ? (
               topic.comments.map(comment => {
                 const canDeleteComment = user?.roles?.includes('OWNER') || user?.roles?.includes('ADMIN') || user?.roles?.includes('ADMIN_DOWNLOADS') || comment.authorId === user?.id;

                 return (
                   <div key={comment.id} className="bg-black/50 border border-white/5 rounded-2xl p-6 flex flex-col sm:flex-row gap-4 hover:border-white/10 transition-colors relative">
                      {canDeleteComment && (
                        <button 
                          onClick={() => handleDeleteComment(comment.id)} 
                          className="absolute top-4 right-4 text-red-500/40 hover:text-red-500 transition-colors"
                          title="Excluir comentário"
                        >
                          <Trash2 size={16} />
                        </button>
                      )}
                      
                      <div className="w-8 h-8 sm:w-10 sm:h-10 rounded-full bg-primary/20 border border-primary/50 flex-shrink-0 flex items-center justify-center font-bold text-primary overflow-hidden">
                         {comment.author?.avatar ? <img src={comment.author.avatar} alt="Avatar" className="w-full h-full object-cover" /> : <User size={16} />}
                      </div>
                      <div className="w-full">
                        <div className="flex flex-wrap items-center gap-2 sm:gap-3 mb-2 pr-8">
                          <span className="font-bold text-white text-sm sm:text-base">{comment.author?.nickname || 'Usuario'}</span>
                          <span className="text-[10px] sm:text-xs text-gray-500 font-mono tracking-wider">{new Date(comment.createdAt).toLocaleDateString('pt-BR')} às {new Date(comment.createdAt).toLocaleTimeString('pt-BR', {hour: '2-digit', minute:'2-digit'})}</span>
                        </div>
                        <p className="text-gray-300 text-sm leading-relaxed whitespace-pre-wrap">{comment.content}</p>
                      </div>
                   </div>
                 );
               })
             ) : (
               <div className="text-center py-10 bg-black/30 rounded-2xl border border-white/5">
                 <MessageSquare size={32} className="mx-auto text-gray-600 mb-3" />
                 <p className="text-gray-400 text-sm font-bold uppercase tracking-widest">Seja o primeiro a comentar</p>
               </div>
             )}
           </div>
        </div>

      </div>
    </div>
  );
};

export default Topico;
