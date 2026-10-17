<template>
  <div class="flex flex-col h-full bg-background text-text">

    <CalendarToolbar v-model:current-view="currentView"
                     :pager-text="pagerText"
                     :user-role="store.userRole"
                     :is-today="isCurrentToday"
                     :schedule-mode="scheduleConfig.mode"
                     @prev="goPrev"
                     @next="goNext"
                     @today="goToday"
                     @open-schedule="isScheduleModalOpen = true" />

    <div class="flex-1 overflow-y-auto p-2 md:p-6 w-full">
      <div class="max-w-7xl mx-auto w-full space-y-4">

        <!-- JÓVÁHAGYÁSRA VÁRÓ ONLINE FOGLALÁSOK SÁV / PANEL -->
        <div v-if="pendingBookings.length > 0"
             class="rounded-2xl border-2 border-amber-500/40 bg-amber-500/5 shadow-sm overflow-hidden transition-all">
          <div class="p-3 md:p-4 bg-amber-500/10 flex items-center justify-between gap-3 cursor-pointer select-none"
               @click="isPendingInboxOpen = !isPendingInboxOpen">
            <div class="flex items-center gap-2.5 md:gap-3">
              <div class="w-9 h-9 md:w-10 md:h-10 rounded-xl bg-amber-500 text-white flex items-center justify-center font-extrabold shadow-xs shrink-0">
                <i class="pi pi-bell text-base md:text-lg"></i>
              </div>
              <div>
                <div class="flex items-center flex-wrap gap-2">
                  <h3 class="text-sm md:text-base font-extrabold text-text">
                    {{ $t('calendar.pendingInbox.title') }}
                  </h3>
                  <span class="px-2 py-0.5 rounded-full bg-amber-500 text-white text-xs font-extrabold shadow-2xs">
                    {{ pendingBookings.length }}
                  </span>
                </div>
                <p class="text-[11px] md:text-xs text-text-muted hidden sm:block">
                  {{ $t('calendar.pendingInbox.subtitle') }}
                </p>
              </div>
            </div>

            <button type="button"
                    class="px-3 py-1.5 rounded-xl bg-surface/80 border border-amber-500/30 text-xs font-bold text-text flex items-center gap-1.5 hover:bg-surface transition-colors cursor-pointer shrink-0">
              <span class="hidden sm:inline">{{ isPendingInboxOpen ? $t('calendar.pendingInbox.hideList') : $t('calendar.pendingInbox.showList') }}</span>
              <i class="pi text-xs" :class="isPendingInboxOpen ? 'pi-chevron-up' : 'pi-chevron-down'"></i>
            </button>
          </div>

          <div v-if="isPendingInboxOpen" class="p-3 md:p-4 space-y-2.5 border-t border-amber-500/20">
            <div v-for="app in pendingBookings"
                 :key="'pending-' + app.id"
                 class="p-3 md:p-4 rounded-xl bg-surface border border-text/10 hover:border-amber-500/40 shadow-2xs flex flex-col lg:flex-row lg:items-center justify-between gap-3 transition-all">
              <div class="flex flex-col gap-1.5 min-w-0">
                <div class="flex items-center flex-wrap gap-2">
                  <span class="font-extrabold text-sm md:text-base text-text">
                    {{ getPendingCustomerName(app) }}
                  </span>
                  <a v-if="getPendingCustomerPhone(app)"
                     :href="'tel:' + getPendingCustomerPhone(app)"
                     @click.stop
                     class="inline-flex items-center gap-1 px-2 py-0.5 rounded-lg bg-primary/10 text-primary text-xs font-bold hover:bg-primary/20 transition-colors">
                    <i class="pi pi-phone text-[10px]"></i>
                    <span>{{ getPendingCustomerPhone(app) }}</span>
                  </a>
                  <span v-if="app.createdAt"
                        class="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-lg bg-amber-500/15 text-amber-600 border border-amber-500/30 text-[11px] font-bold">
                    <i class="pi pi-history text-[10px]"></i>
                    <span>{{ $t('calendar.pendingInbox.submittedAt', { time: formatPendingDateTime(app.createdAt) }) }}</span>
                  </span>
                </div>

                <div class="flex items-center flex-wrap gap-x-3 gap-y-1 text-xs md:text-sm font-bold text-text">
                  <span class="text-text-muted font-semibold">{{ $t('calendar.pendingInbox.requestedSlot') }}</span>
                  <span class="inline-flex items-center gap-1 text-primary cursor-pointer hover:underline"
                        @click="jumpToPendingDay(app)">
                    <i class="pi pi-calendar"></i>
                    <span>{{ formatPendingSlot(app.startDateTime, app.endDateTime) }}</span>
                  </span>
                  <span class="text-text/20">|</span>
                  <span class="text-text-muted">{{ getPendingServicesLabel(app) }}</span>
                  <span class="text-text/20">|</span>
                  <span class="font-extrabold text-primary">{{ app.totalPrice }} EUR</span>
                </div>

                <div v-if="app.notes" class="text-xs text-text-muted italic bg-background/70 px-2.5 py-1.5 rounded-lg border border-text/5">
                  <i class="pi pi-comment text-primary/70 mr-1 text-[10px]"></i>
                  <span>{{ app.notes }}</span>
                </div>
              </div>

              <div class="flex items-center flex-wrap gap-2 shrink-0 pt-2 lg:pt-0 border-t lg:border-t-0 border-text/10">
                <button type="button"
                        @click="approvePendingBooking(app)"
                        :disabled="processingPendingId === app.id"
                        class="px-3.5 py-2 rounded-xl bg-emerald-600 text-white text-xs font-extrabold hover:bg-emerald-500 transition-all shadow-xs flex items-center gap-1.5 cursor-pointer disabled:opacity-50">
                  <i class="pi" :class="processingPendingId === app.id ? 'pi-spin pi-spinner' : 'pi-check'"></i>
                  <span>{{ $t('calendar.pendingInbox.approve') }}</span>
                </button>
                <button type="button"
                        @click="openAppointmentDetails(app)"
                        class="px-3 py-2 rounded-xl bg-background border border-text/15 text-text text-xs font-bold hover:border-primary/50 hover:text-primary transition-all flex items-center gap-1.5 cursor-pointer">
                  <i class="pi pi-pencil text-xs"></i>
                  <span>{{ $t('calendar.pendingInbox.openAndEdit') }}</span>
                </button>
                <button type="button"
                        @click="rejectPendingBooking(app)"
                        :disabled="processingPendingId === app.id"
                        class="px-3 py-2 rounded-xl bg-red-500/10 text-red-500 border border-red-500/30 text-xs font-bold hover:bg-red-500 hover:text-white transition-all flex items-center gap-1.5 cursor-pointer disabled:opacity-50">
                  <i class="pi pi-times text-xs"></i>
                  <span>{{ $t('calendar.pendingInbox.reject') }}</span>
                </button>
              </div>
            </div>
          </div>
        </div>

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
                         :day-schedule-info="currentDayScheduleInfo"
                         :is-saving-schedule="isSavingDaySchedule"
                         @appointment-click="openAppointmentDetails"
                         @new-appointment="openNewAppointment"
                         @update-day-schedule="handleUpdateDaySchedule" />

      </div>
    </div>

    <AppointmentEditorModal :is-open="isModalOpen"
                            :edit-data="selectedAppointment"
                            :default-date="currentDate"
                            @close="closeModal"
                            @saved="handleModalSaved"
                            @deleted="handleModalDeleted" />

    <BookingScheduleModal :is-open="isScheduleModalOpen"
                          :schedule-config="scheduleConfig"
                          @close="isScheduleModalOpen = false"
                          @saved="handleScheduleConfigSaved" />

  </div>
