<template>
  <Dialog 
    :visible="visible" 
    @update:visible="$emit('update:visible', $event)" 
    :style="{ width: '95vw', maxWidth: '520px' }" 
    :header="isEdit ? $t('inventory.productDialog.titleEdit') : $t('inventory.productDialog.titleNew')" 
    :modal="true" 
    class="p-fluid"
    :pt="{
      root: { class: 'bg-surface border border-text/10 rounded-2xl overflow-hidden shadow-2xl' },
      header: { class: 'px-5 py-4 border-b border-text/10 bg-background/50' },
      title: { class: 'text-lg md:text-xl font-bold text-text' },
      content: { class: 'p-4 sm:p-6 bg-surface max-h-[75vh] overflow-y-auto' },
      footer: { class: 'p-4 border-t border-text/10 bg-background/50 flex justify-end gap-2' },
      closeButton: { class: 'hover:bg-text/10 p-2 rounded-full transition-colors w-8 h-8 flex items-center justify-center text-text-muted' }
    }"
  >
    <!-- AI fotó beolvasó gomb -->
    <div class="mb-4 mt-1">
      <button 
        type="button" 
        @click="aiScannerVisible = true" 
        class="w-full py-2.5 px-4 rounded-xl border border-primary/30 bg-primary/10 text-primary font-bold text-xs sm:text-sm hover:bg-primary/20 active:scale-98 transition-all flex items-center justify-center gap-2 shadow-2xs"
      >
        <i class="pi pi-sparkles text-sm"></i>
        <span>{{ $t('inventory.aiScanner.fillFromAi') }}</span>
      </button>

      <!-- Sikeres AI beolvasás visszajelzés -->
      <div v-if="aiSuccessMessage" class="mt-2 p-2.5 bg-emerald-500/10 border border-emerald-500/30 text-emerald-400 rounded-xl text-xs flex items-center justify-between gap-2">
        <div class="flex items-center gap-1.5">
          <i class="pi pi-check-circle text-sm shrink-0"></i>
          <span>{{ $t('inventory.aiScanner.successAlert') }}</span>
        </div>
        <button type="button" @click="aiSuccessMessage = false" class="hover:text-white"><i class="pi pi-times"></i></button>
      </div>
    </div>

    <div class="flex flex-col mb-4">
      <label for="name" class="text-sm font-bold text-text-muted mb-1">{{ $t('inventory.productDialog.name') }}</label>
      <InputText id="name" v-model.trim="product.name" required autofocus class="w-full bg-background border border-text/20 p-2.5 rounded-lg focus:outline-none focus:border-primary text-text font-bold" />
    </div>

    <div class="flex flex-col sm:flex-row gap-4 mb-4">
      <div class="flex-1 flex flex-col">
        <label for="shade" class="text-sm font-bold text-text-muted mb-1">{{ $t('inventory.productDialog.shade') }}</label>
        <InputText id="shade" v-model.trim="product.shade" :placeholder="$t('inventory.productDialog.shadePlaceholder')" class="w-full bg-background border border-text/20 p-2.5 rounded-lg focus:outline-none focus:border-primary text-text font-semibold" />
      </div>
      <div class="flex-1 flex flex-col">
        <label for="ean" class="text-sm font-bold text-text-muted mb-1">{{ $t('inventory.productDialog.ean') }}</label>
        <InputText id="ean" v-model.trim="product.ean" class="w-full bg-background border border-text/20 p-2.5 rounded-lg focus:outline-none focus:border-primary text-text" />
      </div>
    </div>

    <div class="flex flex-col sm:flex-row gap-4 mb-5">
      <div class="flex-1 flex flex-col">
        <label for="unit" class="text-sm font-bold text-text-muted mb-1">{{ $t('inventory.productDialog.unit') }}</label>
        <Dropdown id="unit" v-model="product.unit" :options="unitOptions" optionLabel="label" optionValue="value" :placeholder="$t('inventory.productDialog.unitPlaceholder')" class="w-full bg-background border border-text/20 rounded-lg focus:outline-none focus:border-primary px-3 py-2.5 flex items-center" />
      </div>
      <div class="flex-1 flex flex-col">
        <label for="packageSize" class="text-sm font-bold text-text-muted mb-1">{{ $t('inventory.productDialog.packageSize') }}</label>
        <InputNumber id="packageSize" v-model="product.packageSize" mode="decimal" inputClass="w-full bg-background border border-text/20 p-2.5 rounded-lg focus:outline-none focus:border-primary text-text" />
      </div>
    </div>

    <div class="flex flex-col sm:flex-row gap-4 mb-5">
      <div class="flex-1 flex flex-col">
        <label for="costPrice" class="text-sm font-bold text-text-muted mb-1">{{ $t('inventory.productDialog.costPrice') }}</label>
        <InputNumber id="costPrice" v-model="product.costPrice" mode="currency" currency="EUR" locale="sk-SK" inputClass="w-full bg-background border border-text/20 p-2.5 rounded-lg focus:outline-none focus:border-primary text-text" />
      </div>
      <div class="flex-1 flex flex-col">
        <label for="retailPrice" class="text-sm font-bold text-text-muted mb-1">{{ $t('inventory.productDialog.retailPrice') }}</label>
        <InputNumber id="retailPrice" v-model="product.retailPrice" mode="currency" currency="EUR" locale="sk-SK" inputClass="w-full bg-background border border-text/20 p-2.5 rounded-lg focus:outline-none focus:border-primary text-text" />
      </div>
    </div>

    <div class="flex flex-col mb-5">
      <label for="lowStockThreshold" class="text-sm font-bold text-text-muted mb-1">{{ $t('inventory.productDialog.lowStockThreshold') }}</label>
      <InputNumber id="lowStockThreshold" v-model="product.lowStockThreshold" mode="decimal" inputClass="w-full bg-background border border-text/20 p-2.5 rounded-lg focus:outline-none focus:border-primary text-text" />
    </div>

    <div class="flex gap-4 mb-4">
      <div class="flex items-center">
        <Checkbox v-model="product.isProfessional" inputId="isProfessional" :binary="true" :pt="{ box: ({ context }) => ({ class: context.checked ? '' : '!border-2 !border-text/40 !bg-background' }) }" />
        <label for="isProfessional" class="ml-2 font-medium">{{ $t('inventory.productDialog.professional') }}</label>
      </div>
      <div class="flex items-center">
        <Checkbox v-model="product.isRetail" inputId="isRetail" :binary="true" :pt="{ box: ({ context }) => ({ class: context.checked ? '' : '!border-2 !border-text/40 !bg-background' }) }" />
        <label for="isRetail" class="ml-2 font-medium">{{ $t('inventory.productDialog.retail') }}</label>
      </div>
    </div>

    <template #footer>
      <Button :label="$t('inventory.productDialog.cancel')" icon="pi pi-times" class="bg-background text-text border border-text/20 hover:bg-text/5 px-4 py-2 font-bold" @click="hideDialog" />
      <Button :label="$t('inventory.productDialog.save')" icon="pi pi-check" class="bg-primary text-white hover:brightness-110 px-4 py-2 font-bold" @click="saveProduct" :loading="saving" />
    </template>
  </Dialog>

  <!-- AI Fotó Beolvasó Modális Ablak -->
  <AiProductScannerModal 
    v-model:visible="aiScannerVisible" 
    @scanned="onAiScanned" 
  />
