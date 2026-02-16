// src/services/api.js
import axios from 'axios';

// Létrehozunk egy alap kapcsolatot a környezeti változók alapján
const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Kérés elfogó (Interceptor)
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('salon_token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  // --- ÚJ: Tenant Context kezelése ---
  // Megnézzük, van-e az URL-ben forceTenant paraméter (fejlesztéshez)
  const urlParams = new URLSearchParams(window.location.search);
  const forceTenantId = urlParams.get('forceTenant');

  // Ha van az URL-ben, akkor azt használjuk (és minden API híváshoz hozzácsapjuk Headerként)
  if (forceTenantId) {
    config.headers['X-Tenant-ID'] = forceTenantId;
  }
  // -----------------------------------

  return config;
},
  (error) => {
    return Promise.reject(error);
  }
);

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response && error.response.status === 401) {
      // Token lejárat kezelése (opcionális: logout logika)
    }
    return Promise.reject(error);
  }
);

export default api;
