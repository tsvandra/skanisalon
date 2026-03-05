// src/services/companyApi.js
import api from './api';

export const companyApi = {
  /**
   * Lekéri a cég nyilvános beállításait (publikus profil).
   */
  getPublicConfig() {
    return api.get('/api/Company/public-config');
  },

  // Később ide jöhetnek a további végpontok:
  // updateSettings(data) { return api.put('/api/Company/settings', data); }
};
