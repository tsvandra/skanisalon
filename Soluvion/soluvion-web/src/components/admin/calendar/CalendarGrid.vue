<template>
  <div class="flex flex-col h-full bg-background text-text">
    
    <div class="sticky top-0 z-20 bg-surface border-b border-text/10 p-4 shadow-sm">
      <div class="max-w-7xl mx-auto w-full flex flex-col xl:flex-row justify-between items-start xl:items-center gap-4">
        
        <div class="flex items-center gap-4">
          <div>
            <h2 class="text-2xl font-bold text-text drop-shadow-sm">Vezérlőpult</h2>
            <span class="text-sm text-text-muted">Szerepkör: <strong class="text-primary">{{ store.userRole }}</strong></span>
          </div>
        </div>

        <div class="flex flex-col md:flex-row items-center gap-3 w-full xl:w-auto">
          
          <button @click="goToday" class="w-full md:w-auto px-5 min-h-[44px] font-bold text-sm bg-background border border-text/10 hover:border-primary/50 transition-colors rounded-xl text-text shadow-sm flex items-center justify-center gap-2">
            <i class="pi pi-calendar-times text-primary"></i> {{ $t('calendar.today') || 'Ma' }}
          </button>

          <div class="flex items-center bg-background rounded-xl p-1 shadow-inner border border-text/5 w-full md:w-auto justify-between md:justify-center">
            <button @click="goPrev" class="min-w-[44px] min-h-[44px] flex items-center justify-center rounded-lg hover:bg-text/5 transition-colors text-text">
              <i class="pi pi-chevron-left font-bold text-lg"></i>
            </button>
            
            <div class="px-4 font-black text-text uppercase tracking-wider text-sm md:text-base text-center min-w-[150px] md:min-w-[220px]">
              {{ pagerText }}
            </div>

            <button @click="goNext" class="min-w-[44px] min-h-[44px] flex items-center justify-center rounded-lg hover:bg-text/5 transition-colors text-text">
              <i class="pi pi-chevron-right font-bold text-lg"></i>
            </button>
          </div>

          <div class="flex bg-background rounded-xl p-1 shadow-inner border border-text/5 w-full md:w-auto mt-2 md:mt-0">
            <button v-for="view in views" :key="view.id"
                    @click="currentView = view.id"
                    class="flex-1 md:flex-none min-h-[44px] min-w-[80px] px-4 rounded-lg font-bold text-sm flex items-center justify-center gap-2 transition-all"
                    :class="currentView === view.id ? 'bg-surface text-primary shadow-sm ring-1 ring-text/10' : 'text-text-muted hover:text-text hover:bg-text/5'">
              <i :class="view.icon"></i>
              <span class="hidden sm:inline">{{ $t(`calendar.${view.id}`) || view.label }}</span>
            </button>
          </div>

        </div>
      </div>
    </div>

    <div class="flex-1 overflow-y-auto p-2 md:p-6 w-full">
      <div class="max-w-7xl mx-auto w-full">

        <div v-if="currentView === 'month'" class="space-y-8">
          
          <div class="max-w-5xl mx-auto bg-surface p-3 md:p-5 rounded-2xl border border-text/10 shadow-sm">
            <div class="grid grid-cols-7 gap-1 md:gap-2">
              <div v-for="day in dynamicWeekDays" :key="day" class="text-center text-xs font-bold text-text-muted uppercase mb-1 tracking-wider">
                {{ day }}
              </div>
              
              <div v-for="(dayObj, index) in calendarDays" :key="index"
                   @click="onDayClick(dayObj)"
                   class="relative min-h-[60px] md:min-h-[80px] p-1.5 md:p-2 rounded-lg transition-all cursor-pointer flex flex-col items-center justify-between border"
                   :class="[
                     dayObj.isCurrentMonth ? 'bg-white border-gray-300 shadow-sm hover:border-primary/60' : 'bg-gray-100 border-transparent opacity-50',
                     dayObj.isToday ? 'ring-2 ring-primary ring-offset-2 ring-offset-surface' : ''
                   ]">
                
                <div v-if="dayObj.isCurrentMonth" class="absolute top-0 left-0 right-0 h-1 bg-gray-200 rounded-t-lg overflow-hidden">
                  <div class="h-full bg-primary transition-all" :style="{ width: `${dayObj.loadPercentage}%` }"></div>
                </div>

                <span class="font-black text-sm md:text-base mt-1" :class="dayObj.isToday ? 'text-primary drop-shadow-sm' : 'text-gray-800'">
                  {{ dayObj.date.getDate() }}
                </span>

                <div class="flex gap-0.5 mt-auto pb-0.5 flex-wrap justify-center items-center">
                  <span v-for="n in Math.min(dayObj.appointmentCount, 4)" :key="n" 
                        class="w-1.5 h-1.5 rounded-full shadow-sm"
                        :class="dayObj.hasPending ? 'bg-red-500' : 'bg-green-500'"></span>
                  <span v-if="dayObj.appointmentCount > 4" class="text-[9px] text-gray-600 font-black ml-0.5">+</span>
                </div>
              </div>
            </div>
          </div>

          <div class="max-w-5xl mx-auto mt-8">
            <h3 class="font-bold text-xl text-text border-b border-text/10 pb-2 mb-4 flex items-center gap-2">
              <i class="pi pi-forward text-primary"></i> Következő várható foglalások
            </h3>
            
            <div v-if="upcomingAppointments.length > 0" class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div v-for="app in upcomingAppointments" :key="'upc-'+app.id" 
                   @click="openAppointmentDetails(app)"
                   class="bg-surface rounded-xl p-4 shadow-sm border border-text/10 flex flex-col gap-3 hover:border-primary/50 cursor-pointer transition-colors">
                
                <div class="flex justify-between items-start">
                  <div class="flex items-center gap-3">
                    <div class="w-3 h-3 rounded-full" :class="app.status === 0 ? 'bg-red-500' : 'bg-green-500'"></div>
                    <h4 class="text-md font-bold text-text">Vendég #{{ app.customerId }}</h4>
                  </div>
                  <div class="text-xs bg-background px-2 py-1 rounded text-text-muted font-bold capitalize">
                    {{ getDayNameShort(new Date(app.startDateTime)) }}, {{ formatDateShort(app.startDateTime) }}
                  </div>
                </div>

                <div class="flex items-center gap-4 text-text-muted text-xs font-bold">
                  <div class="flex items-center gap-1"><i class="pi pi-clock text-primary"></i> {{ formatTime(app.startDateTime) }}</div>
                  <div class="flex items-center gap-1"><i class="pi pi-hourglass text-primary"></i> {{ getDurationMinutes(app) }} p</div>
                </div>

                <div class="flex gap-1.5 mt-1">
                  <div v-for="item in app.items" :key="item.id" 
                       class="w-8 h-8 rounded-full bg-primary/10 text-primary flex items-center justify-center text-xs font-black border border-primary/20"
                       :title="mockServiceNames[item.serviceVariantId] || 'Ismeretlen'">
                    {{ getInitials(mockServiceNames[item.serviceVariantId]) }}
                  </div>
                </div>
              </div>
            </div>
            
            <div v-else class="text-center py-8 bg-surface rounded-xl border border-dashed border-text/10 text-text-muted">
              Nincsenek közelgő foglalások.
            </div>
          </div>

        </div>

        <div v-else-if="currentView === 'week'" class="space-y-6">
          <div class="bg-surface p-3 md:p-5 rounded-2xl border border-text/10 shadow-sm">
            <div class="grid grid-cols-7 gap-1 md:gap-2">
              <div v-for="dayObj in currentWeekDays" :key="dayObj.date.toISOString()"
                   @click="onDayClick(dayObj)"
                   class="relative min-h-[80px] md:min-h-[100px] p-2 rounded-xl border border-gray-300 bg-white hover:border-primary/50 cursor-pointer flex flex-col items-center justify-center transition-all shadow-sm"
                   :class="{ 'ring-2 ring-primary ring-offset-2 ring-offset-surface': dayObj.isToday }">
                <div class="absolute top-0 left-0 right-0 h-1 bg-gray-200 rounded-t-xl overflow-hidden">
                  <div class="h-full bg-primary" :style="{ width: `${dayObj.loadPercentage}%` }"></div>
                </div>
                <span class="text-xs text-gray-500 font-bold mt-1 uppercase">{{ getDayNameShort(dayObj.date) }}</span>
                <span class="font-black text-xl md:text-2xl" :class="dayObj.isToday ? 'text-primary' : 'text-gray-800'">{{ dayObj.date.getDate() }}</span>
                <div class="flex gap-1 mt-2">
                  <span v-for="n in Math.min(dayObj.appointmentCount, 3)" :key="n" class="w-1.5 h-1.5 rounded-full bg-primary shadow-sm"></span>
                </div>
              </div>
            </div>
          </div>

          <div class="max-w-5xl mx-auto mt-8">
            <h3 class="font-bold text-xl text-text border-b border-text/10 pb-2 mb-4 flex items-center gap-2">
              <i class="pi pi-calendar-clock text-primary"></i> A hét hátralévő foglalásai
            </h3>
            
            <div v-if="currentWeekUpcomingAppointments.length > 0" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              <div v-for="app in currentWeekUpcomingAppointments" :key="'week-'+app.id" 
                   @click="openAppointmentDetails(app)"
                   class="bg-surface rounded-xl p-4 shadow-sm border border-text/10 flex flex-col gap-3 hover:border-primary/50 cursor-pointer transition-colors">
                
                <div class="flex justify-between items-start">
                  <div class="flex items-center gap-3">
                    <div class="w-3 h-3 rounded-full" :class="app.status === 0 ? 'bg-red-500' : 'bg-green-500'"></div>
                    <h4 class="text-md font-bold text-text">Vendég #{{ app.customerId }}</h4>
                  </div>
                  <div class="text-xs bg-background px-2 py-1 rounded text-text-muted font-bold capitalize">
                    {{ getDayNameShort(new Date(app.startDateTime)) }}, {{ formatDateShort(app.startDateTime) }}
                  </div>
                </div>

                <div class="flex items-center gap-4 text-text-muted text-xs font-bold">
                  <div class="flex items-center gap-1"><i class="pi pi-clock text-primary"></i> {{ formatTime(app.startDateTime) }}</div>
                  <div class="flex items-center gap-1"><i class="pi pi-hourglass text-primary"></i> {{ getDurationMinutes(app) }} p</div>
                </div>

                <div class="flex gap-1.5 mt-1">
                  <div v-for="item in app.items" :key="item.id" 
                       class="w-8 h-8 rounded-full bg-primary/10 text-primary flex items-center justify-center text-xs font-black border border-primary/20"
                       :title="mockServiceNames[item.serviceVariantId] || 'Ismeretlen'">
                    {{ getInitials(mockServiceNames[item.serviceVariantId]) }}
                  </div>
                </div>
              </div>
            </div>
            
            <div v-else class="text-center py-8 bg-surface rounded-xl border border-dashed border-text/10 text-text-muted">
              Nincsenek hátralévő foglalások erre a hétre.
            </div>
          </div>
        </div>

        <div v-else-if="currentView === 'day'" class="space-y-8 bg-surface p-4 md:p-8 rounded-3xl border border-text/10 shadow-sm">
          <div class="flex justify-between items-end border-b border-text/10 pb-4">
            <div>
              <h3 class="text-2xl font-black text-primary capitalize">{{ getDayNameLong(currentDate) }}</h3>
              <p class="text-text font-medium">{{ formatDateLong(currentDate) }}</p>
            </div>
          </div>

          <div class="relative w-full mt-10 mb-12 select-none">
            
            <div v-for="(gap, index) in timelineData.gaps" :key="'gap-'+index" 
                 class="absolute -top-7 text-xs md:text-sm font-bold text-gray-800 flex justify-center items-center bg-white px-2 md:px-3 py-1 rounded-lg border border-gray-300 shadow-sm"
                 :style="{ left: gap.left + '%', width: gap.width + '%' }">
              {{ formatDurationStr(gap.durationMinutes) }}
            </div>

            <div class="w-full h-12 md:h-14 bg-white rounded-xl shadow-inner relative border border-gray-300 overflow-hidden">
              <div v-for="app in timelineData.appointments" :key="'tl-'+app.id"
                   @click="openAppointmentDetails(app)"
                   class="absolute top-0 bottom-0 h-full border-x border-white/50 cursor-pointer transition-transform hover:scale-y-105 flex items-center justify-center overflow-hidden shadow-sm"
                   :class="app.status === 0 ? 'bg-red-500/90 hover:bg-red-400' : 'bg-green-500/90 hover:bg-green-400'"
                   :style="{ left: app.left + '%', width: app.width + '%' }"
                   :title="`Vendég #${app.customerId}: ${formatTime(app.startDateTime)} - ${formatTime(app.endDateTime)}`">
                   <span v-if="app.width > 5" class="text-white text-xs font-bold truncate px-1">#{{ app.customerId }}</span>
              </div>
            </div>

            <div v-for="hour in timelineData.hours" :key="'hr-'+hour.val"
                 class="absolute top-14 md:top-16 text-xs md:text-sm font-bold text-text-muted -translate-x-1/2 flex flex-col items-center">
              <div class="w-0.5 h-2 bg-text/20 mb-1 rounded-full"></div>
              {{ hour.val }}
            </div>
          </div>

          <div class="mt-16 space-y-4">
            <h3 class="font-bold text-xl text-text border-b border-text/10 pb-2 flex items-center gap-2">
              <i class="pi pi-list"></i> Napi Foglalások Részletei
            </h3>
            
            <div v-for="app in currentDayAppointments" :key="'card-'+app.id" 
                 class="bg-background rounded-2xl p-4 md:p-6 shadow-sm border border-text/10 flex flex-col gap-4 hover:border-primary/30 transition-colors">
              <div class="flex justify-between items-start">
                <div class="flex items-center gap-3">
                  <div class="w-4 h-4 rounded-full" :class="app.status === 0 ? 'bg-red-500' : 'bg-green-500'"></div>
                  <h4 class="text-lg md:text-xl font-bold text-text">Vendég #{{ app.customerId }}</h4>
                </div>
                <div class="flex items-center gap-3 text-text-muted text-xs md:text-sm font-bold bg-surface px-3 py-1.5 rounded-lg border border-text/5">
                  <div class="flex items-center gap-1"><i class="pi pi-clock text-primary"></i> {{ formatTime(app.startDateTime) }}</div>
                  <span class="text-text/20">|</span>
                  <div class="flex items-center gap-1"><i class="pi pi-hourglass text-primary"></i> {{ getDurationMinutes(app) }} p</div>
                </div>
              </div>

              <div class="flex flex-wrap gap-2 mt-2">
                <div v-for="item in app.items" :key="item.id" 
                     class="w-10 h-10 md:w-12 md:h-12 rounded-full bg-primary/10 text-primary flex items-center justify-center font-black border border-primary/20 cursor-help hover:bg-primary hover:text-white transition-colors"
                     :title="mockServiceNames[item.serviceVariantId] || 'Ismeretlen'">
                  {{ getInitials(mockServiceNames[item.serviceVariantId]) }}
                </div>
              </div>

              <div class="bg-surface rounded-xl p-3 md:p-4 mt-2 border border-text/5">
                <h5 class="text-xs font-black text-text-muted mb-3 uppercase tracking-widest">Felhasználandó anyagok</h5>
                <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
                  <div v-for="mat in getMockMaterials(app.id)" :key="mat.id" class="flex items-center justify-between bg-background p-2.5 rounded-lg border border-text/5">
                    <div class="flex items-center gap-3">
                      <div class="w-8 h-8 bg-text/5 rounded flex items-center justify-center"><i class="pi pi-image text-text-muted"></i></div>
                      <span class="font-bold text-text text-sm">{{ mat.name }}</span>
                    </div>
                    <div class="font-black text-sm bg-surface px-2 py-1 rounded" :class="getMaterialStatusColor(mat.status)">{{ mat.quantity }} ml</div>
                  </div>
                </div>
              </div>
            </div>

            <div v-if="currentDayAppointments.length === 0" class="text-center py-12 bg-background rounded-2xl border border-dashed border-text/10 text-text-muted">
              <p class="font-medium">Nincs még foglalás erre a napra.</p>
            </div>

            <button class="w-full min-h-[56px] mt-6 flex items-center justify-center gap-2 bg-primary text-white rounded-xl font-bold text-lg hover:brightness-110 transition-all active:scale-95">
              <i class="pi pi-plus"></i> Új foglalás hozzáadása
            </button>
          </div>
        </div>

      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useAppointmentStore } from '@/stores/appointmentStore';

