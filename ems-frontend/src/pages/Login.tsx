import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { authService } from '../services/apiService';
import { useAuthStore } from '../store/authStore';
import { toast } from 'sonner';
import styles from './Login.module.css';

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

      setAuth(accessToken, refreshToken, baseUser);

      try {
        const meRes = await authService.getMe();
        if (meRes.data && meRes.data.fullName) {
          baseUser.fullName = meRes.data.fullName;
          setAuth(accessToken, refreshToken, baseUser);
        }
      } catch (e) {
        console.error("Failed to fetch user profile", e);
      }

      toast.success("Login berhasil!");
      navigate('/dashboard');
    } catch (err: any) {
      toast.error(err.response?.data?.errors?.[0] || "Login gagal");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className={styles.loginContainer}>
      <div className={styles.loginCard}>
        
        {/* Panel Kiri: Branding & Ilustrasi */}
        <div className={styles.leftPanel}>
          
          <div className={styles.logoContainer}>
             <div className={styles.logo}>
               <span>E</span>
             </div>
          </div>
          
          <div className={styles.illustration}>
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1" strokeLinecap="round" strokeLinejoin="round">
              <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
              <circle cx="9" cy="7" r="4" />
              <path d="M22 21v-2a4 4 0 0 0-3-3.87" />
              <path d="M16 3.13a4 4 0 0 1 0 7.75" />
              <polyline points="12 11 15 14 20 9" />
            </svg>
            <h2>Kelola tim Anda <br/>dengan lebih mudah</h2>
            <p>Tingkatkan produktivitas dan pantau performa seluruh tim dalam satu platform yang terintegrasi dan intuitif.</p>
          </div>
          
          <div className={styles.footer}>
            <p>&copy; {new Date().getFullYear()} Employee Management System</p>
          </div>
        </div>

        {/* Panel Kanan: Form Login */}
        <div className={styles.rightPanel}>
          <div className={styles.mobileLogo}>
            <div className={styles.mobileLogoInner}>
              <span>E</span>
            </div>
          </div>
          
          <div className={styles.header}>
            <h1>Selamat Datang</h1>
            <p>Silakan masuk ke akun Anda</p>
          </div>

          <form onSubmit={handleSubmit} className={styles.form}>
            <div className={styles.inputGroup}>
              <label className={styles.label}>Alamat Email</label>
              <input 
                type="email" 
                value={email} 
                onChange={e => setEmail(e.target.value)} 
                className={styles.input}
                placeholder="Masukkan email Anda"
                required 
              />
            </div>
            
            <div className={styles.inputGroup}>
              <div className={styles.passwordHeader}>
                <label className={styles.label}>Kata Sandi</label>
                <a href="#" className={styles.forgotPassword}>Lupa password?</a>
              </div>
              <input 
                type="password" 
                value={password} 
                onChange={e => setPassword(e.target.value)} 
                className={styles.input}
                placeholder="Masukkan kata sandi Anda"
                required 
              />
            </div>
            
            <button 
              type="submit" 
              disabled={loading} 
              className={styles.submitBtn}
            >
              {loading ? (
                <>
                  <svg className={styles.spinner} xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                    <circle opacity="0.25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                    <path opacity="0.75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                  </svg>
                  Masuk...
                </>
              ) : 'Masuk'}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
};
