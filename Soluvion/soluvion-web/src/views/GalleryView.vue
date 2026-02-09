<script setup>
  import { ref, onMounted, inject, watch, nextTick } from 'vue';
  import api from '@/services/api';
  import { DEFAULT_COMPANY_ID } from '@/config';
  import { useAutoSaveQueue } from '@/composables/useAutoSaveQueue';
  import draggable from 'vuedraggable';

  // --- STATE ---
  const images = ref([]);
  const categories = ref([]);
  const groupedImages = ref([]);
  const isLoading = ref(false);
  const isUploading = ref(false);
  const isLoggedIn = ref(false);

  // Preview state
  const showPreview = ref(false);
  const previewImage = ref(null);

  // Upload state
  const fileInputRef = ref(null);
  const currentUploadCategory = ref(null); // Melyik kategóriába töltünk éppen?

  // Focus refek
  const categoryInputRefs = ref({});

  // Injectek
  const company = inject('company', ref(null));
  const { addToQueue } = useAutoSaveQueue();

  // --- ADATLEKÉRÉS ---

  const fetchData = async () => {
    const targetCompanyId = company?.value?.id || DEFAULT_COMPANY_ID;
    isLoading.value = true;
    try {
      const [resCats, resImages] = await Promise.all([
        api.get('/api/Gallery/categories', { params: { companyId: targetCompanyId } }),
        api.get('/api/Gallery', { params: { companyId: targetCompanyId } })
      ]);

      categories.value = resCats.data; // Ezek már sorrendben jönnek a backendről (OrderIndex)
      images.value = resImages.data;
      buildNestedStructure();
    } catch (error) {
      console.error("Hiba:", error);
    } finally {
      isLoading.value = false;
    }
  };

  const buildNestedStructure = () => {
    // Map létrehozása a meglévő kategóriákból (sorrend megőrzése fontos!)
    // Mivel a categories.value már sorrendben van, iteráljunk azon végig
    const groups = [];
    const groupsMap = new Map();

    categories.value.forEach(cat => {
      const group = {
        id: cat.id,
        name: cat.name,
        items: [],
        orderIndex: cat.orderIndex // Megőrizzük az eredeti indexet
      };
      groups.push(group);
      groupsMap.set(cat.id, group);
    });

    // Képek besorolása
    const uncategorized = { id: -1, name: "Egyéb / Nem kategorizált", items: [], orderIndex: 999999 };

    images.value.forEach(img => {
      if (img.categoryId && groupsMap.has(img.categoryId)) {
        groupsMap.get(img.categoryId).items.push(img);
      } else {
        uncategorized.items.push(img);
      }
    });

    // Belső képek rendezése
    groups.forEach(g => g.items.sort((a, b) => (a.orderIndex || 0) - (b.orderIndex || 0)));

    // Ha van egyéb, a végére csapjuk
    if (uncategorized.items.length > 0) {
      groups.push(uncategorized);
    }

    groupedImages.value = groups;
  };

  watch(() => company?.value?.id, (newId) => { if (newId) fetchData(); }, { immediate: true });

  // --- KATEGÓRIA MŰVELETEK ---

  // 1. Létrehozás (Inline)
  const createCategory = async () => {
    if (!isLoggedIn.value) return;

    try {
      // Létrehozzuk a backend-en (hogy legyen ID-ja)
      const res = await api.post('/api/Gallery/categories', { name: "Új galéria" });
      const newCat = res.data;

      // Beszúrjuk a helyi lista ELEJÉRE
      const newGroup = {
        id: newCat.id,
        name: "", // Üresen hagyjuk a UI-n, hogy a placeholder látszódjon
        items: [],
        orderIndex: newCat.orderIndex
      };

      groupedImages.value.unshift(newGroup);

      // Fókuszálás az input mezőre (NextTick kell, hogy a DOM frissüljön előbb)
      await nextTick();
      const inputEl = categoryInputRefs.value[newGroup.id];
      if (inputEl) inputEl.focus();

    } catch (err) {
      console.error(err);
      alert("Hiba a létrehozáskor.");
    }
  };

  // 2. Kategória Mozgatás (Reorder)
  const onCategoryDragChange = (event) => {
    if (event.moved) {
      // Végigmegyünk az új sorrenden és frissítjük az indexeket
      groupedImages.value.forEach((group, index) => {
        if (group.id !== -1 && group.orderIndex !== index) {
          group.orderIndex = index;
          saveCategory(group);
        }
      });
    }
  };

  // 3. Mentés (Átnevezés vagy Mozgatás)
  const saveCategory = (group) => {
    if (!group.id || group.id === -1) return;

    // Ha üres a név, akkor legyen "Új galéria" a default mentésnél
    const nameToSend = group.name.trim() === "" ? "Új galéria" : group.name;

    addToQueue(`cat-${group.id}`, async () => {
      await api.put(`/api/Gallery/categories/${group.id}`, {
        name: nameToSend,
        orderIndex: group.orderIndex
      });
    });
  };

  const deleteCategory = async (group) => {
    if (!confirm(`Törlöd a "${group.name || 'Új galéria'}" mappát?`)) return;
    try {
      await api.delete(`/api/Gallery/categories/${group.id}`);
      groupedImages.value = groupedImages.value.filter(g => g.id !== group.id);
    } catch (err) {
      alert("Nem sikerült törölni. Győződj meg róla, hogy üres!");
    }
  };

  // --- KÉP MŰVELETEK (NESTED DRAG) ---

  const onImageDragChange = (event, group) => {
    if (event.added || event.moved) {
      group.items.forEach((img, index) => {
        const newOrder = index + 1;
        const newCategoryName = group.name || "Új galéria"; // Fallback név

        if (img.orderIndex !== newOrder || img.category !== newCategoryName) {
          img.orderIndex = newOrder;
          img.category = newCategoryName;
          img.categoryId = group.id;
          saveImage(img);
        }
      });
    }
  };

  const saveImage = (img) => {
    addToQueue(img.id, async () => {
      await api.put(`/api/Gallery/${img.id}`, {
        id: img.id,
        title: img.title,
        categoryName: img.category,
        orderIndex: img.orderIndex
      });
    });
  };

  const deleteImage = async (id) => {
    if (!confirm("Törlöd a képet?")) return;
    try {
      images.value = images.value.filter(i => i.id !== id);
      buildNestedStructure();
      await api.delete(`/api/Gallery/${id}`);
    } catch (error) { fetchData(); }
  };

  // --- FELTÖLTÉS (KATEGÓRIA SPECIFIKUS) ---

  const triggerCategoryUpload = (group) => {
    currentUploadCategory.value = group; // Megjegyezzük, hova kattintott
    if (fileInputRef.value) fileInputRef.value.click(); // Kattintunk a rejtett inputra
  };

  const handleFiles = async (event) => {
    const files = event.target.files;
    if (!files || files.length === 0 || !currentUploadCategory.value) return;

    isUploading.value = true;
    // A kiválasztott kategória nevét használjuk
    // Ha a név üres (épp most hoztuk létre), akkor a backend által adott "Új galéria" nevet használjuk,
    // de biztonságosabb ID alapján, viszont a feltöltő endpoint jelenleg nevet vár.
    // Javítás: Használjuk a group.name-et, ha üres, akkor "Új galéria".
    const targetCatName = currentUploadCategory.value.name || "Új galéria";

    for (let i = 0; i < files.length; i++) {
      const formData = new FormData();
      formData.append('file', files[i]);
      formData.append('category', targetCatName);

      try {
        await api.post('/api/Gallery', formData, { headers: { 'Content-Type': undefined } });
      } catch (err) { console.error(err); }
    }

    await fetchData();
    isUploading.value = false;
    currentUploadCategory.value = null;
    event.target.value = ""; // Reset input
  };

  const openPreview = (img) => { previewImage.value = img; showPreview.value = true; };

  onMounted(() => {
    const token = localStorage.getItem('salon_token');
    isLoggedIn.value = !!token;
  });
