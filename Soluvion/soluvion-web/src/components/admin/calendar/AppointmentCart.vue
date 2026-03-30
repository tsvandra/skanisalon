<template>
  <div v-if="items.length > 0" class="space-y-2 mt-2">
    <h4 class="text-xs font-bold text-text-muted uppercase mb-2">{{ $t('calendar.editor.addedServices') }}</h4>

    <div v-for="(item, idx) in items" :key="idx" class="flex items-center justify-between bg-surface border border-text/10 p-2 md:p-3 rounded-lg shadow-sm">
      <div class="flex flex-col flex-1 mr-3">
        <span class="font-bold text-xs md:text-sm text-text">{{ item.name }}</span>
        <span class="text-[10px] md:text-xs text-text-muted font-medium">{{ item.price }} EUR</span>
      </div>
      <div class="flex items-center gap-2 md:gap-3">
        <div class="flex flex-col items-center">
          <ScrubbableInput v-model="item.duration" :min="5" :max="480" :step="5" :sensitivity="10" :suffix="$t('calendar.editor.durationSuffix')" class="h-8 w-20 text-xs" />
        </div>
        <button @click="$emit('remove', idx)" class="w-8 h-8 flex items-center justify-center bg-red-500/10 text-red-500 rounded-full hover:bg-red-500 hover:text-white transition-colors">
          <i class="pi pi-trash text-xs"></i>
        </button>
      </div>
    </div>

    <div class="flex justify-end gap-4 mt-3 pt-2 border-t border-text/5">
      <div class="text-right text-[10px] md:text-xs text-text-muted">
        {{ $t('calendar.editor.totalTime') }}<br><span class="text-sm md:text-base font-bold text-text">{{ computedTotals.duration }} {{ $t('calendar.editor.durationSuffix') }}</span>
      </div>
      <div class="text-right text-[10px] md:text-xs text-text-muted">
        {{ $t('calendar.editor.totalPrice') }}<br><span class="text-sm md:text-base font-bold text-primary">{{ computedTotals.price }} EUR</span>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';
import ScrubbableInput from '@/components/common/ScrubbableInput.vue';

const props = defineProps({
  items: { type: Array, required: true }
});

const emit = defineEmits(['remove']);

const computedTotals = computed(() => {
  return props.items.reduce((acc, item) => {
    acc.duration += parseInt(item.duration) || 0;
    acc.price += item.price || 0;
    return acc;
  }, { duration: 0, price: 0 });
});
</script>
