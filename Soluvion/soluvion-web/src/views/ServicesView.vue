<script setup>
  import { ref, onMounted, inject, watch } from 'vue';
  import InputNumber from 'primevue/inputnumber';
  import apiClient from '@/services/api';
  import { getCompanyIdFromToken } from '@/utils/jwt';
  import { DEFAULT_COMPANY_ID } from '@/config';
  import draggable from 'vuedraggable'; // IMPORTÁLJUK A DRAGGABLE-T

  const services = ref([]); // Ez tárolja a nyers adatokat
  const categories = ref([]); // Ez tárolja a DRAGGABLE struktúrát (Nested)
  const loading = ref(true);
  const isLoggedIn = ref(false);

  const company = inject('company', ref(null));

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

  /* --- ADATTRANSZFORMÁCIÓ (Flat <-> Nested) --- */

  // Lapos listából (Backend) -> Ágyazott lista (Frontend Drag & Drop)
  const buildNestedStructure = (flatServices) => {
    const groups = [];

    // Először rendezzük OrderIndex szerint
    flatServices.sort((a, b) => a.orderIndex - b.orderIndex);

    flatServices.forEach(service => {
      const catName = service.category || "Egyéb";

      // Megkeressük, létezik-e már a kategória a csoportokban
      let group = groups.find(g => g.categoryName === catName);

      if (!group) {
        group = {
          id: 'cat-' + catName, // Egyedi ID a drag-nek
          categoryName: catName,
          // Fejléc variánsok kinyerése az első nem-megjegyzés elemből
          headerVariants: [],
          items: []
        };
        groups.push(group);
      }

      // Ha még nincs header és ez nem megjegyzés, beállítjuk
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

      // Adattisztítás
      rawServices.forEach(service => {
        if (service.variants) {
          service.variants = sortVariants(service.variants);
          service.variants.forEach(v => { if (v.price === 0) v.price = null; });
        }
        if (!service.category) service.category = "Egyéb";
      });

      // Átalakítás nested struktúrává a draggable-hez
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

  const saveService = async (serviceItem) => {
    try {
      const payload = JSON.parse(JSON.stringify(serviceItem));
      if (payload.variants) {
        payload.variants.forEach(v => {
          if (v.price === null || v.price === undefined) v.price = 0;
        });
      }
      // Put hívás... (A válasz feldolgozása itt most egyszerűsített, mert a drag-and-drop miatt a UI a mester)
      await apiClient.put(`/api/Service/${serviceItem.id}`, payload);
    } catch (err) {
      console.error("Hiba a mentesnel:", err);
    }
  };

  /* --- DRAG & DROP ESEMÉNYKEZELÉS --- */

  // Amikor a szolgáltatásokat mozgatják (Kategórián belül vagy között)
  const onServiceDragChange = async (event, group) => {
    // Az esemény lehet 'added', 'removed', vagy 'moved'
    // Minket az érdekel, ha valami bekerült ('added') vagy helyben mozgott ('moved')

    // 1. Ha új kategóriába került, frissíteni kell a kategória nevét
    if (event.added) {
      const item = event.added.element;
      item.category = group.categoryName;
      // Ha ez az első elem és volt fejléc, megpróbálhatjuk igazítani, de most hagyjuk egyszerűen
    }

    // 2. Mindenkinek újraosztjuk az OrderIndex-et ebben a csoportban
    // (Egyszerűsítés: durván újraindexelünk 10-esével, hogy legyen hely később beszúrni)
    const updates = [];
    let baseIndex = group.items[0]?.orderIndex || 0;
    // Ha nagyon az elejére húztuk, korrigálunk
    if (baseIndex < 10) baseIndex = 10;

    group.items.forEach((item, index) => {
      // Az új index: Az előző csoport utolsó indexe + (index * 10) lenne a legprecízebb,
      // de most egyszerűsítsünk: a csoporton belüli sorrend a döntő.
      // A backend globális OrderIndexet vár.
      // TRÜKK: A UI-on lévő sorrend a valóság.
      // Végigmegyünk az ÖSSZES kategórián, és sorban kiosztjuk az indexeket.
    });

    await reorderAll();
  };

  // Kategóriák mozgatása
  const onCategoryDragChange = async () => {
    await reorderAll();
  };

  // Globális újrarendezés és mentés
  // Ez egy kicsit "költséges", de bombabiztos: végigmegy a teljes listán a képernyőn,
  // és mindenkinek kioszt egy új sorszámot (10, 20, 30...), majd elküldi a változásokat.
  const reorderAll = async () => {
    let counter = 10;
    const promises = [];

    categories.value.forEach(group => {
      group.items.forEach(item => {
        // Csak akkor mentünk, ha változott az index vagy a kategória
        if (item.orderIndex !== counter || item.category !== group.categoryName) {
          item.orderIndex = counter;
          item.category = group.categoryName;
          promises.push(saveService(item));
        }
        counter += 10;
      });
    });

    if (promises.length > 0) {
      await Promise.all(promises);
      // Opcionális: fetchServices(); // Ha biztosra akarunk menni, újratölthetünk, de akkor villanhat
    }
  };


  /* --- EGYÉB FUNKCIÓK (Maradtak a régiek) --- */

  const updateCategoryName = async (group, newName) => {
    group.categoryName = newName; // UI frissítés
    // Minden itemet frissítünk
    const promises = group.items.map(service => {
      service.category = newName;
      return saveService(service);
    });
    await Promise.all(promises);
  };

  // Fejléc variáns nevek mentése
  const updateGroupVariantName = async (group, variantIndex, newName) => {
    const promises = group.items.map(service => {
      if (service.variants && service.variants[variantIndex]) {
        service.variants[variantIndex].variantName = newName;
        return saveService(service);
      }
      return Promise.resolve();
    });
    await Promise.all(promises);
  };

  const createNewCategory = async () => {
    if (!isLoggedIn.value) return;
    const newService = {
      name: "Új szolgáltatás",
      category: "ÚJ KATEGÓRIA",
      defaultPrice: 0,
      orderIndex: 9999, // A reorderAll majd helyreteszi
      variants: [{ variantName: "Std", price: 0, duration: 30 }]
    };
    await postNewService(newService);
  };

  const addServiceToGroupEnd = async (group) => {
    const newService = {
      name: "Új szolgáltatás",
      category: group.categoryName,
      defaultPrice: 0,
      orderIndex: 9999,
      variants: [{ variantName: "Std", price: 0, duration: 30 }]
    };
    await postNewService(newService);
  };

  const postNewService = async (dto) => {
    try {
      const payload = JSON.parse(JSON.stringify(dto));
      payload.variants.forEach(v => { v.price = 0; });
      await apiClient.post('/api/Service', payload);
      await fetchServices(); // Itt muszáj újratölteni, hogy bekerüljön a struktúrába
    } catch (err) { console.error(err); }
  };

  const deleteService = async (id) => {
    if (!confirm("Biztosan törölni akarod?")) return;
    try {
      await apiClient.delete(`/api/Service/${id}`);
      await fetchServices();
    } catch (err) { console.error(err); }
  };

  // Variáns kezelők...
  const removeVariant = async (service, vIndex) => {
    service.variants.splice(vIndex, 1);
    await saveService(service);
  };
  const addVariantToService = async (service) => {
    if (!service.variants) service.variants = [];
    service.variants.push({ id: 0, variantName: "Extra", price: 0, duration: 30 });
    await saveService(service);
  };

  onMounted(() => {
    const token = localStorage.getItem('salon_token');
    isLoggedIn.value = !!token;
  });
</script>

<template>
  <div class="smart-container">
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
              <div v-if="isLoggedIn" class="drag-handle-cat" title="Kategória mozgatása">
                ⋮⋮
              </div>

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
                <div class="table-row-wrapper">

                  <div v-if="service.variants && service.variants.length > 0" class="table-row data-row">

                    <div v-if="isLoggedIn" class="drag-handle-item">⋮⋮</div>

                    <div class="col-name">
                      <input v-if="isLoggedIn" v-model="service.name" @change="saveService(service)" class="name-input" />
                      <span v-else class="name-text">{{ service.name }}</span>

                      <div v-if="isLoggedIn" class="row-tools">
                        <button @click="addVariantToService(service)" title="Oszlop +" class="icon-btn tiny">+</button>
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
                                       @blur="saveService(service)" />
                          <span v-else class="price-display">
                            {{ formatCurrency(variant.price) }}
                          </span>
                        </div>
                        <button v-if="isLoggedIn" @click="removeVariant(service, vIndex)" class="variant-remove-btn">×</button>
                      </div>
                    </div>
                  </div>

                  <div v-else class="table-row note-row">
                    <div v-if="isLoggedIn" class="drag-handle-item">⋮⋮</div>

                    <div class="note-cell">
                      <input v-if="isLoggedIn" v-model="service.name" @change="saveService(service)" class="note-input" placeholder="Megjegyzés..." />
                      <span v-else class="note-text">{{ service.name }}</span>
                    </div>

                    <div v-if="isLoggedIn" class="row-tools note-tools">
                      <button @click="deleteService(service.id)" class="icon-btn trash">🗑</button>
                    </div>
                  </div>

                </div>
              </template>
            </draggable>

            <div v-if="isLoggedIn" class="group-footer">
              <button @click="addServiceToGroupEnd(group)" class="add-row-btn">
                + Sor hozzáadása
              </button>
            </div>

          </div>
        </template>
      </draggable>

    </div>
  </div>
</template>

<style scoped>
  .smart-container {
    max-width: 1100px;
    margin: 0 auto;
    padding: 20px;
    box-sizing: border-box;
  }

  .header-actions {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 30px;
  }

  h2 {
    font-weight: 300;
    color: #444;
    margin: 0;
  }

  /* DRAG HANDLES */
  .drag-handle-cat {
    cursor: grab;
    font-size: 1.5rem;
    color: #d4af37;
    margin-right: 15px;
    line-height: 1;
    padding: 5px;
  }

  .drag-handle-item {
    cursor: grab;
    color: #ccc;
    margin-right: 10px;
    font-size: 1.2rem;
    line-height: 1;
    display: flex;
    align-items: center;
  }

    .drag-handle-item:hover {
      color: #666;
    }

  /* GOMBOK */
  .main-add-btn {
    background-color: #333;
    color: #fff;
    border: none;
    padding: 10px 20px;
    border-radius: 4px;
    cursor: pointer;
    font-weight: 600;
    display: flex;
    align-items: center;
    gap: 8px;
  }

  .add-row-btn {
    background: none;
    border: 1px dashed #ccc;
    color: #888;
    width: 100%;
    padding: 8px;
    margin-top: 5px;
    cursor: pointer;
    border-radius: 4px;
    font-size: 0.9rem;
  }

    .add-row-btn:hover {
      background: #f9f9f9;
      color: #333;
      border-color: #bbb;
    }

  /* KATEGÓRIA */
  .category-block {
    margin-bottom: 40px;
    background: #fff;
    border-radius: 4px;
  }
  /* Ha dragolunk, adjunk neki kis hátteret */
  .sortable-ghost {
    opacity: 0.5;
    background: #f0f0f0;
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
    letter-spacing: 1px;
  }

    .category-input:focus {
      outline: none;
      border-bottom: 1px solid #d4af37;
    }

  .category-display {
    font-size: 1.2rem;
    font-weight: bold;
    color: #333;
    text-transform: uppercase;
    letter-spacing: 1px;
  }

  .header-variant-input {
    width: 100%;
    text-align: center;
    border: none;
    background: transparent;
    font-weight: 600;
    color: #666;
    font-size: 0.85rem;
    text-transform: uppercase;
    resize: none;
    overflow: hidden;
    font-family: inherit;
  }

    .header-variant-input:focus {
      background: #fff;
      outline: 1px solid #d4af37;
    }

  .header-label {
    font-weight: 600;
    color: #666;
    font-size: 0.85rem;
    text-transform: uppercase;
    text-align: center;
    display: block;
    width: 100%;
  }

  /* ADATSOROK */
  .data-row {
    display: flex;
    align-items: center;
    padding: 8px 0;
    border-bottom: 1px dashed rgba(0,0,0,0.05);
    background: #fff;
  }

    .data-row:hover {
      background-color: #fcfcfc;
    }

  /* MEGJEGYZÉS */
  .note-row {
    display: flex;
    padding: 8px 0;
    margin-top: 5px;
    margin-bottom: 5px;
    align-items: center;
  }

  .note-cell {
    flex-grow: 1;
    font-style: italic;
    color: #777;
    padding-left: 10px;
    border-left: 2px solid #eee;
  }

  .note-input {
    width: 100%;
    border: none;
    background: transparent;
    font-style: italic;
    color: #666;
  }

    .note-input:focus {
      outline: none;
      background: #fff;
    }

  .note-text {
    display: block;
    width: 100%;
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
    color: #444;
  }

    .name-input:focus {
      outline: none;
      border-bottom: 1px solid #d4af37;
    }

  .name-text {
    font-size: 1rem;
    color: #555;
  }

  .data-row:hover .name-text {
    color: #000;
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
  }

  .header-item {
    align-items: flex-end;
    padding-bottom: 0;
    min-height: 40px;
  }

  /* TOOLS */
  .row-tools {
    display: flex;
    align-items: center;
    gap: 5px;
    margin-left: 15px;
    opacity: 0;
    transition: opacity 0.2s;
  }

  .note-row:hover .row-tools, .data-row:hover .row-tools {
    opacity: 1;
  }

  .icon-btn {
    border: none;
    background: none;
    cursor: pointer;
    color: #ccc;
    font-size: 1rem;
  }

    .icon-btn:hover {
      color: #333;
    }

    .icon-btn.trash:hover {
      color: #d9534f;
    }

    .icon-btn.tiny {
      font-size: 1.2rem;
      line-height: 1rem;
    }

  .variant-remove-btn {
    position: absolute;
    top: -8px;
    right: 0;
    border: none;
    background: none;
    color: #d9534f;
    opacity: 0;
    cursor: pointer;
  }

  .col-variant-item:hover .variant-remove-btn {
    opacity: 1;
  }

  .price-display {
    color: #666;
    font-family: inherit;
  }

  .data-row:hover .price-display {
    color: #000;
    font-weight: 500;
  }

  .price-input {
    width: 100px !important;
  }

    .price-input :deep(input) {
      border: none;
      background: transparent;
      text-align: center;
      color: #555;
      padding: 0;
      font-family: inherit;
    }

      .price-input :deep(input):focus {
        background: white;
        box-shadow: 0 0 0 1px #d4af37;
      }
</style>
