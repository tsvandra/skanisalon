<template>
  <div>
    <h3 class="text-xl font-bold mb-4">{{ $t('booking.selectDateTime') }}</h3>

    <div class="flex flex-col gap-4 max-w-sm mx-auto">
      <div class="flex flex-col gap-1">
        <label class="text-sm text-text-muted">{{ $t('booking.employeeLabel') }}</label>
        <select v-model="localEmployeeId" class="p-3 rounded border border-text/20 bg-background focus:border-primary focus:outline-none text-lg">
          <option :value="0">{{ $t('booking.anyone') }}</option>
        </select>
      </div>

      <div class="flex flex-col gap-1">
        <label class="text-sm text-text-muted">{{ $t('booking.dateTimeLabel') }}</label>
        <input v-model="localDateTime" type="datetime-local" class="p-3 rounded border border-text/20 bg-background focus:border-primary focus:outline-none text-lg" />
      </div>

      <div v-if="errorMessage" class="p-3 bg-red-100 text-red-700 rounded-lg text-sm border border-red-200">
        <i class="pi pi-exclamation-triangle mr-2"></i> {{ errorMessage }}
      </div>
    </div>

    <div class="mt-8 pt-4 border-t border-text/10 flex justify-between items-center">
      <button @click="onPrev" class="px-6 py-2 border border-text/20 text-text rounded-lg transition-colors hover:bg-text/5">
        <i class="pi pi-arrow-left mr-2"></i> {{ $t('booking.back') }}
      </button>
      <button @click="$emit('submit')" :disabled="!localDateTime || isSubmitting"
              class="px-8 py-3 bg-primary text-white font-bold rounded-xl text-lg shadow-md hover:scale-105 transition-transform disabled:opacity-50 disabled:hover:scale-100 flex items-center gap-2">
        <i v-if="isSubmitting" class="pi pi-spin pi-spinner"></i>
        {{ $t('booking.submit') }}
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
