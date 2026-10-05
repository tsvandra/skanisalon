<template>
  <div class="max-w-7xl mx-auto px-4 py-8">
    <div class="mb-8">
      <h1 class="text-3xl font-bold text-text">{{ $t('dashboard.title') }}</h1>
      <p class="text-text-muted mt-2">{{ $t('dashboard.subtitle') }}</p>
    </div>

    <!-- Készletkezelés kikapcsolva infó (csak adminnak) -->
    <div v-if="isAdmin && stockTrackingLoaded && !isStockTrackingEnabled" class="mb-8 bg-text/5 border border-text/10 rounded-2xl p-4 flex items-center gap-3">
      <i class="pi pi-info-circle text-xl text-text-muted"></i>
      <p class="m-0 text-sm text-text-muted">
        <i18n-t keypath="dashboard.stockDisabledInfo" scope="global">
          <template #link>
            <router-link :to="localized('settings')" class="text-primary font-bold">{{ $t('dashboard.stockDisabledLink') }}</router-link>
          </template>
        </i18n-t>
      </p>
    </div>

    <!-- Esti Napi Zárás Widget -->
    <div v-if="pendingMaterialLogs.length > 0" class="mb-8 bg-red-500/5 border border-red-500/20 rounded-2xl p-6 shadow-sm">
      <div class="flex items-center gap-3 mb-4">
        <i class="pi pi-bell text-2xl text-red-500"></i>
        <h2 class="text-xl font-bold text-red-500">{{ $t('dashboard.dailyClose.title') }}</h2>
        <span class="bg-red-500 text-white text-xs font-bold px-2 py-1 rounded-full">{{ pendingMaterialLogs.length }}</span>
      </div>
      <p class="text-sm text-text-muted mb-4">
        {{ $t('dashboard.dailyClose.description') }}
      </p>
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        <div v-for="app in pendingMaterialLogs" :key="app.id" @click="openWrapUp(app)" 
             class="bg-background border border-red-500/30 p-4 rounded-xl cursor-pointer hover:bg-red-500/10 hover:border-red-500/50 transition-all flex items-center justify-between group">
          <div>
            <div class="font-bold text-text group-hover:text-primary transition-colors flex items-center gap-2">
              <i class="pi pi-user text-text-muted text-sm"></i> {{ app.customerName || $t('dashboard.dailyClose.unknownGuest') }}
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
      <router-link :to="localized('orders')" class="block group no-underline">
        <div class="bg-surface border border-text/10 rounded-2xl p-6 shadow-sm hover:shadow-md transition-all duration-300 hover:-translate-y-1 h-full flex flex-col items-center text-center">
          <div class="w-16 h-16 rounded-full bg-primary/10 flex items-center justify-center text-primary mb-4 group-hover:scale-110 transition-transform">
            <i class="pi pi-calendar text-3xl"></i>
          </div>
          <h2 class="text-xl font-bold text-text mb-2 group-hover:text-primary transition-colors">{{ $t('dashboard.cards.orders.title') }}</h2>
          <p class="text-sm text-text-muted">{{ $t('dashboard.cards.orders.description') }}</p>
        </div>
      </router-link>

      <!-- Raktár -->
      <router-link v-if="isAdmin" :to="localized('inventory')" class="block group no-underline">
        <div class="bg-surface border border-text/10 rounded-2xl p-6 shadow-sm hover:shadow-md transition-all duration-300 hover:-translate-y-1 h-full flex flex-col items-center text-center">
          <div class="w-16 h-16 rounded-full bg-primary/10 flex items-center justify-center text-primary mb-4 group-hover:scale-110 transition-transform">
            <i class="pi pi-box text-3xl"></i>
          </div>
          <h2 class="text-xl font-bold text-text mb-2 group-hover:text-primary transition-colors">{{ $t('dashboard.cards.inventory.title') }}</h2>
          <p class="text-sm text-text-muted">{{ $t('dashboard.cards.inventory.description') }}</p>
        </div>
      </router-link>

      <!-- Ügyfelek -->
      <router-link :to="localized('customers')" class="block group no-underline">
        <div class="bg-surface border border-text/10 rounded-2xl p-6 shadow-sm hover:shadow-md transition-all duration-300 hover:-translate-y-1 h-full flex flex-col items-center text-center">
          <div class="w-16 h-16 rounded-full bg-primary/10 flex items-center justify-center text-primary mb-4 group-hover:scale-110 transition-transform">
            <i class="pi pi-users text-3xl"></i>
          </div>
          <h2 class="text-xl font-bold text-text mb-2 group-hover:text-primary transition-colors">{{ $t('dashboard.cards.customers.title') }}</h2>
          <p class="text-sm text-text-muted">{{ $t('dashboard.cards.customers.description') }}</p>
        </div>
      </router-link>
    </div>

    <!-- Kifogyóban lévő termékek Widget -->
    <div v-if="isAdmin && lowStockProducts.length > 0" class="mt-8 bg-orange-500/5 border border-orange-500/20 rounded-2xl p-6 shadow-sm">
      <div class="flex items-center gap-3 mb-4">
        <i class="pi pi-exclamation-triangle text-2xl text-orange-500"></i>
        <h2 class="text-xl font-bold text-orange-500">{{ $t('dashboard.lowStock.title') }}</h2>
        <span class="bg-orange-500 text-white text-xs font-bold px-2 py-1 rounded-full">{{ lowStockProducts.length }}</span>
      </div>
      <p class="text-sm text-text-muted mb-4">
        {{ $t('dashboard.lowStock.description') }}
      </p>
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        <div v-for="product in lowStockProducts" :key="product.id" @click="openPlanner(product)"
             class="bg-background border border-orange-500/30 p-4 rounded-xl cursor-pointer hover:bg-orange-500/10 hover:border-orange-500/50 transition-all flex items-center justify-between group">
          <div>
            <div class="font-bold text-text group-hover:text-primary transition-colors flex items-center gap-2">
              <i class="pi pi-box text-text-muted text-sm"></i> {{ product.name }}
            </div>
            <div class="text-xs text-text-muted mt-1 font-medium">
              <i18n-t keypath="dashboard.lowStock.stockLine" scope="global">
                <template #stock><span class="text-orange-500 font-bold">{{ $t('dashboard.lowStock.pieces', { n: product.currentStock }) }}</span></template>
                <template #min>{{ product.lowStockThreshold }}</template>
              </i18n-t>
            </div>
            <div v-if="product.upcoming30DaysUsage !== undefined" class="text-xs font-bold text-orange-500 mt-2 flex items-center gap-1">
              <i class="pi pi-calendar"></i> {{ $t('dashboard.lowStock.forecast', { n: product.upcoming30DaysUsage }) }}
            </div>
          </div>
          <i class="pi pi-angle-right text-text-muted group-hover:text-orange-500 transition-colors ml-2"></i>
        </div>
      </div>
    </div>

    <!-- Készlet Tervező Modal -->
    <LowStockPlannerModal :is-open="isPlannerOpen" :product="selectedLowStockProduct" :services="services" @close="isPlannerOpen = false" />

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
import { ref, onMounted, inject, computed } from 'vue';
import appointmentApi from '@/services/appointmentApi';
import apiClient from '@/services/api';
import MaterialWrapUpModal from '@/components/admin/dashboard/MaterialWrapUpModal.vue';
import LowStockPlannerModal from '@/components/admin/dashboard/LowStockPlannerModal.vue';
import { useI18n } from 'vue-i18n';
import { useLocalizedText } from '@/composables/useLocalizedText';
import { getCompanyIdFromToken } from '@/utils/jwt';
import { useLocalizedRoute } from '@/composables/useLocalizedRoute';

