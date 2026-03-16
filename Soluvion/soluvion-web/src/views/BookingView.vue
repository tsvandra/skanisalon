<template>
  <div class="max-w-4xl w-full mx-auto px-4 py-8 md:p-12 box-border bg-background text-text min-h-screen">

    <h1 class="text-3xl font-light mb-8 text-center">{{ company?.name || 'Szalon' }} - Időpontfoglalás</h1>

    <div v-if="loading" class="text-center py-12 text-text-muted">
      <i class="pi pi-spin pi-spinner text-4xl mb-4 block"></i>
      {{ $t('common.loading') }}
    </div>

    <div v-else class="bg-surface rounded-2xl shadow-lg p-6 border border-text/10">

      <Stepper v-model:value="activeStep">
        <StepList>
          <Step value="1">Szolgáltatások</Step>
          <Step value="2">Adataid</Step>
          <Step value="3">Időpont & Foglalás</Step>
        </StepList>

        <StepPanels>
          <StepPanel value="1" v-slot="{ activateCallback }">
            <h3 class="text-xl font-bold mb-4">Válassz szolgáltatást</h3>
            <div class="space-y-4 max-h-[50vh] overflow-y-auto pr-2">
              <div v-for="group in categories" :key="group.id" class="border border-text/10 rounded-lg p-4 bg-background">
                <h4 class="font-bold text-primary uppercase tracking-wider mb-3">{{ group.categoryName[currentLang] }}</h4>
                <div v-for="service in group.items" :key="service.id" class="mb-4 last:mb-0">
                  <div class="font-medium text-lg mb-2">{{ service.name[currentLang] }}</div>
                  <div class="flex flex-wrap gap-2">
                    <label v-for="variant in service.variants" :key="variant.id"
                           class="flex items-center gap-2 p-2 rounded border cursor-pointer transition-colors"
                           :class="bookingData.serviceVariantIds.includes(variant.id) ? 'bg-primary/10 border-primary text-primary' : 'border-text/20 hover:border-primary/50'">
                      <input type="checkbox" :value="variant.id" v-model="bookingData.serviceVariantIds" class="hidden" />
                      <span class="font-bold">{{ variant.variantName[currentLang] }}</span>
                      <span class="text-sm">({{ formatCurrency(variant.price) }})</span>
                    </label>
                  </div>
                </div>
              </div>
            </div>
            <div class="mt-6 flex justify-end">
              <button @click="activateCallback('2')" :disabled="bookingData.serviceVariantIds.length === 0"
                      class="px-6 py-2 bg-primary text-white font-bold rounded-lg disabled:opacity-50 transition-colors">
                Tovább <i class="pi pi-arrow-right ml-2"></i>
              </button>
            </div>
          </StepPanel>

          <StepPanel value="2" v-slot="{ activateCallback }">
            <h3 class="text-xl font-bold mb-4">Add meg az adataidat</h3>
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div class="flex flex-col gap-1">
                <label class="text-sm text-text-muted">Teljes név *</label>
                <input v-model="bookingData.fullName" type="text" name="fullName" autocomplete="name" class="p-2 rounded border border-text/20 bg-background focus:border-primary focus:outline-none" required />
              </div>
              <div class="flex flex-col gap-1">
                <label class="text-sm text-text-muted">Email *</label>
                <input v-model="bookingData.email" type="email" name="email" autocomplete="email" class="p-2 rounded border border-text/20 bg-background focus:border-primary focus:outline-none" required />
              </div>
              <div class="flex flex-col gap-1">
                <label class="text-sm text-text-muted">Telefonszám *</label>
                <input v-model="bookingData.phone" type="tel" name="phone" autocomplete="tel" class="p-2 rounded border border-text/20 bg-background focus:border-primary focus:outline-none" required />
              </div>

              <div class="flex flex-col gap-1">
                <label class="text-sm text-text-muted">Hajhossz (Dinamikus attribútum)</label>
                <select v-model="bookingData.attributes.HairLength" name="hairLength" class="p-2 rounded border border-text/20 bg-background focus:border-primary focus:outline-none">
                  <option value="Rövid">Rövid</option>
                  <option value="Félhosszú">Félhosszú</option>
                  <option value="Hosszú">Hosszú</option>
                  <option value="Extra hosszú">Extra hosszú</option>
                </select>
              </div>

              <div class="flex flex-col gap-1 md:col-span-2">
                <label class="text-sm text-text-muted">Megjegyzés a szalonnak</label>
                <textarea v-model="bookingData.notes" rows="2" name="notes" class="p-2 rounded border border-text/20 bg-background focus:border-primary focus:outline-none"></textarea>
              </div>
            </div>

            <div class="mt-6 flex justify-between">
              <button @click="activateCallback('1')" class="px-6 py-2 border border-text/20 text-text rounded-lg transition-colors hover:bg-text/5">
                <i class="pi pi-arrow-left mr-2"></i> Vissza
              </button>
              <button @click="activateCallback('3')" :disabled="!isFormValid"
                      class="px-6 py-2 bg-primary text-white font-bold rounded-lg disabled:opacity-50 transition-colors">
                Tovább <i class="pi pi-arrow-right ml-2"></i>
              </button>
            </div>
          </StepPanel>

          <StepPanel value="3" v-slot="{ activateCallback }">
            <h3 class="text-xl font-bold mb-4">Válassz időpontot</h3>

            <div class="flex flex-col gap-4 max-w-sm mx-auto">
              <div class="flex flex-col gap-1">
                <label class="text-sm text-text-muted">Dátum és Idő (Egyszerűsített Picker)</label>
                <input v-model="selectedDateTime" type="datetime-local" class="p-3 rounded border border-text/20 bg-background focus:border-primary focus:outline-none text-lg" />
              </div>

              <div v-if="errorMessage" class="p-3 bg-red-100 text-red-700 rounded-lg text-sm border border-red-200">
                <i class="pi pi-exclamation-triangle mr-2"></i> {{ errorMessage }}
              </div>
            </div>

            <div class="mt-8 pt-4 border-t border-text/10 flex justify-between items-center">
              <button @click="activateCallback('2')" class="px-6 py-2 border border-text/20 text-text rounded-lg transition-colors hover:bg-text/5">
                <i class="pi pi-arrow-left mr-2"></i> Vissza
              </button>
              <button @click="submitBooking" :disabled="!selectedDateTime || isSubmitting"
                      class="px-8 py-3 bg-primary text-white font-bold rounded-xl text-lg shadow-md hover:scale-105 transition-transform disabled:opacity-50 disabled:hover:scale-100 flex items-center gap-2">
                <i v-if="isSubmitting" class="pi pi-spin pi-spinner"></i>
                Véglegesítés
              </button>
            </div>
          </StepPanel>
        </StepPanels>
      </Stepper>

    </div>
  </div>
