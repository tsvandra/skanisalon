// src/composables/useImageUpload.js
import { ref } from 'vue';
import api from '@/services/api';

export function useImageUpload() {
  const isUploading = ref(false);
  const uploadError = ref(null);

  const uploadImage = async (file, endpoint, extraFields = {}) => {
    if (!file) return null;

    isUploading.value = true;
    uploadError.value = null;

    const formData = new FormData();
    formData.append('file', file);

    // Extra mezők hozzáfűzése (Pl. a galéria kategóriája)
    Object.entries(extraFields).forEach(([key, value]) => {
      formData.append(key, value);
    });

    console.log(`[Upload] Indítás: ${endpoint}`, file.name, extraFields);

    try {
      // JAVÍTÁS: Kifejezetten undefined-ra állítjuk a Content-Type-ot,
      // hogy felülírjuk az api.js globális 'application/json' beállítását!
      const response = await api.post(endpoint, formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
      console.log(`[Upload] Sikeres válasz:`, response.data);
      return response.data;
    } catch (err) {
      console.error('Képfeltöltési hiba:', err);
      uploadError.value = err.response?.data || 'Hiba történt a fájl feltöltése során.';
      throw err;
    } finally {
      isUploading.value = false;
    }
  };

  return {
    isUploading,
    uploadError,
    uploadImage
  };
}
