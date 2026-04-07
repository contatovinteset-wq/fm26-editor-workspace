import React, { useState, useEffect } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { MessageSquare, PlusCircle, Pin, Lock, ChevronLeft } from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import toast from 'react-hot-toast';
import MDEditor from '@uiw/react-md-editor';

const ForumCategory = () => {
  const { slug } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const [category, setCategory] = useState(null);
  const [loading, setLoading] = useState(true);

  const [isCreating, setIsCreating] = useState(false);
  const [newTopicTitle, setNewTopicTitle] = useState('');
  const [newTopicContent, setNewTopicContent] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

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
            setNewTopicContent(prev => prev + `\n![Print da Tela](${data.url})\n`);
          } else {
            toast.error(data.error || 'Erro ao enviar.', { id: toastId });
          }
        } catch (error) {
          toast.error('Erro de conexão.', { id: toastId });
        }
      }
    }
  };

  const fetchCategory = () => {
    fetch(`/api/board/categories/${slug}`)
      .then(res => res.json())
      .then(data => {
        setCategory(data);
        setLoading(false);
      })
      .catch(() => setLoading(false));
  };

  useEffect(() => {
    fetchCategory();
  }, [slug]);

  const handleCreateTopic = async (e) => {
    e.preventDefault();
    if (!user) return toast.error('Você precisa estar logado.');

    try {
      const res = await fetch('/api/board/topics', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ title: newTopicTitle, content: newTopicContent, categoryId: category.id })
      });
      const result = await res.json();
      if (res.ok) {
        if (result.warning) {
          toast.success('Tópico enviado para a Análise da Moderação!');
        } else {
          toast.success('Tópico criado com sucesso!');
        }
        setIsCreating(false);
        setNewTopicTitle('');
        setNewTopicContent('');
        fetchCategory();
      } else {
        toast.error(result.error || 'Erro ao criar tópico.');
      }
    } catch (err) {
      toast.error('Erro de conexão.');
    }
  };

  if (loading) return <div className="min-h-screen pt-24 text-center text-white"><div className="animate-pulse">Carregando...</div></div>;
  if (!category) return <div className="min-h-screen pt-24 text-center text-white">Categoria não encontrada.</div>;

  return (
    <div className="min-h-screen pt-24 pb-16 px-4 sm:px-6 lg:px-8 max-w-7xl mx-auto">
      
      <div className="mb-6">
        <Link to="/forum" className="text-gray-400 hover:text-white flex items-center gap-2 text-sm font-bold uppercase tracking-widest transition-colors w-fit">
          <ChevronLeft size={16} /> Voltar para o Fórum
        </Link>
      </div>

      <div className="flex flex-col md:flex-row justify-between items-start md:items-center mb-10 gap-6">
        <div>
          <h1 className="text-4xl md:text-5xl font-black text-white uppercase tracking-tighter">
            {category.name}
          </h1>
          <p className="text-gray-400 mt-2 text-lg font-medium">{category.description}</p>
        </div>
        
        {user ? (
          <button 
            onClick={() => setIsCreating(!isCreating)}
            className="flex items-center gap-2 bg-accent hover:bg-accentHover text-black px-6 py-3 rounded-xl font-black uppercase tracking-wide transition-all shadow-[0_0_20px_rgba(255,215,0,0.2)]"
          >
            <PlusCircle size={20} />
            {isCreating ? 'Cancelar' : 'Novo Tópico'}
          </button>
        ) : (
          <div className="text-sm font-bold text-gray-500 uppercase tracking-widest border border-white/10 px-4 py-2 rounded-lg">
            Faça login para postar
          </div>
        )}
      </div>

      {isCreating && (
        <div className="bg-white/5 border border-white/10 rounded-xl p-6 mb-8 animate-in fade-in slide-in-from-top-4">
          <h3 className="text-xl font-black text-white uppercase mb-4">Criar Novo Tópico</h3>
          <form onSubmit={handleCreateTopic} className="space-y-4">
            <div>
              <label className="block text-xs font-bold text-gray-500 uppercase tracking-widest mb-1">Título</label>
              <input 
                type="text" 
                placeholder="Ex: Como lidar com a crise financeira no FM?"
                className="w-full bg-[#0d1117] border border-white/10 rounded px-4 py-3 text-white focus:outline-none focus:border-primary"
                value={newTopicTitle}
                onChange={e => setNewTopicTitle(e.target.value)}
                maxLength={100}
                required
              />
            </div>
            
            <div data-color-mode="dark" onPaste={handlePaste}>
              <label className="block text-xs font-bold text-gray-500 uppercase tracking-widest mb-1">Conteúdo</label>
              <MDEditor
                value={newTopicContent}
                onChange={setNewTopicContent}
                height={300}
                previewOptions={{
                  style: { backgroundColor: 'transparent', color: '#e5e7eb' }
                }}
                className="w-full !bg-black/40 !border-white/10"
              />
            </div>
            <div className="flex justify-end gap-4">
              <button type="submit" className="bg-primary hover:bg-primaryHover text-white px-6 py-2 rounded-lg font-bold uppercase tracking-widest transition-colors">
                Publicar
              </button>
            </div>
          </form>
        </div>
      )}

      <div className="bg-white/5 border border-white/10 rounded-xl overflow-hidden">
        {/* Table Header */}
        <div className="hidden md:grid grid-cols-[1fr_100px_100px_200px] gap-4 p-4 bg-black/40 border-b border-white/5 text-xs font-bold text-gray-500 uppercase tracking-widest">
          <div>Tópico</div>
          <div className="text-center">Respostas</div>
          <div className="text-center">Views</div>
          <div className="text-right">Última Atividade</div>
        </div>

        {/* Topic List */}
        <div className="divide-y divide-white/5">
          {category.topics.length === 0 ? (
            <div className="p-8 text-center text-gray-500 font-bold uppercase tracking-widest">
              Nenhum tópico criado nesta categoria ainda.
            </div>
          ) : (
            category.topics.map(topic => (
              <div 
                key={topic.id} 
                onClick={() => navigate(`/forum/t/${topic.id}`)}
                className="grid grid-cols-1 md:grid-cols-[1fr_100px_100px_200px] gap-4 p-4 hover:bg-white/5 transition-colors items-center cursor-pointer group"
              >
                
                {/* Info */}
                <div className="flex items-center gap-3">
                  <img 
                    src={topic.author.avatar || `https://ui-avatars.com/api/?name=${topic.author.nickname}&background=1A1A1A&color=FFD700`}
                    className="w-10 h-10 rounded-full border border-white/10 hidden sm:block group-hover:border-accent transition-colors" 
                    alt="Avatar"
                  />
                  <div>
                    <span className="text-lg font-bold text-white group-hover:text-accent flex items-center gap-2 transition-colors">
                      {topic.isPinned && <Pin size={16} className="text-primary fill-primary" />}
                      {topic.isClosed && <Lock size={16} className="text-gray-500" />}
                      {topic.title}
                    </span>
                    <div className="text-xs text-gray-400 mt-1">
                      por <span className="text-accent">{topic.author.nickname}</span> • {new Date(topic.createdAt).toLocaleDateString('pt-BR')}
                    </div>
                  </div>
                </div>

                {/* Counters */}
                <div className="text-center hidden md:block text-gray-300 font-bold">{topic._count?.posts || 0}</div>
                <div className="text-center hidden md:block text-gray-300 font-bold">{topic.views}</div>
                
                {/* Last Activity */}
                <div className="text-right hidden md:block text-sm text-gray-400">
                  {new Date(topic.updatedAt).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })}
                </div>
              </div>
            ))
          )}
        </div>
      </div>

    </div>
  );
};

export default ForumCategory;
