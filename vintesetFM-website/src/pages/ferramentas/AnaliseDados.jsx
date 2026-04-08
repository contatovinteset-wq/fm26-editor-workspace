import React, { useState } from 'react';
import StaffAnalyzer from '../../components/ferramentas/StaffAnalyzer';

function AnaliseDados() {
  const [activeTab, setActiveTab] = useState('staff');

  return (
    <div className="bg-bgDark min-h-screen pt-24 pb-12 font-outfit text-white">
      <div className="max-w-[1800px] w-[95%] mx-auto px-4 sm:px-6 lg:px-8">
        <h1 className="text-4xl font-bold mb-4 bg-clip-text text-transparent bg-gradient-to-r from-accent to-yellow-400">
          Análise de Dados <span className="text-sm border border-accent/50 px-2 py-1 ml-2 rounded-lg text-accent align-middle">BETA</span>
        </h1>
        <p className="text-gray-400 mb-8 max-w-3xl text-sm sm:text-base">
          Nesta área, você encontra calculadoras e painéis para o Football Manager 26, lendo exportações massivas de dados diretamente de dentro do seu jogo usando o nosso plugin <span className="font-bold text-white">FM26PlayerExport V5</span>.
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
            className={`py-3 px-6 text-sm font-semibold border-b-2 transition-all ${
              activeTab === 'moneyball'
                ? 'border-accent text-accent'
                : 'border-transparent text-gray-400 hover:text-gray-300 hover:border-gray-600'
            }`}
          >
            Moneyball
          </button>
        </div>

        {/* Tab Content */}
        <div className="bg-[#1a1c22] rounded-xl p-4 md:p-6 border border-white/5 shadow-2xl relative overflow-hidden">
          {activeTab === 'staff' && <StaffAnalyzer />}
          
          {activeTab === 'moneyball' && (
            <div className="flex flex-col items-center justify-center py-20 text-center relative z-10">
              <div className="w-20 h-20 mb-6 bg-gray-800 rounded-full flex items-center justify-center ring-4 ring-accent/20 shadow-[0_0_50px_rgba(255,215,0,0.2)] relative animate-pulse">
                <svg className="w-10 h-10 text-accent" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19.428 15.428a2 2 0 00-1.022-.547l-2.387-.477a6 6 0 00-3.86.517l-.318.158a6 6 0 01-3.86.517L6.05 15.21a2 2 0 00-1.806.547M8 4h8l-1 1v5.172a2 2 0 00.586 1.414l5 5c1.26 1.26.367 3.414-1.415 3.414H4.828c-1.782 0-2.674-2.154-1.414-3.414l5-5A2 2 0 009 10.172V5L8 4z" />
                </svg>
              </div>
              <h2 className="text-3xl font-bold text-white mb-2">Moneyball Master</h2>
              <p className="text-gray-400 max-w-lg mb-6">
                Estamos processando relatórios táticos complexos para criar o algoritmo Monyeball supremo que chegará exclusivamente na Fase 2 da plataforma. Prepare-se!
              </p>
              <span className="px-4 py-2 rounded-full bg-accent/10 text-accent border border-accent/20 font-bold uppercase tracking-wider text-sm shadow-lg">
                Em Construção!
              </span>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

export default AnaliseDados;
