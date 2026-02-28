// src/services/api.js
import axios from 'axios';
import { DEFAULT_COMPANY_ID } from '@/config'; // Vagy import.meta.env közvetlenül

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

  // --- TENANT CONTEXT KEZELÉSE (JAVÍTOTT) ---

  // 1. Megnézzük, van-e az URL-ben kényszerítés (fejlesztéshez: ?forceTenant=7)
  const urlParams = new URLSearchParams(window.location.search);
  const forceTenantId = urlParams.get('forceTenant');

  // 2. Megnézzük a .env fájlt (alapértelmezett fejlesztői ID)
  const defaultEnvId = import.meta.env.VITE_DEFAULT_COMPANY_ID;

  if (forceTenantId) {
    // Ha az URL-ben van, az a legerősebb
    config.headers['X-Tenant-ID'] = forceTenantId;
  } else if (defaultEnvId) {
    // Ha nincs az URL-ben, de van a .env-ben, használjuk azt (Így működik a sima localhost:5173 is)
    config.headers['X-Tenant-ID'] = defaultEnvId;
  }
  // ------------------------------------------

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
      console.warn("A munkamenet lejárt, vagy nincs jogosultság. Újra be kell jelentkezni.");
      localStorage.removeItem('salon_token'); // Töröljük a rossz tokent
      window.location.href = '/login'; // Visszairányítjuk a bejelentkezéshez
    }
    // ----------------------------------------------------
    return Promise.reject(error);
  }
);

export default api;
