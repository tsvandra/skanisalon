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

        <AppointmentCard v-for="app in upcomingAppointments" :key="'upc-'+app.id"
                         :app="app"
                         mode="month"
                         :customers-list="customersList"
                         :available-services="availableServices"
                         @click="$emit('appointmentClick', app)" />

      </div>
      <div v-else class="text-center py-6 md:py-8 bg-surface rounded-xl border border-dashed border-text/10 text-text-muted text-sm md:text-base">
        {{ $t('calendar.noUpcomingAppointments') || 'Nincsenek közelgő foglalások.' }}
      </div>
    </div>

  </div>
</template>

<script setup>
  import { computed } from 'vue';
  import { getCustomerColor } from '@/utils/colorUtils';
  import AppointmentCard from './AppointmentCard.vue';

  const props = defineProps({
    dynamicWeekDays: { type: Array, required: true },
    calendarDays: { type: Array, required: true },
    upcomingAppointments: { type: Array, required: true },
    availableServices: { type: Array, default: () => [] },
    customersList: { type: Array, default: () => [] }
  });

  defineEmits(['dayClick', 'appointmentClick']);

  // Formatters for Grid ONLY
  const getCustomerName = (id) => {
    const c = props.customersList.find(x => x.id === id);
    return c && c.name && c.name !== 'Ismeretlen Vendég' ? c.name : `Vendég #${id}`;
  };
</script>
