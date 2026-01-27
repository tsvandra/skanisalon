<script setup>
  import { ref, onMounted, computed } from 'vue';

  const images = ref([]);
  const categories = ref([]); // Itt tároljuk a betöltött kategóriákat
  const selectedFile = ref(null);
  const isLoading = ref(false);
  const isLoggedIn = ref(false);

  // Kategória kezelés
  const selectedCategory = ref(''); // Amit a listából választott
  const customCategory = ref(''); // Amit kézzel írt be
  const isNewCategoryMode = computed(() => selectedCategory.value === 'NEW');

  // Backend URL
  const API_URL = 'https://localhost:7113/api/Gallery'; // Ellenőrizd a portot!

  // Adatok betöltése
  const fetchData = async () => {
    try {
      // 1. Képek lekérése
      const resImages = await fetch(API_URL);
      if (resImages.ok) images.value = await resImages.json();

      // 2. Kategóriák lekérése
      const resCats = await fetch(`${API_URL}/categories`);
      if (resCats.ok) {
        categories.value = await resCats.json();
        // Alapértelmezett választás: az első kategória, vagy ha nincs, akkor üres
        if (categories.value.length > 0) {
          selectedCategory.value = categories.value[0];
        } else {
          selectedCategory.value = 'NEW'; // Ha nincs semmi, akkor rögtön új hozzáadása
        }
      }
    } catch (error) {
      console.error("Hiba az adatok betöltésekor:", error);
    }
  };

  const handleFileChange = (event) => {
    selectedFile.value = event.target.files[0];
  };

  const uploadImage = async () => {
    if (!selectedFile.value) return;

    // Melyik kategórianevet küldjük?
    let categoryToSend = selectedCategory.value;
    if (isNewCategoryMode.value) {
      categoryToSend = customCategory.value.trim();
      if (!categoryToSend) {
        alert("Kérlek add meg az új kategória nevét!");
        return;
      }
    }

    isLoading.value = true;
    const formData = new FormData();
    formData.append('file', selectedFile.value);
    formData.append('category', categoryToSend);

    try {
      const token = localStorage.getItem('salon_token');
      const res = await fetch(API_URL, {
        method: 'POST',
        headers: { 'Authorization': `Bearer ${token}` },
        body: formData
      });

      if (res.ok) {
        // Siker!
        selectedFile.value = null;
        customCategory.value = ''; // Töröljük a beírt szöveget
        document.getElementById('fileInput').value = "";

        // Újratöltjük az egészet (így az új kategória bekerül a listába is!)
        await fetchData();

        // Visszaállunk az imént feltöltött kategóriára a listában
        selectedCategory.value = categoryToSend;

      } else {
        alert("Hiba a feltöltésnél!");
      }
    } catch (error) {
      console.error(error);
    } finally {
      isLoading.value = false;
    }
  };

  const deleteImage = async (id) => {
    if (!confirm("Biztosan törölni szeretnéd?")) return;
    try {
      const token = localStorage.getItem('salon_token');
      const res = await fetch(`${API_URL}/${id}`, {
        method: 'DELETE',
        headers: { 'Authorization': `Bearer ${token}` }
      });
      if (res.ok) await fetchData();
    } catch (error) { console.error(error); }
  };

  onMounted(() => {
    const token = localStorage.getItem('salon_token');
    isLoggedIn.value = !!token;
    fetchData();
  });
</script>

<template>
  <div class="gallery-container">
    <h1>Munkáink</h1>
    <p class="intro">Tekintse meg legújabb frizuráinkat és stílusainkat.</p>

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

        <button @click="uploadImage" :disabled="isLoading || !selectedFile" class="upload-btn">
          {{ isLoading ? 'Feltöltés...' : 'Feltöltés' }}
        </button>
      </div>
    </div>

    <div class="gallery-grid">
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

    <div v-if="images.length === 0" class="empty-state">
      Még nincsenek feltöltött képek.
    </div>
  </div>
</template>

<style scoped>
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
</style>
