import api from './api';

export default {
  // Publikus szolgáltatás lista lekérése
  getPublicServices() {
    return api.get('/api/Service');
    // Később ezt is érdemes lehet egy PublicService kontrollerbe mozgatni, de egyelőre jó így
  },

  // Publikus foglalás beküldése
  createGuestBooking(bookingData) {
    return api.post('/api/PublicBooking', bookingData);
  }
};
