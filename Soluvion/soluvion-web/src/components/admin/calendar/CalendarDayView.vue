<template>
  <div class="space-y-4 md:space-y-8 bg-surface p-3 md:p-8 rounded-2xl md:rounded-3xl border border-text/10 shadow-sm">

    <div class="flex flex-row items-baseline justify-between gap-2 md:gap-3 border-b border-text/10 pb-2 md:pb-4 flex-wrap">
      <div class="flex flex-row items-baseline gap-2 md:gap-3 flex-wrap">
        <h3 class="text-xl md:text-2xl font-black text-primary capitalize">{{ getDayNameLong(currentDate) }}</h3>
        <p class="text-sm md:text-base text-text font-medium">{{ formatDateLong(currentDate) }}</p>
      </div>
    </div>

    <!-- ONLINE FOGLALHATÓ IDŐSÁV KEZELŐ SÁV EZEN A NAPON -->
    <div class="p-3.5 md:p-4 rounded-2xl border transition-all"
         :class="dayScheduleInfo?.isOpen
           ? 'bg-emerald-500/5 border-emerald-500/25'
           : 'bg-background/70 border-text/10'">
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div class="flex items-center gap-2.5 flex-wrap">
          <i class="pi text-lg"
             :class="dayScheduleInfo?.isOpen ? 'pi-check-circle text-emerald-500' : 'pi-lock text-text-muted'"></i>
          <div class="flex flex-col sm:flex-row sm:items-center gap-1 sm:gap-2">
            <span class="text-xs md:text-sm font-bold text-text">
              {{ dayScheduleInfo?.isOpen ? $t('calendar.schedule.dayStatusOpen') : $t('calendar.schedule.dayStatusClosed') }}
            </span>
            <span v-if="dayScheduleInfo?.isOpen" class="text-sm md:text-base font-black text-emerald-600">
              {{ dayScheduleInfo.intervals.map(i => `${i.start} – ${i.end}`).join(', ') }}
            </span>
            <span v-if="dayScheduleInfo?.hasCustomOverride"
                  class="px-2 py-0.5 rounded-full text-[10px] font-bold uppercase tracking-wider bg-primary/15 text-primary border border-primary/30 w-fit">
              {{ $t('calendar.schedule.customBadge') }}
            </span>
          </div>
        </div>

        <div class="flex flex-wrap items-center gap-2">
          <button type="button"
                  @click="toggleEditor"
                  class="px-3 py-1.5 rounded-xl text-xs font-bold bg-primary text-surface hover:brightness-110 transition-all cursor-pointer flex items-center gap-1.5 shadow-sm">
            <i class="pi" :class="isEditorOpen ? 'pi-chevron-up' : 'pi-clock'"></i>
            <span>{{ $t('calendar.schedule.editDayHours') }}</span>
          </button>

          <button v-if="dayScheduleInfo?.isOpen"
                  type="button"
                  @click="closeDayForBooking"
                  :disabled="isSavingSchedule"
                  class="px-3 py-1.5 rounded-xl text-xs font-bold border border-red-500/30 text-red-500 hover:bg-red-500/10 transition-all cursor-pointer">
            <i class="pi pi-ban mr-1"></i>{{ $t('calendar.schedule.closeDay') }}
          </button>

          <button v-if="dayScheduleInfo?.hasCustomOverride"
                  type="button"
                  @click="resetDayOverride"
                  :disabled="isSavingSchedule"
                  class="px-3 py-1.5 rounded-xl text-xs font-bold border border-text/15 text-text-muted hover:text-text hover:bg-text/5 transition-all cursor-pointer">
            <i class="pi pi-refresh mr-1"></i>{{ $t('calendar.schedule.resetToDefault') }}
          </button>
        </div>
      </div>

      <!-- Lenyíló gyors idősáv szerkesztő -->
      <div v-if="isEditorOpen" class="mt-4 pt-4 border-t border-text/10 space-y-3">
        <!-- Gyors sablon gombok -->
        <div class="flex flex-wrap items-center gap-1.5">
          <span class="text-[11px] font-bold text-text-muted mr-1">{{ $t('calendar.schedule.quickPresets') }}</span>
          <button v-for="preset in quickPresets"
                  :key="preset.label"
                  type="button"
                  @click="applyPreset(preset)"
                  class="px-2.5 py-1 rounded-lg text-xs font-bold bg-surface border border-text/15 text-text hover:border-primary hover:text-primary transition-colors cursor-pointer">
            {{ preset.label }}
          </button>
        </div>

        <!-- Egyedi idősávok (24 órás legördülők) -->
        <div class="flex flex-wrap items-center gap-2">
          <div v-for="(iv, idx) in editIntervals"
               :key="idx"
               class="flex items-center gap-1.5 bg-surface px-3 py-1.5 rounded-xl border border-primary/30 shadow-sm">
            <i class="pi pi-clock text-primary text-xs"></i>
            <select v-model="iv.start"
                    class="bg-transparent text-xs md:text-sm font-bold text-text focus:outline-none cursor-pointer">
              <option v-for="tOpt in timeOptions" :key="'ds-'+tOpt" :value="tOpt" class="bg-surface text-text">{{ tOpt }}</option>
            </select>
            <span class="text-text-muted font-bold">–</span>
            <select v-model="iv.end"
                    class="bg-transparent text-xs md:text-sm font-bold text-text focus:outline-none cursor-pointer">
              <option v-for="tOpt in timeOptions" :key="'de-'+tOpt" :value="tOpt" class="bg-surface text-text">{{ tOpt }}</option>
            </select>
            <button v-if="editIntervals.length > 1"
                    type="button"
                    @click="editIntervals.splice(idx, 1)"
                    class="text-red-500 hover:bg-red-500/10 w-6 h-6 rounded flex items-center justify-center cursor-pointer ml-1">
              <i class="pi pi-trash text-xs"></i>
            </button>
          </div>

          <button v-if="editIntervals.length < 3"
                  type="button"
                  @click="editIntervals.push({ start: '14:00', end: '18:00' })"
                  class="px-3 py-1.5 rounded-xl text-xs font-bold text-primary border border-dashed border-primary/40 hover:bg-primary/10 transition-colors cursor-pointer">
            {{ $t('calendar.schedule.addInterval') }}
          </button>

          <button type="button"
                  @click="saveDayOpenHours"
                  :disabled="isSavingSchedule"
                  class="ml-auto px-4 py-2 rounded-xl text-xs md:text-sm font-bold bg-emerald-600 text-white hover:brightness-110 transition-all shadow-sm flex items-center gap-1.5 cursor-pointer disabled:opacity-50">
            <i class="pi" :class="isSavingSchedule ? 'pi-spin pi-spinner' : 'pi-check'"></i>
            <span>{{ $t('calendar.schedule.saveDayHours') }}</span>
          </button>
        </div>
      </div>
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
          <div class="absolute top-0.5 right-0.5 md:top-1 md:right-1 w-1.5 h-1.5 md:w-2 md:h-2 rounded-full shadow-sm border border-white/50" :class="isNoShow(app.status) ? 'bg-gray-400' : (isPending(app.status) ? 'bg-red-500' : 'bg-green-500')"></div>
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
        <i class="pi pi-list"></i> {{ $t('calendar.dailyAppointmentsDetails') }}
      </h3>

      <AppointmentCard v-for="app in currentDayAppointments" :key="'card-'+app.id"
                       :app="app"
                       mode="day"
                       :customers-list="customersList"
                       :available-services="availableServices"
                       @click="$emit('appointmentClick', app)" />

      <div v-if="currentDayAppointments.length === 0" class="text-center py-8 md:py-12 bg-background rounded-xl md:rounded-2xl border border-dashed border-text/10 text-text-muted">
        <p class="font-medium text-sm md:text-base">{{ $t('calendar.noAppointmentsForDay') }}</p>
      </div>

      <button @click="$emit('newAppointment')" class="w-full min-h-[44px] md:min-h-[56px] mt-4 md:mt-6 flex items-center justify-center gap-2 bg-primary text-white rounded-lg md:rounded-xl font-bold text-sm md:text-lg hover:brightness-110 transition-all active:scale-95 shadow-md">
        <i class="pi pi-plus"></i> {{ $t('calendar.addNewAppointment') }}
      </button>
    </div>
  </div>
