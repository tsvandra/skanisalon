<template>
  <div v-if="isOpen" class="fixed inset-0 z-[100] flex items-center justify-center p-4">
    <div class="absolute inset-0 bg-background/80 backdrop-blur-sm" @click="close"></div>
    <div class="relative bg-surface w-full max-w-2xl rounded-2xl shadow-xl flex flex-col max-h-[90vh]">
      <!-- Header -->
      <div class="p-4 md:p-6 border-b border-text/10 flex justify-between items-center bg-surface-50">
        <div>
          <h2 class="text-xl font-bold text-orange-500 flex items-center gap-2">
            <i class="pi pi-chart-line text-orange-500"></i> {{ $t('inventory.lowStockPlanner.title') }} {{ product?.name }}
          </h2>
          <p class="text-xs text-text-muted mt-1">
            {{ $t('inventory.lowStockPlanner.currentStock') }} <strong class="text-orange-500">{{ product?.currentStock }} {{ $t('inventory.units.short.pcs') }}</strong> ({{ $t('inventory.lowStockPlanner.minimum') }} {{ product?.lowStockThreshold }} {{ $t('inventory.units.short.pcs') }})
          </p>
        </div>
        <button @click="close" class="w-8 h-8 rounded-full hover:bg-text/10 flex items-center justify-center text-text-muted">
          <i class="pi pi-times"></i>
        </button>
      </div>

      <!-- Content -->
      <div class="p-4 md:p-6 overflow-y-auto flex-1 space-y-6">
        <div class="bg-orange-500/10 border border-orange-500/20 rounded-xl p-4 flex items-center gap-4">
          <div class="w-12 h-12 bg-orange-500/20 rounded-full flex items-center justify-center shrink-0">
            <i class="pi pi-calendar text-2xl text-orange-500"></i>
          </div>
          <div>
            <div class="text-xs font-bold text-text-muted uppercase">{{ $t('inventory.lowStockPlanner.upcomingUsage') }}</div>
            <div class="text-xl font-black text-orange-500">{{ product?.upcoming30DaysUsage || 0 }} {{ $t('inventory.units.short.pcs') }} ({{ product?.rawUpcomingUsage || 0 }} {{ getUnit(product) }})</div>
          </div>
        </div>

        <div>
          <h3 class="text-sm font-bold text-text-muted uppercase mb-3"><i class="pi pi-list"></i> {{ $t('inventory.lowStockPlanner.relatedBookings') }}</h3>
          
          <div v-if="loading" class="flex justify-center p-6 text-primary"><i class="pi pi-spin pi-spinner text-2xl"></i></div>
          <div v-else-if="appointments.length === 0" class="text-center p-6 bg-background rounded-xl border border-text/5 text-text-muted italic text-sm">
            {{ $t('inventory.lowStockPlanner.noBookings') }}
          </div>
          <div v-else class="space-y-2">
            <div v-for="app in appointments" :key="app.id" @click="goToCalendar(app.startDateTime)" class="p-3 bg-background border border-text/10 rounded-xl flex flex-col sm:flex-row justify-between items-start sm:items-center gap-2 hover:border-orange-500/50 cursor-pointer transition-colors">
              <div>
                <div class="text-xs font-bold text-primary bg-primary/10 px-2 py-0.5 rounded-md inline-block mb-1">
                  {{ formatDate(app.startDateTime) }}
                </div>
                <div class="text-sm font-bold text-text"><i class="pi pi-user text-[10px] text-text-muted mr-1"></i> {{ app.customerName || $t('inventory.lowStockPlanner.unknownGuest') }}</div>
              </div>
              <div class="text-right">
                <div class="text-xs font-bold text-text-muted">{{ $t('inventory.lowStockPlanner.requiredQuantity') }}</div>
                <div class="text-sm font-black text-orange-500">{{ app.productUsage }} {{ getUnit(product) }}</div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Footer -->
      <div class="p-4 border-t border-text/10 bg-background/50 flex justify-end gap-3 rounded-b-2xl">
        <button @click="close" class="px-5 py-2 text-text font-bold hover:bg-text/10 rounded-lg">{{ $t('inventory.lowStockPlanner.close') }}</button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useLocalizedRoute } from '@/composables/useLocalizedRoute';
import appointmentApi from '@/services/appointmentApi';
import apiClient from '@/services/api';

const { t, locale } = useI18n();

const props = defineProps({
  isOpen: Boolean,
  product: Object,
  services: Array
});
const emit = defineEmits(['close']);
const { go } = useLocalizedRoute();

const goToCalendar = (dateStr) => {
  emit('close');
  go('orders', { query: { date: dateStr, view: 'month' } });
};

const appointments = ref([]);
const loading = ref(false);

const getUnit = (p) => {
  const unitMap = {
    0: t('inventory.units.short.ml'),
    1: t('inventory.units.short.g'),
    2: t('inventory.units.short.pcs'),
    3: t('inventory.units.short.m'),
    4: t('inventory.units.short.cm')
  };
  return p ? (unitMap[p.unit] || t('inventory.units.short.pcs')) : t('inventory.units.short.pcs');
};

const formatDate = (dateStr) => {
  return new Date(dateStr).toLocaleString(locale.value, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
};

const loadAppointments = async () => {
  if (!props.product) return;
  loading.value = true;
  appointments.value = [];
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
    
    const filteredApps = [];
    
    apps.forEach(app => {
      let usage = 0;
      
      // Check service defaults
      app.items?.forEach(item => {
        const svc = props.services.find(s => s.variants && s.variants.some(v => v.id === item.serviceVariantId));
        if (svc) {
          const variant = svc.variants.find(v => v.id === item.serviceVariantId);
          if (variant && variant.defaultProducts) {
            const dp = variant.defaultProducts.find(d => d.productId === props.product.id);
            if (dp) usage += dp.defaultQuantity;
          }
        }
      });
      
      // Check formula
      const customer = customers.find(c => c.id === app.customerId);
      if (customer && customer.attributes && customer.attributes.FormulaList) {
        try {
          const formula = JSON.parse(customer.attributes.FormulaList);
          const fItem = formula.find(fi => fi.productId === props.product.id);
          if (fItem) usage += fItem.quantity;
        } catch(e) {}
      }
      
      if (usage > 0) {
        filteredApps.push({
          ...app,
          customerName: customer ? (customer.name || customer.attributes?.FullName) : t('inventory.lowStockPlanner.guest'),
          productUsage: usage
        });
      }
    });
    
    // Sort by date
    filteredApps.sort((a,b) => new Date(a.startDateTime) - new Date(b.startDateTime));
    appointments.value = filteredApps;
    
  } catch(e) {
    console.error("Hiba a lekérdezéskor", e);
  } finally {
    loading.value = false;
  }
};

watch(() => props.isOpen, (newVal) => {
  if (newVal) loadAppointments();
});

const close = () => {
  emit('close');
};
</script>
