<template>
  <span v-if="safeSvg"
        class="inline-flex items-center justify-center leading-none select-none [&>svg]:w-full [&>svg]:h-full [&>svg]:stroke-current"
        :class="sizeClass"
        v-html="safeSvg"></span>
  <span v-else-if="fallbackText" class="leading-none select-none">{{ fallbackText }}</span>
  <i v-else :class="[fallbackIcon, 'leading-none select-none']"></i>
</template>

<script setup>
import { computed } from 'vue';

const props = defineProps({
  svg: { type: String, default: '' },
  fallbackText: { type: String, default: '' },
  fallbackIcon: { type: String, default: 'pi pi-sparkles' },
  sizeClass: { type: String, default: 'w-5 h-5' }
});

const safeSvg = computed(() => {
  const raw = (props.svg || '').trim();
  if (!raw || !raw.toLowerCase().startsWith('<svg') || !raw.toLowerCase().endsWith('</svg>')) {
    return '';
  }
  if (/script|onload|onerror|onclick|javascript:/i.test(raw)) {
    return '';
  }
  return raw;
});
</script>
