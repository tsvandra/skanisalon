<template>
  <div v-if="isOpen"
       class="fixed inset-0 bg-black/60 backdrop-blur-sm z-50 flex items-center justify-center p-2 md:p-4"
       @click="$emit('close')">
    <div class="bg-surface w-full max-w-3xl rounded-2xl shadow-2xl overflow-hidden border border-text/10 flex flex-col max-h-[92vh]"
         @click.stop>

      <!-- Fejléc -->
      <div class="p-4 md:p-6 border-b border-text/10 flex justify-between items-center bg-background/60">
        <h2 class="text-lg md:text-xl font-bold text-text flex items-center gap-2.5">
          <i class="pi pi-calendar-clock text-primary text-xl"></i>
          <span>{{ $t('calendar.schedule.modalTitle') }}</span>
        </h2>
        <button type="button"
                @click="$emit('close')"
                class="w-8 h-8 flex items-center justify-center rounded-full hover:bg-text/10 text-text-muted transition-colors cursor-pointer">
          <i class="pi pi-times"></i>
        </button>
      </div>

      <!-- Tartalom -->
      <div class="p-4 md:p-6 overflow-y-auto space-y-6">

        <!-- 1. Mód választó kártyák -->
        <div>
          <label class="block text-xs font-bold uppercase tracking-wider text-text-muted mb-3">
            {{ $t('calendar.schedule.modeLabel') }}
          </label>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
            <!-- Egyedileg meghirdetett napok -->
            <button type="button"
                    @click="localConfig.mode = 'custom_days'"
                    class="text-left p-4 rounded-2xl border-2 transition-all cursor-pointer flex flex-col gap-1.5"
                    :class="localConfig.mode === 'custom_days'
                      ? 'border-primary bg-primary/10 shadow-sm'
                      : 'border-text/10 bg-background/60 hover:border-primary/40'">
              <div class="flex items-center justify-between">
                <span class="font-bold text-sm md:text-base text-primary flex items-center gap-2">
                  <i class="pi pi-megaphone"></i>
                  <span>{{ $t('calendar.schedule.modeCustomDays') }}</span>
                </span>
                <span class="w-5 h-5 rounded-full border-2 flex items-center justify-center"
                      :class="localConfig.mode === 'custom_days' ? 'border-primary bg-primary text-surface' : 'border-text/30'">
                  <i v-if="localConfig.mode === 'custom_days'" class="pi pi-check text-[10px] font-bold"></i>
                </span>
              </div>
              <p class="text-xs text-text-muted leading-relaxed">
                {{ $t('calendar.schedule.modeCustomDaysDesc') }}
              </p>
            </button>

            <!-- Fix heti munkarend -->
            <button type="button"
                    @click="localConfig.mode = 'weekly_template'"
                    class="text-left p-4 rounded-2xl border-2 transition-all cursor-pointer flex flex-col gap-1.5"
                    :class="localConfig.mode === 'weekly_template'
                      ? 'border-primary bg-primary/10 shadow-sm'
                      : 'border-text/10 bg-background/60 hover:border-primary/40'">
              <div class="flex items-center justify-between">
                <span class="font-bold text-sm md:text-base text-primary flex items-center gap-2">
                  <i class="pi pi-clock"></i>
                  <span>{{ $t('calendar.schedule.modeWeekly') }}</span>
                </span>
                <span class="w-5 h-5 rounded-full border-2 flex items-center justify-center"
                      :class="localConfig.mode === 'weekly_template' ? 'border-primary bg-primary text-surface' : 'border-text/30'">
                  <i v-if="localConfig.mode === 'weekly_template'" class="pi pi-check text-[10px] font-bold"></i>
                </span>
              </div>
              <p class="text-xs text-text-muted leading-relaxed">
                {{ $t('calendar.schedule.modeWeeklyDesc') }}
              </p>
            </button>
          </div>
        </div>

        <!-- 1.5. Minimum előfoglalási idő (alapértelmezetten 2 óra = 120 perc) -->
        <div class="border-t border-text/10 pt-5">
          <div class="p-4 rounded-2xl border border-text/10 bg-background/60 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <div class="flex flex-col gap-1">
              <label class="text-xs md:text-sm font-bold text-text flex items-center gap-2">
                <i class="pi pi-stopwatch text-primary"></i>
                <span>{{ $t('calendar.schedule.minLeadTimeLabel') }}</span>
              </label>
              <p class="text-xs text-text-muted leading-relaxed">
                {{ $t('calendar.schedule.minLeadTimeDesc') }}
              </p>
            </div>
            <select v-model.number="localConfig.minLeadTimeMinutes"
                    class="h-10 px-3 rounded-xl border border-text/20 bg-surface text-xs md:text-sm font-bold text-text focus:border-primary focus:outline-none cursor-pointer shrink-0">
              <option :value="0">{{ $t('calendar.schedule.leadTime0') }}</option>
              <option :value="30">{{ $t('calendar.schedule.leadTime30') }}</option>
              <option :value="60">{{ $t('calendar.schedule.leadTime60') }}</option>
              <option :value="120">{{ $t('calendar.schedule.leadTime120') }}</option>
              <option :value="180">{{ $t('calendar.schedule.leadTime180') }}</option>
              <option :value="240">{{ $t('calendar.schedule.leadTime240') }}</option>
              <option :value="360">{{ $t('calendar.schedule.leadTime360') }}</option>
              <option :value="720">{{ $t('calendar.schedule.leadTime720') }}</option>
              <option :value="1440">{{ $t('calendar.schedule.leadTime1440') }}</option>
            </select>
          </div>
        </div>

        <!-- 2. Fix heti nyitvatartás szerkesztő (csak ha weekly_template mód aktív) -->
        <div v-if="localConfig.mode === 'weekly_template'" class="border-t border-text/10 pt-5">
          <h3 class="text-xs font-bold uppercase tracking-wider text-text-muted mb-3 flex items-center gap-2">
            <i class="pi pi-calendar text-primary"></i>
            <span>{{ $t('calendar.schedule.weeklyHoursTitle') }}</span>
          </h3>

          <div class="space-y-2.5">
            <div v-for="day in weekDaysList"
                 :key="day.key"
                 class="p-3 rounded-xl border border-text/10 bg-background/60 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
              <div class="flex items-center gap-3 min-w-[160px]">
                <button type="button"
                        @click="toggleWeekday(day.key)"
                        class="px-3 py-1.5 rounded-lg text-xs font-bold transition-all cursor-pointer border"
                        :class="localConfig.weeklyHours[day.key]?.isOpen
                          ? 'bg-emerald-500/15 text-emerald-600 border-emerald-500/40'
                          : 'bg-text/5 text-text-muted border-text/15'">
                  {{ localConfig.weeklyHours[day.key]?.isOpen ? $t('calendar.schedule.open') : $t('calendar.schedule.closed') }}
                </button>
                <span class="font-bold text-sm capitalize text-text">{{ day.label }}</span>
              </div>

              <div v-if="localConfig.weeklyHours[day.key]?.isOpen" class="flex flex-wrap items-center gap-2">
                <div v-for="(iv, idx) in localConfig.weeklyHours[day.key].intervals"
                     :key="idx"
                     class="flex items-center gap-1.5 bg-surface px-2.5 py-1.5 rounded-lg border border-text/15">
                  <select v-model="iv.start"
                          class="bg-transparent text-xs md:text-sm font-bold text-text focus:outline-none cursor-pointer">
                    <option v-for="tOpt in timeOptions" :key="'s-'+tOpt" :value="tOpt" class="bg-surface text-text">{{ tOpt }}</option>
                  </select>
                  <span class="text-text-muted font-bold">–</span>
                  <select v-model="iv.end"
                          class="bg-transparent text-xs md:text-sm font-bold text-text focus:outline-none cursor-pointer">
                    <option v-for="tOpt in timeOptions" :key="'e-'+tOpt" :value="tOpt" class="bg-surface text-text">{{ tOpt }}</option>
                  </select>
                  <button v-if="localConfig.weeklyHours[day.key].intervals.length > 1"
                          type="button"
                          @click="removeWeekdayInterval(day.key, idx)"
                          class="text-red-500 hover:bg-red-500/10 w-6 h-6 rounded flex items-center justify-center cursor-pointer ml-1">
                    <i class="pi pi-trash text-xs"></i>
                  </button>
                </div>

                <button v-if="localConfig.weeklyHours[day.key].intervals.length < 3"
                        type="button"
                        @click="addWeekdayInterval(day.key)"
                        class="px-2.5 py-1.5 rounded-lg text-xs font-bold text-primary border border-dashed border-primary/40 hover:bg-primary/10 transition-colors cursor-pointer">
                  {{ $t('calendar.schedule.addInterval') }}
                </button>
              </div>
              <div v-else class="text-xs text-text-muted italic">
                {{ $t('calendar.schedule.dayStatusClosed') }}
              </div>
            </div>
          </div>
        </div>

        <!-- 3. Egyedileg megnyitott / módosított napok áttekintése -->
        <div class="border-t border-text/10 pt-5">
          <h3 class="text-xs font-bold uppercase tracking-wider text-text-muted mb-3 flex items-center gap-2">
            <i class="pi pi-bookmark text-primary"></i>
            <span>{{ $t('calendar.schedule.customOverridesTitle') }}</span>
          </h3>

          <div v-if="sortedOverrides.length > 0" class="grid grid-cols-1 sm:grid-cols-2 gap-2.5">
            <div v-for="item in sortedOverrides"
                 :key="item.date"
                 class="p-3 rounded-xl border border-text/10 bg-background/70 flex items-center justify-between gap-2">
              <div class="flex flex-col">
                <span class="text-xs md:text-sm font-bold text-text capitalize">{{ formatOverrideDate(item.date) }}</span>
                <span v-if="item.schedule.isOpen" class="text-xs font-bold text-emerald-600">
                  {{ item.schedule.intervals.map(i => `${i.start}–${i.end}`).join(', ') }}
                </span>
                <span v-else class="text-xs font-semibold text-red-500">
                  {{ $t('calendar.schedule.closed') }}
                </span>
              </div>
              <button type="button"
                      @click="removeDateOverride(item.date)"
                      class="w-7 h-7 rounded-lg text-red-500 hover:bg-red-500/10 flex items-center justify-center transition-colors cursor-pointer"
                      :title="$t('common.delete')">
                <i class="pi pi-trash text-xs"></i>
              </button>
            </div>
          </div>
          <div v-else class="p-4 rounded-xl bg-background/50 border border-dashed border-text/15 text-xs text-text-muted text-center">
            {{ $t('calendar.schedule.noOverridesYet') }}
          </div>
        </div>

      </div>

      <!-- Lábléc -->
      <div class="p-4 border-t border-text/10 bg-background/60 flex justify-end gap-2.5">
        <button type="button"
                @click="$emit('close')"
                class="px-4 h-11 rounded-xl text-sm font-bold text-text hover:bg-text/10 transition-colors cursor-pointer">
          {{ $t('common.cancel') }}
        </button>
        <button type="button"
                @click="saveConfig"
                :disabled="isSaving"
                class="px-6 h-11 rounded-xl text-sm font-bold bg-primary text-surface hover:brightness-110 transition-all shadow-md flex items-center gap-2 cursor-pointer disabled:opacity-50">
          <i class="pi" :class="isSaving ? 'pi-spin pi-spinner' : 'pi-check'"></i>
          <span>{{ $t('common.save') }}</span>
        </button>
      </div>

    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import bookingApi from '@/services/bookingApi';