</script>

<template>
  <div class="gallery-container">
    <div class="header-actions">
      <h1>Galéria & Munkáim</h1>

      <button v-if="isLoggedIn" @click="createCategory" class="btn primary-btn big-btn">
        <i class="pi pi-plus-circle"></i> Új Galéria Létrehozása
      </button>

      <input type="file" ref="fileInputRef" @change="handleFiles" multiple accept="image/*" style="display: none;" />
    </div>

    <div v-if="isLoading" class="loading-state">
      <i class="pi pi-spin pi-spinner"></i> Betöltés...
    </div>

    <draggable v-else
               v-model="groupedImages"
               group="categories"
               item-key="id"
               handle=".drag-handle-cat"
               @change="onCategoryDragChange"
               :disabled="!isLoggedIn">
      <template #item="{ element: group }">
        <div class="category-section">

          <div class="cat-header">
            <div class="cat-left">
              <div v-if="isLoggedIn && group.id !== -1" class="drag-handle-cat" title="Galéria mozgatása">
                ⋮⋮
              </div>

              <div class="cat-title-wrapper">
                <input v-if="isLoggedIn && group.id !== -1"
                       :ref="el => categoryInputRefs[group.id] = el"
                       v-model="group.name"
                       @change="saveCategory(group)"
                       class="cat-input"
                       placeholder="Új galéria" />
                <h3 v-else class="cat-title">{{ group.name }}</h3>
                <span class="count">({{ group.items.length }})</span>
              </div>
            </div>

            <div v-if="isLoggedIn && group.id !== -1" class="cat-actions">

              <button @click="triggerCategoryUpload(group)" class="btn upload-sm-btn" :disabled="isUploading">
                <i v-if="isUploading && currentUploadCategory?.id === group.id" class="pi pi-spin pi-spinner"></i>
                <i v-else class="pi pi-cloud-upload"></i>
                Képek feltöltése ide
              </button>

              <button v-if="group.items.length === 0"
                      @click="deleteCategory(group)"
                      class="cat-delete-btn"
                      title="Galéria törlése">
                <i class="pi pi-trash"></i>
              </button>
            </div>
          </div>

          <draggable v-model="group.items"
                     group="gallery-images"
                     item-key="id"
                     class="image-grid"
                     @change="(e) => onImageDragChange(e, group)"
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
      </template>
    </draggable>

    <div v-if="!isLoading && groupedImages.length === 0" class="empty-state">
      Kattints az "Új Galéria" gombra a kezdéshez!
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
    border-bottom: 1px solid #333;
    padding-bottom: 1rem;
  }

  h1 {
    color: var(--primary-color, #d4af37);
    margin: 0;
    font-weight: 300;
    letter-spacing: 2px;
  }

  /* GOMBOK */
  .btn {
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-weight: bold;
    display: flex;
    align-items: center;
    gap: 8px;
    transition: transform 0.2s, background 0.2s;
  }

  .primary-btn {
    background-color: var(--primary-color, #d4af37);
    color: #000;
  }

  .big-btn {
    padding: 12px 24px;
    font-size: 1.1rem;
  }

  .upload-sm-btn {
    background-color: #333;
    color: #ddd;
    border: 1px solid #555;
    padding: 6px 12px;
    font-size: 0.9rem;
  }

    .upload-sm-btn:hover {
      background-color: #444;
      color: #fff;
      border-color: var(--primary-color);
    }

  .btn:hover:not(:disabled) {
    transform: scale(1.03);
  }

  /* KATEGÓRIA SZEKCIÓ */
  .category-section {
    margin-bottom: 2rem;
    background: #111;
    border-radius: 8px;
    padding: 10px;
    border: 1px solid #222;
  }

  .cat-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 15px;
    padding: 5px 10px;
    background: #1a1a1a;
    border-radius: 6px;
  }

  .cat-left {
    display: flex;
    align-items: center;
    gap: 15px;
    flex-grow: 1;
  }

  .drag-handle-cat {
    cursor: grab;
    font-size: 1.5rem;
    color: #666;
    padding: 0 5px;
  }

    .drag-handle-cat:hover {
      color: var(--primary-color);
    }

  .cat-title-wrapper {
    display: flex;
    align-items: center;
    gap: 10px;
    flex-grow: 1;
  }

  .cat-input {
    background: transparent;
    border: none;
    border-bottom: 1px dashed #555;
    color: var(--primary-color, #d4af37);
    font-size: 1.2rem;
    font-weight: bold;
    padding: 5px 0;
    width: 100%;
    max-width: 400px;
  }

    .cat-input:focus {
      outline: none;
      border-bottom: 1px solid var(--primary-color);
      background: #000;
    }

    .cat-input::placeholder {
      color: #555;
      font-style: italic;
    }

  .cat-actions {
    display: flex;
    align-items: center;
    gap: 10px;
  }

  .cat-delete-btn {
    background: none;
    border: none;
    color: #555;
    cursor: pointer;
    padding: 5px;
  }

    .cat-delete-btn:hover {
      color: #ff4444;
    }

  /* GRID & KÉPEK */
  .image-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
    gap: 1rem;
    min-height: 80px;
    padding: 10px;
    background: rgba(255, 255, 255, 0.02);
    border-radius: 6px;
  }

  .gallery-card {
    background: #222;
    border-radius: 6px;
    overflow: hidden;
    box-shadow: 0 2px 4px rgba(0,0,0,0.3);
  }

  .img-wrapper {
    width: 100%;
    aspect-ratio: 1 / 1;
    position: relative;
    cursor: pointer;
    overflow: hidden;
  }

    .img-wrapper img {
      width: 100%;
      height: 100%;
      object-fit: cover;
      transition: transform 0.3s;
    }

    .img-wrapper:hover img {
      transform: scale(1.1);
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
    font-size: 1.5rem;
  }

  .img-wrapper:hover .overlay {
    opacity: 1;
  }

  .edit-bar {
    padding: 5px;
    background: #1a1a1a;
    display: flex;
    gap: 5px;
  }

  .caption-input {
    width: 100%;
    background: #333;
    border: none;
    color: #fff;
    padding: 4px;
    font-size: 0.8rem;
    border-radius: 3px;
  }

  .del-btn {
    background: #333;
    color: #ff4444;
    border: 1px solid #ff4444;
    width: 24px;
    cursor: pointer;
    border-radius: 3px;
    display: flex;
    justify-content: center;
    align-items: center;
  }

    .del-btn:hover {
      background: #ff4444;
      color: white;
    }

  .caption-display {
    padding: 5px;
    text-align: center;
    font-size: 0.8rem;
    color: #999;
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

  .lightbox-content img {
    max-width: 95vw;
    max-height: 85vh;
    box-shadow: 0 0 30px rgba(0,0,0,0.5);
  }

  .lightbox-caption {
    color: #fff;
    margin-top: 10px;
    font-size: 1.2rem;
  }

  .close-btn {
    position: absolute;
    top: -40px;
    right: -20px;
    background: none;
    border: none;
    color: #fff;
    font-size: 2.5rem;
    cursor: pointer;
  }
</style>
