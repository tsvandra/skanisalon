<template>
  <div class="flex flex-col h-full bg-background text-text">

    <CalendarToolbar v-model:current-view="currentView"
                     :pager-text="pagerText"
                     :user-role="store.userRole"
                     @prev="goPrev"
                     @next="goNext"
                     @today="goToday" />

    <div class="flex-1 overflow-y-auto p-2 md:p-6 w-full">
      <div class="max-w-7xl mx-auto w-full">

        <CalendarMonthView v-if="currentView === 'month'"
                           :dynamic-week-days="dynamicWeekDays"
                           :calendar-days="calendarDays"
                           :upcoming-appointments="upcomingAppointments"
                           :available-services="availableServices"
                           :customers-list="customersList"
                           @day-click="onDayClick"
                           @appointment-click="openAppointmentDetails" />

        <div v-else-if="currentView === 'week'" class="space-y-6 md:space-y-8">
          <div class="bg-surface p-2 md:p-5 rounded-2xl border border-text/10 shadow-sm">
            <div class="grid grid-cols-7 gap-1 md:gap-2">
              <div v-for="dayObj in currentWeekDays" :key="dayObj.date.toISOString()"
                   @click="onDayClick(dayObj)"
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
              <div v-for="app in currentWeekUpcomingAppointments" :key="'week-'+app.id"
                   @click="openAppointmentDetails(app)"
                   class="bg-surface rounded-xl p-3 md:p-4 shadow-sm border border-text/10 flex flex-col gap-2 md:gap-3 hover:border-primary/50 cursor-pointer transition-colors relative"
                   :style="{ borderLeftWidth: '5px', borderLeftColor: getCustomerColorDarker(app.customerId) }">

                <div class="flex justify-between items-start">
                  <div class="flex items-center gap-2 md:gap-3">
                    <div class="flex items-center justify-center w-7 h-7 md:w-9 md:h-9 rounded-full font-bold text-white drop-shadow-sm text-[10px] md:text-xs shadow-sm"
                         :style="{ backgroundColor: getCustomerColor(app.customerId) }">
                      {{ getCustomerInitials(app.customerId) }}
                    </div>
                    <div class="flex flex-col">
                      <div class="flex items-center gap-1.5">
                        <h4 class="text-sm md:text-md font-bold text-text">{{ getCustomerName(app.customerId) }}</h4>
                        <div class="w-2 h-2 rounded-full shadow-sm" :class="isPending(app.status) ? 'bg-red-500' : 'bg-green-500'" :title="isPending(app.status) ? 'Függőben' : 'Jóváhagyva'"></div>
                      </div>
                    </div>
                  </div>
                  <div class="text-[10px] md:text-xs bg-background px-2 py-1 rounded text-text-muted font-bold capitalize">
                    {{ getDayNameShort(new Date(app.startDateTime)) }}
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
              {{ $t('calendar.noRemainingWeekAppointments') || 'Nincsenek hátralévő foglalások erre a hétre.' }}
            </div>
          </div>
        </div>

        <div v-else-if="currentView === 'day'" class="space-y-4 md:space-y-8 bg-surface p-3 md:p-8 rounded-2xl md:rounded-3xl border border-text/10 shadow-sm">

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
                   @click="openAppointmentDetails(app)"
                   class="absolute top-0 bottom-0 h-full border-x border-white/50 cursor-pointer transition-transform hover:scale-y-105 flex items-center justify-center overflow-hidden shadow-sm"
                   :style="{ left: app.left + '%', width: app.width + '%', backgroundColor: getCustomerColor(app.customerId) }"
                   :title="`${getCustomerName(app.customerId)}: ${formatTime(app.startDateTime)} - ${formatTime(app.endDateTime)}`">

                <div class="absolute top-1 right-1 w-2 h-2 rounded-full shadow-sm border border-white/50" :class="isPending(app.status) ? 'bg-red-500' : 'bg-green-500'"></div>

                <span v-if="app.width >= 3" class="text-white drop-shadow-sm text-[10px] md:text-xs font-bold truncate px-1">{{ getCustomerInitials(app.customerId) }}</span>
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

            <div v-for="app in currentDayAppointments" :key="'card-'+app.id"
                 @click="openAppointmentDetails(app)"
                 class="bg-background rounded-xl md:rounded-2xl p-3 md:p-6 shadow-sm border border-text/10 flex flex-col gap-3 md:gap-4 hover:border-primary/30 transition-colors cursor-pointer relative"
                 :style="{ borderLeftWidth: '6px', borderLeftColor: getCustomerColorDarker(app.customerId) }">

              <div class="flex justify-between items-start">
                <div class="flex items-center gap-2 md:gap-3">
                  <div class="flex items-center justify-center w-8 h-8 md:w-12 md:h-12 rounded-full font-bold text-white drop-shadow-sm text-xs md:text-lg shadow-sm"
                       :style="{ backgroundColor: getCustomerColor(app.customerId) }">
                    {{ getCustomerInitials(app.customerId) }}
                  </div>
                  <div class="flex flex-col">
                    <div class="flex items-center gap-2">
                      <h4 class="text-base md:text-xl font-bold text-text">{{ getCustomerName(app.customerId) }}</h4>
                      <div class="w-2.5 h-2.5 md:w-3 md:h-3 rounded-full shadow-sm" :class="isPending(app.status) ? 'bg-red-500' : 'bg-green-500'" :title="isPending(app.status) ? 'Függőben' : 'Jóváhagyva'"></div>
                    </div>
                  </div>
                </div>
                <div class="flex items-center gap-2 md:gap-3 text-text-muted text-[10px] md:text-sm font-bold bg-surface px-2 md:px-3 py-1 md:py-1.5 rounded-lg border border-text/5">
                  <div class="flex items-center gap-1"><i class="pi pi-clock text-primary"></i> {{ formatTime(app.startDateTime) }}</div>
                  <span class="text-text/20">|</span>
                  <div class="flex items-center gap-1"><i class="pi pi-hourglass text-primary"></i> {{ getDurationMinutes(app) }} p</div>
                </div>
              </div>

              <div class="flex flex-wrap gap-1.5 md:gap-2 mt-1 md:mt-2 pl-2">
                <div v-for="item in app.items" :key="item.id"
                     class="w-8 h-8 md:w-12 md:h-12 rounded-full bg-primary/10 text-primary flex items-center justify-center text-xs md:text-base font-black border border-primary/20 cursor-help hover:bg-primary hover:text-white transition-colors"
                     :title="getVariantFullName(item.serviceVariantId)">
                  {{ getInitials(getVariantFullName(item.serviceVariantId)) }}
                </div>
              </div>
            </div>

            <div v-if="currentDayAppointments.length === 0" class="text-center py-8 md:py-12 bg-background rounded-xl md:rounded-2xl border border-dashed border-text/10 text-text-muted">
              <p class="font-medium text-sm md:text-base">{{ $t('calendar.noAppointmentsForDay') || 'Nincs foglalás erre a napra.' }}</p>
            </div>

            <button @click="openNewAppointment" class="w-full min-h-[44px] md:min-h-[56px] mt-4 md:mt-6 flex items-center justify-center gap-2 bg-primary text-white rounded-lg md:rounded-xl font-bold text-sm md:text-lg hover:brightness-110 transition-all active:scale-95 shadow-md">
              <i class="pi pi-plus"></i> {{ $t('calendar.addNewAppointment') || 'Új foglalás hozzáadása' }}
            </button>
          </div>
        </div>

      </div>
    </div>

    <AppointmentEditorModal :is-open="isModalOpen"
                            :edit-data="selectedAppointment"
                            :default-date="currentDate"
                            @close="closeModal"
                            @saved="handleModalSaved"
                            @deleted="handleModalDeleted" />

  </div>
