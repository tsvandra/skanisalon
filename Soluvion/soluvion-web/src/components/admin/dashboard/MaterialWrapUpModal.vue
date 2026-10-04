<template>
  <div v-if="isOpen" class="fixed inset-0 z-[100] flex items-center justify-center p-4">
    <!-- Backdrop -->
    <div class="absolute inset-0 bg-background/80 backdrop-blur-sm" @click="close"></div>
    
    <!-- Modal -->
    <div class="relative bg-surface w-full max-w-2xl rounded-2xl shadow-xl flex flex-col max-h-[90vh]">
      <!-- Header -->
      <div class="p-4 md:p-6 flex items-center justify-between border-b border-text/10">
        <div>
          <h2 class="text-xl md:text-2xl font-bold text-text flex items-center gap-2">
            <i class="pi pi-box text-primary"></i> Esti Zárás - Anyaglevonás
          </h2>
          <p class="text-sm text-text-muted mt-1">
            {{ formatDateTime(appointment?.startDateTime) }} &bull; {{ appointment?.items?.length || 0 }} szolgáltatás
          </p>
        </div>
        <button @click="close" class="w-8 h-8 flex items-center justify-center rounded-full hover:bg-text/10 text-text-muted transition-colors">
          <i class="pi pi-times"></i>
        </button>
      </div>

      <!-- Content -->
      <div class="p-4 md:p-6 overflow-y-auto custom-scrollbar flex-1 space-y-6">
        <div v-if="loading" class="flex justify-center py-8 text-primary">
          <i class="pi pi-spin pi-spinner text-3xl"></i>
        </div>
        
        <template v-else>
          <!-- Szolgáltatások -->
          <div>
            <h3 class="text-sm font-bold text-text-muted uppercase mb-3">Igénybe vett szolgáltatások</h3>
            <div class="flex flex-wrap gap-2">
              <span v-for="item in appointment?.items" :key="item.id" class="px-3 py-1 bg-background border border-text/10 rounded-full text-sm font-medium">
                {{ getVariantName(item.serviceVariantId) }}
              </span>
              <span v-if="!appointment?.items || appointment.items.length === 0" class="text-text-muted italic text-sm">
                Nem található szolgáltatás (hibás foglalás).
              </span>
            </div>
          </div>

          <!-- Anyagfelhasználás -->
          <div>
            <div class="flex items-center justify-between mb-3">
              <h3 class="text-sm font-bold text-text-muted uppercase">Felhasznált anyagok</h3>
              <button @click="showAddProduct = true" class="text-xs font-bold text-primary hover:brightness-110 flex items-center gap-1">
                <i class="pi pi-plus"></i> Új termék
              </button>
            </div>

            <div v-if="wrapUpItems.length === 0" class="text-center p-6 bg-background rounded-xl border border-text/5 text-text-muted italic">
              Nincs alapértelmezett anyagfelhasználás beállítva ehhez a foglaláshoz.
            </div>

            <div v-else class="space-y-2">
              <div v-for="(item, index) in wrapUpItems" :key="index" class="flex flex-col sm:flex-row sm:items-center gap-3 p-3 bg-background rounded-xl border border-text/10 shadow-sm">
                <div class="flex-1 font-bold text-sm">{{ item.productName }}</div>
                
                <div class="flex items-center gap-3">
                  <div class="flex items-center border border-text/20 rounded-lg overflow-hidden h-9 w-32 bg-surface">
                    <button @click="decreaseQty(item)" class="px-2 h-full hover:bg-text/5 text-text-muted"><i class="pi pi-minus text-xs"></i></button>
                    <input type="number" v-model.number="item.quantity" class="w-full text-center bg-transparent border-none focus:outline-none text-sm font-bold p-0" min="0" step="0.5">
                    <button @click="increaseQty(item)" class="px-2 h-full hover:bg-text/5 text-text-muted"><i class="pi pi-plus text-xs"></i></button>
                  </div>
                  <span class="text-xs font-bold text-text-muted w-8">{{ getUnit(item.productId) }}</span>
                  
                  <button @click="removeItem(index)" class="w-8 h-8 flex justify-center items-center rounded-full text-red-500 hover:bg-red-500/10 transition-colors">
                    <i class="pi pi-trash"></i>
                  </button>
                </div>
              </div>
            </div>
          </div>
          
          <!-- Új termék hozzáadása Dropdown -->
          <div v-if="showAddProduct" class="p-3 border border-primary/30 bg-primary/5 rounded-xl flex flex-col sm:flex-row gap-3">
            <Dropdown v-model="selectedNewProduct" :options="allProducts" optionLabel="name" placeholder="Válassz terméket..." filter class="flex-1" />
            <div class="flex items-center gap-2">
              <InputNumber v-model="newQuantity" :min="0" :maxFractionDigits="2" class="w-24 h-[44px]" placeholder="Menny." />
              <button @click="addNewProduct" class="h-[44px] px-4 bg-primary text-white font-bold rounded-lg hover:brightness-110 disabled:opacity-50" :disabled="!selectedNewProduct || !newQuantity">Hozzáad</button>
              <button @click="showAddProduct = false" class="h-[44px] px-3 bg-background border border-text/20 text-text rounded-lg hover:bg-text/5"><i class="pi pi-times"></i></button>
            </div>
          </div>

        </template>
      </div>

      <!-- Footer -->
      <div class="p-4 md:p-6 border-t border-text/10 bg-background/50 flex justify-end gap-3 rounded-b-2xl">
        <button @click="close" class="px-4 h-[44px] text-text font-bold rounded-lg hover:bg-text/10 transition-colors">Mégsem</button>
        <button @click="saveWrapUp" :disabled="saving || loading" class="px-6 h-[44px] bg-primary text-white font-bold rounded-lg hover:brightness-110 shadow-md transition-transform active:scale-95 disabled:opacity-50 flex items-center gap-2">
          <i v-if="saving" class="pi pi-spin pi-spinner"></i>
          <i v-else class="pi pi-check"></i> Mentés & Levonás
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
// @ts-nocheck
import { ref, watch } from 'vue';
import Dropdown from 'primevue/dropdown';
import InputNumber from 'primevue/inputnumber';
import inventoryApi from '@/services/inventoryApi';
import productApi from '@/services/productApi';
import apiClient from '@/services/api';
import { useI18n } from 'vue-i18n';

