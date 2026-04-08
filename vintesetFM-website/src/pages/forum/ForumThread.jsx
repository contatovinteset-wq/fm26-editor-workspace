import React, { useState, useEffect } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { ChevronLeft, Pin, Lock, Trash2, ShieldAlert, MessageSquare, Edit3 } from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import toast from 'react-hot-toast';

import MDEditor from '@uiw/react-md-editor';

const ForumThread = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const { user, isOwnerOrAdmin } = useAuth();
  
  const [topic, setTopic] = useState(null);
  const [loading, setLoading] = useState(true);
  
  const [replyContent, setReplyContent] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const [editingTopic, setEditingTopic] = useState(false);
  const [editTopicTitle, setEditTopicTitle] = useState('');
  const [editTopicContent, setEditTopicContent] = useState('');

  const [editingPostId, setEditingPostId] = useState(null);
  const [editPostContent, setEditPostContent] = useState('');

  // Check if user is Mod 
  const isMod = user?.roles?.includes('OWNER') || user?.roles?.includes('ADMIN') || user?.roles?.includes('MODERATOR');

  const fetchTopic = () => {
    fetch(`/api/board/topics/${id}`)
      .then(res => res.json())
      .then(data => {
        if(data.error) navigate('/forum');
        setTopic(data);
        setLoading(false);
      })
      .catch(() => navigate('/forum'));
  };

  useEffect(() => {
    fetchTopic();
  }, [id]);

  const handleReply = async () => {
    if (!replyContent.trim()) return toast.error('A resposta não pode estar vazia.');
    setSubmitting(true);
    try {
      const res = await fetch(`/api/board/topics/${id}/posts`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ content: replyContent })
      });
      if (res.ok) {
        toast.success('Resposta publicada!');
        setReplyContent('');
        fetchTopic(); // Re-fetch to see new post
      } else {
        const err = await res.json();
        toast.error(err.error || 'Erro ao publicar.');
      }
    } catch (err) {
      toast.error('Erro de conexão.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleModAction = async (action) => {
    try {
      const res = await fetch(`/api/board/topics/${id}/${action}`, { method: 'POST' });
      if (res.ok) {
        toast.success(`Tópico ${action === 'pin' ? 'fixado/desfixado' : 'fechado/aberto'}!`);
        fetchTopic();
      }
    } catch (e) {
      toast.error('Erro na ação de moderação.');
    }
  };

  const handleEditTopic = async () => {
    try {
      const res = await fetch(`/api/board/topics/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ title: editTopicTitle, content: editTopicContent })
      });
      if (res.ok) {
        toast.success('Tópico editado!');
        setEditingTopic(false);
        fetchTopic();
      } else {
        const err = await res.json();
        toast.error(err.error || 'Erro ao editar tópico.');
      }
    } catch (e) {
      toast.error('Erro de conexão.');
    }
  };

  const handleEditPost = async (postId) => {
    try {
      const res = await fetch(`/api/board/posts/${postId}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ content: editPostContent })
      });
      if (res.ok) {
        toast.success('Resposta editada!');
        setEditingPostId(null);
        fetchTopic();
      } else {
        const err = await res.json();
        toast.error(err.error || 'Erro ao editar resposta.');
      }
    } catch (e) {
      toast.error('Erro de conexão.');
    }
  };

  const handleDeletePost = async (postId) => {
    if (!window.confirm('Tem certeza que deseja excluir esta resposta?')) return;
    try {
      const res = await fetch(`/api/board/posts/${postId}`, { method: 'DELETE' });
      if (res.ok) {
        toast.success('Resposta excluída.');
        fetchTopic();
      }
    } catch(e) {
      toast.error('Erro ao excluir.');
    }
  };

  const handleDeleteTopic = async () => {
    if (!window.confirm('Tem certeza que deseja excluir todo o tópico?')) return;
    try {
      const res = await fetch(`/api/board/topics/${id}`, { method: 'DELETE' });
      if (res.ok) {
        toast.success('Tópico excluído!');
        navigate(`/forum/${topic.category.slug}`);
      }
    } catch(e) {
      toast.error('Erro ao excluir tópico.');
    }
  };

  const handlePaste = async (e) => {
    const items = (e.clipboardData || e.originalEvent?.clipboardData)?.items;
    if (!items) return;
    for (const item of items) {
      if (item.type.indexOf('image/') === 0) {
        e.preventDefault();
        const file = item.getAsFile();
        if (!file) continue;
        const formData = new FormData();
        formData.append('image', file);
        const toastId = toast.loading('Enviando imagem...');
        try {
          const res = await fetch('/api/board/upload', {
            method: 'POST',
            body: formData
          });
          const data = await res.json();
          if (res.ok) {
            toast.success('Imagem anexada!', { id: toastId });
            setNewPostContent(prev => prev + `\n![Print da Tela](${data.url})\n`);
          } else {
            toast.error(data.error || 'Erro ao enviar.', { id: toastId });
          }
        } catch (error) {
          toast.error('Erro de conexão.', { id: toastId });
        }
      }
    }
  };

  if (loading) return <div className="min-h-screen pt-24 text-center text-accent uppercase font-black">Carregando...</div>;
  if (!topic) return null;

  return (
    <div className="min-h-screen pt-24 pb-16 px-4 sm:px-6 lg:px-8 max-w-5xl mx-auto" data-color-mode="dark">
      
      {/* Breadcrumb */}
      <div className="mb-6 flex items-center justify-between">
        <Link to={`/forum/${topic.category.slug}`} className="text-gray-400 hover:text-white flex items-center gap-2 text-sm font-bold uppercase tracking-widest transition-colors w-fit">
          <ChevronLeft size={16} /> Voltar para {topic.category.name}
        </Link>
        
        {/* Mod Controls */}
        {(isMod || user?.id === topic.authorId) && (
          <div className="flex gap-2 flex-wrap">
            <button onClick={handleDeleteTopic} className="px-3 py-1 text-xs font-bold uppercase tracking-widest rounded flex items-center gap-1 bg-red-500/10 text-red-500 hover:bg-red-500/20 transition-colors">
              <Trash2 size={14} /> Excluir
            </button>
            {isMod && (
              <>
                <button onClick={() => handleModAction('pin')} className={`px-3 py-1 text-xs font-bold uppercase tracking-widest rounded flex items-center gap-1 transition-colors ${topic.isPinned ? 'bg-primary text-white' : 'bg-white/10 text-gray-300 hover:bg-white/20'}`}>
                  <Pin size={14} /> {topic.isPinned ? 'Desfixar' : 'Fixar'}
                </button>
                <button onClick={() => handleModAction('close')} className={`px-3 py-1 text-xs font-bold uppercase tracking-widest rounded flex items-center gap-1 transition-colors ${topic.isClosed ? 'bg-red-500 text-white' : 'bg-white/10 text-gray-300 hover:bg-white/20'}`}>
                  <Lock size={14} /> {topic.isClosed ? 'Abrir' : 'Fechar'}
                </button>
              </>
            )}
          </div>
        )}
      </div>

      {/* Título */}
      <div className="mb-8">
        <h1 className="text-3xl md:text-4xl font-black text-white flex items-center gap-3">
          {topic.isPinned && <Pin className="text-primary fill-primary" />}
          {topic.isClosed && <Lock className="text-red-500" />}
          {topic.title}
        </h1>
        <div className="text-gray-500 text-sm font-bold uppercase tracking-widest flex gap-4 mt-2">
          <span>Views: {topic.views}</span>
          <span>Respostas: {topic.posts.length}</span>
        </div>
      </div>

      {/* Post Original (Topico em Si) */}
      <div className="bg-white/5 border border-white/10 rounded-xl overflow-hidden mb-6 flex flex-col md:flex-row">
        {/* Author Panel */}
        <div className="bg-black/40 p-6 md:w-64 border-b md:border-b-0 md:border-r border-white/5 flex flex-col items-centertext-center">
          <img src={topic.author.avatar || `https://ui-avatars.com/api/?name=${topic.author.nickname}&background=1A1A1A&color=FFD700`} alt="Avatar" className="w-20 h-20 rounded-full border-2 border-white/20 mx-auto mb-3" />
          <div className="font-black text-white text-lg text-center break-words">{topic.author.nickname}</div>
          <div className="text-xs font-bold text-accent uppercase tracking-widest text-center mt-1">Autor</div>
        </div>
        {/* Content */}
        <div className="flex-1 flex flex-col relative bg-[#0d1117] overflow-x-auto"> 
          <div className="p-6 flex-1 break-words">
            {editingTopic ? (
               <div className="flex flex-col gap-3">
                 <input 
                   className="w-full bg-black/50 border border-white/10 rounded px-3 py-2 text-white outline-none focus:border-accent transition-colors"
                   value={editTopicTitle}
                   onChange={e => setEditTopicTitle(e.target.value)}
                   placeholder="Título do Tópico"
                 />
                 <div className="mt-2" onPaste={handlePaste}>
                   <MDEditor value={editTopicContent} onChange={setEditTopicContent} preview="edit" height={300} />
                 </div>
                 <div className="flex justify-end gap-2 mt-2">
                   <button onClick={() => setEditingTopic(false)} className="px-4 py-2 bg-white/5 text-white rounded text-sm font-bold uppercase tracking-widest hover:bg-white/10">Cancelar</button>
                   <button onClick={handleEditTopic} className="px-4 py-2 bg-accent text-black rounded text-sm font-black uppercase tracking-widest hover:bg-accentHover">Salvar</button>
                 </div>
               </div>
            ) : (
               <MDEditor.Markdown source={topic.content} style={{ backgroundColor: 'transparent', color: '#e5e7eb' }} />
            )}
          </div>
          {!editingTopic && (isMod || user?.id === topic.authorId) && (
            <div className="p-3 bg-black/20 border-t border-white/5 flex justify-end gap-3">
              {(isMod || user?.id === topic.authorId) && (
                <button onClick={() => { setEditTopicTitle(topic.title); setEditTopicContent(topic.content); setEditingTopic(true); }} className="text-gray-500 hover:text-accent flex items-center gap-1 text-xs font-bold uppercase transition-colors">
                  <Edit3 size={14} /> Editar
                </button>
              )}
            </div>
          )}
        </div>
      </div>

      {/* Posts/Replies */}
      {topic.posts.map((post, idx) => {
        const isPostAuthor = user?.id === post.authorId;
        return (
          <div key={post.id} className="bg-white/5 border border-white/10 rounded-xl overflow-hidden mb-6 flex flex-col md:flex-row">
            <div className="bg-black/40 p-6 md:w-64 border-b md:border-b-0 md:border-r border-white/5 flex flex-col items-center">
              <img src={post.author.avatar || `https://ui-avatars.com/api/?name=${post.author.nickname}&background=1A1A1A&color=FFD700`} alt="Avatar" className="w-16 h-16 rounded-full border border-white/20 mx-auto mb-3" />
              <div className="font-bold text-white text-md text-center break-words">{post.author.nickname}</div>
              <div className="text-xs font-bold text-gray-500 uppercase tracking-widest text-center mt-1">#{idx+1}</div>
            </div>
            <div className="flex-1 flex flex-col relative bg-[#0d1117]">
              <div className="p-6 flex-1 overflow-x-auto break-words">
                {editingPostId === post.id ? (
                   <div className="flex flex-col gap-3">
                     <div onPaste={handlePaste}>
                       <MDEditor value={editPostContent} onChange={setEditPostContent} preview="edit" height={200} />
                     </div>
                     <div className="flex justify-end gap-2 mt-2">
                       <button onClick={() => setEditingPostId(null)} className="px-4 py-2 bg-white/5 text-white rounded text-sm font-bold uppercase tracking-widest hover:bg-white/10">Cancelar</button>
                       <button onClick={() => handleEditPost(post.id)} className="px-4 py-2 bg-accent text-black rounded text-sm font-black uppercase tracking-widest hover:bg-accentHover">Salvar</button>
                     </div>
                   </div>
                ) : (
                  <MDEditor.Markdown source={post.content} style={{ backgroundColor: 'transparent', color: '#e5e7eb' }} />
                )}
              </div>
              {!editingPostId && (isMod || isPostAuthor) && (
                <div className="p-3 bg-black/20 border-t border-white/5 flex justify-end gap-3">
                  {(isMod || isPostAuthor) && (
                    <button onClick={() => { setEditPostContent(post.content); setEditingPostId(post.id); }} className="text-gray-500 hover:text-accent flex items-center gap-1 text-xs font-bold uppercase transition-colors">
                      <Edit3 size={14} /> Editar
                    </button>
                  )}
                  {(isMod || isPostAuthor) && (
                    <button onClick={() => handleDeletePost(post.id)} className="text-gray-500 hover:text-red-500 flex items-center gap-1 text-xs font-bold uppercase transition-colors">
                      <Trash2 size={14} /> Excluir
                    </button>
                  )}
                </div>
              )}
            </div>
          </div>
        )
      })}

      {/* Reply Area */}
      <div className="mt-10">
        {topic.isClosed ? (
          <div className="bg-red-500/10 border border-red-500/20 text-red-500 p-6 rounded-xl text-center font-bold uppercase tracking-widest flex items-center justify-center gap-3">
            <Lock size={20} /> Este tópico encontra-se fechado para novas respostas.
          </div>
        ) : !user ? (
          <div className="bg-white/5 border border-white/10 p-6 rounded-xl text-center">
            <h3 className="text-lg font-bold text-white uppercase tracking-widest mb-2">Você precisa estar logado para responder</h3>
            <Link to="/login" className="text-accent hover:text-white transition-colors underline underline-offset-4">Fazer Login</Link>
          </div>
        ) : (
          <div className="bg-primary/20 border border-primary/30 p-6 rounded-[20px]">
            <h3 className="text-xl font-black text-white uppercase mb-4 flex items-center gap-2">
              <MessageSquare size={20} className="text-accent" /> Deixe sua Resposta
            </h3>
            <div className="mb-4" onPaste={handlePaste}>
              {/* Rich Text Editor */}
              <MDEditor
                value={replyContent}
                onChange={setReplyContent}
                height={300}
                preview="edit"
                hideToolbar={false}
              />
            </div>
            <div className="flex justify-end">
              <button 
                onClick={handleReply}
                disabled={submitting}
                className="bg-accent hover:bg-accentHover text-black px-8 py-3 rounded-xl font-black uppercase tracking-widest transition-all shadow-lg shadow-accent/20 disabled:opacity-50"
              >
                {submitting ? 'Enviando...' : 'Responder'}
              </button>
            </div>
          </div>
        )}
      </div>

    </div>
  );
};

export default ForumThread;
