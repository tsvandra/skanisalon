<template>
  <Dialog
    :visible="visible"
    @update:visible="hideDialog"
    :style="{ width: '95vw', maxWidth: '820px' }"
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
          class="w-10 h-10 rounded-xl flex items-center justify-center shrink-0 border"
          :class="
            doc?.type === 0
              ? 'bg-emerald-500/15 text-emerald-400 border-emerald-500/30'
              : doc?.type === 1
                ? 'bg-amber-500/15 text-amber-400 border-amber-500/30'
                : 'bg-sky-500/15 text-sky-400 border-sky-500/30'
          "
        >
          <i :class="doc?.type === 0 ? 'pi pi-file-import' : 'pi pi-file-export'" class="text-base"></i>
        </div>

        <div class="min-w-0">
          <div class="flex items-center gap-2 flex-wrap">
            <h2 class="text-base sm:text-lg font-black text-text">
              {{ $t('inventory.documents.docHeaderTitle', { id: doc?.id || documentId || '' }) }}
            </h2>
            <span
              v-if="doc"
              class="px-2.5 py-0.5 rounded-lg text-xs font-bold border"
              :class="
                doc.type === 0
                  ? 'bg-emerald-500/15 text-emerald-400 border-emerald-500/30'
                  : doc.type === 1
                    ? 'bg-amber-500/15 text-amber-400 border-amber-500/30'
                    : 'bg-sky-500/15 text-sky-400 border-sky-500/30'
              "
            >
              {{ getTypeLabel(doc.type) }}
            </span>
            <span
              v-if="doc?.isReversed"
              class="px-2 py-0.5 rounded-lg text-xs font-bold bg-red-500/15 text-red-400 border border-red-500/30 line-through"
            >
              {{ $t('inventory.history.reversedBadge') }}
            </span>
            <span
              v-if="doc?.reversalOfDocumentId"
              class="px-2 py-0.5 rounded-lg text-xs font-bold bg-purple-500/15 text-purple-300 border border-purple-500/30"
            >
              {{ $t('inventory.history.reversalOfBadge', { id: doc.reversalOfDocumentId }) }}
            </span>
          </div>
          <p v-if="doc" class="text-xs text-text-muted mt-0.5 flex flex-wrap items-center gap-2">
            <span>{{ formatDateTime(doc.createdAt) }}</span>
            <span v-if="doc.createdByName">• {{ $t('inventory.history.recordedBy', { name: doc.createdByName }) }}</span>
            <span v-if="doc.customerName">• {{ $t('inventory.history.relatedAppointment', { customer: doc.customerName }) }}</span>
          </p>
        </div>
      </div>
    </template>

    <!-- Betöltés -->
    <div v-if="loading" class="py-14 text-center text-text-muted space-y-2 animate-pulse">
      <i class="pi pi-spinner pi-spin text-2xl text-primary"></i>
      <div class="text-xs font-bold">{{ $t('common.loading') }}</div>
    </div>

    <template v-else-if="doc">
      <!-- Bizonylat megjegyzés és Összérték kártya -->
      <div class="p-3.5 rounded-xl bg-background/60 border border-text/10 space-y-2">
        <div class="flex items-center justify-between gap-2">
          <span class="text-xs font-bold text-text-muted uppercase tracking-wider flex items-center gap-1.5">
            <i class="pi pi-file-edit text-primary text-xs"></i>
            {{ $t('inventory.documents.docNote') }}
          </span>
          <div class="flex items-center gap-2">
            <span
              v-if="doc.totalValue > 0"
              class="px-2.5 py-0.5 rounded-lg text-xs font-black bg-emerald-500/15 text-emerald-400 border border-emerald-500/30"
            >
              {{ $t('inventory.documents.totalValue', { value: formatCurrency(doc.totalValue) }) }}
            </span>
            <button
              v-if="!editingDocNote"
              type="button"
              @click="startEditDocNote"
              class="text-xs font-bold text-primary hover:underline flex items-center gap-1"
            >
              <i class="pi pi-pencil text-[10px]"></i>
              <span>{{ $t('inventory.history.editNote') }}</span>
            </button>
          </div>
        </div>

        <div v-if="editingDocNote" class="flex items-center gap-2 pt-1">
          <InputText
            v-model="editingDocNoteText"
            :placeholder="$t('inventory.history.docNotePlaceholder')"
            class="flex-1 bg-surface border border-primary/50 px-3 py-1.5 rounded-lg text-xs sm:text-sm text-text"
            @keyup.enter="saveDocNote"
          />
          <button
            type="button"
            @click="saveDocNote"
            :disabled="savingNote"
            class="px-3 py-1.5 rounded-lg bg-primary text-white font-bold text-xs hover:brightness-110"
          >
            {{ $t('inventory.history.saveNote') }}
          </button>
          <button
            type="button"
            @click="editingDocNote = false"
            class="px-2.5 py-1.5 rounded-lg bg-surface border border-text/20 text-text-muted text-xs hover:text-text"
          >
            {{ $t('inventory.history.cancelNote') }}
          </button>
        </div>
        <div v-else class="text-sm text-text break-words">
          <span v-if="doc.note" class="font-medium">{{ doc.note }}</span>
          <span v-else class="text-text-muted italic text-xs">{{ $t('inventory.documents.noDocNote') }}</span>
        </div>
      </div>

      <!-- Tételek listája a bizonylaton -->
      <div class="space-y-2.5">
        <div class="text-xs font-bold text-text-muted uppercase tracking-wider flex items-center justify-between">
          <span>{{ $t('inventory.documents.itemsTitle', { count: doc.items?.length || 0 }) }}</span>
        </div>

        <div class="space-y-2">
          <div
            v-for="item in doc.items"
            :key="item.id"
            class="p-3 rounded-xl border transition-all space-y-2"
            :class="
              highlightProductId && String(item.productId) === String(highlightProductId)
                ? 'bg-primary/5 border-primary/40 ring-1 ring-primary/30'
                : 'bg-background/60 border-text/10 hover:border-text/25'
            "
          >
            <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2.5">
              <!-- Bal oldal: Kép, Terméknév, Árnyalat, Kiszerelés -->
              <div class="flex items-center gap-2.5 min-w-0">
                <div
                  v-if="item.productImageUrl"
                  class="w-10 h-10 rounded-lg overflow-hidden border border-text/15 bg-background shrink-0"
                >
                  <img :src="item.productImageUrl" class="w-full h-full object-cover" alt="Kép" />
                </div>
                <div
                  v-else
                  class="w-10 h-10 rounded-lg border border-text/10 bg-background/50 flex items-center justify-center shrink-0 text-text-muted/40"
                >
                  <i class="pi pi-box text-sm"></i>
                </div>

                <div class="min-w-0">
                  <div class="flex items-center gap-1.5 flex-wrap">
                    <span class="font-bold text-sm text-text break-words">
                      {{ item.productName }}
                    </span>
                    <span
                      v-if="item.productShade"
                      class="text-[11px] font-black bg-primary/15 text-primary border border-primary/30 px-1.5 py-0.2 rounded-md shrink-0"
                    >
                      {{ item.productShade }}
                    </span>
                    <span
                      v-if="highlightProductId && String(item.productId) === String(highlightProductId)"
                      class="text-[10px] font-bold bg-primary text-white px-1.5 py-0.2 rounded shrink-0"
                    >
                      {{ $t('inventory.documents.currentProductBadge') }}
                    </span>
                  </div>
                  <div class="text-xs text-text-muted flex items-center gap-2 mt-0.5">
                    <span v-if="item.productPackageSize > 0">
                      {{ item.productPackageSize }} {{ getUnitName(item.productUnit) }}
                    </span>
                    <span v-if="item.productEan" class="font-mono text-[11px] opacity-75">
                      EAN: {{ item.productEan }}
                    </span>
                  </div>
                </div>
              </div>

              <!-- Jobb oldal: Egységár, Egyenleg változás, Mennyiség és Termék história gomb -->
              <div class="flex items-center gap-2 self-end sm:self-center flex-wrap">
                <span
                  v-if="item.costPrice > 0"
                  class="text-xs font-semibold text-text-muted bg-surface px-2 py-0.5 rounded-lg border border-text/10"
                >
                  {{ formatCurrency(item.costPrice) }} / {{ $t('inventory.units.short.pcs') }}
                </span>

                <span
                  v-if="item.stockBefore != null && item.stockAfter != null"
                  class="text-xs font-semibold text-text-muted bg-surface px-2 py-0.5 rounded-lg border border-text/10"
                >
                  {{ formatQty(item.stockBefore) }} → {{ formatQty(item.stockAfter) }} {{ $t('inventory.units.short.pcs') }}
                </span>

                <span
                  class="text-sm font-black px-2.5 py-0.5 rounded-xl border"
                  :class="
                    doc.type === 0
                      ? 'bg-emerald-500/15 text-emerald-400 border-emerald-500/30'
                      : 'bg-amber-500/15 text-amber-400 border-amber-500/30'
                  "
                >
                  {{ doc.type === 0 ? `+${formatQty(item.quantity)}` : `-${formatQty(item.quantity)}` }}
                  {{ $t('inventory.units.short.pcs') }}
                </span>

                <button
                  type="button"
                  @click="openProductHistory(item)"
                  class="w-8 h-8 rounded-lg bg-surface border border-text/15 text-text-muted hover:text-primary hover:border-primary/40 flex items-center justify-center transition-colors"
                  :title="$t('inventory.documents.viewProductHistory')"
                >
                  <i class="pi pi-history text-xs"></i>
                </button>
              </div>
            </div>

            <!-- Soronkénti (tételszintű) megjegyzés -->
            <div class="pt-1 border-t border-text/10 text-xs">
              <div v-if="editingItemNoteId === item.id" class="flex items-center gap-1.5">
                <InputText
                  v-model="editingItemNoteText"
                  :placeholder="$t('inventory.history.itemNotePlaceholder')"
                  class="flex-1 bg-surface border border-primary/50 px-2.5 py-1 rounded-lg text-xs text-text"
                  @keyup.enter="saveItemNote(item)"
                />
                <button
                  type="button"
                  @click="saveItemNote(item)"
                  :disabled="savingNote"
                  class="px-2.5 py-1 rounded-lg bg-primary text-white font-bold text-xs"
                >
                  {{ $t('inventory.history.saveNote') }}
                </button>
                <button
                  type="button"
                  @click="editingItemNoteId = null"
                  class="px-2 py-1 rounded-lg bg-surface border border-text/20 text-text-muted text-xs"
                >
                  {{ $t('inventory.history.cancelNote') }}
                </button>
              </div>
              <div v-else-if="item.itemNote" class="flex items-center justify-between gap-2 text-text">
                <div class="flex items-center gap-1.5 min-w-0">
                  <i class="pi pi-comment text-primary text-[11px] shrink-0"></i>
                  <span class="font-bold text-primary">{{ $t('inventory.history.itemNoteLabel') }}</span>
                  <span class="truncate">{{ item.itemNote }}</span>
                </div>
                <button
                  type="button"
                  @click="startEditItemNote(item)"
                  class="text-text-muted hover:text-primary p-0.5"
                >
                  <i class="pi pi-pencil text-[10px]"></i>
                </button>
              </div>
              <button
                v-else
                type="button"
                @click="startEditItemNote(item)"
                class="text-[11px] font-bold text-text-muted hover:text-primary inline-flex items-center gap-1 transition-colors"
              >
                <i class="pi pi-plus-circle text-[10px]"></i>
                <span>{{ $t('inventory.history.addItemNote') }}</span>
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
        {{ $t('inventory.documents.close') }}
      </button>
    </template>
  </Dialog>
