<script setup>
  import { ref, inject, watch, computed, nextTick } from 'vue';
  import InputNumber from 'primevue/inputnumber';
  import apiClient from '@/services/api';
  import { DEFAULT_COMPANY_ID } from '@/config';
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
      el.classList.remove('!border-primary', '!bg-text/5');
    });
  };

  const onNoteDragOver = (event) => {
    event.preventDefault();
    event.currentTarget.classList.add('!border-primary', '!bg-text/5');
  };

  const onNoteDragLeave = (event) => {
    event.currentTarget.classList.remove('!border-primary', '!bg-text/5');
  };

  const onNoteDrop = async (event, targetService) => {
    event.preventDefault();
    event.currentTarget.classList.remove('!border-primary', '!bg-text/5');

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
  <div class="max-w-[1000px] mx-auto p-5 box-border bg-background text-text min-h-screen" :style="{
    '--primary-color': company?.primaryColor || '#d4af37',
    '--secondary-color': company?.secondaryColor || '#1a1a1a'
  }">
    <div class="flex justify-between items-center mb-8">
      <h2 class="font-light text-text m-0 tracking-wide text-3xl">{{ isLoggedIn ? $t('services.editorTitle') : $t('services.title') }}</h2>

      <button v-if="isLoggedIn" @click="createNewCategory" class="bg-primary text-black border-none px-5 py-2.5 rounded-md cursor-pointer font-bold flex items-center gap-2 transition-all hover:brightness-90 shadow-sm">
        <i class="pi pi-folder-open"></i> {{ $t('services.newCategory') }}
      </button>
    </div>

    <div v-if="loading" class="text-text-muted">
      <i class="pi pi-spin pi-spinner mr-2"></i>{{ $t('common.loading') }}
    </div>

    <div v-else class="pb-12">

      <draggable v-model="categories" item-key="id" handle=".drag-handle-cat" @change="onCategoryDragChange" :disabled="!isLoggedIn">
        <template #item="{ element: group }">

          <div class="mb-10 bg-text/10 border border-text/10 rounded-2xl p-4 md:p-6 shadow-xl">

            <div class="flex items-end border-b border-text/20 pb-4 mb-5">
              <div v-if="isLoggedIn" class="cursor-grab text-2xl text-primary mr-4 drag-handle-cat transition-colors hover:text-primary/80" title="Kategória mozgatása">⋮⋮</div>

              <div class="flex-grow flex items-center pr-4 min-w-[180px] overflow-hidden">
                <div class="relative w-full flex items-center group/tools">
                  <input v-if="isLoggedIn"
                         v-model="group.categoryName[currentLang]"
                         @change="updateCategoryName(group)"
                         class="text-xl font-bold text-primary border-none bg-transparent w-full uppercase tracking-widest focus:outline-none focus:border-b focus:border-primary transition-colors"
                         :placeholder="$t('services.categoryNamePlaceholder')" />
                  <span v-else class="text-xl font-bold text-primary uppercase tracking-widest">{{ group.categoryName[currentLang] }}</span>

                  <button v-if="isLoggedIn"
                          @click="translateCategoryName(group)"
                          class="opacity-30 bg-transparent border-none text-primary cursor-pointer ml-1.5 text-base transition-opacity duration-200 group-hover/tools:opacity-100 hover:scale-110" title="Fordítás">
                    <i v-if="translatingField === `${group.id || 'cat'}-categoryName-${currentLang}`" class="pi pi-spin pi-spinner"></i>
                    <i v-else class="pi pi-sparkles"></i>
                  </button>
                </div>
              </div>

              <div class="flex justify-end gap-4 shrink-0">
                <div v-for="(v, vIndex) in group.headerVariants" :key="vIndex" class="w-[130px] flex items-end justify-center relative text-center min-h-[40px] pb-0">

                  <div v-if="isLoggedIn" class="relative w-full flex items-center group/tools">
                    <textarea v-model="v.variantName[currentLang]"
                              @change="updateGroupVariantName(group, vIndex)"
                              class="w-full text-center border-none bg-transparent font-semibold text-text-muted text-sm uppercase resize-none overflow-hidden focus:bg-text/5 focus:outline focus:outline-1 focus:outline-primary focus:text-text rounded transition-colors"
                              rows="2"></textarea>

                    <button v-if="isLoggedIn"
                            @click="translateHeaderVariant(group, v, vIndex)"
                            class="opacity-30 bg-transparent border-none text-primary cursor-pointer ml-1 text-xs transition-opacity duration-200 group-hover/tools:opacity-100 hover:scale-110" title="Fordítás">
                      <i v-if="translatingField === `header-${vIndex}-variantName-${currentLang}`" class="pi pi-spin pi-spinner"></i>
                      <i v-else class="pi pi-sparkles"></i>
                    </button>
                  </div>

                  <span v-else class="font-semibold text-text-muted text-sm uppercase text-center block w-full whitespace-normal break-words">{{ v.variantName[currentLang] }}</span>

                </div>
              </div>
            </div>

            <draggable v-model="group.items" item-key="id" group="services" handle=".drag-handle-item" @change="(e) => onServiceDragChange(e, group)" :disabled="!isLoggedIn">
              <template #item="{ element: service }">

                <div class="service-drop-zone bg-background border border-text/10 rounded-xl mb-3 shadow-sm transition-all duration-200 hover:border-primary/40 hover:shadow-md overflow-hidden" @dragover="onNoteDragOver" @dragleave="onNoteDragLeave" @drop="(e) => onNoteDrop(e, service)">

                  <div class="flex items-center p-3 sm:p-4 group/row transition-colors relative">
                    <div v-if="isLoggedIn" class="cursor-grab text-text-muted mr-2.5 text-lg flex items-center h-full hover:text-primary drag-handle-item transition-colors">⋮⋮</div>

                    <div class="flex-grow flex items-center pr-4 min-w-[180px] overflow-hidden">
                      <div class="relative w-full flex items-center group/tools">
                        <textarea v-if="isLoggedIn"
                                  v-model="service.name[currentLang]"
                                  @change="saveService(service, false)"
                                  @input="autoResize"
                                  class="w-full border-none bg-transparent text-base text-text resize-none overflow-hidden leading-snug p-0 focus:outline-none focus:border-b focus:border-primary transition-colors"
                                  rows="1"></textarea>
                        <span v-else class="text-base text-text whitespace-normal break-words leading-snug block w-full group-hover/row:text-primary transition-colors">{{ service.name[currentLang] }}</span>

                        <button v-if="isLoggedIn"
                                @click="translateServiceField(service, 'name')"
                                class="opacity-30 bg-transparent border-none text-primary cursor-pointer ml-1.5 text-base transition-opacity duration-200 group-hover/tools:opacity-100 hover:scale-110" title="Fordítás">
                          <i v-if="translatingField === `${service.id}-name-${currentLang}`" class="pi pi-spin pi-spinner"></i>
                          <i v-else class="pi pi-sparkles"></i>
                        </button>
                      </div>

                      <div v-if="isLoggedIn" class="flex items-center gap-1.5 ml-4 opacity-0 transition-opacity duration-200 group-hover/row:opacity-100">
                        <button @click="toggleNote(service)" class="border-none bg-transparent cursor-pointer text-text-muted text-base hover:text-primary transition-colors"><i class="pi pi-comment"></i></button>
                        <span class="text-text/30 mx-1">|</span>
                        <button @click="addVariantToService(service, group)" class="border-none bg-transparent cursor-pointer text-text-muted text-lg font-bold hover:text-text transition-colors">+</button>
                        <button @click="deleteService(service.id)" class="border-none bg-transparent cursor-pointer text-text-muted text-base hover:text-red-500 transition-colors">🗑</button>
                      </div>
                    </div>

                    <div class="flex justify-end gap-4 shrink-0">
                      <div v-for="(variant, vIndex) in service.variants" :key="variant.id || vIndex" class="w-[130px] flex items-start justify-center relative text-center group/variant">

                        <div class="w-full">
                          <InputNumber v-if="isLoggedIn"
                                       v-model="variant.price"
                                       mode="currency" currency="EUR" locale="hu-HU" :minFractionDigits="0"
                                       class="w-[100px] [&_input]:border-none [&_input]:bg-transparent [&_input]:text-center [&_input]:text-text-muted [&_input]:p-0 [&_input]:focus:bg-text/10 [&_input]:focus:ring-1 [&_input]:focus:ring-primary [&_input]:focus:text-text [&_input]:transition-all [&_input]:rounded"
                                       @update:modelValue="saveService(service, false)"
                                       @blur="saveService(service, false)" />
                          <span v-else class="text-text-muted font-inherit group-hover/row:text-text group-hover/row:font-medium transition-colors">{{ formatCurrency(variant.price) }}</span>
                        </div>

                        <button v-if="isLoggedIn" @click="removeVariant(service, vIndex, group)" class="absolute -top-2 right-0 border-none bg-transparent text-red-500 opacity-0 cursor-pointer group-hover/variant:opacity-100 transition-opacity text-lg font-bold hover:scale-110">&times;</button>
                      </div>
                    </div>
                  </div>

                  <div v-if="service.description && (service.description[currentLang] || (isLoggedIn && service.description[company?.defaultLanguage || 'hu']))"
                       class="flex items-start p-3 sm:p-4 pl-12 sm:pl-14 bg-text/5 border-t border-text/5 relative group/note"
                       :draggable="isLoggedIn"
                       @dragstart="(e) => onNoteDragStart(e, service)"
                       @dragend="onNoteDragEnd">

                    <div v-if="isLoggedIn" class="absolute left-4 top-4 cursor-grab text-text-muted text-sm hover:text-primary transition-colors"><i class="pi pi-arrows-alt"></i></div>

                    <div class="flex-grow border-l-2 border-text/20 pl-3 relative flex items-center group/tools">
                      <textarea v-if="isLoggedIn"
                                v-model="service.description[currentLang]"
                                @change="saveService(service, false)"
                                @input="autoResize"
                                class="w-full border-none bg-transparent italic text-text-muted resize-none overflow-hidden leading-relaxed focus:outline-none focus:bg-background focus:text-text p-2 rounded transition-colors"
                                :placeholder="(currentLang !== (company?.defaultLanguage || 'hu') && service.description[company?.defaultLanguage || 'hu']) ? service.description[company?.defaultLanguage || 'hu'] : $t('services.notePlaceholder')"></textarea>
                      <span v-else class="block w-full whitespace-pre-wrap break-words leading-relaxed italic text-text-muted group-hover/note:text-text transition-colors">{{ service.description[currentLang] }}</span>

                      <button v-if="isLoggedIn"
                              @click="translateServiceField(service, 'description')"
                              class="opacity-30 bg-transparent border-none text-primary cursor-pointer ml-2 text-base transition-opacity duration-200 group-hover/tools:opacity-100 hover:scale-110" title="Fordítás">
                        <i v-if="translatingField === `${service.id}-description-${currentLang}`" class="pi pi-spin pi-spinner"></i>
                        <i v-else class="pi pi-sparkles"></i>
                      </button>
                    </div>
                  </div>
                </div>

              </template>
            </draggable>

            <div v-if="isLoggedIn" class="flex gap-2.5 mt-4">
              <button @click="addServiceToGroupEnd(group)" class="bg-transparent border border-dashed border-text/30 text-text-muted w-full p-2.5 cursor-pointer rounded-lg text-sm transition-all duration-200 hover:bg-background hover:text-primary hover:border-primary/50 shadow-sm">
                {{ $t('services.addService') }}
              </button>
            </div>

          </div>
        </template>
      </draggable>

    </div>
  </div>
</template>
