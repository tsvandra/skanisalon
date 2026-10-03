<template>
  <Dialog 
    :visible="visible" 
    @update:visible="$emit('update:visible', $event)" 
    :style="{width: '500px'}" 
    :header="isEdit ? 'Termék szerkesztése' : 'Új termék felvitele'" 
    :modal="true" 
    class="p-fluid"
  >
    <div class="flex flex-col gap-1 mb-4">
      <label for="name" class="font-bold">Termék neve *</label>
      <InputText id="name" v-model.trim="product.name" required autofocus class="w-full" />
    </div>

    <div class="flex flex-col gap-1 mb-4">
      <label for="ean" class="font-bold">Vonalkód (EAN)</label>
      <InputText id="ean" v-model.trim="product.ean" class="w-full" />
    </div>

    <div class="flex flex-col sm:flex-row gap-4 mb-4">
      <div class="flex-1 flex flex-col gap-1">
        <label for="unit" class="font-bold">Mértékegység</label>
        <Dropdown id="unit" v-model="product.unit" :options="unitOptions" optionLabel="label" optionValue="value" placeholder="Válassz..." class="w-full" />
      </div>
      <div class="flex-1 flex flex-col gap-1">
        <label for="packageSize" class="font-bold">Kiszerelés</label>
        <InputNumber id="packageSize" v-model="product.packageSize" mode="decimal" class="w-full" />
      </div>
    </div>

    <div class="flex flex-col sm:flex-row gap-4 mb-4">
      <div class="flex-1 flex flex-col gap-1">
        <label for="costPrice" class="font-bold">Beszerzési ár</label>
        <InputNumber id="costPrice" v-model="product.costPrice" mode="currency" currency="EUR" locale="sk-SK" class="w-full" />
      </div>
      <div class="flex-1 flex flex-col gap-1">
        <label for="retailPrice" class="font-bold">Eladási ár</label>
        <InputNumber id="retailPrice" v-model="product.retailPrice" mode="currency" currency="EUR" locale="sk-SK" class="w-full" />
      </div>
    </div>

    <div class="flex flex-col gap-1 mb-4">
      <label for="lowStockThreshold" class="font-bold">Minimum készlet (Figyelmeztetéshez)</label>
      <InputNumber id="lowStockThreshold" v-model="product.lowStockThreshold" mode="decimal" class="w-full" />
    </div>

    <div class="flex gap-4 mb-4">
      <div class="flex items-center">
        <Checkbox v-model="product.isProfessional" inputId="isProfessional" :binary="true" />
        <label for="isProfessional" class="ml-2">Professzionális (Szalonhasználat)</label>
      </div>
      <div class="flex items-center">
        <Checkbox v-model="product.isRetail" inputId="isRetail" :binary="true" />
        <label for="isRetail" class="ml-2">Lakossági (Eladható)</label>
      </div>
    </div>

    <template #footer>
      <Button label="Mégse" icon="pi pi-times" text @click="hideDialog" />
      <Button label="Mentés" icon="pi pi-check" @click="saveProduct" :loading="saving" />
    </template>
  </Dialog>
</template>

<script setup>
import { ref, watch, computed } from 'vue';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import InputNumber from 'primevue/inputnumber';
import Dropdown from 'primevue/dropdown';
import Checkbox from 'primevue/checkbox';
import Button from 'primevue/button';
import productApi from '@/services/productApi';

const props = defineProps({
  visible: Boolean,
  productData: Object
});

const emit = defineEmits(['update:visible', 'saved']);

const product = ref({
  name: '',
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

const unitOptions = [
  { label: 'Milliliter (ml)', value: 0 },
  { label: 'Gramm (g)', value: 1 },
  { label: 'Darab (db)', value: 2 }
];

watch(() => props.visible, (newVal) => {
  if (newVal) {
    if (props.productData) {
      product.value = { ...props.productData };
    } else {
      product.value = {
        name: '', ean: '', unit: 0, packageSize: 0, costPrice: 0, retailPrice: 0, lowStockThreshold: 0, isProfessional: true, isRetail: false
      };
    }
  }
});

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

