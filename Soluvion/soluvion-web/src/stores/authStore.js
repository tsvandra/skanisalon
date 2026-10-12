import { defineStore } from 'pinia';
import { jwtDecode } from 'jwt-decode';

export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: localStorage.getItem('salon_token') || null,
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
      const token = localStorage.getItem('salon_token');
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

      localStorage.setItem('salon_token', token);
      this.token = token;
      this.initAuth();
    },

    logout() {
      this.token = null;
      this.user = null;
      localStorage.removeItem('salon_token');
    },
  },
});
