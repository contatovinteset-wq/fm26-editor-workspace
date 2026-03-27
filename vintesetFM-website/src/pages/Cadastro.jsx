import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { User, Key, Mail, ShieldCheck, Twitch } from 'lucide-react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

const GoogleIconSVG = ({ size = 24, className = "" }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg" className={className}>
    <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4"/>
    <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853"/>
    <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" fill="#FBBC05"/>
    <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335"/>
  </svg>
);

const Cadastro = () => {
  const [nickname, setNickname] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const { fetchUser } = useAuth();

  const handleRegister = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const res = await fetch('/api/auth/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ nickname, email, password })
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.error || 'Erro ao registrar.');
      await fetchUser();
      navigate('/minhaconta');
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="w-full min-h-screen flex bg-bgDark overflow-hidden text-white pt-16">
      
      {/* Lado Direito - Imagem Imersiva (Invertido da tela de Login) */}
      <div className="hidden lg:flex lg:w-1/2 relative flex-col justify-between p-12 overflow-hidden bg-black order-last">
        <div className="absolute inset-0 bg-gradient-to-l from-bgDark/40 to-bgDark z-10"></div>
        <img 
          src="/ReiDaMesaFM-Logo.jpg" 
          alt="Manager Workspace" 
          className="absolute inset-0 w-full h-full object-cover opacity-10 filter blur-md scale-110"
        />
        
        <div className="relative z-20 flex justify-end">
          <Link to="/" className="flex items-center gap-3 w-fit hover:opacity-80 transition-opacity">
            <span className="font-black text-2xl tracking-tight uppercase">Vinteset<span className="text-accent">FM</span></span>
            <img src="/vinteset_escudo.png" alt="Logo Vinteset" className="w-12 h-12" />
          </Link>
        </div>

        <div className="relative z-20 max-w-lg text-right ml-auto">
          <h1 className="text-4xl font-black uppercase tracking-tight mb-4">Crie Seu Legado</h1>
          <p className="text-lg text-gray-400">Junte-se à maior comunidade de Football Manager. Discuta táticas, baixe mods e dispute no Rei da Mesa.</p>
        </div>
      </div>

      {/* Lado Esquerdo - Formulário */}
      <div className="w-full lg:w-1/2 flex items-center justify-center p-6 sm:p-12 relative order-first">
        <div className="w-full max-w-md space-y-8">
           
           <div className="text-center lg:text-left">
             <h2 className="text-3xl font-black uppercase tracking-tight">Criar Conta</h2>
             <p className="text-sm text-gray-400 mt-2">Torne-se um Manager registrado gratuitamente.</p>
           </div>

           <div className="flex flex-col sm:flex-row gap-3">
             <a href="/api/auth/google" className="flex-1 flex items-center justify-center gap-2 bg-white hover:bg-gray-100 text-black font-bold py-3 px-4 rounded-xl transition-all shadow-lg hover:shadow-white/10">
               <GoogleIconSVG size={18} /> Google
             </a>
             <a href="/api/auth/twitch" className="flex-1 flex items-center justify-center gap-2 bg-[#9146FF] hover:bg-[#772CE8] text-white font-bold py-3 px-4 rounded-xl transition-all shadow-lg hover:shadow-[#9146FF]/20">
               <Twitch size={18} /> Twitch
             </a>
           </div>

           <div className="flex items-center gap-4 my-6">
             <div className="h-px bg-white/10 flex-1"></div>
             <span className="text-[10px] text-gray-500 uppercase font-bold tracking-widest">CADASTRO MANUAL</span>
             <div className="h-px bg-white/10 flex-1"></div>
           </div>

           {error && (
             <div className="bg-red-500/10 border border-red-500/20 text-red-500 p-3 rounded-xl text-xs font-bold text-center">
               {error}
             </div>
           )}

           <form className="space-y-4" onSubmit={handleRegister}>
              <div>
                <label className="block text-xs font-bold text-gray-400 uppercase tracking-widest mb-2 ml-1">Nome de Manager</label>
                <div className="relative">
                  <User className="absolute left-4 top-1/2 -translate-y-1/2 text-gray-500" size={18} />
                  <input 
                    type="text" 
                    placeholder="Seu Nickname"
                    value={nickname}
                    onChange={(e) => setNickname(e.target.value)}
                    required
                    className="w-full bg-black/50 border border-white/10 rounded-xl py-3 pl-12 pr-4 text-sm text-white focus:outline-none focus:border-accent/50 transition-colors"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-bold text-gray-400 uppercase tracking-widest mb-2 ml-1">E-mail</label>
                <div className="relative">
                  <Mail className="absolute left-4 top-1/2 -translate-y-1/2 text-gray-500" size={18} />
                  <input 
                    type="email" 
                    placeholder="manager@vinteset.com"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    required
                    className="w-full bg-black/50 border border-white/10 rounded-xl py-3 pl-12 pr-4 text-sm text-white focus:outline-none focus:border-accent/50 transition-colors"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-bold text-gray-400 uppercase tracking-widest mb-2 ml-1">Senha</label>
                <div className="relative">
                  <Key className="absolute left-4 top-1/2 -translate-y-1/2 text-gray-500" size={18} />
                  <input 
                    type="password" 
                    placeholder="••••••••"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    required
                    className="w-full bg-black/50 border border-white/10 rounded-xl py-3 pl-12 pr-4 text-sm text-white focus:outline-none focus:border-accent/50 transition-colors"
                  />
                </div>
              </div>

              <motion.button 
                type="submit"
                disabled={loading}
                whileHover={{ scale: 1.02 }}
                whileTap={{ scale: 0.98 }}
                className={`w-full bg-accent hover:bg-accentHover text-black font-black uppercase tracking-widest py-4 rounded-xl shadow-[0_0_20px_rgba(255,215,0,0.2)] flex items-center justify-center gap-2 transition-colors mt-6 ${loading ? 'opacity-50 cursor-not-allowed' : ''}`}
              >
                <ShieldCheck size={20} /> {loading ? 'Aguarde...' : 'Registrar Contrato'}
              </motion.button>
           </form>

           <div className="text-center pt-4">
              <p className="text-sm text-gray-400">
                Já possui sua licença de Manager?{' '}
                <Link to="/login" className="text-accent font-bold hover:underline">
                  Faça login aqui.
                </Link>
              </p>
           </div>
        </div>
      </div>
    </div>
  );
};

export default Cadastro;
