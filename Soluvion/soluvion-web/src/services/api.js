// src/services/api.js
import axios from 'axios';
import { localizedPath } from '@/router/routeSlugs';
import { getStoredToken, removeStoredToken } from '@/stores/authStore';
import { getCompanyIdFromToken } from '@/utils/jwt';

// Létrehozunk egy alap kapcsolatot a környezeti változók alapján
const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Kérés elfogó (Interceptor)
api.interceptors.request.use((config) => {
  const token = getStoredToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  // --- TENANT CONTEXT KEZELÉSE ---
  // 1. Mindig elküldjük a frontend böngésző domainjét (pl. skanisalon.sk vagy salon.soluvion.app)
  const hostname = typeof window !== 'undefined' ? window.location?.hostname : '';
  if (hostname) {
    config.headers['X-Tenant-Domain'] = hostname;
  }

  // 2. Kényszerítés az URL-ből vagy munkamenetből (sessionStorage) KIZÁRÓLAG teszt környezetben!
  // Éles domaineken (pl. skanisalon.sk) a forceTenant SZIGORÚAN TILTVA van a szivárgások ellen.
  const isDevOrTestHost = hostname === 'localhost' || hostname === '127.0.0.1' || hostname.endsWith('.netlify.app');
  let forceTenantId = null;

  if (isDevOrTestHost && typeof window !== 'undefined') {
    const urlParams = new URLSearchParams(window.location.search);
    const paramVal = urlParams.get('forceTenant');
    if (paramVal) {
      if (paramVal === '0' || paramVal === 'clear' || paramVal === 'reset') {
        sessionStorage.removeItem('soluvion_force_tenant');
      } else {
        sessionStorage.setItem('soluvion_force_tenant', paramVal);
        forceTenantId = paramVal;
      }
    } else {
      forceTenantId = sessionStorage.getItem('soluvion_force_tenant');
    }
  }

  // 3. Bejelentkezett felhasználó cége a tokenből
  const tokenCompanyId = getCompanyIdFromToken();

  // 4. .env alapértelmezett fejlesztői cég ID (kizárólag localhost / teszt környezethez, ha nincs aktív teszt tenant)
  const defaultEnvId = isDevOrTestHost ? import.meta.env.VITE_DEFAULT_COMPANY_ID : null;

  const resolvedTenantId = forceTenantId || tokenCompanyId || defaultEnvId;

  if (resolvedTenantId) {
    config.headers['X-Tenant-ID'] = resolvedTenantId;
    config.headers['X-Company-Id'] = resolvedTenantId;
  }
  // -------------------------------

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
      removeStoredToken();

      const currentPath = window.location.pathname + window.location.search;
      const loginPath = localizedPath('login');

      if (window.location.pathname !== loginPath) {
        window.location.href = `${loginPath}?redirect=${encodeURIComponent(currentPath)}`; // Visszairányítjuk a bejelentkezéshez
      } else {
        window.location.href = loginPath; // Ha már a login oldalon vagyunk, csak frissítjük
      }
    }
    // ----------------------------------------------------
    return Promise.reject(error);
  }
);

export default api;
