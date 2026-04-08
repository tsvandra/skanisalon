<template>
  <div v-if="items.length > 0" class="space-y-2 mt-2">
    <h4 class="text-xs font-bold text-text-muted uppercase mb-2">{{ $t('calendar.editor.addedServices') }}</h4>

    <div v-for="(item, idx) in items" :key="idx" class="flex flex-col sm:flex-row sm:items-center justify-between bg-surface border border-text/10 p-2 md:p-3 rounded-lg shadow-sm gap-2">
      <div class="flex flex-col flex-1 mr-3">
        <span class="font-bold text-xs md:text-sm text-text">{{ item.name }}</span>
      </div>

      <div class="flex items-center justify-end gap-2 md:gap-3 shrink-0">
        <div class="flex flex-col items-center">
          <ScrubbableInput v-model="item.price" :min="0" :max="10000" :step="0.5" :decimals="2" suffix="EUR" class="h-8 w-20 text-xs text-primary bg-primary/5" />
        </div>

        <div class="flex flex-col items-center">
          <ScrubbableInput v-model="item.duration" :min="5" :max="480" :step="5" :sensitivity="10" :suffix="$t('calendar.editor.durationSuffix')" class="h-8 w-20 text-xs" />
        </div>

        <button @click="$emit('remove', idx)" class="w-8 h-8 flex items-center justify-center bg-red-500/10 text-red-500 rounded-full hover:bg-red-500 hover:text-white transition-colors shrink-0">
          <i class="pi pi-trash text-xs"></i>
        </button>
      </div>
    </div>

    <div class="flex justify-end gap-4 mt-3 pt-3 border-t border-text/5">
      <div class="text-right text-[10px] md:text-xs text-text-muted flex flex-col items-end justify-center">
        <span>{{ $t('calendar.editor.totalTime') }}</span>
        <span class="text-sm md:text-base font-bold text-text mt-1">{{ totalDuration }} {{ $t('calendar.editor.durationSuffix') }}</span>
      </div>

      <div class="text-right text-[10px] md:text-xs text-text-muted flex flex-col items-end justify-center">
        <span>{{ $t('calendar.editor.totalPrice') }}</span>
        <ScrubbableInput v-model="totalPrice" :min="0" :max="50000" :step="1" :decimals="2" suffix="EUR" class="h-8 md:h-9 w-24 md:w-28 text-sm md:text-base font-bold text-primary bg-primary/10 border-primary/30 mt-1" />
      </div>
    </div>
    <div class="text-right text-[9px] text-text-muted/60 -mt-1 mr-1">Húzd a csúszkát vagy kattints az ár felülbírálásához</div>
  </div>
</template>

<script setup>
  import { computed } from 'vue';
  import ScrubbableInput from '@/components/common/ScrubbableInput.vue';

  const props = defineProps({
    items: { type: Array, required: true }
  });

  const emit = defineEmits(['remove']);

  // Csak Getter az összidőhöz
  const totalDuration = computed(() => {
    return props.items.reduce((acc, item) => acc + (parseInt(item.duration) || 0), 0);
  });

  // Zseniális Getter/Setter a Végösszeg súlyozott szétosztásához
  const totalPrice = computed({
    get: () => {
      return props.items.reduce((acc, item) => acc + (parseFloat(item.price) || 0), 0);
    },
    set: (newTotal) => {
      const oldTotal = props.items.reduce((acc, item) => acc + (parseFloat(item.price) || 0), 0);

      // 1. Eset: A régi ár nulla volt (nem tudunk súlyozni) -> Egyenlő elosztás
      if (oldTotal === 0) {
        const splitValue = newTotal / (props.items.length || 1);
        props.items.forEach(item => {
          item.price = Number(splitValue.toFixed(2));
        });
      }
      // 2. Eset: Arányos (Súlyozott) elosztás a régi árakhoz képest
      else {
        const ratio = newTotal / oldTotal;
        props.items.forEach(item => {
          item.price = Number((parseFloat(item.price) * ratio).toFixed(2));
        });
      }

      // Tizedes Kerekítési Hiba korrekciója az első elemen (hogy a szumma fillérre pontosan a beírt összeg legyen)
      const currentSum = props.items.reduce((acc, item) => acc + parseFloat(item.price), 0);
      const diff = Number((newTotal - currentSum).toFixed(2));

      if (Math.abs(diff) > 0 && props.items.length > 0) {
        props.items[0].price = Number((parseFloat(props.items[0].price) + diff).toFixed(2));
      }
    }
  });
</script>
