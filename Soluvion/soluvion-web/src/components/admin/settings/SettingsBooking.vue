<script setup>
  import ToggleSwitch from 'primevue/toggleswitch';

  const props = defineProps({
    companyData: {
      type: Object,
      required: true
    }
  });

  const emit = defineEmits(['save']);

  const onToggleChange = () => {
    emit('save');
  };
</script>

<template>
  <div class="p-2 md:p-4 animate-fade-in space-y-6">
    <div>
      <h3 class="text-lg font-light text-primary mb-2 uppercase tracking-widest border-b border-text/10 pb-2">
        {{ $t('adminSettings.booking.title') }}
      </h3>
      <p class="text-text-muted text-sm m-0">
        {{ $t('adminSettings.booking.intro') }}
      </p>
    </div>

    <div class="p-5 rounded-xl border transition-colors flex flex-col sm:flex-row sm:items-center justify-between gap-4"
         :class="companyData.isOnlineBookingEnabled ? 'bg-green-500/5 border-green-500/30' : 'bg-text/5 border-text/10'">
      <div class="space-y-1">
        <div class="flex items-center gap-2 font-bold text-base"
             :class="companyData.isOnlineBookingEnabled ? 'text-green-500' : 'text-text-muted'">
          <i class="pi" :class="companyData.isOnlineBookingEnabled ? 'pi-check-circle' : 'pi-lock'"></i>
          <span>
            {{ companyData.isOnlineBookingEnabled ? $t('adminSettings.booking.enabled') : $t('adminSettings.booking.disabled') }}
          </span>
        </div>
        <p class="text-xs text-text-muted m-0 max-w-xl">
          {{ $t('adminSettings.booking.toggleDescription') }}
        </p>
      </div>

      <div class="flex items-center gap-3 self-end sm:self-center">
        <label class="text-sm font-semibold text-text cursor-pointer select-none" @click="companyData.isOnlineBookingEnabled = !companyData.isOnlineBookingEnabled; onToggleChange()">
          {{ $t('adminSettings.booking.toggleLabel') }}
        </label>
        <ToggleSwitch v-model="companyData.isOnlineBookingEnabled" @change="onToggleChange" />
      </div>
    </div>

    <p class="text-xs text-text-muted m-0 italic">
      {{ $t('adminSettings.booking.hint') }}
    </p>
  </div>
</template>