const store = useAppointmentStore();
const { locale } = useI18n();

const currentLang = computed(() => locale.value || 'hu-HU');

const views = [
  { id: 'day', icon: 'pi pi-clock', label: 'Napi' },
  { id: 'week', icon: 'pi pi-list', label: 'Heti' },
  { id: 'month', icon: 'pi pi-calendar', label: 'Havi' }
];
const currentView = ref('month');
const currentDate = ref(new Date());

const currentMonthName = computed(() => currentDate.value.toLocaleString(currentLang.value, { month: 'long' }).toUpperCase());
const currentYear = computed(() => currentDate.value.getFullYear());

const pagerText = computed(() => {
  if (currentView.value === 'month') {
    return `${currentMonthName.value} ${currentYear.value}`;
  }
  else if (currentView.value === 'day') {
    const d = currentDate.value;
    const month = d.toLocaleString(currentLang.value, { month: 'long' }).toUpperCase();
    return `${d.getFullYear()}. ${month} ${d.getDate()}.`;
  }
  else if (currentView.value === 'week') {
    if (currentWeekDays.value.length === 0) return `${currentMonthName.value} ${currentYear.value}`;

    const start = currentWeekDays.value[0].date;
    const end = currentWeekDays.value[6].date;

    const startMonth = start.toLocaleString(currentLang.value, { month: 'short' }).toUpperCase();
    const endMonth = end.toLocaleString(currentLang.value, { month: 'short' }).toUpperCase();

    if (start.getMonth() === end.getMonth()) {
      return `${start.getFullYear()}. ${startMonth} ${start.getDate()} - ${end.getDate()}.`;
    } else {
      return `${start.getFullYear()}. ${startMonth} ${start.getDate()} - ${endMonth} ${end.getDate()}.`;
    }
  }
  return '';
});

