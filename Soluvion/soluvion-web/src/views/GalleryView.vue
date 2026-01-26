<script setup>
  import { ref, onMounted } from 'vue';

  const images = ref([]);
  const selectedFile = ref(null);
  const category = ref('Hajvágás');
  const isLoading = ref(false);
  const isLoggedIn = ref(false); // ÚJ: Itt tároljuk, hogy be vagy-e lépve

  // Képek betöltése
  const fetchImages = async () => {
    try {
      const res = await fetch('https://localhost:7113/api/Gallery'); // Ellenőrizd a portot!
      if (res.ok) {
        images.value = await res.json();
      }
    } catch (error) {
      console.error("Hiba a képek betöltésekor:", error);
    }
  };

  // Fájl kiválasztása
  const handleFileChange = (event) => {
    selectedFile.value = event.target.files[0];
  };

  // Kép feltöltése (Csak ha be vagy lépve)
  const uploadImage = async () => {
    if (!selectedFile.value) return;
    isLoading.value = true;

    const formData = new FormData();
    formData.append('file', selectedFile.value);
    formData.append('category', category.value);
    // Itt a jövőben majd elküldjük a CompanyId-t is automatikusan a Token segítségével

    try {
      // A token csatolása a kéréshez (hogy a backend tudja, ki vagy)
      const token = localStorage.getItem('salon_token');

      const res = await fetch('https://localhost:7113/api/Gallery', { // Ellenőrizd a portot!
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}` // "Itt a belépőkártyám!"
        },
        body: formData
      });

      if (res.ok) {
        selectedFile.value = null;
        // Input mező törlése
        document.getElementById('fileInput').value = "";
        await fetchImages(); // Lista frissítése
      } else {
        alert("Hiba a feltöltésnél! (Lehet, hogy lejárt a belépésed?)");
      }
    } catch (error) {
      console.error("Hiba:", error);
      alert("Nem sikerült csatlakozni a szerverhez.");
    } finally {
      isLoading.value = false;
    }
  };

  // Kép törlése (Csak ha be vagy lépve)
  const deleteImage = async (id) => {
    if (!confirm("Biztosan törölni szeretnéd ezt a képet?")) return;

    try {
      const token = localStorage.getItem('salon_token');

      const res = await fetch(`https://localhost:7113/api/Gallery/${id}`, { // Ellenőrizd a portot!
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${token}` // "Itt a belépőkártyám, jogom van törölni!"
        }
      });

      if (res.ok) {
        await fetchImages();
      } else {
        alert("Hiba a törlésnél!");
      }
    } catch (error) {
      console.error("Hiba:", error);
    }
  };

  onMounted(() => {
    // 1. Megnézzük, van-e token a "zsebben"
    const token = localStorage.getItem('salon_token');
    isLoggedIn.value = !!token; // Ha van szöveg, akkor true, ha nincs, akkor false

    fetchImages();
  });
</script>

<template>
  <div class="gallery-container">
    <h1>Munkáink</h1>
    <p class="intro">Tekintse meg legújabb frizuráinkat és stílusainkat.</p>

    <div class="upload-section" v-if="isLoggedIn">
      <h3>Új kép feltöltése (Admin)</h3>
      <div class="upload-controls">
        <input type="file" @change="handleFileChange" id="fileInput" accept="image/*" />
        <select v-model="category">
          <option>Hajvágás</option>
          <option>Festés</option>
          <option>Esküvői frizura</option>
        </select>
        <button @click="uploadImage" :disabled="isLoading || !selectedFile">
          {{ isLoading ? 'Feltöltés...' : 'Feltöltés' }}
        </button>
      </div>
    </div>

    <div class="gallery-grid">
      <div v-for="image in images" :key="image.id" class="gallery-item">
        <img :src="image.imageUrl" :alt="image.category" loading="lazy" />
        <div class="overlay">
          <span>{{ image.category }}</span>
          <button v-if="isLoggedIn" @click="deleteImage(image.id)" class="delete-btn">Törlés</button>
        </div>
      </div>
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
    color: #333;
    margin-bottom: 1rem;
  }

  .intro {
    color: #666;
    margin-bottom: 2rem;
  }

  /* Feltöltő doboz stílusa */
  .upload-section {
    background: #f8f9fa;
    padding: 1.5rem;
    border-radius: 8px;
    border: 1px dashed #d4af37;
    margin-bottom: 2rem;
    display: inline-block;
    min-width: 300px;
  }

    .upload-section h3 {
      margin-top: 0;
      color: #d4af37;
    }

  .upload-controls {
    display: flex;
    gap: 10px;
    justify-content: center;
    align-items: center;
    flex-wrap: wrap;
  }

    .upload-controls button {
      background-color: #d4af37;
      color: white;
      border: none;
      padding: 8px 16px;
      border-radius: 4px;
      cursor: pointer;
    }

      .upload-controls button:disabled {
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
    box-shadow: 0 4px 6px rgba(0,0,0,0.1);
    aspect-ratio: 1;
    group: hover;
  }

    .gallery-item img {
      width: 100%;
      height: 100%;
      object-fit: cover;
      transition: transform 0.3s ease;
    }

    .gallery-item:hover img {
      transform: scale(1.05);
    }

  .overlay {
    position: absolute;
    bottom: 0;
    left: 0;
    right: 0;
    background: rgba(0, 0, 0, 0.6);
    color: white;
    padding: 10px;
    transform: translateY(100%);
    transition: transform 0.3s ease;
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  .gallery-item:hover .overlay {
    transform: translateY(0);
  }

  .delete-btn {
    background-color: #ff4444;
    color: white;
    border: none;
    padding: 4px 8px;
    border-radius: 4px;
    cursor: pointer;
    font-size: 0.8rem;
  }

    .delete-btn:hover {
      background-color: #cc0000;
    }
</style>
