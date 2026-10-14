<template>
  <Dialog
    :visible="visible"
    @update:visible="hideDialog"
    :style="{ width: '95vw', maxWidth: '860px' }"
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
        <div class="w-10 h-10 rounded-xl bg-primary/10 text-primary border border-primary/20 flex items-center justify-center shrink-0">
          <i class="pi pi-folder-open text-lg"></i>
        </div>
        <div class="min-w-0">
          <h2 class="text-base sm:text-lg font-black text-text truncate">
            {{ $t('inventory.documents.listTitle') }}
          </h2>
          <p class="text-xs text-text-muted truncate">
            {{ $t('inventory.documents.listSubtitle') }}
          </p>
        </div>
      </div>
    </template>

    <!-- Kereső & Szűrő sáv -->
    <div class="flex flex-col sm:flex-row gap-2.5 items-stretch sm:items-center justify-between">
      <div class="relative flex-1">
        <i class="pi pi-search absolute left-3 top-1/2 -translate-y-1/2 text-text-muted text-xs"></i>
        <input
          type="text"
          v-model="searchQuery"
          :placeholder="$t('inventory.documents.searchPlaceholder')"
          class="w-full h-10 pl-9 pr-8 bg-background border border-text/20 rounded-xl text-xs sm:text-sm text-text focus:outline-none focus:border-primary"
        />
        <button
          v-if="searchQuery"
          type="button"
          @click="searchQuery = ''"
          class="absolute right-2.5 top-1/2 -translate-y-1/2 text-text-muted hover:text-text"
        >
          <i class="pi pi-times text-xs"></i>
        </button>
      </div>

      <div class="flex items-center gap-1.5 overflow-x-auto no-scrollbar">
        <button
          type="button"
          @click="activeFilter = 'all'"
          :class="activeFilter === 'all' ? 'bg-primary text-white border-primary' : 'bg-background text-text-muted border-text/10 hover:text-text'"
          class="px-3 py-1.5 rounded-xl text-xs font-bold border transition-all whitespace-nowrap"
        >
          {{ $t('inventory.history.filterAll', { count: documents.length }) }}
        </button>
        <button
          type="button"
          @click="activeFilter = 'receipts'"
          :class="activeFilter === 'receipts' ? 'bg-emerald-600 text-white border-emerald-600' : 'bg-background text-text-muted border-text/10 hover:text-text'"
          class="px-3 py-1.5 rounded-xl text-xs font-bold border transition-all whitespace-nowrap"
        >
          {{ $t('inventory.history.filterReceipts', { count: receiptsCount }) }}
        </button>
        <button
          type="button"
          @click="activeFilter = 'issues'"
          :class="activeFilter === 'issues' ? 'bg-amber-500 text-black border-amber-500 font-black' : 'bg-background text-text-muted border-text/10 hover:text-text'"
          class="px-3 py-1.5 rounded-xl text-xs font-bold border transition-all whitespace-nowrap"
        >
          {{ $t('inventory.history.filterIssues', { count: issuesCount }) }}
        </button>
      </div>
    </div>

    <!-- Betöltés -->
    <div v-if="loading" class="py-14 text-center text-text-muted space-y-2 animate-pulse">
      <i class="pi pi-spinner pi-spin text-2xl text-primary"></i>
      <div class="text-xs font-bold">{{ $t('common.loading') }}</div>
    </div>

    <!-- Üres állapot -->
    <div
      v-else-if="filteredDocuments.length === 0"
      class="py-12 text-center bg-background/40 rounded-2xl border border-dashed border-text/15 text-text-muted space-y-2"
    >
      <i class="pi pi-inbox text-3xl opacity-40"></i>
      <p class="text-sm font-medium">{{ $t('inventory.documents.empty') }}</p>
    </div>

    <!-- Bizonylatok listája -->
    <div v-else class="space-y-2.5">
      <div
        v-for="doc in filteredDocuments"
        :key="doc.id"
        @click="selectDocument(doc.id)"
        class="p-3.5 rounded-xl bg-background/60 border border-text/10 hover:border-primary/40 cursor-pointer transition-all space-y-2 group"
        :class="{ 'opacity-60': doc.isReversed }"
      >
        <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
          <div class="flex items-center gap-2 flex-wrap">
            <span class="font-black text-sm text-text group-hover:text-primary transition-colors">
              #{{ doc.id }}
            </span>

            <span
              class="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-lg text-xs font-bold border"
              :class="
                doc.type === 0
                  ? 'bg-emerald-500/15 text-emerald-400 border-emerald-500/30'
                  : doc.type === 1
                    ? 'bg-amber-500/15 text-amber-400 border-amber-500/30'
                    : 'bg-sky-500/15 text-sky-400 border-sky-500/30'
              "
            >
              <i :class="doc.type === 0 ? 'pi pi-arrow-down-left' : 'pi pi-arrow-up-right'" class="text-[10px]"></i>
              <span>{{ getTypeLabel(doc.type) }}</span>
            </span>

            <span
              v-if="doc.isReversed"
              class="px-2 py-0.5 rounded-lg text-[11px] font-bold bg-red-500/15 text-red-400 border border-red-500/30 line-through"
            >
              {{ $t('inventory.history.reversedBadge') }}
            </span>

            <span class="text-xs text-text-muted font-medium flex items-center gap-1">
              <i class="pi pi-clock text-[11px]"></i>
              {{ formatDateTime(doc.createdAt) }}
            </span>

            <span v-if="doc.createdByName" class="text-xs text-text-muted flex items-center gap-1">
              <i class="pi pi-user text-[10px] text-primary"></i>
              {{ doc.createdByName }}
            </span>

            <span v-if="doc.customerName" class="text-xs text-sky-400 font-semibold flex items-center gap-1">
              <i class="pi pi-calendar text-[10px]"></i>
              {{ doc.customerName }}
            </span>
          </div>

          <div class="flex items-center gap-2 self-end sm:self-center">
            <span
              v-if="doc.totalValue > 0"
              class="text-xs font-bold text-emerald-400 bg-emerald-500/10 border border-emerald-500/25 px-2.5 py-0.5 rounded-lg"
            >
              {{ formatCurrency(doc.totalValue) }}
            </span>

            <span class="text-xs font-bold text-primary bg-primary/10 border border-primary/25 px-2.5 py-0.5 rounded-lg flex items-center gap-1">
              <span>{{ $t('inventory.documents.itemsCount', { count: doc.itemsCount || doc.items?.length || 0 }) }}</span>
              <i class="pi pi-chevron-right text-[10px]"></i>
            </span>
          </div>
        </div>

        <div v-if="doc.note" class="text-xs text-text font-medium break-words">
          {{ doc.note }}
        </div>

        <!-- Termék előnézeti címkék (max 5 termék) -->
        <div v-if="doc.items && doc.items.length > 0" class="flex flex-wrap items-center gap-1.5 pt-0.5">
          <span
            v-for="item in doc.items.slice(0, 5)"
            :key="item.id"
            class="text-[11px] bg-surface border border-text/10 text-text-muted px-2 py-0.5 rounded-md truncate max-w-[220px]"
          >
            {{ item.productName }}
            <strong v-if="item.productShade" class="text-primary">[{{ item.productShade }}]</strong>
            ({{ doc.type === 0 ? '+' : '-' }}{{ formatQty(item.quantity) }})
          </span>
          <span
            v-if="doc.items.length > 5"
            class="text-[11px] font-bold text-text-muted px-1.5 py-0.5"
          >
            +{{ doc.items.length - 5 }}...
          </span>
        </div>
      </div>
    </div>

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
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import { useI18n } from 'vue-i18n';
import inventoryApi from '@/services/inventoryApi';

