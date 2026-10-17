import api from './api';

export default {
  // Publikus szolgáltatás lista lekérése
  getPublicServices() {
    return api.get('/api/Service');
  },

  // Publikus foglalás beküldése
  createGuestBooking(bookingData) {
    return api.post('/api/PublicBooking', bookingData);
  },

  // Publikus szabad időpontok és idősávok lekérése
  getPublicAvailability(params) {
    return api.get('/api/PublicBooking/availability', { params });
  },

  // Admin: Online foglalási munkarend konfiguráció lekérése
  getScheduleConfig() {
    return api.get('/api/PublicBooking/schedule-config');
  },

  // Admin: Online foglalási munkarend konfiguráció mentése
  updateScheduleConfig(payload) {
    return api.put('/api/PublicBooking/schedule-config', payload);
  },

  // Admin: Egy adott nap (YYYY-MM-DD) egyedi nyitvatartásának / meghirdetett idősávjának mentése
  updateDayOverride(dateStr, payload) {
    return api.put(`/api/PublicBooking/day-override/${dateStr}`, payload);
  },

  // Lekéri a cég ügyfeleit
  getCustomers() {
    return api.get('/api/customers');
  },

  // Új ügyfelet hoz létre
  createCustomer(payload) {
    return api.post('/api/customers', payload);
  },

  // Ügyfél módosítása
  getCustomerById(id) { return api.get(`/api/customers/${id}`); }, getCustomerAppointments(id) { return api.get(`/api/customers/${id}/appointments`); }, updateCustomer(id, payload) {
    return api.put(`/api/customers/${id}`, payload);
  },

  // Ügyfelek összevonása (előnézet: semmit nem módosít)
  previewMergeCustomers(payload) {
    return api.post('/api/customers/merge/preview', payload);
  },

  mergeCustomers(payload) {
    return api.post('/api/customers/merge', payload);
  },

  // Ügyfél törlése
  deleteCustomer(id) {
    return api.delete(`/api/customers/${id}`);
  }
};
