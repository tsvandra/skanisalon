<template>
  <div class="space-y-6 md:space-y-8">
    <div class="bg-surface p-2 md:p-5 rounded-2xl border border-text/10 shadow-sm">
      <div class="grid grid-cols-7 gap-1 md:gap-2">
        <div v-for="dayObj in currentWeekDays" :key="dayObj.date.toISOString()"
             @click="$emit('dayClick', dayObj)"
             class="relative min-h-[70px] md:min-h-[100px] p-1.5 md:p-2 rounded-xl border border-gray-300 bg-white hover:border-primary/50 cursor-pointer flex flex-col items-center justify-center transition-all shadow-sm"
             :class="{ 'ring-2 ring-primary ring-offset-1 md:ring-offset-2 ring-offset-surface': dayObj.isToday }">
          <div class="absolute top-0 left-0 right-0 h-1 bg-gray-200 rounded-t-xl overflow-hidden">
            <div class="h-full bg-primary" :style="{ width: `${dayObj.loadPercentage}%` }"></div>
          </div>
          <span class="text-[10px] md:text-xs text-gray-500 font-bold mt-1 uppercase">{{ getDayNameShort(dayObj.date) }}</span>
          <span class="font-black text-lg md:text-2xl" :class="dayObj.isToday ? 'text-primary' : 'text-gray-800'">{{ dayObj.date.getDate() }}</span>

          <div class="flex gap-1 mt-1 md:mt-2">
            <span v-for="app in dayObj.appointments.slice(0, 3)" :key="app.id"
                  class="w-2 h-2 md:w-2.5 md:h-2.5 rounded-full shadow-sm"
                  :style="{ backgroundColor: getCustomerColor(app.customerId) }"
                  :title="getCustomerName(app.customerId)"></span>
          </div>
        </div>
      </div>
    </div>

    <div class="max-w-5xl mx-auto mt-6 md:mt-8">
      <h3 class="font-bold text-lg md:text-xl text-text border-b border-text/10 pb-2 mb-3 md:mb-4 flex items-center gap-2">
        <i class="pi pi-calendar-clock text-primary"></i> {{ $t('calendar.remainingWeekAppointments') || 'A hét hátralévő foglalásai' }}
      </h3>

      <div v-if="currentWeekUpcomingAppointments.length > 0" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3 md:gap-4">

        <AppointmentCard v-for="app in currentWeekUpcomingAppointments" :key="'week-'+app.id"
                         :app="app"
                         mode="week"
                         :customers-list="customersList"
                         :available-services="availableServices"
                         @click="$emit('appointmentClick', app)" />

      </div>

      <div v-else class="text-center py-6 md:py-8 bg-surface rounded-xl border border-dashed border-text/10 text-text-muted text-sm md:text-base">
        {{ $t('calendar.noRemainingWeekAppointments') || 'Nincsenek hátralévő foglalások erre a hétre.' }}
      </div>
    </div>
  </div>
</template>

<script setup>
  import { computed } from 'vue';
  import { useI18n } from 'vue-i18n';
  import { getCustomerColor } from '@/utils/colorUtils';
  import AppointmentCard from './AppointmentCard.vue';

  const props = defineProps({
    currentWeekDays: { type: Array, required: true },
    currentWeekUpcomingAppointments: { type: Array, required: true },
    availableServices: { type: Array, default: () => [] },
    customersList: { type: Array, default: () => [] }
  });

  defineEmits(['dayClick', 'appointmentClick']);

  const { locale } = useI18n();
  const currentLang = computed(() => locale.value || 'hu-HU');

  // Formatters for Grid ONLY
  const getDayNameShort = (date) => new Date(date).toLocaleString(currentLang.value, { weekday: 'short' });

  const getCustomerName = (id) => {
    const c = props.customersList.find(x => x.id === id);
    return c && c.name && c.name !== 'Ismeretlen Vendég' ? c.name : `Vendég #${id}`;
  };
</script>
