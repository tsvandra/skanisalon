<script setup>
  import { ref, onMounted, computed, inject, watch } from 'vue';
  import api from '@/services/api';
  import { DEFAULT_COMPANY_ID } from '@/config';

  const images = ref([]);
  const categories = ref([]);
  const selectedFile = ref(null);
  const isLoading = ref(false);
  const isUploading = ref(false); // Külön state a gombnak
  const isLoggedIn = ref(false);

  // INJECT: Az aktuális cég (Admin vagy Demo)
  const company = inject('company', ref(null));

  // Kategória kezelés
  const selectedCategory = ref('');
  const customCategory = ref('');
  const isNewCategoryMode = computed(() => selectedCategory.value === 'NEW');

  // Adatok betöltése
  const fetchData = async () => {
    // Ha még nincs betöltve a company, használjuk a defaultot
    const targetCompanyId = company?.value?.id || DEFAULT_COMPANY_ID;

    isLoading.value = true;
    try {
      // 1. Képek lekérése adott céghez
      const resImages = await api.get('/api/Gallery', {
        params: { companyId: targetCompanyId }
      });
      images.value = resImages.data;

      // 2. Kategóriák lekérése adott céghez
      const resCats = await api.get('/api/Gallery/categories', {
        params: { companyId: targetCompanyId }
      });
      categories.value = resCats.data;

      // Default select beállítás
      if (categories.value.length > 0) {
        selectedCategory.value = categories.value[0];
      } else {
        selectedCategory.value = 'NEW';
      }
    } catch (error) {
      console.error("Hiba az adatok betöltésekor:", error);
    } finally {
      isLoading.value = false;
    }
  };

  // WATCHER: Ha változik a cég (pl. URL váltás), újratöltjük
  watch(
    () => company?.value?.id,
    (newId) => {
      if (newId) fetchData();
    },
    { immediate: true }
  );

  const handleFileChange = (event) => {
    selectedFile.value = event.target.files[0];
  };

  const uploadImage = async () => {
    if (!selectedFile.value) return;

    // Admin ellenőrzés (kliens oldalon is)
    if (!isLoggedIn.value) {
      alert("Nincs jogosultságod feltölteni!");
      return;
    }

    let categoryToSend = selectedCategory.value;
    if (isNewCategoryMode.value) {
      categoryToSend = customCategory.value.trim();
      if (!categoryToSend) {
        alert("Kérlek add meg az új kategória nevét!");
        return;
      }
    }

    isUploading.value = true;
    const formData = new FormData();
    formData.append('file', selectedFile.value);
    formData.append('category', categoryToSend);

    try {
      // A backend a tokenből szedi ki a CompanyId-t, nem kell külön küldeni
      await api.post('/api/Gallery', formData, {
        headers: {
          'Content-Type': undefined
        }
      });
      // Siker esetén takarítás
      selectedFile.value = null;
      customCategory.value = '';
      const fileInput = document.getElementById('fileInput');
      if (fileInput) fileInput.value = "";

      await fetchData(); // Lista frissítése
      selectedCategory.value = categoryToSend; // Maradjon a kategórián

    } catch (error) {
      console.error(error);
      alert("Hiba a feltöltésnél! Ellenőrizd, hogy be vagy-e jelentkezve.");
    } finally {
      isUploading.value = false;
    }
  };

  const deleteImage = async (id) => {
    if (!confirm("Biztosan törölni szeretnéd?")) return;
    try {
      await api.delete(`/api/Gallery/${id}`);
      await fetchData();
    } catch (error) { console.error(error); }
  };

  onMounted(() => {
    const token = localStorage.getItem('salon_token');
    isLoggedIn.value = !!token;
  });
</script>

