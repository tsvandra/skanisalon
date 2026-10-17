<template>
  <div class="py-2">
    <h3 class="text-xl font-bold mb-6 text-text">{{ $t('booking.yourDetails') }}</h3>

    <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
      <div class="flex flex-col gap-1.5">
        <label class="text-xs font-bold uppercase tracking-wider text-text-muted">{{ $t('booking.fullName') }}</label>
        <div class="relative flex items-center">
          <i class="pi pi-user absolute left-3.5 text-text-muted text-sm pointer-events-none"></i>
          <input v-model="localData.fullName"
                 type="text"
                 autocomplete="name"
                 class="w-full pl-10 pr-4 py-3 rounded-xl border border-text/15 bg-background text-text focus:border-primary focus:ring-1 focus:ring-primary/40 focus:outline-none transition-all" />
        </div>
      </div>

      <div class="flex flex-col gap-1.5">
        <label class="text-xs font-bold uppercase tracking-wider text-text-muted">{{ $t('booking.email') }}</label>
        <div class="relative flex items-center">
          <i class="pi pi-envelope absolute left-3.5 text-text-muted text-sm pointer-events-none"></i>
          <input v-model="localData.email"
                 type="email"
                 autocomplete="email"
                 class="w-full pl-10 pr-4 py-3 rounded-xl border border-text/15 bg-background text-text focus:border-primary focus:ring-1 focus:ring-primary/40 focus:outline-none transition-all" />
        </div>
      </div>

      <div class="flex flex-col gap-1.5 md:col-span-2">
        <label class="text-xs font-bold uppercase tracking-wider text-text-muted">{{ $t('booking.phone') }}</label>
        <div class="relative flex items-center">
          <i class="pi pi-phone absolute left-3.5 text-text-muted text-sm pointer-events-none"></i>
          <input v-model="localData.phone"
                 type="tel"
                 name="phone"
                 autocomplete="tel"
                 class="w-full pl-10 pr-4 py-3 rounded-xl border border-text/15 bg-background text-text focus:border-primary focus:ring-1 focus:ring-primary/40 focus:outline-none transition-all" />
        </div>
      </div>

      <!-- Preferált visszaigazolási / kommunikációs csatorna kiválasztása -->
      <div class="flex flex-col gap-2 md:col-span-2 pt-1">
        <label class="text-xs font-bold uppercase tracking-wider text-text-muted flex items-center gap-1.5">
          <i class="pi pi-send text-primary text-xs"></i>
          <span>{{ $t('booking.preferredChannelLabel') }}</span>
        </label>
        <div class="grid grid-cols-2 sm:grid-cols-5 gap-2">
          <button v-for="ch in channels"
                  :key="ch.id"
                  type="button"
                  @click="localData.preferredChannel = ch.id"
                  class="min-h-[44px] px-3 py-2 rounded-xl border text-xs sm:text-sm font-bold flex items-center justify-center gap-2 transition-all cursor-pointer select-none"
                  :class="localData.preferredChannel === ch.id
                    ? 'bg-primary/15 border-primary text-primary shadow-xs ring-1 ring-primary/30'
                    : 'bg-background border-text/15 text-text-muted hover:border-primary/40 hover:text-text'">
            <i :class="ch.icon" class="text-base"></i>
            <span>{{ ch.label }}</span>
          </button>
        </div>

        <div v-if="localData.preferredChannel === 'Messenger'" class="mt-1">
          <div class="relative flex items-center">
            <i class="pi pi-facebook absolute left-3.5 text-blue-500 text-sm pointer-events-none"></i>
            <input v-model="localData.messengerHandle"
                   type="text"
                   autocomplete="off"
                   :placeholder="$t('booking.messengerHandlePlaceholder')"
                   class="w-full pl-10 pr-4 py-2.5 rounded-xl border border-text/15 bg-background text-text text-sm focus:border-primary focus:ring-1 focus:ring-primary/40 focus:outline-none transition-all" />
          </div>
        </div>
      </div>

      <!-- Böngésző autofill csapda lakcímhez: megakadályozza, hogy a Chrome/Safari a név+telefon automatikus kitöltésekor a megjegyzés mezőbe írja a lakcímet -->
      <div class="sr-only" aria-hidden="true">
        <input type="text" name="street-address" autocomplete="street-address" tabindex="-1" />
        <input type="text" name="address-level2" autocomplete="address-level2" tabindex="-1" />
        <input type="text" name="postal-code" autocomplete="postal-code" tabindex="-1" />
      </div>

      <!-- Honeypot mező spam robotok ellen (valódi felhasználóknak láthatatlan) -->
      <div class="sr-only" aria-hidden="true">
        <label for="booking_website_hp">Website</label>
        <input id="booking_website_hp"
               v-model="localData.website"
               type="text"
               name="website"
               tabindex="-1"
               autocomplete="off" />
      </div>
    </div>

    <!-- Megjegyzés a szolgáltatónak (külön szekcióban a kontakt mezőktől, lépések között megőrizve) -->
    <div class="mt-5 pt-4 border-t border-text/10">
      <BookingNotesField v-model="localData.notes" />
    </div>

    <div class="mt-6 pt-4 border-t border-text/10 flex justify-between items-center">
      <button type="button"
              @click="onPrev"
              class="min-h-[48px] px-6 py-2.5 border border-text/20 text-text font-semibold rounded-xl transition-colors hover:bg-text/5 cursor-pointer flex items-center gap-2">
        <i class="pi pi-arrow-left text-sm"></i> {{ $t('booking.back') }}
      </button>
      <button type="button"
              @click="onNext"
              :disabled="!isFormValid"
              class="min-h-[48px] px-7 py-2.5 bg-primary text-surface font-bold rounded-xl disabled:opacity-40 disabled:cursor-not-allowed transition-all hover:brightness-110 active:scale-95 shadow-md cursor-pointer flex items-center gap-2">
        <span>{{ $t('booking.next') }}</span>
        <i class="pi pi-arrow-right text-sm"></i>
      </button>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';
import BookingNotesField from '@/components/booking/BookingNotesField.vue';
import { COMMUNICATION_CHANNELS } from '@/utils/bookingReplyHelper';

const channels = COMMUNICATION_CHANNELS;

const props = defineProps({
  modelValue: { type: Object, required: true },
  onNext: { type: Function, required: true },
  onPrev: { type: Function, required: true }
});

const emit = defineEmits(['update:modelValue']);

const updateField = (field, value) => {
  emit('update:modelValue', {
    ...props.modelValue,
    [field]: value
  });
};

const localData = computed(() => ({
  get fullName() { return props.modelValue.fullName || ''; },
  set fullName(val) { updateField('fullName', val); },
  get email() { return props.modelValue.email || ''; },
  set email(val) { updateField('email', val); },
  get phone() { return props.modelValue.phone || ''; },
  set phone(val) { updateField('phone', val); },
  get preferredChannel() { return props.modelValue.preferredChannel || 'WhatsApp'; },
  set preferredChannel(val) { updateField('preferredChannel', val); },
  get messengerHandle() { return props.modelValue.messengerHandle || ''; },
  set messengerHandle(val) { updateField('messengerHandle', val); },
  get notes() { return props.modelValue.notes || ''; },
  set notes(val) { updateField('notes', val); },
  get website() { return props.modelValue.website || ''; },
  set website(val) { updateField('website', val); }
}));

const isFormValid = computed(() => {
  return (props.modelValue.fullName?.trim().length || 0) >= 2 &&
         (props.modelValue.email?.includes('@') || false) &&
         (props.modelValue.phone?.trim().length || 0) >= 6;
});
</script>