</template>

<script setup>
import { ref, watch } from 'vue';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import { useI18n } from 'vue-i18n';
import { useToast } from 'primevue/usetoast';
import inventoryApi from '@/services/inventoryApi';

const props = defineProps({
  visible: Boolean,
  documentId: Number,
  highlightProductId: [Number, String]
});

const emit = defineEmits(['update:visible', 'note-updated', 'open-product-history']);

const { t, locale } = useI18n();
const toast = useToast();

const loading = ref(false);
const doc = ref(null);

const editingDocNote = ref(false);
const editingDocNoteText = ref('');
const editingItemNoteId = ref(null);
const editingItemNoteText = ref('');
const savingNote = ref(false);

const fetchDocument = async () => {
  if (!props.documentId) return;
  loading.value = true;
  try {
    const res = await inventoryApi.getDocument(props.documentId);
    doc.value = res.data;
  } catch (err) {
    console.error('Hiba a bizonylat lekérésekor:', err);
  } finally {
    loading.value = false;
  }
};

watch(
  () => [props.visible, props.documentId],
  ([vis]) => {
    if (vis && props.documentId) {
      editingDocNote.value = false;
      editingItemNoteId.value = null;
      fetchDocument();
    }
  }
);

const hideDialog = () => {
  emit('update:visible', false);
};

