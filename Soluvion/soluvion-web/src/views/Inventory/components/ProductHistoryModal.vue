<template>
  <Dialog
    :visible="visible"
    @update:visible="hideDialog"
    :style="{ width: '95vw', maxWidth: '780px' }"
    :modal="true"
    :dismissableMask="true"
    class="p-fluid"
    :pt="{
      root: { class: 'bg-surface border border-text/10 rounded-2xl overflow-hidden shadow-2xl' },
      header: { class: 'px-4 sm:px-6 py-4 border-b border-text/10 bg-background/50' },
      title: { class: 'text-base sm:text-lg font-bold text-text flex items-center gap-2' },
      content: { class: 'p-4 sm:p-6 bg-surface max-h-[82vh] overflow-y-auto space-y-4' },
      footer: { class: 'px-4 sm:px-6 py-3.5 border-t border-text/10 bg-background/50 flex justify-end gap-2' },
      closeButton: { class: 'hover:bg-text/10 p-2 rounded-full transition-colors w-8 h-8 flex items-center justify-center text-text-muted' }
    }"
  >
    <template #header>
      <div class="flex items-center gap-3 min-w-0 pr-2">
        <div
          v-if="historyData?.imageUrl || product?.imageUrl"
          class="w-11 h-11 rounded-xl overflow-hidden border border-text/15 bg-background shrink-0 shadow-2xs"
        >
          <img :src="historyData?.imageUrl || product?.imageUrl" class="w-full h-full object-cover" alt="Termékkép" />
        </div>
        <div
          v-else
          class="w-11 h-11 rounded-xl bg-primary/10 text-primary border border-primary/20 flex items-center justify-center shrink-0"
        >
          <i class="pi pi-history text-lg"></i>
        </div>

        <div class="min-w-0">
          <div class="flex items-center gap-2 flex-wrap">
            <h2 class="text-base sm:text-lg font-black text-text truncate">
              {{ historyData?.productName || product?.name || $t('inventory.history.title') }}
            </h2>
            <span
              v-if="historyData?.shade || product?.shade"
              class="text-xs font-black bg-primary/15 text-primary border border-primary/30 px-2 py-0.5 rounded-lg shrink-0"
            >
              {{ historyData?.shade || product?.shade }}
            </span>
          </div>
          <p class="text-xs text-text-muted truncate">
            {{ $t('inventory.history.subtitle') }}
          </p>
        </div>
      </div>
    </template>

    <!-- Betöltés -->
    <div v-if="loading" class="py-14 text-center text-text-muted space-y-2 animate-pulse">
      <i class="pi pi-spinner pi-spin text-2xl text-primary"></i>
      <div class="text-xs font-bold">{{ $t('common.loading') }}</div>
    </div>

    <template v-else-if="historyData">
      <!-- Összesítő KPI Kártyák -->
      <div class="grid grid-cols-2 sm:grid-cols-4 gap-2.5">
        <div class="p-3 rounded-xl bg-background/60 border border-text/10">
          <div class="text-[11px] font-bold text-text-muted uppercase tracking-wider">
            {{ $t('inventory.history.currentStock') }}
          </div>
          <div class="text-lg font-black text-primary mt-0.5">
            {{ formatQty(historyData.currentStock) }} {{ $t('inventory.units.short.pcs') }}
          </div>
        </div>

        <div class="p-3 rounded-xl bg-emerald-500/5 border border-emerald-500/20">
          <div class="text-[11px] font-bold text-emerald-400/80 uppercase tracking-wider">
            {{ $t('inventory.history.totalReceived') }}
          </div>
          <div class="text-lg font-black text-emerald-400 mt-0.5">
            +{{ formatQty(historyData.totalReceivedQty) }} {{ $t('inventory.units.short.pcs') }}
          </div>
        </div>

        <div class="p-3 rounded-xl bg-amber-500/5 border border-amber-500/20">
          <div class="text-[11px] font-bold text-amber-400/80 uppercase tracking-wider">
            {{ $t('inventory.history.totalIssued') }}
          </div>
          <div class="text-lg font-black text-amber-400 mt-0.5">
            -{{ formatQty(historyData.totalIssuedQty) }} {{ $t('inventory.units.short.pcs') }}
          </div>
        </div>

        <div class="p-3 rounded-xl bg-background/60 border border-text/10">
          <div class="text-[11px] font-bold text-text-muted uppercase tracking-wider">
            {{ $t('inventory.history.lastCostPrice') }}
          </div>
          <div class="text-lg font-black text-text mt-0.5 flex items-baseline gap-1.5 flex-wrap">
            <span>{{ historyData.lastCostPrice ? formatCurrency(historyData.lastCostPrice) : '-' }}</span>
            <span
              v-if="historyData.previousCostPrice && historyData.lastCostPrice && historyData.previousCostPrice !== historyData.lastCostPrice"
              class="text-[10px] font-bold px-1.5 py-0.2 rounded"
              :class="historyData.lastCostPrice > historyData.previousCostPrice ? 'bg-red-500/15 text-red-400' : 'bg-emerald-500/15 text-emerald-400'"
            >
              {{ historyData.lastCostPrice > historyData.previousCostPrice ? '↑' : '↓' }} {{ formatCurrency(historyData.previousCostPrice) }}
            </span>
          </div>
        </div>
      </div>

      <!-- Szűrő Fülek -->
      <div class="flex items-center gap-1.5 overflow-x-auto no-scrollbar py-0.5">
        <button
          type="button"
          @click="activeFilter = 'all'"
          :class="activeFilter === 'all' ? 'bg-primary text-white border-primary shadow-2xs' : 'bg-background text-text-muted border-text/10 hover:text-text'"
          class="px-3 py-1.5 rounded-xl text-xs font-bold border transition-all whitespace-nowrap"
        >
          {{ $t('inventory.history.filterAll', { count: movements.length }) }}
        </button>
        <button
          type="button"
          @click="activeFilter = 'receipts'"
          :class="activeFilter === 'receipts' ? 'bg-emerald-600 text-white border-emerald-600 shadow-2xs' : 'bg-background text-text-muted border-text/10 hover:text-text'"
          class="px-3 py-1.5 rounded-xl text-xs font-bold border transition-all whitespace-nowrap"
        >
          {{ $t('inventory.history.filterReceipts', { count: receiptsCount }) }}
        </button>
        <button
          type="button"
          @click="activeFilter = 'issues'"
          :class="activeFilter === 'issues' ? 'bg-amber-500 text-black border-amber-500 font-black shadow-2xs' : 'bg-background text-text-muted border-text/10 hover:text-text'"
          class="px-3 py-1.5 rounded-xl text-xs font-bold border transition-all whitespace-nowrap"
        >
          {{ $t('inventory.history.filterIssues', { count: issuesCount }) }}
        </button>
      </div>

      <!-- Üres állapot -->
      <div
        v-if="filteredMovements.length === 0"
        class="py-12 text-center bg-background/40 rounded-2xl border border-dashed border-text/15 text-text-muted space-y-2"
      >
        <i class="pi pi-inbox text-3xl opacity-40"></i>
        <p class="text-sm font-medium">{{ $t('inventory.history.empty') }}</p>
      </div>

      <!-- Mozgások listája -->
      <div v-else class="space-y-2.5">
        <div
          v-for="mov in filteredMovements"
          :key="mov.itemId"
          class="p-3.5 rounded-xl bg-background/60 border border-text/10 hover:border-primary/30 transition-all space-y-2.5"
          :class="{ 'opacity-60': mov.isReversed }"
        >
          <!-- Felső sor: Típus badge, Dátum, Bizonylat megnyitó gomb & Mennyiség/Egyenleg -->
          <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
            <div class="flex items-center gap-2 flex-wrap">
              <!-- Típus jelvény -->
              <span
                class="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-lg text-xs font-bold border"
                :class="
                  mov.type === 0
                    ? 'bg-emerald-500/15 text-emerald-400 border-emerald-500/30'
                    : mov.type === 1
                      ? 'bg-amber-500/15 text-amber-400 border-amber-500/30'
                      : 'bg-sky-500/15 text-sky-400 border-sky-500/30'
                "
              >
                <i :class="mov.type === 0 ? 'pi pi-arrow-down-left' : 'pi pi-arrow-up-right'" class="text-[10px]"></i>
                <span>{{ getTypeLabel(mov.type) }}</span>
              </span>

              <!-- Sztornó jelvények -->
              <span
                v-if="mov.isReversed"
                class="px-2 py-0.5 rounded-lg text-[11px] font-bold bg-red-500/15 text-red-400 border border-red-500/30 line-through"
              >
                {{ $t('inventory.history.reversedBadge') }}
              </span>
              <span
                v-if="mov.reversalOfDocumentId"
                class="px-2 py-0.5 rounded-lg text-[11px] font-bold bg-purple-500/15 text-purple-300 border border-purple-500/30"
              >
                {{ $t('inventory.history.reversalOfBadge', { id: mov.reversalOfDocumentId }) }}
              </span>

              <!-- Dátum és idő -->
              <span class="text-xs text-text-muted font-medium flex items-center gap-1">
                <i class="pi pi-clock text-[11px]"></i>
                {{ formatDateTime(mov.createdAt) }}
              </span>

              <!-- Bizonylat (Príjemka / Výdajka) megnyitása gomb -->
              <button
                type="button"
                @click="openDocument(mov.documentId)"
                class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-lg text-xs font-bold bg-primary/10 text-primary border border-primary/25 hover:bg-primary hover:text-white transition-all active:scale-95"
              >
                <i class="pi pi-file text-[11px]"></i>
                <span>
                  {{
                    mov.documentItemsCount > 1
                      ? $t('inventory.history.openDocument', { id: mov.documentId, count: mov.documentItemsCount })
                      : $t('inventory.history.openSingleDocument', { id: mov.documentId })
                  }}
                </span>
                <i class="pi pi-external-link text-[10px]"></i>
              </button>
            </div>

            <!-- Jobb oldal: Mennyiség változás és Készlet egyenleg (Előtte -> Utána) -->
            <div class="flex items-center gap-2.5 self-end sm:self-center">
              <span
                v-if="mov.costPrice > 0"
                class="text-xs font-semibold text-text-muted bg-surface px-2 py-0.5 rounded-lg border border-text/10"
              >
                {{ $t('inventory.history.unitCost', { price: formatCurrency(mov.costPrice) }) }}
              </span>

              <span class="text-xs font-semibold text-text-muted bg-surface px-2 py-0.5 rounded-lg border border-text/10">
                {{
                  $t('inventory.history.balanceChange', {
                    before: formatQty(mov.stockBefore),
                    after: formatQty(mov.stockAfter),
                    unit: $t('inventory.units.short.pcs')
                  })
                }}
              </span>

              <span
                class="text-sm sm:text-base font-black px-2.5 py-0.5 rounded-xl border"
                :class="
                  mov.signedQuantity > 0
                    ? 'bg-emerald-500/15 text-emerald-400 border-emerald-500/30'
                    : 'bg-amber-500/15 text-amber-400 border-amber-500/30'
                "
              >
                {{ mov.signedQuantity > 0 ? `+${formatQty(mov.quantity)}` : `-${formatQty(mov.quantity)}` }}
                {{ $t('inventory.units.short.pcs') }}
              </span>
            </div>
          </div>

          <!-- Meta adatok: Rögzítő felhasználó, Kapcsolódó vendég -->
          <div
            v-if="mov.createdByName || mov.customerName"
            class="flex flex-wrap items-center gap-3 text-xs text-text-muted"
          >
            <span v-if="mov.createdByName" class="inline-flex items-center gap-1">
              <i class="pi pi-user text-[11px] text-primary"></i>
              <span>{{ $t('inventory.history.recordedBy', { name: mov.createdByName }) }}</span>
            </span>
            <span v-if="mov.customerName" class="inline-flex items-center gap-1">
              <i class="pi pi-calendar text-[11px] text-sky-400"></i>
              <span>{{ $t('inventory.history.relatedAppointment', { customer: mov.customerName }) }}</span>
            </span>
          </div>

          <!-- Megjegyzések blokk (Bizonylat közös megjegyzés + Tételszintű megjegyzés szerkesztéssel) -->
          <div class="pt-1 border-t border-text/10 space-y-1.5 text-xs">
            <!-- 1. Bizonylat megjegyzés -->
            <div v-if="editingDocNoteId === mov.documentId" class="flex items-center gap-1.5">
              <InputText
                v-model="editingDocNoteText"
                :placeholder="$t('inventory.history.docNotePlaceholder')"
                class="flex-1 bg-surface border border-primary/50 px-2.5 py-1.5 rounded-lg text-xs text-text"
                @keyup.enter="saveDocNote(mov)"
              />
              <button
                type="button"
                @click="saveDocNote(mov)"
                :disabled="savingNote"
                class="px-2.5 py-1.5 rounded-lg bg-primary text-white font-bold text-xs hover:brightness-110"
              >
                {{ $t('inventory.history.saveNote') }}
              </button>
              <button
                type="button"
                @click="editingDocNoteId = null"
                class="px-2 py-1.5 rounded-lg bg-surface border border-text/20 text-text-muted text-xs hover:text-text"
              >
                {{ $t('inventory.history.cancelNote') }}
              </button>
            </div>
            <div v-else-if="mov.documentNote" class="flex items-start justify-between gap-2 text-text-muted">
              <div class="flex items-start gap-1.5 min-w-0">
                <i class="pi pi-file-edit text-primary text-xs mt-0.5 shrink-0"></i>
                <div class="break-words">
                  <span class="font-bold text-text/80 mr-1">{{ $t('inventory.history.documentNoteLabel') }}</span>
                  <span class="text-text">{{ mov.documentNote }}</span>
                </div>
              </div>
              <button
                type="button"
                @click="startEditDocNote(mov)"
                class="text-text-muted hover:text-primary p-1 rounded transition-colors shrink-0"
                :title="$t('inventory.history.editNote')"
              >
                <i class="pi pi-pencil text-[11px]"></i>
              </button>
            </div>

            <!-- 2. Sorhoz (tételhez) tartozó egyedi megjegyzés -->
            <div v-if="editingItemNoteId === mov.itemId" class="flex items-center gap-1.5">
              <InputText
                v-model="editingItemNoteText"
                :placeholder="$t('inventory.history.itemNotePlaceholder')"
                class="flex-1 bg-surface border border-primary/50 px-2.5 py-1.5 rounded-lg text-xs text-text"
                @keyup.enter="saveItemNote(mov)"
              />
              <button
                type="button"
                @click="saveItemNote(mov)"
                :disabled="savingNote"
                class="px-2.5 py-1.5 rounded-lg bg-primary text-white font-bold text-xs hover:brightness-110"
              >
                {{ $t('inventory.history.saveNote') }}
              </button>
              <button
                type="button"
                @click="editingItemNoteId = null"
                class="px-2 py-1.5 rounded-lg bg-surface border border-text/20 text-text-muted text-xs hover:text-text"
              >
                {{ $t('inventory.history.cancelNote') }}
              </button>
            </div>
            <div v-else-if="mov.itemNote" class="flex items-start justify-between gap-2 bg-primary/5 border border-primary/20 rounded-lg px-2.5 py-1.5">
              <div class="flex items-start gap-1.5 min-w-0">
                <i class="pi pi-comment text-primary text-xs mt-0.5 shrink-0"></i>
                <div class="break-words">
                  <span class="font-bold text-primary mr-1">{{ $t('inventory.history.itemNoteLabel') }}</span>
                  <span class="text-text font-medium">{{ mov.itemNote }}</span>
                </div>
              </div>
              <button
                type="button"
                @click="startEditItemNote(mov)"
                class="text-text-muted hover:text-primary p-1 rounded transition-colors shrink-0"
                :title="$t('inventory.history.editNote')"
              >
                <i class="pi pi-pencil text-[11px]"></i>
              </button>
            </div>
            <div v-else class="flex items-center gap-3 pt-0.5">
              <button
                type="button"
                @click="startEditItemNote(mov)"
                class="text-[11px] font-bold text-text-muted hover:text-primary inline-flex items-center gap-1 transition-colors"
              >
                <i class="pi pi-plus-circle text-[10px]"></i>
                <span>{{ $t('inventory.history.addItemNote') }}</span>
              </button>
              <button
                v-if="!mov.documentNote"
                type="button"
                @click="startEditDocNote(mov)"
                class="text-[11px] font-bold text-text-muted hover:text-primary inline-flex items-center gap-1 transition-colors"
              >
                <i class="pi pi-file-edit text-[10px]"></i>
                <span>{{ $t('inventory.history.editNote') }}</span>
              </button>
            </div>
          </div>
        </div>
      </div>
    </template>

    <template #footer>
      <button
        type="button"
        @click="hideDialog"
        class="h-10 px-5 rounded-xl text-xs sm:text-sm font-bold bg-background text-text border border-text/20 hover:bg-text/5 active:scale-95 transition-all"
      >
        {{ $t('inventory.history.close') }}
      </button>
    </template>
  </Dialog>
