import { defineStore } from 'pinia';
import { jwtDecode } from 'jwt-decode';

export const TOKEN_STORAGE_KEY = 'auth_token';
const LEGACY_STORAGE_KEYS = ['salon_token', 'token'];

/**
 * Lekéri a mentett JWT tokent a localStorage-ból.
 * Automatikusan átvándoroltatja a régi 'salon_token' és 'token' kulcsokat
 * az új, iparági szabványos 'auth_token' kulcsra, megőrizve a meglévő bejelentkezéseket.
 */
export const getStoredToken = () => {
  const currentToken = localStorage.getItem(TOKEN_STORAGE_KEY);
  if (currentToken) return currentToken;

  for (const legacyKey of LEGACY_STORAGE_KEYS) {
    const legacy = localStorage.getItem(legacyKey);
    if (legacy) {
      localStorage.setItem(TOKEN_STORAGE_KEY, legacy);
      localStorage.removeItem(legacyKey);
      return legacy;
    }
  }

  return null;
};

/**
 * Törli az összes auth tokent a tárolóból.
 */
export const removeStoredToken = () => {
  localStorage.removeItem(TOKEN_STORAGE_KEY);
  LEGACY_STORAGE_KEYS.forEach((k) => localStorage.removeItem(k));
};

export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: getStoredToken(),
    user: null,
  }),

  getters: {
    isAuthenticated: (state) => !!state.token,
    userRole: (state) => state.user?.role || null,
    isEmployee: (state) => ['Admin', 'Owner', 'Employee'].includes(state.user?.role),
    isAdmin: (state) => ['Admin', 'Owner'].includes(state.user?.role),
    companyId: (state) => state.user?.companyId || null,
    username: (state) => state.user?.username || null,
  },

  actions: {
    initAuth() {
      const token = getStoredToken();
      if (!token) {
        this.token = null;
        this.user = null;
        return;
      }

      try {
        const decoded = jwtDecode(token);

        // Token lejárati idő ellenőrzése
        if (decoded.exp && decoded.exp * 1000 < Date.now()) {
          console.warn('[AuthStore] A munkamenet/token lejárt, kijelentkeztetés...');
          this.logout();
          return;
        }

        const roleClaim =
          decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
          decoded.role ||
          decoded.Role;
        const nameClaim =
          decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/name'] ||
          decoded.name ||
          decoded.Username ||
          decoded.username;
        const companyId = parseInt(decoded.CompanyId || decoded.companyId || 0);

        this.token = token;
        this.user = {
          username: nameClaim || '',
          role: roleClaim || 'Customer',
          companyId: companyId || null,
        };
      } catch (err) {
        console.error('[AuthStore] Érvénytelen token dekódoláskor:', err);
        this.logout();
      }
    },

    setToken(token) {
      if (!token) {
        this.logout();
        return;
      }

      localStorage.setItem(TOKEN_STORAGE_KEY, token);
      LEGACY_STORAGE_KEYS.forEach((k) => localStorage.removeItem(k));
      this.token = token;
      this.initAuth();
    },

    logout() {
      this.token = null;
      this.user = null;
      removeStoredToken();
    },
  },
});
