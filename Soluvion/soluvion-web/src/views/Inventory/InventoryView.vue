<template>
  <div class="p-6">
    <div class="flex justify-between items-center mb-6">
      <h1 class="text-3xl font-bold">Raktár & Termékek</h1>
      <div class="flex gap-2">
        <Button label="Új termék" icon="pi pi-plus" @click="openProductDialog" />
        <Button label="Inventúra / Mozgás" icon="pi pi-box" severity="secondary" @click="openStockDialog" />
      </div>
    </div>

    <!-- PrimeVue DataTable -->
    <DataTable :value="products" responsiveLayout="scroll" :loading="loading" class="p-datatable-sm shadow-sm rounded-lg">
      <Column header="Név" :sortable="true" field="name">
        <template #body="slotProps">
          <span class="cursor-pointer text-primary font-bold hover:underline" @click="editProduct(slotProps.data)" title="Termékkártya szerkesztése">
            {{ slotProps.data.name }}
          </span>
        </template>
      </Column>
      <Column field="ean" header="Vonalkód / EAN" :sortable="true"></Column>
      <Column header="Típus">
        <template #body="slotProps">
          <Badge v-if="slotProps.data.isProfessional" value="Professzionális" severity="info" class="mr-2" />
          <Badge v-if="slotProps.data.isRetail" value="Lakossági" severity="success" />
        </template>
      </Column>
      <Column field="packageSize" header="Kiszerelés">
        <template #body="slotProps">
          {{ slotProps.data.packageSize }} {{ getUnitName(slotProps.data.unit) }}
        </template>
      </Column>
      <Column field="currentStock" header="Készlet" :sortable="true">
        <template #body="slotProps">
          <div 
            class="cursor-pointer inline-flex items-center gap-2 px-3 py-1 rounded-md hover:bg-surface-200 transition-colors" 
            :class="{'text-red-500 font-bold': slotProps.data.currentStock <= slotProps.data.lowStockThreshold, 'font-bold': true}"
            @click="openQuickStock(slotProps.data)"
            title="Készlet gyors módosítása"
          >
            {{ slotProps.data.currentStock }} db
            <i class="pi pi-pencil text-xs opacity-50"></i>
          </div>
        </template>
      </Column>
      
      <template #empty>
        Még nincsenek termékek felvéve.
      </template>
    </DataTable>

    <!-- Dialogs -->
    <ProductDialog 
      v-model:visible="productDialogVisible" 
      :productData="selectedProduct" 
      @saved="fetchProducts" 
    />
    <StockAdjustmentDialog 
      v-model:visible="stockDialogVisible" 
      :products="products"
      @saved="fetchProducts" 
    />
    <QuickStockDialog
      v-model:visible="quickStockVisible"
      :product="selectedProductForStock"
      @saved="fetchProducts"
    />
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import Button from 'primevue/button';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Badge from 'primevue/badge';

import productApi from '@/services/productApi';
import ProductDialog from './components/ProductDialog.vue';
import StockAdjustmentDialog from './components/StockAdjustmentDialog.vue';
import QuickStockDialog from './components/QuickStockDialog.vue';

const products = ref([]);
const loading = ref(false);

const productDialogVisible = ref(false);
const stockDialogVisible = ref(false);
const quickStockVisible = ref(false);

const selectedProduct = ref(null);
const selectedProductForStock = ref(null);

const fetchProducts = async () => {
  loading.value = true;
  try {
    const res = await productApi.getAllProducts();
    products.value = res.data;
  } catch (error) {
    console.error('Hiba a termékek lekérésekor:', error);
  } finally {
    loading.value = false;
  }
};

const getUnitName = (unitEnum) => {
  const units = ['ml', 'g', 'db'];
  return units[unitEnum] || '';
};

const openProductDialog = () => {
  selectedProduct.value = null;
  productDialogVisible.value = true;
};

const editProduct = (product) => {
  selectedProduct.value = { ...product };
  productDialogVisible.value = true;
};

const openStockDialog = () => {
  stockDialogVisible.value = true;
};

const openQuickStock = (product) => {
  selectedProductForStock.value = { ...product };
  quickStockVisible.value = true;
};

onMounted(() => {
  fetchProducts();
});
</script>

