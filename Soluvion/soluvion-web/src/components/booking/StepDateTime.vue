<template>
  <div class="py-2">
    <h3 class="text-xl font-bold mb-5 text-text">
      {{ bookableEmployees.length > 1 ? $t('booking.selectDateTime') : $t('booking.selectDateTimeSolo') }}
    </h3>

    <div class="flex flex-col gap-6">
      <!-- Munkatárs választó (kizárólag akkor jelenik meg, ha a szalonnak 1-nél több valódi dolgozója van) -->
      <div v-if="bookableEmployees.length > 1" class="flex flex-col gap-1.5 max-w-md">
        <label class="text-xs font-bold uppercase tracking-wider text-text-muted">{{ $t('booking.employeeLabel') }}</label>
        <div class="relative flex items-center">
          <i class="pi pi-id-card absolute left-3.5 text-text-muted text-sm pointer-events-none"></i>
          <select v-model="localEmployeeId"
                  class="w-full pl-10 pr-4 py-3 rounded-xl border border-text/15 bg-background text-text focus:border-primary focus:ring-1 focus:ring-primary/40 focus:outline-none text-sm font-semibold cursor-pointer transition-all">
            <option :value="0">{{ $t('booking.anyone') }}</option>
            <option v-for="emp in bookableEmployees" :key="emp.id" :value="emp.id">
              {{ emp.name }}
            </option>
          </select>
        </div>
      </div>

      <!-- 1. LÉPÉS: HAVI SZABAD IDŐPONT NAPTÁR (Admin CalendarMonthView stílusban) -->
      <div class="bg-surface p-3 md:p-6 rounded-2xl border border-text/10 shadow-sm space-y-4">
        <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-text/10 pb-3">
          <div class="flex items-center gap-2">
            <i class="pi pi-calendar text-primary text-lg"></i>
            <h4 class="font-bold text-sm md:text-base text-text">{{ $t('booking.calendarTitle') }}</h4>
          </div>

          <!-- Hónap léptető -->
          <div class="flex items-center bg-background rounded-xl p-1 border border-text/10 self-start sm:self-auto">
            <button type="button"
                    @click="prevMonth"
                    :disabled="isCurrentMonth"
                    class="w-9 h-8 rounded-lg flex items-center justify-center text-text hover:bg-text/5 disabled:opacity-30 disabled:cursor-not-allowed transition-colors cursor-pointer">
              <i class="pi pi-chevron-left text-xs font-bold"></i>
            </button>
            <span class="px-3 md:px-4 font-black text-xs md:text-sm text-primary uppercase tracking-wider min-w-[140px] text-center">
              {{ currentMonthLabel }}
            </span>
            <button type="button"
                    @click="nextMonth"
                    class="w-9 h-8 rounded-lg flex items-center justify-center text-text hover:bg-text/5 transition-colors cursor-pointer">
              <i class="pi pi-chevron-right text-xs font-bold"></i>
            </button>
          </div>
        </div>

        <!-- Jelmagyarázat -->
        <div class="flex flex-wrap items-center gap-4 text-[11px] font-semibold text-text-muted">
          <span class="inline-flex items-center gap-1.5">
            <span class="w-2.5 h-2.5 rounded-full bg-emerald-500"></span>
            <span>{{ $t('booking.legendAvailable') }}</span>
          </span>
          <span class="inline-flex items-center gap-1.5">
            <span class="w-2.5 h-2.5 rounded-full bg-amber-500"></span>
            <span>{{ $t('booking.legendFull') }}</span>
          </span>
          <span class="inline-flex items-center gap-1.5">
            <span class="w-2.5 h-2.5 rounded-full bg-gray-300"></span>
            <span>{{ $t('booking.legendClosed') }}</span>
          </span>
        </div>

        <!-- Hét napjai fejléc (Hétfőtől Vasárnapig) -->
        <div class="grid grid-cols-7 gap-1 md:gap-2 pt-1">
          <div v-for="(dayName, idx) in weekDayHeaders"
               :key="idx"
               class="text-center text-[10px] md:text-xs font-bold text-text-muted uppercase mb-1 tracking-wider">
            {{ dayName }}
          </div>

          <!-- 42 napos naptárrács -->
          <button v-for="(cell, index) in calendarDays"
                  :key="cell.dateStr || index"
                  type="button"
                  :disabled="!cell.isSelectable"
                  @click="selectDayCell(cell)"
                  class="relative overflow-hidden min-h-[54px] md:min-h-[76px] p-1 md:p-2 rounded-xl transition-all flex flex-col items-center justify-between border text-center"
                  :class="[
                    !cell.isCurrentMonth ? 'bg-gray-100/60 border-transparent opacity-35 cursor-not-allowed' : '',
                    cell.isCurrentMonth && !cell.isSelectable ? 'bg-gray-100/80 border-gray-200/80 text-gray-400 cursor-not-allowed opacity-65' : '',
                    cell.isCurrentMonth && cell.isSelectable && selectedDate !== cell.dateStr
                      ? 'bg-white border-emerald-500/40 shadow-sm hover:border-primary hover:shadow-md cursor-pointer'
                      : '',
                    cell.isCurrentMonth && cell.isSelectable && selectedDate === cell.dateStr
                      ? 'bg-primary/10 border-primary ring-2 ring-primary shadow-md scale-[1.02] cursor-pointer'
                      : '',
                    cell.isToday ? 'ring-1 ring-primary/50' : ''
                  ]">

            <!-- Terheltségi csík felül (mint az Admin Naptárban) -->
            <div v-if="cell.isCurrentMonth && cell.isOpen"
                 class="absolute top-0 left-0 right-0 h-1 bg-gray-200 overflow-hidden">
              <div class="h-full transition-all"
                   :class="cell.hasEnoughFreeTime ? 'bg-primary' : 'bg-amber-500'"
                   :style="{ width: `${cell.loadPercentage}%` }"></div>
            </div>

            <!-- Nap száma -->
            <span class="font-black text-xs md:text-base mt-1"
                  :class="[
                    selectedDate === cell.dateStr ? 'text-primary' : (cell.isSelectable ? 'text-gray-800' : 'text-gray-400')
                  ]">
              {{ cell.dayNumber }}
            </span>

            <!-- Nyitvatartási idősáv jelvény -->
            <span v-if="cell.isCurrentMonth && cell.isOpen && cell.hoursBadge"
                  class="text-[8px] md:text-[10px] font-bold px-1.5 py-0.2 rounded-full leading-tight truncate max-w-full"
                  :class="cell.hasEnoughFreeTime
                    ? (selectedDate === cell.dateStr ? 'bg-primary text-surface' : 'bg-emerald-500/15 text-emerald-700')
                    : 'bg-amber-500/15 text-amber-700'">
              {{ cell.hoursBadge }}
            </span>

            <!-- Állapot pötty alul -->
            <div class="flex items-center justify-center pb-0.5 mt-auto">
              <span v-if="cell.isCurrentMonth && cell.isSelectable"
                    class="w-2 h-2 md:w-2.5 md:h-2.5 rounded-full shadow-sm"
                    :class="selectedDate === cell.dateStr ? 'bg-primary' : 'bg-emerald-500'"></span>
              <span v-else-if="cell.isCurrentMonth && cell.isOpen && !cell.isPast && !cell.hasEnoughFreeTime"
                    class="w-2 h-2 rounded-full bg-amber-500"></span>
            </div>
          </button>
        </div>

        <!-- Ha nincs szabad nap a hónapban -->
        <div v-if="!isLoadingAvailability && !hasAnyAvailableDayInCurrentMonth"
             class="p-3.5 rounded-xl bg-amber-500/10 border border-amber-500/30 text-amber-700 text-xs md:text-sm font-semibold flex items-center gap-2">
          <i class="pi pi-info-circle text-base shrink-0"></i>
          <span>{{ $t('booking.noAvailableDayInMonth', { min: effectiveDuration }) }}</span>
        </div>
      </div>

      <!-- 2. LÉPÉS: NAPI IDŐVONAL ÉS CSÚSZTATHATÓ IDŐPONT BLOKK (15 perces ugrásokkal) -->
      <div v-if="selectedDayData && selectedDayData.hasEnoughFreeTime"
           class="bg-surface p-4 md:p-6 rounded-2xl border border-primary/30 shadow-md space-y-5 animate-fade-in">

        <!-- Napi fejléc + Kiválasztott időpont visszajelző -->
        <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-text/10 pb-4">
          <div>
            <div class="flex items-baseline gap-2 flex-wrap">
              <h4 class="text-lg md:text-xl font-black text-primary capitalize">
                {{ selectedDayNameLong }}
              </h4>
              <span class="text-xs md:text-sm font-semibold text-text">
                {{ selectedDateFormattedLong }}
              </span>
            </div>
            <p class="text-xs text-text-muted mt-0.5 flex items-center gap-1.5">
              <i class="pi pi-clock text-primary"></i>
              <span>{{ $t('booking.openHoursLabel') }}</span>
              <strong class="text-text">{{ selectedDayOpenHoursText }}</strong>
              <span class="hidden sm:inline text-text-muted/70">· {{ $t('booking.timelineHint') }}</span>
            </p>
          </div>

          <!-- Aktuálisan beállított idősáv kártya -->
          <div v-if="selectedStartMinutes !== null"
               class="px-4 py-2.5 rounded-xl bg-primary text-surface shadow-sm flex items-center gap-2.5 self-start sm:self-auto shrink-0">
            <i class="pi pi-calendar-clock text-lg"></i>
            <div class="flex flex-col">
              <span class="text-[10px] uppercase tracking-wider font-bold opacity-85">
                {{ $t('booking.yourAppointment') }} ({{ $t('booking.durationMinutes', { min: effectiveDuration }) }})
              </span>
              <span class="text-base md:text-lg font-black tracking-wide leading-tight">
                {{ formatMinutesToHHMM(selectedStartMinutes) }} – {{ formatMinutesToHHMM(selectedStartMinutes + effectiveDuration) }}
              </span>
            </div>
          </div>
        </div>

        <!-- INTERAKTÍV IDŐVONAL SÁV (Admin CalendarDayView stílusban, húzható blokkal) -->
        <div class="relative w-full pt-8 pb-8 select-none">

          <!-- Szabad idősávok méretbuborékai az idővonal felett -->
          <button v-for="(gap, idx) in timelineModel.freeGapBadges"
                  :key="'fg-'+idx"
                  type="button"
                  @click="jumpToFreeGap(gap)"
                  class="absolute top-0 text-[10px] md:text-xs font-bold text-emerald-800 bg-emerald-50 hover:bg-emerald-100 px-2 py-0.5 rounded-md border border-emerald-300 shadow-xs whitespace-nowrap transition-colors cursor-pointer -translate-x-1/2"
                  :style="{ left: `${gap.centerPct}%` }">
            {{ $t('booking.freeGapLabel', { time: formatDurationBadge(gap.durationMinutes) }) }}
          </button>

          <!-- Maga az idővonal sín (alapból ~30%-os szürke, amiben a szabad sávok lekerekített fehér kapszulák -> inverz lekerekítés a szürke zónáknál!) -->
          <div ref="timelineTrackRef"
               @pointerdown="onTrackPointerDown"
               class="w-full h-12 md:h-14 bg-gray-300 rounded-xl shadow-inner relative border border-gray-300 overflow-hidden cursor-pointer touch-none">

            <!-- SZABAD IDŐSÁVOK (Fehér, lekerekített kapszulák – így a szürke nem elérhető zónák szélei inverzen kifelé kerekednek a felső/alsó szegélyhez) -->
            <div v-for="(freeSeg, idx) in timelineModel.freeSegments"
                 :key="'free-'+idx"
                 class="absolute top-0 bottom-0 bg-white rounded-xl pointer-events-none"
                 :style="{ left: `${freeSeg.left}%`, width: `${freeSeg.width}%` }"></div>

            <!-- Órás segédvonalak -->
            <div v-for="hour in timelineModel.hours"
                 :key="'grid-'+hour.val"
                 class="absolute top-0 bottom-0 w-px bg-gray-400/25 pointer-events-none"
                 :style="{ left: `${hour.left}%` }"></div>

            <!-- NEM ELÉRHETŐ / FOGLALT IDŐSÁVOK (Tooltip réteg) -->
            <div v-for="(unavail, idx) in timelineModel.unavailableSegments"
                 :key="'unavail-'+idx"
                 class="absolute top-0 bottom-0 select-none"
                 :style="{
                   left: `${unavail.left}%`,
                   width: `${unavail.width}%`
                 }"
                 :title="`${$t('booking.occupiedBlock')}: ${unavail.start} – ${unavail.end}`">
            </div>

            <!-- A VENDÉG SAJÁT CSÚSZTATHATÓ IDŐPONT BLOKKJA (box-border belső kerettel, hogy sose lógjon bele a szürke zónába) -->
            <div v-if="guestBlockStyle"
                 @pointerdown.stop="onBlockPointerDown"
                 class="guest-slider-block absolute top-0 bottom-0 rounded-xl bg-primary text-surface shadow-md border-2 border-white box-border flex items-center justify-center px-1 overflow-hidden cursor-grab active:cursor-grabbing transition-[left] duration-75 z-10 select-none touch-none"
                 :style="guestBlockStyle">
              <span class="slider-time-label text-[11px] md:text-xs font-black tracking-tight whitespace-nowrap">
                {{ formatMinutesToHHMM(selectedStartMinutes) }}
              </span>
              <span class="slider-grip-lines items-center gap-0.5 opacity-75">
                <span class="w-0.5 h-3.5 bg-current rounded-full"></span>
                <span class="w-0.5 h-3.5 bg-current rounded-full"></span>
              </span>
            </div>
          </div>

          <!-- Óra skála az idővonal alatt (mint a CalendarDayView-ban) -->
          <div v-for="hour in timelineModel.hours"
               :key="'hr-'+hour.val"
               class="absolute bottom-0 text-[10px] md:text-xs font-bold text-text-muted -translate-x-1/2 flex flex-col items-center pointer-events-none"
               :style="{ left: `${hour.left}%` }">
            <div class="w-0.5 h-1.5 bg-text/25 mb-0.5 rounded-full"></div>
            <span>{{ hour.val }}:00</span>
          </div>
        </div>
      </div>

      <div v-else-if="!isLoadingAvailability"
           class="p-4 rounded-xl bg-background/60 border border-dashed border-text/15 text-center text-xs md:text-sm text-text-muted">
        {{ $t('booking.selectDayPrompt') }}
      </div>

      <div v-if="errorMessage"
           class="p-3.5 bg-red-500/10 text-red-500 rounded-xl text-sm border border-red-500/30 flex items-center gap-2 font-medium">
        <i class="pi pi-exclamation-triangle shrink-0"></i>
        <span>{{ errorMessage }}</span>
      </div>
    </div>

    <!-- Navigációs gombok -->
    <div class="mt-8 pt-4 border-t border-text/10 flex justify-between items-center">
      <button type="button"
              @click="onPrev"
              class="min-h-[48px] px-6 py-2.5 border border-text/20 text-text font-semibold rounded-xl transition-colors hover:bg-text/5 cursor-pointer flex items-center gap-2">
        <i class="pi pi-arrow-left text-sm"></i> {{ $t('booking.back') }}
      </button>
      <button type="button"
              @click="$emit('submit')"
              :disabled="!selectedDate || selectedStartMinutes === null || isSubmitting"
              class="min-h-[48px] px-8 py-3 bg-primary text-surface font-bold rounded-xl text-base shadow-md hover:brightness-110 active:scale-95 transition-all disabled:opacity-40 disabled:cursor-not-allowed flex items-center gap-2 cursor-pointer">
        <i v-if="isSubmitting" class="pi pi-spin pi-spinner"></i>
        <i v-else class="pi pi-check"></i>
        <span>{{ $t('booking.submit') }}</span>
      </button>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted, onBeforeUnmount } from 'vue';