</template>

<script setup>
  import { ref, computed, onMounted, watch } from "vue"; import { useRoute } from "vue-router";
  import { useI18n } from 'vue-i18n';
  import { useLocalizedText } from '@/composables/useLocalizedText';
  import { useAppointmentStore } from '@/stores/appointmentStore';
  import bookingApi from '@/services/bookingApi';

  // Kiszervezett komponensek beimportálása
  import AppointmentEditorModal from './AppointmentEditorModal.vue';
  import BookingScheduleModal from './BookingScheduleModal.vue';
  import CalendarToolbar from './CalendarToolbar.vue';
  import CalendarMonthView from './CalendarMonthView.vue';
  import CalendarWeekView from './CalendarWeekView.vue';
  import CalendarDayView from './CalendarDayView.vue';

  const store = useAppointmentStore();
  const { locale, t } = useI18n();
  const { getLocText } = useLocalizedText();
  const currentLang = computed(() => locale.value || 'hu-HU');

  // --- FÜGGŐBEN LÉVŐ (JÓVÁHAGYÁSRA VÁRÓ) FOGLALÁSOK KEZELÉSE ---
  const isPendingInboxOpen = ref(true);
  const processingPendingId = ref(null);
  const pendingBookings = computed(() => store.pendingAppointments || []);

  // --- ONLINE FOGLALÁSI MUNKAREND KONFIGURÁCIÓ ---
  const isScheduleModalOpen = ref(false);
  const isSavingDaySchedule = ref(false);
  const scheduleConfig = ref({
    mode: 'weekly_template',
    weeklyHours: {},
    dateOverrides: {}
  });

  const toDateKey = (d) => {
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  };

  const resolveDaySchedule = (date) => {
    const key = toDateKey(date);
    const overrides = scheduleConfig.value?.dateOverrides || {};
    if (overrides[key]) {
      const ov = overrides[key];
      const intervals = Array.isArray(ov.intervals) ? ov.intervals : [];
      return {
        isOpen: !!ov.isOpen && intervals.length > 0,
        hasCustomOverride: true,
        intervals
      };
    }

    if (scheduleConfig.value?.mode === 'custom_days') {
      return { isOpen: false, hasCustomOverride: false, intervals: [] };
    }

    const isoDow = ((date.getDay() + 6) % 7) + 1;
    const wh = scheduleConfig.value?.weeklyHours?.[String(isoDow)];
    if (wh && wh.isOpen && Array.isArray(wh.intervals) && wh.intervals.length > 0) {
      return {
        isOpen: true,
        hasCustomOverride: false,
        intervals: wh.intervals
      };
    }
    return { isOpen: false, hasCustomOverride: false, intervals: [] };
  };

  const fetchScheduleConfig = async () => {
    try {
      const res = await bookingApi.getScheduleConfig();
      if (res.data) {
        scheduleConfig.value = res.data;
      }
    } catch (err) {
      console.error('Hiba a munkarend betöltésekor:', err);
    }
  };

  const handleScheduleConfigSaved = (updatedConfig) => {
    if (updatedConfig) {
      scheduleConfig.value = updatedConfig;
    }
  };

  const handleUpdateDaySchedule = async (payload) => {
    if (isSavingDaySchedule.value) return;
    isSavingDaySchedule.value = true;
    try {
      const dateStr = toDateKey(currentDate.value);
      const res = await bookingApi.updateDayOverride(dateStr, payload);
      if (res.data) {
        scheduleConfig.value = res.data;
      }
    } catch (err) {
      console.error('Hiba a napi nyitvatartás mentésekor:', err);
    } finally {
      isSavingDaySchedule.value = false;
    }
  };

  // --- ALAP NÉZET ÉS NAPTÁR LOGIKA ---
  const route = useRoute(); const currentView = ref(route.query.view ? route.query.view : (route.query.date ? "day" : "month"));
  const currentDate = ref(route.query.date ? new Date(route.query.date) : new Date());

  const currentDayScheduleInfo = computed(() => resolveDaySchedule(currentDate.value));

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

    const schedInfo = resolveDaySchedule(date);
    const bookingHoursLabel = schedInfo.isOpen && schedInfo.intervals.length > 0
      ? schedInfo.intervals.map(i => `${i.start}-${i.end}`).join(', ')
      : '';

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
      loadPercentage: percentage,
      isOpenForBooking: schedInfo.isOpen,
      hasCustomOverride: schedInfo.hasCustomOverride,
      bookingHoursLabel
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

  const isCurrentToday = computed(() => {
    const today = new Date();
    if (currentView.value === 'day') {
      return currentDate.value.toDateString() === today.toDateString();
    }
    if (currentView.value === 'week') {
      return currentWeekDays.value.some(d => d.date.toDateString() === today.toDateString());
    }
    if (currentView.value === 'month') {
      return currentDate.value.getFullYear() === today.getFullYear() &&
             currentDate.value.getMonth() === today.getMonth();
    }
    return false;
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
    const sched = currentDayScheduleInfo.value;
    if (sched?.isOpen && sched.intervals?.length > 0) {
      const sMins = sched.intervals.map(i => { const [h, m] = i.start.split(':').map(Number); return h * 60 + m; });
      const eMins = sched.intervals.map(i => { const [h, m] = i.end.split(':').map(Number); return h * 60 + m; });
      minMinutes = Math.floor(Math.min(...sMins) / 60) * 60;
      maxMinutes = Math.ceil(Math.max(...eMins) / 60) * 60;
    }
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

  // --- FÜGGŐBEN LÉVŐ FOGLALÁSOK SEGÉDFÜGGVÉNYEI ---
  const getPendingCustomerName = (app) => {
    const c = customersList.value.find(x => x.id === app.customerId);
    if (c && c.name && c.name !== 'Ismeretlen Vendég') return c.name;
    if (app.customerName && app.customerName !== 'Ismeretlen') return app.customerName;
    return `${t('calendar.guest')} #${app.customerId}`;
  };

  const getPendingCustomerPhone = (app) => {
    if (app.customerPhone) return app.customerPhone;
    const c = customersList.value.find(x => x.id === app.customerId);
    return c?.phone || '';
  };

  const formatPendingDateTime = (iso) => {
    if (!iso) return '';
    const dt = new Date(iso);
    if (isNaN(dt.getTime())) return '';
    return dt.toLocaleString(currentLang.value, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const formatPendingSlot = (startIso, endIso) => {
    if (!startIso) return '';
    const s = new Date(startIso);
    const e = endIso ? new Date(endIso) : null;
    if (isNaN(s.getTime())) return '';
    const datePart = s.toLocaleDateString(currentLang.value, {
      month: 'short',
      day: 'numeric',
      weekday: 'short'
    });
    const startPart = s.toLocaleTimeString(currentLang.value, { hour: '2-digit', minute: '2-digit' });
    const endPart = e && !isNaN(e.getTime())
      ? e.toLocaleTimeString(currentLang.value, { hour: '2-digit', minute: '2-digit' })
      : '';
    return endPart ? `${datePart}, ${startPart}–${endPart}` : `${datePart}, ${startPart}`;
  };

  const getPendingServicesLabel = (app) => {
    if (!Array.isArray(app.items) || app.items.length === 0) return '';
    return app.items.map(item => {
      for (const s of availableServices.value) {
        const v = s.variants?.find(vx => vx.id === item.serviceVariantId);
        if (v) return `${getLocText(s.name)} (${getLocText(v.variantName)})`;
      }
      return t('calendar.unknown');
    }).join(', ');
  };

  const jumpToPendingDay = (app) => {
    if (!app?.startDateTime) return;
    currentDate.value = new Date(app.startDateTime);
    currentView.value = 'day';
  };

  const approvePendingBooking = async (app) => {
    if (!app?.id || processingPendingId.value) return;
    processingPendingId.value = app.id;
    try {
      await store.updateAppointmentStatus(app.id, 1);
      fetchDataForCurrentView();
    } catch (err) {
      alert(t('calendar.editor.saveError') + '\n' + (err.response?.data?.message || err.message));
    } finally {
      processingPendingId.value = null;
    }
  };

  const rejectPendingBooking = async (app) => {
    if (!app?.id || processingPendingId.value) return;
    if (!confirm(t('calendar.pendingInbox.confirmReject'))) return;
    processingPendingId.value = app.id;
    try {
      await store.updateAppointmentStatus(app.id, 3);
      fetchDataForCurrentView();
    } catch (err) {
      alert(t('calendar.editor.saveError') + '\n' + (err.response?.data?.message || err.message));
    } finally {
      processingPendingId.value = null;
    }
  };

  onMounted(() => {
    store.initUserPermissions();
    fetchDataForCurrentView();
    fetchServicesForAdmin();
    fetchCustomers();
    fetchScheduleConfig();
  });
  watch(currentDate, () => fetchDataForCurrentView());
</script>
