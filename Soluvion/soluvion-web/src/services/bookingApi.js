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
  }
};
