<script setup>
  import { ref, onMounted, computed, inject, watch } from 'vue';
  import InputNumber from 'primevue/inputnumber';
  import apiClient from '@/services/api';
  import { getCompanyIdFromToken } from '@/utils/jwt';
  import { DEFAULT_COMPANY_ID } from '@/config';

  const services = ref([]);
  const loading = ref(true);
  const isLoggedIn = ref(false); // Admin státusz

  const company = inject('company', ref(null));

  /* --- Stabil sorrend biztosítása --- */
  const sortVariants = (variants) => {
    if (!variants) return [];
    return variants.sort((a, b) => {
      if (a.id === 0 && b.id === 0) return 0;
      if (a.id === 0) return 1;
      if (b.id === 0) return -1;
      return a.id - b.id;
    });
  };

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
      });

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

  const areVariantSignaturesEqual = (variantsA, variantsB) => {
    if (!variantsA || !variantsB) return false;
    if (variantsA.length !== variantsB.length) return false;
    for (let i = 0; i < variantsA.length; i++) {
      if (variantsA[i].variantName !== variantsB[i].variantName) return false;
    }
    return true;
  }

  const displayList = computed(() => {
    if (!services.value) return [];

    return services.value.map((service, index) => {
      const isTreeMode = service.variants && service.variants.length >= 4;
      let showHeader = false;
      if (!isTreeMode) {
        if (index === 0) {
          showHeader = service.variants && service.variants.length > 0;
        } else {
          const prevService = services.value[index - 1];
          const prevWasTree = prevService?.variants && prevService.variants.length >= 4;
          if (prevWasTree || !areVariantSignaturesEqual(service.variants, prevService?.variants)) {
            showHeader = service.variants && service.variants.length > 0;
          }
        }
      }
      return { ...service, showHeader, isTreeMode };
    });
  });

  const insertNewService = async () => {
    if (!isLoggedIn.value) return;
    const prevService = services.value[services.value.length - 1];
    let newVariants = [];
    if (prevService && prevService.variants && prevService.variants.length < 4) {
      newVariants = prevService.variants.map(v => ({
        variantName: v.variantName,
        price: null,
        duration: v.duration
      }));
    }
    const newService = {
      name: "Uj szolgaltatas",
      defaultPrice: 0,
      orderIndex: (prevService?.orderIndex || 0) + 10,
      variants: newVariants
    };
    try {

      const payload = JSON.parse(JSON.stringify(newService));
      if (payload.variants) {
        payload.variants.forEach(v => {
          if (!v.price) v.price = 0;
        });
      }

      await apiClient.post('/api/Service', newService);
      await fetchServices();
    } catch (err) {
      console.error("Hiba a letrehozasnal:", err);
    }
  };

  const removeVariant = async (service, variantIndex) => {
    service.variants.splice(variantIndex, 1);
    await saveService(service);
  };

  const addVariantToService = async (service) => {
    if (!service.variants) service.variants = [];
    service.variants.push({
      id: 0,
      variantName: "Uj tipus",
      price: null,
      duration: 30
    });
    await saveService(service);
  };

  const saveService = async (serviceItem) => {
    try {

      const payload = JSON.parse(JSON.stringify(serviceItem));
      if (payload.variants) {
        payload.variants.forEach(v => {
          if (v.price === null || v.price === undefined) {
            v.price = 0;
          }
        });
      }

      const response = await apiClient.put(`/api/Service/${serviceItem.id}`, serviceItem);
      if (response.status === 200 && response.data) {
        const index = services.value.findIndex(s => s.id === serviceItem.id);
        if (index !== -1) {
          const updatedService = response.data;

          if (updatedService.variants) {
            updatedService.variants = sortVariants(updatedService.variants);
            updatedService.variants.forEach(v => {
              if (v.price === 0) v.price = null;
            });
          }
          services.value[index] = { ...services.value[index], ...response.data };
        }
      }
    } catch (err) {
      console.error("Hiba a mentesnel:", err);
    }
  };

  const deleteService = async (id) => {
    if (!confirm("Biztosan torolni akarod ezt a szolgaltatast?")) return;
    try {
      await apiClient.delete(`/api/Service/${id}`);
      await fetchServices();
    } catch (err) {
      console.error("Hiba a torlesnel:", err);
    }
  };

  const formatCurrency = (val) => {
    if (val === null || val === undefined || val === 0) return '';
    return val.toLocaleString('hu-HU', { style: 'currency', currency: 'EUR', maximumFractionDigits: 0 });
  };

  onMounted(() => {
    const token = localStorage.getItem('salon_token');
    isLoggedIn.value = !!token;
  });
</script>

