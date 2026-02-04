// src/services/api.js
import axios from 'axios';

// Létrehozunk egy alap kapcsolatot a környezeti változók alapján
const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL, // Ez olvassa ki a Netlify-on beállított címet
  headers: {
    'Content-Type': 'application/json',
  },
});

export default {
  // Ez a függvény kéri el a cég adatait az ID alapján
  getCompanyDetails(id) {
    return apiClient.get(`/Company/${id}`);
  }
};
