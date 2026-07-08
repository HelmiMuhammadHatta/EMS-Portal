import axios from 'axios';
import { useAuthStore } from '../store/authStore';

export const api = axios.create({
  baseURL: 'http://localhost:5000/api/v1',
  headers: { 'Content-Type': 'application/json' }
});

api.interceptors.request.use(
  (config) => {
    const token = useAuthStore.getState().accessToken;
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    if (error.response?.status === 401 && !originalRequest._retry && originalRequest.url !== '/Auth/login') {
      originalRequest._retry = true;
      try {
        const authState = useAuthStore.getState();
        if (!authState.refreshToken) {
            authState.clearAuth();
            window.location.href = '/';
            return Promise.reject(error);
        }
        
        const res = await axios.post('http://localhost:5000/api/v1/Auth/refresh-token', {
          accessToken: authState.accessToken,
          refreshToken: authState.refreshToken
        });
        
        const { accessToken, refreshToken: newRefresh } = res.data.data;
        
        const base64Url = accessToken.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));
        const payload = JSON.parse(jsonPayload);

        const user = {
            id: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'],
            email: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'],
            employeeId: payload['employeeId'],
            role: payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'],
            permissions: typeof payload.permissions === 'string' ? [payload.permissions] : (payload.permissions || [])
        };
        
        authState.setAuth(accessToken, newRefresh, user);
        originalRequest.headers.Authorization = `Bearer ${accessToken}`;
        return axios(originalRequest);
      } catch (refreshError) {
        useAuthStore.getState().clearAuth();
        window.location.href = '/';
        return Promise.reject(refreshError);
      }
    }
    return Promise.reject(error);
  }
);
