<template>
  <div class="py-2">
    <h3 class="text-xl font-bold mb-6 text-text">{{ $t('booking.yourDetails') }}</h3>

    <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
      <div class="flex flex-col gap-1.5">
        <label class="text-xs font-bold uppercase tracking-wider text-text-muted">{{ $t('booking.fullName') }}</label>
        <div class="relative flex items-center">
          <i class="pi pi-user absolute left-3.5 text-text-muted text-sm pointer-events-none"></i>
          <input v-model="localData.fullName"
                 type="text"
                 autocomplete="name"
                 class="w-full pl-10 pr-4 py-3 rounded-xl border border-text/15 bg-background text-text focus:border-primary focus:ring-1 focus:ring-primary/40 focus:outline-none transition-all" />
        </div>
      </div>

      <div class="flex flex-col gap-1.5">
        <label class="text-xs font-bold uppercase tracking-wider text-text-muted">{{ $t('booking.email') }}</label>
        <div class="relative flex items-center">
          <i class="pi pi-envelope absolute left-3.5 text-text-muted text-sm pointer-events-none"></i>
          <input v-model="localData.email"
                 type="email"
                 autocomplete="email"
                 class="w-full pl-10 pr-4 py-3 rounded-xl border border-text/15 bg-background text-text focus:border-primary focus:ring-1 focus:ring-primary/40 focus:outline-none transition-all" />
        </div>
      </div>

      <div class="flex flex-col gap-1.5 md:col-span-2">
        <label class="text-xs font-bold uppercase tracking-wider text-text-muted">{{ $t('booking.phone') }}</label>
        <div class="relative flex items-center">
          <i class="pi pi-phone absolute left-3.5 text-text-muted text-sm pointer-events-none"></i>
          <input v-model="localData.phone"
                 type="tel"
                 autocomplete="tel"
                 class="w-full pl-10 pr-4 py-3 rounded-xl border border-text/15 bg-background text-text focus:border-primary focus:ring-1 focus:ring-primary/40 focus:outline-none transition-all" />
        </div>
      </div>

      <div class="flex flex-col gap-1.5 md:col-span-2">
        <label class="text-xs font-bold uppercase tracking-wider text-text-muted">{{ $t('booking.notes') }}</label>
        <textarea v-model="localData.notes"
                  rows="3"
                  class="w-full p-3.5 rounded-xl border border-text/15 bg-background text-text focus:border-primary focus:ring-1 focus:ring-primary/40 focus:outline-none transition-all resize-none"></textarea>
      </div>
    </div>

    <div class="mt-8 pt-4 border-t border-text/10 flex justify-between items-center">
      <button type="button"
              @click="onPrev"
              class="min-h-[48px] px-6 py-2.5 border border-text/20 text-text font-semibold rounded-xl transition-colors hover:bg-text/5 cursor-pointer flex items-center gap-2">
        <i class="pi pi-arrow-left text-sm"></i> {{ $t('booking.back') }}
      </button>
      <button type="button"
              @click="onNext"
              :disabled="!isFormValid"
              class="min-h-[48px] px-7 py-2.5 bg-primary text-surface font-bold rounded-xl disabled:opacity-40 disabled:cursor-not-allowed transition-all hover:brightness-110 active:scale-95 shadow-md cursor-pointer flex items-center gap-2">
        <span>{{ $t('booking.next') }}</span>
        <i class="pi pi-arrow-right text-sm"></i>
      </button>
    </div>
  </div>
</template>

<script setup>
import { computed, watch, ref } from 'vue';

const props = defineProps({
  modelValue: { type: Object, required: true },
  onNext: { type: Function, required: true },
  onPrev: { type: Function, required: true }
});

const emit = defineEmits(['update:modelValue']);
const localData = ref({ ...props.modelValue });

watch(localData, (newValue) => {
  emit('update:modelValue', newValue);
}, { deep: true });

const isFormValid = computed(() => {
  return (localData.value.fullName?.length || 0) > 2 &&
         (localData.value.email?.includes('@') || false) &&
         (localData.value.phone?.length || 0) > 5;
});
</script>