</template>

<script setup>
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import { useI18n } from 'vue-i18n';
import { useToast } from 'primevue/usetoast';
import inventoryApi from '@/services/inventoryApi';

const props = defineProps({
  visible: Boolean,
  product: Object
});

const emit = defineEmits(['update:visible', 'open-document']);

const { t, locale } = useI18n();
const toast = useToast();

const loading = ref(false);
const historyData = ref(null);
const activeFilter = ref('all');

const editingItemNoteId = ref(null);
const editingItemNoteText = ref('');
const editingDocNoteId = ref(null);
const editingDocNoteText = ref('');
const savingNote = ref(false);

const movements = computed(() => historyData.value?.movements || []);

const receiptsCount = computed(() => movements.value.filter(m => m.type === 0).length);
const issuesCount = computed(() => movements.value.filter(m => m.type !== 0).length);

const filteredMovements = computed(() => {
  if (activeFilter.value === 'receipts') {
    return movements.value.filter(m => m.type === 0);
  }
  if (activeFilter.value === 'issues') {
    return movements.value.filter(m => m.type !== 0);
  }
  return movements.value;
});

const fetchHistory = async () => {
  if (!props.product?.id) return;
  loading.value = true;
  try {
    const res = await inventoryApi.getProductHistory(props.product.id);
    historyData.value = res.data;
  } catch (err) {
    console.error('Hiba a termék mozgástörténet lekérésekor:', err);
  } finally {
    loading.value = false;
  }
};

