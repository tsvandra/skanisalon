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

        <CalendarWeekView v-else-if="currentView === 'week'"
                          :current-week-days="currentWeekDays"
                          :current-week-upcoming-appointments="currentWeekUpcomingAppointments"
                          :available-services="availableServices"
                          :customers-list="customersList"
                          @day-click="onDayClick"
                          @appointment-click="openAppointmentDetails" />

        <CalendarDayView v-else-if="currentView === 'day'"
                         :current-date="currentDate"
                         :timeline-data="timelineData"
                         :current-day-appointments="currentDayAppointments"
                         :available-services="availableServices"
                         :customers-list="customersList"
                         @appointment-click="openAppointmentDetails"
                         @new-appointment="openNewAppointment" />

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

  // Kiszervezett komponensek beimportálása
  import AppointmentEditorModal from './AppointmentEditorModal.vue';
  import CalendarToolbar from './CalendarToolbar.vue';
  import CalendarMonthView from './CalendarMonthView.vue';
  import CalendarWeekView from './CalendarWeekView.vue';
  import CalendarDayView from './CalendarDayView.vue';

  const store = useAppointmentStore();
  const { locale } = useI18n();
  const currentLang = computed(() => locale.value || 'hu-HU');

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
    }
    return '';
  });

  const getDayNameShort = (date) => date.toLocaleString(currentLang.value, { weekday: 'short' });

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

  const isPending = (status) => {
    if (status === 0 || status === '0') return true;
    if (typeof status === 'string' && status.toLowerCase() === 'pending') return true;
    return false;
  };

  // --- DÁTUM OBJEKTUM GENERÁLÁSA (Ide került az új Load Percentage algoritmus) ---
  const createDayObject = (date, isCurrentMonth) => {
    const isToday = date.toDateString() === new Date().toDateString();
    const targetStr = date.toDateString();

    const dayApps = displayAppointments.value
      .filter(a => new Date(a.startDateTime).toDateString() === targetStr)
      .sort((a, b) => new Date(a.startDateTime) - new Date(b.startDateTime));

    // Tényleges terheltség számítása (összeolvasztott időszakokkal)
    let coveredMinutes = 0;
    if (dayApps.length > 0) {
      const intervals = dayApps.map(a => {
        const s = new Date(a.startDateTime);
        const e = new Date(a.endDateTime);
        return [s.getHours() * 60 + s.getMinutes(), e.getHours() * 60 + e.getMinutes()];
      }).sort((a, b) => a[0] - b[0]);

      const merged = [intervals[0]];
      for (let i = 1; i < intervals.length; i++) {
        let last = merged[merged.length - 1];
        let curr = intervals[i];
        if (curr[0] <= last[1]) {
          last[1] = Math.max(last[1], curr[1]); // Ha fedik egymást, összeolvasztjuk
        } else {
          merged.push(curr);
        }
      }
      coveredMinutes = merged.reduce((sum, interval) => sum + (interval[1] - interval[0]), 0);
    }

    // Feltételezünk egy 8 órás (480 perces) munkanapot az arányhoz
    const WORK_DAY_MINUTES = 480;
    let percentage = (coveredMinutes / WORK_DAY_MINUTES) * 100;
    if (percentage > 100) percentage = 100; // Ne folyjon le a kártyáról

    return {
      date, isCurrentMonth, isToday, appointments: dayApps,
      hasAppointments: dayApps.length > 0, appointmentCount: dayApps.length,
      hasPending: dayApps.some(a => isPending(a.status)),
      loadPercentage: percentage
    };
  };

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

  // --- TIMELINE GENERÁLÁS (Ide került az új Gap és Lane algoritmus) ---
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

    // Ütközések kezelése (Sávok/Lanes kiosztása)
    const lanes = [];
    mappedApps.forEach(app => {
      let placed = false;
      for (let i = 0; i < lanes.length; i++) {
        if (lanes[i] <= app.startM) {
          lanes[i] = app.endM;
          app.lane = i;
          placed = true;
          break;
        }
      }
      if (!placed) {
        app.lane = lanes.length;
        lanes.push(app.endM);
      }
    });

    const totalLanes = lanes.length > 0 ? lanes.length : 1;

    mappedApps.forEach(app => {
      app.top = (app.lane / totalLanes) * 100;
      app.height = (1 / totalLanes) * 100;
    });

    // SZABADIDŐKRE (Gaps) vonatkozó összeolvasztó algoritmus
    const gaps = [];
    if (mappedApps.length > 0) {
      const intervals = mappedApps.map(app => [app.startM, app.endM]).sort((a, b) => a[0] - b[0]);

      const merged = [intervals[0]];
      for (let i = 1; i < intervals.length; i++) {
        let last = merged[merged.length - 1];
        let curr = intervals[i];
        if (curr[0] <= last[1]) {
          last[1] = Math.max(last[1], curr[1]);
        } else {
          merged.push(curr);
        }
      }

      for (let i = 0; i < merged.length - 1; i++) {
        const diff = merged[i + 1][0] - merged[i][1];
        if (diff > 0) {
          gaps.push({
            durationMinutes: diff,
            left: ((merged[i][1] - minMinutes) / totalMinutes) * 100,
            width: (diff / totalMinutes) * 100
          });
        }
      }
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
  // ADATOK LEKÉRÉSE A MEGJELENÍTÉSHEZ
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

  // ============================================================================
  // MODAL KEZELÉS (Közvetítő logika)
  // ============================================================================
  const isModalOpen = ref(false);
  const selectedAppointment = ref(null);

  const openNewAppointment = () => {
    selectedAppointment.value = null;
    isModalOpen.value = true;
  };

  const openAppointmentDetails = (app) => {
    selectedAppointment.value = app;
    isModalOpen.value = true;
  };

  const closeModal = () => {
    isModalOpen.value = false;
    selectedAppointment.value = null;
  };

  const handleModalSaved = () => {
    closeModal();
    fetchDataForCurrentView();
    fetchCustomers();
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
