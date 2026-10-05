<template>
  <div class="p-6">
    <div class="flex justify-between items-center mb-6">
      <h1 class="text-3xl font-bold">{{ $t('inventory.view.title') }}</h1>
      <div class="flex gap-2">
        <Button :label="$t('inventory.view.newProduct')" icon="pi pi-plus" @click="openProductDialog" />
        <Button :label="$t('inventory.view.stockMovement')" icon="pi pi-box" severity="secondary" @click="openStockDialog" />
      </div>
    </div>

    <!-- PrimeVue DataTable -->
    <DataTable :value="products" responsiveLayout="scroll" :loading="loading" class="p-datatable-sm shadow-sm rounded-lg">
      <Column :header="$t('inventory.view.columns.name')" :sortable="true" field="name">
        <template #body="slotProps">
          <span class="cursor-pointer text-primary font-bold hover:underline" @click="editProduct(slotProps.data)" :title="$t('inventory.view.editProductTitle')">
            {{ slotProps.data.name }}
          </span>
        </template>
      </Column>
      <Column field="ean" :header="$t('inventory.view.columns.ean')" :sortable="true" headerClass="hidden md:table-cell" bodyClass="hidden md:table-cell"></Column>
      <Column :header="$t('inventory.view.columns.type')" headerClass="hidden md:table-cell" bodyClass="hidden md:table-cell">
        <template #body="slotProps">
          <Badge v-if="slotProps.data.isProfessional" :value="$t('inventory.view.professional')" severity="info" class="mr-2 px-1" />
          <Badge v-if="slotProps.data.isRetail" :value="$t('inventory.view.retail')" severity="success" class="px-1" />
        </template>
      </Column>
      <Column field="packageSize" :header="$t('inventory.view.columns.packageSize')" headerClass="whitespace-nowrap !pr-6" bodyClass="whitespace-nowrap !pr-6">
        <template #body="slotProps">
          {{ slotProps.data.packageSize }} {{ getUnitName(slotProps.data.unit) }}
        </template>
      </Column>
      <Column field="currentStock" :header="$t('inventory.view.columns.stock')" :sortable="true" headerClass="whitespace-nowrap !min-w-[6rem]" bodyClass="whitespace-nowrap !min-w-[6rem]">
        <template #body="slotProps">
          <div 
            class="cursor-pointer inline-flex items-center gap-2 px-3 py-1 rounded-md hover:bg-surface-200 transition-colors" 
            :class="{'text-red-500 font-bold': slotProps.data.currentStock <= slotProps.data.lowStockThreshold, 'font-bold': true}"
            @click="openQuickStock(slotProps.data)"
            :title="$t('inventory.view.quickStockTitle')"
          >
            {{ slotProps.data.currentStock }} {{ $t('inventory.units.short.pcs') }}
            <i class="pi pi-pencil text-xs opacity-50"></i>
          </div>
        </template>
      </Column>
      
      <template #empty>
        {{ $t('inventory.view.empty') }}
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
import { useI18n } from 'vue-i18n';

const { t } = useI18n();

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
  const units = [
    t('inventory.units.short.ml'),
    t('inventory.units.short.g'),
    t('inventory.units.short.pcs'),
    t('inventory.units.short.m'),
    t('inventory.units.short.cm')
  ];
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

