import React, { useState } from 'react';
import { Bug } from 'lucide-react';
import StaffAnalyzer from '../../components/ferramentas/StaffAnalyzer';
import MoneyballAnalyzer from '../../components/ferramentas/MoneyballAnalyzer';

function AnaliseDados() {
  const [activeTab, setActiveTab] = useState('staff');

  return (
    <div className="bg-bgDark min-h-screen pt-24 pb-12 font-outfit text-white">
      <div className="max-w-[1800px] w-[95%] mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between items-start mb-4">
          <h1 className="text-4xl font-bold bg-clip-text text-transparent bg-gradient-to-r from-accent to-yellow-400">
            Análise de Dados <span className="text-sm border border-accent/50 px-2 py-1 ml-2 rounded-lg text-accent align-middle">BETA</span>
          </h1>
          <a href="/forum/topic/850b04c4-e59d-46af-991a-05647de6b1db" target="_blank" rel="noreferrer"
             className="hidden sm:flex items-center justify-center space-x-1.5 bg-gray-800/80 hover:bg-gray-700/80 border border-yellow-500/30 text-yellow-500 hover:text-yellow-400 rounded-lg py-2 px-4 font-semibold transition-all shadow-sm">
             <Bug className="w-4 h-4" />
             <span>Reportar Bug</span>
          </a>
        </div>
        <p className="text-gray-400 mb-8 max-w-3xl text-sm sm:text-base">
          Nesta área, você encontra calculadoras e painéis para o Football Manager 26, lendo exportações massivas de dados diretamente de dentro do seu jogo usando o nosso plugin <span className="font-bold text-white">FM26PlayerExport</span>.
        </p>
        
        {/* Tabs */}
        <div className="flex space-x-4 border-b border-gray-700/50 mb-8 overflow-x-auto">
          <button 
            onClick={() => setActiveTab('staff')}
            className={`pb-4 px-2 font-medium transition-colors border-b-2 flex-shrink-0 ${activeTab === 'staff' ? 'border-accent text-white' : 'border-transparent text-gray-500 hover:text-gray-300'}`}
          >
            Análise de Equipe Técnica (Staff)
          </button>
          <button 
            onClick={() => setActiveTab('moneyball')}
            className={`pb-4 px-2 font-medium transition-colors border-b-2 flex-shrink-0 ${activeTab === 'moneyball' ? 'border-accent text-white' : 'border-transparent text-gray-500 hover:text-gray-300'}`}
          >
            Moneyball
          </button>
        </div>

        {/* Tab Content */}
        <div className="bg-[#1a1c22] rounded-xl p-4 md:p-6 border border-white/5 shadow-2xl relative overflow-hidden">
          {activeTab === 'staff' && <StaffAnalyzer />}
          {activeTab === 'moneyball' && (
             <div className="animation-fade-in flex flex-col w-full h-full">
                <MoneyballAnalyzer />
             </div>
          )}
        </div>
      </div>
    </div>
  );
}

export default AnaliseDados;
