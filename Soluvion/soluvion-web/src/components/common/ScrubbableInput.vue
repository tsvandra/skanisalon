<template>
  <div class="relative flex items-center justify-center bg-background border border-text/20 rounded-lg px-2 cursor-ew-resize select-none hover:border-primary/50 transition-colors group overflow-hidden"
       style="touch-action: none;"
       @pointerdown="startDrag"
       title="Húzd balra/jobbra, vagy KATTINTS a gépeléshez">

    <template v-if="!isEditing">
      <i class="pi pi-chevron-left absolute left-1.5 text-text/20 group-hover:text-primary/50 transition-colors text-[10px] pointer-events-none"></i>

      <div class="flex items-baseline gap-1 z-10 pointer-events-none">
        <span class="font-black text-sm md:text-base text-inherit">{{ formattedValue }}</span>
        <span v-if="suffix" class="text-[10px] text-text-muted font-bold uppercase">{{ suffix }}</span>
      </div>

      <i class="pi pi-chevron-right absolute right-1.5 text-text/20 group-hover:text-primary/50 transition-colors text-[10px] pointer-events-none"></i>
    </template>

    <template v-else>
      <input ref="inputRef"
             type="number"
             :step="step"
             v-model="inputValue"
             @blur="saveEdit"
             @keydown.enter="saveEdit"
             class="w-full h-full bg-transparent border-none text-center font-black text-sm md:text-base text-inherit focus:outline-none focus:ring-0 p-0 m-0 z-20 hide-arrows" />
    </template>
  </div>
</template>

<script setup>
  import { ref, computed, nextTick } from 'vue';

  const props = defineProps({
    modelValue: { type: Number, required: true },
    min: { type: Number, default: 0 },
    max: { type: Number, default: 99999 },
    step: { type: Number, default: 1 },
    suffix: { type: String, default: '' },
    sensitivity: { type: Number, default: 5 },
    decimals: { type: Number, default: 0 } // ÚJ: Tizedesjegyek száma
  });

  const emit = defineEmits(['update:modelValue']);

  const isDragging = ref(false);
  const isEditing = ref(false);
  const inputRef = ref(null);
  const inputValue = ref(props.modelValue);

  const startX = ref(0);
  const startValue = ref(0);

  // Változók a sima kattintás felismeréséhez
  let clickStartX = 0;
  let clickStartTime = 0;

  const formattedValue = computed(() => {
    return Number(props.modelValue).toFixed(props.decimals);
  });

  const startDrag = (event) => {
    if (isEditing.value) return;

    isDragging.value = true;
    startX.value = event.clientX || (event.touches ? event.touches[0].clientX : 0);
    startValue.value = props.modelValue;

    clickStartX = startX.value;
    clickStartTime = Date.now();

    document.body.style.userSelect = 'none';
    window.addEventListener('pointermove', onDrag, { passive: false });
    window.addEventListener('pointerup', stopDrag);
    window.addEventListener('pointercancel', stopDrag);
  };

  const onDrag = (event) => {
    if (!isDragging.value) return;
    event.preventDefault();

    const currentX = event.clientX || (event.touches ? event.touches[0].clientX : 0);
    const deltaX = currentX - startX.value;

    const steps = Math.floor(deltaX / props.sensitivity);
    let newValue = startValue.value + (steps * props.step);

    if (newValue < props.min) newValue = props.min;
    if (newValue > props.max) newValue = props.max;

    if (newValue !== props.modelValue) {
      // Kerekítés tizedesjegy pontosságra
      emit('update:modelValue', Number(newValue.toFixed(props.decimals)));
    }
  };

  const stopDrag = (event) => {
    isDragging.value = false;
    document.body.style.userSelect = '';
    window.removeEventListener('pointermove', onDrag);
    window.removeEventListener('pointerup', stopDrag);
    window.removeEventListener('pointercancel', stopDrag);

    // Kattintás (Click) felismerése: Ha nem húzta el az egeret, és gyorsan engedte fel
    const currentX = event.clientX || (event.changedTouches ? event.changedTouches[0].clientX : 0) || clickStartX;
    const timeDiff = Date.now() - clickStartTime;

    if (Math.abs(currentX - clickStartX) < 5 && timeDiff < 400) {
      inputValue.value = props.decimals > 0 ? Number(props.modelValue).toFixed(props.decimals) : props.modelValue;
      isEditing.value = true;
      nextTick(() => {
        if (inputRef.value) {
          inputRef.value.focus();
          inputRef.value.select();
        }
      });
    }
  };

  const saveEdit = () => {
    isEditing.value = false;
    let val = parseFloat(inputValue.value);

    if (isNaN(val)) val = props.min;
    if (val < props.min) val = props.min;
    if (val > props.max) val = props.max;

    emit('update:modelValue', Number(val.toFixed(props.decimals)));
  };
</script>

<style scoped>
  .hide-arrows::-webkit-outer-spin-button,
  .hide-arrows::-webkit-inner-spin-button {
    -webkit-appearance: none;
    margin: 0;
  }

  .hide-arrows {
    -moz-appearance: textfield;
  }
</style>
