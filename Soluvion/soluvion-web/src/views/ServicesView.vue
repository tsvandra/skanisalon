<script setup>
  import { ref, inject, watch, computed, nextTick } from 'vue';
  import InputNumber from 'primevue/inputnumber';
  import apiClient from '@/services/api';
  import draggable from 'vuedraggable';
  import { useI18n } from 'vue-i18n';
  import { useCompanyStore } from '@/stores/companyStore';
  import { useDragAndDrop } from '@/composables/useDragAndDrop';
  import { useTranslation } from '@/composables/useTranslation';

  const { locale } = useI18n();
  const isLoggedIn = inject('isLoggedIn');
  const companyStore = useCompanyStore();

  const services = ref([]);
  const categories = ref([]);
  const loading = ref(true);

  const currentLang = computed(() => locale.value);
  const company = inject('company', ref(null));
  const saveQueues = new Map();

  const { reorderNestedItems } = useDragAndDrop();
  const { translatingField, translateField } = useTranslation();

  const sortVariants = (variants) => {
    if (!variants) return [];
    return variants.sort((a, b) => {
      if (a.id === 0 && b.id === 0) return 0;
      if (a.id === 0) return 1;
      if (b.id === 0) return -1;
      return a.id - b.id;
    });
  };

  const formatCurrency = (val) => {
    if (val === null || val === undefined || val === 0) return '';
    return val.toLocaleString('hu-HU', { style: 'currency', currency: 'EUR', maximumFractionDigits: 0 });
  };

  const autoResize = (event) => {
    if (event && event.target) {
      event.target.style.height = 'auto';
      event.target.style.height = event.target.scrollHeight + 'px';
    }
  };

  const resizeAllTextareas = async () => {
    await nextTick();
    const textareas = document.querySelectorAll('textarea');
    textareas.forEach(el => {
      el.style.height = 'auto';
      if (el.scrollHeight > 0) {
        el.style.height = el.scrollHeight + 'px';
      }
    });
  };

  watch(currentLang, () => {
    resizeAllTextareas();
  });

  const ensureDict = (field, defaultValue = "") => {
    if (field && typeof field === 'object' && field !== null) return field;
    return { [currentLang.value]: field || defaultValue };
  };

  const doTranslate = async (obj, fieldName, loadingKey, saveAction) => {
    await translateField({
      obj,
      fieldName,
      targetLang: currentLang.value,
      defaultLang: company.value?.defaultLanguage || 'hu',
      loadingKey,
      onSuccess: async (updatedObj) => {
        resizeAllTextareas();
        await saveAction(updatedObj);
      }
    });
  };

  const translateServiceField = (service, fieldName) => doTranslate(service, fieldName, `${service.id}-${fieldName}-${currentLang.value}`, (s) => saveService(s, false));
  const translateCategoryName = (group) => doTranslate(group, 'categoryName', `${group.id || 'cat'}-categoryName-${currentLang.value}`, updateCategoryName);
  const translateHeaderVariant = (group, variant, vIndex) => doTranslate(variant, 'variantName', `header-${vIndex}-variantName-${currentLang.value}`, () => updateGroupVariantName(group, vIndex));

  const buildNestedStructure = (flatServices) => {
    const groups = [];
    const defaultLang = company.value?.defaultLanguage || 'hu';

    flatServices.sort((a, b) => a.orderIndex - b.orderIndex);

    flatServices.forEach(service => {
      const catDict = ensureDict(service.category, "Egyéb");
      const catNameStr = catDict[defaultLang] || catDict['hu'] || "Egyéb";

      let group = groups.find(g => {
        const gName = g.categoryName[defaultLang] || g.categoryName['hu'] || "";
        return gName === catNameStr;
      });

      if (!group) {
        group = {
          id: 'cat-' + catNameStr.replace(/\s+/g, '-').toLowerCase(),
          categoryName: catDict,
          headerVariants: [],
          items: []
        };
        groups.push(group);
      }

      if (group.headerVariants.length === 0 && service.variants && service.variants.length > 0) {
        group.headerVariants = JSON.parse(JSON.stringify(service.variants));
      }
      group.items.push(service);
    });
    return groups;
  };

  const processServices = (serviceList) => {
    serviceList.forEach(service => {
      if (service.variants) {
        service.variants = sortVariants(service.variants);
        service.variants.forEach(v => {
          if (v.price === 0) v.price = null;
          v.variantName = ensureDict(v.variantName, "Extra");
        });
      }
      service.name = ensureDict(service.name, "Névtelen");
      service.category = ensureDict(service.category, "Egyéb");
      service.description = ensureDict(service.description, "");
    });
    categories.value = buildNestedStructure(serviceList);
  };

  const fetchServices = async () => {
    loading.value = true;
    try {
      const response = await apiClient.get('/api/Service');
      const rawServices = response.data;

      if (!Array.isArray(rawServices)) {
        if (rawServices && Array.isArray(rawServices.$values)) {
          processServices(rawServices.$values);
        }
        return;
      }
      processServices(rawServices);
    } catch (error) { console.error('Hiba a betolteskor:', error); }
    finally {
      loading.value = false;
      resizeAllTextareas();
    }
  };

  watch(() => company?.value?.id, (newId) => { if (newId) fetchServices(); }, { immediate: true });

  const saveService = async (serviceItem, refreshLocal = false) => {
    const serviceId = serviceItem.id;
    if (!saveQueues.has(serviceId)) {
      saveQueues.set(serviceId, Promise.resolve());
    }
    const currentQueue = saveQueues.get(serviceId);
    const newPromise = currentQueue.then(async () => {
      try {
        const payload = JSON.parse(JSON.stringify(serviceItem));
        if (!payload.variants) payload.variants = [];
        else {
          payload.variants.forEach(v => {
            if (v.price === null || v.price === undefined || v.price === '') v.price = 0;
            else v.price = Number(v.price);
          });
        }
        const response = await apiClient.put(`/api/Service/${serviceItem.id}`, payload);
        if (refreshLocal && response.status === 200) {
          const updated = response.data;
          if (updated.variants) {
            updated.variants = sortVariants(updated.variants);
            updated.variants.forEach(v => {
              if (v.price === 0) v.price = null;
              v.variantName = ensureDict(v.variantName);
            });
          }
          updated.name = ensureDict(updated.name);
          updated.category = ensureDict(updated.category);
          updated.description = ensureDict(updated.description);
          Object.assign(serviceItem, updated);
          resizeAllTextareas();
        }
      } catch (err) { console.error("Hiba a mentesnel:", err); }
    });
    saveQueues.set(serviceId, newPromise);
    return newPromise;
  };

  const reorderAll = async () => {
    await reorderNestedItems(
      categories.value,
      'items',
      saveService,
      (item, group) => {
        const itemCatName = item.category['hu'];
        const groupCatName = group.categoryName['hu'];
        if (itemCatName !== groupCatName) {
          item.category = JSON.parse(JSON.stringify(group.categoryName));
          return true;
        }
        return false;
      }
    );
  };

  const onServiceDragChange = async () => { await reorderAll(); };
  const onCategoryDragChange = async () => { await reorderAll(); };

  const updateCategoryName = async (group) => {
    const promises = group.items.map(service => {
      service.category = JSON.parse(JSON.stringify(group.categoryName));
      return saveService(service, false);
    });
    await Promise.all(promises);
  };

  const updateGroupVariantName = async (group, variantIndex) => {
    const promises = group.items.map(service => {
      if (service.variants && service.variants[variantIndex]) {
        service.variants[variantIndex].variantName = JSON.parse(JSON.stringify(group.headerVariants[variantIndex].variantName));
        return saveService(service, false);
      }
      return Promise.resolve();
    });
    await Promise.all(promises);
  };

  const onNoteDragStart = (event, service) => {
    draggedNoteContent.value = service.description[currentLang.value];
    draggedFromServiceId.value = service.id;
    event.dataTransfer.effectAllowed = 'move';
    event.target.style.opacity = '0.5';
  };

  const draggedNoteContent = ref(null);
  const draggedFromServiceId = ref(null);

  const onNoteDragEnd = (event) => {
    event.target.style.opacity = '1';
    draggedNoteContent.value = null;
    draggedFromServiceId.value = null;
    document.querySelectorAll('.service-drop-zone').forEach(el => {
      el.classList.remove('!border-primary', '!bg-primary/5');
    });
  };

  const onNoteDragOver = (event) => {
    event.preventDefault();
    event.currentTarget.classList.add('!border-primary', '!bg-primary/5');
  };

  const onNoteDragLeave = (event) => {
    event.currentTarget.classList.remove('!border-primary', '!bg-primary/5');
  };

  const onNoteDrop = async (event, targetService) => {
    event.preventDefault();
    event.currentTarget.classList.remove('!border-primary', '!bg-primary/5');

    if (!draggedNoteContent.value || draggedFromServiceId.value === targetService.id) return;

    const currentDesc = targetService.description[currentLang.value] || "";
    if (currentDesc.trim() !== '') {
      targetService.description[currentLang.value] = currentDesc + '\n' + draggedNoteContent.value;
    } else {
      targetService.description[currentLang.value] = draggedNoteContent.value;
    }

    for (const group of categories.value) {
      const sourceService = group.items.find(s => s.id === draggedFromServiceId.value);
      if (sourceService) {
        sourceService.description[currentLang.value] = '';
        await saveService(sourceService, true);
        break;
      }
    }
    resizeAllTextareas();
    await saveService(targetService, true);
  };

  const toggleNote = async (service) => {
    if (!service.description[currentLang.value]) {
      service.description[currentLang.value] = " ";
      resizeAllTextareas();
    }
  };

  const createNewCategory = async () => {
    if (!isLoggedIn.value) return;
    const newService = {
      name: { [currentLang.value]: "Új szolgáltatás" },
      category: { [currentLang.value]: "ÚJ KATEGÓRIA" },
      defaultPrice: 0,
      orderIndex: 99999,
      variants: [{ variantName: { [currentLang.value]: "Normál" }, price: 0, duration: 30 }],
      description: { [currentLang.value]: "" }
    };
    await postNewService(newService);
  };

  const addServiceToGroupEnd = async (group) => {
    let variants = [{ variantName: { [currentLang.value]: "Normál" }, price: 0, duration: 30 }];
    if (group.headerVariants && group.headerVariants.length > 0) {
      variants = group.headerVariants.map(v => ({
        variantName: JSON.parse(JSON.stringify(v.variantName)),
        price: 0,
        duration: v.duration
      }));
    }
    const newService = {
      name: { [currentLang.value]: "Új szolgáltatás" },
      category: JSON.parse(JSON.stringify(group.categoryName)),
      defaultPrice: 0,
      orderIndex: 99999,
      variants: variants,
      description: { [currentLang.value]: "" }
    };
    await postNewService(newService);
  };

  const postNewService = async (dto) => {
    try {
      const payload = JSON.parse(JSON.stringify(dto));
      if (!payload.variants) payload.variants = [];
      payload.variants.forEach(v => { v.price = 0; });
      await apiClient.post('/api/Service', payload);
      await fetchServices();
    } catch (err) { console.error(err); }
  };

  const deleteService = async (id) => {
    if (!confirm("Biztosan törölni akarod?")) return;
    try {
      await apiClient.delete(`/api/Service/${id}`);
      await fetchServices();
    } catch (err) { console.error(err); }
  };

  const removeVariant = async (service, vIndex, group) => {
    service.variants.splice(vIndex, 1);
    if (group) group.headerVariants = [...service.variants];
    await saveService(service, true);
  };

  const addVariantToService = async (service, group) => {
    if (!service.variants) service.variants = [];
    service.variants.push({
      id: 0,
      variantName: { [currentLang.value]: "Extra" },
      price: 0,
      duration: 30
    });
    if (group) group.headerVariants = [...service.variants];
    await saveService(service, true);
  };
