<template>
  <div>
    <label class="block text-[10px] md:text-xs font-bold text-text-muted mb-1.5 uppercase flex items-center gap-1">
      <i class="pi pi-user"></i> {{ $t('calendar.editor.client') }}
    </label>
    <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
      <div class="relative transition-all" :class="{'md:col-span-2': modelValue !== 'new'}">
        <select :value="modelValue" @change="handleChange" class="w-full h-[44px] bg-background border border-text/20 rounded-lg px-3 text-sm text-text font-bold focus:outline-none focus:border-primary appearance-none cursor-pointer">
          <option value="" disabled>{{ $t('calendar.editor.selectFromList') }}</option>
          <option value="new" class="text-primary font-bold">{{ $t('calendar.editor.addNewClient') }}</option>
          <option v-for="c in customersList" :key="c.id" :value="c.id">{{ c.name }}</option>
        </select>
        <i class="pi pi-chevron-down absolute right-3 top-1/2 -translate-y-1/2 text-text-muted pointer-events-none text-sm"></i>
      </div>

      <div v-if="modelValue === 'new'" class="flex flex-col gap-2">
        <input type="text" :value="customerFullName" @input="$emit('update:customerFullName', $event.target.value)" :placeholder="$t('calendar.editor.fullNameOptional')" class="w-full h-[44px] bg-background border border-text/20 rounded-lg px-3 text-sm text-text focus:outline-none focus:border-primary">
        <input type="tel" :value="customerPhone" @input="$emit('update:customerPhone', $event.target.value)" :placeholder="$t('calendar.editor.phoneOptional')" class="w-full h-[44px] bg-background border border-text/20 rounded-lg px-3 text-sm text-text focus:outline-none focus:border-primary">
        <span class="text-[10px] text-text-muted leading-tight">{{ $t('calendar.editor.clientValidationWarning') }}</span>
      </div>
    </div>
  </div>
</template>

<script setup>
const props = defineProps({
  modelValue: { type: [String, Number], required: true }, // customerId
  customerFullName: { type: String, default: '' },
  customerPhone: { type: String, default: '' },
  customersList: { type: Array, required: true }
});

const emit = defineEmits(['update:modelValue', 'update:customerFullName', 'update:customerPhone']);

const handleChange = (e) => {
  const val = e.target.value;
  emit('update:modelValue', val);

  if (val === 'new') {
    emit('update:customerFullName', '');
    emit('update:customerPhone', '');
  } else {
    const c = props.customersList.find(x => x.id.toString() === val.toString());
    if (c) emit('update:customerFullName', c.name);
  }
};
</script>
