import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { authService } from '../services/apiService';
import { useAuthStore } from '../store/authStore';
import { toast } from 'sonner';

export const Login = () => {
  const [email, setEmail] = useState('admin@ems.local');
  const [password, setPassword] = useState('Admin123!');
  const navigate = useNavigate();
  const setAuth = useAuthStore(state => state.setAuth);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      const res = await authService.login({ email, password });
      const { accessToken, refreshToken } = res.data;
      
      const base64Url = accessToken.split('.')[1];
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
      const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
          return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
      }).join(''));
      const payload = JSON.parse(jsonPayload);

      const baseUser: any = {
          id: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'],
          email: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'],
          employeeId: payload['employeeId'],
          role: payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'],
          permissions: typeof payload.permissions === 'string' ? [payload.permissions] : (payload.permissions || [])
      };

      // Set auth first so that axios interceptor can use the token for getMe
      setAuth(accessToken, refreshToken, baseUser);

      try {
        const meRes = await authService.getMe();
        if (meRes.data && meRes.data.fullName) {
          baseUser.fullName = meRes.data.fullName;
          // Update auth with the full name
          setAuth(accessToken, refreshToken, baseUser);
        }
      } catch (e) {
        console.error("Failed to fetch user profile", e);
      }

      toast.success("Login successful!");
      navigate('/dashboard');
    } catch (err: any) {
      toast.error(err.response?.data?.errors?.[0] || "Login failed");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex h-screen items-center justify-center bg-slate-50 p-4 font-sans relative overflow-hidden">
      {/* Decorative background elements */}
      <div className="absolute top-[-10%] left-[-10%] w-[40%] h-[40%] rounded-full bg-blue-100/50 blur-3xl pointer-events-none"></div>
      <div className="absolute bottom-[-10%] right-[-10%] w-[40%] h-[40%] rounded-full bg-indigo-100/50 blur-3xl pointer-events-none"></div>

      <div className="w-full max-w-5xl bg-white rounded-xl shadow-[0_20px_50px_rgba(8,_112,_184,_0.07)] border border-slate-100 overflow-hidden relative z-10 flex flex-col md:flex-row">
        
        {/* Left Panel: Branding & Illustration */}
        <div className="hidden md:flex md:w-[60%] bg-gradient-to-br from-blue-700 via-indigo-700 to-purple-800 p-12 flex-col justify-between items-center text-white relative overflow-hidden">
          {/* subtle pattern overlay */}
          <div className="absolute inset-0 bg-[url('data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSI0IiBoZWlnaHQ9IjQiPgo8cmVjdCB3aWR0aD0iNCIgaGVpZ2h0PSI0IiBmaWxsPSIjZmZmIiBmaWxsLW9wYWNpdHk9IjAuMDUiLz4KPC9zdmc+')] opacity-20"></div>
          
          <div className="w-full flex justify-start relative z-10">
             <div className="w-10 h-10 bg-white/20 backdrop-blur-md rounded-md flex items-center justify-center shadow-lg border border-white/30">
               <span className="text-white font-bold text-xl">E</span>
             </div>
          </div>
          
          <div className="text-center relative z-10 my-8">
            <svg className="w-64 h-64 mx-auto mb-8 opacity-90 drop-shadow-2xl" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1" strokeLinecap="round" strokeLinejoin="round">
              <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
              <circle cx="9" cy="7" r="4" />
              <path d="M22 21v-2a4 4 0 0 0-3-3.87" />
              <path d="M16 3.13a4 4 0 0 1 0 7.75" />
              <polyline points="12 11 15 14 20 9" />
            </svg>
            <h2 className="text-4xl font-extrabold mb-4 tracking-tight leading-tight">Kelola tim Anda <br/>dengan lebih mudah</h2>
            <p className="text-blue-100 text-lg font-medium max-w-md mx-auto">Tingkatkan produktivitas dan pantau performa seluruh tim dalam satu platform yang terintegrasi dan intuitif.</p>
          </div>
          
          <div className="w-full text-center relative z-10">
            <p className="text-blue-200/80 text-sm">&copy; {new Date().getFullYear()} Employee Management System</p>
          </div>
        </div>

        {/* Right Panel: Login Form */}
        <div className="w-full md:w-[40%] p-8 sm:p-12 flex flex-col justify-center bg-white">
          <div className="flex justify-center mb-8 md:hidden">
            <div className="w-12 h-12 bg-blue-600 rounded-md flex items-center justify-center shadow-lg shadow-blue-200">
              <span className="text-white font-bold text-2xl">E</span>
            </div>
          </div>
          
          <div className="text-center md:text-left mb-8">
            <h1 className="text-2xl sm:text-3xl font-bold text-slate-900 tracking-tight mb-2">Welcome back</h1>
            <p className="text-sm text-slate-500">Please sign in to your account</p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-5">
            <div>
              <label className="block text-sm font-medium mb-1.5 text-slate-700">Email Address</label>
              <input 
                type="email" 
                value={email} 
                onChange={e => setEmail(e.target.value)} 
                className="w-full px-4 py-2 bg-white border border-slate-200 rounded-md focus:ring-1 focus:ring-blue-600 focus:border-blue-600 outline-none transition-all shadow-sm text-sm" 
                placeholder="Enter your email"
                required 
              />
            </div>
            <div>
              <div className="flex justify-between items-center mb-1.5">
                <label className="block text-sm font-medium text-slate-700">Password</label>
              </div>
              <input 
                type="password" 
                value={password} 
                onChange={e => setPassword(e.target.value)} 
                className="w-full px-4 py-2 bg-white border border-slate-200 rounded-md focus:ring-1 focus:ring-blue-600 focus:border-blue-600 outline-none transition-all shadow-sm text-sm" 
                placeholder="Enter your password"
                required 
              />
              <div className="flex justify-end mt-2">
                <a href="#" className="text-xs font-medium text-blue-600 hover:text-blue-700 transition-colors">Lupa password?</a>
              </div>
            </div>
            
            <div className="pt-4">
              <button 
                type="submit" 
                disabled={loading} 
                className="w-full bg-blue-600 text-white py-2.5 rounded-md hover:bg-blue-700 shadow-sm font-medium transition-all disabled:opacity-70 disabled:hover:shadow-none flex justify-center items-center gap-2 text-sm"
              >
                {loading ? (
                  <>
                    <svg className="animate-spin -ml-1 mr-2 h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                    Signing in...
                  </>
                ) : 'Sign In'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};
