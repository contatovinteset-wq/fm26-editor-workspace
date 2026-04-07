import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { MessageSquare, Users, Eye, TrendingUp, PlusCircle } from 'lucide-react';

const ForumHome = () => {
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    fetch('/api/board/categories')
      .then(res => res.json())
      .then(data => {
        setCategories(data);
        setLoading(false);
      });
  }, []);

  if (loading) {
    return (
      <div className="min-h-screen pt-24 pb-16 px-4 bg-black text-white flex items-center justify-center">
        <div className="animate-pulse text-accent font-black uppercase text-xl">Carregando Fórum...</div>
      </div>
    );
  }

  return (
    <div className="min-h-screen pt-24 pb-16 px-4 sm:px-6 lg:px-8 max-w-7xl mx-auto">
      <div className="flex flex-col md:flex-row justify-between items-start md:items-center mb-10 gap-6">
        <div>
          <h1 className="text-4xl md:text-5xl font-black text-white uppercase tracking-tighter flex items-center gap-4">
            <MessageSquare className="text-accent" size={40} />
            Fórum da Comunidade
          </h1>
          <p className="text-gray-400 mt-2 text-lg font-medium">
            Participe das discussões, compartilhe táticas e viva o Football Manager.
          </p>
        </div>
        <div className="text-sm font-bold text-gray-500 uppercase tracking-widest border border-white/10 px-4 py-2 rounded-lg bg-white/5">
          👉 Selecione uma Categoria abaixo para Postar
        </div>
      </div>

      <div className="grid gap-6">
        {categories.map((cat) => (
          <div 
            key={cat.id} 
            onClick={() => navigate(`/forum/${cat.slug}`)}
            className="bg-white/5 border border-white/10 rounded-xl p-6 transition-all hover:border-primary/50 relative overflow-hidden group cursor-pointer"
          >
            <div className="absolute top-0 left-0 w-1 h-full bg-accent opacity-0 group-hover:opacity-100 transition-opacity" />
            <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-6">
              
              <div className="flex-1">
                <span className="text-2xl font-black text-white uppercase group-hover:text-accent transition-colors">
                  {cat.name}
                </span>
                <p className="text-gray-400 mt-2 text-sm">{cat.description}</p>
              </div>

              <div className="flex items-center gap-8 text-center md:text-right">
                <div>
                  <div className="text-2xl font-black text-white">{cat._count?.topics || 0}</div>
                  <div className="text-xs font-bold text-gray-500 uppercase tracking-widest">Tópicos</div>
                </div>

                <div className="w-px h-12 bg-white/10 hidden md:block" />

                <div className="min-w-[200px] text-left hidden md:block">
                  <div className="text-xs font-bold text-gray-500 uppercase tracking-widest mb-1">Último Tópico</div>
                  {cat.latestTopic ? (
                    <div className="flex items-center gap-3">
                      <img 
                        src={cat.latestTopic.author.avatar || `https://ui-avatars.com/api/?name=${cat.latestTopic.author.nickname}&background=1A1A1A&color=FFD700`}
                        alt="Avatar"
                        className="w-8 h-8 rounded-full border border-white/10"
                      />
                      <div className="truncate max-w-[150px]">
                        <Link to={`/forum/t/${cat.latestTopic.id}`} className="text-sm font-bold text-white hover:text-accent truncate block">
                          {cat.latestTopic.title}
                        </Link>
                        <span className="text-xs text-gray-400">{new Date(cat.latestTopic.updatedAt).toLocaleDateString('pt-BR')}</span>
                      </div>
                    </div>
                  ) : (
                    <span className="text-sm text-gray-500">Nenhum tópico ainda.</span>
                  )}
                </div>
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

export default ForumHome;