</template>

<script setup>
  import { ref, computed, onMounted, watch } from 'vue';
  import { useI18n } from 'vue-i18n';
  import { useAppointmentStore } from '@/stores/appointmentStore';
  import bookingApi from '@/services/bookingApi';
  import { getCustomerColor, getCustomerColorDarker } from '@/utils/colorUtils';

  import AppointmentEditorModal from './AppointmentEditorModal.vue';
  import CalendarToolbar from './CalendarToolbar.vue';
  import CalendarMonthView from './CalendarMonthView.vue'; // ÚJ IMPORT

  const store = useAppointmentStore();
  const { locale } = useI18n();
  const currentLang = computed(() => locale.value || 'hu-HU');

  const isPending = (status) => {
    if (status === 0 || status === '0') return true;
    if (typeof status === 'string' && status.toLowerCase() === 'pending') return true;
    return false;
  };

  // --- ALAP NÉZET ÉS NAPTÁR LOGIKA ---
  const currentView = ref('month');
  const currentDate = ref(new Date());

  const currentMonthName = computed(() => currentDate.value.toLocaleString(currentLang.value, { month: 'long' }).toUpperCase());
  const currentYear = computed(() => currentDate.value.getFullYear());

  const pagerText = computed(() => {
    if (currentView.value === 'month') return `${currentMonthName.value} ${currentYear.value}`;
    if (currentView.value === 'day') {
      const d = currentDate.value;
      const month = d.toLocaleString(currentLang.value, { month: 'long' }).toUpperCase();
      return `${d.getFullYear()}. ${month} ${d.getDate()}.`;
    }
    if (currentView.value === 'week') {
      if (currentWeekDays.value.length === 0) return `${currentMonthName.value} ${currentYear.value}`;
      const start = currentWeekDays.value[0].date; const end = currentWeekDays.value[6].date;
      const startMonth = start.toLocaleString(currentLang.value, { month: 'short' }).toUpperCase();
      const endMonth = end.toLocaleString(currentLang.value, { month: 'short' }).toUpperCase();
      if (start.getMonth() === end.getMonth()) return `${start.getFullYear()}. ${startMonth} ${start.getDate()} - ${end.getDate()}.`;
      return `${start.getFullYear()}. ${startMonth} ${start.getDate()} - ${endMonth} ${end.getDate()}.`;
    } return '';
  });

  const getDayNameShort = (date) => date.toLocaleString(currentLang.value, { weekday: 'short' });
  const getDayNameLong = (date) => date.toLocaleString(currentLang.value, { weekday: 'long' });
  const formatDateLong = (date) => date.toLocaleDateString(currentLang.value, { year: 'numeric', month: 'long', day: 'numeric' });
  const formatTime = (iso) => iso ? new Date(iso).toLocaleTimeString(currentLang.value, { hour: '2-digit', minute: '2-digit' }) : '';
  const formatDurationStr = (m) => m >= 60 ? `${Math.floor(m / 60)}:${(m % 60).toString().padStart(2, '0')}` : `${m}p`;
  const getDurationMinutes = (app) => Math.round((new Date(app.endDateTime) - new Date(app.startDateTime)) / 60000);

  const dynamicWeekDays = computed(() => {
    const days = []; const baseDate = new Date(2024, 0, 1);
    for (let i = 0; i < 7; i++) { const d = new Date(baseDate); d.setDate(d.getDate() + i); days.push(getDayNameShort(d)); }
    return days;
  });

  const goPrev = () => {
    const d = new Date(currentDate.value);
    if (currentView.value === 'month') d.setMonth(d.getMonth() - 1);
    else if (currentView.value === 'week') d.setDate(d.getDate() - 7); else d.setDate(d.getDate() - 1);
    currentDate.value = d;
  };
  const goNext = () => {
    const d = new Date(currentDate.value);
    if (currentView.value === 'month') d.setMonth(d.getMonth() + 1);
    else if (currentView.value === 'week') d.setDate(d.getDate() + 7); else d.setDate(d.getDate() + 1);
    currentDate.value = d;
  };
  const goToday = () => { currentDate.value = new Date(); };

  const displayAppointments = computed(() => store.appointments || []);

  const calendarDays = computed(() => {
    const year = currentDate.value.getFullYear(); const month = currentDate.value.getMonth();
    const firstDay = new Date(year, month, 1); let startingDay = firstDay.getDay() || 7; startingDay--;
    const days = [];
    for (let i = 0; i < startingDay; i++) days.push(createDayObject(new Date(year, month, -startingDay + i + 1), false));
    for (let i = 1; i <= new Date(year, month + 1, 0).getDate(); i++) days.push(createDayObject(new Date(year, month, i), true));
    const remainingCells = 42 - days.length;
    for (let i = 1; i <= remainingCells; i++) days.push(createDayObject(new Date(year, month + 1, i), false));
    return days;
  });

  const currentWeekDays = computed(() => {
    const targetStr = currentDate.value.toDateString(); const idx = calendarDays.value.findIndex(d => d.date.toDateString() === targetStr);
    if (idx === -1) return []; const start = idx - (idx % 7); return calendarDays.value.slice(start, start + 7);
  });

  const createDayObject = (date, isCurrentMonth) => {
    const isToday = date.toDateString() === new Date().toDateString();
    const targetStr = date.toDateString();

    const dayApps = displayAppointments.value
      .filter(a => new Date(a.startDateTime).toDateString() === targetStr)
      .sort((a, b) => new Date(a.startDateTime) - new Date(b.startDateTime));

    return {
      date, isCurrentMonth, isToday, appointments: dayApps,
      hasAppointments: dayApps.length > 0, appointmentCount: dayApps.length,
      hasPending: dayApps.some(a => isPending(a.status)),
      loadPercentage: dayApps.length > 0 ? (dayApps.length * 15) : 0
    };
  };

  const onDayClick = (dayObj) => { currentDate.value = dayObj.date; currentView.value = 'day'; };

  const upcomingAppointments = computed(() => {
    const now = new Date(); now.setHours(0, 0, 0, 0);
    return displayAppointments.value.filter(app => new Date(app.startDateTime) >= now).sort((a, b) => new Date(a.startDateTime) - new Date(b.startDateTime)).slice(0, 5);
  });

  const currentWeekUpcomingAppointments = computed(() => {
    if (currentWeekDays.value.length === 0) return [];
    const now = new Date(); now.setHours(0, 0, 0, 0);
    const weekStart = new Date(currentWeekDays.value[0].date); weekStart.setHours(0, 0, 0, 0);
    const weekEnd = new Date(currentWeekDays.value[6].date); weekEnd.setHours(23, 59, 59, 999);
    const filterStart = now > weekStart ? now : weekStart;
    return displayAppointments.value.filter(app => { const appDate = new Date(app.startDateTime); return appDate >= filterStart && appDate <= weekEnd; }).sort((a, b) => new Date(a.startDateTime) - new Date(b.startDateTime));
  });

  const currentDayAppointments = computed(() => {
    const targetStr = currentDate.value.toDateString();
    return displayAppointments.value.filter(app => new Date(app.startDateTime).toDateString() === targetStr).sort((a, b) => new Date(a.startDateTime) - new Date(b.startDateTime));
  });

  const timelineData = computed(() => {
    const apps = currentDayAppointments.value;
    let minMinutes = 8 * 60; let maxMinutes = 15 * 60;
    if (apps.length > 0) {
      const actualMin = Math.min(...apps.map(a => { const d = new Date(a.startDateTime); return isNaN(d) ? 8 * 60 : d.getHours() * 60 + d.getMinutes(); }));
      const actualMax = Math.max(...apps.map(a => { const d = new Date(a.endDateTime); return isNaN(d) ? 15 * 60 : d.getHours() * 60 + d.getMinutes(); }));
      if (actualMin < minMinutes) minMinutes = Math.floor(actualMin / 60) * 60; if (actualMax > maxMinutes) maxMinutes = Math.ceil(actualMax / 60) * 60;
    }
    if (minMinutes >= maxMinutes) maxMinutes = minMinutes + 60;
    const totalMinutes = maxMinutes - minMinutes;
    const hours = []; for (let m = minMinutes; m <= maxMinutes; m += 60) hours.push({ val: m / 60, left: ((m - minMinutes) / totalMinutes) * 100 });
    const mappedApps = [];
    apps.forEach(app => {
      const sDate = new Date(app.startDateTime); const eDate = new Date(app.endDateTime); if (isNaN(sDate) || isNaN(eDate)) return;
      const startM = sDate.getHours() * 60 + sDate.getMinutes(); const endM = eDate.getHours() * 60 + eDate.getMinutes();
      let width = ((endM - startM) / totalMinutes) * 100; if (width <= 0) width = 2;
      mappedApps.push({ ...app, startM, endM, left: ((startM - minMinutes) / totalMinutes) * 100, width: width });
    });
    mappedApps.sort((a, b) => a.startM - b.startM);
    const gaps = [];
    for (let i = 0; i < mappedApps.length - 1; i++) {
      const diff = mappedApps[i + 1].startM - mappedApps[i].endM;
      if (diff > 0) gaps.push({ durationMinutes: diff, left: ((mappedApps[i].endM - minMinutes) / totalMinutes) * 100, width: (diff / totalMinutes) * 100 });
    }
    return { minMinutes, maxMinutes, totalMinutes, hours, appointments: mappedApps, gaps };
  });

  const fetchDataForCurrentView = () => {
    const d = new Date(currentDate.value);
    const start = new Date(d.getFullYear(), d.getMonth() - 1, 1);
    const end = new Date(d.getFullYear(), d.getMonth() + 2, 0);
    store.fetchAppointments(start, end);
  };

  // ============================================================================
  // ADATOK LEKÉRÉSE A MEGJELENÍTÉSHEZ (A kártyák feliratozásához szükséges)
  // ============================================================================
  const availableServices = ref([]);
  const customersList = ref([]);

  const fetchServicesForAdmin = async () => {
    try {
      const response = await bookingApi.getPublicServices();
      const rawServices = response.data.$values || response.data || [];
      availableServices.value = rawServices.map(s => {
        const vars = s.variants?.$values || s.variants || [];
        return { ...s, variants: vars.filter(v => v.price != null && v.price > 0) };
      }).filter(s => s.variants.length > 0);
    } catch (error) { console.error('Hiba a szolgáltatások betöltésekor:', error); }
  };

  const fetchCustomers = async () => {
    try {
      const response = await bookingApi.getCustomers();
      customersList.value = response.data.$values || response.data || [];
    } catch (error) { console.error('Hiba az ügyfelek betöltésekor:', error); }
  };

  const getLocText = (dict) => dict ? (dict[currentLang.value] || dict['hu'] || '') : '';
  const getInitials = (name) => name ? name.split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase() : '?';

  const getCustomerName = (id) => {
    const c = customersList.value.find(x => x.id === id);
    return c && c.name && c.name !== 'Ismeretlen Vendég' ? c.name : `Vendég #${id}`;
  };

  const getCustomerInitials = (id) => {
    const name = getCustomerName(id);
    if (name.startsWith('Vendég #')) return `#${id}`;
    return getInitials(name);
  };

  const getVariantFullName = (variantId) => {
    for (const s of availableServices.value) {
      const v = s.variants?.find(vx => vx.id === variantId);
      if (v) return `${getLocText(s.name)} - ${getLocText(v.variantName)}`;
    } return 'Ismeretlen';
  };

  // ============================================================================
  // MODAL KEZELÉS (Közvetítő logika)
  // ============================================================================
  const isModalOpen = ref(false);
  const selectedAppointment = ref(null);

  const openNewAppointment = () => {
    selectedAppointment.value = null; // null jelzi az új foglalást
    isModalOpen.value = true;
  };

  const openAppointmentDetails = (app) => {
    selectedAppointment.value = app; // Átadjuk a szerkesztendő adatot
    isModalOpen.value = true;
  };

  const closeModal = () => {
    isModalOpen.value = false;
    selectedAppointment.value = null;
  };

  const handleModalSaved = () => {
    closeModal();
    fetchDataForCurrentView();
    fetchCustomers(); // Hátha új ügyfél lett rögzítve, frissítsük a listát a naptárban is
  };

  const handleModalDeleted = () => {
    closeModal();
    fetchDataForCurrentView();
  };

  onMounted(() => {
    store.initUserPermissions();
    fetchDataForCurrentView();
    fetchServicesForAdmin();
    fetchCustomers();
  });
  watch(currentDate, () => fetchDataForCurrentView());
</script>