const openProductHistory = (item) => {
  emit('open-product-history', {
    id: item.productId,
    name: item.productName,
    shade: item.productShade,
    imageUrl: item.productImageUrl
  });
};

const startEditDocNote = () => {
  editingItemNoteId.value = null;
  editingDocNote.value = true;
  editingDocNoteText.value = doc.value?.note || '';
};

const saveDocNote = async () => {
  if (!doc.value) return;
  savingNote.value = true;
  try {
    const trimmed = editingDocNoteText.value?.trim() || null;
    await inventoryApi.updateDocumentNote(doc.value.id, trimmed);
    doc.value.note = trimmed;
    editingDocNote.value = false;
    emit('note-updated');
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

const startEditItemNote = (item) => {
  editingDocNote.value = false;
  editingItemNoteId.value = item.id;
  editingItemNoteText.value = item.itemNote || '';
};

const saveItemNote = async (item) => {
  savingNote.value = true;
  try {
    const trimmed = editingItemNoteText.value?.trim() || null;
    await inventoryApi.updateItemNote(item.id, trimmed);
    item.itemNote = trimmed;
    editingItemNoteId.value = null;
    emit('note-updated');
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

const getTypeLabel = (type) => {
  if (type === 0) return t('inventory.history.typeReceipt');
  if (type === 1) return t('inventory.history.typeIssue');
  return t('inventory.history.typeAdjustment');
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
</script>

