import React from 'react';
import { motion } from 'framer-motion';
import { Download, Share2, Heart, MessageSquare, ArrowLeft, Tag, Calendar, User, CheckCircle2 } from 'lucide-react';
import { Link, useParams, Navigate } from 'react-router-dom';

const Topico = () => {
  if (import.meta.env.PROD) {
    return <Navigate to="/downloads" replace />;
  }
  
  const { id } = useParams(); // simulando ler o ID da rota
  
  // Tópico Mock (na vida real faria fetch pelo ID)
  const mockTopico = {
    id: id || "t123",
    title: "Tática Vinteset Invencível 4-3-3 (Versão Final 2026)",
    category: "Táticas",
    author: "Vinteset",
    date: "10 Mar 2026",
    likes: 42,
    downloads: 1337,
    content: `Fala galera da comunidade!\n\nHoje estou trazendo a versão final da tática que usamos para ganhar a Champions League com o Wrexham no último save da Live. O segredo dessa tática é a intensidade insana dos Box-to-Box na ligação com os pontas invertidos.\n\nInstruções:\n1. Coloque sempre alas atacando.\n2. Mantenha os zagueiros concentrados no combate físico.\n3. Treine resistência, pois cansa muito o time.\n\n**Baixe abaixo o arquivo .fmf e desfrute de muito Gegenpress no seu save!** Qualquer dúvida, mandem nos comentários.`,
    size: "24.5 KB",
    image: "https://images.unsplash.com/photo-1579952363873-27f3bade9f55?q=80&w=1200", // soccer tactic generic image
    comments: [
      { id: 1, author: "GegenManager", text: "Testei aqui no meu save com o Vasco da Gama e finalmente saí do sufoco! Muito obrigado Vinteset!", time: "2 dias atrás" },
      { id: 2, author: "TaticoMaster", text: "Funciona bem com zagueiros lentos nas extremidades ou seria melhor adaptar os alas?", time: "Ontem" },
    ]
  };

  return (
    <div className="w-full min-h-screen bg-bgDark text-white pt-24 pb-16">
      <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8">
        
        {/* Nav / Voltar */}
        <div className="mb-8">
          <Link to="/downloads" className="inline-flex items-center gap-2 text-gray-400 hover:text-white transition-colors">
            <ArrowLeft size={16} /> Voltar para o Fórum
          </Link>
        </div>

        {/* Cabelhaço do Tópico */}
        <div className="bg-gray-900 border border-white/10 rounded-t-3xl p-8 relative overflow-hidden">
           {/* Decorativo de Categoria */}
           <div className="absolute top-0 right-0 py-2 px-10 bg-accent text-black font-black uppercase text-xs tracking-widest shadow-xl rounded-bl-3xl">
             {mockTopico.category}
           </div>

           <h1 className="text-3xl sm:text-4xl font-black tracking-tight mb-4 pr-20">{mockTopico.title}</h1>
           
           <div className="flex flex-wrap items-center gap-6 text-sm text-gray-400 font-mono">
              <div className="flex items-center gap-2 text-white bg-white/5 px-3 py-1 rounded-full border border-white/10">
                <div className="w-6 h-6 rounded-full bg-accent/20 flex items-center justify-center text-[10px]"><User size={12} className="text-accent" /></div>
                <span className="font-bold">{mockTopico.author}</span>
                {mockTopico.author === "Vinteset" && <CheckCircle2 size={14} className="text-blue-400" />}
              </div>
              <span className="flex items-center gap-2"><Calendar size={16} /> {mockTopico.date}</span>
              <span className="flex items-center gap-2 text-green-400"><Download size={16} /> {mockTopico.downloads} downloads</span>
              <span className="flex items-center gap-2 text-red-400"><Heart size={16} /> {mockTopico.likes} curtidas</span>
           </div>
        </div>

        {/* Corpo do Tópico */}
        <div className="bg-black/40 border-x border-white/10 border-b border-white/5 rounded-b-3xl p-8 shadow-2xl mb-12 relative">
           
           {/* Imagem (Se houver) */}
           {mockTopico.image && (
             <div className="w-full h-80 rounded-2xl overflow-hidden mb-8 border border-white/10 relative">
                <div className="absolute inset-0 bg-gradient-to-t from-bgDark to-transparent z-10 pointer-events-none"></div>
                <img src={mockTopico.image} alt="Tática Preview" className="w-full h-full object-cover" />
             </div>
           )}

           {/* Conteúdo Rico (Marcador mock) */}
           <div className="prose prose-invert max-w-none mb-12 whitespace-pre-line text-gray-300 leading-relaxed text-lg">
             {mockTopico.content}
           </div>

           {/* Call to Action Principal - O Download */}
           <div className="w-full bg-gradient-to-r from-accent/20 to-transparent p-1 rounded-2xl border border-accent/20">
             <div className="bg-gray-900 rounded-xl p-8 flex flex-col sm:flex-row items-center justify-between gap-6">
               <div>
                  <h3 className="text-xl font-bold mb-1 flex items-center gap-2"><Tag size={20} className="text-accent" /> Arquivo Anexo</h3>
                  <p className="text-gray-400 text-sm">Tatica_Vinteset_433.fmf <span className="text-xs bg-white/10 px-2 py-0.5 rounded ml-2">{mockTopico.size}</span></p>
               </div>
               
               <div className="flex gap-3 w-full sm:w-auto">
                 <button className="flex-1 sm:flex-none border border-white/10 hover:bg-white/5 p-4 rounded-xl text-gray-400 hover:text-white transition-colors flex items-center justify-center">
                   <Share2 size={20} />
                 </button>
                 <button className="flex-1 sm:flex-none flex items-center justify-center gap-3 bg-accent hover:bg-accentHover text-black font-black uppercase tracking-widest px-8 py-4 rounded-xl shadow-[0_0_20px_rgba(255,215,0,0.3)] transition-transform hover:scale-105">
                   <Download size={20} /> Baixar Mod
                 </button>
               </div>
             </div>
           </div>
        </div>

        {/* Seção de Comentários */}
        <div>
           <div className="flex items-center gap-3 mb-6">
              <MessageSquare className="text-gray-500" />
              <h2 className="text-2xl font-black uppercase tracking-tight">Discussão da Comunidade <span className="text-accent">({mockTopico.comments.length})</span></h2>
           </div>

           {/* Mock form de responder */}
           <div className="bg-gray-900 border border-white/10 rounded-2xl p-6 mb-8 flex gap-4">
              <div className="w-10 h-10 rounded-full bg-white/10 flex-shrink-0 flex items-center justify-center font-bold text-gray-500">VO</div>
              <div className="w-full">
                 <textarea 
                   placeholder="O que achou dessa postagem? Deixe seu comentário..." 
                   className="w-full bg-black/50 border border-white/10 rounded-xl p-4 text-sm text-white focus:outline-none focus:border-accent/50 min-h-[100px] mb-3 transition-colors"
                 ></textarea>
                 <div className="flex justify-end">
                   <button className="bg-white hover:bg-gray-200 text-black font-bold uppercase tracking-widest text-xs px-6 py-3 rounded-lg transition-colors shadow-lg">Comentar</button>
                 </div>
              </div>
           </div>

           {/* Lista de Comentários */}
           <div className="space-y-4">
             {mockTopico.comments.map(comment => (
               <div key={comment.id} className="bg-black/50 border border-white/5 rounded-2xl p-6 flex gap-4 hover:border-white/10 transition-colors">
                  <div className="w-10 h-10 rounded-full bg-primary/20 border border-primary/50 flex-shrink-0 flex items-center justify-center font-bold text-primary">
                     {comment.author.substring(0,2).toUpperCase()}
                  </div>
                  <div>
                    <div className="flex items-center gap-3 mb-2">
                      <span className="font-bold">{comment.author}</span>
                      <span className="text-[10px] text-gray-500 font-mono">{comment.time}</span>
                    </div>
                    <p className="text-gray-300 text-sm leading-relaxed">{comment.text}</p>
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