const { locale, t } = useI18n();
const { to: localized } = useLocalizedRoute();
const currentLang = ref(locale.value || 'hu-HU');

const userRole = inject('userRole');
const isAdmin = computed(() => ['Admin', 'Owner'].includes(userRole?.value));

const stockTrackingStart = ref(null);
const stockTrackingLoaded = ref(false);
const isStockTrackingEnabled = computed(() => !!stockTrackingStart.value);

const pendingMaterialLogs = ref([]);
const lowStockProducts = ref([]);
const services = ref([]);
const isWrapUpModalOpen = ref(false);
const isPlannerOpen = ref(false);
const selectedLowStockProduct = ref(null);
const openPlanner = (p) => { selectedLowStockProduct.value = p; isPlannerOpen.value = true; };
const selectedAppointment = ref(null);

const { getLocText } = useLocalizedText();

const servicesLoaded = ref(false);

const fetchServices = async () => {
  try {
    const response = await apiClient.get('/api/Service');
    services.value = response.data || [];
    servicesLoaded.value = true;
  } catch (error) {
    console.error("Hiba a szolgáltatások betöltésekor", error);
  }
};

const getServiceName = (app) => {
  if (!app.items || app.items.length === 0) return t('dashboard.serviceName.unknown');
  
  const firstItem = app.items[0];
  let primaryName = t('dashboard.serviceName.default');
  
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
    return t('dashboard.serviceName.moreItems', { name: primaryName, extra: app.items.length - 1 });
  }
  return primaryName;
};