</template>

<script setup>
import { ref, watch, computed } from 'vue';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import InputNumber from 'primevue/inputnumber';
import Dropdown from 'primevue/dropdown';
import Checkbox from 'primevue/checkbox';
import Button from 'primevue/button';
import { useI18n } from 'vue-i18n';
import productApi from '@/services/productApi';
import AiProductScannerModal from './AiProductScannerModal.vue';

const { t } = useI18n();

const props = defineProps({
  visible: Boolean,
  productData: Object
});

const emit = defineEmits(['update:visible', 'saved']);

const aiScannerVisible = ref(false);
const aiSuccessMessage = ref(false);

const product = ref({
  name: '',
  shade: '',
  ean: '',
  unit: 0,
  packageSize: 0,
  costPrice: 0,
  retailPrice: 0,
  lowStockThreshold: 0,
  isProfessional: true,
  isRetail: false
});

const saving = ref(false);
const isEdit = computed(() => !!props.productData?.id);

const unitOptions = computed(() => [
  { label: t('inventory.units.full.ml'), value: 0 },
  { label: t('inventory.units.full.g'), value: 1 },
  { label: t('inventory.units.full.pcs'), value: 2 },
  { label: t('inventory.units.full.m'), value: 3 },
  { label: t('inventory.units.full.cm'), value: 4 }
]);

watch(() => props.visible, (newVal) => {
  if (newVal) {
    aiSuccessMessage.value = false;
    if (props.productData) {
      product.value = { ...props.productData };
    } else {
      product.value = {
        name: '', shade: '', ean: '', unit: 0, packageSize: 0, costPrice: 0, retailPrice: 0, lowStockThreshold: 0, isProfessional: true, isRetail: false
      };
    }
  }
});

const onAiScanned = (scannedData) => {
  if (!scannedData) return;
  if (scannedData.name) product.value.name = scannedData.name;
  if (scannedData.shade) product.value.shade = scannedData.shade;
  if (scannedData.ean) product.value.ean = scannedData.ean;
  if (scannedData.packageSize) product.value.packageSize = scannedData.packageSize;
  if (scannedData.unit !== undefined && scannedData.unit !== null) product.value.unit = scannedData.unit;
  if (scannedData.isProfessional !== undefined) product.value.isProfessional = scannedData.isProfessional;
  if (scannedData.isRetail !== undefined) product.value.isRetail = scannedData.isRetail;
  aiSuccessMessage.value = true;
};

const hideDialog = () => {
  emit('update:visible', false);
};

const saveProduct = async () => {
  if (!product.value.name) return;
  
  saving.value = true;
  try {
    if (isEdit.value) {
      await productApi.updateProduct(product.value.id, product.value);
    } else {
      await productApi.createProduct(product.value);
    }
    emit('saved');
    hideDialog();
  } catch (error) {
    console.error('Hiba mentéskor', error);
  } finally {
    saving.value = false;
  }
};
</script>