const props = defineProps({
  visible: Boolean
});

const emit = defineEmits(['update:visible', 'open-document']);

const { t, locale } = useI18n();

const loading = ref(false);
const documents = ref([]);
const searchQuery = ref('');
const activeFilter = ref('all');

const fetchDocuments = async () => {
  loading.value = true;
  try {
    const res = await inventoryApi.getAllDocuments();
    documents.value = res.data || [];
  } catch (err) {
    console.error('Hiba a bizonylatok lekérésekor:', err);
  } finally {
    loading.value = false;
  }
};

watch(
  () => props.visible,
  (vis) => {
    if (vis) {
      fetchDocuments();
    }
  }
);

const receiptsCount = computed(() => documents.value.filter(d => d.type === 0).length);
const issuesCount = computed(() => documents.value.filter(d => d.type !== 0).length);

const filteredDocuments = computed(() => {
  let list = documents.value;
  if (activeFilter.value === 'receipts') {
    list = list.filter(d => d.type === 0);
  } else if (activeFilter.value === 'issues') {
    list = list.filter(d => d.type !== 0);
  }

  if (searchQuery.value.trim()) {
    const q = searchQuery.value.trim().toLowerCase();
    list = list.filter(d => {
      if (String(d.id).includes(q) || `#${d.id}`.includes(q)) return true;
      if (d.note && d.note.toLowerCase().includes(q)) return true;
      if (d.createdByName && d.createdByName.toLowerCase().includes(q)) return true;
      if (d.customerName && d.customerName.toLowerCase().includes(q)) return true;
      if (
        d.items &&
        d.items.some(
          i =>
            (i.productName && i.productName.toLowerCase().includes(q)) ||
            (i.productShade && i.productShade.toLowerCase().includes(q)) ||
            (i.itemNote && i.itemNote.toLowerCase().includes(q))
        )
      ) {
        return true;
      }
      return false;
    });
  }

  return list;
});

const hideDialog = () => {
  emit('update:visible', false);
};

const selectDocument = (documentId) => {
  emit('open-document', { documentId, highlightProductId: null });
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
  refresh: fetchDocuments
});
</script>

