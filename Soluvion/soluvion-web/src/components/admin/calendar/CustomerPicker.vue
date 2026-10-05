<template>
  <div>
    <label class="block text-[10px] md:text-xs font-bold text-text-muted mb-1.5 uppercase flex items-center justify-between">
      <div class="flex items-center gap-1"><i class="pi pi-user"></i> {{ $t('calendar.editor.client') }}</div>
      <button v-if="modelValue && modelValue !== 'new'" @click.stop="goToCustomer" class="text-primary hover:text-primary/70 transition-colors flex items-center gap-1" :title="$t('orders.calendar.customerPicker.goToCustomerCard')">
        <i class="pi pi-external-link"></i> {{ $t('orders.calendar.customerPicker.customerCard') }}
      </button>
    </label>
    <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
      <div ref="rootEl" class="relative transition-all" :class="{'md:col-span-2': modelValue !== 'new'}">
        <input
          type="text"
          :value="inputValue"
          @input="onInput"
          @focus="openList"
          @click="openList"
          @keydown="onKeydown"
          :placeholder="$t('calendar.editor.searchClient')"
          autocomplete="off"
          class="w-full h-[44px] bg-background border border-text/20 rounded-lg pl-3 pr-9 text-sm text-text font-bold focus:outline-none focus:border-primary"
          :class="{'text-primary': modelValue === 'new' && !isOpen}"
        >
        <i class="pi pi-chevron-down absolute right-3 top-1/2 -translate-y-1/2 text-text-muted pointer-events-none text-sm"></i>

        <ul v-if="isOpen" class="absolute z-20 left-0 right-0 mt-1 max-h-60 overflow-y-auto bg-surface border border-text/20 rounded-lg shadow-xl py-1">
          <li
            v-for="(opt, idx) in options"
            :key="opt.id"
            @mousedown.prevent="select(opt)"
            @mousemove="highlight = idx"
            class="px-3 py-2 text-sm cursor-pointer flex justify-between items-center gap-3"
            :class="[
              idx === highlight ? 'bg-primary/10' : '',
              opt.id === 'new' ? 'text-primary font-bold' : 'text-text font-bold'
            ]"
          >
            <span class="truncate">{{ opt.name }}</span>
            <span v-if="opt.phone" class="text-xs text-text-muted font-normal shrink-0">{{ opt.phone }}</span>
          </li>
          <li v-if="filtered.length === 0" class="px-3 py-2 text-sm text-text-muted">{{ $t('calendar.editor.noClientFound') }}</li>
        </ul>
      </div>

      <div v-if="modelValue === 'new'" class="flex flex-col gap-2">
        <input type="text" :value="customerFullName" @input="$emit('update:customerFullName', $event.target.value)" :placeholder="$t('calendar.editor.fullNameOptional')" class="w-full h-[44px] bg-background border border-text/20 rounded-lg px-3 text-sm text-text focus:outline-none focus:border-primary">
        <input type="tel" :value="customerPhone" @input="$emit('update:customerPhone', $event.target.value)" :placeholder="$t('calendar.editor.phoneOptional')" class="w-full h-[44px] bg-background border border-text/20 rounded-lg px-3 text-sm text-text focus:outline-none focus:border-primary">
        <span class="text-[10px] text-text-muted leading-tight">{{ $t('calendar.editor.clientValidationWarning') }}</span>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onBeforeUnmount } from 'vue';
import { useLocalizedRoute } from '@/composables/useLocalizedRoute';
import { useI18n } from 'vue-i18n';

const { go } = useLocalizedRoute();
const { t } = useI18n();

const props = defineProps({
  modelValue: { type: [String, Number], required: true }, // customerId
  customerFullName: { type: String, default: '' },
  customerPhone: { type: String, default: '' },
  customersList: { type: Array, required: true }
});

const emit = defineEmits(['update:modelValue', 'update:customerFullName', 'update:customerPhone']);

const goToCustomer = () => {
  if (props.modelValue && props.modelValue !== 'new') {
    go('customers', { query: { customerId: props.modelValue } });
  }
};

const rootEl = ref(null);
const isOpen = ref(false);
const query = ref('');
const highlight = ref(0);

// Ékezet- és kisbetű-független összehasonlításhoz
const normalize = (s) => (s || '').toString().toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');

const selectedLabel = computed(() => {
  if (props.modelValue === 'new') return t('calendar.editor.addNewClient');
  if (!props.modelValue) return '';
  const c = props.customersList.find(x => x.id.toString() === props.modelValue.toString());
  return c ? c.name : props.customerFullName;
});

// Nyitott listánál a keresőszöveg, zárt állapotban a kiválasztott ügyfél neve látszik
const inputValue = computed(() => (isOpen.value ? query.value : selectedLabel.value));

const filtered = computed(() => {
  const q = normalize(query.value).trim();
  if (!q) return props.customersList;
  const digits = q.replace(/\D/g, '');
  return props.customersList.filter(c =>
    normalize(c.name).includes(q) ||
    (digits && (c.phone || '').replace(/\D/g, '').includes(digits))
  );
});

const options = computed(() => [
  { id: 'new', name: t('calendar.editor.addNewClient') },
  ...filtered.value
]);

const openList = () => {
  if (isOpen.value) return;
  query.value = '';
  highlight.value = 0;
  isOpen.value = true;
};

const closeList = () => { isOpen.value = false; };

const onInput = (e) => {
  query.value = e.target.value;
  // Keresésnél az első valódi találatot emeljük ki (a "+ Új ügyfél" sor a 0. helyen áll)
  highlight.value = query.value.trim() && filtered.value.length ? 1 : 0;
  isOpen.value = true;
};

const select = (opt) => {
  if (opt.id === 'new') {
    emit('update:modelValue', 'new');
    emit('update:customerFullName', '');
    emit('update:customerPhone', '');
  } else {
    emit('update:modelValue', opt.id);
    emit('update:customerFullName', opt.name);
  }
  closeList();
};

const onKeydown = (e) => {
  if (e.key === 'ArrowDown') {
    e.preventDefault();
    if (!isOpen.value) return openList();
    highlight.value = Math.min(highlight.value + 1, options.value.length - 1);
  } else if (e.key === 'ArrowUp') {
    e.preventDefault();
    highlight.value = Math.max(highlight.value - 1, 0);
  } else if (e.key === 'Enter') {
    if (isOpen.value) {
      e.preventDefault();
      const opt = options.value[highlight.value];
      if (opt) select(opt);
    }
  } else if (e.key === 'Escape' || e.key === 'Tab') {
    closeList();
  }
};

const onOutsideClick = (e) => {
  if (isOpen.value && rootEl.value && !rootEl.value.contains(e.target)) closeList();
};

onMounted(() => document.addEventListener('mousedown', onOutsideClick));
onBeforeUnmount(() => document.removeEventListener('mousedown', onOutsideClick));
</script>

