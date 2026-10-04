<template>
  <Dialog 
    :visible="visible" 
    @update:visible="$emit('update:visible', $event)" 
    :style="{width: '600px'}" 
    header="Készletmódosítás / Inventúra" 
    :modal="true" 
    class="p-fluid"
  >
    <div class="flex flex-col gap-1 mb-4">
      <label for="type" class="font-bold">Művelet típusa *</label>
      <Dropdown 
        id="type" 
        v-model="movement.type" 
        :options="typeOptions" 
        optionLabel="label" 
        optionValue="value"
        class="w-full"
      />
    </div>

    <div class="flex flex-col gap-1 mb-4">
      <label for="note" class="font-bold">Megjegyzés</label>
      <InputText id="note" v-model="movement.note" placeholder="Pl. Havi leltár, árkezés, sérült áru..." class="w-full" />
    </div>

    <div class="mb-4">
      <label class="font-bold">Termékek hozzáadása</label>
      <div class="flex gap-2 mb-2">
        <Dropdown 
          v-model="selectedProduct" 
          :options="products" 
          optionLabel="name" 
          placeholder="Válassz terméket..." 
          filter 
          class="flex-1"
        />
        <Button icon="pi pi-plus" @click="addProductToMovement" :disabled="!selectedProduct ? true : false" />
      </div>
    </div>

    <div v-if="movement.items.length > 0" class="mb-4">
      <DataTable :value="movement.items" class="p-datatable-sm">
        <Column field="productName" header="Termék"></Column>
        <Column header="Mennyiség">
          <template #body="slotProps">
            <InputNumber v-model="slotProps.data.quantity" mode="decimal" class="w-full" />
          </template>
        </Column>
        <Column v-if="movement.type === 0" header="Beszerzési ár">
          <template #body="slotProps">
            <InputNumber v-model="slotProps.data.costPrice" mode="currency" currency="EUR" locale="sk-SK" class="w-full" />
          </template>
        </Column>
        <Column header="">
          <template #body="slotProps">
            <Button icon="pi pi-trash" severity="danger" text @click="removeItem(slotProps.index)" />
          </template>
        </Column>
      </DataTable>
    </div>

    <template #footer>
      <Button label="Mégse" icon="pi pi-times" text @click="hideDialog" />
      <Button label="Mentés" icon="pi pi-check" @click="saveMovement" :loading="saving" :disabled="movement.items.length === 0 ? true : false" />
    </template>
  </Dialog>
</template>

<script setup>
import { ref, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Dropdown from 'primevue/dropdown';
import InputText from 'primevue/inputtext';
import InputNumber from 'primevue/inputnumber';
import Button from 'primevue/button';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import inventoryApi from '@/services/inventoryApi';

const props = defineProps({
  visible: Boolean,
  products: Array
});

const emit = defineEmits(['update:visible', 'saved']);

// 0: Receipt, 1: Issue, 2: Adjustment
const typeOptions = [
  { label: 'Bevételezés (Készlet növelése)', value: 0 },
  { label: 'Kiadás / Selejtezés (Készlet csökkentése)', value: 1 }
];

const selectedProduct = ref(null);
const saving = ref(false);

const movement = ref({
  type: 0,
  note: '',
  items: []
});

watch(() => props.visible, (newVal) => {
  if (newVal) {
    selectedProduct.value = null;
    movement.value = {
      type: 0,
      note: '',
      items: []
    };
  }
});

const addProductToMovement = () => {
  if (!selectedProduct.value) return;
  
  // Ellenőrizzük, hogy benne van-e már
  const exists = movement.value.items.find(i => i.productId === selectedProduct.value.id);
  if (exists) return; // Már benne van

  movement.value.items.push({
    productId: selectedProduct.value.id,
    productName: selectedProduct.value.name,
    quantity: 1,
    costPrice: selectedProduct.value.costPrice || 0
  });

  selectedProduct.value = null;
};

const removeItem = (index) => {
  movement.value.items.splice(index, 1);
};

const hideDialog = () => {
  emit('update:visible', false);
};

const saveMovement = async () => {
  saving.value = true;
  try {
    const payload = {
      type: movement.value.type,
      note: movement.value.note,
      items: movement.value.items.map(i => ({
        productId: i.productId,
        quantity: i.quantity,
        costPrice: movement.value.type === 0 ? i.costPrice : 0
      }))
    };

    await inventoryApi.createDocument(payload);
    emit('saved');
    hideDialog();
  } catch (error) {
    console.error('Hiba mozgás mentésekor', error);
  } finally {
    saving.value = false;
  }
};
</script>

