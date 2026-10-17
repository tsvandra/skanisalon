<template>
  <div class="max-w-5xl w-full mx-auto px-4 py-8 md:p-12 box-border bg-background text-text min-h-screen">
    <div class="text-center mb-8">
      <h1 class="text-3xl md:text-4xl font-light tracking-wide mb-2 text-text">
        {{ $t('booking.title', { companyName: company?.name || $t('common.defaultCompany') }) }}
      </h1>
      <p class="text-sm md:text-base text-text-muted max-w-xl mx-auto">
        {{ $t('booking.subtitle') }}
      </p>
    </div>

    <div v-if="!canSeeBookingForm" class="bg-surface rounded-2xl shadow-lg p-8 md:p-16 text-center border border-text/10 flex flex-col items-center animate-fade-in">
      <i class="pi pi-calendar-clock text-primary text-6xl md:text-8xl mb-6 opacity-80"></i>
      <h2 class="text-3xl font-bold mb-4 text-text">{{ $t('booking.comingSoonTitle') }}</h2>
      <p class="text-lg text-text-muted max-w-md">
        {{ $t('booking.comingSoonText') }}
      </p>
    </div>

    <div v-else-if="isSuccess" class="bg-surface rounded-2xl shadow-lg p-8 md:p-16 text-center border border-text/10 flex flex-col items-center animate-fade-in">
      <i class="pi pi-check-circle text-green-500 text-6xl md:text-8xl mb-6"></i>
      <h2 class="text-3xl font-bold mb-4 text-primary">{{ $t('booking.successTitle') }}</h2>
      <p class="text-lg text-text-muted mb-10 max-w-md">{{ $t('booking.success') }}</p>
      <button @click="resetBooking" class="px-8 py-3 border-2 border-primary text-primary font-bold rounded-xl transition-all hover:bg-primary hover:text-surface cursor-pointer">
        <i class="pi pi-calendar-plus mr-2"></i> {{ $t('booking.newBooking') }}
      </button>
    </div>

    <div v-else class="bg-surface rounded-2xl shadow-lg p-6 sm:p-8 md:p-10 border border-text/10 animate-fade-in">

      <div v-if="isDevLockActive && !isLoggedIn" class="bg-red-500/10 border border-red-500/30 text-red-600 rounded-lg p-3 mb-6 text-sm font-bold flex items-center gap-2">
        <i class="pi pi-exclamation-triangle"></i> {{ $t('booking.devOverrideWarning') }}
      </div>
      <div v-if="isLoggedIn && (!isFeatureAllowed || !isTurnedOnByAdmin || isDevLockActive)" class="bg-orange-500/10 border border-orange-500/30 text-orange-600 rounded-lg p-3 mb-6 text-sm font-bold flex items-center gap-2">
        <i class="pi pi-info-circle"></i> {{ $t('booking.adminViewWarning') }}
      </div>

      <!-- Kiválasztott szolgáltatások összefoglaló sávja a 2. és 3. lépésben -->
      <div v-if="activeStep !== '1' && selectedItems.length > 0"
           class="mb-6 p-4 rounded-2xl bg-background/80 border border-primary/25 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div class="flex flex-col gap-1.5">
          <span class="text-[11px] font-bold uppercase tracking-widest text-text-muted">
            {{ $t('booking.selectedServicesTitle') }}
          </span>
          <div class="flex flex-wrap gap-1.5">
            <span v-for="item in selectedItems"
                  :key="item.variantId"
                  class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-primary/10 border border-primary/30 text-xs font-semibold text-text">
              <ServicePictogram :svg="item.pictogram" sizeClass="w-3.5 h-3.5" class="text-primary shrink-0" />
              <span>{{ item.serviceName }} · {{ item.variantName }}</span>
            </span>
          </div>
        </div>

        <div class="flex items-center gap-4 shrink-0 pt-2 sm:pt-0 border-t sm:border-t-0 border-text/10">
          <div class="text-right">
            <div class="text-xs text-text-muted font-medium">
              ~{{ $t('booking.durationMinutes', { min: totalDuration }) }}
            </div>
            <div class="text-lg font-extrabold text-primary">
              {{ formatCurrency(totalPrice) }}
            </div>
          </div>
        </div>
      </div>

      <Stepper v-model:value="activeStep">
        <StepList>
          <Step value="1">{{ $t('booking.stepServices') }}</Step>
          <Step value="2">{{ $t('booking.stepDetails') }}</Step>
          <Step value="3">{{ $t('booking.stepSummary') }}</Step>
        </StepList>
        <StepPanels class="!px-2 sm:!px-4 !pt-4 !pb-2">
          <StepPanel value="1" v-slot="{ activateCallback }" class="!bg-transparent !p-2 sm:!p-4 rounded-xl">
            <StepServices v-model:selectedVariants="bookingData.serviceVariantIds"
                          v-model:selectedItems="selectedItems"
                          v-model:notes="bookingData.notes"
                          :onNext="() => activateCallback('2')" />
          </StepPanel>
          <StepPanel value="2" v-slot="{ activateCallback }" class="!bg-transparent !p-2 sm:!p-4 rounded-xl">
            <StepDetails v-model="bookingData" :onNext="() => activateCallback('3')" :onPrev="() => activateCallback('1')" />
          </StepPanel>
          <StepPanel value="3" v-slot="{ activateCallback }" class="!bg-transparent !p-2 sm:!p-4 rounded-xl">
            <StepDateTime v-model:employeeId="bookingData.employeeId"
                          v-model:selectedDateTime="selectedDateTime"
                          v-model:notes="bookingData.notes"
                          :totalDuration="totalDuration"
                          :isSubmitting="isSubmitting"
                          :errorMessage="errorMessage"
                          :onPrev="() => activateCallback('2')"
                          @submit="submitBooking" />
          </StepPanel>
        </StepPanels>
      </Stepper>
    </div>
  </div>
