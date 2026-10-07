<template>
  <Dialog 
    :visible="visible" 
    @update:visible="$emit('update:visible', $event)" 
    :style="{ width: '92vw', maxWidth: '440px' }" 
    :header="$t('inventory.quickStockDialog.header')" 
    :modal="true" 
    class="p-fluid"
    :pt="{
      root: { class: 'bg-surface border border-text/10 rounded-2xl overflow-hidden shadow-2xl' },
      header: { class: 'px-5 py-4 border-b border-text/10 bg-background/50' },
      title: { class: 'text-lg md:text-xl font-bold text-text flex items-center gap-2' },
      content: { class: 'p-5 bg-surface' },
      footer: { class: 'p-4 border-t border-text/10 bg-background/50 flex justify-end gap-2' },
      closeButton: { class: 'hover:bg-text/10 p-2 rounded-full transition-colors w-8 h-8 flex items-center justify-center text-text-muted' }
    }"
  >
    <div v-if="product" class="space-y-4">
      <!-- Termék infó kártya -->
      <div class="p-3.5 bg-background/60 border border-text/10 rounded-xl">
        <div class="text-base font-bold text-text leading-snug">{{ product.name }}</div>
        <div class="text-xs text-text-muted mt-1 flex items-center gap-2">
          <span>{{ $t('inventory.quickStockDialog.currentStock') }}</span>
          <span class="font-black text-primary text-sm px-2 py-0.5 rounded-md bg-primary/10 border border-primary/20">
            {{ product.currentStock }} {{ $t('inventory.units.short.pcs') }}
          </span>
        </div>
      </div>
      
      <!-- Új készlet és léptető gombok -->
      <div class="flex flex-col gap-1.5">
        <label for="newStock" class="text-xs font-bold text-text-muted uppercase tracking-wider">
          {{ $t('inventory.quickStockDialog.newStock') }}
        </label>
        
        <div class="flex items-center gap-2">
          <button 
            type="button" 
            @click="adjustStock(-1)"
            class="w-12 h-11 rounded-xl bg-background border border-text/20 text-text font-bold text-lg hover:border-primary hover:text-primary active:scale-95 transition-all flex items-center justify-center shrink-0"
            :title="$t('inventory.view.quickSub')"
          >
            -
          </button>
          
          <InputNumber 
            id="newStock" 
            v-model="newStock" 
            mode="decimal" 
            autofocus 
            class="flex-1" 
            inputClass="w-full text-center text-lg font-black bg-background border border-text/20 p-2.5 rounded-xl focus:outline-none focus:border-primary text-text h-11"
            :min="0" 
          />

          <button 
            type="button" 
            @click="adjustStock(1)"
            class="w-12 h-11 rounded-xl bg-background border border-text/20 text-text font-bold text-lg hover:border-primary hover:text-primary active:scale-95 transition-all flex items-center justify-center shrink-0"
            :title="$t('inventory.view.quickAdd')"
          >
            +
          </button>
        </div>

        <!-- Gyors módosító pill gombok (mobilon rendkívül gyors!) -->
        <div class="flex items-center gap-2 pt-1">
          <span class="text-xs text-text-muted shrink-0">{{ $t('inventory.quickStockDialog.quickAdjust') }}</span>
          <div class="flex gap-1.5 overflow-x-auto no-scrollbar py-0.5">
            <button 
              type="button" 
              @click="adjustStock(-5)" 
              class="px-2.5 py-1 rounded-lg text-xs font-bold bg-background border border-text/10 text-text-muted hover:text-primary hover:border-primary/40 active:scale-95 transition-all"
            >
              -5
            </button>
            <button 
              type="button" 
              @click="adjustStock(-1)" 
              class="px-2.5 py-1 rounded-lg text-xs font-bold bg-background border border-text/10 text-text-muted hover:text-primary hover:border-primary/40 active:scale-95 transition-all"
            >
              -1
            </button>
            <button 
              type="button" 
              @click="adjustStock(1)" 
              class="px-2.5 py-1 rounded-lg text-xs font-bold bg-background border border-text/10 text-text-muted hover:text-primary hover:border-primary/40 active:scale-95 transition-all"
            >
              +1
            </button>
            <button 
              type="button" 
              @click="adjustStock(5)" 
              class="px-2.5 py-1 rounded-lg text-xs font-bold bg-background border border-text/10 text-text-muted hover:text-primary hover:border-primary/40 active:scale-95 transition-all"
            >
              +5
            </button>
            <button 
              type="button" 
              @click="adjustStock(10)" 
              class="px-2.5 py-1 rounded-lg text-xs font-bold bg-background border border-text/10 text-text-muted hover:text-primary hover:border-primary/40 active:scale-95 transition-all"
            >
              +10
            </button>
          </div>
        </div>
      </div>

      <!-- Megjegyzés -->
      <div class="flex flex-col gap-1.5">
        <label for="note" class="text-xs font-bold text-text-muted uppercase tracking-wider">
          {{ $t('inventory.quickStockDialog.note') }}
        </label>
        <InputText 
          id="note" 
          v-model="note" 
          :placeholder="$t('inventory.quickStockDialog.notePlaceholder')" 
          class="w-full bg-background border border-text/20 p-2.5 rounded-xl focus:outline-none focus:border-primary text-text text-sm" 
        />
      </div>

      <!-- Eltérés visszajelzés -->
      <div v-if="stockDifference !== 0" 
           :class="stockDifference > 0 ? 'bg-emerald-500/10 border-emerald-500/30 text-emerald-400' : 'bg-amber-500/10 border-amber-500/30 text-amber-400'"
           class="p-3 rounded-xl border text-xs font-medium flex items-center gap-2">
        <i :class="stockDifference > 0 ? 'pi pi-arrow-up text-emerald-400' : 'pi pi-arrow-down text-amber-400'" class="text-sm"></i>
        <span>
          {{ $t('inventory.quickStockDialog.differenceBefore') }} 
          <strong>{{ Math.abs(stockDifference) }} {{ $t('inventory.units.short.pcs') }}</strong> 
          {{ $t('inventory.quickStockDialog.differenceAfter', { kind: stockDifference > 0 ? $t('inventory.quickStockDialog.receiptKind') : $t('inventory.quickStockDialog.issueKind') }) }}
        </span>
      </div>
      <div v-else class="p-3 rounded-xl border border-text/10 bg-background/50 text-text-muted text-xs text-center">
        {{ $t('inventory.quickStockDialog.noDifference') }}
      </div>
    </div>

    <template #footer>
      <button 
        type="button"
        @click="hideDialog" 
        class="h-10 px-4 rounded-xl text-xs md:text-sm font-bold bg-background text-text border border-text/20 hover:bg-text/5 active:scale-95 transition-all"
      >
        {{ $t('inventory.quickStockDialog.cancel') }}
      </button>
      <button 
        type="button"
        @click="saveAdjustment" 
        :disabled="stockDifference === 0 || saving" 
        class="h-10 px-5 rounded-xl text-xs md:text-sm font-bold bg-primary text-white shadow-md hover:brightness-110 active:scale-95 transition-all disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
      >
        <i v-if="saving" class="pi pi-spinner pi-spin text-xs"></i>
        <i v-else class="pi pi-check text-xs"></i>
        <span>{{ $t('inventory.quickStockDialog.save') }}</span>
      </button>
    </template>
  </Dialog>
</template>

<script setup>
// @ts-nocheck
import { ref, watch, computed } from 'vue';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import InputNumber from 'primevue/inputnumber';
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

const adjustStock = (delta) => {
  newStock.value = Math.max(0, (newStock.value || 0) + delta);
};

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
