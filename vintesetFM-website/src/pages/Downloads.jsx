import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { 
  DownloadCloud, 
  Search, 
  PlusCircle,
  Wrench,
  Palette,
  Database
} from 'lucide-react';
import forumMocks from '../data/forumMocks.json';
import TopicCard from '../components/forum/TopicCard';
import CreateTopicModal from '../components/forum/CreateTopicModal';

const CATEGORIES = [
  { id: 'all', label: 'Todos', icon: DownloadCloud },
  { id: 'ferramentas', label: 'Ferramentas', icon: Wrench },
  { id: 'mods', label: 'Mods/Skins', icon: Palette },
  { id: 'db', label: 'Databases', icon: Database }
];

const Downloads = () => {
  const [activeTab, setActiveTab] = useState('all');
  const [searchQuery, setSearchQuery] = useState('');
  const [isModalOpen, setIsModalOpen] = useState(false);

  // Filtro de Busca e Aba
  const filteredTopics = forumMocks.filter(topic => {
    const matchesTab = activeTab === 'all' || topic.category === activeTab;
    const matchesSearch = topic.title.toLowerCase().includes(searchQuery.toLowerCase()) || 
                          topic.description.toLowerCase().includes(searchQuery.toLowerCase());
    return matchesTab && matchesSearch;
  });

  return (
    <div className="min-h-screen pt-24 pb-16 px-4 sm:px-6 lg:px-8 max-w-7xl mx-auto">
      
      {/* Header Fórum */}
      <div className="flex flex-col md:flex-row justify-between items-start md:items-center mb-10 gap-6">
        <div>
          <h1 className="text-4xl md:text-5xl font-black text-white uppercase tracking-tighter flex items-center gap-4">
            <DownloadCloud className="text-accent" size={40} />
            Hub de Downloads
          </h1>
          <p className="text-gray-400 mt-2 text-lg">
            Compartilhe e descubra as melhores ferramentas, skins e databases para o FM26.
          </p>
        </div>
        
        <button 
          onClick={() => setIsModalOpen(true)}
          className="flex items-center gap-2 bg-accent hover:bg-accentHover text-black px-6 py-3 rounded-xl font-black uppercase tracking-wide transition-all duration-300 shadow-[0_0_20px_rgba(255,215,0,0.2)]"
        >
          <PlusCircle size={20} />
          Criar Tópico
        </button>
      </div>

      {/* Submenus (Tabs) e Barra de Busca */}
      <div className="flex flex-col lg:flex-row gap-4 mb-8">
        <div className="flex bg-white/5 p-1 rounded-xl border border-white/10 overflow-x-auto no-scrollbar">
          {CATEGORIES.map((cat) => {
            const Icon = cat.icon;
            const isActive = activeTab === cat.id;
            return (
              <button
                key={cat.id}
                onClick={() => setActiveTab(cat.id)}
                className={`flex items-center gap-2 px-6 py-3 rounded-lg text-sm font-bold uppercase tracking-wide transition-all whitespace-nowrap ${
                  isActive 
                    ? 'bg-primary/30 text-white shadow-inner border border-primary/40' 
                    : 'text-gray-400 hover:text-white hover:bg-white/10'
                }`}
              >
                <Icon size={16} className={isActive ? "text-accent" : ""} />
                {cat.label}
              </button>
            );
          })}
        </div>

        <div className="flex-grow flex items-center bg-black/40 border border-white/10 rounded-xl px-4 py-2 ring-1 ring-transparent focus-within:ring-accent/50 focus-within:border-accent/50 transition-all">
          <Search size={20} className="text-gray-400 mr-3" />
          <input 
            type="text"
            placeholder="Buscar mods, ferramentas, autores..."
            className="bg-transparent border-none text-white outline-none w-full placeholder:text-gray-600 font-medium"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
          />
        </div>
      </div>

      {/* Tópicos (Estilo Fórum) */}
      <div className="flex flex-col gap-4">
        {filteredTopics.length > 0 ? (
          filteredTopics.map((topic, index) => {
            const categoryLabel = CATEGORIES.find(c => c.id === topic.category)?.label || topic.category;
            return (
              <TopicCard 
                key={topic.id} 
                topic={topic} 
                index={index} 
                categoryLabel={categoryLabel} 
              />
            );
          })
        ) : (
          <div className="text-center py-20 bg-black/40 rounded-xl border border-white/5">
            <DownloadCloud size={48} className="mx-auto text-gray-600 mb-4" />
            <h3 className="text-xl font-bold text-gray-400 uppercase">Nenhum Tópico Encontrado</h3>
            <p className="text-gray-500 mt-2 text-sm">Tente limpar sua busca ou trocar de categoria.</p>
          </div>
        )}
      </div>

      {/* Create Topic Modal */}
      {isModalOpen && (
        <CreateTopicModal onClose={() => setIsModalOpen(false)} />
      )}

    </div>
  );
};

export default Downloads;
