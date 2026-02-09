<script setup>
  import { ref, onMounted, inject, watch } from 'vue';
  import InputNumber from 'primevue/inputnumber';
  import apiClient from '@/services/api';
  import { getCompanyIdFromToken } from '@/utils/jwt';
  import { DEFAULT_COMPANY_ID } from '@/config';
  import draggable from 'vuedraggable';

  const services = ref([]);
  const categories = ref([]);
  const loading = ref(true);
  const isLoggedIn = ref(false);

  const company = inject('company', ref(null));

  const draggedNoteContent = ref(null);
  const draggedFromServiceId = ref(null);

  /* --- SEGÉDFÜGGVÉNYEK --- */

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

  /* --- ADATTRANSZFORMÁCIÓ --- */

  const buildNestedStructure = (flatServices) => {
    const groups = [];
    flatServices.sort((a, b) => a.orderIndex - b.orderIndex);

    flatServices.forEach(service => {
      const catName = service.category || "Egyéb";
      let group = groups.find(g => g.categoryName === catName);

      if (!group) {
        group = {
          id: 'cat-' + catName,
          categoryName: catName,
          headerVariants: [],
          items: []
        };
        groups.push(group);
      }

      if (group.headerVariants.length === 0 && service.variants && service.variants.length > 0) {
        group.headerVariants = [...service.variants];
      }

      group.items.push(service);
    });

    return groups;
  };

  /* --- API MŰVELETEK --- */

  const fetchServices = async () => {
    const targetCompanyId = company?.value?.id || DEFAULT_COMPANY_ID;
    loading.value = true;
    try {
      const response = await apiClient.get('/api/Service', {
        params: { companyId: targetCompanyId }
      });

      const rawServices = response.data;

      rawServices.forEach(service => {
        if (service.variants) {
          service.variants = sortVariants(service.variants);
          service.variants.forEach(v => { if (v.price === 0) v.price = null; });
        }
        if (!service.category) service.category = "Egyéb";
        if (service.description === null) service.description = "";
      });

      categories.value = buildNestedStructure(rawServices);

    } catch (error) {
      console.error('Hiba a betolteskor:', error);
    } finally {
      loading.value = false;
    }
  };

  watch(
    () => company?.value?.id,
    (newId) => { if (newId) fetchServices(); },
    { immediate: true }
  );

  // --- JAVÍTÁS: refreshLocal paraméter bevezetése ---
  // Ha false (alapértelmezett), akkor nem írjuk felül a lokális adatokat a szerver válaszával.
  // Ez megakadályozza, hogy a lassú válasz felülírja a gyors gépelést.
  const saveService = async (serviceItem, refreshLocal = false) => {
    try {
      const payload = JSON.parse(JSON.stringify(serviceItem));

      if (!payload.variants) payload.variants = [];
      else {
        payload.variants.forEach(v => {
          if (v.price === null || v.price === undefined) v.price = 0;
        });
      }

      const response = await apiClient.put(`/api/Service/${serviceItem.id}`, payload);

      // Csak akkor frissítünk a válaszból, ha kifejezetten kértük (pl. új variáns ID miatt)
      if (refreshLocal && response.status === 200) {
        // Itt megkeressük az elemet a memóriában és frissítjük
        // (De csak ha muszáj, mert ez okozza az ugrálást)
        /* Megjegyzés: A jelenlegi struktúrában (categories -> groups -> items)
           a 'serviceItem' referencia, tehát ha azt módosítjuk, a UI frissül.
           De most a 'serviceItem'-et nem bántjuk, csak ha 'refreshLocal' true.
        */
        const updated = response.data;

        // Adattisztítás a válaszból
        if (updated.variants) {
          updated.variants = sortVariants(updated.variants);
          updated.variants.forEach(v => { if (v.price === 0) v.price = null; });
        }
        if (updated.description === null) updated.description = "";

        // Mezők átmásolása a reaktív objektumba
        Object.assign(serviceItem, updated);
      }

    } catch (err) {
      console.error("Hiba a mentesnel:", err);
    }
  };

  /* --- UPDATE & DRAG --- */

  const onServiceDragChange = async (event, group) => {
    if (event.added) {
      const item = event.added.element;
      item.category = group.categoryName;
    }
    await reorderAll();
  };

  const onCategoryDragChange = async () => {
    await reorderAll();
  };

  const reorderAll = async () => {
    let counter = 10;
    const promises = [];
    categories.value.forEach(group => {
      group.items.forEach(item => {
        if (item.orderIndex !== counter || item.category !== group.categoryName) {
          item.orderIndex = counter;
          item.category = group.categoryName;
          promises.push(saveService(item, false)); // false: sorrendnél se villogjon a UI
        }
        counter += 10;
      });
    });
    if (promises.length > 0) await Promise.all(promises);
  };

  const updateCategoryName = async (group, newName) => {
    group.categoryName = newName;
    const promises = group.items.map(service => {
      service.category = newName;
      return saveService(service, false);
    });
    await Promise.all(promises);
  };

  const updateGroupVariantName = async (group, variantIndex, newName) => {
    const promises = group.items.map(service => {
      if (service.variants && service.variants[variantIndex]) {
        service.variants[variantIndex].variantName = newName;
        return saveService(service, false);
      }
      return Promise.resolve();
    });
    await Promise.all(promises);
  };

  /* --- NOTE DRAG --- */

  const onNoteDragStart = (event, service) => {
    draggedNoteContent.value = service.description;
    draggedFromServiceId.value = service.id;
    event.dataTransfer.effectAllowed = 'move';
    event.target.style.opacity = '0.5';
  };

  const onNoteDragEnd = (event) => {
    event.target.style.opacity = '1';
    draggedNoteContent.value = null;
    draggedFromServiceId.value = null;
    document.querySelectorAll('.service-drop-zone').forEach(el => el.classList.remove('drag-over'));
  };

  const onNoteDragOver = (event) => {
    event.preventDefault();
    const target = event.currentTarget;
    target.classList.add('drag-over');
  };

  const onNoteDragLeave = (event) => {
    const target = event.currentTarget;
    target.classList.remove('drag-over');
  };

  const onNoteDrop = async (event, targetService) => {
    event.preventDefault();
    event.currentTarget.classList.remove('drag-over');

    if (!draggedNoteContent.value || draggedFromServiceId.value === targetService.id) return;

    if (targetService.description && targetService.description.trim() !== '') {
      targetService.description += '\n' + draggedNoteContent.value;
    } else {
      targetService.description = draggedNoteContent.value;
    }

    for (const group of categories.value) {
      const sourceService = group.items.find(s => s.id === draggedFromServiceId.value);
      if (sourceService) {
        sourceService.description = '';
        await saveService(sourceService, true); // Itt frissítünk, hogy biztos eltűnjön
        break;
      }
    }

    await saveService(targetService, true); // Itt is, hogy biztos megjelenjen
  };

  /* --- LÉTREHOZÁS --- */

  const toggleNote = async (service) => {
    if (!service.description) {
      service.description = " ";
    }
  };

  const createNewCategory = async () => {
    if (!isLoggedIn.value) return;
    const newService = {
      name: "Új szolgáltatás",
      category: "ÚJ KATEGÓRIA",
      defaultPrice: 0,
      orderIndex: 99999,
      variants: [{ variantName: "Normál", price: 0, duration: 30 }],
      description: ""
    };
    await postNewService(newService);
  };

  const addServiceToGroupEnd = async (group) => {
    let variants = [{ variantName: "Normál", price: 0, duration: 30 }];
    if (group.headerVariants && group.headerVariants.length > 0) {
      variants = group.headerVariants.map(v => ({
        variantName: v.variantName, price: 0, duration: v.duration
      }));
    }

    const newService = {
      name: "Új szolgáltatás",
      category: group.categoryName,
      defaultPrice: 0,
      orderIndex: 99999,
      variants: variants,
      description: ""
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

  // --- VARIÁNS MÓDOSÍTÁS: ITT KELL A TRUE (Refresh) ---
  // Mert itt új ID-kat kaphatunk a szervertől
  const removeVariant = async (service, vIndex, group) => {
    service.variants.splice(vIndex, 1);
    if (group) group.headerVariants = [...service.variants];
    await saveService(service, true);
  };

  const addVariantToService = async (service, group) => {
    if (!service.variants) service.variants = [];
    service.variants.push({ id: 0, variantName: "Extra", price: 0, duration: 30 });
    if (group) group.headerVariants = [...service.variants];
    await saveService(service, true);
  };

  onMounted(() => {
    const token = localStorage.getItem('salon_token');
    isLoggedIn.value = !!token;
  });
</script>

<template>
  <div class="smart-container dark-theme">
    <div class="header-actions">
      <h2>{{ isLoggedIn ? 'Árlista Szerkesztő' : 'Árlista' }}</h2>
      <button v-if="isLoggedIn" @click="createNewCategory" class="main-add-btn">
        <i class="pi pi-folder-open"></i> Új Kategória
      </button>
    </div>

    <div v-if="loading">Betöltés...</div>

    <div v-else class="services-wrapper">

      <draggable v-model="categories"
                 item-key="id"
                 handle=".drag-handle-cat"
                 @change="onCategoryDragChange"
                 :disabled="!isLoggedIn">
        <template #item="{ element: group }">

          <div class="category-block">
            <div class="table-row header-row">
              <div v-if="isLoggedIn" class="drag-handle-cat" title="Kategória mozgatása">⋮⋮</div>
              <div class="col-name header-title-cell">
                <input v-if="isLoggedIn"
                       v-model="group.categoryName"
                       @change="updateCategoryName(group, group.categoryName)"
                       class="category-input"
                       placeholder="Kategória neve" />
                <span v-else class="category-display">{{ group.categoryName }}</span>
              </div>
              <div class="col-variants-group">
                <div v-for="(v, vIndex) in group.headerVariants" :key="vIndex" class="col-variant-item header-item">
                  <textarea v-if="isLoggedIn"
                            :value="v.variantName"
                            @change="(e) => updateGroupVariantName(group, vIndex, e.target.value)"
                            class="header-variant-input"
                            rows="2"></textarea>
                  <span v-else class="header-label">{{ v.variantName }}</span>
                </div>
              </div>
            </div>

            <draggable v-model="group.items"
                       item-key="id"
                       group="services"
                       handle=".drag-handle-item"
                       @change="(e) => onServiceDragChange(e, group)"
                       :disabled="!isLoggedIn">
              <template #item="{ element: service }">

                <div class="service-drop-zone"
                     @dragover="onNoteDragOver"
                     @dragleave="onNoteDragLeave"
                     @drop="(e) => onNoteDrop(e, service)">

                  <div class="table-row data-row">
                    <div v-if="isLoggedIn" class="drag-handle-item">⋮⋮</div>

                    <div class="col-name">
                      <textarea v-if="isLoggedIn"
                                v-model="service.name"
                                @change="saveService(service, false)"
                                @input="autoResize"
                                class="name-input"
                                rows="1"></textarea>
                      <span v-else class="name-text">{{ service.name }}</span>

                      <div v-if="isLoggedIn" class="row-tools">
                        <button @click="toggleNote(service)" title="Megjegyzés hozzáadása" class="icon-btn note-toggle">
                          <i class="pi pi-comment"></i>
                        </button>
                        <span class="tool-separator">|</span>
                        <button @click="addVariantToService(service, group)" title="Oszlop +" class="icon-btn tiny">+</button>
                        <button @click="deleteService(service.id)" title="Törlés" class="icon-btn trash">🗑</button>
                      </div>
                    </div>

                    <div class="col-variants-group">
                      <div v-for="(variant, vIndex) in service.variants" :key="variant.id || vIndex" class="col-variant-item">
                        <div class="price-wrapper">
                          <InputNumber v-if="isLoggedIn"
                                       v-model="variant.price"
                                       mode="currency" currency="EUR" locale="hu-HU" :minFractionDigits="0"
                                       class="price-input"
                                       placeholder=""
                                       @update:modelValue="saveService(service, false)"
                                       @blur="saveService(service, false)" />
                          <span v-else class="price-display">
                            {{ formatCurrency(variant.price) }}
                          </span>
                        </div>
                        <button v-if="isLoggedIn" @click="removeVariant(service, vIndex, group)" class="variant-remove-btn">×</button>
                      </div>
                    </div>
                  </div>

                  <div v-if="service.description"
                       class="note-block"
                       :draggable="isLoggedIn"
                       @dragstart="(e) => onNoteDragStart(e, service)"
                       @dragend="onNoteDragEnd">

                    <div v-if="isLoggedIn" class="note-drag-handle" title="Húzd át másik szolgáltatásra">
                      <i class="pi pi-arrows-alt"></i>
                    </div>

                    <div class="note-content">
                      <textarea v-if="isLoggedIn"
                                v-model="service.description"
                                @change="saveService(service, false)"
                                @input="autoResize"
                                class="note-input"
                                placeholder="Megjegyzés..."></textarea>
                      <span v-else class="note-text">{{ service.description }}</span>
                    </div>
                  </div>

                </div>

              </template>
            </draggable>

            <div v-if="isLoggedIn" class="group-footer">
              <button @click="addServiceToGroupEnd(group)" class="add-row-btn">
                + Szolgáltatás hozzáadása
              </button>
            </div>

          </div>
        </template>
      </draggable>

    </div>
  </div>
</template>

<style scoped>
  /* STÍLUSOK (VÁLTOZATLANOK) */
  .smart-container {
    max-width: 1000px;
    margin: 0 auto;
    padding: 20px;
    box-sizing: border-box;
    background-color: #000;
    color: #ddd;
    min-height: 100vh;
  }

  h2 {
    font-weight: 300;
    color: #fff;
    margin: 0;
    letter-spacing: 1px;
  }

  .services-wrapper {
    padding-bottom: 50px;
  }

  .main-add-btn {
    background-color: #d4af37;
    color: #000;
    border: none;
    padding: 10px 20px;
    border-radius: 4px;
    cursor: pointer;
    font-weight: 600;
    display: flex;
    align-items: center;
    gap: 8px;
  }

    .main-add-btn:hover {
      background-color: #b5952f;
    }

  .group-footer {
    display: flex;
    gap: 10px;
    margin-top: 5px;
    padding: 5px 0;
  }

  .add-row-btn {
    background: none;
    border: 1px dashed #444;
    color: #888;
    width: 100%;
    padding: 8px;
    margin-top: 5px;
    cursor: pointer;
    border-radius: 4px;
    font-size: 0.9rem;
    transition: all 0.2s;
  }

    .add-row-btn:hover {
      background: #111;
      color: #d4af37;
      border-color: #666;
    }

  .category-block {
    margin-bottom: 40px;
  }

  .sortable-ghost {
    opacity: 0.5;
    background: #111;
  }

  .header-row {
    display: flex;
    align-items: flex-end;
    border-bottom: 2px solid #333;
    padding-bottom: 10px;
    margin-bottom: 10px;
  }

  .category-input {
    font-size: 1.2rem;
    font-weight: bold;
    color: #d4af37;
    border: none;
    background: transparent;
    width: 100%;
    text-transform: uppercase;
    letter-spacing: 2px;
  }

    .category-input:focus {
      outline: none;
      border-bottom: 1px solid #d4af37;
    }

  .category-display {
    font-size: 1.2rem;
    font-weight: bold;
    color: #fff;
    text-transform: uppercase;
    letter-spacing: 2px;
  }

  .header-variant-input {
    width: 100%;
    text-align: center;
    border: none;
    background: transparent;
    font-weight: 600;
    color: #aaa;
    font-size: 0.85rem;
    text-transform: uppercase;
    resize: none;
    overflow: hidden;
    font-family: inherit;
  }

    .header-variant-input:focus {
      background: #111;
      outline: 1px solid #d4af37;
      color: #fff;
    }

  .header-label {
    font-weight: 600;
    color: #aaa;
    font-size: 0.85rem;
    text-transform: uppercase;
    text-align: center;
    display: block;
    width: 100%;
    white-space: normal;
    word-wrap: break-word;
  }

  .service-drop-zone {
    border: 1px solid transparent;
    border-radius: 4px;
    transition: border-color 0.2s, background-color 0.2s;
  }

    .service-drop-zone.drag-over {
      border-color: #d4af37;
      background-color: #1a1a1a;
    }

  .data-row {
    display: flex;
    align-items: center;
    padding: 10px 0;
    border-bottom: 1px solid #1a1a1a;
  }

    .data-row:hover {
      background-color: #111;
    }

  .col-name {
    flex-grow: 1;
    display: flex;
    align-items: center;
    padding-right: 15px;
    min-width: 180px;
    overflow: hidden;
  }

  .name-input {
    width: 100%;
    border: none;
    background: transparent;
    font-size: 1rem;
    color: #eee;
    resize: none;
    overflow: hidden;
    font-family: inherit;
    line-height: 1.4;
    padding: 0;
  }

    .name-input:focus {
      outline: none;
      border-bottom: 1px solid #d4af37;
    }

  .name-text {
    font-size: 1rem;
    color: #ddd;
    white-space: normal;
    word-break: break-word;
    line-height: 1.4;
    display: block;
    width: 100%;
  }

  .data-row:hover .name-text {
    color: #fff;
  }

  .note-block {
    display: flex;
    align-items: flex-start;
    padding: 5px 0 5px 40px;
    margin-bottom: 5px;
    position: relative;
  }

  .note-drag-handle {
    cursor: grab;
    color: #666;
    margin-right: 10px;
    padding-top: 3px;
    font-size: 0.9rem;
  }

    .note-drag-handle:hover {
      color: #d4af37;
    }

  .note-content {
    flex-grow: 1;
    border-left: 2px solid #333;
    padding-left: 10px;
  }

  .note-input {
    width: 100%;
    border: none;
    background: transparent;
    font-style: italic;
    color: #aaa;
    resize: none;
    overflow: hidden;
    font-family: inherit;
    line-height: 1.4;
  }

    .note-input:focus {
      outline: none;
      background: #111;
      color: #fff;
    }

  .note-text {
    display: block;
    width: 100%;
    white-space: pre-wrap;
    word-break: break-word;
    line-height: 1.4;
    font-style: italic;
    color: #888;
  }

  .drag-handle-cat {
    cursor: grab;
    font-size: 1.5rem;
    color: #d4af37;
    margin-right: 15px;
  }

  .drag-handle-item {
    cursor: grab;
    color: #555;
    margin-right: 10px;
    font-size: 1.2rem;
    display: flex;
    align-items: center;
    height: 100%;
  }

    .drag-handle-item:hover {
      color: #999;
    }

  .col-variants-group {
    display: flex;
    justify-content: flex-end;
    gap: 15px;
    flex-shrink: 0;
  }

  .col-variant-item {
    width: 130px;
    display: flex;
    align-items: center;
    justify-content: center;
    position: relative;
    text-align: center;
    align-items: flex-start;
  }

  .header-item {
    align-items: flex-end;
    padding-bottom: 0;
    min-height: 40px;
  }

  .row-tools {
    display: flex;
    align-items: center;
    gap: 5px;
    margin-left: 15px;
    opacity: 0;
    transition: opacity 0.2s;
  }

  .data-row:hover .row-tools {
    opacity: 1;
  }

  .icon-btn {
    border: none;
    background: none;
    cursor: pointer;
    color: #555;
    font-size: 1rem;
  }

    .icon-btn:hover {
      color: #fff;
    }

    .icon-btn.trash:hover {
      color: #ff4444;
    }

    .icon-btn.note-toggle:hover {
      color: #d4af37;
    }

  .tool-separator {
    color: #333;
    margin: 0 5px;
  }

  .variant-remove-btn {
    position: absolute;
    top: -8px;
    right: 0;
    border: none;
    background: none;
    color: #ff4444;
    opacity: 0;
    cursor: pointer;
  }

  .col-variant-item:hover .variant-remove-btn {
    opacity: 1;
  }

  .price-display {
    color: #aaa;
    font-family: inherit;
  }

  .data-row:hover .price-display {
    color: #fff;
    font-weight: 500;
  }

  .price-input {
    width: 100px !important;
  }

    .price-input :deep(input) {
      border: none;
      background: transparent;
      text-align: center;
      color: #ccc;
      padding: 0;
      font-family: inherit;
    }

      .price-input :deep(input):focus {
        background: #111;
        box-shadow: 0 0 0 1px #d4af37;
        color: #fff;
      }

  .header-actions {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 30px;
  }
</style>