const props = defineProps({
  isOpen: { type: Boolean, default: false },
  scheduleConfig: { type: Object, required: true }
});

const emit = defineEmits(['close', 'saved']);
const { locale } = useI18n();

const isSaving = ref(false);

const buildDefaultLocalConfig = (src) => {
  const clone = JSON.parse(JSON.stringify(src || {}));
  if (!clone.mode) clone.mode = 'weekly_template';
  if (typeof clone.minLeadTimeMinutes !== 'number') clone.minLeadTimeMinutes = 120;
  if (!clone.weeklyHours) clone.weeklyHours = {};
  for (let d = 1; d <= 7; d++) {
    const k = String(d);
    if (!clone.weeklyHours[k]) {
      clone.weeklyHours[k] = {
        isOpen: d <= 5,
        intervals: [{ start: '08:00', end: d <= 5 ? '17:00' : '14:00' }]
      };
    } else if (!Array.isArray(clone.weeklyHours[k].intervals) || clone.weeklyHours[k].intervals.length === 0) {
      clone.weeklyHours[k].intervals = [{ start: '08:00', end: '17:00' }];
    }
  }
  if (!clone.dateOverrides) clone.dateOverrides = {};
  return clone;
};

const localConfig = ref(buildDefaultLocalConfig(props.scheduleConfig));

