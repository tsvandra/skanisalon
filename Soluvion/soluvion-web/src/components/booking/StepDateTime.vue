<template>
  <div class="py-2">
    <h3 class="text-xl font-bold mb-6 text-text">{{ $t('booking.selectDateTime') }}</h3>

    <div class="flex flex-col gap-5 max-w-md mx-auto">
      <div class="flex flex-col gap-1.5">
        <label class="text-xs font-bold uppercase tracking-wider text-text-muted">{{ $t('booking.employeeLabel') }}</label>
        <div class="relative flex items-center">
          <i class="pi pi-id-card absolute left-3.5 text-text-muted text-sm pointer-events-none"></i>
          <select v-model="localEmployeeId"
                  class="w-full pl-10 pr-4 py-3.5 rounded-xl border border-text/15 bg-background text-text focus:border-primary focus:ring-1 focus:ring-primary/40 focus:outline-none text-base cursor-pointer transition-all">
            <option :value="0">{{ $t('booking.anyone') }}</option>
          </select>
        </div>
      </div>

      <div class="flex flex-col gap-1.5">
        <label class="text-xs font-bold uppercase tracking-wider text-text-muted">{{ $t('booking.dateTimeLabel') }}</label>
        <div class="relative flex items-center">
          <i class="pi pi-calendar absolute left-3.5 text-text-muted text-sm pointer-events-none"></i>
          <input v-model="localDateTime"
                 type="datetime-local"
                 @click="$event.target.showPicker?.()"
                 class="w-full pl-10 pr-4 py-3.5 rounded-xl border border-text/15 bg-background text-text focus:border-primary focus:ring-1 focus:ring-primary/40 focus:outline-none text-base cursor-pointer transition-all" />
        </div>
      </div>

      <div v-if="errorMessage" class="p-3.5 bg-red-500/10 text-red-500 rounded-xl text-sm border border-red-500/30 flex items-center gap-2 font-medium">
        <i class="pi pi-exclamation-triangle shrink-0"></i>
        <span>{{ errorMessage }}</span>
      </div>
    </div>

    <div class="mt-8 pt-4 border-t border-text/10 flex justify-between items-center">
      <button type="button"
              @click="onPrev"
              class="min-h-[48px] px-6 py-2.5 border border-text/20 text-text font-semibold rounded-xl transition-colors hover:bg-text/5 cursor-pointer flex items-center gap-2">
        <i class="pi pi-arrow-left text-sm"></i> {{ $t('booking.back') }}
      </button>
      <button type="button"
              @click="$emit('submit')"
              :disabled="!localDateTime || isSubmitting"
              class="min-h-[48px] px-8 py-3 bg-primary text-surface font-bold rounded-xl text-base shadow-md hover:brightness-110 active:scale-95 transition-all disabled:opacity-40 disabled:cursor-not-allowed flex items-center gap-2 cursor-pointer">
        <i v-if="isSubmitting" class="pi pi-spin pi-spinner"></i>
        <i v-else class="pi pi-check"></i>
        <span>{{ $t('booking.submit') }}</span>
      </button>
    </div>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue';

const props = defineProps({
  employeeId: { type: Number, required: true },
  selectedDateTime: { type: String, required: true },
  isSubmitting: { type: Boolean, default: false },
  errorMessage: { type: String, default: '' },
  onPrev: { type: Function, required: true }
});

const emit = defineEmits(['update:employeeId', 'update:selectedDateTime', 'submit']);

const localEmployeeId = ref(props.employeeId);
const localDateTime = ref(props.selectedDateTime);

watch(localEmployeeId, (val) => emit('update:employeeId', val));
watch(localDateTime, (val) => emit('update:selectedDateTime', val));
</script>
