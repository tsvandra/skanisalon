<template>
  <Dialog 
    :visible="visible" 
    @update:visible="$emit('update:visible', $event)" 
    :style="{width: '450px'}" 
    header="Leltári készletmódosítás" 
    :modal="true" 
    class="p-fluid"
  >
    <div v-if="product" class="mb-4">
      <div class="text-lg font-bold mb-1">{{ product.name }}</div>
      <div class="text-sm text-surface-500 mb-4">Jelenlegi készlet a rendszerben: <strong>{{ product.currentStock }}</strong></div>
      
      <div class="flex flex-col gap-1 mb-4">
        <label for="newStock" class="font-bold">Új, valós darabszám (polcon lévő)</label>
        <InputNumber id="newStock" v-model="newStock" mode="decimal" autofocus class="w-full" :min="0" />
      </div>

      <div class="flex flex-col gap-1 mb-4">
        <label for="note" class="font-bold">Megjegyzés / Indoklás</label>
        <InputText id="note" v-model="note" placeholder="Pl. Éves leltár eltérés, selejt, stb." class="w-full" />
      </div>

      <Message v-if="stockDifference !== 0" :severity="stockDifference > 0 ? 'success' : 'warn'" :closable="false" class="mt-2">
        A rendszer <strong>{{ Math.abs(stockDifference) }}</strong> darabos {{ stockDifference > 0 ? 'bevételezést' : 'kiadást' }} fog rögzíteni.
      </Message>
      <Message v-else severity="info" :closable="false" class="mt-2">
        Nincs eltérés, nem jön létre bizonylat.
      </Message>
    </div>

    <template #footer>
      <Button label="Mégse" icon="pi pi-times" text @click="hideDialog" />
      <Button label="Módosítás mentése" icon="pi pi-check" @click="saveAdjustment" :loading="saving" :disabled="stockDifference === 0 ? true : false" />
    </template>
  </Dialog>
</template>

<script setup>
// @ts-nocheck
import { ref, watch, computed } from 'vue';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import InputNumber from 'primevue/inputnumber';
import Button from 'primevue/button';
import Message from 'primevue/message';
import inventoryApi from '@/services/inventoryApi.js';

const props = defineProps({
  visible: Boolean,
  product: Object
});

const emit = defineEmits(['update:visible', 'saved']);

const newStock = ref(0);
const note = ref('');
const saving = ref(false);

watch(() => props.visible, (newVal) => {
  if (newVal && props.product) {
    newStock.value = props.product.currentStock || 0;
    note.value = 'Leltári korrekció';
  }
});

const stockDifference = computed(() => {
  if (!props.product) return 0;
  return (newStock.value || 0) - (props.product.currentStock || 0);
});

const hideDialog = () => {
  emit('update:visible', false);
};

const saveAdjustment = async () => {
  if (!props.product) return;
  const diff = stockDifference.value;
  if (diff === 0) return;

  saving.value = true;
  try {
    const type = diff > 0 ? 0 : 1; 
    
    const payload = {
      type: type,
      note: note.value,
      items: [
        {
          productId: props.product.id,
          quantity: Math.abs(diff),
          costPrice: 0 
        }
      ]
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

