<script setup>
// @ts-nocheck
  import { ref, inject, watch, computed, nextTick, onMounted } from 'vue';
  import InputNumber from 'primevue/inputnumber';
  import ToggleSwitch from 'primevue/toggleswitch';
  import apiClient from '@/services/api';
  import draggable from 'vuedraggable';
  import { useI18n } from 'vue-i18n';
  import { useCompanyStore } from '@/stores/companyStore';
  import { useDragAndDrop } from '@/composables/useDragAndDrop';
  import { useTranslation } from '@/composables/useTranslation';
  import { useAutoSaveQueue } from '@/composables/useAutoSaveQueue';
  import ServiceRecipeModal from '@/components/admin/ServiceRecipeModal.vue';
  import ServicePictogram from '@/components/common/ServicePictogram.vue';
  import { pickLocalized } from '@/utils/localizedText';
  import { DEFAULT_LANG } from '@/router/routeSlugs';

  const { t, locale } = useI18n();
  const isLoggedIn = inject('isLoggedIn');
  const companyStore = useCompanyStore();
  const services = ref([]);
  const categories = ref([]);
  const isRecipeModalOpen = ref(false);
  const editingRecipeVariant = ref(null);
  const editingRecipeServiceName = ref("");
  const editingRecipeVariantName = ref("");

  const openRecipeModal = (service, variant, group, vIndex) => {
    editingRecipeVariant.value = variant;
    editingRecipeServiceName.value = service.name[currentLang.value] || "";
    editingRecipeVariantName.value = group.headerVariants[vIndex]?.variantName[currentLang.value] || "";
    isRecipeModalOpen.value = true;
  };

  const onRecipeSaved = (updatedProducts) => {
    if (editingRecipeVariant.value) {
      editingRecipeVariant.value.defaultProducts = updatedProducts;
    }
  };
  const companyAttributes = ref([]); // ÚJ: Vendég jellemzők betöltése
  const loading = ref(true);

  const currentLang = computed(() => locale.value);
  const company = inject('company', ref(null));
  const { addToQueue } = useAutoSaveQueue();

  const { reorderNestedItems } = useDragAndDrop();
  const { translatingField, translateField } = useTranslation();

  // --- ÚJ: Profil Módosító Modal Állapota ---
  const isModifierModalOpen = ref(false);
  const editingVariant = ref(null);
  const editingService = ref(null);

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
    return val.toLocaleString(locale.value, { style: 'currency', currency: 'EUR', maximumFractionDigits: 0 });
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
    const defaultLang = company.value?.defaultLanguage || DEFAULT_LANG;

    flatServices.sort((a, b) => a.orderIndex - b.orderIndex);

    flatServices.forEach(service => {
      const catDict = ensureDict(service.category, "Egyéb");
      const catNameStr = pickLocalized(catDict, defaultLang, defaultLang, "Egyéb");

      let group = groups.find(g => {
        const gName = pickLocalized(g.categoryName, defaultLang, defaultLang, "");
        return gName === catNameStr;
      });

      if (!group) {
        group = {
          id: 'cat-' + catNameStr.replace(/\s+/g, '-').toLowerCase(),
          categoryName: catDict,
          categoryPictogram: service.categoryPictogram || null,
          isBookable: service.isBookable !== false,
          headerVariants: [],
          items: []
        };
        groups.push(group);
      }

      if (!group.categoryPictogram && service.categoryPictogram) {
        group.categoryPictogram = service.categoryPictogram;
      }

      if (group.headerVariants.length === 0 && service.variants && service.variants.length > 0) {
        group.headerVariants = JSON.parse(JSON.stringify(service.variants));
      }
      group.items.push(service);
      group.isBookable = group.items.some(s => s.isBookable !== false);
    });
    return groups;
  };

  const processServices = (serviceList) => {
    serviceList.forEach(service => {
      const fallbackDur = Number(service.defaultDuration) > 0 ? Number(service.defaultDuration) : 30;
      if (service.variants) {
        service.variants = sortVariants(service.variants);
        service.variants.forEach(v => {
          if (v.price === 0) v.price = null;
          v.duration = Number(v.duration) > 0 ? Number(v.duration) : fallbackDur;
          v.variantName = ensureDict(v.variantName, "Extra");
          if (!v.profileModifiers) v.profileModifiers = {};
        });
      }
      service.name = ensureDict(service.name, "Névtelen");
      service.category = ensureDict(service.category, "Egyéb");
      service.description = ensureDict(service.description, "");
      if (service.isBookable === undefined || service.isBookable === null) {
        service.isBookable = true;
      }
    });
    categories.value = buildNestedStructure(serviceList);
  };

  const fetchServicesAndAttributes = async () => {
    loading.value = true;
    try {
      // API hívások párhuzamosan
      const [servicesRes, attrRes] = await Promise.all([
        apiClient.get('/api/Service'),
        isLoggedIn.value ? apiClient.get('/api/company-attributes').catch(() => ({ data: [] })) : { data: [] }
      ]);

      const rawServices = servicesRes.data;
      if (!Array.isArray(rawServices)) {
        if (rawServices && Array.isArray(rawServices.$values)) {
          processServices(rawServices.$values);
        }
      } else {
        processServices(rawServices);
      }

      if (isLoggedIn.value) {
        companyAttributes.value = (attrRes.data.$values || attrRes.data || []).filter(a => a.isActive);
      }

    } catch (error) { console.error('Hiba a betolteskor:', error); }
    finally {
      loading.value = false;
      resizeAllTextareas();
    }
  };

  watch(() => company?.value?.id, (newId) => { if (newId) fetchServicesAndAttributes(); }, { immediate: true });

  const saveService = async (serviceItem, refreshLocal = false) => {
    return addToQueue(serviceItem.id, async () => {
      try {
        const payload = JSON.parse(JSON.stringify(serviceItem));
        if (!payload.variants) payload.variants = [];
        else {
          payload.variants.forEach(v => {
            if (v.price === null || v.price === undefined || v.price === '') v.price = 0;
            else v.price = Number(v.price);
            v.duration = Math.max(5, Number(v.duration) || 30);
          });
          const activeDurs = payload.variants.filter(v => v.price > 0 && v.duration > 0).map(v => v.duration);
          payload.defaultDuration = activeDurs.length > 0 ? activeDurs[0] : (payload.variants[0]?.duration || payload.defaultDuration || 30);
          serviceItem.defaultDuration = payload.defaultDuration;
        }
        const response = await apiClient.put(`/api/Service/${serviceItem.id}`, payload);
        if (refreshLocal && response.status === 200) {
          const updated = response.data;
          const fallbackDur = Number(updated.defaultDuration) > 0 ? Number(updated.defaultDuration) : 30;
          if (updated.variants) {
            updated.variants = sortVariants(updated.variants);
            updated.variants.forEach(v => {
              if (v.price === 0) v.price = null;
              v.duration = Number(v.duration) > 0 ? Number(v.duration) : fallbackDur;
              v.variantName = ensureDict(v.variantName);
              if (!v.profileModifiers) v.profileModifiers = {};
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
  };

  const reorderAll = async () => {
    await reorderNestedItems(
      categories.value,
      'items',
      saveService,
      (item, group) => {
        const catLang = company.value?.defaultLanguage || DEFAULT_LANG;
        const itemCatName = pickLocalized(item.category, catLang, catLang);
        const groupCatName = pickLocalized(group.categoryName, catLang, catLang);
        if (itemCatName !== groupCatName) {
          item.category = JSON.parse(JSON.stringify(group.categoryName));
          item.categoryPictogram = group.categoryPictogram || null;
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

  const toggleCategoryBookable = async (group) => {
    const newVal = !!group.isBookable;
    const promises = group.items.map(service => {
      service.isBookable = newVal;
      return saveService(service, false);
    });
    await Promise.all(promises);
  };

  const toggleServiceBookable = async (service, group) => {
    service.isBookable = !service.isBookable;
    if (group) {
      group.isBookable = group.items.some(s => s.isBookable !== false);
    }
    await saveService(service, false);
  };

  const generatingPictogramKey = ref(null);
  const generatingAllPictograms = ref(false);
  const generatingProgress = ref({ current: 0, total: 0 });
  const selectedPictogramModel = ref('gpt-6-astra'); // 'gpt-6-astra' | 'gpt-6.1-sol'
  const categoryPictogramHistory = ref({});

  const getHistoryStorageKey = () => `soluvion_cat_pic_history_${company.value?.id || 'default'}`;

  const loadPictogramHistory = () => {
    try {
      const raw = localStorage.getItem(getHistoryStorageKey());
      if (raw) {
        categoryPictogramHistory.value = JSON.parse(raw) || {};
      }
    } catch {
      categoryPictogramHistory.value = {};
    }
  };

  const savePictogramHistory = () => {
    try {
      localStorage.setItem(getHistoryStorageKey(), JSON.stringify(categoryPictogramHistory.value));
    } catch {
      // Ignore storage quota errors
    }
  };

  const pushToGroupHistory = (groupId, svg) => {
    if (!groupId || !svg || !svg.trim()) return;
    const trimmed = svg.trim();
    const existing = Array.isArray(categoryPictogramHistory.value[groupId])
      ? [...categoryPictogramHistory.value[groupId]]
      : [];
    const filtered = existing.filter(item => item !== trimmed);
    filtered.push(trimmed);
    // Csak az utolsó 3 generált piktogramot őrizzük meg kategóriánként
    categoryPictogramHistory.value[groupId] = filtered.slice(-3);
    savePictogramHistory();
  };

  const getGroupHistory = (group) => {
    if (!group?.id) return [];
    const list = categoryPictogramHistory.value[group.id] || [];
    if (group.categoryPictogram && !list.includes(group.categoryPictogram.trim())) {
      return [...list, group.categoryPictogram.trim()].slice(-3);
    }
    return list;
  };

  watch(() => company?.value?.id, () => {
    loadPictogramHistory();
  }, { immediate: true });

  const getGroupSiblingNames = (group) => {
    const defaultLang = company.value?.defaultLanguage || DEFAULT_LANG;
    return (group?.items || [])
      .map(s => pickLocalized(s.name, currentLang.value, defaultLang, ''))
      .filter(Boolean);
  };

  const generateCategoryPictogram = async (group) => {
    if (!isLoggedIn.value || generatingPictogramKey.value) return;
    const defaultLang = company.value?.defaultLanguage || DEFAULT_LANG;
    const catName = pickLocalized(group.categoryName, currentLang.value, defaultLang, 'Service');
    const key = `cat-${group.id}`;
    generatingPictogramKey.value = key;

    // Ha már volt piktogramja, biztosítjuk, hogy bekerüljön az előzményekbe
    if (group.categoryPictogram) {
      pushToGroupHistory(group.id, group.categoryPictogram);
    }

    const recentSvgs = getGroupHistory(group);
    const previousSvgsCombined = recentSvgs.length > 0 ? recentSvgs.join('\n---\n') : (group.categoryPictogram || null);

    try {
      const res = await apiClient.post('/api/Service/generate-pictogram', {
        categoryName: catName,
        isCategoryLevel: true,
        mode: 'svg',
        preferredModel: selectedPictogramModel.value,
        previousSvg: previousSvgsCombined,
        siblingServices: getGroupSiblingNames(group)
      });

      const finalSvg = res.data?.svg || '';
      if (finalSvg) {
        group.categoryPictogram = finalSvg;
        group.items.forEach(s => { s.categoryPictogram = finalSvg; });
        pushToGroupHistory(group.id, finalSvg);
      }
    } catch (err) {
      console.error('Failed to generate category pictogram:', err);
    } finally {
      generatingPictogramKey.value = null;
    }
  };

  const selectCategoryPictogramFromHistory = async (group, svg) => {
    if (!isLoggedIn.value || !svg || group.categoryPictogram === svg) return;
    group.categoryPictogram = svg;
    await Promise.all(group.items.map(s => {
      s.categoryPictogram = svg;
      return saveService(s, false);
    }));
  };

  const deleteCategoryPictogram = async (group) => {
    if (!isLoggedIn.value) return;
    group.categoryPictogram = null;
    await Promise.all(group.items.map(s => {
      s.categoryPictogram = '';
      return saveService(s, false);
    }));
  };

  const generateMissingPictograms = async () => {
    if (!isLoggedIn.value || generatingAllPictograms.value) return;
    const missingGroups = categories.value.filter(g => !g.categoryPictogram || !g.categoryPictogram.trim());
    if (missingGroups.length === 0) return;

    generatingAllPictograms.value = true;
    generatingProgress.value = { current: 0, total: missingGroups.length };
    try {
      for (const group of missingGroups) {
        generatingProgress.value.current++;
        await generateCategoryPictogram(group);
      }
    } catch (err) {
      console.error('Failed to generate missing pictograms:', err);
    } finally {
      generatingAllPictograms.value = false;
      generatingProgress.value = { current: 0, total: 0 };
    }
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
      name: { [currentLang.value]: t('services.newServiceName') },
      category: { [currentLang.value]: t('services.newCategoryName') },
      defaultPrice: 0,
      orderIndex: 99999,
      isBookable: true,
      variants: [{ variantName: { [currentLang.value]: t('services.normal') }, price: 0, duration: 30, profileModifiers: {} }],
      description: { [currentLang.value]: "" }
    };
    await postNewService(newService);
  };

  const addServiceToGroupEnd = async (group) => {
    let variants = [{ variantName: { [currentLang.value]: t('services.normal') }, price: 0, duration: 30, profileModifiers: {} }];
    if (group.headerVariants && group.headerVariants.length > 0) {
      variants = group.headerVariants.map(v => ({
        variantName: JSON.parse(JSON.stringify(v.variantName)),
        price: 0,
        duration: v.duration,
        profileModifiers: {}
      }));
    }
    const newService = {
      name: { [currentLang.value]: t('services.newServiceName') },
      category: JSON.parse(JSON.stringify(group.categoryName)),
      categoryPictogram: group.categoryPictogram || null,
      isBookable: group.isBookable !== false,
      defaultPrice: 0,
      orderIndex: 99999,
      variants: variants,
      description: { [currentLang.value]: "" }
    };
    const res = await postNewService(newService);
    if (res?.id) {
      activeServiceId.value = res.id;
    }
  };

  const postNewService = async (dto) => {
    try {
      const payload = JSON.parse(JSON.stringify(dto));
      if (!payload.variants) payload.variants = [];
      payload.variants.forEach(v => { v.price = 0; });
      const res = await apiClient.post('/api/Service', payload);
      await fetchServicesAndAttributes();
      return res.data;
    } catch (err) {
      console.error(err);
      return null;
    }
  };

  const deleteService = async (id) => {
    if (!confirm(t('services.confirmDelete'))) return;
    try {
      await apiClient.delete(`/api/Service/${id}`);
      if (activeServiceId.value === id) activeServiceId.value = null;
      await fetchServicesAndAttributes();
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
      variantName: { [currentLang.value]: t('services.extra') },
      price: 0,
      duration: 30,
      profileModifiers: {}
    });
    if (group) group.headerVariants = [...service.variants];
    await saveService(service, true);
  };

  // --- ÚJ: Globális Beállítások mód, Soronkénti Beállítások Panel és Jelmagyarázat ---
  const isGlobalEditMode = ref(false);
  const activeServiceId = ref(null);
  const showLegendHelp = ref(false);

  const toggleGlobalEditMode = async () => {
    isGlobalEditMode.value = !isGlobalEditMode.value;
    await resizeAllTextareas();
  };

  const toggleServiceEditor = async (service) => {
    if (activeServiceId.value === service.id) {
      activeServiceId.value = null;
    } else {
      activeServiceId.value = service.id;
      if (!service.description) {
        service.description = { [currentLang.value]: '' };
      }
      await resizeAllTextareas();
    }
  };

  const stepVariantDuration = (service, variant, delta) => {
    const current = Number(variant.duration) || 30;
    variant.duration = Math.max(5, Math.min(600, current + delta));
    saveService(service, false);
  };

  // --- Profil Módosító Modal Kezelése ---
  const editingGroup = ref(null);
  const editingVariantIndex = ref(null);

  const openModifierModal = (group, vIndex) => {
    editingGroup.value = group;
    editingVariantIndex.value = vIndex;
    editingVariant.value = group.headerVariants[vIndex];
    if (!editingVariant.value.profileModifiers) editingVariant.value.profileModifiers = {};
    isModifierModalOpen.value = true;
  };

  const updateGroupVariantModifiers = async (group, variantIndex) => {
    const sourceModifiers = group.headerVariants[variantIndex].profileModifiers || {};
    const promises = group.items.map(service => {
      if (service.variants && service.variants[variantIndex]) {
        // Klónozzuk a szabályt a fejlécből az adott szolgáltatás variánsába
        service.variants[variantIndex].profileModifiers = JSON.parse(JSON.stringify(sourceModifiers));
        return saveService(service, false);
      }
      return Promise.resolve();
    });
    await Promise.all(promises);
  };

  const closeModifierModal = async () => {
    // Üres értékek takarítása mentés előtt
    if (editingVariant.value && editingVariant.value.profileModifiers) {
      for (const key in editingVariant.value.profileModifiers) {
        if (!editingVariant.value.profileModifiers[key] || editingVariant.value.profileModifiers[key].trim() === '') {
          delete editingVariant.value.profileModifiers[key];
        }
      }
    }

    isModifierModalOpen.value = false;

    // Ha a fejlécben módosítottuk, mentsük el az összes alá tartozó szolgáltatásba!
    if (editingGroup.value && editingVariantIndex.value !== null) {
      await updateGroupVariantModifiers(editingGroup.value, editingVariantIndex.value);
    }

    editingVariant.value = null;
    editingGroup.value = null;
    editingVariantIndex.value = null;
  };
</script>

<template>
  <div class="max-w-5xl w-full mx-auto px-2.5 sm:px-4 py-4 md:p-8 box-border bg-background text-text min-h-screen overflow-x-hidden" :style="{
    '--primary-color': company?.primaryColor || '#d4af37',
    '--secondary-color': company?.secondaryColor || '#1a1a1a'
  }">

    <!-- FEJLÉC: Cím + Adminban Jelmagyarázat (💡) és Globális Beállítások (⚙️) gomb -->
    <div class="flex flex-col gap-3 sm:gap-4 mb-6 md:mb-8">
      <div class="flex flex-wrap justify-between items-center gap-2.5">
        <h2 class="font-light text-text m-0 tracking-wide text-2xl sm:text-3xl">
          {{ (isLoggedIn && isGlobalEditMode) ? $t('services.editorTitle') : $t('services.title') }}
        </h2>

        <div v-if="isLoggedIn" class="flex items-center gap-2 flex-wrap">
          <!-- 💡 / (i) Jelmagyarázat gomb -->
          <button type="button"
                  @click="showLegendHelp = true"
                  class="px-3 py-2 min-h-[38px] sm:min-h-[42px] rounded-xl border border-primary/35 bg-primary/10 text-primary hover:bg-primary/20 font-semibold text-xs sm:text-sm flex items-center gap-1.5 cursor-pointer transition-all shadow-xs">
            <i class="pi pi-lightbulb text-xs sm:text-sm"></i>
            <span>{{ $t('services.helpLegendBtn') }}</span>
          </button>

          <!-- ⚙️ Globális / Oldal beállítások kapcsoló gomb -->
          <button type="button"
                  @click="toggleGlobalEditMode"
                  class="px-3.5 py-2 min-h-[38px] sm:min-h-[42px] rounded-xl border font-bold text-xs sm:text-sm flex items-center gap-1.5 cursor-pointer transition-all shadow-xs"
                  :class="isGlobalEditMode
                    ? 'bg-primary text-black border-primary shadow-md'
                    : 'bg-surface text-text border-text/15 hover:border-primary/50 hover:text-primary'">
            <i class="pi pi-cog text-xs sm:text-sm" :class="{ 'animate-spin-slow': isGlobalEditMode }"></i>
            <span>{{ isGlobalEditMode ? $t('services.globalSettingsActive') : $t('services.globalSettingsBtn') }}</span>
          </button>
        </div>
      </div>

      <!-- GLOBÁLIS ESZKÖZTÁR (Csak akkor látszik, ha a felső ⚙️ Oldal beállítások be van kapcsolva) -->
      <div v-if="isLoggedIn && isGlobalEditMode"
           class="bg-surface border border-primary/30 rounded-2xl p-3 sm:p-4 shadow-md flex flex-wrap items-center justify-between gap-2.5 animate-fade-in">
        <div class="flex flex-wrap items-center gap-2 w-full sm:w-auto">
          <!-- AI Modell Választó (GPT-6 Astra vs GPT-6.1 Sol) -->
          <div class="inline-flex items-center bg-background border border-text/15 rounded-xl p-1 min-h-[40px] w-full sm:w-auto justify-center">
            <button type="button"
                    @click="selectedPictogramModel = 'gpt-6-astra'"
                    class="flex-1 sm:flex-initial px-3 py-1.5 rounded-lg text-xs font-bold transition-all cursor-pointer border-none flex items-center justify-center gap-1.5"
                    :class="selectedPictogramModel === 'gpt-6-astra' ? 'bg-primary text-black shadow-sm' : 'bg-transparent text-text-muted hover:text-text'">
              <i class="pi pi-sparkles text-[11px]"></i>
              <span>{{ $t('services.modelAstra') }}</span>
            </button>
            <button type="button"
                    @click="selectedPictogramModel = 'gpt-6.1-sol'"
                    class="flex-1 sm:flex-initial px-3 py-1.5 rounded-lg text-xs font-bold transition-all cursor-pointer border-none flex items-center justify-center gap-1.5"
                    :class="selectedPictogramModel === 'gpt-6.1-sol' ? 'bg-primary text-black shadow-sm' : 'bg-transparent text-text-muted hover:text-text'">
              <i class="pi pi-code text-[11px]"></i>
              <span>{{ $t('services.modelSol') }}</span>
            </button>
          </div>

          <button @click="generateMissingPictograms"
                  :disabled="generatingAllPictograms"
                  class="bg-background border border-primary/40 text-primary px-3.5 py-2 min-h-[40px] rounded-xl cursor-pointer font-semibold text-xs sm:text-sm flex items-center justify-center gap-2 transition-all hover:bg-primary/10 w-full sm:w-auto disabled:opacity-50">
            <i :class="generatingAllPictograms ? 'pi pi-spin pi-spinner' : 'pi pi-sparkles'"></i>
            <span>{{ generatingAllPictograms ? `${$t('services.generatingPictograms')} (${generatingProgress.current}/${generatingProgress.total})` : $t('services.generateMissingPictograms') }}</span>
          </button>
        </div>

        <button @click="createNewCategory"
                class="bg-primary text-black border-none px-4 py-2 min-h-[40px] rounded-xl cursor-pointer font-bold text-xs sm:text-sm flex items-center justify-center gap-2 transition-all hover:brightness-95 shadow-sm w-full sm:w-auto">
          <i class="pi pi-folder-open"></i> {{ $t('services.newCategory') }}
        </button>
      </div>
    </div>

    <div v-if="loading" class="text-text-muted flex items-center min-h-[44px]">
      <i class="pi pi-spin pi-spinner mr-2 text-xl"></i>{{ $t('common.loading') }}
    </div>

    <div v-else class="pb-12">

      <draggable v-model="categories" item-key="id" handle=".drag-handle-cat" @change="onCategoryDragChange" :disabled="!isLoggedIn || !isGlobalEditMode">
        <template #item="{ element: group }">

          <div class="mb-6 sm:mb-10 bg-surface border border-text/10 rounded-2xl shadow-md overflow-hidden flex flex-col w-full">

            <!-- KATEGÓRIA FEJLÉC -->
            <div class="bg-text/5 p-2.5 sm:p-3.5 md:p-4 border-b border-text/10 flex flex-wrap items-center justify-between gap-2.5">
              <div class="flex items-center flex-1 min-w-0">
                <div v-if="isLoggedIn && isGlobalEditMode" class="cursor-grab text-xl sm:text-2xl text-primary flex items-center justify-center min-w-[30px] min-h-[34px] drag-handle-cat transition-colors hover:text-primary/80 mr-1 shrink-0" :title="$t('services.moveCategory')">⋮⋮</div>

                <!-- Kategória Piktogram vezérlők (Globális szerkesztő módban AI generátor + előzmények + törlés, alapból tiszta ikon) -->
                <div v-if="isLoggedIn && isGlobalEditMode" class="flex items-center gap-1 mr-2 shrink-0">
                  <button @click="generateCategoryPictogram(group)"
                          type="button"
                          class="relative w-9 h-9 sm:w-10 sm:h-10 rounded-xl border border-primary/30 bg-primary/10 text-primary flex items-center justify-center shrink-0 cursor-pointer hover:bg-primary/20 transition-all group/catpic"
                          :title="$t('services.generateCategoryPictogram')">
                    <i v-if="generatingPictogramKey === `cat-${group.id}`" class="pi pi-spin pi-spinner text-sm"></i>
                    <ServicePictogram v-else :svg="group.categoryPictogram" sizeClass="w-5 h-5" />
                    <span class="absolute -bottom-1 -right-1 w-4 h-4 rounded-full bg-surface border border-primary/40 text-primary flex items-center justify-center text-[9px] opacity-80 group-hover/catpic:scale-110 transition-transform">
                      <i class="pi pi-sparkles"></i>
                    </span>
                  </button>

                  <!-- Utolsó 3 generált piktogram előzmény-választó -->
                  <div v-if="getGroupHistory(group).length > 1"
                       class="hidden sm:inline-flex items-center gap-1 bg-background/80 border border-primary/20 rounded-xl p-1"
                       :title="$t('services.recentPictogramsHint')">
                    <button v-for="(histSvg, hIdx) in getGroupHistory(group)"
                            :key="hIdx"
                            type="button"
                            @click="selectCategoryPictogramFromHistory(group, histSvg)"
                            class="w-7 h-7 rounded-lg flex items-center justify-center transition-all cursor-pointer border"
                            :class="group.categoryPictogram === histSvg
                              ? 'bg-primary text-surface border-primary shadow-xs scale-105'
                              : 'bg-surface/80 text-text-muted border-transparent hover:text-primary hover:border-primary/30'"
                            :title="`${$t('services.selectPictogramVariant')} #${hIdx + 1}`">
                      <ServicePictogram :svg="histSvg" sizeClass="w-4 h-4" />
                    </button>
                  </div>

                  <!-- Piktogram törlése gomb -->
                  <button v-if="group.categoryPictogram"
                          @click="deleteCategoryPictogram(group)"
                          type="button"
                          class="w-7 h-7 rounded-lg border border-red-500/25 bg-red-500/10 text-red-500 hover:bg-red-500 hover:text-white flex items-center justify-center shrink-0 cursor-pointer transition-all"
                          :title="$t('services.deletePictogram')">
                    <i class="pi pi-trash text-[11px]"></i>
                  </button>
                </div>

                <div v-else-if="group.categoryPictogram" class="w-8 h-8 sm:w-9 sm:h-9 rounded-xl bg-primary/10 text-primary flex items-center justify-center shrink-0 mr-2">
                  <ServicePictogram :svg="group.categoryPictogram" sizeClass="w-4 h-4 sm:w-5 sm:h-5" />
                </div>

                <div class="relative flex-1 min-w-0 flex items-center group/tools">
                  <input v-if="isLoggedIn && isGlobalEditMode"
                         v-model="group.categoryName[currentLang]"
                         @change="updateCategoryName(group)"
                         class="text-base sm:text-lg md:text-xl font-bold text-primary border-b border-primary/30 bg-background/60 rounded-lg w-full min-w-0 uppercase tracking-wider sm:tracking-widest focus:outline-none focus:border-primary transition-colors py-1 px-2"
                         :placeholder="$t('services.categoryNamePlaceholder')" />
                  <span v-else class="text-base sm:text-lg md:text-xl font-bold text-primary uppercase tracking-wider sm:tracking-widest py-0.5 px-1 break-words leading-snug">{{ group.categoryName[currentLang] }}</span>

                  <button v-if="isLoggedIn && isGlobalEditMode"
                          @click="translateCategoryName(group)"
                          class="ml-1.5 bg-primary/10 border border-primary/30 rounded-lg text-primary cursor-pointer flex items-center justify-center min-w-[34px] min-h-[34px] text-sm hover:bg-primary/20 transition-all shrink-0"
                          :title="$t('services.translate')">
                    <i v-if="translatingField === `${group.id || 'cat'}-categoryName-${currentLang}`" class="pi pi-spin pi-spinner"></i>
                    <i v-else class="pi pi-sparkles"></i>
                  </button>
                </div>
              </div>

              <!-- Kategória Online Foglalható kapcsoló (Globális szerkesztő módban szerkeszthető, alapból csak jelvény ha nem foglalható) -->
              <div v-if="isLoggedIn && isGlobalEditMode" class="flex items-center gap-2 bg-background/70 px-2.5 py-1 rounded-xl border border-text/10 shrink-0">
                <i class="pi text-xs" :class="group.isBookable ? 'pi-calendar-plus text-primary' : 'pi-info-circle text-text-muted'"></i>
                <span class="text-[11px] sm:text-xs font-semibold" :class="group.isBookable ? 'text-text' : 'text-text-muted'">
                  {{ group.isBookable ? $t('services.onlineBookable') : $t('services.notBookableInfoOnly') }}
                </span>
                <ToggleSwitch v-model="group.isBookable" @change="toggleCategoryBookable(group)" class="scale-75 origin-right" />
              </div>
              <span v-else-if="isLoggedIn && !group.isBookable"
                    class="inline-flex items-center gap-1 text-[10px] sm:text-[11px] font-semibold text-text-muted bg-background/70 px-2 py-1 rounded-lg border border-text/10 shrink-0">
                <i class="pi pi-info-circle text-[11px]"></i>
                <span>{{ $t('services.notBookableInfoOnly') }}</span>
              </span>
            </div>

            <div class="w-full bg-background">
              <div class="w-full flex flex-col">

                <!-- OSZLOPFEJLÉCEK SZERKESZTÉSE MOBILON (Ha a Globális Beállítások aktív) -->
                <div v-if="isLoggedIn && isGlobalEditMode"
                     class="sm:hidden border-b border-text/10 bg-text/5 p-2.5 space-y-2">
                  <div class="text-[10px] font-bold uppercase tracking-wider text-text-muted">
                    {{ $t('services.variantName') }} ({{ group.headerVariants?.length || 0 }})
                  </div>
                  <div class="grid grid-cols-1 gap-2">
                    <div v-for="(v, vIndex) in group.headerVariants"
                         :key="`mob-hdr-${vIndex}`"
                         class="flex items-center gap-1.5 bg-background border border-text/15 rounded-xl p-1.5">
                      <span class="text-[10px] font-bold text-text-muted px-1.5 shrink-0">#{{ vIndex + 1 }}</span>
                      <input v-model="v.variantName[currentLang]"
                             @change="updateGroupVariantName(group, vIndex)"
                             class="flex-1 min-w-0 border border-primary/25 bg-surface font-bold text-text text-xs uppercase focus:outline-none focus:border-primary rounded-lg py-1.5 px-2" />
                      <button @click="translateHeaderVariant(group, v, vIndex)"
                              type="button"
                              class="bg-primary/10 border border-primary/30 rounded-lg text-primary cursor-pointer flex items-center justify-center w-8 h-8 text-xs shrink-0 hover:bg-primary/20"
                              :title="$t('services.translate')">
                        <i v-if="translatingField === `header-${vIndex}-variantName-${currentLang}`" class="pi pi-spin pi-spinner"></i>
                        <i v-else class="pi pi-sparkles"></i>
                      </button>
                      <button @click="openModifierModal(group, vIndex)"
                              type="button"
                              class="border border-primary/25 bg-primary/10 text-primary text-[11px] font-bold rounded-lg hover:bg-primary hover:text-black transition-colors h-8 px-2.5 flex items-center justify-center gap-1 cursor-pointer shrink-0"
                              :class="{'!bg-primary/25': v.profileModifiers && Object.keys(v.profileModifiers).length > 0}">
                        <i class="pi pi-bolt text-[10px]"></i>
                        <span>{{ (v.profileModifiers && Object.keys(v.profileModifiers).length > 0) ? $t('services.modifierActive') : $t('services.modifierSetup') }}</span>
                      </button>
                    </div>
                  </div>
                </div>

                <!-- OSZLOPFEJLÉCEK (Rövid / Közepes / Hosszú haj stb.) -->
                <div class="flex items-end border-b border-text/10 px-2.5 sm:px-3 md:px-4 pt-2.5 pb-1.5 bg-text/5 w-full">
                  <div class="flex-1 min-w-0 pr-2 text-[11px] sm:text-xs font-bold text-text-muted uppercase tracking-wider sm:tracking-widest pb-1 opacity-70"
                       :class="isLoggedIn && isGlobalEditMode ? 'pl-6 sm:pl-8' : 'pl-0.5'">
                    {{ $t('nav.services') }}
                  </div>

                  <div class="flex justify-end items-end gap-1 sm:gap-2 md:gap-4 shrink-0">
                    <div v-for="(v, vIndex) in group.headerVariants" :key="vIndex" class="w-[54px] sm:w-[82px] md:w-[115px] flex items-end justify-center relative text-center shrink-0">
                      <div class="relative w-full flex flex-col items-center justify-center group/tools">
                        <!-- Desktopon inline szerkesztés Globális módban -->
                        <template v-if="isLoggedIn && isGlobalEditMode">
                          <div class="hidden sm:flex items-center gap-1 w-full">
                            <textarea v-model="v.variantName[currentLang]"
                                      @change="updateGroupVariantName(group, vIndex)"
                                      class="w-full min-w-0 text-center border border-primary/25 bg-background font-bold text-text text-xs uppercase resize-none overflow-hidden focus:outline-none focus:border-primary rounded-lg transition-colors py-1 px-1"
                                      rows="1"></textarea>
                            <button @click="translateHeaderVariant(group, v, vIndex)"
                                    type="button"
                                    class="bg-primary/10 border border-primary/30 rounded-md text-primary cursor-pointer flex items-center justify-center w-6 h-6 text-[11px] shrink-0 hover:bg-primary/20"
                                    :title="$t('services.translate')">
                              <i v-if="translatingField === `header-${vIndex}-variantName-${currentLang}`" class="pi pi-spin pi-spinner"></i>
                              <i v-else class="pi pi-sparkles"></i>
                            </button>
                          </div>

                          <button @click="openModifierModal(group, vIndex)"
                                  type="button"
                                  class="hidden sm:flex mt-1.5 border border-primary/25 bg-primary/10 text-primary text-[10px] font-bold rounded-lg hover:bg-primary hover:text-black transition-colors py-1 px-2 items-center justify-center gap-1 cursor-pointer w-full"
                                  :class="{'!bg-primary/25': v.profileModifiers && Object.keys(v.profileModifiers).length > 0}">
                            <i class="pi pi-bolt text-[10px]"></i>
                            <span>{{ (v.profileModifiers && Object.keys(v.profileModifiers).length > 0) ? $t('services.modifierActive') : $t('services.modifierSetup') }}</span>
                          </button>

                          <!-- Mobilon a kompakt felirat látszik az oszlop tetején -->
                          <span class="sm:hidden font-bold text-text-muted text-[10px] uppercase text-center block w-full whitespace-normal break-words leading-tight py-0.5">
                            {{ v.variantName[currentLang] }}
                          </span>
                        </template>
                        <span v-else class="font-bold text-text-muted text-[10px] sm:text-xs md:text-sm uppercase text-center block w-full whitespace-normal break-words leading-tight py-0.5">
                          {{ v.variantName[currentLang] }}
                        </span>
                      </div>
                    </div>

                    <!-- Helyfoglaló az egyéni ⚙️ gomb oszlopának, hogy az árak pontosan a fejléc alatt álljanak -->
                    <div v-if="isLoggedIn" class="w-8 sm:w-9 shrink-0"></div>
                  </div>
                </div>

                <!-- SZOLGÁLTATÁS SOROK -->
                <draggable v-model="group.items" item-key="id" group="services" handle=".drag-handle-item" @change="(e) => onServiceDragChange(e, group)" :disabled="!isLoggedIn || !isGlobalEditMode" class="flex flex-col w-full">
                  <template #item="{ element: service }">

                    <div class="service-drop-zone flex flex-col border-b border-text/10 last:border-0 transition-colors relative group/row w-full"
                         :class="[
                           isLoggedIn && service.isBookable === false ? 'opacity-65' : '',
                           activeServiceId === service.id ? 'bg-primary/5' : 'hover:bg-text/5'
                         ]"
                         @dragover="onNoteDragOver"
                         @dragleave="onNoteDragLeave"
                         @drop="(e) => onNoteDrop(e, service)">

                      <!-- 1. TISZTA SOR (Pontosan úgy néz ki, mint a vendégeknek + jobb szélen a ⚙️ gomb adminnak) -->
                      <div class="flex items-center px-2.5 sm:px-3 md:px-4 py-2 sm:py-3 w-full">

                        <div class="flex-1 min-w-0 flex items-center gap-1.5 sm:gap-2 pr-2">
                          <div v-if="isLoggedIn && isGlobalEditMode"
                               class="cursor-grab text-text-muted text-base sm:text-lg flex items-center justify-center w-6 sm:w-7 min-h-[32px] hover:text-primary drag-handle-item transition-colors shrink-0">
                            ⋮⋮
                          </div>

                          <div class="flex items-center gap-1.5 flex-wrap min-w-0">
                            <span class="text-sm sm:text-base font-medium text-text whitespace-normal break-words leading-snug group-hover/row:text-primary transition-colors py-0.5">
                              {{ service.name[currentLang] }}
                            </span>
                            <span v-if="isLoggedIn && service.isBookable === false"
                                   class="text-[9px] sm:text-[10px] font-bold uppercase tracking-wider px-1.5 py-0.5 rounded-full bg-text/10 text-text-muted shrink-0">
                              {{ $t('services.notBookableInfoOnly') }}
                            </span>
                          </div>
                        </div>

                        <!-- Árak oszlopai + Admin ⚙️ gomb -->
                        <div class="flex justify-end items-center gap-1 sm:gap-2 md:gap-4 shrink-0">
                          <div v-for="(variant, vIndex) in service.variants"
                               :key="variant.id || vIndex"
                               class="w-[54px] sm:w-[82px] md:w-[115px] flex flex-col items-center justify-center text-center shrink-0">
                            <span class="text-text font-semibold text-xs sm:text-sm md:text-base whitespace-nowrap transition-colors">
                              {{ formatCurrency(variant.price) }}
                            </span>
                          </div>

                          <!-- Soronkénti ⚙️ Szolgáltatás Beállítások Gomb -->
                          <button v-if="isLoggedIn"
                                  type="button"
                                  @click="toggleServiceEditor(service)"
                                  class="w-8 h-8 sm:w-9 sm:h-9 rounded-xl border flex items-center justify-center shrink-0 cursor-pointer transition-all"
                                  :class="activeServiceId === service.id
                                    ? 'bg-primary text-black border-primary shadow-sm scale-105'
                                    : 'bg-surface text-text-muted border-text/15 hover:border-primary hover:text-primary'"
                                  :title="$t('services.serviceSettingsBtn')">
                            <i class="pi text-xs sm:text-sm" :class="activeServiceId === service.id ? 'pi-times' : 'pi-cog'"></i>
                          </button>
                        </div>
                      </div>

                      <!-- Megjegyzés / Leírás megjelenítése (tiszta nézetben) -->
                      <div v-if="activeServiceId !== service.id && service.description && service.description[currentLang] && service.description[currentLang].trim() !== ''"
                           class="flex items-start px-2.5 sm:px-4 md:px-6 pb-2.5 w-full">
                        <div class="flex-1 min-w-0 border-l-[3px] border-primary/30 pl-2.5 sm:pl-3 bg-background/60 rounded-r-md">
                          <span class="block w-full whitespace-pre-wrap break-words leading-relaxed italic text-xs sm:text-sm text-text-muted py-1 px-1">
                            {{ service.description[currentLang] }}
                          </span>
                        </div>
                      </div>

                      <!-- 2. LENYÍLÓ SZOLGÁLTATÁS SZERKESZTŐ PANEL (100% telefonszélességre optimalizálva) -->
                      <div v-if="isLoggedIn && activeServiceId === service.id"
                           class="w-full box-border border-t border-primary/30 bg-surface/95 px-2.5 py-3 sm:px-4 sm:py-4 md:p-5 space-y-3.5 sm:space-y-4 animate-fade-in">

                        <!-- Panel fejléc: Cím + 💡 Jelmagyarázat + Bezárás -->
                        <div class="flex flex-wrap items-center justify-between gap-2 border-b border-text/10 pb-2.5">
                          <div class="flex items-center gap-1.5 text-primary font-bold text-xs sm:text-sm md:text-base min-w-0">
                            <i class="pi pi-sliders-h shrink-0"></i>
                            <span class="truncate">{{ $t('services.serviceSettingsTitle') }}</span>
                          </div>

                          <div class="flex items-center gap-1.5 shrink-0">
                            <button type="button"
                                    @click="showLegendHelp = true"
                                    class="px-2 sm:px-2.5 py-1.5 rounded-lg border border-primary/30 bg-primary/10 text-primary hover:bg-primary/20 text-[11px] sm:text-xs font-semibold flex items-center gap-1 cursor-pointer transition-all">
                              <i class="pi pi-lightbulb text-[11px]"></i>
                              <span>{{ $t('services.helpLegendBtn') }}</span>
                            </button>
                            <button type="button"
                                    @click="activeServiceId = null"
                                    class="px-2.5 sm:px-3 py-1.5 rounded-lg border border-text/15 bg-background text-text-muted hover:text-text text-[11px] sm:text-xs font-bold flex items-center gap-1 cursor-pointer transition-all">
                              <i class="pi pi-check text-[11px] text-primary"></i>
                              <span>{{ $t('services.closeSettings') }}</span>
                            </button>
                          </div>
                        </div>

                        <!-- A) Szolgáltatás neve + AI Fordítás -->
                        <div class="space-y-1">
                          <label class="block text-[11px] font-bold uppercase tracking-wider text-text-muted">
                            {{ $t('services.serviceNamePlaceholder') }} ({{ currentLang.toUpperCase() }})
                          </label>
                          <div class="flex items-stretch gap-1.5 sm:gap-2 w-full">
                            <textarea v-model="service.name[currentLang]"
                                      @change="saveService(service, false)"
                                      @input="autoResize"
                                      rows="1"
                                      class="flex-1 min-w-0 border border-text/20 bg-background font-semibold text-sm sm:text-base text-text resize-none overflow-hidden leading-snug py-2 px-2.5 sm:px-3 rounded-xl focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary/30 transition-all"></textarea>
                            <button type="button"
                                    @click="translateServiceField(service, 'name')"
                                    class="px-2.5 sm:px-3.5 rounded-xl border border-primary/35 bg-primary/10 text-primary hover:bg-primary/20 font-bold text-xs flex items-center gap-1.5 shrink-0 cursor-pointer transition-all"
                                    :title="$t('services.translate')">
                              <i v-if="translatingField === `${service.id}-name-${currentLang}`" class="pi pi-spin pi-spinner"></i>
                              <i v-else class="pi pi-sparkles"></i>
                              <span class="hidden xs:inline sm:inline">{{ $t('services.translate') }}</span>
                            </button>
                          </div>
                        </div>

                        <!-- B) Variánsok kártyái: Ár (EUR), Alapértelmezett Időtartam (perc), Anyagfelhasználás (Receptúra) -->
                        <div class="space-y-2">
                          <div class="flex items-center justify-between gap-2">
                            <span class="text-[11px] font-bold uppercase tracking-wider text-text-muted">
                              {{ $t('services.variantName') }} ({{ service.variants?.length || 0 }})
                            </span>
                            <button type="button"
                                    @click="addVariantToService(service, group)"
                                    class="text-xs font-bold text-primary hover:underline flex items-center gap-1 cursor-pointer bg-transparent border-none p-0">
                              <i class="pi pi-plus text-[10px]"></i>
                              <span>{{ $t('services.addVariant') }}</span>
                            </button>
                          </div>

                          <div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-2.5">
                            <div v-for="(variant, vIndex) in service.variants"
                                 :key="variant.id || vIndex"
                                 class="bg-background border border-text/15 rounded-xl p-2.5 sm:p-3 flex flex-col gap-2.5 relative min-w-0">

                              <!-- Variáns fejléc + Törlés -->
                              <div class="flex items-center justify-between gap-2 border-b border-text/10 pb-1.5">
                                <span class="font-bold text-xs uppercase tracking-wider text-primary truncate">
                                  {{ group.headerVariants[vIndex]?.variantName?.[currentLang] || variant.variantName?.[currentLang] || `${$t('services.variantName')} #${vIndex + 1}` }}
                                </span>
                                <button v-if="service.variants.length > 1"
                                        type="button"
                                        @click="removeVariant(service, vIndex, group)"
                                        class="w-6 h-6 rounded-lg bg-red-500/10 text-red-500 hover:bg-red-500 hover:text-white flex items-center justify-center text-xs cursor-pointer transition-colors border-none shrink-0">
                                  <i class="pi pi-trash text-[11px]"></i>
                                </button>
                              </div>

                              <!-- Ár (EUR) és Időtartam (perc) egymás mellett, telefonszélességre szabva -->
                              <div class="grid grid-cols-2 gap-2 min-w-0">
                                <!-- Ár -->
                                <div class="flex flex-col gap-1 min-w-0">
                                  <label class="text-[11px] font-semibold text-text-muted truncate">{{ $t('services.priceLabel') }}</label>
                                  <InputNumber v-model="variant.price"
                                               mode="currency" currency="EUR" :locale="locale" :minFractionDigits="0"
                                               class="w-full min-w-0 [&_input]:min-w-0 [&_input]:w-full [&_input]:border [&_input]:border-text/20 [&_input]:bg-surface [&_input]:text-center [&_input]:font-bold [&_input]:text-sm [&_input]:text-text [&_input]:h-9 [&_input]:px-1.5 [&_input]:focus:border-primary [&_input]:focus:outline-none [&_input]:rounded-lg"
                                               @update:modelValue="saveService(service, false)"
                                               @blur="saveService(service, false)" />
                                </div>

                                <!-- Alapértelmezett Időtartam (perc) -->
                                <div class="flex flex-col gap-1 min-w-0">
                                  <label class="text-[11px] font-semibold text-text-muted flex items-center gap-1 truncate">
                                    <i class="pi pi-clock text-[10px] text-primary shrink-0"></i>
                                    <span class="truncate">{{ $t('services.durationLabel') }}</span>
                                  </label>
                                  <div class="flex items-center h-9 rounded-lg border border-text/20 bg-surface overflow-hidden focus-within:border-primary min-w-0">
                                    <button type="button"
                                            @click="stepVariantDuration(service, variant, -5)"
                                            class="w-7 h-full bg-text/5 hover:bg-text/10 text-text font-bold text-xs flex items-center justify-center cursor-pointer border-none shrink-0">
                                      -
                                    </button>
                                    <input type="number"
                                           v-model.number="variant.duration"
                                           min="5"
                                           max="600"
                                           step="5"
                                           @change="saveService(service, false)"
                                           class="flex-1 min-w-0 w-full text-center bg-transparent border-none text-xs font-bold text-text focus:outline-none p-0 [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none" />
                                    <span class="text-[10px] text-text-muted pr-1 shrink-0">{{ $t('services.minutesUnit') }}</span>
                                    <button type="button"
                                            @click="stepVariantDuration(service, variant, 5)"
                                            class="w-7 h-full bg-text/5 hover:bg-text/10 text-text font-bold text-xs flex items-center justify-center cursor-pointer border-none shrink-0">
                                      +
                                    </button>
                                  </div>
                                </div>
                              </div>

                              <!-- Anyagfelhasználás / Receptúra gomb -->
                              <button type="button"
                                      @click="openRecipeModal(service, variant, group, vIndex)"
                                      class="w-full py-1.5 px-2.5 rounded-lg border text-xs font-semibold flex items-center justify-center gap-1.5 cursor-pointer transition-all min-w-0"
                                      :class="variant.defaultProducts?.length
                                        ? 'border-primary/40 bg-primary/10 text-primary'
                                        : 'border-text/15 bg-surface text-text-muted hover:text-text hover:border-text/30'">
                                <i class="pi pi-box text-xs shrink-0"></i>
                                <span class="truncate">
                                  {{ variant.defaultProducts?.length
                                    ? `${$t('services.recipeConfigured')} (${$t('services.productCount', { count: variant.defaultProducts.length })})`
                                    : $t('services.recipeSetDefault') }}
                                </span>
                              </button>
                            </div>
                          </div>
                        </div>

                        <!-- C) Megjegyzés / Leírás + AI Fordítás -->
                        <div class="space-y-1">
                          <label class="block text-[11px] font-bold uppercase tracking-wider text-text-muted">
                            {{ $t('services.noteLabel') }} ({{ currentLang.toUpperCase() }})
                          </label>
                          <div class="flex items-stretch gap-1.5 sm:gap-2 w-full">
                            <textarea v-model="service.description[currentLang]"
                                      @change="saveService(service, false)"
                                      @input="autoResize"
                                      rows="2"
                                      class="flex-1 min-w-0 border border-text/20 bg-background italic text-xs sm:text-sm text-text resize-none overflow-hidden leading-relaxed py-2 px-2.5 sm:px-3 rounded-xl focus:outline-none focus:border-primary transition-all"
                                      :placeholder="(currentLang !== (company?.defaultLanguage || 'hu') && service.description[company?.defaultLanguage || 'hu']) ? service.description[company?.defaultLanguage || 'hu'] : $t('services.notePlaceholder')"></textarea>
                            <button type="button"
                                    @click="translateServiceField(service, 'description')"
                                    class="px-2.5 sm:px-3.5 rounded-xl border border-primary/35 bg-primary/10 text-primary hover:bg-primary/20 font-bold text-xs flex items-center gap-1.5 shrink-0 cursor-pointer transition-all"
                                    :title="$t('services.translate')">
                              <i v-if="translatingField === `${service.id}-description-${currentLang}`" class="pi pi-spin pi-spinner"></i>
                              <i v-else class="pi pi-sparkles"></i>
                              <span class="hidden xs:inline sm:inline">{{ $t('services.translate') }}</span>
                            </button>
                          </div>
                        </div>

                        <!-- D) Alsó sáv: Online foglalhatóság kapcsoló + Szolgáltatás törlése -->
                        <div class="flex flex-wrap items-center justify-between gap-2 pt-2 border-t border-text/10">
                          <button type="button"
                                  @click="toggleServiceBookable(service, group)"
                                  class="px-3 py-1.5 rounded-xl border text-xs font-bold flex items-center gap-1.5 cursor-pointer transition-all"
                                  :class="service.isBookable !== false
                                    ? 'border-primary/40 bg-primary/10 text-primary'
                                    : 'border-text/15 bg-background text-text-muted'">
                            <i class="pi text-xs" :class="service.isBookable !== false ? 'pi-calendar-plus' : 'pi-calendar-times'"></i>
                            <span>{{ service.isBookable !== false ? $t('services.onlineBookable') : $t('services.notBookableInfoOnly') }}</span>
                          </button>

                          <button type="button"
                                  @click="deleteService(service.id)"
                                  class="px-3 py-1.5 rounded-xl border border-red-500/30 bg-red-500/10 text-red-500 hover:bg-red-500 hover:text-white text-xs font-bold flex items-center gap-1.5 cursor-pointer transition-all">
                            <i class="pi pi-trash text-xs"></i>
                            <span>{{ $t('services.deleteServiceBtn') }}</span>
                          </button>
                        </div>

                      </div>

                    </div>
                  </template>
                </draggable>

              </div>
            </div>

            <!-- Új szolgáltatás hozzáadása a kategóriához (Globális szerkesztő módban látszik) -->
            <div v-if="isLoggedIn && isGlobalEditMode" class="bg-background border-t border-text/10 p-2 md:p-3">
              <button @click="addServiceToGroupEnd(group)" class="bg-transparent border border-dashed border-text/30 text-text-muted w-full min-h-[44px] cursor-pointer rounded-lg text-sm transition-all duration-200 hover:bg-text/5 hover:text-primary hover:border-primary/50 flex items-center justify-center font-medium">
                <i class="pi pi-plus mr-2"></i> {{ $t('services.addService') }}
              </button>
            </div>

          </div>
        </template>
      </draggable>

    </div>

    <!-- 💡 / (i) JELMAGYARÁZAT (SÚGÓ) MODAL -->
    <div v-if="showLegendHelp"
         class="fixed inset-0 bg-black/60 backdrop-blur-sm z-[1150] flex items-center justify-center p-4"
         @click.self="showLegendHelp = false">
      <div class="bg-surface w-full max-w-lg rounded-2xl border border-text/15 shadow-2xl overflow-hidden flex flex-col max-h-[85vh] animate-fade-in">
        <div class="p-4 md:p-5 border-b border-text/10 bg-background/60 flex items-center justify-between gap-3">
          <div>
            <h3 class="font-bold text-base md:text-lg text-text flex items-center gap-2 m-0">
              <i class="pi pi-lightbulb text-primary text-lg"></i>
              <span>{{ $t('services.helpLegendTitle') }}</span>
            </h3>
            <p class="text-xs text-text-muted mt-1 mb-0">{{ $t('services.helpLegendSubtitle') }}</p>
          </div>
          <button type="button"
                  @click="showLegendHelp = false"
                  class="w-8 h-8 rounded-lg bg-surface border border-text/15 text-text-muted hover:text-text flex items-center justify-center cursor-pointer">
            <i class="pi pi-times text-xs"></i>
          </button>
        </div>

        <div class="p-4 md:p-5 space-y-3.5 overflow-y-auto text-sm">
          <div class="flex items-start gap-3 p-3 rounded-xl bg-background border border-text/10">
            <div class="w-9 h-9 rounded-xl bg-primary/15 text-primary flex items-center justify-center shrink-0">
              <i class="pi pi-cog text-base"></i>
            </div>
            <div>
              <div class="font-bold text-text text-xs md:text-sm">{{ $t('services.helpGlobalTitle') }}</div>
              <p class="text-xs text-text-muted mt-0.5 mb-0 leading-relaxed">{{ $t('services.helpGlobalDesc') }}</p>
            </div>
          </div>

          <div class="flex items-start gap-3 p-3 rounded-xl bg-background border border-text/10">
            <div class="w-9 h-9 rounded-xl bg-primary/15 text-primary flex items-center justify-center shrink-0">
              <i class="pi pi-sliders-h text-base"></i>
            </div>
            <div>
              <div class="font-bold text-text text-xs md:text-sm">{{ $t('services.helpRowTitle') }}</div>
              <p class="text-xs text-text-muted mt-0.5 mb-0 leading-relaxed">{{ $t('services.helpRowDesc') }}</p>
            </div>
          </div>

          <div class="flex items-start gap-3 p-3 rounded-xl bg-background border border-text/10">
            <div class="w-9 h-9 rounded-xl bg-primary/15 text-primary flex items-center justify-center shrink-0">
              <i class="pi pi-clock text-base"></i>
            </div>
            <div>
              <div class="font-bold text-text text-xs md:text-sm">{{ $t('services.helpDurationTitle') }}</div>
              <p class="text-xs text-text-muted mt-0.5 mb-0 leading-relaxed">{{ $t('services.helpDurationDesc') }}</p>
            </div>
          </div>

          <div class="flex items-start gap-3 p-3 rounded-xl bg-background border border-text/10">
            <div class="w-9 h-9 rounded-xl bg-primary/15 text-primary flex items-center justify-center shrink-0">
              <i class="pi pi-calendar-plus text-base"></i>
            </div>
            <div>
              <div class="font-bold text-text text-xs md:text-sm">{{ $t('services.helpBookableTitle') }}</div>
              <p class="text-xs text-text-muted mt-0.5 mb-0 leading-relaxed">{{ $t('services.helpBookableDesc') }}</p>
            </div>
          </div>

          <div class="flex items-start gap-3 p-3 rounded-xl bg-background border border-text/10">
            <div class="w-9 h-9 rounded-xl bg-primary/15 text-primary flex items-center justify-center shrink-0">
              <i class="pi pi-sparkles text-base"></i>
            </div>
            <div>
              <div class="font-bold text-text text-xs md:text-sm">{{ $t('services.helpTranslateTitle') }}</div>
              <p class="text-xs text-text-muted mt-0.5 mb-0 leading-relaxed">{{ $t('services.helpTranslateDesc') }}</p>
            </div>
          </div>

          <div class="flex items-start gap-3 p-3 rounded-xl bg-background border border-text/10">
            <div class="w-9 h-9 rounded-xl bg-primary/15 text-primary flex items-center justify-center shrink-0">
              <i class="pi pi-box text-base"></i>
            </div>
            <div>
              <div class="font-bold text-text text-xs md:text-sm">{{ $t('services.helpRecipeTitle') }}</div>
              <p class="text-xs text-text-muted mt-0.5 mb-0 leading-relaxed">{{ $t('services.helpRecipeDesc') }}</p>
            </div>
          </div>

          <div class="flex items-start gap-3 p-3 rounded-xl bg-background border border-text/10">
            <div class="w-9 h-9 rounded-xl bg-primary/15 text-primary flex items-center justify-center shrink-0">
              <i class="pi pi-bolt text-base"></i>
            </div>
            <div>
              <div class="font-bold text-text text-xs md:text-sm">{{ $t('services.helpModifierTitle') }}</div>
              <p class="text-xs text-text-muted mt-0.5 mb-0 leading-relaxed">{{ $t('services.helpModifierDesc') }}</p>
            </div>
          </div>
        </div>

        <div class="p-4 border-t border-text/10 bg-background/60 flex justify-end">
          <button type="button"
                  @click="showLegendHelp = false"
                  class="px-5 py-2 bg-primary text-black font-bold text-xs md:text-sm rounded-xl cursor-pointer border-none hover:brightness-95 transition-all">
            {{ $t('services.closeSettings') }}
          </button>
        </div>
      </div>
    </div>

    <div v-if="isModifierModalOpen" class="fixed inset-0 bg-black/60 backdrop-blur-sm z-[1100] flex items-center justify-center p-4">
      <div class="bg-surface w-full max-w-md rounded-2xl shadow-xl overflow-hidden flex flex-col">

        <div class="p-5 border-b border-text/10 bg-background/50">
          <h3 class="font-bold text-lg text-text flex items-center gap-2">
            <i class="pi pi-bolt text-primary"></i> {{ $t('services.modifierModal.title') }}
          </h3>
          <p class="text-xs text-text-muted mt-1 leading-tight">
            {{ $t('services.modifierModal.description') }}
          </p>
        </div>

        <div class="p-5 space-y-4 overflow-y-auto max-h-[60vh]">

          <div v-if="companyAttributes.length === 0" class="text-sm text-text-muted text-center py-4 italic">
            {{ $t('services.modifierModal.noAttributes') }}
          </div>

          <div v-else class="space-y-4">
            <div v-for="attr in companyAttributes" :key="attr.key">
              <label class="block text-xs font-bold text-text-muted mb-1.5 uppercase">{{ attr.label }}</label>

              <select v-if="attr.dataType === 'select'" v-model="editingVariant.profileModifiers[attr.key]"
                      class="w-full h-10 bg-background border border-text/20 rounded-lg px-3 text-sm focus:outline-none focus:border-primary">
                <option value="">{{ $t('services.modifierModal.noModify') }}</option>
                <option v-for="opt in attr.options" :key="opt" :value="opt">{{ opt }}</option>
              </select>

              <input v-else type="text" v-model="editingVariant.profileModifiers[attr.key]" :placeholder="$t('services.modifierModal.emptyPlaceholder')"
                     class="w-full h-10 bg-background border border-text/20 rounded-lg px-3 text-sm focus:outline-none focus:border-primary">
            </div>
          </div>
        </div>

        <div class="p-5 border-t border-text/10 flex justify-end gap-3 bg-background/50">
          <button @click="closeModifierModal" class="px-6 h-10 bg-primary text-white font-bold rounded-lg hover:brightness-110 shadow-md transition-transform active:scale-95 flex items-center gap-2">
            <i class="pi pi-check"></i> {{ $t('services.modifierModal.done') }}
          </button>
        </div>

      </div>
    </div>

  </div>

  <ServiceRecipeModal 
    v-model:visible="isRecipeModalOpen"
    :variant="editingRecipeVariant"
    :serviceName="editingRecipeServiceName"
    :variantName="editingRecipeVariantName"
    @saved="onRecipeSaved"
  />
</template>

