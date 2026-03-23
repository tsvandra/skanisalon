<template>
  <div class="relative flex items-center justify-center bg-background border border-text/20 rounded-lg px-4 cursor-ew-resize select-none hover:border-primary/50 transition-colors group overflow-hidden"
       style="touch-action: none;"
       @pointerdown="startDrag"
       title="Húzd balra/jobbra az érték változtatásához">
    <i class="pi pi-chevron-left absolute left-2 text-text/20 group-hover:text-primary/50 transition-colors text-[10px] pointer-events-none"></i>

    <div class="flex items-baseline gap-1 z-10 pointer-events-none">
      <span class="font-black text-lg text-text">{{ modelValue }}</span>
      <span v-if="suffix" class="text-xs text-text-muted font-bold uppercase">{{ suffix }}</span>
    </div>

    <i class="pi pi-chevron-right absolute right-2 text-text/20 group-hover:text-primary/50 transition-colors text-[10px] pointer-events-none"></i>
  </div>
</template>

<script setup>
import { ref, onUnmounted } from 'vue';

const props = defineProps({
  modelValue: { type: Number, required: true },
  min: { type: Number, default: 0 },
  max: { type: Number, default: 999 },
  step: { type: Number, default: 1 },
  suffix: { type: String, default: '' },
  sensitivity: { type: Number, default: 5 } // Hány pixel vízszintes húzás jelent egy "lépést"
});

const emit = defineEmits(['update:modelValue']);

const startX = ref(0);
const startValue = ref(0);
const isDragging = ref(false);

const startDrag = (event) => {
  isDragging.value = true;
  // Kezdeti pozíció és érték mentése
  startX.value = event.clientX || (event.touches ? event.touches[0].clientX : 0);
  startValue.value = props.modelValue;

  // Megakadályozzuk a szövegkijelölést húzás közben
  document.body.style.userSelect = 'none';

  window.addEventListener('pointermove', onDrag, { passive: false });
  window.addEventListener('pointerup', stopDrag);
  window.addEventListener('pointercancel', stopDrag);
};

const onDrag = (event) => {
  if (!isDragging.value) return;
  event.preventDefault(); // Megakadályozza a mobil görgetést húzás közben

  const currentX = event.clientX || (event.touches ? event.touches[0].clientX : 0);
  const deltaX = currentX - startX.value;

  // Kiszámoljuk a lépések számát
  const steps = Math.floor(deltaX / props.sensitivity);
  let newValue = startValue.value + (steps * props.step);

  // Határok ellenőrzése
  if (newValue < props.min) newValue = props.min;
  if (newValue > props.max) newValue = props.max;

  if (newValue !== props.modelValue) {
    emit('update:modelValue', newValue);
  }
};

const stopDrag = () => {
  isDragging.value = false;
  document.body.style.userSelect = '';
  window.removeEventListener('pointermove', onDrag);
  window.removeEventListener('pointerup', stopDrag);
  window.removeEventListener('pointercancel', stopDrag);
};

// Tisztítás, ha a komponens megsemmisül húzás közben
onUnmounted(() => {
  window.removeEventListener('pointermove', onDrag);
  window.removeEventListener('pointerup', stopDrag);
  window.removeEventListener('pointercancel', stopDrag);
});
</script>
