<script setup>
  import { ref, inject, watch, nextTick, computed } from 'vue';
  import api from '@/services/api';
  import { useAutoSaveQueue } from '@/composables/useAutoSaveQueue';
  import { useImageUpload } from '@/composables/useImageUpload';
  import { useDragAndDrop } from '@/composables/useDragAndDrop';
  import { useTranslation } from '@/composables/useTranslation';
  import draggable from 'vuedraggable';
  import { useI18n } from 'vue-i18n';

  const { locale } = useI18n();
  const isLoggedIn = inject('isLoggedIn');

  const currentLang = computed(() => locale.value);

  const images = ref([]);
  const categories = ref([]);
  const groupedImages = ref([]);
  const isLoading = ref(false);
  const showPreview = ref(false);
  const previewImage = ref(null);
  const fileInputRef = ref(null);
  const currentUploadCategory = ref(null);
  const categoryInputRefs = ref({});
  const company = inject('company', ref(null));

  const { addToQueue } = useAutoSaveQueue();
  const { isUploading, uploadImage } = useImageUpload();
  const { reorderNestedItems, reorderFlatItems } = useDragAndDrop();
  const { translatingField, translateField } = useTranslation();

  const ensureDict = (field, defaultValue = "") => {
    if (field && typeof field === 'object' && field !== null) return field;
    return { [currentLang.value]: field || defaultValue };
  };

  const triggerTranslation = async (obj, fieldName) => {
    await translateField({
      obj,
      fieldName,
      targetLang: currentLang.value,
      defaultLang: company.value?.defaultLanguage || 'hu',
      loadingKey: `${obj.id || 'new'}-${fieldName}-${currentLang.value}`,
      onSuccess: (updatedObj) => {
        if (updatedObj.items) saveCategory(updatedObj);
        else saveImage(updatedObj);
      }
    });
  };

  const fetchData = async () => {
    isLoading.value = true;
    try {
      const timestamp = new Date().getTime();
      const [resCats, resImages] = await Promise.all([
        api.get(`/api/Gallery/categories?t=${timestamp}`),
        api.get(`/api/Gallery?t=${timestamp}`)
      ]);

      const rawCats = Array.isArray(resCats.data) ? resCats.data : [];
      const rawImages = Array.isArray(resImages.data) ? resImages.data : [];

      categories.value = rawCats.map(c => ({ ...c, name: ensureDict(c.name, "Névtelen galéria") }));

      images.value = rawImages.map(i => ({
        ...i,
        category: ensureDict(i.category, "Egyéb"),
        title: ensureDict(i.title, "")
      }));

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

  const saveImage = (img) => {
    addToQueue(img.id, async () => {
      await api.put(`/api/Gallery/${img.id}`, {
        id: img.id,
        title: img.title,
        category: img.category,
        orderIndex: img.orderIndex
      });
    });
  };

  const onCategoryDragChange = async () => {
    const validGroups = groupedImages.value.filter(g => g.id !== -1);
    await reorderFlatItems(validGroups, saveCategory);
  };

  const onImageDragChange = async () => {
    await reorderNestedItems(
      groupedImages.value,
      'items',
      saveImage,
      (img, group) => {
        if (img.categoryId !== group.id) {
          img.categoryId = group.id;
          img.category = JSON.parse(JSON.stringify(group.name));
          return true;
        }
        return false;
      }
    );
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

    const targetCatId = currentUploadCategory.value.id;

    for (let i = 0; i < files.length; i++) {
      try {
        await uploadImage(files[i], '/api/Gallery', { categoryId: targetCatId });
      } catch (err) {
        console.error("Hiba az egyik kép feltöltésekor:", err);
      }
    }

    await fetchData();
    currentUploadCategory.value = null;
    event.target.value = "";
  };

  const openPreview = (img) => { previewImage.value = img; showPreview.value = true; };
</script>

<template>
  <div class="max-w-screen-xl mx-auto p-8 text-text">
    <div class="flex justify-between items-center mb-8 border-b border-text/10 pb-4">
      <h2 class="text-primary m-0 font-light tracking-widest text-3xl">{{ $t('gallery.title') }}</h2>

      <button v-if="isLoggedIn" @click="createCategory" class="bg-primary text-black font-bold flex items-center gap-2 rounded transition-transform duration-200 hover:scale-105 px-6 py-3 text-lg border-none cursor-pointer shadow-md">
        <i class="pi pi-plus-circle"></i> {{ $t('gallery.newGallery') }}
      </button>

      <input type="file" ref="fileInputRef" @change="handleFiles" multiple accept="image/*" class="hidden" />
    </div>

    <div v-if="isLoading" class="text-text-muted">
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
        <div class="mb-8 bg-text/5 border border-text/10 rounded-xl p-4 shadow-sm">

          <div class="flex justify-between items-center mb-4 px-4 py-2 bg-text/5 rounded-lg shadow-sm border border-text/5">
            <div class="flex items-center gap-4 flex-grow">
              <div v-if="isLoggedIn && group.id !== -1" class="cursor-grab text-2xl text-text-muted px-1 hover:text-primary drag-handle-cat transition-colors" title="Galéria mozgatása">⋮⋮</div>

              <div class="flex items-center gap-3 flex-grow">
                <div class="relative flex items-center w-auto flex-grow group">
                  <input v-if="isLoggedIn && group.id !== -1"
                         :ref="el => categoryInputRefs[group.id] = el"
                         v-model="group.name[currentLang]"
                         @change="saveCategory(group)"
                         class="bg-transparent border-none border-b border-dashed border-text/30 text-primary text-xl font-bold py-1 w-full max-w-[400px] focus:outline-none focus:border-solid focus:border-primary focus:bg-text/10 placeholder-text/50 transition-all rounded-t-sm px-1"
                         placeholder="Új galéria" />
                  <h3 v-else class="text-primary text-xl font-bold py-1 m-0">{{ group.name[currentLang] }}</h3>

                  <button v-if="isLoggedIn && group.id !== -1"
                          @click="triggerTranslation(group, 'name')"
                          class="opacity-30 bg-transparent border-none text-primary cursor-pointer ml-2 text-base transition-all duration-200 group-hover:opacity-100 hover:scale-110 hover:drop-shadow-[0_0_5px_rgba(212,175,55,0.8)]" title="Fordítás">
                    <i v-if="translatingField === `${group.id}-name-${currentLang}`" class="pi pi-spin pi-spinner"></i>
                    <i v-else class="pi pi-sparkles"></i>
                  </button>
                </div>
                <span class="text-text-muted font-bold">({{ group.items.length }})</span>
              </div>
            </div>

            <div v-if="isLoggedIn && group.id !== -1" class="flex items-center gap-3">
              <button @click="triggerCategoryUpload(group)" class="bg-text/5 text-text-muted border border-text/20 px-4 py-2 text-sm rounded flex items-center gap-2 cursor-pointer transition-colors duration-200 hover:bg-primary hover:text-black hover:border-primary shadow-sm" :disabled="isUploading">
                <i v-if="isUploading && currentUploadCategory?.id === group.id" class="pi pi-spin pi-spinner"></i>
                <i v-else class="pi pi-cloud-upload"></i> {{ $t('gallery.uploadHere') }}
              </button>
              <button v-if="group.items.length === 0" @click="deleteCategory(group)" class="bg-transparent border-none text-text-muted cursor-pointer p-2 transition-colors duration-200 hover:text-red-500"><i class="pi pi-trash text-lg"></i></button>
            </div>
          </div>

          <draggable v-model="group.items" group="gallery-images" item-key="id" class="grid grid-cols-[repeat(auto-fill,minmax(180px,1fr))] gap-4 min-h-[100px] p-4 bg-text/5 border border-text/5 rounded-lg shadow-inner" @change="(e) => onImageDragChange(e, group)" :disabled="!isLoggedIn" ghost-class="opacity-50">
            <template #item="{ element: img }">
              <div class="bg-surface rounded-md overflow-hidden shadow-md border border-text/10 flex flex-col">
                <div class="w-full aspect-square relative cursor-pointer overflow-hidden group/img" @click="openPreview(img)">
                  <img :src="img.imageUrl" loading="lazy" :alt="img.title[currentLang]" class="w-full h-full object-cover transition-transform duration-300 group-hover/img:scale-110" />
                  <div class="absolute inset-0 bg-black/50 flex items-center justify-center opacity-0 transition-opacity duration-200 text-white text-3xl group-hover/img:opacity-100"><i class="pi pi-search-plus"></i></div>
                </div>

                <div v-if="isLoggedIn" class="p-2 bg-surface flex gap-2 items-center border-t border-text/10">
                  <div class="relative flex items-center w-auto flex-grow group/tools">
                    <input v-model="img.title[currentLang]"
                           @change="saveImage(img)"
                           placeholder="Cím..."
                           class="w-full bg-text/5 border border-transparent text-text p-1.5 text-xs rounded focus:outline-none focus:border-primary focus:bg-text/10 transition-all" />

                    <button v-if="isLoggedIn"
                            @click="triggerTranslation(img, 'title')"
                            class="opacity-30 bg-transparent border-none text-primary cursor-pointer ml-1 text-sm transition-all duration-200 group-hover/tools:opacity-100 hover:scale-110 hover:drop-shadow-[0_0_5px_rgba(212,175,55,0.8)]" title="Fordítás">
                      <i v-if="translatingField === `${img.id}-title-${currentLang}`" class="pi pi-spin pi-spinner"></i>
                      <i v-else class="pi pi-sparkles"></i>
                    </button>
                  </div>

                  <button @click="deleteImage(img.id)" class="bg-text/5 text-red-500 border border-transparent w-7 h-7 p-0 cursor-pointer rounded flex justify-center items-center transition-colors duration-200 hover:bg-red-500 hover:text-white"><i class="pi pi-trash text-sm"></i></button>
                </div>

                <div v-else-if="img.title && img.title[currentLang]" class="p-2 text-center text-sm text-text-muted bg-surface">
                  {{ img.title[currentLang] }}
                </div>

              </div>
            </template>
          </draggable>

        </div>
      </template>
    </draggable>

    <div v-if="!isLoading && groupedImages.length === 0" class="text-text-muted text-center mt-10 text-lg">
      {{ $t('gallery.emptyState') }}
    </div>

    <div v-if="showPreview" class="fixed inset-0 bg-black/95 z-[9999] flex justify-center items-center backdrop-blur-sm" @click="showPreview = false">
      <div class="relative" @click.stop>
        <img :src="previewImage?.imageUrl" class="max-w-[95vw] max-h-[85vh] shadow-[0_0_40px_rgba(0,0,0,0.8)] rounded-md border border-white/10" />
        <p v-if="previewImage?.title" class="text-white mt-4 text-2xl text-center font-light tracking-wide">{{ previewImage.title[currentLang] }}</p>
        <button class="absolute -top-12 -right-6 bg-transparent border-none text-white/50 text-5xl cursor-pointer hover:text-primary transition-colors duration-200" @click="showPreview = false">&times;</button>
      </div>
    </div>
  </div>
</template>
