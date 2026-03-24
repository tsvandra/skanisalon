<template>
  <div class="space-y-6 md:space-y-8">

    <div class="max-w-5xl mx-auto bg-surface p-2 md:p-5 rounded-2xl border border-text/10 shadow-sm">
      <div class="grid grid-cols-7 gap-1 md:gap-2">
        <div v-for="day in dynamicWeekDays" :key="day" class="text-center text-[10px] md:text-xs font-bold text-text-muted uppercase mb-1 tracking-wider">
          {{ day }}
        </div>

        <div v-for="(dayObj, index) in calendarDays" :key="index"
             @click="$emit('dayClick', dayObj)"
             class="relative min-h-[50px] md:min-h-[80px] p-1 md:p-2 rounded-lg transition-all cursor-pointer flex flex-col items-center justify-between border"
             :class="[
               dayObj.isCurrentMonth ? 'bg-white border-gray-300 shadow-sm hover:border-primary/60' : 'bg-gray-100 border-transparent opacity-50',
               dayObj.isToday ? 'ring-2 ring-primary ring-offset-1 md:ring-offset-2 ring-offset-surface' : ''
             ]">

          <div v-if="dayObj.isCurrentMonth" class="absolute top-0 left-0 right-0 h-1 bg-gray-200 rounded-t-lg overflow-hidden">
            <div class="h-full bg-primary transition-all" :style="{ width: `${dayObj.loadPercentage}%` }"></div>
          </div>

          <span class="font-black text-xs md:text-base mt-1" :class="dayObj.isToday ? 'text-primary drop-shadow-sm' : 'text-gray-800'">
            {{ dayObj.date.getDate() }}
          </span>

          <div class="flex gap-1 mt-auto pb-0.5 flex-wrap justify-center items-center">
            <span v-for="app in dayObj.appointments.slice(0, 4)" :key="app.id"
                  class="w-2 h-2 md:w-2.5 md:h-2.5 rounded-full shadow-sm"
                  :style="{ backgroundColor: getCustomerColor(app.customerId) }"
                  :title="getCustomerName(app.customerId)"></span>
            <span v-if="dayObj.appointments.length > 4" class="text-[8px] md:text-[9px] text-gray-600 font-black ml-0.5">+{{ dayObj.appointments.length - 4 }}</span>
          </div>
        </div>
      </div>
    </div>

    <div class="max-w-5xl mx-auto mt-6 md:mt-8">
      <h3 class="font-bold text-lg md:text-xl text-text border-b border-text/10 pb-2 mb-3 md:mb-4 flex items-center gap-2">
        <i class="pi pi-forward text-primary"></i> {{ $t('calendar.upcomingAppointments') || 'Következő várható foglalások' }}
      </h3>

      <div v-if="upcomingAppointments.length > 0" class="grid grid-cols-1 md:grid-cols-2 gap-3 md:gap-4">
        <div v-for="app in upcomingAppointments" :key="'upc-'+app.id"
             @click="$emit('appointmentClick', app)"
             class="bg-surface rounded-xl p-3 md:p-4 shadow-sm border border-text/10 flex flex-col gap-2 md:gap-3 hover:border-primary/50 cursor-pointer transition-colors relative"
             :style="{ borderLeftWidth: '5px', borderLeftColor: getCustomerColorDarker(app.customerId) }">

          <div class="flex justify-between items-start">
            <div class="flex items-center gap-2 md:gap-3">
              <div class="flex items-center justify-center w-7 h-7 md:w-9 md:h-9 rounded-full font-bold text-white drop-shadow-sm text-[10px] md:text-xs shadow-sm"
                   :style="{ backgroundColor: getCustomerColor(app.customerId) }">
                {{ getCustomerInitials(app.customerId) }}
              </div>
              <div class="flex flex-col">
                <div class="flex items-center gap-2">
                  <h4 class="text-sm md:text-md font-bold text-text">{{ getCustomerName(app.customerId) }}</h4>
                  <div class="w-2 h-2 rounded-full shadow-sm" :class="isPending(app.status) ? 'bg-red-500' : 'bg-green-500'" :title="isPending(app.status) ? 'Függőben' : 'Jóváhagyva'"></div>
                </div>
              </div>
            </div>
            <div class="text-[10px] md:text-xs bg-background px-2 py-1 rounded text-text-muted font-bold capitalize">
              {{ getDayNameShort(app.startDateTime) }}, {{ formatDateShort(app.startDateTime) }}
            </div>
          </div>

          <div class="flex items-center gap-3 md:gap-4 text-text-muted text-[10px] md:text-xs font-bold pl-1">
            <div class="flex items-center gap-1"><i class="pi pi-clock text-primary"></i> {{ formatTime(app.startDateTime) }}</div>
            <div class="flex items-center gap-1"><i class="pi pi-hourglass text-primary"></i> {{ getDurationMinutes(app) }} p</div>
          </div>

          <div class="flex gap-1 md:gap-1.5 mt-1 pl-1">
            <div v-for="item in app.items" :key="item.id"
                 class="w-6 h-6 md:w-8 md:h-8 rounded-full bg-primary/10 text-primary flex items-center justify-center text-[10px] md:text-xs font-black border border-primary/20 cursor-help"
                 :title="getVariantFullName(item.serviceVariantId)">
              {{ getInitials(getVariantFullName(item.serviceVariantId)) }}
            </div>
          </div>
        </div>
      </div>
      <div v-else class="text-center py-6 md:py-8 bg-surface rounded-xl border border-dashed border-text/10 text-text-muted text-sm md:text-base">
        {{ $t('calendar.noUpcomingAppointments') || 'Nincsenek közelgő foglalások.' }}
      </div>
    </div>

  </div>
</template>

<script setup>
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { getCustomerColor, getCustomerColorDarker } from '@/utils/colorUtils';

const props = defineProps({
  dynamicWeekDays: { type: Array, required: true },
  calendarDays: { type: Array, required: true },
  upcomingAppointments: { type: Array, required: true },
  availableServices: { type: Array, default: () => [] },
  customersList: { type: Array, default: () => [] }
});

defineEmits(['dayClick', 'appointmentClick']);

const { locale } = useI18n();
const currentLang = computed(() => locale.value || 'hu-HU');

// --- Formázó segédfüggvények, amelyek ezen a nézeten belül szükségesek ---
const isPending = (status) => status === 0 || status === '0' || (typeof status === 'string' && status.toLowerCase() === 'pending');
const getDayNameShort = (date) => new Date(date).toLocaleString(currentLang.value, { weekday: 'short' });
const formatDateShort = (iso) => iso ? new Date(iso).toLocaleDateString(currentLang.value, { month: 'short', day: 'numeric' }) : '';
const formatTime = (iso) => iso ? new Date(iso).toLocaleTimeString(currentLang.value, { hour: '2-digit', minute: '2-digit' }) : '';
const getDurationMinutes = (app) => Math.round((new Date(app.endDateTime) - new Date(app.startDateTime)) / 60000);
const getLocText = (dict) => dict ? (dict[currentLang.value] || dict['hu'] || '') : '';
const getInitials = (name) => name ? name.split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase() : '?';

// --- Névkereső funkciók a kapott Props adatok alapján ---
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
