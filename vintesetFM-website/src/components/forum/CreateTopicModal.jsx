import React from 'react';
import { motion } from 'framer-motion';
import { PlusCircle } from 'lucide-react';

const CreateTopicModal = ({ onClose }) => {
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
             <input type="text" className="w-full bg-black/50 border border-white/10 rounded-lg px-4 py-3 text-white focus:outline-none focus:border-accent transition-colors" placeholder="Ex: Facepack Vinteset 2026" />
           </div>
           <div className="grid grid-cols-2 gap-4">
             <div>
               <label className="block text-xs font-bold uppercase tracking-widest text-gray-400 mb-2">Categoria</label>
               <select className="w-full bg-black/50 border border-white/10 rounded-lg px-4 py-3 text-white focus:outline-none focus:border-accent transition-colors appearance-none">
                 <option value="ferramentas">Ferramenta</option>
                 <option value="mods">Mod/Skin</option>
                 <option value="db">Database</option>
               </select>
             </div>
             <div>
               <label className="block text-xs font-bold uppercase tracking-widest text-gray-400 mb-2">Link (Drive/MF/etc)</label>
               <input type="url" className="w-full bg-black/50 border border-white/10 rounded-lg px-4 py-3 text-white focus:outline-none focus:border-accent transition-colors" placeholder="https://" />
             </div>
           </div>
           <div>
             <label className="block text-xs font-bold uppercase tracking-widest text-gray-400 mb-2">Descrição / Release Notes</label>
             <textarea rows="4" className="w-full bg-black/50 border border-white/10 rounded-lg px-4 py-3 text-white focus:outline-none focus:border-accent transition-colors resize-none" placeholder="Conte os detalhes do arquivo..."></textarea>
           </div>
           
           <button 
            onClick={() => {
              alert("Esse formulário será futuramente conectado ao banco de dados!");
              onClose();
            }}
            className="w-full bg-accent text-black font-black uppercase tracking-wide py-4 rounded-xl hover:bg-accentHover transition-colors mt-4"
           >
             Enviar Para Aprovação (Em Breve)
           </button>
        </div>
      </motion.div>
    </div>
  );
};

export default CreateTopicModal;