const props = defineProps({
  isOpen: Boolean,
  appointment: Object
});
const emit = defineEmits(['close', 'saved']);

const { locale } = useI18n();
const currentLang = ref(locale.value || 'hu-HU');

const loading = ref(false);
const saving = ref(false);
const services = ref([]);
const allProducts = ref([]);
const wrapUpItems = ref([]);

const showAddProduct = ref(false);
const selectedNewProduct = ref(null);
const newQuantity = ref(1);

const formatDateTime = (iso) => {
  if (!iso) return '';
  const d = new Date(iso);
  return d.toLocaleDateString('hu-HU', { month: 'short', day: 'numeric' }) + ' ' + d.toLocaleTimeString('hu-HU', { hour: '2-digit', minute: '2-digit' });
};

const getLocText = (dict) => dict ? (dict[currentLang.value] || dict['hu'] || '') : '';

const getVariantName = (variantId) => {
  for (const s of services.value) {
    const v = s.variants?.find(vx => vx.id === variantId);
    if (v) return `${getLocText(s.name)} - ${getLocText(v.variantName)}`;
  }
  return `Tétel #${variantId}`;
};

const getUnit = (productId) => {
  const p = allProducts.value.find(x => x.id === productId);
  return p ? p.unit : 'db';
};

const decreaseQty = (item) => { if (item.quantity > 0) item.quantity = Math.max(0, item.quantity - 0.5); };
const increaseQty = (item) => { item.quantity += 0.5; };
const removeItem = (index) => { wrapUpItems.value.splice(index, 1); };

const addNewProduct = () => {
  if (!selectedNewProduct.value || newQuantity.value <= 0) return;
  
  const existing = wrapUpItems.value.find(x => x.productId === selectedNewProduct.value.id);
  if (existing) {
    existing.quantity += newQuantity.value;
  } else {
    wrapUpItems.value.push({
      productId: selectedNewProduct.value.id,
      productName: selectedNewProduct.value.name,
      quantity: newQuantity.value,
      costPrice: selectedNewProduct.value.price || 0
    });
  }
  
  selectedNewProduct.value = null;
  newQuantity.value = 1;
  showAddProduct.value = false;
};

const loadData = async () => {
  if (!props.appointment || !props.isOpen) return;
  loading.value = true;
  wrapUpItems.value = [];
  try {
    const [svcRes, prodRes] = await Promise.all([
      apiClient.get('/api/Service'),
      productApi.getAllProducts()
    ]);
    services.value = svcRes.data || [];
    allProducts.value = prodRes.data || [];

    const productMap = new Map();
    
    props.appointment.items?.forEach(appItem => {
      for (const s of services.value) {
        const variant = s.variants?.find(v => v.id === appItem.serviceVariantId);
        if (variant && variant.defaultProducts) {
          variant.defaultProducts.forEach(dp => {
            const existing = productMap.get(dp.productId);
            if (existing) {
              existing.quantity += dp.defaultQuantity;
            } else {
              productMap.set(dp.productId, {
                productId: dp.productId,
                productName: dp.productName,
                quantity: dp.defaultQuantity,
                costPrice: 0 
              });
            }
          });
        }
      }
    });

    wrapUpItems.value = Array.from(productMap.values());
  } catch (error) {
    console.error("Hiba az adatok betöltésekor", error);
  } finally {
    loading.value = false;
  }
};

const close = () => {
  emit('close');
  showAddProduct.value = false;
};

const saveWrapUp = async () => {
  saving.value = true;
  try {
    const validItems = wrapUpItems.value.filter(i => i.quantity > 0).map(i => ({
      productId: i.productId,
      quantity: i.quantity,
      costPrice: i.costPrice || 0
    }));

    if (validItems.length > 0) {
      const payload = {
        type: 'Outbound',
        note: `Napi zárás (Foglalás ID: ${props.appointment.id})`,
        appointmentId: props.appointment.id,
        items: validItems
      };
      await inventoryApi.createDocument(payload);
    } else {
      const app = (await apiClient.get(`/api/Appointment/${props.appointment.id}`)).data;
      await apiClient.post(`/api/Appointment/${props.appointment.id}/mark-materials-recorded`);
    }
    
    emit('saved');
    close();
  } catch (error) {
    console.error("Hiba a levonás során", error);
    alert("Hiba történt a mentés során. Lehet, hogy nincs elég készlet a raktárban?");
  } finally {
    saving.value = false;
  }
};

watch(() => props.isOpen, (newVal) => {
  if (newVal) loadData();
});
</script>