</template>

<script setup>
import { ref, inject, onMounted, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import apiClient from '@/services/api';

import Stepper from 'primevue/stepper';
import StepList from 'primevue/steplist';
import StepPanels from 'primevue/steppanels';
import Step from 'primevue/step';
import StepPanel from 'primevue/steppanel';

// Stepper és i18n
const activeStep = ref('1');
const { locale } = useI18n();
const currentLang = computed(() => locale.value);
const company = inject('company', ref(null));

// Állapotok
const loading = ref(true);
const isSubmitting = ref(false);
const errorMessage = ref('');
const categories = ref([]);

// Foglalási adatok
const bookingData = ref({
  fullName: '',
  email: '',
  phone: '',
  attributes: {
    HairLength: 'Félhosszú' // Alapértelmezett JSONB attribútum
  },
  notes: '',
  serviceVariantIds: [],
  employeeId: 1 // Alapértelmezett dolgozó ID-ja (Később dinamikus lehet)
});
const selectedDateTime = ref('');

// Validáció a 2. lépéshez
const isFormValid = computed(() => {
  return bookingData.value.fullName.length > 2 &&
         bookingData.value.email.includes('@') &&
         bookingData.value.phone.length > 5;
});

// Pénznem formázó
const formatCurrency = (val) => {
  if (val === null || val === undefined) return '';
  return val.toLocaleString('hu-HU', { style: 'currency', currency: 'EUR', maximumFractionDigits: 0 });
};

// Szolgáltatások betöltése és formázása (Hasonló a ServicesView-hoz)
const fetchServices = async () => {
  try {
    const response = await apiClient.get('/api/Service');
    const flatServices = response.data.$values || response.data;

    // Csak az egyszerűség kedvéért egy alap csoportosítás:
    const groups = {};
    flatServices.forEach(s => {
      const catName = s.category[currentLang.value] || s.category['hu'] || 'Egyéb';
      if (!groups[catName]) {
        groups[catName] = { id: catName, categoryName: s.category, items: [] };
      }
      groups[catName].items.push(s);
    });
    categories.value = Object.values(groups);
  } catch (error) {
    console.error('Hiba a szolgáltatások betöltésekor', error);
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  fetchServices();
});

// Foglalás beküldése
const submitBooking = async () => {
  isSubmitting.value = true;
  errorMessage.value = '';

  try {
    const payload = {
      ...bookingData.value,
      startDateTime: new Date(selectedDateTime.value).toISOString()
    };

    // Publikus végpont hívása (amibe nem kell Token)
    const response = await apiClient.post('/api/PublicBooking', payload);

    alert('Sikeres foglalás! Hamarosan küldjük a visszaigazoló emailt.');
    // Itt el lehet navigálni egy "Sikeres Foglalás" oldalra (router.push('/success'))
    window.location.reload();

  } catch (error) {
    if (error.response && error.response.status === 400) {
      errorMessage.value = error.response.data; // Ütközés üzenet a SmartEngine-ből
    } else {
      errorMessage.value = 'Hiba történt a foglalás során. Kérlek próbáld újra!';
    }
  } finally {
    isSubmitting.value = false;
  }
};
</script>