const getDayNameShort = (date) => date.toLocaleString(currentLang.value, { weekday: 'short' });
const getDayNameLong = (date) => date.toLocaleString(currentLang.value, { weekday: 'long' });
const formatDateLong = (date) => date.toLocaleDateString(currentLang.value, { year: 'numeric', month: 'long', day: 'numeric' });
const formatDateShort = (iso) => iso ? new Date(iso).toLocaleDateString(currentLang.value, { month: 'short', day: 'numeric' }) : '';

const dynamicWeekDays = computed(() => {
  const days = [];
  const baseDate = new Date(2024, 0, 1); 
  for (let i = 0; i < 7; i++) {
    const d = new Date(baseDate);
    d.setDate(d.getDate() + i);
    days.push(getDayNameShort(d));
  }
  return days;
});

const goPrev = () => {
  const d = new Date(currentDate.value);
  if (currentView.value === 'month') d.setMonth(d.getMonth() - 1);
  else if (currentView.value === 'week') d.setDate(d.getDate() - 7);
  else d.setDate(d.getDate() - 1);
  currentDate.value = d;
};
const goNext = () => {
  const d = new Date(currentDate.value);
  if (currentView.value === 'month') d.setMonth(d.getMonth() + 1);
  else if (currentView.value === 'week') d.setDate(d.getDate() + 7);
  else d.setDate(d.getDate() + 1);
  currentDate.value = d;
};
const goToday = () => { currentDate.value = new Date(); };

