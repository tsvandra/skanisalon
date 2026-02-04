// src/services/api.js
import axios from 'axios';

// Létrehozunk egy alap kapcsolatot a környezeti változók alapján
const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL, // Ez olvassa ki a Netlify-on beállított címet
  headers: {
    'Content-Type': 'application/json',
  },
});

// Kérés elfogó (Interceptor): Minden kéréshez automatikusan hozzáadjuk a tokent, ha van
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('salon_token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export default apiClient;
