import { Outlet, Link, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { useQuery } from '@tanstack/react-query';
import { employeeService } from '../services/apiService';
import { LayoutDashboard, Users, Calendar, Clock, LogOut, Menu, UserCircle, Settings as SettingsIcon, FileText } from 'lucide-react';
import { useState } from 'react';

export const Layout = () => {
  const { user, clearAuth, hasPermission } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [sidebarOpen, setSidebarOpen] = useState(false);

  const displayName = user?.fullName ?? user?.email?.split('@')[0] ?? 'User';

  const handleLogout = () => {
    clearAuth();
    navigate('/');
  };

  const navItems = [
    { path: '/dashboard', label: 'Dashboard', icon: <LayoutDashboard size={20} /> },
    ...(hasPermission('employee.read') ? [{ path: '/employees', label: 'Employees', icon: <Users size={20} /> }] : []),
    { path: '/leaves', label: 'Leaves', icon: <Calendar size={20} /> },
    { path: '/attendance', label: 'Attendance', icon: <Clock size={20} /> },
    { path: '/daily-reports', label: 'Daily Reports', icon: <FileText size={20} /> },
    ...(user?.role === 'Admin' || user?.role === 'HR' ? [{ path: '/settings', label: 'Settings', icon: <SettingsIcon size={20} /> }] : []),
  ];

  return (
    <div className="flex h-screen bg-slate-50 text-slate-900 font-sans">
      {/* Mobile Sidebar Overlay */}
      {sidebarOpen && (
        <div className="fixed inset-0 bg-slate-900/50 z-20 lg:hidden" onClick={() => setSidebarOpen(false)} />
      )}

      {/* Sidebar */}
      <aside className={`fixed lg:static inset-y-0 left-0 w-64 bg-white border-r border-slate-200 flex flex-col z-30 transition-transform duration-300 ${sidebarOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0'}`}>
        <div className="h-16 flex items-center px-6 border-b border-slate-100">
          <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center mr-3 shadow-sm">
            <span className="text-white font-bold text-lg">E</span>
          </div>
          <span className="font-bold text-xl text-slate-800 tracking-tight">EMS Portal</span>
        </div>
        
        <div className="px-6 py-4 text-xs font-semibold text-slate-400 uppercase tracking-wider">Main Menu</div>
        
        <nav className="flex-1 px-4 space-y-1.5 overflow-y-auto">
          {navItems.map((item) => {
            const isActive = location.pathname === item.path;
            return (
              <Link 
                key={item.path}
                to={item.path} 
                onClick={() => setSidebarOpen(false)}
                className={`flex items-center gap-3 px-3 py-3 rounded-md transition-colors ${
                  isActive 
                    ? 'bg-blue-50 text-blue-600 font-medium border-l-[3px] border-blue-600' 
                    : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900 border-l-[3px] border-transparent'
                }`}
              >
                <span className={isActive ? 'text-blue-600' : 'text-slate-400'}>{item.icon}</span>
                {item.label}
              </Link>
            );
          })}
        </nav>

        <div className="p-4 border-t border-slate-100">
          <div className="flex items-center gap-3 mb-4 p-3 rounded-lg bg-slate-50 border border-slate-100">
            <div className="w-10 h-10 rounded-full bg-blue-600 flex items-center justify-center text-white shrink-0">
              <span className="font-semibold">{displayName.charAt(0).toUpperCase()}</span>
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-semibold text-slate-900 truncate" title={displayName}>{displayName}</p>
              <p className="text-xs text-slate-500 truncate font-medium">{user?.role}</p>
            </div>
          </div>
          <button 
            onClick={handleLogout} 
            className="flex items-center justify-center w-full gap-2 py-2.5 px-4 text-sm text-slate-600 hover:text-red-600 hover:bg-red-50 rounded-md font-medium transition-colors"
          >
            <LogOut size={20} />
            Logout
          </button>
        </div>
      </aside>

      {/* Main Content Area */}
      <div className="flex-1 flex flex-col min-w-0 overflow-hidden">
        {/* Mobile Topbar */}
        <header className="lg:hidden h-16 bg-white border-b border-slate-200 flex items-center px-4 shrink-0 z-10 shadow-sm">
          <button className="text-slate-500 hover:text-slate-700 p-2 -ml-2 rounded-md" onClick={() => setSidebarOpen(true)}>
            <Menu size={24} />
          </button>
          <span className="font-bold text-lg text-slate-800 ml-2">EMS Portal</span>
        </header>

        {/* Page Content */}
        <main className="flex-1 overflow-auto bg-slate-50">
          <div className="min-h-full">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
};