const calendarDays = computed(() => {
  const year = currentDate.value.getFullYear();
  const month = currentDate.value.getMonth();
  const firstDay = new Date(year, month, 1);
  let startingDay = firstDay.getDay() || 7; startingDay--; 
  
  const days = [];
  for (let i = 0; i < startingDay; i++) days.push(createDayObject(new Date(year, month, -startingDay + i + 1), false));
  for (let i = 1; i <= new Date(year, month + 1, 0).getDate(); i++) days.push(createDayObject(new Date(year, month, i), true));
  const remainingCells = 42 - days.length;
  for (let i = 1; i <= remainingCells; i++) days.push(createDayObject(new Date(year, month + 1, i), false));
  return days;
});

const currentWeekDays = computed(() => {
  const targetDateStr = currentDate.value.toDateString();
  const index = calendarDays.value.findIndex(d => d.date.toDateString() === targetDateStr);
  if (index === -1) return [];
  const start = index - (index % 7);
  return calendarDays.value.slice(start, start + 7);
});

const createDayObject = (date, isCurrentMonth) => {
  const isToday = date.toDateString() === new Date().toDateString();
  const targetStr = date.toDateString();
  const dayApps = store.appointments.filter(a => new Date(a.startDateTime).toDateString() === targetStr);
  
  return {
    date, isCurrentMonth, isToday,
    hasAppointments: dayApps.length > 0,
    appointmentCount: dayApps.length,
    hasPending: dayApps.some(a => a.status === 0),
    loadPercentage: dayApps.length > 0 ? (dayApps.length * 15) : 0
  };
};

