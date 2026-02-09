<script setup>
  import { ref, onMounted, inject, watch, computed } from 'vue';
  import api from '@/services/api';
  import { DEFAULT_COMPANY_ID } from '@/config';
  import { useAutoSaveQueue } from '@/composables/useAutoSaveQueue';
  import draggable from 'vuedraggable';

  // --- STATE ---
  const images = ref([]);
  const groupedImages = ref([]); // Kategóriákba rendezett struktúra
  const isLoading = ref(false);
  const isUploading = ref(false);
  const isLoggedIn = ref(false);
  const uploadProgress = ref(0); // Opcionális: ha később progress bar-t akarunk

  // Preview / Lightbox
  const showPreview = ref(false);
  const previewImage = ref(null);

  // Injectek
  const company = inject('company', ref(null));
  const { addToQueue } = useAutoSaveQueue();

  // --- ADATLEKÉRÉS ÉS TRANSZFORMÁCIÓ ---

  const fetchData = async () => {
    const targetCompanyId = company?.value?.id || DEFAULT_COMPANY_ID;
    isLoading.value = true;
    try {
      const res = await api.get('/api/Gallery', { params: { companyId: targetCompanyId } });
      images.value = res.data;
      buildNestedStructure();
    } catch (error) {
      console.error("Hiba a betöltéskor:", error);
    } finally {
      isLoading.value = false;
    }
  };

  // A lapos listát (images) átalakítjuk csoportokká a drag-and-drop-hoz
  const buildNestedStructure = () => {
    const groups = {};

    // 1. Csoportosítás
    images.value.forEach(img => {
      const cat = img.category || "Egyéb";
      if (!groups[cat]) {
        groups[cat] = {
          name: cat,
          items: []
        };
      }
      groups[cat].items.push(img);
    });

    // 2. Objektumból tömb + Belső rendezés OrderIndex szerint
    groupedImages.value = Object.values(groups).map(group => {
      group.items.sort((a, b) => (a.orderIndex || 0) - (b.orderIndex || 0));
      return group;
    });

    // 3. Opcionális: Kategóriák sorrendje (itt most ABC, de lehetne más is)
    groupedImages.value.sort((a, b) => a.name.localeCompare(b.name));
  };

  watch(() => company?.value?.id, (newId) => { if (newId) fetchData(); }, { immediate: true });

  // --- DRAG AND DROP & MENTÉS ---

  const onDragChange = (event, groupName) => {
    // Ha változott a sorrend vagy kategóriát váltott egy elem
    if (event.added || event.moved) {
      reorderGroup(groupName);
    }
  };

  const reorderGroup = async (groupName) => {
    const group = groupedImages.value.find(g => g.name === groupName);
    if (!group) return;

    // Végigiterálunk az új sorrenden
    group.items.forEach((img, index) => {
      // Csak akkor mentünk, ha változott a sorrend vagy a kategória
      // (A backend update-hez beállítjuk az új értékeket)
      const newOrder = index + 1;
      const newCategory = groupName; // Ha át lett húzva máshonnan, ez az új kategória

      if (img.orderIndex !== newOrder || img.category !== newCategory) {
        img.orderIndex = newOrder;
        img.category = newCategory;
        saveImage(img);
      }
    });
  };

  const saveImage = (img) => {
    // A Composable kezeli a sorbaállítást (ne írják felül egymást a hívások)
    addToQueue(img.id, async () => {
      const payload = {
        id: img.id,
        title: img.title,
        categoryName: img.category,
        orderIndex: img.orderIndex
      };
      await api.put(`/api/Gallery/${img.id}`, payload);
    });
  };

  // --- FELTÖLTÉS (BULK) ---

  const fileInputRef = ref(null);

  const triggerUpload = () => {
    fileInputRef.value.click();
  };

  const handleFiles = async (event) => {
    const files = event.target.files;
    if (!files || files.length === 0) return;
    if (!isLoggedIn.value) return alert("Jelentkezz be a feltöltéshez!");

    isUploading.value = true;

    // Kiválasztjuk az első kategóriát defaultnak, vagy "Új képek"
    let targetCategory = groupedImages.value.length > 0 ? groupedImages.value[0].name : "Új képek";

    // Párhuzamos feltöltés helyett sorban (sequential), hogy a szervert ne terheljük túl
    // és a sorrend megmaradjon.
    for (let i = 0; i < files.length; i++) {
      const file = files[i];
      const formData = new FormData();
      formData.append('file', file);
      formData.append('category', targetCategory);

      try {
        const res = await api.post('/api/Gallery', formData, { headers: { 'Content-Type': undefined } });
        // Azonnal hozzáadjuk a helyi listához, hogy látszódjon
        const newImg = res.data;
        images.value.push(newImg);
      } catch (err) {
        console.error(`Hiba a ${file.name} feltöltésekor:`, err);
      }
    }

    // Újraépítjük a struktúrát és takarítunk
    buildNestedStructure();
    isUploading.value = false;
    if (fileInputRef.value) fileInputRef.value.value = "";
  };

  // --- TÖRLÉS & EGYÉB ---

  const deleteImage = async (id) => {
    if (!confirm("Biztosan törölni szeretnéd?")) return;
    try {
      // UI frissítés azonnal (optimista)
      images.value = images.value.filter(i => i.id !== id);
      buildNestedStructure();

      await api.delete(`/api/Gallery/${id}`);
    } catch (error) {
      console.error(error);
      fetchData(); // Ha hiba volt, töltsük vissza az eredetit
    }
  };

  const openPreview = (img) => {
    previewImage.value = img;
    showPreview.value = true;
  };

  onMounted(() => {
    const token = localStorage.getItem('salon_token');
    isLoggedIn.value = !!token;
  });
</script>

