import { Outlet, Link, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { LayoutDashboard, Users, Calendar, Clock, LogOut, Menu, Settings as SettingsIcon, FileText, CalendarClock, UserPlus, Briefcase } from 'lucide-react';
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

  const menuGroups = [
    {
      title: 'Operasional',
      items: [
        { path: '/dashboard', label: 'Dasbor', icon: <LayoutDashboard size={20} /> },
        ...(hasPermission('employee.read') ? [{ path: '/employees', label: 'Karyawan', icon: <Users size={20} /> }] : []),
        { path: '/leaves', label: 'Cuti', icon: <Calendar size={20} /> },
        { path: '/attendance', label: 'Kehadiran', icon: <Clock size={20} /> },
        ...(hasPermission('employee.read') ? [{ path: '/shift-schedules', label: 'Jadwal Shift', icon: <CalendarClock size={20} /> }] : []),
        { path: '/daily-reports', label: 'Laporan Harian', icon: <FileText size={20} /> },
      ]
    },
    {
      title: 'Rekrutmen',
      items: [
        { path: '/candidates', label: 'Kandidat', icon: <UserPlus size={20} /> },
        { path: '/job-openings', label: 'Lowongan', icon: <Briefcase size={20} /> },
      ]
    },
    {
      title: 'Sistem',
      items: [
        ...(user?.role === 'Admin' || user?.role === 'HR' ? [{ path: '/settings', label: 'Pengaturan', icon: <SettingsIcon size={20} /> }] : []),
      ]
    }
  ];

  return (
    <div className="flex h-screen bg-slate-50 text-slate-900 font-sans" style={{fontFamily: 'var(--ds-font-body)'}}>
      {/* Mobile Sidebar Overlay */}
      {sidebarOpen && (
        <div className="fixed inset-0 bg-slate-900/50 z-20 lg:hidden" onClick={() => setSidebarOpen(false)} />
      )}

      {/* Sidebar */}
      <aside className={`fixed lg:static inset-y-0 left-0 w-64 bg-white border-r border-slate-200 flex flex-col z-30 transition-transform duration-300 ${sidebarOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0'}`}>
        <div className="h-16 flex items-center px-6 border-b border-slate-100">
          <div className="w-8 h-8 rounded-lg flex items-center justify-center mr-3 shadow-sm text-white font-bold text-lg" style={{backgroundColor: 'var(--ds-primary-600)'}}>
            <span>E</span>
          </div>
          <span className="font-bold text-xl text-slate-800 tracking-tight" style={{fontFamily: 'var(--ds-font-heading)'}}>EMS Portal</span>
        </div>
        
        <nav className="flex-1 px-4 py-4 space-y-6 overflow-y-auto">
          {menuGroups.map((group, idx) => (
            group.items.length > 0 && (
              <div key={idx}>
                <h3 className="px-3 mb-2 text-xs font-bold text-slate-400 uppercase tracking-wider">{group.title}</h3>
                <div className="space-y-1">
                  {group.items.map((item) => {
                    const isActive = location.pathname === item.path;
                    return (
                      <Link 
                        key={item.path}
                        to={item.path} 
                        onClick={() => setSidebarOpen(false)}
                        className={`flex items-center gap-3 px-3 py-2.5 rounded-md transition-colors font-medium ${
                          isActive 
                            ? 'bg-blue-50 text-blue-600' 
                            : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900'
                        }`}
                        style={isActive ? { backgroundColor: 'var(--ds-primary-50)', color: 'var(--ds-primary-600)'} : {}}
                      >
                        <span className={isActive ? '' : 'text-slate-400'} style={isActive ? {color: 'var(--ds-primary-600)'} : {}}>{item.icon}</span>
                        {item.label}
                      </Link>
                    );
                  })}
                </div>
              </div>
            )
          ))}
        </nav>

        <div className="p-4 border-t border-slate-100">
          <div className="flex items-center gap-3 mb-4 p-3 rounded-lg bg-slate-50 border border-slate-100">
            <div className="w-10 h-10 rounded-full flex items-center justify-center text-white shrink-0" style={{backgroundColor: 'var(--ds-primary-600)'}}>
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
            Keluar
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
          <span className="font-bold text-lg text-slate-800 ml-2" style={{fontFamily: 'var(--ds-font-heading)'}}>EMS Portal</span>
        </header>

        {/* Page Content */}
        <main className="flex-1 overflow-auto" style={{backgroundColor: 'var(--ds-bg-page)'}}>
          <div className="min-h-full">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
};