const onDayClick = (dayObj) => {
  currentDate.value = dayObj.date;
  currentView.value = 'day';
};

// --- ÚJ: KÖVETKEZŐ 5 FOGLALÁS LOGIKA (HAVI NÉZET) ---
const upcomingAppointments = computed(() => {
  const now = new Date();
  now.setHours(0,0,0,0); 
  return store.appointments
    .filter(app => new Date(app.startDateTime) >= now)
    .sort((a, b) => new Date(a.startDateTime) - new Date(b.startDateTime))
    .slice(0, 5);
});

// --- ÚJ: KIVÁLASZTOTT HÉT HÁTRALÉVŐ FOGLALÁSAI (HETI NÉZET) ---
const currentWeekUpcomingAppointments = computed(() => {
  if (currentWeekDays.value.length === 0) return [];

  const now = new Date();
  now.setHours(0,0,0,0); 

  const weekStart = new Date(currentWeekDays.value[0].date);
  weekStart.setHours(0,0,0,0);

  const weekEnd = new Date(currentWeekDays.value[6].date);
  weekEnd.setHours(23,59,59,999);

  const filterStart = now > weekStart ? now : weekStart;

  return store.appointments
    .filter(app => {
      const appDate = new Date(app.startDateTime);
      return appDate >= filterStart && appDate <= weekEnd;
    })
    .sort((a, b) => new Date(a.startDateTime) - new Date(b.startDateTime));
});

