<script setup>
  import { ref, onMounted, computed, inject, watch } from 'vue';
  import InputNumber from 'primevue/inputnumber';
  import apiClient from '@/services/api';
  import { getCompanyIdFromToken } from '@/utils/jwt';
  import { DEFAULT_COMPANY_ID } from '@/config';

  const services = ref([]);
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

  /* --- ADATBETÖLTÉS --- */

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
          service.variants.forEach(v => {
            if (v.price === 0) v.price = null;
          });
        }
        if (!service.category) service.category = "Egyéb";
      });

      // Fontos: Itt is rendezzük OrderIndex szerint, bár a backend is megteszi
      rawServices.sort((a, b) => a.orderIndex - b.orderIndex);

      services.value = rawServices;

    } catch (error) {
      console.error('Hiba a betolteskor:', error);
    } finally {
      loading.value = false;
    }
  };

  watch(
    () => company?.value?.id,
    (newId) => {
      if (newId) fetchServices();
    },
    { immediate: true }
  );

  /* --- CSOPORTOSÍTÁS LOGIKA --- */

  const groupedServices = computed(() => {
    if (!services.value) return [];

    const groups = [];
    let currentGroup = null;

    services.value.forEach(service => {
      const catName = service.category || "Egyéb";

      // Ha a sor egy MEGJEGYZÉS (nincs variánsa), az nem töri meg a csoport fejléc logikáját,
      // de a megjelenítésnél külön kezeljük.

      if (!currentGroup || currentGroup.categoryName !== catName) {

        // Keresünk egy olyan elemet a csoportban, ami NEM megjegyzés, hogy abból vegyük a fejlécet
        // (Ha az első elem megjegyzés, akkor várunk a következőre)
        let headerSource = service.variants && service.variants.length > 0 ? service : null;

        currentGroup = {
          categoryName: catName,
          headerVariants: headerSource ? [...headerSource.variants] : [],
          items: []
        };
        groups.push(currentGroup);
      }

      // Ha még nincs header beállítva (mert az első elem megjegyzés volt), de ez most egy normál service
      if (currentGroup.headerVariants.length === 0 && service.variants && service.variants.length > 0) {
        currentGroup.headerVariants = [...service.variants];
      }

      currentGroup.items.push(service);
    });

    return groups;
  });

  /* --- SZERKESZTÉS & MENTÉS --- */

  const saveService = async (serviceItem) => {
    try {
      const payload = JSON.parse(JSON.stringify(serviceItem));
      if (payload.variants) {
        payload.variants.forEach(v => {
          if (v.price === null || v.price === undefined) v.price = 0;
        });
      }

      const response = await apiClient.put(`/api/Service/${serviceItem.id}`, payload);

      if (response.status === 200 && response.data) {
        const index = services.value.findIndex(s => s.id === serviceItem.id);
        if (index !== -1) {
          const updated = response.data;
          if (updated.variants) {
            updated.variants = sortVariants(updated.variants);
            updated.variants.forEach(v => { if (v.price === 0) v.price = null; });
          }
          if (!updated.category) updated.category = "Egyéb";

          services.value[index] = { ...services.value[index], ...updated };
        }
      }
    } catch (err) {
      console.error("Hiba a mentesnel:", err);
    }
  };

  const updateCategoryName = async (group, newName) => {
    const promises = group.items.map(service => {
      service.category = newName;
      return saveService(service);
    });
    await Promise.all(promises);
  };

  const updateGroupVariantName = async (group, variantIndex, newName) => {
    const promises = group.items.map(service => {
      // Csak azokat frissítjük, amik NEM megjegyzések (van variánsuk)
      if (service.variants && service.variants[variantIndex]) {
        service.variants[variantIndex].variantName = newName;
        return saveService(service);
      }
      return Promise.resolve();
    });
    await Promise.all(promises);
  };

  /* --- LÉTREHOZÁS & BESZÚRÁS LOGIKA (ÚJ) --- */

  // Kiszámolja a megfelelő indexet a beszúráshoz
  const getNextOrderIndex = (currentService, groupItems) => {
    if (!currentService) return 0;

    const currentIndex = groupItems.findIndex(s => s.id === currentService.id);
    const nextService = groupItems[currentIndex + 1];

    const currentOrder = currentService.orderIndex;

    if (nextService) {
      // Ha van következő, akkor a kettő közé
      const nextOrder = nextService.orderIndex;
      // Matematikai közép (pl. 10 és 20 között -> 15)
      const mid = Math.floor((currentOrder + nextOrder) / 2);

      // Ha nincs hely (pl. 10 és 11 között), akkor toljuk el (egyelőre +1, a backend order stabil)
      if (mid <= currentOrder) return currentOrder + 1;
      return mid;
    } else {
      // Ha ez az utolsó, akkor +10
      return currentOrder + 10;
    }
  };

  // Új sor (vagy megjegyzés) beszúrása egy adott sor ALÁ
  const insertServiceBelow = async (currentService, group, isNote = false) => {
    if (!isLoggedIn.value) return;

    const newOrderIndex = getNextOrderIndex(currentService, group.items);
    let newVariants = [];

    if (isNote) {
      // Megjegyzésnél üres a variáns lista
      newVariants = [];
    } else {
      // Normál szerviznél másoljuk a struktúrát a csoportból (vagy az aktuálisból)
      // Ha az aktuális sor is Note volt, akkor a csoport fejlécéből próbáljuk kitalálni a struktúrát
      const templateVariants = (currentService.variants && currentService.variants.length > 0)
        ? currentService.variants
        : (group.headerVariants.length > 0 ? group.headerVariants : []);

      if (templateVariants.length > 0) {
        newVariants = templateVariants.map(v => ({
          variantName: v.variantName,
          price: 0,
          duration: v.duration
        }));
      } else {
        // Fallback, ha semmi nincs
        newVariants = [{ variantName: "Normál", price: 0, duration: 30 }];
      }
    }

    const newService = {
      name: isNote ? "Új megjegyzés..." : "Új szolgáltatás",
      category: group.categoryName,
      defaultPrice: 0,
      orderIndex: newOrderIndex,
      variants: newVariants
    };

    await postNewService(newService);
  };

  // Kategória végi hozzáadás (gomb a lista alján)
  const addServiceToGroupEnd = async (group) => {
    const lastItem = group.items[group.items.length - 1];
    await insertServiceBelow(lastItem, group, false); // False = Normál sor
  };

  const createNewCategory = async () => {
    if (!isLoggedIn.value) return;
    const lastService = services.value[services.value.length - 1];
    const newOrderIndex = (lastService?.orderIndex || 0) + 100;

    const defaultVariants = [
      { variantName: "Rövid", price: 0, duration: 30 },
      { variantName: "Közép", price: 0, duration: 45 },
      { variantName: "Hosszú", price: 0, duration: 60 }
    ];

    const newService = {
      name: "Új szolgáltatás",
      category: "ÚJ KATEGÓRIA",
      defaultPrice: 0,
      orderIndex: newOrderIndex,
      variants: defaultVariants
    };

    await postNewService(newService);
  };

  const postNewService = async (dto) => {
    try {
      const payload = JSON.parse(JSON.stringify(dto));
      payload.variants.forEach(v => { v.price = 0; });
      await apiClient.post('/api/Service', payload);
      await fetchServices();
    } catch (err) {
      console.error("Letrehozas hiba:", err);
    }
  };

  const deleteService = async (id) => {
    if (!confirm("Biztosan törölni akarod?")) return;
    try {
      await apiClient.delete(`/api/Service/${id}`);
      await fetchServices();
    } catch (err) {
      console.error("Torles hiba:", err);
    }
  };

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

      <div v-for="(group, gIndex) in groupedServices" :key="gIndex" class="category-block">

        <div class="table-row header-row">
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

        <div v-for="service in group.items" :key="service.id" class="table-row-wrapper">

          <div v-if="service.variants && service.variants.length > 0" class="table-row data-row">

            <div class="col-name">
              <input v-if="isLoggedIn" v-model="service.name" @change="saveService(service)" class="name-input" />
              <span v-else class="name-text">{{ service.name }}</span>

              <div v-if="isLoggedIn" class="row-tools">
                <div class="insert-tools">
                  <button @click="insertServiceBelow(service, group, false)" title="Új sor beszúrása ez alá" class="icon-btn add-below">↳ Sor</button>
                  <button @click="insertServiceBelow(service, group, true)" title="Megjegyzés beszúrása ez alá" class="icon-btn add-note">↳ Note</button>
                </div>
                <span class="tool-separator">|</span>
                <button @click="addVariantToService(service)" title="Oszlop hozzáadása" class="icon-btn tiny">+</button>
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
                <button v-if="isLoggedIn" @click="removeVariant(service, vIndex)" class="variant-remove-btn" title="Törlés">×</button>
              </div>
            </div>
          </div>

          <div v-else class="table-row note-row">
            <div class="note-cell">
              <input v-if="isLoggedIn" v-model="service.name" @change="saveService(service)" class="note-input" placeholder="Megjegyzés..." />
              <span v-else class="note-text">{{ service.name }}</span>
            </div>

            <div v-if="isLoggedIn" class="row-tools note-tools">
              <div class="insert-tools">
                <button @click="insertServiceBelow(service, group, false)" title="Új sor beszúrása ez alá" class="icon-btn add-below">↳ Sor</button>
                <button @click="insertServiceBelow(service, group, true)" title="Megjegyzés beszúrása ez alá" class="icon-btn add-note">↳ Note</button>
              </div>
              <button @click="deleteService(service.id)" class="icon-btn trash">🗑</button>
            </div>
          </div>

        </div> <div v-if="isLoggedIn" class="group-footer">
          <button @click="addServiceToGroupEnd(group)" class="add-row-btn">
            + Sor hozzáadása a kategória végére
          </button>
        </div>

      </div>

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
    transition: background 0.2s;
  }

    .main-add-btn:hover {
      background-color: #000;
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

  /* --- KATEGÓRIA BLOKK --- */
  .category-block {
    margin-bottom: 40px;
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

  /* --- ADATSOROK --- */
  .table-row-wrapper {
    /* Csak wrapper */
  }

  .data-row {
    display: flex;
    align-items: center;
    padding: 8px 0;
    border-bottom: 1px dashed rgba(0,0,0,0.05);
  }

    .data-row:hover {
      background-color: #fcfcfc;
    }

  /* MEGJEGYZÉS SOR STÍLUS */
  .note-row {
    display: flex;
    padding: 8px 0;
    border-bottom: 1px solid transparent;
    margin-top: 5px;
    margin-bottom: 5px;
  }

  .note-cell {
    flex-grow: 1;
    font-style: italic;
    color: #777;
    padding-left: 10px;
    border-left: 2px solid #eee; /* Kicsit elüt a többitől */
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

  /* --- TOOLS --- */
  .row-tools {
    display: flex;
    align-items: center;
    gap: 5px;
    margin-left: 15px;
    opacity: 0;
    transition: opacity 0.2s;
  }
  /* Megjegyzés sornál is jelenjen meg a tools */
  .note-row:hover .row-tools,
  .data-row:hover .row-tools {
    opacity: 1;
  }

  .insert-tools {
    display: flex;
    gap: 3px;
    margin-right: 5px;
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

    .icon-btn.add-below, .icon-btn.add-note {
      font-size: 0.75rem;
      background: #f0f0f0;
      padding: 2px 5px;
      border-radius: 3px;
      color: #666;
    }

      .icon-btn.add-below:hover, .icon-btn.add-note:hover {
        background: #e0e0e0;
        color: #000;
      }

  .tool-separator {
    color: #eee;
    margin: 0 5px;
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

  /* PrimeVue Override */
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
