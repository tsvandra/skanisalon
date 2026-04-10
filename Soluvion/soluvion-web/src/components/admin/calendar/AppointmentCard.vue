<template>
  <div @click="$emit('click', app)"
       class="bg-surface shadow-sm border border-text/10 flex flex-col hover:border-primary/50 cursor-pointer transition-colors relative"
       :class="mode === 'day' ? 'rounded-xl md:rounded-2xl p-3 md:p-6 gap-3' : 'rounded-xl p-3 md:p-4 gap-2'"
       :style="{ borderLeftWidth: mode === 'day' ? '6px' : '5px', borderLeftColor: getCustomerColorDarker(app.customerId) }">

    <div class="flex justify-between items-start" :class="{'mb-1': mode !== 'day'}">
      <div class="flex items-center gap-2 md:gap-3">
        <div class="flex items-center justify-center rounded-full font-bold text-white drop-shadow-sm shadow-sm"
             :class="mode === 'day' ? 'w-8 h-8 md:w-12 md:h-12 text-xs md:text-lg' : 'w-7 h-7 md:w-9 md:h-9 text-[10px] md:text-xs'"
             :style="{ backgroundColor: getCustomerColor(app.customerId) }">
          {{ getCustomerInitials(app.customerId) }}
        </div>
        <div class="flex flex-col">
          <div class="flex items-center gap-1.5 md:gap-2">
            <h4 class="font-bold text-text" :class="mode === 'day' ? 'text-base md:text-xl' : 'text-sm md:text-md'">
              {{ getCustomerName(app.customerId) }}
            </h4>
            <div class="rounded-full shadow-sm"
                 :class="[isPending(app.status) ? 'bg-red-500' : 'bg-green-500', mode === 'day' ? 'w-2.5 h-2.5 md:w-3 md:h-3' : 'w-2 h-2']"
                 :title="isPending(app.status) ? 'Függőben' : 'Jóváhagyva'"></div>
          </div>
        </div>
      </div>

      <div v-if="mode === 'day'" class="flex flex-wrap items-center justify-end gap-2 md:gap-3 text-text-muted text-[10px] md:text-sm font-bold bg-surface px-2 md:px-3 py-1 md:py-1.5 rounded-lg border border-text/5">
        <div class="flex items-center gap-1"><i class="pi pi-clock text-primary"></i> {{ formatTime(app.startDateTime) }}</div>
        <span class="text-text/20">|</span>
        <div class="flex items-center gap-1"><i class="pi pi-hourglass text-primary"></i> {{ getDurationMinutes(app) }} p</div>
        <span class="text-text/20">|</span>
        <div class="flex items-center gap-1 text-primary"><i class="pi pi-tag"></i> {{ formatPrice(app) }} EUR</div>
      </div>

      <div v-else class="text-[10px] md:text-xs bg-background px-2 py-1 rounded text-text-muted font-bold capitalize whitespace-nowrap">
        {{ mode === 'month' ? `${getDayNameShort(app.startDateTime)}, ${formatDateShort(app.startDateTime)}` : getDayNameShort(app.startDateTime) }}
      </div>
    </div>

    <div v-if="mode !== 'day'" class="flex items-center flex-wrap gap-x-3 gap-y-1 md:gap-x-4 text-text-muted text-[10px] md:text-xs font-bold pl-1">
      <div class="flex items-center gap-1"><i class="pi pi-clock text-primary"></i> {{ formatTime(app.startDateTime) }}</div>
      <div class="flex items-center gap-1"><i class="pi pi-hourglass text-primary"></i> {{ getDurationMinutes(app) }} p</div>
      <div class="flex items-center gap-1 text-primary"><i class="pi pi-tag"></i> {{ formatPrice(app) }} EUR</div>
    </div>

    <div :class="mode === 'day' ? 'pl-2' : 'pl-1'">

      <div v-if="app.notes" class="text-text-muted italic truncate cursor-help"
           :class="mode === 'day' ? 'text-xs md:text-sm w-full max-w-[85%] mb-2' : 'text-[10px] md:text-xs w-full mb-1.5'"
           :title="app.notes">
        <i class="pi pi-comment mr-1 text-primary/70" :class="mode === 'day' ? 'text-[10px]' : 'text-[8px]'"></i> {{ app.notes }}
      </div>

      <div class="flex gap-1 md:gap-1.5" :class="{'flex-wrap': mode === 'day'}">
        <div v-for="item in app.items" :key="item.id"
             class="rounded-full bg-primary/10 text-primary flex items-center justify-center font-black border border-primary/20 cursor-help transition-colors"
             :class="[mode === 'day' ? 'w-8 h-8 md:w-12 md:h-12 text-xs md:text-base hover:bg-primary hover:text-white' : 'w-6 h-6 md:w-8 md:h-8 text-[10px] md:text-xs']"
             :title="getVariantFullName(item.serviceVariantId)">
          {{ getInitials(getVariantFullName(item.serviceVariantId)) }}
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { getCustomerColor, getCustomerColorDarker } from '@/utils/colorUtils';

const props = defineProps({
  app: { type: Object, required: true },
  mode: { type: String, default: 'day' }, // Lehet 'day', 'week', vagy 'month'
  availableServices: { type: Array, default: () => [] },
  customersList: { type: Array, default: () => [] }
});

defineEmits(['click']);

const { locale, t } = useI18n();
const currentLang = computed(() => locale.value || 'hu-HU');

// Formázók
const isPending = (status) => status === 0 || status === '0' || (typeof status === 'string' && status.toLowerCase() === 'pending');
const getDayNameShort = (date) => new Date(date).toLocaleString(currentLang.value, { weekday: 'short' });
const formatDateShort = (iso) => iso ? new Date(iso).toLocaleDateString(currentLang.value, { month: 'short', day: 'numeric' }) : '';
const formatTime = (iso) => iso ? new Date(iso).toLocaleTimeString(currentLang.value, { hour: '2-digit', minute: '2-digit' }) : '';
const getDurationMinutes = (app) => Math.round((new Date(app.endDateTime) - new Date(app.startDateTime)) / 60000);
const getLocText = (dict) => dict ? (dict[currentLang.value] || dict['hu'] || '') : '';
const getInitials = (name) => name ? name.split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase() : '?';

const formatPrice = (app) => {
  const total = app.items?.reduce((sum, item) => sum + (item.price || 0), 0) || 0;
  return total % 1 === 0 ? total.toString() : total.toFixed(2);
};

// Névkeresők
const getCustomerName = (id) => {
  const c = props.customersList.find(x => x.id === id);
  return c && c.name && c.name !== 'Ismeretlen Vendég' ? c.name : `Vendég #${id}`;
};

const getCustomerInitials = (id) => {
  const name = getCustomerName(id);
  if (name.startsWith('Vendég #')) return `#${id}`;
  return getInitials(name);
};

const getVariantFullName = (variantId) => {
  for (const s of props.availableServices) {
    const v = s.variants?.find(vx => vx.id === variantId);
    if (v) return `${getLocText(s.name)} - ${getLocText(v.variantName)}`;
  }
  return 'Ismeretlen';
};
</script>
