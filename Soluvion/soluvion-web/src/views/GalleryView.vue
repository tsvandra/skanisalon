<script setup>
  import { ref, onMounted, inject, watch, nextTick, computed } from 'vue';
  import api from '@/services/api';
  import { DEFAULT_COMPANY_ID } from '@/config';
  import { useAutoSaveQueue } from '@/composables/useAutoSaveQueue';
  import draggable from 'vuedraggable';
  import { useI18n } from 'vue-i18n'; // <--- ÚJ

  const { locale } = useI18n(); // <--- Globális locale
  const isLoggedIn = inject('isLoggedIn');

  // Globális nyelv
  const currentLang = computed(() => locale.value);

  const images = ref([]);
  const categories = ref([]);
  const groupedImages = ref([]);
  const isLoading = ref(false);
  const isUploading = ref(false);
  const translatingField = ref(null);
  const showPreview = ref(false);
  const previewImage = ref(null);
  const fileInputRef = ref(null);
  const currentUploadCategory = ref(null);
  const categoryInputRefs = ref({});
  const company = inject('company', ref(null));
  const { addToQueue } = useAutoSaveQueue();

  const ensureDict = (field, defaultValue = "") => {
    if (field && typeof field === 'object') return field;
    return { [currentLang.value]: field || defaultValue };
  };

  const translateField = async (obj, fieldName, targetLang) => {
    const sourceText = obj[fieldName]['hu'] || obj[fieldName][currentLang.value];
    if (!sourceText || sourceText.trim() === '') return;
    const loadingKey = `${obj.id || 'new'}-${fieldName}-${targetLang}`;
    translatingField.value = loadingKey;
    try {
      const response = await api.post('/api/Translation', { text: sourceText, targetLanguage: targetLang });
      if (response.data && response.data.translatedText) {
        obj[fieldName][targetLang] = response.data.translatedText;
        if (obj.items) saveCategory(obj); else saveImage(obj);
      }
    } catch (err) { console.error("Fordítási hiba:", err); }
    finally { translatingField.value = null; }
  };

  const triggerTranslation = (obj, fieldName) => {
    if (currentLang.value === 'hu') { alert("Válts nyelvet fentről!"); return; }
    translateField(obj, fieldName, currentLang.value);
  };

  const fetchData = async () => {
    const targetCompanyId = company?.value?.id || DEFAULT_COMPANY_ID;
    isLoading.value = true;
    try {
      const [resCats, resImages] = await Promise.all([
        api.get('/api/Gallery/categories', { params: { companyId: targetCompanyId } }),
        api.get('/api/Gallery', { params: { companyId: targetCompanyId } })
      ]);
      categories.value = resCats.data.map(c => ({ ...c, name: ensureDict(c.name, "Névtelen galéria") }));
      images.value = resImages.data.map(i => ({ ...i, category: ensureDict(i.category, "Egyéb") }));
      buildNestedStructure();
    } catch (error) { console.error("Hiba:", error); }
    finally { isLoading.value = false; }
  };

  const buildNestedStructure = () => {
    const groups = [];
    const groupsMap = new Map();
    categories.value.forEach(cat => {
      const group = { id: cat.id, name: cat.name, items: [], orderIndex: cat.orderIndex };
      groups.push(group);
      groupsMap.set(cat.id, group);
    });
    const uncategorized = {
      id: -1,
      name: { [currentLang.value]: "Egyéb / Nem kategorizált" },
      items: [],
      orderIndex: 999999
    };
    images.value.forEach(img => {
      if (img.categoryId && groupsMap.has(img.categoryId)) {
        groupsMap.get(img.categoryId).items.push(img);
      } else {
        uncategorized.items.push(img);
      }
    });
    groups.forEach(g => g.items.sort((a, b) => (a.orderIndex || 0) - (b.orderIndex || 0)));
    if (uncategorized.items.length > 0) groups.push(uncategorized);
    groupedImages.value = groups;
  };

  watch(() => company?.value?.id, (newId) => { if (newId) fetchData(); }, { immediate: true });

  const createCategory = async () => {
    if (!isLoggedIn.value) return;
    try {
      const payload = { name: { [currentLang.value]: "Új galéria" } };
      const res = await api.post('/api/Gallery/categories', payload);
      const newCat = res.data;
      newCat.name = ensureDict(newCat.name);
      const newGroup = { id: newCat.id, name: newCat.name, items: [], orderIndex: newCat.orderIndex };
      groupedImages.value.unshift(newGroup);
      await nextTick();
      const inputEl = categoryInputRefs.value[newGroup.id];
      if (inputEl) inputEl.focus();
    } catch (err) { console.error(err); }
  };

  const onCategoryDragChange = (event) => {
    if (event.moved) {
      groupedImages.value.forEach((group, index) => {
        if (group.id !== -1 && group.orderIndex !== index) {
          group.orderIndex = index;
          saveCategory(group);
        }
      });
    }
  };

  const saveCategory = (group) => {
    if (!group.id || group.id === -1) return;
    const currentName = group.name[currentLang.value]?.trim();
    if (!currentName) group.name[currentLang.value] = "Új galéria";
    addToQueue(`cat-${group.id}`, async () => {
      await api.put(`/api/Gallery/categories/${group.id}`, { name: group.name, orderIndex: group.orderIndex });
    });
  };

  const deleteCategory = async (group) => {
    if (!confirm(`Törlöd a "${group.name[currentLang.value]}" mappát?`)) return;
    try {
      await api.delete(`/api/Gallery/categories/${group.id}`);
      groupedImages.value = groupedImages.value.filter(g => g.id !== group.id);
    } catch (err) { alert("Nem sikerült törölni."); }
  };

  const onImageDragChange = (event, group) => {
    if (event.added || event.moved) {
      group.items.forEach((img, index) => {
        const newOrder = index + 1;
        if (img.orderIndex !== newOrder || img.categoryId !== group.id) {
          img.orderIndex = newOrder;
          img.categoryId = group.id;
          img.category = JSON.parse(JSON.stringify(group.name));
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

  const triggerCategoryUpload = (group) => {
    currentUploadCategory.value = group;
    if (fileInputRef.value) fileInputRef.value.click();
  };

  const handleFiles = async (event) => {
    const files = event.target.files;
    if (!files || files.length === 0 || !currentUploadCategory.value) return;
    isUploading.value = true;
    const targetCatName = currentUploadCategory.value.name['hu'] || "Új galéria";
    for (let i = 0; i < files.length; i++) {
      const formData = new FormData();
      formData.append('file', files[i]);
      formData.append('category', targetCatName);
      try { await api.post('/api/Gallery', formData, { headers: { 'Content-Type': undefined } }); }
      catch (err) { console.error(err); }
    }
    await fetchData();
    isUploading.value = false;
    currentUploadCategory.value = null;
    event.target.value = "";
  };

  const openPreview = (img) => { previewImage.value = img; showPreview.value = true; };
</script>

<template>
  <div class="gallery-container">
    <div class="header-actions">
      <h1>{{ $t('gallery.title') }}</h1>

      <button v-if="isLoggedIn" @click="createCategory" class="btn primary-btn big-btn">
        <i class="pi pi-plus-circle"></i> {{ $t('gallery.newGallery') }}
      </button>

      <input type="file" ref="fileInputRef" @change="handleFiles" multiple accept="image/*" style="display: none;" />
    </div>

    <div v-if="isLoading" class="loading-state">
      <i class="pi pi-spin pi-spinner"></i> {{ $t('common.loading') }}
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
              <div v-if="isLoggedIn && group.id !== -1" class="drag-handle-cat" title="Galéria mozgatása">⋮⋮</div>

              <div class="cat-title-wrapper">
                <div class="input-with-tools">
                  <input v-if="isLoggedIn && group.id !== -1"
                         :ref="el => categoryInputRefs[group.id] = el"
                         v-model="group.name[currentLang]"
                         @change="saveCategory(group)"
                         class="cat-input"
                         placeholder="Új galéria" />
                  <h3 v-else class="cat-title">{{ group.name[currentLang] }}</h3>

                  <button v-if="isLoggedIn && currentLang !== 'hu' && group.id !== -1"
                          @click="triggerTranslation(group.name, 'name')"
                          class="magic-btn" title="Fordítás">
                    <i v-if="translatingField === `${group.id}-name-${currentLang}`" class="pi pi-spin pi-spinner"></i>
                    <i v-else class="pi pi-sparkles"></i>
                  </button>
                </div>
                <span class="count">({{ group.items.length }})</span>
              </div>
            </div>

            <div v-if="isLoggedIn && group.id !== -1" class="cat-actions">
              <button @click="triggerCategoryUpload(group)" class="btn upload-sm-btn" :disabled="isUploading">
                <i v-if="isUploading && currentUploadCategory?.id === group.id" class="pi pi-spin pi-spinner"></i>
                <i v-else class="pi pi-cloud-upload"></i> {{ $t('gallery.uploadHere') }}
              </button>
              <button v-if="group.items.length === 0" @click="deleteCategory(group)" class="cat-delete-btn"><i class="pi pi-trash"></i></button>
            </div>
          </div>

          <draggable v-model="group.items" group="gallery-images" item-key="id" class="image-grid" @change="(e) => onImageDragChange(e, group)" :disabled="!isLoggedIn" ghost-class="ghost-card">
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
      {{ $t('gallery.emptyState') }}
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
  /* (Stílusok változatlanok) */
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

  .input-with-tools {
    position: relative;
    display: flex;
    align-items: center;
    width: auto;
    flex-grow: 1;
  }

  .magic-btn {
    opacity: 0;
    background: none;
    border: none;
    color: #d4af37;
    cursor: pointer;
    margin-left: 5px;
    font-size: 1rem;
    transition: opacity 0.2s;
  }

  .input-with-tools:hover .magic-btn {
    opacity: 1;
  }

  .magic-btn:hover {
    transform: scale(1.1);
    text-shadow: 0 0 5px #d4af37;
  }

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
