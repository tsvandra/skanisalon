<template>
  <div class="space-y-4 md:space-y-8 bg-surface p-3 md:p-8 rounded-2xl md:rounded-3xl border border-text/10 shadow-sm">

    <div class="flex flex-row items-baseline gap-2 md:gap-3 border-b border-text/10 pb-2 md:pb-4 flex-wrap">
      <h3 class="text-xl md:text-2xl font-black text-primary capitalize">{{ getDayNameLong(currentDate) }}</h3>
      <p class="text-sm md:text-base text-text font-medium">{{ formatDateLong(currentDate) }}</p>
    </div>

    <div class="relative w-full mt-6 md:mt-10 mb-8 md:mb-12 select-none">
      <div v-for="(gap, index) in timelineData.gaps" :key="'gap-'+index"
           class="absolute -top-6 md:-top-7 text-[10px] md:text-sm font-bold text-gray-800 flex justify-center items-center bg-white px-1.5 md:px-3 py-0.5 md:py-1 rounded border border-gray-300 shadow-sm whitespace-nowrap"
           :style="{ left: gap.left + '%', width: gap.width + '%' }">
        {{ formatDurationStr(gap.durationMinutes) }}
      </div>

      <div class="w-full h-8 md:h-12 bg-white rounded-lg md:rounded-xl shadow-inner relative border border-gray-300 overflow-hidden">
        <div v-for="app in timelineData.appointments" :key="'tl-'+app.id"
             @click="$emit('appointmentClick', app)"
             class="absolute border-x border-white/50 cursor-pointer transition-all hover:brightness-110 flex items-center justify-center overflow-hidden shadow-sm"
             :style="{ left: app.left + '%', width: app.width + '%', top: app.top + '%', height: app.height + '%', backgroundColor: getCustomerColor(app.customerId) }"
             :title="`${getCustomerName(app.customerId)}: ${formatTime(app.startDateTime)} - ${formatTime(app.endDateTime)}`">
          <div class="absolute top-0.5 right-0.5 md:top-1 md:right-1 w-1.5 h-1.5 md:w-2 md:h-2 rounded-full shadow-sm border border-white/50" :class="isPending(app.status) ? 'bg-red-500' : 'bg-green-500'"></div>
          <span v-if="app.width >= 3" class="text-white drop-shadow-sm text-[8px] md:text-[10px] font-bold truncate px-1">{{ getCustomerInitials(app.customerId) }}</span>
        </div>
      </div>

      <div v-for="hour in timelineData.hours" :key="'hr-'+hour.val"
           class="absolute top-9 md:top-14 text-[10px] md:text-sm font-bold text-text-muted -translate-x-1/2 flex flex-col items-center"
           :style="{ left: hour.left + '%' }">
        <div class="w-0.5 h-1.5 md:h-2 bg-text/20 mb-0.5 md:mb-1 rounded-full"></div>
        {{ hour.val }}
      </div>
    </div>

    <div class="mt-8 md:mt-16 space-y-3 md:space-y-4">
      <h3 class="font-bold text-lg md:text-xl text-text border-b border-text/10 pb-2 flex items-center gap-2">
        <i class="pi pi-list"></i> {{ $t('calendar.dailyAppointmentsDetails') || 'Napi Foglalások Részletei' }}
      </h3>

      <AppointmentCard v-for="app in currentDayAppointments" :key="'card-'+app.id"
                       :app="app"
                       mode="day"
                       :customers-list="customersList"
                       :available-services="availableServices"
                       @click="$emit('appointmentClick', app)" />

      <div v-if="currentDayAppointments.length === 0" class="text-center py-8 md:py-12 bg-background rounded-xl md:rounded-2xl border border-dashed border-text/10 text-text-muted">
        <p class="font-medium text-sm md:text-base">{{ $t('calendar.noAppointmentsForDay') || 'Nincs foglalás erre a napra.' }}</p>
      </div>

      <button @click="$emit('newAppointment')" class="w-full min-h-[44px] md:min-h-[56px] mt-4 md:mt-6 flex items-center justify-center gap-2 bg-primary text-white rounded-lg md:rounded-xl font-bold text-sm md:text-lg hover:brightness-110 transition-all active:scale-95 shadow-md">
        <i class="pi pi-plus"></i> {{ $t('calendar.addNewAppointment') || 'Új foglalás hozzáadása' }}
      </button>
    </div>
  </div>
</template>

<script setup>
  import { computed } from 'vue';
  import { useI18n } from 'vue-i18n';
  import { getCustomerColor } from '@/utils/colorUtils';
  import AppointmentCard from './AppointmentCard.vue';

  const props = defineProps({
    currentDate: { type: Date, required: true },
    timelineData: { type: Object, required: true },
    currentDayAppointments: { type: Array, required: true },
    availableServices: { type: Array, default: () => [] },
    customersList: { type: Array, default: () => [] }
  });

  defineEmits(['appointmentClick', 'newAppointment']);

  const { locale } = useI18n();
  const currentLang = computed(() => locale.value || 'hu-HU');

  // Formatters for Timeline ONLY
  const isPending = (status) => status === 0 || status === '0' || (typeof status === 'string' && status.toLowerCase() === 'pending');
  const getDayNameLong = (date) => new Date(date).toLocaleString(currentLang.value, { weekday: 'long' });
  const formatDateLong = (date) => new Date(date).toLocaleDateString(currentLang.value, { year: 'numeric', month: 'long', day: 'numeric' });
  const formatTime = (iso) => iso ? new Date(iso).toLocaleTimeString(currentLang.value, { hour: '2-digit', minute: '2-digit' }) : '';
  const formatDurationStr = (m) => m >= 60 ? `${Math.floor(m / 60)}:${(m % 60).toString().padStart(2, '0')}` : `${m}p`;
  const getInitials = (name) => name ? name.split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase() : '?';

  const getCustomerName = (id) => {
    const c = props.customersList.find(x => x.id === id);
    return c && c.name && c.name !== 'Ismeretlen Vendég' ? c.name : `Vendég #${id}`;
  };

  const getCustomerInitials = (id) => {
    const name = getCustomerName(id);
    if (name.startsWith('Vendég #')) return `#${id}`;
    return getInitials(name);
  };
</script>
