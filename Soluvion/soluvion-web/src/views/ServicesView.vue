<script setup>
  import { ref, onMounted, computed } from 'vue';
  import InputNumber from 'primevue/inputnumber';
  import apiClient from '@/services/api';
  import { getCompanyIdFromToken } from '../utils/jwt';
 
  //import DataTable from 'primevue/datatable';
  //import Column from 'primevue/column';
  //import Dialog from 'primevue/dialog';
  //import InputText from 'primevue/inputtext';

  const services = ref([]);
  const loading = ref(true);
  const isLoggedIn = ref(false); // Admin státusz

  const fetchServices = async () => {
    loading.value = true;

    const targetCompanyId = getCompanyIdFromToken() || 7;

    try {
      const response = await apiClient.get('/api/Service', {
        params: { companyId: targetCompanyId }
      });
      services.value = response.data;
    } catch (error) {
      console.error('Hiba a betolteskor:', error);
    } finally {
      loading.value = false;
    }
  };


  const areVariantSignaturesEqual = (variantsA, variantsB) => {
    if (!variantsA || !variantsB) return false;
    if (variantsA.length !== variantsB.length) return false;

    for (let i = 0; i < variantsA.length; i++) {
      if (variantsA[i].variantName !== variantsB[i].variantName) return false;
    }

    return true;
  }

  const displayList = computed(() => {
    return services.value.map((service, index) => {

      const isTreeMode = service.variants && service.variants.length >= 4;

      let showHeader = false;
      if (!isTreeMode) {
        if (index === 0) {
          showHeader = service.variants && service.variants.length > 0;
        } else {
          const prevService = services.value[index - 1];

          const prevWasTree = prevService.variants && prevService.variants.length >= 4;

          if (prevWasTree || !areVariantSignaturesEqual(service.variants, prevService.variants)) {
            showHeader = service.variants && service.variants.length > 0;
          }
        }
      }

      return {
        ...service,
        showHeader: showHeader,
        isTreeMode: isTreeMode
      };
    });
  });


  const insertNewService = async () => {
    if (!isLoggedIn.value) return;

    const prevService = services.value[services.value.length - 1];
    let newVariants = [];

    if (prevService && prevService.variants && prevService.variants.length < 4) {
      newVariants = prevService.variants.map(v => ({
        variantName: v.variantName,
        price: 0,
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
    if (!service.variants) {
      service.variants = [];
    }

    service.variants.push({
      id: 0,
      variantName: "Uj tipus",
      price: 0,
      duration: 30
    });
    await saveService(service);
  };

  const saveService = async (service) => {
    try {

      const response = await apiClient.put(`/api/Service/${service.id}`, service);

      if (response.status === 200 && response.data) {
        const index = services.value.findIndex(s => s.id === service.id);
        if (index !== -1) {
          service.value[index] = { ...service.value[index], ...response.data };
        }
      }
    } catch (err) {
      console.error("Hiba a mentesnel:", err);
      if (err.response && err.response.data) {
        console.error("Szerver uzenet:", err.response.data);
      }
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

  const formatCurrency = (val) => val ? val.toLocaleString('hu-HU', { style: 'currency:', currency: 'EUR', maximumFractionDigits: 0 }) : '-';

  onMounted(() => {
    // Megnézzük, be van-e lépve
    const token = localStorage.getItem('salon_token');
    isLoggedIn.value = !!token;

    fetchServices();
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

    <div v-else class="grid-layout">

      <div v-for="(service) in displayList" :key="service.id" class="service-block">

        <div v-if="service.showHeader" class="variant-header-row">
          <div class="spacer-cell"></div> <div v-for="(v, vIndex) in service.variants" :key="vIndex" class="variant-label">
            <input v-if="isLoggedIn" v-model="v.variantName" @change="saveService(service)" class="header-input" />
            <span v-else>{{ v.variantName }}</span>
          </div>
        </div>

        <div class="data-row">
          <div class="name-cell">
            <input v-if="isLoggedIn" v-model="service.name" @change="saveService(service)" class="name-input" />
            <span v-else class="name-text">{{ service.name }}</span>

            <div v-if="isLoggedIn" class="row-tools">
              <button @click="addVariantToService(service)" title="Variáció hozzáadása (+)" class="icon-btn">+</button>
              <button @click="deleteService(service.id)" title="Sor törlése" class="icon-btn trash">🗑</button>
            </div>
          </div>

          <div class="variants-container">
            <div v-for="(variant, vIndex) in service.variants" :key="vIndex" class="variant-cell">

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
    max-width: 900px;
    margin: 0 auto;
    padding: 20px;
  }

  .grid-layout {
    display: flex;
    flex-direction: column;
    gap: 5px;
  }

  /* Header Row Stílusok */
  .variant-header-row {
    display: flex;
    margin-top: 25px; /* Nagyobb térköz az új csoportnál */
    padding-bottom: 5px;
    border-bottom: 2px solid #f0f0f0;
    color: #888;
    font-size: 0.9rem;
    font-weight: bold;
  }

  .spacer-cell {
    width: 250px;
    flex-shrink: 0;
  }

  .variant-label {
    width: 120px;
    text-align: center;
    padding: 0 5px;
  }

  .header-input {
    width: 100%;
    text-align: center;
    border: none;
    background: transparent;
    font-weight: bold;
    color: #666;
    cursor: pointer;
  }

    .header-input:focus {
      outline: 1px solid #d4af37;
      background: #fff;
    }

  /* Data Row Stílusok */
  .data-row {
    display: flex;
    align-items: center;
    padding: 8px 0;
    border-bottom: 1px dashed #f0f0f0;
    transition: background-color 0.2s;
  }

    .data-row:hover {
      background-color: #fafafa;
      color: #333 !important;
    }

      .data-row:hover input,
      .data-row:hover .name-text,
      .data-row:hover .header-input,
      .data-row:hover span,
      .data-row:hover .price-display {
        color: #333 !important;
      }
      /* PrimeVue inputok belsejét is színezzük */
      .data-row:hover .price-input :deep(input) {
        color: #333 !important;
      }

      /* Az ikonok is legyenek sötétek hoverkor */
      .data-row:hover .icon-btn {
        opacity: 1;
        color: #666;
      }

      .data-row:hover .icon-btn:hover {
        color: #000;
      }

  .name-cell {
    width: 250px;
    flex-shrink: 0;
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding-right: 15px;
  }

  .name-input {
    width: 100%;
    border: none;
    font-size: 1rem;
    background: transparent;
    font-weight: 500;
  }

    .name-input:focus {
      outline: none;
      border-bottom: 1px solid #d4af37;
    }

  .name-text {
    font-weight: 500;
  }

  .variants-container {
    display: flex;
    flex-wrap: wrap; /* Ha nagyon sok variáció lenne, törjön a sor */
  }

  .variant-cell {
    width: 120px;
    display: flex;
    align-items: center;
    justify-content: center;
    position: relative;
  }

  /* Gombok és Interakciók */
  .icon-btn {
    background: none;
    border: none;
    cursor: pointer;
    opacity: 0; /* Csak hoverre látszik */
    transition: opacity 0.2s;
    font-size: 1.2rem;
    padding: 0 5px;
    color: #aaa;
  }

  .name-cell:hover .icon-btn {
    opacity: 1;
  }

  .icon-btn:hover {
    color: #333;
  }

  .icon-btn.trash:hover {
    color: red;
  }

  .variant-remove-btn {
    position: absolute;
    top: -8px;
    right: 5px;
    background: none;
    border: none;
    color: #ff4444;
    cursor: pointer;
    font-size: 1.2rem;
    opacity: 0;
    transition: opacity 0.2s;
  }

  .variant-cell:hover .variant-remove-btn {
    opacity: 1;
  }

  /* PrimeVue InputNumber felüldefiniálása */
  .price-input {
    width: 90px !important;
  }

    .price-input :deep(input) {
      text-align: center;
      border: 1px solid transparent;
      background: transparent;
      font-family: inherit;
      font-size: 1rem;
    }

      .price-input :deep(input):focus {
        border-color: #d4af37;
        background: white;
        box-shadow: none;
      }

  .add-btn {
    background-color: #d4af37;
    color: white;
    border: none;
    padding: 10px 20px;
    border-radius: 5px;
    cursor: pointer;
    font-weight: bold;
    margin-bottom: 20px;
    display: flex;
    align-items: center;
    gap: 10px;
  }

    .add-btn:hover {
      background-color: #b5952f;
    }

  .header-actions {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }
</style>
