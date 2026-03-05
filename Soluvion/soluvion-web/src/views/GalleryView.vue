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
  const fileInputRef = ref(null);
  const currentUploadCategory = ref(null);
  const categoryInputRefs = ref({});
  const company = inject('company', ref(null));

  const previewImage = ref(null);
  const previewList = ref([]);
  const previewIndex = ref(0);
  const touchStartX = ref(0);
  const touchEndX = ref(0);

  const expandedCategoryId = ref(null);
  const transitionName = ref('fade');

  const { addToQueue } = useAutoSaveQueue();
  const { isUploading, uploadImage } = useImageUpload();
  const { reorderNestedItems, reorderFlatItems } = useDragAndDrop();
  const { translatingField, translateField } = useTranslation();

  const ensureDict = (field, defaultValue = "") => {
    if (field && typeof field === 'object' && field !== null) return field;
    return { [currentLang.value]: field || defaultValue };
  };

  const getOptimizedUrl = (url, width = 'auto') => {
    if (!url) return '';
    if (!url.includes('cloudinary.com') || url.includes('f_auto')) return url;
    const parts = url.split('/upload/');
    if (parts.length === 2) {
      return `${parts[0]}/upload/f_auto,q_auto,w_${width},c_limit/${parts[1]}`;
    }
    return url;
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
    const uncategorized = { id: -1, name: { [currentLang.value]: "Egyéb / Nem kategorizált" }, items: [], orderIndex: 999999 };

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
      const res = await api.post('/api/Gallery/categories', { name: { [currentLang.value]: "Új galéria" } });
      const newCat = res.data;
      newCat.name = ensureDict(newCat.name);
      const newGroup = { id: newCat.id, name: newCat.name, items: [], orderIndex: newCat.orderIndex };
      groupedImages.value.unshift(newGroup);
      await nextTick();
      if (categoryInputRefs.value[newGroup.id]) categoryInputRefs.value[newGroup.id].focus();
    } catch (err) { console.error(err); }
  };

  const saveCategory = (group) => {
    if (!group.id || group.id === -1) return;
    if (!group.name[currentLang.value]?.trim()) group.name[currentLang.value] = "Új galéria";
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
      await api.put(`/api/Gallery/${img.id}`, { id: img.id, title: img.title, category: img.category, orderIndex: img.orderIndex });
    });
  };

  const onCategoryDragChange = async () => {
    await reorderFlatItems(groupedImages.value.filter(g => g.id !== -1), saveCategory);
  };

  const onImageDragChange = async () => {
    await reorderNestedItems(groupedImages.value, 'items', saveImage, (img, group) => {
      if (img.categoryId !== group.id) {
        img.categoryId = group.id;
        img.category = JSON.parse(JSON.stringify(group.name));
        return true;
      }
      return false;
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
    for (let i = 0; i < files.length; i++) {
      try { await uploadImage(files[i], '/api/Gallery', { categoryId: currentUploadCategory.value.id }); }
      catch (err) { console.error("Hiba:", err); }
    }
    await fetchData();
    currentUploadCategory.value = null;
    event.target.value = "";
  };

  // --- OKOS GALÉRIA FUNKCIÓK UX JAVÍTÁSSAL ---

  const toggleCategory = (id) => {
    const isOpening = expandedCategoryId.value !== id;
    expandedCategoryId.value = isOpening ? id : null;

    // Ha kinyitunk egy kategóriát, odagörgetünk hozzá
    if (isOpening) {
      setTimeout(() => {
        const catEl = document.getElementById(`cat-${id}`);
        if (catEl) {
          catEl.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
      }, 350); // Várunk egy picit, amíg a többi összecsukódik
    }
  };

  const openPreview = (img, index, items, groupId) => {
    if (!isLoggedIn.value && groupId && expandedCategoryId.value !== groupId) {
      toggleCategory(groupId); // Használjuk az okosított toggle-t!
    }

    previewList.value = items;
    previewIndex.value = index;
    previewImage.value = img;
    transitionName.value = 'fade';
    showPreview.value = true;
  };

  const nextImage = () => {
    if (previewIndex.value < previewList.value.length - 1) {
      transitionName.value = 'slide-next';
      previewIndex.value++;
      previewImage.value = previewList.value[previewIndex.value];
    }
  };

  const prevImage = () => {
    if (previewIndex.value > 0) {
      transitionName.value = 'slide-prev';
      previewIndex.value--;
      previewImage.value = previewList.value[previewIndex.value];
    }
  };

  const handleTouchStart = (e) => { touchStartX.value = e.changedTouches[0].screenX; };
  const handleTouchEnd = (e) => {
    touchEndX.value = e.changedTouches[0].screenX;
    if (touchStartX.value - touchEndX.value > 40) nextImage();
    if (touchEndX.value - touchStartX.value > 40) prevImage();
  };

  const closePreview = () => {
    showPreview.value = false;

    setTimeout(() => {
      const thumbEl = document.getElementById(`thumb-${previewImage.value.id}`);
      if (thumbEl) {
        thumbEl.scrollIntoView({ behavior: 'smooth', block: 'center' });

        thumbEl.classList.add('ring-4', 'ring-primary', 'scale-[1.03]', 'z-10');
        setTimeout(() => {
          thumbEl.classList.remove('ring-4', 'ring-primary', 'scale-[1.03]', 'z-10');
        }, 1000);
      }
    }, 50);
  };
</script>

<template>
  <div class="max-w-7xl mx-auto px-4 py-6 md:p-8 text-text">

    <div class="flex flex-col sm:flex-row justify-between items-start sm:items-center mb-8 border-b border-text/10 pb-4 gap-4">
      <h2 class="text-primary m-0 font-light tracking-widest text-3xl">{{ $t('gallery.title') }}</h2>

      <button v-if="isLoggedIn" @click="createCategory" class="w-full sm:w-auto bg-primary text-black font-bold flex justify-center items-center gap-2 rounded-xl transition-transform duration-200 hover:scale-[1.02] px-6 py-3 min-h-[48px] border-none cursor-pointer shadow-md">
        <i class="pi pi-plus-circle"></i> {{ $t('gallery.newGallery') }}
      </button>

      <input type="file" ref="fileInputRef" @change="handleFiles" multiple accept="image/*" class="hidden" />
    </div>

    <div v-if="isLoading" class="text-text-muted flex items-center min-h-[44px]">
      <i class="pi pi-spin pi-spinner mr-2 text-xl"></i> {{ $t('common.loading') }}
    </div>

    <draggable v-else
               v-model="groupedImages"
               group="categories"
               item-key="id"
               handle=".drag-handle-cat"
               @change="onCategoryDragChange"
               :disabled="!isLoggedIn">
      <template #item="{ element: group }">
        <div :id="`cat-${group.id}`" class="mb-8 bg-surface border border-text/10 rounded-2xl p-3 md:p-5 shadow-sm scroll-mt-24">

          <div class="flex flex-col md:flex-row justify-between items-start md:items-center mb-4 p-3 bg-text/5 rounded-xl shadow-sm border border-text/5 gap-4 transition-colors duration-200"
               :class="{ 'cursor-pointer hover:bg-text/10': !isLoggedIn }"
               @click="!isLoggedIn && toggleCategory(group.id)">

            <div class="flex items-center gap-2 md:gap-4 flex-grow w-full md:w-auto">
              <div v-if="isLoggedIn && group.id !== -1" class="cursor-grab text-2xl text-text-muted flex items-center justify-center min-w-[44px] min-h-[44px] hover:text-primary drag-handle-cat transition-colors shrink-0" title="Galéria mozgatása">⋮⋮</div>

              <div class="flex items-center gap-3 flex-grow min-w-0">
                <div class="relative flex items-center w-full group/cat">
                  <input v-if="isLoggedIn && group.id !== -1"
                         :ref="el => categoryInputRefs[group.id] = el"
                         v-model="group.name[currentLang]"
                         @change="saveCategory(group)"
                         class="bg-transparent border-none border-b border-dashed border-text/30 text-primary text-lg md:text-xl font-bold py-2 w-full min-h-[44px] focus:outline-none focus:border-solid focus:border-primary focus:bg-text/10 placeholder-text/50 transition-all rounded-t-sm px-1 truncate"
                         placeholder="Új galéria" />

                  <h3 v-else class="text-primary text-lg md:text-xl font-bold py-2 m-0 truncate select-none">{{ group.name[currentLang] }}</h3>

                  <button v-if="isLoggedIn && group.id !== -1"
                          @click.stop="triggerTranslation(group, 'name')"
                          class="opacity-100 md:opacity-0 bg-transparent border-none text-primary cursor-pointer ml-1 flex items-center justify-center min-w-[44px] min-h-[44px] transition-all duration-200 md:group-hover/cat:opacity-100 hover:scale-110 shrink-0" title="Fordítás">
                    <i v-if="translatingField === `${group.id}-name-${currentLang}`" class="pi pi-spin pi-spinner"></i>
                    <i v-else class="pi pi-sparkles"></i>
                  </button>
                </div>

                <span class="text-text-muted font-bold shrink-0 select-none">({{ group.items.length }})</span>

                <i v-if="!isLoggedIn && group.items.length > 3"
                   :class="expandedCategoryId === group.id ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"
                   class="text-primary ml-auto text-xl pr-2 transition-transform duration-300"></i>
              </div>
            </div>

            <div v-if="isLoggedIn && group.id !== -1" class="flex items-center gap-2 w-full md:w-auto justify-end">
              <button @click.stop="triggerCategoryUpload(group)" class="bg-background text-text-muted border border-text/20 px-4 py-2 min-h-[44px] font-bold text-sm rounded-lg flex items-center justify-center gap-2 cursor-pointer transition-colors duration-200 hover:bg-primary hover:text-white hover:border-primary shadow-sm flex-grow md:flex-grow-0" :disabled="isUploading">
                <i v-if="isUploading && currentUploadCategory?.id === group.id" class="pi pi-spin pi-spinner"></i>
                <i v-else class="pi pi-cloud-upload"></i> {{ $t('gallery.uploadHere') }}
              </button>
              <button v-if="group.items.length === 0" @click.stop="deleteCategory(group)" class="bg-transparent border border-red-500/30 text-red-500 rounded-lg cursor-pointer flex items-center justify-center min-w-[44px] min-h-[44px] transition-colors duration-200 hover:bg-red-500 hover:text-white shrink-0"><i class="pi pi-trash text-lg"></i></button>
            </div>
          </div>

          <draggable v-if="isLoggedIn" v-model="group.items" group="gallery-images" item-key="id" class="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-[repeat(auto-fill,minmax(140px,1fr))] gap-2 md:gap-4 min-h-[100px] p-2 md:p-4 bg-background border border-text/5 rounded-xl shadow-inner" @change="(e) => onImageDragChange(e, group)" ghost-class="opacity-50">
            <template #item="{ element: img, index }">
              <div :id="`thumb-${img.id}`" class="bg-surface rounded-xl overflow-hidden shadow-sm border border-text/10 flex flex-col group/card transition-all duration-300">
                <div class="w-full aspect-square relative cursor-pointer overflow-hidden" @click="openPreview(img, index, group.items, group.id)">
                  <img :src="getOptimizedUrl(img.imageUrl, 500)" loading="lazy" :alt="img.title[currentLang]" class="w-full h-full object-cover transition-transform duration-500 group-hover/card:scale-110" />
                </div>
                <div class="p-2 bg-surface flex flex-col gap-2 items-stretch border-t border-text/10">
                  <div class="relative flex items-center w-full group/tools">
                    <input v-model="img.title[currentLang]" @change="saveImage(img)" placeholder="Cím..." class="w-full bg-text/5 border border-transparent text-text p-2 min-h-[44px] text-xs md:text-sm rounded-lg focus:outline-none focus:border-primary focus:bg-text/10 transition-all" />
                    <button @click="triggerTranslation(img, 'title')" class="absolute right-0 opacity-100 md:opacity-0 bg-transparent border-none text-primary cursor-pointer flex items-center justify-center min-w-[44px] min-h-[44px] text-lg transition-all duration-200 md:group-hover/tools:opacity-100 hover:scale-110 shrink-0"><i class="pi pi-sparkles"></i></button>
                  </div>
                  <button @click="deleteImage(img.id)" class="bg-red-500/10 text-red-500 border border-transparent w-full min-h-[44px] font-bold cursor-pointer rounded-lg flex justify-center items-center transition-colors duration-200 hover:bg-red-500 hover:text-white"><i class="pi pi-trash mr-2"></i></button>
                </div>
              </div>
            </template>
          </draggable>

          <div v-else>
            <div class="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-[repeat(auto-fill,minmax(140px,1fr))] gap-2 md:gap-4 p-2 md:p-4 bg-background border border-text/5 rounded-xl shadow-inner transition-all duration-500">
              <div v-for="(img, index) in (expandedCategoryId === group.id ? group.items : group.items.slice(0, 3))" :key="img.id" :id="`thumb-${img.id}`" class="bg-surface rounded-xl overflow-hidden shadow-sm border border-text/10 flex flex-col group/card transition-all duration-300">

                <div class="w-full aspect-square relative cursor-pointer overflow-hidden" @click.stop="openPreview(img, index, group.items, group.id)">
                  <img :src="getOptimizedUrl(img.imageUrl, 500)" loading="lazy" :alt="img.title[currentLang]" class="w-full h-full object-cover transition-transform duration-500 group-hover/card:scale-110" />
                  <div class="absolute inset-0 bg-black/40 flex items-center justify-center opacity-0 transition-opacity duration-300 text-white text-3xl group-hover/card:opacity-100"><i class="pi pi-search-plus"></i></div>
                </div>

                <div v-if="img.title && img.title[currentLang]" class="p-2 text-center text-xs md:text-sm text-text-muted bg-surface font-medium truncate">
                  {{ img.title[currentLang] }}
                </div>
              </div>
            </div>
          </div>

        </div>
      </template>
    </draggable>

    <div v-if="!isLoading && groupedImages.length === 0" class="text-text-muted text-center mt-10 text-lg border-2 border-dashed border-text/10 rounded-2xl p-10">
      <i class="pi pi-images text-4xl mb-4 block opacity-50"></i>
      {{ $t('gallery.emptyState') }}
    </div>

    <div v-if="showPreview"
         class="fixed inset-0 bg-black/95 z-[9999] flex justify-center items-center backdrop-blur-sm p-0 md:p-4 select-none"
         @click="closePreview"
         @touchstart="handleTouchStart"
         @touchend="handleTouchEnd">

      <button class="fixed top-4 right-4 z-[10000] w-12 h-12 bg-black/50 border border-white/20 rounded-full flex items-center justify-center text-white text-2xl cursor-pointer hover:bg-primary hover:border-primary transition-all duration-300" @click.stop="closePreview">
        <i class="pi pi-times"></i>
      </button>

      <button v-if="previewIndex > 0" class="hidden md:flex absolute left-4 z-[10000] w-14 h-14 bg-black/50 border border-white/20 rounded-full items-center justify-center text-white text-2xl cursor-pointer hover:bg-primary hover:border-primary transition-all duration-300" @click.stop="prevImage">
        <i class="pi pi-chevron-left"></i>
      </button>

      <div class="relative flex flex-col items-center justify-center w-full h-full px-2 overflow-hidden" @click.stop="closePreview">

        <Transition mode="out-in"
                    :enter-active-class="'transition-all duration-200 ease-out'"
                    :leave-active-class="'transition-all duration-200 ease-in'"
                    :enter-from-class="transitionName === 'slide-next' ? 'opacity-0 translate-x-10' : (transitionName === 'slide-prev' ? 'opacity-0 -translate-x-10' : 'opacity-0')"
                    :enter-to-class="'opacity-100 translate-x-0'"
                    :leave-from-class="'opacity-100 translate-x-0'"
                    :leave-to-class="transitionName === 'slide-next' ? 'opacity-0 -translate-x-10' : (transitionName === 'slide-prev' ? 'opacity-0 translate-x-10' : 'opacity-0')">
          <div :key="previewImage?.id" class="flex flex-col items-center justify-center w-full h-full cursor-pointer">
            <img :src="getOptimizedUrl(previewImage?.imageUrl, 1920)" class="max-w-full max-h-[85vh] shadow-[0_0_40px_rgba(0,0,0,0.8)] rounded-xl border border-white/10 object-contain" />
            <p v-if="previewImage?.title && previewImage.title[currentLang]" class="text-white mt-4 md:mt-6 text-xl md:text-2xl text-center font-light tracking-wide px-4 drop-shadow-md">{{ previewImage.title[currentLang] }}</p>
          </div>
        </Transition>

      </div>

      <button v-if="previewIndex < previewList.length - 1" class="hidden md:flex absolute right-4 z-[10000] w-14 h-14 bg-black/50 border border-white/20 rounded-full items-center justify-center text-white text-2xl cursor-pointer hover:bg-primary hover:border-primary transition-all duration-300" @click.stop="nextImage">
        <i class="pi pi-chevron-right"></i>
      </button>

      <div v-if="previewList.length > 1" class="md:hidden absolute bottom-8 text-white/50 text-sm flex items-center gap-2 tracking-widest pointer-events-none">
        <i class="pi pi-angle-left"></i> SWIPE <i class="pi pi-angle-right"></i>
      </div>

    </div>

  </div>
</template>
