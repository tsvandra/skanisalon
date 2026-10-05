<template>
  <Dialog 
    :visible="visible" 
    @update:visible="$emit('update:visible', $event)" 
    :style="{width: '450px'}" 
    :header="$t('inventory.quickStockDialog.header')" 
    :modal="true" 
    class="p-fluid"
  >
    <div v-if="product" class="mb-4">
      <div class="text-lg font-bold mb-1">{{ product.name }}</div>
      <div class="text-sm text-surface-500 mb-4">{{ $t('inventory.quickStockDialog.currentStock') }} <strong>{{ product.currentStock }}</strong></div>
      
      <div class="flex flex-col gap-1 mb-4">
        <label for="newStock" class="font-bold">{{ $t('inventory.quickStockDialog.newStock') }}</label>
        <InputNumber id="newStock" v-model="newStock" mode="decimal" autofocus class="w-full" :min="0" />
      </div>

      <div class="flex flex-col gap-1 mb-4">
        <label for="note" class="font-bold">{{ $t('inventory.quickStockDialog.note') }}</label>
        <InputText id="note" v-model="note" :placeholder="$t('inventory.quickStockDialog.notePlaceholder')" class="w-full" />
      </div>

      <Message v-if="stockDifference !== 0" :severity="stockDifference > 0 ? 'success' : 'warn'" :closable="false" class="mt-2">
        {{ $t('inventory.quickStockDialog.differenceBefore') }} <strong>{{ Math.abs(stockDifference) }}</strong> {{ $t('inventory.quickStockDialog.differenceAfter', { kind: stockDifference > 0 ? $t('inventory.quickStockDialog.receiptKind') : $t('inventory.quickStockDialog.issueKind') }) }}
      </Message>
      <Message v-else severity="info" :closable="false" class="mt-2">
        {{ $t('inventory.quickStockDialog.noDifference') }}
      </Message>
    </div>

    <template #footer>
      <Button :label="$t('inventory.quickStockDialog.cancel')" icon="pi pi-times" text @click="hideDialog" />
      <Button :label="$t('inventory.quickStockDialog.save')" icon="pi pi-check" @click="saveAdjustment" :loading="saving" :disabled="stockDifference === 0 ? true : false" />
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
import { useI18n } from 'vue-i18n';
import inventoryApi from '@/services/inventoryApi.js';

const { t } = useI18n();

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
    note.value = t('inventory.quickStockDialog.defaultNote');
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

