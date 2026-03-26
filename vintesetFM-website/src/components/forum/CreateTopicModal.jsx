import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { PlusCircle, Loader2 } from 'lucide-react';

const CreateTopicModal = ({ onClose }) => {
  const [title, setTitle] = useState('');
  const [category, setCategory] = useState('ferramentas');
  const [externalLink, setExternalLink] = useState('');
  const [content, setContent] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async () => {
    if (!title.trim() || !content.trim()) {
      setError('Por favor, preencha o título e a descrição.');
      return;
    }
    
    setIsLoading(true);
    setError('');

    try {
      const res = await fetch('/api/forum', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ title, category, externalLink, content })
      });

      if (!res.ok) {
        const errorData = await res.json();
        throw new Error(errorData.error || 'Erro de conexão com o banco de dados. Tente novamente.');
      }

      // Tópico criado com sucesso!
      window.location.reload(); // Atualiza a página para mostrar o novo tópico
    } catch (err) {
      console.error(err);
      setError(err.message || 'Erro ao criar tópico. Tente novamente.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center bg-black/80 backdrop-blur-sm px-4">
      <motion.div 
        initial={{ opacity: 0, scale: 0.95 }}
        animate={{ opacity: 1, scale: 1 }}
        className="bg-gray-900 border border-white/10 rounded-2xl p-6 md:p-8 w-full max-w-2xl relative shadow-2xl"
      >
        <button 
          onClick={onClose}
          className="absolute top-4 right-4 text-gray-500 hover:text-white bg-white/5 hover:bg-white/20 p-2 rounded-full transition-colors"
        >
          &times;
        </button>
        <h2 className="text-2xl font-black text-white uppercase tracking-tighter mb-6 flex items-center gap-3">
          <PlusCircle className="text-accent" />
          Criar Novo Tópico
        </h2>
        <div className="space-y-4">
         <div>
             <label className="block text-xs font-bold uppercase tracking-widest text-gray-400 mb-2">Título do Mod/Ferramenta</label>
             <input type="text" value={title} onChange={e => setTitle(e.target.value)} className="w-full bg-black/50 border border-white/10 rounded-lg px-4 py-3 text-white focus:outline-none focus:border-accent transition-colors" placeholder="Ex: Facepack Vinteset 2026" />
           </div>
           <div className="grid grid-cols-2 gap-4">
             <div>
               <label className="block text-xs font-bold uppercase tracking-widest text-gray-400 mb-2">Categoria</label>
               <select value={category} onChange={e => setCategory(e.target.value)} className="w-full bg-black/50 border border-white/10 rounded-lg px-4 py-3 text-white focus:outline-none focus:border-accent transition-colors appearance-none">
                 <option value="ferramentas">Ferramenta</option>
                 <option value="mods">Mod/Skin</option>
                 <option value="db">Database</option>
               </select>
             </div>
             <div>
               <label className="block text-xs font-bold uppercase tracking-widest text-gray-400 mb-2">Link (Drive/MF/etc)</label>
               <input type="url" value={externalLink} onChange={e => setExternalLink(e.target.value)} className="w-full bg-black/50 border border-white/10 rounded-lg px-4 py-3 text-white focus:outline-none focus:border-accent transition-colors" placeholder="https://" />
             </div>
           </div>
           <div>
             <label className="block text-xs font-bold uppercase tracking-widest text-gray-400 mb-2">Descrição / Release Notes</label>
             <textarea rows="4" value={content} onChange={e => setContent(e.target.value)} className="w-full bg-black/50 border border-white/10 rounded-lg px-4 py-3 text-white focus:outline-none focus:border-accent transition-colors resize-none" placeholder="Conte os detalhes do arquivo..."></textarea>
           </div>
           
           {error && (
             <div className="bg-red-500/10 border border-red-500/20 text-red-500 p-3 rounded-xl text-xs font-bold mt-2">
               {error}
             </div>
           )}

           <button 
            onClick={handleSubmit}
            disabled={isLoading}
            className="w-full flex justify-center items-center gap-2 bg-accent text-black font-black uppercase tracking-wide py-4 rounded-xl hover:bg-accentHover transition-colors mt-4 disabled:opacity-50 disabled:cursor-not-allowed"
           >
             {isLoading ? <Loader2 className="animate-spin" size={20} /> : 'Criar Tópico'}
           </button>
        </div>
      </motion.div>
    </div>
  );
};

export default CreateTopicModal;
