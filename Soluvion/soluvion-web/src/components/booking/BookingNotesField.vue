<template>
  <div class="flex flex-col gap-1.5">
    <label :for="inputId" class="text-xs font-bold uppercase tracking-wider text-text-muted flex items-center gap-1.5 cursor-pointer">
      <i class="pi pi-comment text-primary text-xs"></i>
      <span>{{ $t('booking.notes') }}</span>
    </label>

    <div class="relative flex items-start">
      <i class="pi pi-pencil absolute left-3.5 top-3.5 text-text-muted text-sm pointer-events-none"></i>
      <textarea :id="inputId"
                ref="textareaRef"
                :value="modelValue"
                @input="onInput"
                rows="1"
                name="salon_booking_remark_text"
                autocomplete="off"
                data-lpignore="true"
                data-1p-ignore="true"
                data-bwignore="true"
                data-form-type="other"
                :placeholder="$t('booking.notesPlaceholder')"
                :class="[
                  'w-full min-h-[44px] pl-10 pr-4 py-2.5 rounded-xl border border-text/15 bg-background text-text text-sm leading-relaxed placeholder:text-text-muted/60 focus:border-primary focus:ring-1 focus:ring-primary/40 focus:outline-none transition-[border-color,box-shadow] resize-none',
                  isOverflowing ? 'overflow-y-auto' : 'overflow-y-hidden'
                ]"></textarea>
    </div>
  </div>
</template>

<script setup>
import { ref, watch, nextTick, onMounted, onBeforeUnmount } from 'vue';

const props = defineProps({
  modelValue: {
    type: String,
    default: ''
  },
  maxRows: {
    type: Number,
    default: 5
  }
});

const emit = defineEmits(['update:modelValue']);

const inputId = `booking-notes-${Math.random().toString(36).slice(2, 8)}`;
const textareaRef = ref(null);
const isOverflowing = ref(false);
let resizeObserver = null;

const adjustHeight = () => {
  const el = textareaRef.value;
  if (!el) return;
  el.style.height = 'auto';
  if (!props.modelValue || props.modelValue.length === 0) {
    el.style.height = '44px';
    isOverflowing.value = false;
    return;
  }

  const computedStyle = window.getComputedStyle(el);
  const lineHeight = parseFloat(computedStyle.lineHeight) || 22;
  const paddingTop = parseFloat(computedStyle.paddingTop) || 10;
  const paddingBottom = parseFloat(computedStyle.paddingBottom) || 10;
  const borderTop = parseFloat(computedStyle.borderTopWidth) || 1;
  const borderBottom = parseFloat(computedStyle.borderBottomWidth) || 1;

  const maxHeight = Math.ceil(lineHeight * props.maxRows + paddingTop + paddingBottom + borderTop + borderBottom);
  const contentHeight = el.scrollHeight + borderTop + borderBottom;

  if (contentHeight > maxHeight) {
    el.style.height = `${maxHeight}px`;
    isOverflowing.value = true;
  } else {
    el.style.height = `${Math.max(contentHeight, 44)}px`;
    isOverflowing.value = false;
  }
};

const onInput = (e) => {
  emit('update:modelValue', e.target.value);
  nextTick(adjustHeight);
};

watch(() => props.modelValue, () => {
  nextTick(adjustHeight);
});

watch(() => props.maxRows, () => {
  nextTick(adjustHeight);
});

onMounted(() => {
  nextTick(adjustHeight);
  if (typeof ResizeObserver !== 'undefined' && textareaRef.value) {
    resizeObserver = new ResizeObserver(() => {
      adjustHeight();
    });
    resizeObserver.observe(textareaRef.value);
  }
});

onBeforeUnmount(() => {
  if (resizeObserver) {
    resizeObserver.disconnect();
    resizeObserver = null;
  }
});
</script>
