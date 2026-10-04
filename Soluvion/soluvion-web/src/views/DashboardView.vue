<template>
  <div class="max-w-7xl mx-auto px-4 py-8">
    <div class="mb-8">
      <h1 class="text-3xl font-bold text-text">Vezérlőpult</h1>
      <p class="text-text-muted mt-2">Válaszd ki, melyik belső rendszert szeretnéd kezelni.</p>
    </div>

    <!-- Esti Napi Zárás Widget -->
    <div v-if="pendingMaterialLogs.length > 0" class="mb-8 bg-red-500/5 border border-red-500/20 rounded-2xl p-6 shadow-sm">
      <div class="flex items-center gap-3 mb-4">
        <i class="pi pi-bell text-2xl text-red-500"></i>
        <h2 class="text-xl font-bold text-red-500">Napi Zárás - Elmaradt adminisztráció</h2>
        <span class="bg-red-500 text-white text-xs font-bold px-2 py-1 rounded-full">{{ pendingMaterialLogs.length }}</span>
      </div>
      <p class="text-sm text-text-muted mb-4">
        Az alábbi vendégek már befejezettként (fizetve) lettek megjelölve a naptárban, de a felhasznált anyagok még nem lettek levonva a raktárból. Kattints rájuk a levonáshoz!
      </p>
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        <div v-for="app in pendingMaterialLogs" :key="app.id" @click="openWrapUp(app)" 
             class="bg-background border border-red-500/30 p-4 rounded-xl cursor-pointer hover:bg-red-500/10 hover:border-red-500/50 transition-all flex items-center justify-between group">
          <div>
            <div class="font-bold text-text group-hover:text-primary transition-colors flex items-center gap-2">
              <i class="pi pi-user text-text-muted text-sm"></i> {{ app.customerName || 'Ismeretlen Vendég' }}
            </div>
            <div class="text-xs text-text-muted mt-1 font-medium">
              {{ formatDateShort(app.startDateTime) }} {{ formatTime(app.startDateTime) }} &bull; {{ getServiceName(app) }}
            </div>
          </div>
          <i class="pi pi-box text-text-muted group-hover:text-red-500 transition-colors ml-2"></i>
        </div>
      </div>
    </div>

    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      <!-- Megrendelések / Naptár -->
      <router-link to="/megrendelesek" class="block group no-underline">
        <div class="bg-surface border border-text/10 rounded-2xl p-6 shadow-sm hover:shadow-md transition-all duration-300 hover:-translate-y-1 h-full flex flex-col items-center text-center">
          <div class="w-16 h-16 rounded-full bg-primary/10 flex items-center justify-center text-primary mb-4 group-hover:scale-110 transition-transform">
            <i class="pi pi-calendar text-3xl"></i>
          </div>
          <h2 class="text-xl font-bold text-text mb-2 group-hover:text-primary transition-colors">Megrendelések / Naptár</h2>
          <p class="text-sm text-text-muted">Időpontok, foglalások kezelése, napi beosztás megtekintése.</p>
        </div>
      </router-link>

      <!-- Raktár -->
      <router-link to="/raktar" class="block group no-underline">
        <div class="bg-surface border border-text/10 rounded-2xl p-6 shadow-sm hover:shadow-md transition-all duration-300 hover:-translate-y-1 h-full flex flex-col items-center text-center">
          <div class="w-16 h-16 rounded-full bg-primary/10 flex items-center justify-center text-primary mb-4 group-hover:scale-110 transition-transform">
            <i class="pi pi-box text-3xl"></i>
          </div>
          <h2 class="text-xl font-bold text-text mb-2 group-hover:text-primary transition-colors">Raktár & Termékek</h2>
          <p class="text-sm text-text-muted">Készletkezelés, bevételezések, kiadások és leltározás.</p>
        </div>
      </router-link>

      <!-- Ügyfelek -->
      <router-link to="/ugyfelek" class="block group no-underline">
        <div class="bg-surface border border-text/10 rounded-2xl p-6 shadow-sm hover:shadow-md transition-all duration-300 hover:-translate-y-1 h-full flex flex-col items-center text-center">
          <div class="w-16 h-16 rounded-full bg-primary/10 flex items-center justify-center text-primary mb-4 group-hover:scale-110 transition-transform">
            <i class="pi pi-users text-3xl"></i>
          </div>
          <h2 class="text-xl font-bold text-text mb-2 group-hover:text-primary transition-colors">Ügyfelek (CRM)</h2>
          <p class="text-sm text-text-muted">Vendég adatbázis, vendégprofilok és szolgáltatási előzmények.</p>
        </div>
      </router-link>
    </div>

    <!-- Napi Zárás Modal -->
    <MaterialWrapUpModal 
      :is-open="isWrapUpModalOpen" 
      :appointment="selectedAppointment" 
      @close="isWrapUpModalOpen = false" 
      @saved="handleWrapUpSaved" 
    />
  </div>
</template>

<script setup>
// @ts-nocheck
import { ref, onMounted } from 'vue';
import appointmentApi from '@/services/appointmentApi';
import apiClient from '@/services/api';
import MaterialWrapUpModal from '@/components/admin/dashboard/MaterialWrapUpModal.vue';
import { useI18n } from 'vue-i18n';

const { locale } = useI18n();
const currentLang = ref(locale.value || 'hu-HU');

const pendingMaterialLogs = ref([]);
const services = ref([]);
const isWrapUpModalOpen = ref(false);
const selectedAppointment = ref(null);

const getLocText = (dict) => dict ? (dict[currentLang.value] || dict['hu'] || '') : '';

const fetchServices = async () => {
  try {
    const response = await apiClient.get('/api/Service');
    services.value = response.data || [];
  } catch (error) {
    console.error("Hiba a szolgáltatások betöltésekor", error);
  }
};

const getServiceName = (app) => {
  if (!app.items || app.items.length === 0) return 'Ismeretlen szolgáltatás';
  
  const firstItem = app.items[0];
  let primaryName = 'Szolgáltatás';
  
  for (const s of services.value) {
    const variant = s.variants?.find(v => v.id === firstItem.serviceVariantId);
    if (variant) {
      const sName = getLocText(s.name);
      const vName = getLocText(variant.variantName);
      primaryName = `${sName} - ${vName}`;
      break;
    }
  }

  if (app.items.length > 1) {
    return `${primaryName} (+${app.items.length - 1} tétel)`;
  }
  return primaryName;
};

const fetchPendingLogs = async () => {
  try {
    const end = new Date();
    const start = new Date();
    start.setDate(start.getDate() - 7); // Last 7 days

    const response = await appointmentApi.getAppointments(start, end);
    const all = response.data || [];
    
    pendingMaterialLogs.value = all.filter(a => (a.status === 'Completed' || a.status === '2' || a.status === 2) && !a.materialUsageRecorded);
  } catch (error) {
    console.error("Hiba az elmaradt zárások betöltésekor", error);
  }
};

const formatDateShort = (iso) => iso ? new Date(iso).toLocaleDateString('hu-HU', { month: 'short', day: 'numeric' }) : '';
const formatTime = (iso) => iso ? new Date(iso).toLocaleTimeString('hu-HU', { hour: '2-digit', minute: '2-digit' }) : '';

const openWrapUp = (app) => {
  selectedAppointment.value = app;
  isWrapUpModalOpen.value = true;
};

const handleWrapUpSaved = () => {
  isWrapUpModalOpen.value = false;
  fetchPendingLogs(); 
};

onMounted(() => {
  fetchServices();
  fetchPendingLogs();
});
</script>