watch(
  () => [props.visible, props.product?.id],
  ([vis]) => {
    if (vis && props.product?.id) {
      activeFilter.value = 'all';
      editingItemNoteId.value = null;
      editingDocNoteId.value = null;
      fetchHistory();
    }
  }
);

const hideDialog = () => {
  emit('update:visible', false);
};

const openDocument = (documentId) => {
  emit('open-document', {
    documentId,
    highlightProductId: props.product?.id
  });
};

const startEditItemNote = (mov) => {
  editingDocNoteId.value = null;
  editingItemNoteId.value = mov.itemId;
  editingItemNoteText.value = mov.itemNote || '';
};

const saveItemNote = async (mov) => {
  savingNote.value = true;
  try {
    const trimmed = editingItemNoteText.value?.trim() || null;
    await inventoryApi.updateItemNote(mov.itemId, trimmed);
    mov.itemNote = trimmed;
    editingItemNoteId.value = null;
    toast.add({
      severity: 'success',
      summary: t('inventory.history.noteSaved'),
      life: 2200
    });
  } catch (err) {
    console.error('Hiba a tétel megjegyzés mentésekor:', err);
    toast.add({
      severity: 'error',
      summary: t('inventory.history.noteSaveError'),
      life: 3000
    });
  } finally {
    savingNote.value = false;
  }
};