<template>
  <div class="smart-container">
    <div class="header-actions">
      <h2>Árlista Szerkesztő</h2>
      <button v-if="isLoggedIn" @click="insertNewService" class="add-btn">
        <i class="pi pi-plus"></i> Új sor hozzáadása
      </button>
    </div>

    <div v-if="loading">Betöltés...</div>

    <div v-else class="services-wrapper">

      <div v-for="(service) in displayList" :key="service.id" class="service-block">

        <div v-if="service.showHeader" class="table-row header-row">
          <div class="col-name spacer"></div>
          <div class="col-variants-group">
            <div v-for="(v, vIndex) in service.variants" :key="vIndex" class="col-variant-item header-item">

              <textarea v-if="isLoggedIn"
                        v-model="v.variantName"
                        @change="saveService(service)"
                        class="header-input"
                        rows="2"></textarea>

              <span v-else class="header-label">{{ v.variantName }}</span>
            </div>
          </div>
        </div>

        <div class="table-row data-row">
          <div class="col-name">
            <input v-if="isLoggedIn" v-model="service.name" @change="saveService(service)" class="name-input" />
            <span v-else class="name-text">{{ service.name }}</span>

            <div v-if="isLoggedIn" class="row-tools">
              <button @click="addVariantToService(service)" title="Variáció hozzáadása (+)" class="icon-btn">+</button>
              <button @click="deleteService(service.id)" title="Sor törlése" class="icon-btn trash">🗑</button>
            </div>
          </div>

          <div class="col-variants-group">
            <div v-for="(variant, vIndex) in service.variants" :key="vIndex" class="col-variant-item">
              <div class="price-wrapper">
                <InputNumber v-if="isLoggedIn"
                             v-model="variant.price"
                             mode="currency" currency="EUR" locale="hu-HU" :minFractionDigits="0"
                             class="price-input"
                             @blur="saveService(service)" />
                <span v-else class="price-display">
                  {{ formatCurrency(variant.price) }}
                </span>
              </div>
              <button v-if="isLoggedIn" @click="removeVariant(service, vIndex)" class="variant-remove-btn" title="Variáció törlése">×</button>
            </div>
          </div>
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

  .services-wrapper {
    display: flex;
    flex-direction: column;
    padding-right: 10px;
  }

  /* --- STRUKTÚRA --- */

  .table-row {
    display: flex;
    align-items: center;
    padding: 8px 0;
  }

  .col-name {
    flex-grow: 1;
    display: flex;
    align-items: center;
    padding-right: 15px;
    min-width: 180px;
    overflow: hidden;
  }

  .col-variants-group {
    display: flex;
    justify-content: flex-end;
    gap: 15px;
    flex-shrink: 0;
  }

  .col-variant-item {
    width: 130px; /* Fix szélesség */
    display: flex;
    align-items: center;
    justify-content: center;
    position: relative;
    text-align: center;
  }

  .header-item {
    align-items: flex-end; /* Alulra igazítjuk, hogy az ár felett legyen */
    padding-bottom: 5px;
    min-height: 40px; /* Hagyunk helyet a 2 sornak */
  }

  /* --- STÍLUSOK --- */

  /* Fejléc (Header) */
  .header-row {
    margin-top: 30px;
    padding-bottom: 5px;
    border-bottom: 1px solid #ddd;
    margin-bottom: 5px;
  }

  /* TEXTAREA és SPAN stílusok (Tördelés engedélyezése) */
  .header-input, .header-label {
    width: 100%;
    text-align: center;
    border: none;
    background: transparent;
    font-weight: 600;
    color: #777;
    font-size: 0.85rem;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    /* A kulcs a tördeléshez */
    white-space: normal;
    word-wrap: break-word;
    line-height: 1.2;
    font-family: inherit;
  }

  /* Külön stílus a Textarea-nak, hogy ne nézzen ki szövegdoboznak */
  .header-input {
    resize: none; /* Ne lehessen átméretezni egérrel */
    overflow: hidden; /* Gördítősáv elrejtése */
    height: auto;
    padding: 0;
    display: flex;
    align-items: center;
    justify-content: center;
  }

    .header-input:focus {
      outline: 1px solid #d4af37;
      background: #fff;
    }

  /* Adat sor (Data Row) */
  .data-row {
    border-bottom: 1px dashed rgba(0,0,0,0.05);
    transition: background-color 0.2s;
  }

    .data-row:hover {
      background-color: #fcfcfc;
    }

  .name-text {
    font-weight: 400;
    color: #555;
    font-size: 1rem;
    line-height: 1.4;
  }

  .name-input {
    width: 100%;
    border: none;
    font-size: 1rem;
    background: transparent;
    color: #555;
  }

    .name-input:focus {
      outline: none;
      border-bottom: 1px solid #d4af37;
    }

  .price-display {
    font-family: inherit;
    color: #666;
    font-weight: 400;
    font-size: 1rem;
  }

  .data-row:hover .price-display,
  .data-row:hover .name-text {
    color: #333;
  }

  /* Eszközök */
  .row-tools {
    margin-left: 10px;
    display: flex;
    align-items: center;
    flex-shrink: 0;
  }

  .icon-btn {
    background: none;
    border: none;
    cursor: pointer;
    opacity: 0;
    transition: opacity 0.2s;
    font-size: 1rem;
    padding: 0 4px;
    color: #bbb;
  }

  .data-row:hover .icon-btn {
    opacity: 1;
  }

  .icon-btn:hover {
    color: #555;
  }

  .icon-btn.trash:hover {
    color: #d9534f;
  }

  .variant-remove-btn {
    position: absolute;
    top: -10px;
    right: 0;
    border: none;
    background: none;
    color: #d9534f;
    cursor: pointer;
    font-size: 1.1rem;
    opacity: 0;
  }

  .col-variant-item:hover .variant-remove-btn {
    opacity: 1;
  }

  /* PrimeVue Input */
  .price-input {
    width: 100px !important;
  }

    .price-input :deep(input) {
      border: none;
      background: transparent;
      text-align: center;
      color: #666;
      font-family: inherit;
      padding: 0;
    }

      .price-input :deep(input):focus {
        background: white;
        box-shadow: 0 0 0 1px #d4af37;
      }

  .add-btn {
    background-color: #d4af37;
    color: white;
    border: none;
    padding: 8px 16px;
    border-radius: 4px;
    cursor: pointer;
    font-weight: 500;
  }

    .add-btn:hover {
      background-color: #b5952f;
    }

  .header-actions {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;
  }

  h2 {
    font-weight: 300;
    color: #444;
  }
</style>