const fetchStockTracking = async () => {
  try {
    const companyId = getCompanyIdFromToken();
    if (!companyId) return;
    const res = await apiClient.get(`/api/Company/${companyId}`);
    stockTrackingStart.value = res.data?.stockTrackingStartDate ? new Date(res.data.stockTrackingStartDate) : null;
  } catch (error) {
    console.error("Hiba a készletkezelés beállításainak betöltésekor", error);
    stockTrackingStart.value = null;
  } finally {
    stockTrackingLoaded.value = true;
  }
};

// Eldönti, hogy egy foglaláshoz tartozik-e bármilyen levonandó anyag.
// Bizonytalan esetben (pl. ismeretlen variáns, hibás JSON) true-t ad, hogy a foglalás ne tűnjön el a listáról.
const appointmentHasMaterials = (app, customersById) => {
  for (const item of app.items || []) {
    let variantFound = false;
    for (const s of services.value) {
      const variant = s.variants?.find(v => v.id === item.serviceVariantId);
      if (variant) {
        variantFound = true;
        if (variant.defaultProducts?.some(dp => dp.defaultQuantity > 0)) return true;
        break;
      }
    }
    if (!variantFound) return true;
  }

  if (app.extraMaterials) {
    try {
      const extra = typeof app.extraMaterials === 'string' ? JSON.parse(app.extraMaterials) : app.extraMaterials;
      if (Array.isArray(extra) && extra.some(e => e.quantity > 0)) return true;
    } catch (e) { return true; }
  }

  const formulaRaw = customersById.get(app.customerId)?.attributes?.FormulaList;
  if (formulaRaw) {
    try {
      const formula = JSON.parse(formulaRaw);
      if (Array.isArray(formula) && formula.some(f => f.quantity > 0)) return true;
    } catch (e) { return true; }
  }

  return false;
};

// Az anyag nélküli foglalásokat automatikusan lezárja (mint az üres "Mentés & Levonás"), és kiszűri a listából.
const skipAppointmentsWithoutMaterials = async (candidates) => {
  if (candidates.length === 0 || !servicesLoaded.value) return candidates;

  let customersById;
  try {
    const custRes = await apiClient.get('/api/customers');
    const customers = custRes.data?.$values || custRes.data || [];
    customersById = new Map(customers.map(c => [c.id, c]));
  } catch (error) {
    console.error("Hiba az ügyfelek betöltésekor", error);
    return candidates;
  }

  const withMaterials = [];
  const withoutMaterials = [];
  candidates.forEach(a => (appointmentHasMaterials(a, customersById) ? withMaterials : withoutMaterials).push(a));

  const results = await Promise.allSettled(
    withoutMaterials.map(a => apiClient.post(`/api/Appointment/${a.id}/mark-materials-recorded`))
  );

  // Amit nem sikerült lezárni, az maradjon a listán
  results.forEach((r, i) => { if (r.status === 'rejected') withMaterials.push(withoutMaterials[i]); });

  return withMaterials.sort((x, y) => new Date(x.startDateTime) - new Date(y.startDateTime));
};

