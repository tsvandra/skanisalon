<template>
  <div class="fixed inset-0 bg-black/60 backdrop-blur-sm z-[1100] flex items-center justify-center p-4">
    <div class="bg-surface w-full max-w-2xl rounded-2xl shadow-2xl overflow-hidden border border-text/10 flex flex-col max-h-[92vh]">

      <div class="p-5 border-b border-text/10 flex justify-between items-center bg-background/50 shrink-0">
        <h2 class="text-xl font-bold text-text flex items-center gap-2">
          <i class="pi pi-sitemap text-primary"></i>
          {{ $t('customers.merge.title') }}
          <span class="text-xs font-bold text-text-muted ml-2">{{ step }}/2</span>
        </h2>
        <button @click="$emit('close')" class="w-8 h-8 flex items-center justify-center rounded-full hover:bg-text/10 text-text-muted transition-colors">
          <i class="pi pi-times"></i>
        </button>
      </div>

      <!-- 1. LÉPÉS: ügyfelek kiválasztása -->
      <div v-if="step === 1" class="p-5 flex flex-col gap-4 overflow-hidden">
        <p class="text-sm text-text-muted">{{ $t('customers.merge.selectHint') }}</p>

        <div class="relative">
          <i class="pi pi-search absolute left-3 top-1/2 -translate-y-1/2 text-text-muted"></i>
          <input type="text" v-model="search" :placeholder="$t('customers.merge.searchPlaceholder')"
                 class="w-full h-[44px] pl-10 pr-4 bg-background border border-text/20 rounded-xl text-sm focus:outline-none focus:border-primary">
        </div>

        <div v-if="selectedCustomers.length > 0" class="flex flex-wrap gap-2">
          <span v-for="c in selectedCustomers" :key="c.id"
                class="inline-flex items-center gap-1.5 bg-primary/10 text-primary border border-primary/20 rounded-lg pl-2.5 pr-1.5 py-1 text-xs font-bold">
            {{ c.name }}
            <button @click="toggle(c.id)" class="w-4 h-4 rounded hover:bg-primary/20 flex items-center justify-center"><i class="pi pi-times text-[9px]"></i></button>
          </span>
        </div>

        <div class="overflow-y-auto border border-text/10 rounded-xl divide-y divide-text/5 min-h-[160px] max-h-[40vh]">
          <label v-for="c in filtered" :key="c.id"
                 class="flex items-center gap-3 px-3 py-2.5 cursor-pointer hover:bg-text/5 transition-colors">
            <input type="checkbox" :checked="selectedIds.includes(c.id)" @change="toggle(c.id)" class="w-4 h-4 rounded border-text/30 accent-primary">
            <div class="min-w-0 flex-1">
              <div class="text-sm font-bold text-text truncate">{{ c.name }}</div>
              <div class="text-xs text-text-muted truncate">
                {{ c.phone || $t('customers.merge.noPhone') }}<span v-if="c.email"> · {{ c.email }}</span>
              </div>
            </div>
            <span class="text-[10px] font-mono text-text-muted shrink-0">#{{ c.id }}</span>
          </label>
          <div v-if="filtered.length === 0" class="p-6 text-center text-sm text-text-muted">{{ $t('customers.merge.noResults') }}</div>
        </div>
      </div>

      <!-- 2. LÉPÉS: végleges adatok -->
      <div v-else class="p-5 flex flex-col gap-5 overflow-y-auto">
        <p class="text-sm text-text-muted">
          {{ $t('customers.merge.finalHint') }}
        </p>

        <div v-for="field in baseFields" :key="field.key">
          <label class="block text-xs font-bold text-text-muted mb-1.5 uppercase">
            {{ field.label }} <span v-if="field.required" class="text-red-500">*</span>
          </label>
          <textarea v-if="field.multiline" v-model="final[field.key]" rows="3"
                    class="w-full bg-background border border-text/20 rounded-xl p-3 text-sm focus:outline-none focus:border-primary resize-none font-medium"></textarea>
          <input v-else :type="field.type || 'text'" v-model="final[field.key]" autocomplete="off"
                 class="w-full h-[44px] bg-background border border-text/20 rounded-xl px-4 text-sm focus:outline-none focus:border-primary font-medium">
          <div v-if="options(field.key).length > 1" class="flex flex-wrap gap-1.5 mt-2">
            <button v-for="opt in options(field.key)" :key="opt" @click="final[field.key] = opt" type="button"
                    class="text-[11px] px-2 py-1 rounded-md border font-bold max-w-full truncate transition-colors"
                    :class="final[field.key] === opt ? 'bg-primary text-white border-primary' : 'bg-background text-text border-text/20 hover:border-primary/50'"
                    :title="opt">
              {{ opt }}
            </button>
          </div>
        </div>

        <div v-if="attributeKeys.length > 0">
          <div class="h-px bg-text/10 mb-4"></div>
          <label class="block text-xs font-bold text-text-muted mb-3 uppercase flex items-center gap-1"><i class="pi pi-tags"></i> {{ $t('customers.merge.attributes') }}</label>
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div v-for="key in attributeKeys" :key="key">
              <label class="block text-xs font-bold text-text-muted mb-1.5 uppercase">{{ attributeLabel(key) }}</label>
              <input type="text" v-model="final.attributes[key]"
                     class="w-full h-[40px] bg-background border border-text/20 rounded-xl px-3 text-sm focus:outline-none focus:border-primary font-medium">
              <div v-if="options('attr:' + key).length > 1" class="flex flex-wrap gap-1.5 mt-2">
                <button v-for="opt in options('attr:' + key)" :key="opt" @click="final.attributes[key] = opt" type="button"
                        class="text-[11px] px-2 py-1 rounded-md border font-bold max-w-full truncate transition-colors"
                        :class="final.attributes[key] === opt ? 'bg-primary text-white border-primary' : 'bg-background text-text border-text/20 hover:border-primary/50'">
                  {{ opt }}
                </button>
              </div>
            </div>
          </div>
        </div>

        <div class="rounded-xl border p-4 text-sm" :class="previewError ? 'border-red-500/30 bg-red-500/5 text-red-500' : 'border-primary/20 bg-primary/5 text-text'">
          <div v-if="loadingPreview" class="flex items-center gap-2 text-text-muted"><i class="pi pi-spinner pi-spin"></i> {{ $t('customers.merge.checking') }}</div>
          <div v-else-if="previewError">{{ previewError }}</div>
          <div v-else-if="preview" class="space-y-1">
            <div class="font-bold flex items-center gap-2"><i class="pi pi-calendar text-primary"></i> {{ $t('customers.merge.previewTitle') }}</div>
            <div>{{ $t('customers.merge.previewTotalPrefix') }}<b>{{ preview.totalAppointmentsAfterMerge }}</b>{{ $t('customers.merge.previewTotalSuffix') }}</div>
            <div v-if="preview.movedAppointments > 0">{{ $t('customers.merge.previewMovedPrefix') }}<b>{{ preview.movedAppointments }}</b>{{ $t('customers.merge.previewMovedSuffix') }}</div>
            <div v-if="preview.removedDuplicateAppointments > 0">
              {{ $t('customers.merge.previewDuplicatePrefix') }}<b>{{ preview.removedDuplicateAppointments }}</b>{{ $t('customers.merge.previewDuplicateSuffix') }}
            </div>
          </div>
        </div>
      </div>

      <div class="p-5 border-t border-text/10 bg-background/50 flex justify-between gap-3 shrink-0">
        <button v-if="step === 2" @click="step = 1" :disabled="merging" class="px-5 h-[44px] text-text text-sm font-bold rounded-xl hover:bg-text/10 transition-colors flex items-center gap-2">
          <i class="pi pi-arrow-left"></i> {{ $t('customers.merge.back') }}
        </button>
        <button v-else @click="$emit('close')" class="px-5 h-[44px] text-text text-sm font-bold rounded-xl hover:bg-text/10 transition-colors">{{ $t('common.cancel') }}</button>

        <button v-if="step === 1" @click="goToStep2" :disabled="selectedIds.length < 2"
                class="px-6 h-[44px] bg-primary text-white text-sm font-bold rounded-xl hover:brightness-110 shadow-md transition-transform active:scale-95 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2">
          {{ $t('customers.merge.nextSelected', { count: selectedIds.length }) }} <i class="pi pi-arrow-right"></i>
        </button>
        <button v-else @click="submit" :disabled="!canSubmit"
                class="px-6 h-[44px] bg-primary text-white text-sm font-bold rounded-xl hover:brightness-110 shadow-md transition-transform active:scale-95 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2">
          <i class="pi" :class="merging ? 'pi-spinner pi-spin' : 'pi-check'"></i> {{ $t('customers.merge.submit') }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import bookingApi from '@/services/bookingApi';

const { t } = useI18n();

const props = defineProps({
  customers: { type: Array, required: true },
  companyAttributes: { type: Array, default: () => [] },
  initialCustomerId: { type: [Number, String], default: null }
});
const emit = defineEmits(['close', 'merged']);

const step = ref(1);
const search = ref('');
const selectedIds = ref(props.initialCustomerId != null ? [Number(props.initialCustomerId)] : []);
const merging = ref(false);
const loadingPreview = ref(false);
const preview = ref(null);
const previewError = ref('');

const final = ref({ name: '', phone: '', email: '', notes: '', attributes: {} });

const baseFields = computed(() => [
  { key: 'name', label: t('customers.merge.fields.name'), required: true },
  { key: 'phone', label: t('customers.merge.fields.phone'), type: 'tel' },
  { key: 'email', label: t('customers.merge.fields.email'), type: 'email' },
  { key: 'notes', label: t('customers.merge.fields.notes'), multiline: true }
]);

const normalize = (s) => (s || '').toString().toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');

const filtered = computed(() => {
  const q = normalize(search.value).trim();
  if (!q) return props.customers;
  const digits = q.replace(/\D/g, '');
  return props.customers.filter(c =>
    normalize(c.name).includes(q) ||
    normalize(c.email).includes(q) ||
    (digits && (c.phone || '').replace(/\D/g, '').includes(digits))
  );
});

const selectedCustomers = computed(() =>
  selectedIds.value.map(id => props.customers.find(c => c.id === id)).filter(Boolean)
);

const toggle = (id) => {
  const i = selectedIds.value.indexOf(id);
  if (i >= 0) selectedIds.value.splice(i, 1);
  else selectedIds.value.push(id);
};

// Az "ügyfelenkénti" alternatív értékek (duplikátumok nélkül) a gyors átvételhez
const options = (key) => {
  const values = selectedCustomers.value.map(c => {
    if (key.startsWith('attr:')) return c.attributes?.[key.slice(5)];
    return c[key];
  }).map(v => (v ?? '').toString().trim()).filter(Boolean);
  return [...new Set(values)];
};

const attributeKeys = computed(() => {
  const keys = new Set();
  selectedCustomers.value.forEach(c => Object.keys(c.attributes || {}).forEach(k => { if (k !== 'FormulaList') keys.add(k); }));
  return [...keys];
});

const attributeLabel = (key) => props.companyAttributes.find(a => a.key === key)?.label || key;

// Az összevont ügyfél azonosítója a legrégebbi (legkisebb azonosítójú) ügyfél lesz
const primaryId = computed(() => Math.min(...selectedIds.value));

const buildPayload = () => {
  const cleanAttributes = {};
  for (const [k, v] of Object.entries(final.value.attributes)) {
    if (v !== null && v !== undefined && v.toString().trim() !== '') cleanAttributes[k] = v.toString().trim();
  }
  return {
    primaryCustomerId: primaryId.value,
    mergedCustomerIds: selectedIds.value.filter(id => id !== primaryId.value),
    finalData: {
      fullName: final.value.name.trim(),
      phone: final.value.phone?.trim() || null,
      email: final.value.email?.trim() || null,
      notes: final.value.notes?.trim() || null,
      attributes: cleanAttributes
    }
  };
};

const goToStep2 = async () => {
  if (selectedIds.value.length < 2) return;

  // Alapértelmezett végleges adatok: az első kiválasztott ügyfél értékei, hiányzónál a többiek közül az első kitöltött
  const pick = (key) => options(key)[0] || '';
  const notes = [...new Set(selectedCustomers.value.map(c => (c.notes || '').trim()).filter(Boolean))].join('\n');
  const attrs = {};
  attributeKeys.value.forEach(k => { attrs[k] = options('attr:' + k)[0] || ''; });
  final.value = { name: pick('name'), phone: pick('phone'), email: pick('email'), notes, attributes: attrs };

  step.value = 2;
  preview.value = null;
  previewError.value = '';
  loadingPreview.value = true;
  try {
    const res = await bookingApi.previewMergeCustomers(buildPayload());
    preview.value = res.data;
  } catch (err) {
    const data = err.response?.data;
    const detail = typeof data === 'string' ? data : (data?.message || '');
    const status = err.response?.status;
    previewError.value = t('customers.merge.previewFailed')
      + (status ? (status === 404 && !detail ? t('customers.merge.httpStatusNotFound', { status }) : t('customers.merge.httpStatus', { status })) : t('customers.merge.noConnection'))
      + (detail ? ` ${detail}` : '');
  } finally {
    loadingPreview.value = false;
  }
};

const canSubmit = computed(() =>
  !merging.value && !loadingPreview.value && !previewError.value && final.value.name.trim().length > 0
);

const submit = async () => {
  if (!canSubmit.value) return;
  const names = selectedCustomers.value.map(c => c.name).join(', ');
  if (!confirm(t('customers.merge.confirm', { names }))) return;

  merging.value = true;
  try {
    const res = await bookingApi.mergeCustomers(buildPayload());
    emit('merged', res.data);
  } catch (err) {
    console.error('Hiba az összevonás során:', err);
    const msg = typeof err.response?.data === 'string' ? err.response.data : (err.response?.data?.message || err.message);
    alert(t('customers.merge.failed', { message: msg }));
  } finally {
    merging.value = false;
  }
};
</script>