</template>

<script setup>
  import { ref, computed, watch } from 'vue';
  import { useI18n } from 'vue-i18n';
  import { getCustomerColor } from '@/utils/colorUtils';
  import AppointmentCard from './AppointmentCard.vue';

  const props = defineProps({
    currentDate: { type: Date, required: true },
    timelineData: { type: Object, required: true },
    currentDayAppointments: { type: Array, required: true },
    availableServices: { type: Array, default: () => [] },
    customersList: { type: Array, default: () => [] },
    dayScheduleInfo: { type: Object, default: () => ({ isOpen: false, hasCustomOverride: false, intervals: [] }) },
    isSavingSchedule: { type: Boolean, default: false }
  });

  const emit = defineEmits(['appointmentClick', 'newAppointment', 'updateDaySchedule']);

  const { locale, t } = useI18n();
  const currentLang = computed(() => locale.value || 'hu-HU');

  const isEditorOpen = ref(false);
  const editIntervals = ref([{ start: '08:00', end: '17:00' }]);

  const quickPresets = [
    { label: '08:00 – 16:00', intervals: [{ start: '08:00', end: '16:00' }] },
    { label: '08:00 – 18:00', intervals: [{ start: '08:00', end: '18:00' }] },
    { label: '08:00 – 12:00', intervals: [{ start: '08:00', end: '12:00' }] },
    { label: '13:00 – 18:00', intervals: [{ start: '13:00', end: '18:00' }] },
    { label: '16:00 – 18:00', intervals: [{ start: '16:00', end: '18:00' }] }
  ];

  const timeOptions = computed(() => {
    const list = [];
    for (let h = 6; h <= 22; h++) {
      for (let m = 0; m < 60; m += 15) {
        if (h === 22 && m > 0) break;
        list.push(`${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`);
      }
    }
    return list;
  });

  const syncEditorFromProps = () => {
    if (props.dayScheduleInfo?.intervals?.length > 0) {
      editIntervals.value = props.dayScheduleInfo.intervals.map(i => ({ start: i.start, end: i.end }));
    } else {
      editIntervals.value = [{ start: '08:00', end: '17:00' }];
    }
  };

  watch(() => props.currentDate, () => {
    syncEditorFromProps();
  });

  const toggleEditor = () => {
    if (!isEditorOpen.value) {
      syncEditorFromProps();
    }
    isEditorOpen.value = !isEditorOpen.value;
  };

  const applyPreset = (preset) => {
    editIntervals.value = preset.intervals.map(i => ({ ...i }));
  };

  const saveDayOpenHours = () => {
    emit('updateDaySchedule', {
      clearOverride: false,
      isOpen: true,
      intervals: editIntervals.value
    });
    isEditorOpen.value = false;
  };

  const closeDayForBooking = () => {
    emit('updateDaySchedule', {
      clearOverride: false,
      isOpen: false,
      intervals: []
    });
    isEditorOpen.value = false;
  };

  const resetDayOverride = () => {
    emit('updateDaySchedule', {
      clearOverride: true,
      isOpen: false,
      intervals: []
    });
    isEditorOpen.value = false;
  };

  // Formatters for Timeline ONLY
  const isPending = (status) => status === 0 || status === '0' || (typeof status === 'string' && status.toLowerCase() === 'pending');
  const isNoShow = (status) => status === 4 || status === '4' || (typeof status === 'string' && status.toLowerCase() === 'noshow');
  const getDayNameLong = (date) => new Date(date).toLocaleString(currentLang.value, { weekday: 'long' });
  const formatDateLong = (date) => new Date(date).toLocaleDateString(currentLang.value, { year: 'numeric', month: 'long', day: 'numeric' });
  const formatTime = (iso) => iso ? new Date(iso).toLocaleTimeString(currentLang.value, { hour: '2-digit', minute: '2-digit' }) : '';
  const formatDurationStr = (m) => m >= 60 ? `${Math.floor(m / 60)}:${(m % 60).toString().padStart(2, '0')}` : `${m}${t('orders.calendar.card.minutesShort')}`;
  const getInitials = (name) => name ? name.split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase() : '?';

  const guestPrefix = () => `${t('calendar.guest')} #`;
  const getCustomerName = (id) => {
    const c = props.customersList.find(x => x.id === id);
    return c && c.name && c.name !== 'Ismeretlen Vendég' ? c.name : `${guestPrefix()}${id}`;
  };

  const getCustomerInitials = (id) => {
    const name = getCustomerName(id);
    if (name.startsWith(guestPrefix())) return `#${id}`;
    return getInitials(name);
  };
</script>