const startEditDocNote = (mov) => {
  editingItemNoteId.value = null;
  editingDocNoteId.value = mov.documentId;
  editingDocNoteText.value = mov.documentNote || '';
};

const saveDocNote = async (mov) => {
  savingNote.value = true;
  try {
    const trimmed = editingDocNoteText.value?.trim() || null;
    await inventoryApi.updateDocumentNote(mov.documentId, trimmed);
    // Frissítjük az összes azonos bizonylathoz tartozó sort
    movements.value.forEach(m => {
      if (m.documentId === mov.documentId) {
        m.documentNote = trimmed;
      }
    });
    editingDocNoteId.value = null;
    toast.add({
      severity: 'success',
      summary: t('inventory.history.noteSaved'),
      life: 2200
    });
  } catch (err) {
    console.error('Hiba a bizonylat megjegyzés mentésekor:', err);
    toast.add({
      severity: 'error',
      summary: t('inventory.history.noteSaveError'),
      life: 3000
    });
  } finally {
    savingNote.value = false;
  }
};

const getTypeLabel = (type) => {
  if (type === 0) return t('inventory.history.typeReceipt');
  if (type === 1) return t('inventory.history.typeIssue');
  return t('inventory.history.typeAdjustment');
};

const formatQty = (val) => {
  if (val == null || isNaN(val)) return '0';
  const num = Number(val);
  return Number.isInteger(num) ? String(num) : num.toFixed(2).replace(/\.?0+$/, '');
};

const formatCurrency = (val) => {
  if (val == null || isNaN(val)) return '-';
  return `${Number(val).toFixed(2)} €`;
};

const formatDateTime = (iso) => {
  if (!iso) return '';
  const d = new Date(iso);
  return (
    d.toLocaleDateString(locale.value || 'hu-HU', {
      year: 'numeric',
      month: 'short',
      day: '2-digit'
    }) +
    ' ' +
    d.toLocaleTimeString(locale.value || 'hu-HU', {
      hour: '2-digit',
      minute: '2-digit'
    })
  );
};

defineExpose({
  refresh: fetchHistory
});
</script>

