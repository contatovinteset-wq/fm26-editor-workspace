import React, { Suspense } from 'react';
import { Outlet } from 'react-router-dom';
import Navbar from './Navbar';
import Footer from './Footer';
import FloatingDiscord from './FloatingDiscord';

const PageLoader = () => (
  <div className="flex items-center justify-center min-h-[60vh]">
    <div className="w-10 h-10 border-4 border-white/20 border-t-primary rounded-full animate-spin" />
  </div>
);

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
          <Suspense fallback={<PageLoader />}>
            <Outlet />
          </Suspense>
        </main>
        
        <Footer />
        <FloatingDiscord />
      </div>
    </div>
  );
};

export default Layout;
