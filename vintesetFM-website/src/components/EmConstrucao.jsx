import React from 'react';
import { AlertTriangle } from 'lucide-react';
import { Link } from 'react-router-dom';

const EmConstrucao = () => {
  return (
    <div className="w-full min-h-screen bg-bgDark text-white pt-24 pb-16 flex flex-col items-center justify-center p-6">
      <AlertTriangle size={64} className="text-accent mb-6 animate-pulse" />
      <h1 className="text-4xl sm:text-5xl font-black uppercase tracking-tighter mb-4 text-center">
         O Rei da Mesa está <span className="text-transparent bg-clip-text bg-gradient-to-r from-accent to-accentHover">Em Construção</span>
      </h1>
      <p className="text-gray-400 text-center max-w-lg mb-8 text-lg">
         O Fantasy Game exclusivo da nossa comunidade está recebendo os últimos ajustes no backend e nas integrações ao vivo. Volte em breve!
      </p>
      <Link to="/" className="px-8 py-3 bg-white/5 hover:bg-white/10 border border-white/10 rounded-xl font-bold transition-all flex items-center gap-2">
         Voltar para o Início
      </Link>
    </div>
  );
};

export default EmConstrucao;