<template>
  <div class="gallery-container">
    <div class="header-actions">
      <h1>Galéria & Munkáim</h1>

      <div v-if="isLoggedIn" class="upload-wrapper">
        <input type="file" ref="fileInputRef" @change="handleFiles" multiple accept="image/*" style="display: none;" />
        <button @click="triggerUpload" :disabled="isUploading" class="upload-btn">
          <i class="pi pi-cloud-upload"></i>
          {{ isUploading ? 'Feltöltés folyamatban...' : 'Új képek feltöltése' }}
        </button>
      </div>
    </div>

    <div v-if="isLoading" class="loading"><i class="pi pi-spin pi-spinner"></i> Betöltés...</div>

    <div v-else class="groups-container">

      <div v-for="group in groupedImages" :key="group.name" class="category-section">
        <h3 class="cat-title">{{ group.name }} <span class="count">({{ group.items.length }})</span></h3>

        <draggable v-model="group.items"
                   group="gallery-images"
                   item-key="id"
                   class="image-grid"
                   @change="(e) => onDragChange(e, group.name)"
                   :disabled="!isLoggedIn">

          <template #item="{ element: img }">
            <div class="gallery-card">

              <div class="img-wrapper" @click="openPreview(img)">
                <img :src="img.imageUrl" loading="lazy" />
                <div class="overlay"><i class="pi pi-eye"></i></div>
              </div>

              <div v-if="isLoggedIn" class="edit-bar">
                <input v-model="img.title"
                       @change="saveImage(img)"
                       placeholder="Képaláírás..."
                       class="caption-input" />

                <button @click="deleteImage(img.id)" class="del-btn" title="Törlés">
                  <i class="pi pi-trash"></i>
                </button>
              </div>

              <div v-else-if="img.title" class="caption-display">
                {{ img.title }}
              </div>

            </div>
          </template>
        </draggable>
      </div>

      <div v-if="images.length === 0 && !isLoading" class="empty-state">
        Még nincsenek feltöltött képek.
      </div>
    </div>

    <div v-if="showPreview" class="lightbox" @click="showPreview = false">
      <div class="lightbox-content" @click.stop>
        <img :src="previewImage?.imageUrl" />
        <p v-if="previewImage?.title">{{ previewImage.title }}</p>
        <button class="close-btn" @click="showPreview = false">×</button>
      </div>
    </div>

  </div>
</template>

<style scoped>
  .gallery-container {
    max-width: 1200px;
    margin: 0 auto;
    padding: 2rem;
    color: #fff;
  }

  .header-actions {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 2rem;
    flex-wrap: wrap;
    gap: 1rem;
  }

  h1 {
    color: var(--primary-color, #d4af37);
    margin: 0;
  }

  .upload-btn {
    background-color: var(--primary-color, #d4af37);
    color: #000;
    border: none;
    padding: 10px 20px;
    border-radius: 4px;
    cursor: pointer;
    font-weight: bold;
    display: flex;
    align-items: center;
    gap: 8px;
    transition: transform 0.2s;
  }

    .upload-btn:hover {
      transform: scale(1.05);
      background-color: #b5952f;
    }

  .category-section {
    margin-bottom: 3rem;
  }

  .cat-title {
    border-bottom: 1px solid #333;
    padding-bottom: 10px;
    margin-bottom: 15px;
    color: #ddd;
    text-transform: uppercase;
    letter-spacing: 1px;
  }

  .count {
    font-size: 0.8rem;
    color: #666;
  }

  .image-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
    gap: 1.5rem;
    min-height: 100px; /* Hogy üresen is lehessen bele húzni */
  }

  .gallery-card {
    background: #1a1a1a;
    border-radius: 8px;
    overflow: hidden;
    transition: box-shadow 0.3s;
    display: flex;
    flex-direction: column;
  }

    .gallery-card:hover {
      box-shadow: 0 5px 15px rgba(0,0,0,0.5);
    }

  .img-wrapper {
    position: relative;
    aspect-ratio: 1;
    cursor: pointer;
  }

    .img-wrapper img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }

  .overlay {
    position: absolute;
    inset: 0;
    background: rgba(0,0,0,0.4);
    display: flex;
    align-items: center;
    justify-content: center;
    opacity: 0;
    transition: opacity 0.2s;
    color: #fff;
    font-size: 2rem;
  }

  .img-wrapper:hover .overlay {
    opacity: 1;
  }

  .edit-bar {
    padding: 10px;
    display: flex;
    gap: 5px;
    background: #222;
  }

  .caption-input {
    flex-grow: 1;
    background: #333;
    border: none;
    color: #fff;
    padding: 5px;
    border-radius: 3px;
    font-size: 0.9rem;
  }

  .del-btn {
    background: #ff4444;
    color: white;
    border: none;
    border-radius: 3px;
    cursor: pointer;
    width: 30px;
  }

  .caption-display {
    padding: 10px;
    text-align: center;
    font-size: 0.9rem;
    color: #aaa;
  }

  /* Lightbox */
  .lightbox {
    position: fixed;
    inset: 0;
    background: rgba(0,0,0,0.9);
    z-index: 1000;
    display: flex;
    justify-content: center;
    align-items: center;
  }

  .lightbox-content {
    position: relative;
    max-width: 90vw;
    max-height: 90vh;
    text-align: center;
  }

    .lightbox-content img {
      max-width: 100%;
      max-height: 85vh;
      box-shadow: 0 0 20px rgba(0,0,0,0.8);
    }

    .lightbox-content p {
      color: #fff;
      margin-top: 10px;
      font-size: 1.2rem;
    }

  .close-btn {
    position: absolute;
    top: -40px;
    right: -40px;
    background: none;
    border: none;
    color: #fff;
    font-size: 3rem;
    cursor: pointer;
  }
</style>
