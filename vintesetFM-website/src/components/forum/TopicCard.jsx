import React from 'react';
import { motion } from 'framer-motion';
import { User, ThumbsUp, MessageSquare, Calendar } from 'lucide-react';
import { Link } from 'react-router-dom';

const TopicCard = ({ topic, index, categoryLabel }) => {
  return (
    <motion.div 
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay: index * 0.05 }}
      className="bg-gray-900 border border-white/10 hover:border-accent/40 rounded-xl p-5 flex flex-col md:flex-row items-start md:items-center gap-6 transition-all group"
    >
      {/* Avatar e Status */}
      <div className="hidden md:flex flex-col items-center justify-center min-w-[80px]">
        <div className="w-14 h-14 rounded-full bg-gradient-to-br from-primary/60 to-accent/40 border-2 border-white/10 flex items-center justify-center shadow-lg group-hover:border-accent transition-colors">
           <User size={24} className="text-white" />
        </div>
        <span className="text-[10px] font-bold uppercase tracking-widest text-accent mt-2">
          {topic.author}
        </span>
      </div>

      {/* Corpo Principal */}
      <div className="flex-grow flex flex-col">
        <div className="flex items-center gap-2 mb-1">
          <span className="px-2 py-0.5 bg-white/10 text-xs font-bold uppercase tracking-wider text-gray-300 rounded">
            {categoryLabel}
          </span>
          {topic.isHot && (
            <span className="px-2 py-0.5 bg-red-500/20 text-red-400 border border-red-500/30 text-xs font-bold uppercase tracking-wider rounded flex items-center gap-1">
              🔥 Em Alta
            </span>
          )}
        </div>
        
        <h3 className="text-xl font-black text-white uppercase tracking-tight group-hover:text-accent transition-colors cursor-pointer">
          {topic.title}
        </h3>
        <p className="text-gray-400 text-sm mt-2 line-clamp-2">
          {topic.description}
        </p>

        {/* Mobile Author Info */}
        <div className="flex md:hidden items-center gap-2 mt-4 text-xs font-bold text-gray-400 uppercase tracking-widest">
          <User size={14} className="text-accent" /> {topic.author}
        </div>
      </div>

      {/* Estatísticas (Stats) */}
      <div className="flex md:flex-col items-center justify-center gap-4 md:gap-2 min-w-[120px] bg-black/40 md:bg-transparent p-3 md:p-0 rounded-lg w-full md:w-auto mt-4 md:mt-0">
        <div className="flex items-center gap-2 text-gray-300">
           <ThumbsUp size={16} className="text-accent" /> 
           <span className="font-bold text-lg">{topic.likes}</span>
        </div>
        <div className="flex items-center gap-2 text-gray-500 text-sm">
           <MessageSquare size={14} /> {topic.comments} comentários
        </div>
        <div className="flex items-center gap-1.5 text-gray-600 text-xs mt-1 hidden md:flex">
           <Calendar size={12} /> {topic.date}
        </div>
      </div>

      {/* Ação */}
      <div className="hidden lg:flex items-center justify-center pl-4 border-l border-white/5 h-full">
         <Link to={`/downloads/${topic.id}`} className="bg-white/5 hover:bg-white/10 border border-white/10 text-white font-bold text-sm px-6 py-2 rounded-lg transition-colors whitespace-nowrap">
           Baixar / Ver
         </Link>
      </div>
    </motion.div>
  );
};

export default TopicCard;
