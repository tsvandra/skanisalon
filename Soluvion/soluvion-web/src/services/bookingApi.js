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
