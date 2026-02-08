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
},
  (error) => {
    return Promise.reject(error);
  }
);

// Válasz interceptor: (Opcionális, de hasznos)
// Itt lehetne globálisan kezelni, ha lejár a token (401 hiba)
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response && error.response.status === 401) {
      // Ha lejárt a token, kiléptethetjük a felhasználót vagy törölhetjük a tokent
      // localStorage.removeItem('salon_token');
      // window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default apiClient;