import { useI18n } from 'vue-i18n';
import bookingApi from '@/services/bookingApi';

const props = defineProps({
  employeeId: { type: Number, required: true },
  selectedDateTime: { type: String, required: true },
  totalDuration: { type: Number, default: 30 },
  isSubmitting: { type: Boolean, default: false },
  errorMessage: { type: String, default: '' },
  onPrev: { type: Function, required: true }
});

const emit = defineEmits(['update:employeeId', 'update:selectedDateTime', 'submit']);
const { locale, t } = useI18n();

const localEmployeeId = ref(props.employeeId);
const effectiveDuration = computed(() => Math.max(15, Number(props.totalDuration) || 30));

const toDateStr = (d) => {
  const year = d.getFullYear();
  const month = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

const formatMinutesToHHMM = (mins) => {
  if (mins == null || isNaN(mins)) return '';
  const clamped = Math.max(0, Math.min(1440, Math.round(mins)));
  const h = Math.floor(clamped / 60);
  const m = clamped % 60;
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`;
};

const parseHHMMToMinutes = (hhmm) => {
  if (!hhmm || !hhmm.includes(':')) return null;
  const [h, m] = hhmm.split(':').map(Number);
  if (isNaN(h) || isNaN(m)) return null;
  return h * 60 + m;
};

const formatDurationBadge = (m) => {
  if (m >= 60) {
    const h = Math.floor(m / 60);
    const rem = m % 60;
    return rem > 0 ? `${h}:${String(rem).padStart(2, '0')}` : `${h}h`;
  }
  return `${m}p`;
};

const today = new Date();
const todayStr = toDateStr(today);

const viewYear = ref(today.getFullYear());
const viewMonth = ref(today.getMonth()); // 0-indexed

const selectedDate = ref('');
const selectedStartMinutes = ref(null);

const isLoadingAvailability = ref(false);
const availabilityMap = ref({});
const bookableEmployees = ref([]);

const isCurrentMonth = computed(() => {
  return viewYear.value === today.getFullYear() && viewMonth.value <= today.getMonth();
});

const currentMonthLabel = computed(() => {
  const d = new Date(viewYear.value, viewMonth.value, 1);
  return d.toLocaleDateString(locale.value || 'hu', { year: 'numeric', month: 'long' });
});

const weekDayHeaders = computed(() => {
  const headers = [];
  for (let i = 0; i < 7; i++) {
    const d = new Date(2024, 0, 1 + i); // 2024-01-01 Hétfő
    headers.push(d.toLocaleDateString(locale.value || 'hu', { weekday: 'short' }));
  }
  return headers;
});

// 42 cellás naptár felépítése (pontosan úgy, mint a CalendarGrid.vue-ban)
const rawGridDates = computed(() => {
  const year = viewYear.value;
  const month = viewMonth.value;
  const firstDay = new Date(year, month, 1);
  let startingDay = firstDay.getDay() || 7;
  startingDay--;

  const list = [];
  for (let i = 0; i < startingDay; i++) {
    list.push({ date: new Date(year, month, -startingDay + i + 1), isCurrentMonth: false });
  }
  const daysInMonth = new Date(year, month + 1, 0).getDate();
  for (let i = 1; i <= daysInMonth; i++) {
    list.push({ date: new Date(year, month, i), isCurrentMonth: true });
  }
  const remainingCells = 42 - list.length;
  for (let i = 1; i <= remainingCells; i++) {
    list.push({ date: new Date(year, month + 1, i), isCurrentMonth: false });
  }
  return list;
});

const calendarDays = computed(() => {
  return rawGridDates.value.map(item => {
    const dateStr = toDateStr(item.date);
    const isPast = dateStr < todayStr;
    const isToday = dateStr === todayStr;
    const avail = availabilityMap.value[dateStr];

    const isOpen = avail ? !!avail.isOpen : false;
    const hasEnoughFreeTime = avail ? !!avail.hasEnoughFreeTime : false;
    const loadPercentage = avail ? (avail.loadPercentage || 0) : 0;
    const openIntervals = avail?.openIntervals || [];
    const hoursBadge = openIntervals.length > 0
      ? openIntervals.map(i => `${i.start}-${i.end}`).join(', ')
      : '';

    return {
      date: item.date,
      dateStr,
      dayNumber: item.date.getDate(),
      isCurrentMonth: item.isCurrentMonth,
      isPast,
      isToday,
      isOpen,
      hasEnoughFreeTime,
      loadPercentage,
      hoursBadge,
      isSelectable: item.isCurrentMonth && !isPast && isOpen && hasEnoughFreeTime
    };
  });
});

const hasAnyAvailableDayInCurrentMonth = computed(() => {
  return calendarDays.value.some(c => c.isCurrentMonth && c.isSelectable);
});

const selectedDayData = computed(() => {
  if (!selectedDate.value) return null;
  return availabilityMap.value[selectedDate.value] || null;
});

const selectedDayNameLong = computed(() => {
  if (!selectedDate.value) return '';
  const [y, m, d] = selectedDate.value.split('-').map(Number);
  return new Date(y, m - 1, d).toLocaleDateString(locale.value || 'hu', { weekday: 'long' });
});

const selectedDateFormattedLong = computed(() => {
  if (!selectedDate.value) return '';
  const [y, m, d] = selectedDate.value.split('-').map(Number);
  return new Date(y, m - 1, d).toLocaleDateString(locale.value || 'hu', { year: 'numeric', month: 'long', day: 'numeric' });
});

const selectedDayOpenHoursText = computed(() => {
  const openIvs = selectedDayData.value?.openIntervals || [];
  return openIvs.map(i => `${i.start} – ${i.end}`).join(', ');
});

// Érvényes 5 perces kezdési időpontok listája a szabad réseken belül
const validStartMinutesList = computed(() => {
  const day = selectedDayData.value;
  if (!day || !day.freeIntervals) return [];
  const dur = effectiveDuration.value;
  const list = [];

  for (const free of day.freeIntervals) {
    const firstSlot = Math.ceil(free.startMinutes / 5) * 5;
    for (let m = firstSlot; m + dur <= free.endMinutes; m += 5) {
      if (!list.includes(m)) {
        list.push(m);
      }
    }
  }
  return list.sort((a, b) => a - b);
});

const currentSlotIndex = computed(() => {
  if (selectedStartMinutes.value === null) return 0;
  const idx = validStartMinutesList.value.indexOf(selectedStartMinutes.value);
  return idx >= 0 ? idx : 0;
});

// Napi idővonal modell (szabad rések, nem elérhető/foglalt sávok, óra skála)
const timelineModel = computed(() => {
  const day = selectedDayData.value;
  if (!day || !day.openIntervals || day.openIntervals.length === 0) {
    return { minMinutes: 480, maxMinutes: 1020, totalMinutes: 540, hours: [], unavailableSegments: [], freeGapBadges: [] };
  }

  const minOpen = Math.min(...day.openIntervals.map(i => i.startMinutes));
  const maxOpen = Math.max(...day.openIntervals.map(i => i.endMinutes));

  const minMinutes = Math.floor(minOpen / 60) * 60;
  let maxMinutes = Math.ceil(maxOpen / 60) * 60;
  if (maxMinutes <= minMinutes) maxMinutes = minMinutes + 60;
  const totalMinutes = maxMinutes - minMinutes;

  const hours = [];
  for (let m = minMinutes; m <= maxMinutes; m += 60) {
    hours.push({
      val: m / 60,
      left: ((m - minMinutes) / totalMinutes) * 100
    });
  }

  // Minden olyan szakasz a [minMinutes, maxMinutes] tartományban, ami NEM szabad (akár szünet, akár foglalt időpont)
  const sortedFree = [...(day.freeIntervals || [])]
    .map(iv => ({
      startMinutes: Math.max(minMinutes, iv.startMinutes),
      endMinutes: Math.min(maxMinutes, iv.endMinutes)
    }))
    .filter(iv => iv.endMinutes > iv.startMinutes)
    .sort((a, b) => a.startMinutes - b.startMinutes);

  const freeSegments = sortedFree.map(free => ({
    left: ((free.startMinutes - minMinutes) / totalMinutes) * 100,
    width: ((free.endMinutes - free.startMinutes) / totalMinutes) * 100
  }));

  const unavailableSegments = [];
  let cursor = minMinutes;
  for (const free of sortedFree) {
    if (free.startMinutes > cursor) {
      unavailableSegments.push({
        start: formatMinutesToHHMM(cursor),
        end: formatMinutesToHHMM(free.startMinutes),
        left: ((cursor - minMinutes) / totalMinutes) * 100,
        width: ((free.startMinutes - cursor) / totalMinutes) * 100
      });
    }
    cursor = Math.max(cursor, free.endMinutes);
  }
  if (cursor < maxMinutes) {
    unavailableSegments.push({
      start: formatMinutesToHHMM(cursor),
      end: formatMinutesToHHMM(maxMinutes),
      left: ((cursor - minMinutes) / totalMinutes) * 100,
      width: ((maxMinutes - cursor) / totalMinutes) * 100
    });
  }

  const freeGapBadges = (day.freeIntervals || [])
    .filter(iv => (iv.endMinutes - iv.startMinutes) >= effectiveDuration.value)
    .map(iv => {
      const s = Math.max(minMinutes, iv.startMinutes);
      const e = Math.min(maxMinutes, iv.endMinutes);
      const center = (s + e) / 2;
      return {
        startMinutes: iv.startMinutes,
        endMinutes: iv.endMinutes,
        durationMinutes: iv.endMinutes - iv.startMinutes,
        centerPct: ((center - minMinutes) / totalMinutes) * 100
      };
    });

  return {
    minMinutes,
    maxMinutes,
    totalMinutes,
    hours,
    freeSegments,
    unavailableSegments,
    freeGapBadges
  };
});

const guestBlockStyle = computed(() => {
  if (selectedStartMinutes.value === null) return null;
  const { minMinutes, totalMinutes } = timelineModel.value;
  if (!totalMinutes) return null;

  const left = ((selectedStartMinutes.value - minMinutes) / totalMinutes) * 100;
  const width = (effectiveDuration.value / totalMinutes) * 100;
  return {
    left: `${Math.max(0, Math.min(100, left))}%`,
    width: `${Math.max(3, Math.min(100, width))}%`
  };
});

const syncDateTimeOutput = () => {
  if (selectedDate.value && selectedStartMinutes.value !== null) {
    const timeStr = formatMinutesToHHMM(selectedStartMinutes.value);
    emit('update:selectedDateTime', `${selectedDate.value}T${timeStr}`);
  } else {
    emit('update:selectedDateTime', '');
  }
};

const selectStartMinutes = (mins) => {
  selectedStartMinutes.value = mins;
  syncDateTimeOutput();
};

const selectDayCell = (cell) => {
  if (!cell.isSelectable) return;
  const isDifferentDay = selectedDate.value !== cell.dateStr;
  selectedDate.value = cell.dateStr;

  const validSlots = validStartMinutesList.value;
  if (validSlots.length > 0) {
    if (isDifferentDay || selectedStartMinutes.value === null || !validSlots.includes(selectedStartMinutes.value)) {
      selectedStartMinutes.value = validSlots[0];
    }
  } else {
    selectedStartMinutes.value = null;
  }
  syncDateTimeOutput();
};

const stepSlot = (delta) => {
  const list = validStartMinutesList.value;
  if (list.length === 0) return;
  const nextIdx = Math.max(0, Math.min(list.length - 1, currentSlotIndex.value + delta));
  selectStartMinutes(list[nextIdx]);
};

const onSliderIndexInput = (e) => {
  const idx = Number(e.target.value);
  const list = validStartMinutesList.value;
  if (idx >= 0 && idx < list.length) {
    selectStartMinutes(list[idx]);
  }
};

const jumpToFreeGap = (gap) => {
  const firstSlot = Math.ceil(gap.startMinutes / 5) * 5;
  if (validStartMinutesList.value.includes(firstSlot)) {
    selectStartMinutes(firstSlot);
  }
};

// ============================================================================
// HÚZHATÓ (DRAGGABLE) IDŐPONT BLOKK LOGIKA AZ IDŐVONALON
// ============================================================================
const timelineTrackRef = ref(null);
const isDragging = ref(false);
const dragGrabOffsetMinutes = ref(0);

const findNearestValidSlot = (targetMinutes) => {
  const list = validStartMinutesList.value;
  if (list.length === 0) return null;
  let best = list[0];
  let bestDiff = Math.abs(targetMinutes - best);
  for (let i = 1; i < list.length; i++) {
    const diff = Math.abs(targetMinutes - list[i]);
    if (diff < bestDiff) {
      bestDiff = diff;
      best = list[i];
    }
  }
  return best;
};

const updateSlotFromClientX = (clientX, offsetMins = 0) => {
  const trackEl = timelineTrackRef.value;
  if (!trackEl) return;
  const rect = trackEl.getBoundingClientRect();
  if (rect.width <= 0) return;

  const ratio = Math.max(0, Math.min(1, (clientX - rect.left) / rect.width));
  const { minMinutes, totalMinutes } = timelineModel.value;
  const rawStart = minMinutes + ratio * totalMinutes - offsetMins;
  const nearest = findNearestValidSlot(rawStart);
  if (nearest !== null && nearest !== selectedStartMinutes.value) {
    selectStartMinutes(nearest);
  }
};

const onBlockPointerDown = (e) => {
  const trackEl = timelineTrackRef.value;
  if (!trackEl || selectedStartMinutes.value === null) return;
  const rect = trackEl.getBoundingClientRect();
  if (rect.width <= 0) return;

  const ratio = (e.clientX - rect.left) / rect.width;
  const pointerMinutes = timelineModel.value.minMinutes + ratio * timelineModel.value.totalMinutes;
  dragGrabOffsetMinutes.value = pointerMinutes - selectedStartMinutes.value;
  isDragging.value = true;

  window.addEventListener('pointermove', onWindowPointerMove);
  window.addEventListener('pointerup', onWindowPointerUp);
};

const onTrackPointerDown = (e) => {
  dragGrabOffsetMinutes.value = effectiveDuration.value / 2;
  updateSlotFromClientX(e.clientX, dragGrabOffsetMinutes.value);
  isDragging.value = true;

  window.addEventListener('pointermove', onWindowPointerMove);
  window.addEventListener('pointerup', onWindowPointerUp);
};

const onWindowPointerMove = (e) => {
  if (!isDragging.value) return;
  updateSlotFromClientX(e.clientX, dragGrabOffsetMinutes.value);
};

const onWindowPointerUp = () => {
  isDragging.value = false;
  window.removeEventListener('pointermove', onWindowPointerMove);
  window.removeEventListener('pointerup', onWindowPointerUp);
};

onBeforeUnmount(() => {
  window.removeEventListener('pointermove', onWindowPointerMove);
  window.removeEventListener('pointerup', onWindowPointerUp);
});

// ============================================================================
// ADATBETÖLTÉS A BACKENDRŐL
// ============================================================================
const fetchMonthAvailability = async () => {
  const cells = rawGridDates.value;
  if (cells.length === 0) return;

  const startStr = toDateStr(cells[0].date);
  const endStr = toDateStr(cells[cells.length - 1].date);

  isLoadingAvailability.value = true;
  try {
    const res = await bookingApi.getPublicAvailability({
      start: startStr,
      end: endStr,
      durationMinutes: effectiveDuration.value,
      employeeId: localEmployeeId.value > 0 ? localEmployeeId.value : undefined
    });

    if (Array.isArray(res.data?.employees)) {
      bookableEmployees.value = res.data.employees;
    }

    const days = res.data?.days || [];
    const nextMap = {};
    for (const d of days) {
      nextMap[d.date] = d;
    }
    availabilityMap.value = nextMap;

    // Ha a jelenleg kiválasztott nap már nem elérhető, vagy még nincs kiválasztva nap, válasszuk az első elérhetőt
    const currentSelectedCell = calendarDays.value.find(c => c.dateStr === selectedDate.value);
    if (!currentSelectedCell || !currentSelectedCell.isSelectable) {
      const firstSelectable = calendarDays.value.find(c => c.isCurrentMonth && c.isSelectable);
      if (firstSelectable) {
        selectDayCell(firstSelectable);
      } else {
        selectedStartMinutes.value = null;
        syncDateTimeOutput();
      }
    } else {
      // Ellenőrizzük, hogy a kiválasztott kezdési perc még érvényes-e
      const validSlots = validStartMinutesList.value;
      if (validSlots.length > 0 && !validSlots.includes(selectedStartMinutes.value)) {
        selectStartMinutes(validSlots[0]);
      } else if (validSlots.length === 0) {
        selectedStartMinutes.value = null;
        syncDateTimeOutput();
      }
    }
  } catch (err) {
    console.error('Hiba a szabad időpontok lekérésekor:', err);
  } finally {
    isLoadingAvailability.value = false;
  }
};

const prevMonth = () => {
  if (isCurrentMonth.value) return;
  if (viewMonth.value === 0) {
    viewMonth.value = 11;
    viewYear.value--;
  } else {
    viewMonth.value--;
  }
};

const nextMonth = () => {
  if (viewMonth.value === 11) {
    viewMonth.value = 0;
    viewYear.value++;
  } else {
    viewMonth.value++;
  }
};

onMounted(() => {
  if (props.selectedDateTime && props.selectedDateTime.includes('T')) {
    const [dPart, tPart] = props.selectedDateTime.split('T');
    selectedDate.value = dPart;
    selectedStartMinutes.value = parseHHMMToMinutes(tPart.substring(0, 5));
    const [y, m] = dPart.split('-').map(Number);
    if (!isNaN(y) && !isNaN(m)) {
      viewYear.value = y;
      viewMonth.value = m - 1;
    }
  }
  fetchMonthAvailability();
});

watch([viewYear, viewMonth, effectiveDuration, localEmployeeId], () => {
  fetchMonthAvailability();
});

watch(() => props.employeeId, (val) => {
  localEmployeeId.value = val;
});

watch(localEmployeeId, (val) => {
  emit('update:employeeId', val);
});
</script>

<style scoped>
.guest-slider-block {
  container-type: inline-size;
}
.slider-time-label {
  display: none;
}
.slider-grip-lines {
  display: inline-flex;
}
@container (min-width: 44px) {
  .slider-time-label {
    display: inline;
  }
  .slider-grip-lines {
    display: none;
  }
}
</style>

