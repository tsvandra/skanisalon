<script setup>
  import { computed } from 'vue';
  import { useI18n } from 'vue-i18n';

  const props = defineProps({
    companyData: {
      type: Object,
      required: true
    }
  });

  const emit = defineEmits(['save']);

  const { t, locale } = useI18n();

  const isEnabled = computed(() => !!props.companyData.stockTrackingStartDate);

  // A backend UTC ISO-t ad vissza; a datetime-local input helyi időt vár ("YYYY-MM-DDTHH:mm")
  const pad = (n) => String(n).padStart(2, '0');
  const toLocalInput = (iso) => {
    if (!iso) return '';
    const d = new Date(iso.endsWith('Z') || /[+-]\d{2}:?\d{2}$/.test(iso) ? iso : iso + 'Z');
    if (isNaN(d.getTime())) return '';
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  };

  const localValue = computed({
    get: () => toLocalInput(props.companyData.stockTrackingStartDate),
    set: (val) => {
      props.companyData.stockTrackingStartDate = val ? new Date(val).toISOString() : null;
    }
  });

  const formattedStart = computed(() => {
    if (!isEnabled.value) return '';
    return new Date(localValue.value).toLocaleString(locale.value, {
      year: 'numeric', month: 'long', day: 'numeric', hour: '2-digit', minute: '2-digit'
    });
  });

  const enableNow = () => {
    if (!confirm(t('adminSettings.inventory.enableConfirm'))) return;
    props.companyData.stockTrackingStartDate = new Date().toISOString();
    emit('save');
  };

  const disable = () => {
    if (!confirm(t('adminSettings.inventory.disableConfirm'))) return;
    props.companyData.stockTrackingStartDate = null;
    emit('save');
  };
</script>

<template>
  <div class="p-2 md:p-4 animate-fade-in space-y-6">
    <div>
      <h3 class="text-lg font-light text-primary mb-2 uppercase tracking-widest border-b border-text/10 pb-2">{{ $t('adminSettings.inventory.title') }}</h3>
      <p class="text-text-muted text-sm m-0">
        {{ $t('adminSettings.inventory.introBefore') }}
        <strong>{{ $t('adminSettings.inventory.introStrong') }}</strong> {{ $t('adminSettings.inventory.introAfter') }}
      </p>
    </div>

    <div class="p-4 rounded-xl border"
         :class="isEnabled ? 'bg-green-500/5 border-green-500/30' : 'bg-text/5 border-text/10'">
      <div class="flex items-center gap-2 font-bold" :class="isEnabled ? 'text-green-500' : 'text-text-muted'">
        <i class="pi" :class="isEnabled ? 'pi-check-circle' : 'pi-pause-circle'"></i>
        <span v-if="isEnabled">{{ $t('adminSettings.inventory.enabledSince', { start: formattedStart }) }}</span>
        <span v-else>{{ $t('adminSettings.inventory.disabled') }}</span>
      </div>
      <p v-if="!isEnabled" class="text-xs text-text-muted mt-2 mb-0">
        {{ $t('adminSettings.inventory.recommendedOrder') }}
      </p>
    </div>

    <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
      <div>
        <label class="block mb-2 font-bold text-text-muted text-xs uppercase tracking-wider">{{ $t('adminSettings.inventory.startLabel') }}</label>
        <input v-model="localValue"
               type="datetime-local"
               @click="$event.target.showPicker?.()"
               class="w-full bg-background border border-text/20 text-text hover:border-primary focus:border-primary focus:ring-1 focus:ring-primary transition-colors rounded-lg p-3 shadow-sm cursor-pointer" />
        <p class="text-xs text-text-muted mt-2 mb-0">
          {{ $t('adminSettings.inventory.hintBefore') }} <em>{{ $t('adminSettings.inventory.hintSaveWord') }}</em> {{ $t('adminSettings.inventory.hintAfter') }}
        </p>
      </div>

      <div class="flex items-end gap-3 flex-wrap">
        <button type="button" @click="enableNow"
                class="cursor-pointer h-[46px] px-5 bg-primary text-black font-bold rounded-lg hover:brightness-110 transition-all flex items-center gap-2">
          <i class="pi pi-play"></i> {{ $t('adminSettings.inventory.enableNow') }}
        </button>
        <button v-if="isEnabled" type="button" @click="disable"
                class="cursor-pointer h-[46px] px-5 bg-background border border-red-500/40 text-red-500 font-bold rounded-lg hover:bg-red-500/10 transition-all flex items-center gap-2">
          <i class="pi pi-stop-circle"></i> {{ $t('adminSettings.inventory.disable') }}
        </button>
      </div>
    </div>
  </div>
</template>

