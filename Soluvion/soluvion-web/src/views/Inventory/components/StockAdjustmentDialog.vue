<template>
  <Dialog 
    :visible="visible" 
    @update:visible="$emit('update:visible', $event)" 
    :style="{ width: '95vw', maxWidth: '600px' }" 
    :header="$t('inventory.stockDialog.header')" 
    :modal="true" 
    class="p-fluid"
    :pt="{
      root: { class: 'bg-surface border border-text/10 rounded-2xl overflow-hidden shadow-2xl' },
      header: { class: 'px-5 py-4 border-b border-text/10 bg-background/50' },
      title: { class: 'text-lg md:text-xl font-bold text-text flex items-center gap-2' },
      content: { class: 'p-4 sm:p-5 bg-surface max-h-[75vh] overflow-y-auto space-y-4' },
      footer: { class: 'p-4 border-t border-text/10 bg-background/50 flex justify-end gap-2' },
      closeButton: { class: 'hover:bg-text/10 p-2 rounded-full transition-colors w-8 h-8 flex items-center justify-center text-text-muted' }
    }"
  >
    <div class="flex flex-col gap-1.5">
      <label for="type" class="text-xs font-bold text-text-muted uppercase tracking-wider">{{ $t('inventory.stockDialog.type') }}</label>
      <Dropdown 
        id="type" 
        v-model="movement.type" 
        :options="typeOptions" 
        optionLabel="label" 
        optionValue="value"
        class="w-full bg-background border border-text/20 rounded-xl px-3 py-2 text-sm text-text"
      />
    </div>

    <div class="flex flex-col gap-1.5">
      <label for="note" class="text-xs font-bold text-text-muted uppercase tracking-wider">{{ $t('inventory.stockDialog.note') }}</label>
      <InputText id="note" v-model="movement.note" :placeholder="$t('inventory.stockDialog.notePlaceholder')" class="w-full bg-background border border-text/20 p-2.5 rounded-xl text-sm text-text focus:outline-none focus:border-primary" />
    </div>

    <div class="space-y-2">
      <label class="text-xs font-bold text-text-muted uppercase tracking-wider">{{ $t('inventory.stockDialog.addProducts') }}</label>
      <div class="flex gap-2">
        <Dropdown 
          v-model="selectedProduct" 
          :options="products" 
          optionLabel="name" 
          :placeholder="$t('inventory.stockDialog.selectProduct')" 
          filter 
          class="flex-1 bg-background border border-text/20 rounded-xl px-3 py-2 text-sm text-text"
        />
        <button 
          type="button" 
          @click="addProductToMovement" 
          :disabled="!selectedProduct" 
          class="h-10 px-4 rounded-xl bg-primary text-white font-bold disabled:opacity-50 disabled:cursor-not-allowed hover:brightness-110 active:scale-95 transition-all flex items-center justify-center shrink-0"
        >
          <i class="pi pi-plus"></i>
        </button>
      </div>
    </div>

    <!-- Hozzáadott tételek listája (mobilon kártyaként, asztalin táblázatként) -->
    <div v-if="movement.items.length > 0" class="space-y-2 pt-2">
      <div class="text-xs font-bold text-text-muted uppercase tracking-wider">
        {{ movement.items.length }} {{ $t('inventory.stockDialog.columns.product') }}
      </div>

      <!-- Mobilon kártyás lista -->
      <div class="space-y-2">
        <div 
          v-for="(item, index) in movement.items" 
          :key="item.productId" 
          class="p-3 bg-background/60 border border-text/10 rounded-xl flex flex-col sm:flex-row sm:items-center justify-between gap-3"
        >
          <div class="font-bold text-sm text-text truncate sm:w-1/2" :title="item.productName">
            {{ item.productName }}
          </div>

          <div class="flex items-center gap-2">
            <div class="flex items-center gap-1.5 flex-1 sm:flex-none">
              <span class="text-xs text-text-muted sm:hidden">{{ $t('inventory.stockDialog.columns.quantity') }}:</span>
              <InputNumber 
                v-model="item.quantity" 
                mode="decimal" 
                :min="0.1" 
                inputClass="w-20 text-center bg-background border border-text/20 p-1.5 rounded-lg text-sm text-text font-bold" 
              />
            </div>

            <div v-if="movement.type === 0" class="flex items-center gap-1.5 flex-1 sm:flex-none">
              <span class="text-xs text-text-muted sm:hidden">{{ $t('inventory.stockDialog.columns.costPrice') }}:</span>
              <InputNumber 
                v-model="item.costPrice" 
                mode="currency" 
                currency="EUR" 
                locale="sk-SK" 
                inputClass="w-24 text-center bg-background border border-text/20 p-1.5 rounded-lg text-sm text-text font-bold" 
              />
            </div>

            <button 
              type="button" 
              @click="removeItem(index)" 
              class="w-8 h-8 rounded-lg bg-red-500/10 text-red-500 flex items-center justify-center hover:bg-red-500 hover:text-white transition-colors shrink-0"
              title="Törlés"
            >
              <i class="pi pi-trash text-xs"></i>
            </button>
          </div>
        </div>
      </div>
    </div>

    <template #footer>
      <button 
        type="button"
        @click="hideDialog" 
        class="h-10 px-4 rounded-xl text-xs md:text-sm font-bold bg-background text-text border border-text/20 hover:bg-text/5 active:scale-95 transition-all"
      >
        {{ $t('inventory.stockDialog.cancel') }}
      </button>
      <button 
        type="button"
        @click="saveMovement" 
        :disabled="movement.items.length === 0 || saving" 
        class="h-10 px-5 rounded-xl text-xs md:text-sm font-bold bg-primary text-white shadow-md hover:brightness-110 active:scale-95 transition-all disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
      >
        <i v-if="saving" class="pi pi-spinner pi-spin text-xs"></i>
        <i v-else class="pi pi-check text-xs"></i>
        <span>{{ $t('inventory.stockDialog.save') }}</span>
      </button>
    </template>
  </Dialog>
</template>

<script setup>
import { ref, watch, computed } from 'vue';
import Dialog from 'primevue/dialog';
import Dropdown from 'primevue/dropdown';
import InputText from 'primevue/inputtext';
import InputNumber from 'primevue/inputnumber';
import { useI18n } from 'vue-i18n';
import inventoryApi from '@/services/inventoryApi';

const { t } = useI18n();

const props = defineProps({
  visible: Boolean,
  products: Array
});

const emit = defineEmits(['update:visible', 'saved']);

// 0: Receipt, 1: Issue, 2: Adjustment
const typeOptions = computed(() => [
  { label: t('inventory.stockDialog.typeReceipt'), value: 0 },
  { label: t('inventory.stockDialog.typeIssue'), value: 1 }
]);

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