// --- VÍZSZINTES IDŐSÁV ---
const currentDayAppointments = computed(() => {
  const targetDateStr = currentDate.value.toDateString();
  return store.appointments.filter(app => new Date(app.startDateTime).toDateString() === targetDateStr)
         .sort((a, b) => new Date(a.startDateTime) - new Date(b.startDateTime));
});

const timelineData = computed(() => {
  const apps = currentDayAppointments.value;
  let minMinutes = 8 * 60; let maxMinutes = 15 * 60;

  if (apps.length > 0) {
    const firstM = new Date(apps[0].startDateTime).getHours() * 60 + new Date(apps[0].startDateTime).getMinutes();
    if (firstM < minMinutes) minMinutes = Math.floor(firstM / 60) * 60;
    const lastM = new Date(apps[apps.length - 1].endDateTime).getHours() * 60 + new Date(apps[apps.length - 1].endDateTime).getMinutes();
    if (lastM > maxMinutes) maxMinutes = Math.ceil(lastM / 60) * 60;
  }

  const totalMinutes = maxMinutes - minMinutes;
  const hours = [];
  for (let m = minMinutes; m <= maxMinutes; m += 60) hours.push({ val: m / 60, left: ((m - minMinutes) / totalMinutes) * 100 });

  const mappedApps = apps.map(app => {
    const sDate = new Date(app.startDateTime); const eDate = new Date(app.endDateTime);
    const startM = sDate.getHours() * 60 + sDate.getMinutes(); const endM = eDate.getHours() * 60 + eDate.getMinutes();
    return { ...app, startM, endM, left: ((startM - minMinutes) / totalMinutes) * 100, width: ((endM - startM) / totalMinutes) * 100 };
  });

  const gaps = [];
  for (let i = 0; i < mappedApps.length - 1; i++) {
    const diff = mappedApps[i + 1].startM - mappedApps[i].endM;
    if (diff > 0) gaps.push({ durationMinutes: diff, left: ((mappedApps[i].endM - minMinutes) / totalMinutes) * 100, width: (diff / totalMinutes) * 100 });
  }

  return { minMinutes, maxMinutes, totalMinutes, hours, appointments: mappedApps, gaps };
});

