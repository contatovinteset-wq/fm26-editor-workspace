import React from 'react';
import { Outlet } from 'react-router-dom';
import Navbar from './Navbar';
import Footer from './Footer';
import FloatingDiscord from './FloatingDiscord';

const Layout = () => {
  return (
    <div className="min-h-screen relative flex flex-col font-sans selection:bg-primary selection:text-white">
      {/* Background Noise Global */}
      <div className="noise-overlay z-0"></div>
      
      {/* Content */}
      <div className="relative z-10 flex flex-col min-h-screen">
        <Navbar />
        
        {/* Main Content Area - Ocupa espaço flexível entre Nav e Footer */}
        <main className="flex-grow relative">
          <Outlet />
        </main>
        
        <Footer />
        <FloatingDiscord />
      </div>
    </div>
  );
};

export default Layout;
