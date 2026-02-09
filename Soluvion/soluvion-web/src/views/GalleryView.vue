<script setup>
  import { ref, onMounted, inject, watch } from 'vue';
  import api from '@/services/api';
  import { DEFAULT_COMPANY_ID } from '@/config';
  import { useAutoSaveQueue } from '@/composables/useAutoSaveQueue';
  import draggable from 'vuedraggable';

  // --- STATE ---
  const images = ref([]);
  const categories = ref([]); // Nyers kategória lista a szerverről
  const groupedImages = ref([]); // Összefésült szerkezet (Kategória + Képek)
  const isLoading = ref(false);
  const isUploading = ref(false);
  const isLoggedIn = ref(false);

  // Preview / Lightbox state
  const showPreview = ref(false);
  const previewImage = ref(null);

  // Injectek
  const company = inject('company', ref(null));
  const { addToQueue } = useAutoSaveQueue();

  // --- ADATLEKÉRÉS ---

  const fetchData = async () => {
    const targetCompanyId = company?.value?.id || DEFAULT_COMPANY_ID;
    isLoading.value = true;
    try {
      // Párhuzamosan lekérjük a kategóriákat és a képeket
      const [resCats, resImages] = await Promise.all([
        api.get('/api/Gallery/categories', { params: { companyId: targetCompanyId } }),
        api.get('/api/Gallery', { params: { companyId: targetCompanyId } })
      ]);

      categories.value = resCats.data;
      images.value = resImages.data;

      buildNestedStructure();
    } catch (error) {
      console.error("Hiba a betöltéskor:", error);
    } finally {
      isLoading.value = false;
    }
  };

  // Struktúra építése: Kategóriák + Hozzájuk tartozó képek
  const buildNestedStructure = () => {
    // 1. Létrehozunk egy map-et a kategóriákból
    const groupsMap = new Map();

    categories.value.forEach(cat => {
      groupsMap.set(cat.id, {
        id: cat.id,
        name: cat.name,
        items: []
      });
    });

    // 2. Besoroljuk a képeket
    const uncategorized = { id: -1, name: "Egyéb / Nem kategorizált", items: [] };

    images.value.forEach(img => {
      // A backend most már categoryId-t is küld
      if (img.categoryId && groupsMap.has(img.categoryId)) {
        groupsMap.get(img.categoryId).items.push(img);
      } else {
        // Ha olyan kategóriája van ami nincs a listában (pl. stringként jött régen), vagy null
        // Megpróbáljuk név alapján, vagy berakjuk az egyébhez
        const catByName = Array.from(groupsMap.values()).find(g => g.name === img.category);
        if (catByName) {
          catByName.items.push(img);
        } else {
          uncategorized.items.push(img);
        }
      }
    });

    // 3. Listává alakítjuk és rendezzük a képeket OrderIndex szerint
    let result = Array.from(groupsMap.values());

    result.forEach(group => {
      group.items.sort((a, b) => (a.orderIndex || 0) - (b.orderIndex || 0));
    });

    // Ha van nem kategorizált kép, hozzáadjuk a végére
    if (uncategorized.items.length > 0) {
      result.push(uncategorized);
    }

    groupedImages.value = result;
  };

  watch(() => company?.value?.id, (newId) => { if (newId) fetchData(); }, { immediate: true });

  // --- KATEGÓRIA KEZELÉS (ÚJ/ÁTNEVEZÉS/TÖRLÉS) ---

  const createCategory = async () => {
    if (!isLoggedIn.value) return;
    const name = prompt("Add meg az új galéria nevét:");
    if (!name) return;

    try {
      await api.post('/api/Gallery/categories', { name });
      await fetchData(); // Frissítjük a listát
    } catch (err) {
      console.error(err);
      alert("Hiba a létrehozáskor.");
    }
  };

  const renameCategory = async (group) => {
    if (!group.id || group.id === -1) return; // "Egyéb" nem átnevezhető

    // A UI-n a v-model már frissítette a group.name-et, most elküldjük
    addToQueue(`cat-${group.id}`, async () => {
      await api.put(`/api/Gallery/categories/${group.id}`, { name: group.name });
    });
  };

  const deleteCategory = async (group) => {
    if (!confirm(`Biztosan törölni szeretnéd a "${group.name}" galériát?`)) return;
    try {
      await api.delete(`/api/Gallery/categories/${group.id}`);
      await fetchData();
    } catch (err) {
      console.error(err);
      alert("Nem sikerült törölni. Győződj meg róla, hogy üres a galéria!");
    }
  };

  // --- DRAG AND DROP & KÉP MENTÉS ---

  const onDragChange = (event, group) => {
    if (event.added || event.moved) {
      reorderGroup(group);
    }
  };

  const reorderGroup = async (group) => {
    group.items.forEach((img, index) => {
      const newOrder = index + 1;
      // Ha kategóriát váltott, a group.name az új kategória neve
      const newCategoryName = group.name;

      if (img.orderIndex !== newOrder || img.category !== newCategoryName) {
        img.orderIndex = newOrder;
        img.category = newCategoryName;
        img.categoryId = group.id; // Lokálisan is frissítjük az ID-t
        saveImage(img);
      }
    });
  };

  const saveImage = (img) => {
    addToQueue(img.id, async () => {
      const payload = {
        id: img.id,
        title: img.title,
        categoryName: img.category, // Backend név alapján keres/vált
        orderIndex: img.orderIndex
      };
      await api.put(`/api/Gallery/${img.id}`, payload);
    });
  };

  // --- FELTÖLTÉS ---

  const fileInputRef = ref(null);

  const triggerUpload = () => {
    fileInputRef.value.click();
  };

  const handleFiles = async (event) => {
    const files = event.target.files;
    if (!files || files.length === 0) return;
    if (!isLoggedIn.value) return;

    isUploading.value = true;
    // Az első kategóriába töltjük, vagy ha nincs, akkor a legelsőbe ami létrejön
    let targetCategoryName = groupedImages.value.length > 0 ? groupedImages.value[0].name : "Új képek";

    for (let i = 0; i < files.length; i++) {
      const formData = new FormData();
      formData.append('file', files[i]);
      formData.append('category', targetCategoryName);

      try {
        await api.post('/api/Gallery', formData, { headers: { 'Content-Type': undefined } });
      } catch (err) {
        console.error(err);
      }
    }

    await fetchData(); // Újratöltjük az egészet, hogy a képek a helyükre kerüljenek
    isUploading.value = false;
    if (fileInputRef.value) fileInputRef.value.value = "";
  };

  // --- EGYÉB ---

  const deleteImage = async (id) => {
    if (!confirm("Törlöd a képet?")) return;
    try {
      images.value = images.value.filter(i => i.id !== id);
      buildNestedStructure(); // UI frissítés
      await api.delete(`/api/Gallery/${id}`);
    } catch (error) {
      fetchData();
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

      <div v-if="isLoggedIn" class="action-buttons">
        <button @click="createCategory" class="btn secondary-btn">
          <i class="pi pi-folder-plus"></i> Új Galéria
        </button>

        <input type="file" ref="fileInputRef" @change="handleFiles" multiple accept="image/*" style="display: none;" />
        <button @click="triggerUpload" :disabled="isUploading" class="btn primary-btn">
          <i v-if="isUploading" class="pi pi-spin pi-spinner"></i>
          <i v-else class="pi pi-cloud-upload"></i>
          {{ isUploading ? 'Feltöltés...' : 'Képek Feltöltése' }}
        </button>
      </div>
    </div>

    <div v-if="isLoading" class="loading-state">
      <i class="pi pi-spin pi-spinner"></i> Betöltés...
    </div>

    <div v-else class="groups-container">
      <div v-for="group in groupedImages" :key="group.id" class="category-section">

        <div class="cat-header">
          <div class="cat-title-wrapper">
            <input v-if="isLoggedIn && group.id !== -1"
                   v-model="group.name"
                   @change="renameCategory(group)"
                   class="cat-input"
                   placeholder="Galéria neve" />
            <h3 v-else class="cat-title">{{ group.name }}</h3>
            <span class="count">({{ group.items.length }})</span>
          </div>

          <button v-if="isLoggedIn && group.items.length === 0 && group.id !== -1"
                  @click="deleteCategory(group)"
                  class="cat-delete-btn"
                  title="Üres galéria törlése">
            <i class="pi pi-trash"></i>
          </button>
        </div>

        <draggable v-model="group.items"
                   group="gallery-images"
                   item-key="id"
                   class="image-grid"
                   @change="(e) => onDragChange(e, group)"
                   :disabled="!isLoggedIn"
                   ghost-class="ghost-card">
          <template #item="{ element: img }">
            <div class="gallery-card">
              <div class="img-wrapper" @click="openPreview(img)">
                <img :src="img.imageUrl" loading="lazy" :alt="img.title" />
                <div class="overlay"><i class="pi pi-eye"></i></div>
              </div>

              <div v-if="isLoggedIn" class="edit-bar">
                <input v-model="img.title" @change="saveImage(img)" placeholder="Cím..." class="caption-input" />
                <button @click="deleteImage(img.id)" class="del-btn"><i class="pi pi-trash"></i></button>
              </div>
              <div v-else-if="img.title" class="caption-display">{{ img.title }}</div>
            </div>
          </template>
        </draggable>
      </div>

      <div v-if="images.length === 0 && categories.length === 0 && !isLoading" class="empty-state">
        Még nincsenek galériák létrehozva.
      </div>
    </div>

    <div v-if="showPreview" class="lightbox" @click="showPreview = false">
      <div class="lightbox-content" @click.stop>
        <img :src="previewImage?.imageUrl" />
        <p v-if="previewImage?.title" class="lightbox-caption">{{ previewImage.title }}</p>
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
    border-bottom: 1px solid #333;
    padding-bottom: 1rem;
  }

  h1 {
    color: var(--primary-color, #d4af37);
    margin: 0;
    font-weight: 300;
    letter-spacing: 2px;
  }

  .action-buttons {
    display: flex;
    gap: 10px;
  }

  .btn {
    border: none;
    padding: 10px 16px;
    border-radius: 4px;
    cursor: pointer;
    font-weight: bold;
    display: flex;
    align-items: center;
    gap: 8px;
    transition: transform 0.2s;
  }

  .primary-btn {
    background-color: var(--primary-color, #d4af37);
    color: #000;
  }

  .secondary-btn {
    background-color: #333;
    color: #fff;
    border: 1px solid #555;
  }

  .btn:hover:not(:disabled) {
    transform: scale(1.05);
    filter: brightness(1.1);
  }

  /* Kategória Header */
  .category-section {
    margin-bottom: 3rem;
  }

  .cat-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 15px;
    border-left: 3px solid var(--primary-color, #d4af37);
    padding-left: 10px;
  }

  .cat-title-wrapper {
    display: flex;
    align-items: center;
    gap: 10px;
    flex-grow: 1;
  }

  .cat-title {
    color: #ddd;
    text-transform: uppercase;
    letter-spacing: 1px;
    font-size: 1.2rem;
    margin: 0;
  }

  .cat-input {
    background: transparent;
    border: none;
    border-bottom: 1px solid #555;
    color: var(--primary-color, #d4af37);
    font-size: 1.2rem;
    font-weight: bold;
    text-transform: uppercase;
    letter-spacing: 1px;
    padding: 5px 0;
    width: 100%;
    max-width: 300px;
  }

    .cat-input:focus {
      outline: none;
      border-bottom-color: var(--primary-color);
    }

  .count {
    font-size: 0.9rem;
    color: #666;
  }

  .cat-delete-btn {
    background: none;
    border: none;
    color: #555;
    cursor: pointer;
    padding: 5px;
    font-size: 1rem;
  }

    .cat-delete-btn:hover {
      color: #ff4444;
    }

  /* GRID ÉS THUMBNAILS FIX */
  .image-grid {
    display: grid;
    /* Reszponzív oszlopok, de minimum 200px szélesek */
    grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
    gap: 1.5rem;
    min-height: 100px;
    padding: 10px;
    background: rgba(255, 255, 255, 0.02);
    border-radius: 8px;
  }

  .gallery-card {
    background: #1a1a1a;
    border-radius: 8px;
    overflow: hidden; /* Ez fontos, hogy ne lógjon ki semmi */
    box-shadow: 0 4px 6px rgba(0,0,0,0.3);
    display: flex;
    flex-direction: column;
  }

  /* ITT A KÉP MÉRET FIX */
  .img-wrapper {
    width: 100%;
    /* Ez kényszeríti a tökéletes négyzet alakot a szélesség alapján */
    aspect-ratio: 1 / 1;
    position: relative;
    cursor: pointer;
    overflow: hidden; /* Hogy a zoomolásnál ne lógjon ki */
  }

    .img-wrapper img {
      width: 100%;
      height: 100%;
      /* Ez gondoskodik róla, hogy kitöltse a négyzetet torzítás nélkül */
      object-fit: cover;
      display: block;
      transition: transform 0.3s ease;
    }

    .img-wrapper:hover img {
      transform: scale(1.1); /* Finom zoom effekt */
    }

  .overlay {
    position: absolute;
    inset: 0;
    background: rgba(0,0,0,0.5);
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
    padding: 8px;
    background: #222;
    display: flex;
    gap: 5px;
  }

  .caption-input {
    width: 100%;
    background: #333;
    border: 1px solid transparent;
    color: #fff;
    padding: 4px;
    font-size: 0.8rem;
    border-radius: 3px;
  }

  .del-btn {
    background: #333;
    border: 1px solid #ff4444;
    color: #ff4444;
    width: 25px;
    cursor: pointer;
    border-radius: 3px;
    display: flex;
    align-items: center;
    justify-content: center;
  }

    .del-btn:hover {
      background: #ff4444;
      color: #fff;
    }

  .caption-display {
    padding: 8px;
    text-align: center;
    font-size: 0.85rem;
    color: #bbb;
    font-style: italic;
    background: #222;
  }

  /* Lightbox */
  .lightbox {
    position: fixed;
    inset: 0;
    background: rgba(0,0,0,0.95);
    z-index: 9999;
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
      max-height: 80vh;
      box-shadow: 0 0 30px rgba(0,0,0,0.5);
      border-radius: 4px;
    }

  .lightbox-caption {
    color: #fff;
    margin-top: 15px;
    font-size: 1.2rem;
  }

  .close-btn {
    position: absolute;
    top: -40px;
    right: -20px;
    background: none;
    border: none;
    color: #fff;
    font-size: 3rem;
    cursor: pointer;
  }
</style>