watch(() => props.isOpen, (open) => {
  if (open) {
    localConfig.value = buildDefaultLocalConfig(props.scheduleConfig);
  }
});

// 24 órás időpont opciók 15 perces lépésekben (06:00 - 22:00)
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

const weekDaysList = computed(() => {
  const list = [];
  for (let i = 0; i < 7; i++) {
    const d = new Date(2024, 0, 1 + i); // 2024-01-01 Hétfő
    list.push({
      key: String(i + 1),
      label: d.toLocaleDateString(locale.value || 'hu', { weekday: 'long' })
    });
  }
  return list;
});

const toggleWeekday = (key) => {
  const item = localConfig.value.weeklyHours[key];
  if (item) {
    item.isOpen = !item.isOpen;
    if (item.isOpen && (!item.intervals || item.intervals.length === 0)) {
      item.intervals = [{ start: '08:00', end: '17:00' }];
    }
  }
};

const addWeekdayInterval = (key) => {
  const item = localConfig.value.weeklyHours[key];
  if (item && item.intervals.length < 3) {
    item.intervals.push({ start: '13:00', end: '17:00' });
  }
};

const removeWeekdayInterval = (key, idx) => {
  const item = localConfig.value.weeklyHours[key];
  if (item && item.intervals.length > 1) {
    item.intervals.splice(idx, 1);
  }
};

const sortedOverrides = computed(() => {
  const overrides = localConfig.value.dateOverrides || {};
  return Object.keys(overrides)
    .sort()
    .map(date => ({ date, schedule: overrides[date] }));
});

const removeDateOverride = (dateKey) => {
  if (localConfig.value.dateOverrides) {
    delete localConfig.value.dateOverrides[dateKey];
  }
};

const formatOverrideDate = (dateStr) => {
  const [y, m, d] = dateStr.split('-').map(Number);
  const dt = new Date(y, m - 1, d);
  return dt.toLocaleDateString(locale.value || 'hu', { year: 'numeric', month: 'short', day: 'numeric', weekday: 'short' });
};

const saveConfig = async () => {
  if (isSaving.value) return;
  isSaving.value = true;
  try {
    const res = await bookingApi.updateScheduleConfig(localConfig.value);
    emit('saved', res.data);
    emit('close');
  } catch (err) {
    console.error('Hiba a munkarend mentésekor:', err);
  } finally {
    isSaving.value = false;
  }
};
</script>