const openAppointmentDetails = (app) => console.log("Megnyitás:", app.id);

const formatTime = (iso) => iso ? new Date(iso).toLocaleTimeString(currentLang.value, { hour: '2-digit', minute: '2-digit' }) : '';
const formatDurationStr = (m) => m >= 60 ? `${Math.floor(m / 60)}:${(m % 60).toString().padStart(2, '0')}` : `${m}p`;
const getDurationMinutes = (app) => Math.round((new Date(app.endDateTime) - new Date(app.startDateTime)) / 60000);
const mockServiceNames = { 1: "Férfi hajvágás gép és olló", 2: "Szakáll igazítás", 3: "Női mosás és szárítás" };
const getInitials = (name) => name ? name.split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase() : '?';

const getMockMaterials = () => [
  { id: 1, name: "Schwarzkopf Osis+ Wax", quantity: 15, status: "ok" },
  { id: 2, name: "Silver Sampon", quantity: 30, status: "warning" },
  { id: 3, name: "Hajlakk erős tartás", quantity: 50, status: "danger" }
];
const getMaterialStatusColor = (status) => status === 'ok' ? 'text-green-500' : status === 'warning' ? 'text-orange-500' : 'text-red-500';

const fetchDataForCurrentView = () => {
  const d = new Date(currentDate.value);
  const start = new Date(d.getFullYear(), d.getMonth() - 1, 1);
  const end = new Date(d.getFullYear(), d.getMonth() + 2, 0);
  store.fetchAppointments(start, end);
};

onMounted(() => { store.initUserPermissions(); fetchDataForCurrentView(); });
watch(currentDate, () => fetchDataForCurrentView());
</script>