const fetchPendingLogs = async () => {
  if (!isStockTrackingEnabled.value) {
    pendingMaterialLogs.value = [];
    return;
  }
  try {
    const end = new Date();
    const start = new Date();
    start.setDate(start.getDate() - 7); // Last 7 days

    const response = await appointmentApi.getAppointments(start, end);
    const all = response.data || [];
    
    const candidates = all.filter(a =>
      (a.status === 'Completed' || a.status === '2' || a.status === 2) &&
      !a.materialUsageRecorded &&
      new Date(a.startDateTime) >= stockTrackingStart.value
    );

    pendingMaterialLogs.value = await skipAppointmentsWithoutMaterials(candidates);
  } catch (error) {
    console.error("Hiba az elmaradt zárások betöltésekor", error);
  }
};

const fetchLowStockProducts = async () => {
  if (!isStockTrackingEnabled.value) {
    lowStockProducts.value = [];
    return;
  }
  try {
    const response = await apiClient.get('/api/products');
    const allProducts = response.data || [];
    lowStockProducts.value = allProducts.filter(p => !p.isDeleted && p.currentStock <= p.lowStockThreshold);

    if (lowStockProducts.value.length > 0) {
      try {
        const now = new Date();
        const next30 = new Date();
        next30.setDate(now.getDate() + 30);
        
        const [appsRes, custsRes] = await Promise.all([
          appointmentApi.getAppointments(now, next30),
          apiClient.get('/api/customers')
        ]);
        
        const apps = appsRes.data?.$values || appsRes.data || [];
        const customers = custsRes.data?.$values || custsRes.data || [];
        
        const usageMap = {};
        lowStockProducts.value.forEach(p => usageMap[p.id] = 0);
        
        apps.forEach(app => {
          app.items?.forEach(item => {
            const svc = services.value.find(s => s.variants && s.variants.some(v => v.id === item.serviceVariantId));
            if (svc) {
              const variant = svc.variants.find(v => v.id === item.serviceVariantId);
              if (variant && variant.defaultProducts) {
                variant.defaultProducts.forEach(dp => {
                  if (usageMap[dp.productId] !== undefined) {
                    usageMap[dp.productId] += dp.defaultQuantity;
                  }
                });
              }
            }
          });
          
          const customer = customers.find(c => c.id === app.customerId);
          if (customer && customer.attributes && customer.attributes.FormulaList) {
            try {
              const formula = JSON.parse(customer.attributes.FormulaList);
              formula.forEach(fi => {
                if (usageMap[fi.productId] !== undefined) {
                  usageMap[fi.productId] += fi.quantity;
                }
              });
            } catch(e) {}
          }
        });
        
        lowStockProducts.value.forEach(p => {
          const rawQuantity = usageMap[p.id] || 0;
          const pkgSize = (p.packageSize && p.packageSize > 0) ? p.packageSize : 1;
          const piecesNeeded = rawQuantity > 0 ? (rawQuantity / pkgSize) : 0;
          p.upcoming30DaysUsage = Math.ceil(piecesNeeded * 10) / 10;
          p.rawUpcomingUsage = rawQuantity;
        });
      } catch(e) {
         console.error("Hiba a fogyás számolásakor", e);
      }
    }
  } catch (error) {
    console.error(error);
  }
};

const formatDateShort = (iso) => iso ? new Date(iso).toLocaleDateString(locale.value, { month: 'short', day: 'numeric' }) : '';
const formatTime = (iso) => iso ? new Date(iso).toLocaleTimeString(locale.value, { hour: '2-digit', minute: '2-digit' }) : '';

const openWrapUp = (app) => {
  selectedAppointment.value = app;
  isWrapUpModalOpen.value = true;
};

const handleWrapUpSaved = () => {
  isWrapUpModalOpen.value = false;
  fetchPendingLogs(); 
  fetchLowStockProducts();
};

onMounted(async () => {
  await Promise.all([fetchServices(), fetchStockTracking()]);
  fetchPendingLogs();
  fetchLowStockProducts();
});
</script>