<template>
  <div class="gallery-container">
    <h1>Munkáim</h1>
    <p class="intro">Tekintsd meg a legújabb frizuráimat és stílusaimat.</p>

    <div class="upload-section" v-if="isLoggedIn">
      <h3>Új kép feltöltése</h3>
      <div class="upload-controls">

        <input type="file" @change="handleFileChange" id="fileInput" accept="image/*" class="file-input" />

        <div class="category-wrapper">
          <select v-model="selectedCategory" class="cat-select">
            <option v-for="cat in categories" :key="cat" :value="cat">{{ cat }}</option>
            <option value="NEW" style="font-weight: bold; color: var(--primary-color);">+ Új kategória...</option>
          </select>

          <input v-if="isNewCategoryMode"
                 v-model="customCategory"
                 type="text"
                 placeholder="Írd be az új nevet..."
                 class="custom-cat-input" />
        </div>

        <button @click="uploadImage" :disabled="isUploading || !selectedFile" class="upload-btn">
          {{ isUploading ? 'Feltöltés...' : 'Feltöltés' }}
        </button>
      </div>
    </div>

    <div v-if="isLoading && images.length === 0" class="loading-state">
      <i class="pi pi-spin pi-spinner" style="font-size: 2rem"></i>
    </div>

    <div class="gallery-grid" v-else>
      <div v-for="image in images" :key="image.id" class="gallery-item">
        <img :src="image.imageUrl" :alt="image.category" loading="lazy" />
        <div class="overlay">
          <span class="cat-badge">{{ image.category }}</span>
          <button v-if="isLoggedIn" @click="deleteImage(image.id)" class="delete-btn" title="Törlés">
            <i class="pi pi-trash"></i>
          </button>
        </div>
      </div>
    </div>

    <div v-if="!isLoading && images.length === 0" class="empty-state">
      Még nincsenek feltöltött képek ebben a galériában.
    </div>
  </div>
</template>

<style scoped>
  /* A stílusokat megtartottam, mert azok jók voltak */
  .gallery-container {
    padding: 2rem;
    text-align: center;
    max-width: 1200px;
    margin: 0 auto;
  }

  h1 {
    font-size: 2.5rem;
    color: var(--primary-color);
    margin-bottom: 1rem;
  }

  .intro {
    color: #888;
    margin-bottom: 2rem;
  }

  /* Feltöltő doboz */
  .upload-section {
    background: #f8f9fa;
    padding: 1.5rem;
    border-radius: 8px;
    border: 1px dashed var(--primary-color);
    margin-bottom: 2rem;
    display: inline-block;
    min-width: 300px;
    max-width: 100%;
  }

    .upload-section h3 {
      margin-top: 0;
      color: var(--primary-color);
    }

  .upload-controls {
    display: flex;
    gap: 15px;
    justify-content: center;
    align-items: center;
    flex-wrap: wrap;
  }

  .category-wrapper {
    display: flex;
    gap: 5px;
    align-items: center;
    flex-wrap: wrap;
    justify-content: center;
  }

  .cat-select {
    padding: 8px;
    border-radius: 4px;
    border: 1px solid #ddd;
    min-width: 150px;
  }

  .custom-cat-input {
    padding: 8px;
    border-radius: 4px;
    border: 1px solid var(--primary-color);
    outline: none;
  }

  .upload-btn {
    background-color: var(--primary-color);
    color: var(--secondary-color);
    border: none;
    padding: 8px 16px;
    border-radius: 4px;
    cursor: pointer;
    font-weight: bold;
  }

    .upload-btn:disabled {
      background-color: #ccc;
      cursor: not-allowed;
    }

  /* Galéria rács */
  .gallery-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
    gap: 1.5rem;
  }

  .gallery-item {
    position: relative;
    overflow: hidden;
    border-radius: 8px;
    aspect-ratio: 1;
    background: #2c2c2c;
    box-shadow: 0 4px 6px rgba(0,0,0,0.1);
  }

    .gallery-item img {
      width: 100%;
      height: 100%;
      object-fit: cover;
      transition: transform 0.5s ease;
    }

    .gallery-item:hover img {
      transform: scale(1.1);
    }

  .overlay {
    position: absolute;
    bottom: 0;
    left: 0;
    right: 0;
    background: linear-gradient(to top, rgba(0,0,0,0.8), transparent);
    padding: 15px;
    transform: translateY(100%);
    transition: transform 0.3s ease;
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  .gallery-item:hover .overlay {
    transform: translateY(0);
  }

  .cat-badge {
    background-color: var(--primary-color);
    color: var(--secondary-color);
    padding: 4px 8px;
    border-radius: 4px;
    font-size: 0.8rem;
    font-weight: bold;
  }

  .delete-btn {
    background-color: #ff4444;
    color: white;
    border: none;
    padding: 6px 10px;
    border-radius: 4px;
    cursor: pointer;
  }

    .delete-btn:hover {
      background-color: #cc0000;
    }

  .empty-state {
    margin-top: 3rem;
    color: #666;
    font-style: italic;
  }

  .loading-state {
    margin-top: 3rem;
    color: var(--primary-color);
  }
</style>