</script>

<template>
  <div class="max-w-5xl w-full mx-auto px-4 py-6 md:p-8 box-border bg-background text-text min-h-screen" :style="{
    '--primary-color': company?.primaryColor || '#d4af37',
    '--secondary-color': company?.secondaryColor || '#1a1a1a'
  }">

    <div class="flex flex-col sm:flex-row justify-between items-start sm:items-center mb-8 gap-4">
      <h2 class="font-light text-text m-0 tracking-wide text-3xl">{{ isLoggedIn ? $t('services.editorTitle') : $t('services.title') }}</h2>

      <button v-if="isLoggedIn" @click="createNewCategory" class="bg-primary text-black border-none px-5 py-2 min-h-[44px] rounded-lg cursor-pointer font-bold flex items-center justify-center gap-2 transition-all hover:brightness-90 shadow-sm w-full sm:w-auto">
        <i class="pi pi-folder-open"></i> {{ $t('services.newCategory') }}
      </button>
    </div>

    <div v-if="loading" class="text-text-muted flex items-center min-h-[44px]">
      <i class="pi pi-spin pi-spinner mr-2 text-xl"></i>{{ $t('common.loading') }}
    </div>

    <div v-else class="pb-12">

      <draggable v-model="categories" item-key="id" handle=".drag-handle-cat" @change="onCategoryDragChange" :disabled="!isLoggedIn">
        <template #item="{ element: group }">

          <div class="mb-10 bg-surface border border-text/10 rounded-2xl shadow-md overflow-hidden flex flex-col">

            <div class="bg-text/5 p-3 md:p-4 border-b border-text/10 flex items-center justify-between gap-4">
              <div class="flex items-center flex-grow">
                <div v-if="isLoggedIn" class="cursor-grab text-2xl text-primary flex items-center justify-center min-w-[40px] min-h-[40px] drag-handle-cat transition-colors hover:text-primary/80" title="Kategória mozgatása">⋮⋮</div>

                <div class="relative w-full flex items-center group/tools flex-grow">
                  <input v-if="isLoggedIn"
                         v-model="group.categoryName[currentLang]"
                         @change="updateCategoryName(group)"
                         class="text-lg md:text-xl font-bold text-primary border-none bg-transparent w-full uppercase tracking-widest focus:outline-none focus:border-b focus:border-primary transition-colors py-1 px-2"
                         :placeholder="$t('services.categoryNamePlaceholder')" />
                  <span v-else class="text-lg md:text-xl font-bold text-primary uppercase tracking-widest py-1 px-2">{{ group.categoryName[currentLang] }}</span>

                  <button v-if="isLoggedIn"
                          @click="translateCategoryName(group)"
                          class="opacity-100 md:opacity-0 bg-transparent border-none text-primary cursor-pointer flex items-center justify-center min-w-[40px] min-h-[40px] text-lg transition-opacity duration-200 md:group-hover/tools:opacity-100 hover:scale-110 shrink-0" title="Fordítás">
                    <i v-if="translatingField === `${group.id || 'cat'}-categoryName-${currentLang}`" class="pi pi-spin pi-spinner"></i>
                    <i v-else class="pi pi-sparkles"></i>
                  </button>
                </div>
              </div>
            </div>

            <div class="w-full overflow-x-auto bg-background [&::-webkit-scrollbar]:h-[6px] [&::-webkit-scrollbar-track]:bg-transparent [&::-webkit-scrollbar-thumb]:bg-text-muted/50 [&::-webkit-scrollbar-thumb]:rounded-full [&::-webkit-scrollbar-thumb:hover]:bg-primary">
              <div class="min-w-[500px] md:min-w-0 flex flex-col w-full">

                <div class="flex items-end border-b border-text/10 px-2 pt-3 pb-2 bg-text/5">
                  <div class="flex-grow pl-[45px] md:pl-[50px] text-xs font-bold text-text-muted uppercase tracking-widest pb-1 opacity-70">
                    {{ $t('nav.services') }}
                  </div>

                  <div class="flex justify-end gap-2 md:gap-4 shrink-0 pr-2">
                    <div v-for="(v, vIndex) in group.headerVariants" :key="vIndex" class="w-[90px] md:w-[120px] flex items-end justify-center relative text-center">
                      <div class="relative w-full flex items-center justify-center group/tools">
                        <textarea v-if="isLoggedIn"
                                  v-model="v.variantName[currentLang]"
                                  @change="updateGroupVariantName(group, vIndex)"
                                  class="w-full text-center border-none bg-transparent font-bold text-text-muted text-xs md:text-sm uppercase resize-none overflow-hidden focus:bg-text/5 focus:outline focus:outline-1 focus:outline-primary focus:text-text rounded transition-colors py-1"
                                  rows="1"></textarea>
                        <span v-else class="font-bold text-text-muted text-xs md:text-sm uppercase text-center block w-full whitespace-normal break-words py-1">{{ v.variantName[currentLang] }}</span>

                        <button v-if="isLoggedIn"
                                @click="translateHeaderVariant(group, v, vIndex)"
                                class="absolute -top-6 md:-top-5 right-0 opacity-100 md:opacity-0 bg-transparent border-none text-primary cursor-pointer flex items-center justify-center w-[24px] h-[24px] text-xs transition-opacity duration-200 md:group-hover/tools:opacity-100 hover:scale-110" title="Fordítás">
                          <i v-if="translatingField === `header-${vIndex}-variantName-${currentLang}`" class="pi pi-spin pi-spinner"></i>
                          <i v-else class="pi pi-sparkles"></i>
                        </button>
                      </div>
                    </div>
                  </div>
                </div>

                <draggable v-model="group.items" item-key="id" group="services" handle=".drag-handle-item" @change="(e) => onServiceDragChange(e, group)" :disabled="!isLoggedIn" class="flex flex-col">
                  <template #item="{ element: service }">

                    <div class="service-drop-zone flex flex-col border-b border-text/10 last:border-0 hover:bg-text/5 transition-colors relative group/row" @dragover="onNoteDragOver" @dragleave="onNoteDragLeave" @drop="(e) => onNoteDrop(e, service)">

                      <div class="flex items-center px-2 py-2 md:py-3 w-full">

                        <div class="flex-grow flex items-center gap-1 pr-4 min-w-[200px]">
                          <div v-if="isLoggedIn" class="cursor-grab text-text-muted text-lg flex items-center justify-center min-w-[40px] min-h-[44px] hover:text-primary drag-handle-item transition-colors">⋮⋮</div>

                          <div class="relative w-full flex items-center group/tools flex-grow">
                            <textarea v-if="isLoggedIn"
                                      v-model="service.name[currentLang]"
                                      @change="saveService(service, false)"
                                      @input="autoResize"
                                      class="w-full border-none bg-transparent font-medium text-base text-text resize-none overflow-hidden leading-snug py-2 px-1 focus:outline-none focus:bg-background rounded transition-colors"
                                      rows="1"></textarea>
                            <span v-else class="text-base font-medium text-text whitespace-normal break-words leading-snug block w-full group-hover/row:text-primary transition-colors py-2 px-1">{{ service.name[currentLang] }}</span>

                            <button v-if="isLoggedIn"
                                    @click="translateServiceField(service, 'name')"
                                    class="opacity-100 md:opacity-0 bg-transparent border-none text-primary cursor-pointer flex items-center justify-center min-w-[40px] min-h-[44px] text-lg transition-opacity duration-200 md:group-hover/tools:opacity-100 hover:scale-110 shrink-0" title="Fordítás">
                              <i v-if="translatingField === `${service.id}-name-${currentLang}`" class="pi pi-spin pi-spinner"></i>
                              <i v-else class="pi pi-sparkles"></i>
                            </button>
                          </div>

                          <div v-if="isLoggedIn" class="flex items-center gap-1 opacity-100 md:opacity-0 transition-opacity duration-200 md:group-hover/row:opacity-100 shrink-0">
                            <button @click="toggleNote(service)" class="border-none bg-transparent cursor-pointer flex items-center justify-center w-[36px] h-[44px] text-text-muted text-base hover:text-primary transition-colors"><i class="pi pi-comment"></i></button>
                            <button @click="addVariantToService(service, group)" class="border-none bg-transparent cursor-pointer flex items-center justify-center w-[36px] h-[44px] text-text-muted text-xl font-bold hover:text-text transition-colors">+</button>
                            <button @click="deleteService(service.id)" class="border-none bg-transparent cursor-pointer flex items-center justify-center w-[36px] h-[44px] text-text-muted text-lg hover:text-red-500 transition-colors">🗑</button>
                          </div>
                        </div>

                        <div class="flex justify-end gap-2 md:gap-4 shrink-0 pr-2">
                          <div v-for="(variant, vIndex) in service.variants" :key="variant.id || vIndex" class="w-[90px] md:w-[120px] flex items-center justify-center relative text-center group/variant min-h-[44px] shrink-0">

                            <div class="w-full flex justify-center">
                              <InputNumber v-if="isLoggedIn"
                                           v-model="variant.price"
                                           mode="currency" currency="EUR" locale="hu-HU" :minFractionDigits="0"
                                           class="w-full max-w-[90px] md:max-w-[100px] [&_input]:border-none [&_input]:bg-background [&_input]:text-center [&_input]:text-text [&_input]:min-h-[38px] [&_input]:w-full [&_input]:focus:ring-1 [&_input]:focus:ring-primary [&_input]:transition-all [&_input]:rounded-md [&_input]:shadow-inner"
                                           @update:modelValue="saveService(service, false)"
                                           @blur="saveService(service, false)" />
                              <span v-else class="text-text font-inherit transition-colors">{{ formatCurrency(variant.price) }}</span>
                            </div>

                            <button v-if="isLoggedIn" @click="removeVariant(service, vIndex, group)" class="absolute -top-2 md:-top-3 right-0 border-none bg-transparent text-red-500 opacity-100 md:opacity-0 cursor-pointer flex items-center justify-center w-[24px] h-[24px] md:group-hover/variant:opacity-100 transition-opacity text-xl font-bold hover:scale-110 bg-surface rounded-full shadow-sm">&times;</button>
                          </div>
                        </div>
                      </div>

                      <div v-if="service.description && (service.description[currentLang] || (isLoggedIn && service.description[company?.defaultLanguage || 'hu']))"
                           class="flex items-start pl-[40px] md:pl-[50px] pr-2 pb-3 w-full relative group/note"
                           :draggable="isLoggedIn"
                           @dragstart="(e) => onNoteDragStart(e, service)"
                           @dragend="onNoteDragEnd">

                        <div v-if="isLoggedIn" class="absolute left-2 top-1 cursor-grab text-text-muted/50 text-xs flex items-center justify-center w-[24px] h-[24px] hover:text-primary transition-colors"><i class="pi pi-arrows-alt"></i></div>

                        <div class="flex-grow border-l-[3px] border-primary/30 pl-3 relative flex items-center group/tools bg-background rounded-r-md">
                          <textarea v-if="isLoggedIn"
                                    v-model="service.description[currentLang]"
                                    @change="saveService(service, false)"
                                    @input="autoResize"
                                    class="w-full border-none bg-transparent italic text-sm text-text-muted resize-none overflow-hidden leading-relaxed focus:outline-none focus:text-text py-2 px-1 min-h-[40px] transition-colors"
                                    :placeholder="(currentLang !== (company?.defaultLanguage || 'hu') && service.description[company?.defaultLanguage || 'hu']) ? service.description[company?.defaultLanguage || 'hu'] : $t('services.notePlaceholder')"></textarea>
                          <span v-else class="block w-full whitespace-pre-wrap break-words leading-relaxed italic text-sm text-text-muted group-hover/note:text-text transition-colors py-2 px-1">{{ service.description[currentLang] }}</span>

                          <button v-if="isLoggedIn"
                                  @click="translateServiceField(service, 'description')"
                                  class="opacity-100 md:opacity-0 bg-transparent border-none text-primary cursor-pointer flex items-center justify-center min-w-[40px] min-h-[40px] text-lg transition-opacity duration-200 md:group-hover/tools:opacity-100 hover:scale-110 shrink-0" title="Fordítás">
                            <i v-if="translatingField === `${service.id}-description-${currentLang}`" class="pi pi-spin pi-spinner"></i>
                            <i v-else class="pi pi-sparkles"></i>
                          </button>
                        </div>
                      </div>

                    </div>
                  </template>
                </draggable>

              </div>
            </div>

            <div v-if="isLoggedIn" class="bg-background border-t border-text/10 p-2 md:p-3">
              <button @click="addServiceToGroupEnd(group)" class="bg-transparent border border-dashed border-text/30 text-text-muted w-full min-h-[44px] cursor-pointer rounded-lg text-sm transition-all duration-200 hover:bg-text/5 hover:text-primary hover:border-primary/50 flex items-center justify-center font-medium">
                <i class="pi pi-plus mr-2"></i> {{ $t('services.addService') }}
              </button>
            </div>

          </div>
        </template>
      </draggable>

    </div>
  </div>
</template>
