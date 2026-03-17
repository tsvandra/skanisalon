<template>
  <div>
    <h3 class="text-xl font-bold mb-4">{{ $t('booking.yourDetails') }}</h3>
    <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
      <div class="flex flex-col gap-1">
        <label class="text-sm text-text-muted">{{ $t('booking.fullName') }}</label>
        <input v-model="localData.fullName" type="text" autocomplete="name" class="p-2 rounded border border-text/20 bg-background focus:border-primary focus:outline-none" />
      </div>
      <div class="flex flex-col gap-1">
        <label class="text-sm text-text-muted">{{ $t('booking.email') }}</label>
        <input v-model="localData.email" type="email" autocomplete="email" class="p-2 rounded border border-text/20 bg-background focus:border-primary focus:outline-none" />
      </div>
      <div class="flex flex-col gap-1 md:col-span-2">
        <label class="text-sm text-text-muted">{{ $t('booking.phone') }}</label>
        <input v-model="localData.phone" type="tel" autocomplete="tel" class="p-2 rounded border border-text/20 bg-background focus:border-primary focus:outline-none" />
      </div>
      <div class="flex flex-col gap-1 md:col-span-2">
        <label class="text-sm text-text-muted">{{ $t('booking.notes') }}</label>
        <textarea v-model="localData.notes" rows="2" class="p-2 rounded border border-text/20 bg-background focus:border-primary focus:outline-none"></textarea>
      </div>
    </div>

    <div class="mt-6 flex justify-between">
      <button @click="onPrev" class="px-6 py-2 border border-text/20 text-text rounded-lg transition-colors hover:bg-text/5">
        <i class="pi pi-arrow-left mr-2"></i> {{ $t('booking.back') }}
      </button>
      <button @click="onNext" :disabled="!isFormValid"
              class="px-6 py-2 bg-primary text-white font-bold rounded-lg disabled:opacity-50 transition-colors">
        {{ $t('booking.next') }} <i class="pi pi-arrow-right ml-2"></i>
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