</template>

<script setup>
  import { ref, inject, computed } from 'vue';
  import { useI18n } from 'vue-i18n';
  import bookingApi from '@/services/bookingApi';
  import ServicePictogram from '@/components/common/ServicePictogram.vue';

  import StepServices from '@/components/booking/StepServices.vue';
  import StepDetails from '@/components/booking/StepDetails.vue';
  import StepDateTime from '@/components/booking/StepDateTime.vue';

  import Stepper from 'primevue/stepper';
  import StepList from 'primevue/steplist';
  import StepPanels from 'primevue/steppanels';
  import Step from 'primevue/step';
  import StepPanel from 'primevue/steppanel';

  const { t, te, locale } = useI18n();
  const company = inject('company', ref(null));
  const isLoggedIn = inject('isLoggedIn', ref(false));

  const activeStep = ref('1');
  const isSuccess = ref(false);
  const selectedItems = ref([]);

  const totalPrice = computed(() => {
    return selectedItems.value.reduce((sum, item) => sum + (Number(item.price) || 0), 0);
  });

  const totalDuration = computed(() => {
    return selectedItems.value.reduce((sum, item) => sum + (Number(item.duration) || 30), 0);
  });

  const formatCurrency = (val) => {
    if (val == null || val === 0) return '';
    return val.toLocaleString(locale.value, { style: 'currency', currency: 'EUR', maximumFractionDigits: 0 });
  };

  // --- SaaS JOGOSULTSÁG ÉS MEGJELENÍTÉS LOGIKA ---

  // 1. Fejlesztői (override) letiltás a .env-ből
  const isDevLockActive = import.meta.env.VITE_LOCK_PUBLIC_BOOKING === 'true';

  // 2. Tartalmazza a cég csomagja az online foglalást?
  const isFeatureAllowed = computed(() => {
    return company.value?.enabledFeatures?.includes('OnlineBooking') || company.value?.isOnlineBookingEnabled || false;
  });

  // 3. Bekapcsolta a szalonvezető a beállításokban?
  const isTurnedOnByAdmin = computed(() => {
    return company.value?.isOnlineBookingEnabled === true;
  });

  // Láthatja-e a látogató a foglalási űrlapot?
  const canSeeBookingForm = computed(() => {
    if (isDevLockActive && !isLoggedIn.value) return false;
    return isFeatureAllowed.value && isTurnedOnByAdmin.value;
  });
  // ---------------------------------------------

  const getInitialBookingData = () => ({
    fullName: '',
    email: '',
    phone: '',
    preferredChannel: 'WhatsApp',
    messengerHandle: '',
    attributes: {},
    notes: '',
    website: '',
    serviceVariantIds: [],
    employeeId: 0
  });

  const bookingData = ref(getInitialBookingData());
  const selectedDateTime = ref('');
  const isSubmitting = ref(false);
  const errorMessage = ref('');

  const submitBooking = async () => {
    isSubmitting.value = true;
    errorMessage.value = '';

    try {
      const payload = {
        ...bookingData.value,
        clientLanguage: locale.value || 'hu',
        startDateTime: new Date(selectedDateTime.value).toISOString()
      };
      await bookingApi.createGuestBooking(payload);
      isSuccess.value = true;
    } catch (error) {
      let code = error.response?.data?.errorCode;
      if (!code && error.response?.data?.errors) {
        const firstFieldErrors = Object.values(error.response.data.errors)[0];
        if (Array.isArray(firstFieldErrors) && firstFieldErrors.length > 0) {
          code = firstFieldErrors[0];
        }
      }
      if (code && te('errors.' + code)) {
        errorMessage.value = t('errors.' + code);
      } else if (error.response && error.response.status === 429) {
        errorMessage.value = t('errors.TOO_MANY_BOOKING_REQUESTS');
      } else if (error.response && error.response.status === 400) {
        errorMessage.value = t('errors.BAD_REQUEST');
      } else if (error.response && error.response.status === 500) {
        errorMessage.value = t('errors.SERVER_ERROR');
      } else {
        errorMessage.value = t('booking.errorNetwork');
      }
    } finally {
      isSubmitting.value = false;
    }
  };

  const resetBooking = () => {
    bookingData.value = getInitialBookingData();
    selectedItems.value = [];
    selectedDateTime.value = '';
    activeStep.value = '1';
    errorMessage.value = '';
    isSuccess.value = false;
  };
</script>

<style scoped>
  .animate-fade-in {
    animation: fadeIn 0.5s ease-out;
  }

  @keyframes fadeIn {
    from {
      opacity: 0;
      transform: translateY(10px);
    }

    to {
      opacity: 1;
      transform: translateY(0);
    }
  }
</style>
