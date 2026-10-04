<template>
  <Dialog :visible="visible" @update:visible="$emit('update:visible', $event)" :style="{width: '600px'}" header="Alapértelmezett Anyagfelhasználás" :modal="true" class="p-fluid">
    <div v-if="variant" class="mb-4">
      <div class="text-sm text-text-muted mb-4">
        Állítsd be, hogy ehhez a szolgáltatás-variánshoz (<strong class="text-primary">{{ serviceName }} - {{ variantName }}</strong>) 
        milyen termékeket és mekkora mennyiséget készítsen be a rendszer alapértelmezetten a Napi Záráskor.
      </div>

      <div v-if="loading" class="flex justify-center p-4">
        <i class="pi pi-spin pi-spinner text-2xl text-primary"></i>
      </div>
      <div v-else>
        <!-- Hozzáadás sor -->
        <div class="flex gap-2 mb-4 items-end bg-surface p-3 rounded-lg border border-text/10">
          <div class="flex-grow flex flex-col gap-1">
            <label class="font-bold text-xs">Termék kiválasztása</label>
            <Dropdown v-model="selectedProduct" :options="allProducts" optionLabel="name" placeholder="Keress egy terméket..." class="w-full" filter />
          </div>
          <div class="w-32 flex flex-col gap-1">
            <label class="font-bold text-xs">Mennyiség {{ selectedProduct ? '(' + getUnit(selectedProduct.id) + ')' : '' }}</label>
            <InputNumber v-model="quantity" mode="decimal" :minFractionDigits="0" :maxFractionDigits="2" class="w-full" />
          </div>
          <Button icon="pi pi-plus" @click="addProduct" :disabled="!selectedProduct || quantity <= 0" class="w-12 h-10 shrink-0" />
        </div>

        <!-- Jelenlegi lista -->
        <div v-if="recipeProducts.length === 0" class="text-center p-4 text-text-muted italic bg-background rounded-lg border border-text/5">
          Nincs beállítva alapértelmezett anyagfelhasználás.
        </div>
        <div v-else class="flex flex-col gap-2">
          <div v-for="(rp, index) in recipeProducts" :key="index" class="flex items-center justify-between p-2 bg-background rounded border border-text/10">
            <div class="font-medium text-sm">{{ rp.productName }}</div>
            <div class="flex items-center gap-4">
              <span class="text-primary font-bold">{{ rp.defaultQuantity }} {{ getUnit(rp.productId) }}</span>
              <button @click="removeProduct(index)" class="text-red-500 hover:text-red-700 transition-colors w-6 h-6 flex justify-center items-center rounded-full hover:bg-red-500/10">
                <i class="pi pi-trash text-sm"></i>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <template #footer>
      <Button label="Mégse" icon="pi pi-times" text @click="hideDialog" />
      <Button label="Mentés" icon="pi pi-check" @click="saveRecipe" :loading="saving" />
    </template>
  </Dialog>
</template>

<script setup>
// @ts-nocheck
import { ref, watch, onMounted } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Dropdown from 'primevue/dropdown';
import InputNumber from 'primevue/inputnumber';
import productApi from '@/services/productApi';
import api from '@/services/api'; // direct api for recipe saving

const props = defineProps({
  visible: Boolean,
  variant: Object,
  serviceName: String,
  variantName: String
});

const emit = defineEmits(['update:visible', 'saved']);

const allProducts = ref([]);
const recipeProducts = ref([]);
const selectedProduct = ref(null);
const quantity = ref(1);
const loading = ref(false);
const saving = ref(false);

const loadProducts = async () => {
  try {
    const res = await productApi.getAllProducts();
    allProducts.value = res.data || [];
  } catch (error) {
    console.error("Hiba a termékek betöltésekor", error);
  }
};

const getUnit = (id) => {
  const p = allProducts.value.find(x => x.id === id);
  if (!p) return '';
  const units = ['ml', 'g', 'db', 'm', 'cm'];
  return units[p.unit] || '';
};

watch(() => props.visible, async (newVal) => {
  if (newVal) {
    selectedProduct.value = null;
    quantity.value = 1;
    if (allProducts.value.length === 0) {
      await loadProducts();
    }
    // Deep copy default products to edit locally
    recipeProducts.value = JSON.parse(JSON.stringify(props.variant?.defaultProducts || []));
  }
});

const addProduct = () => {
  if (!selectedProduct.value || quantity.value <= 0) return;
  
  // Check if already exists
  const existing = recipeProducts.value.find(p => p.productId === selectedProduct.value.id);
  if (existing) {
    existing.defaultQuantity += quantity.value;
  } else {
    recipeProducts.value.push({
      productId: selectedProduct.value.id,
      productName: selectedProduct.value.name,
      defaultQuantity: quantity.value
    });
  }
  
  selectedProduct.value = null;
  quantity.value = 1;
};

const removeProduct = (index) => {
  recipeProducts.value.splice(index, 1);
};

const hideDialog = () => {
  emit('update:visible', false);
};

const saveRecipe = async () => {
  if (!props.variant) return;
  saving.value = true;
  try {
    // API call to save recipe
    await api.put(`/api/service/variant/${props.variant.id}/recipe`, recipeProducts.value);
    emit('saved', recipeProducts.value);
    hideDialog();
  } catch (error) {
    console.error("Hiba a receptúra mentésekor", error);
  } finally {
    saving.value = false;
  }
};
</script>
