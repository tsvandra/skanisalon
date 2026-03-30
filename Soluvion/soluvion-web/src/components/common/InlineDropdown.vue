<template>
  <div class="relative flex items-center justify-end w-full shrink-0" @click.stop="$emit('toggle')">
    <div class="w-full bg-transparent text-xs font-bold cursor-pointer pr-4 text-right truncate transition-all select-none flex items-center justify-end"
         :class="modelValue ? 'text-text' : 'text-text-muted hover:text-text'">
      {{ selectedLabel || placeholder }}
    </div>

    <i class="pi pi-chevron-down absolute right-0 top-1/2 -translate-y-1/2 text-[10px] pointer-events-none transition-transform duration-200"
       :class="[modelValue ? 'text-primary' : 'text-text-muted group-hover:text-text', { 'rotate-180': isOpen }]"></i>

    <div v-if="isOpen"
         class="absolute right-0 top-full mt-1 w-48 bg-surface border border-text/10 rounded-lg shadow-xl z-50 overflow-hidden animate-fade-in text-left">
      <div class="max-h-40 overflow-y-auto [&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar-thumb]:bg-text/20">
        <div v-for="opt in options" :key="opt.value"
             @click.stop="handleSelect(opt.value)"
             class="px-3 py-2 text-xs font-bold cursor-pointer transition-colors flex justify-between items-center"
             :class="modelValue === opt.value ? 'bg-primary text-white' : 'text-text hover:bg-text/5'">
          <span class="truncate">{{ opt.label }}</span>
          <span v-if="opt.hint" class="opacity-70 text-[10px] shrink-0 ml-2">{{ opt.hint }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
  import { computed } from 'vue';

  const props = defineProps({
    modelValue: { type: [String, Number], default: null },
    options: { type: Array, required: true },
    placeholder: { type: String, default: '' }, 
    isOpen: { type: Boolean, default: false }
  });

  const emit = defineEmits(['update:modelValue', 'toggle', 'select']);

  const selectedLabel = computed(() => {
    const selected = props.options.find(o => o.value === props.modelValue);
    if (selected) {
      return selected.hint ? `${selected.label} (${selected.hint})` : selected.label;
    }
    return '';
  });

  const handleSelect = (val) => {
    emit('update:modelValue', val);
    emit('select', val);
  };
</script>

<style scoped>
  .select-none {
    user-select: none;
  }

  .animate-fade-in {
    animation: fadeIn 0.2s ease-out;
    transform-origin: top right;
  }

  @keyframes fadeIn {
    from {
      opacity: 0;
      transform: scaleY(0.9) translateY(-5px);
    }

    to {
      opacity: 1;
      transform: scaleY(1) translateY(0);
    }
  }
</style>
