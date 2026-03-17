<template>
  <div class="max-w-4xl w-full mx-auto px-4 py-8 md:p-12 box-border bg-background text-text min-h-screen">
    <h1 class="text-3xl font-light mb-8 text-center">
      {{ $t('booking.title', { companyName: company?.name || $t('common.defaultCompany') }) }}
    </h1>

    <div v-if="isSuccess" class="bg-surface rounded-2xl shadow-lg p-8 md:p-16 text-center border border-text/10 flex flex-col items-center animate-fade-in">
      <i class="pi pi-check-circle text-green-500 text-6xl md:text-8xl mb-6"></i>
      <h2 class="text-3xl font-bold mb-4 text-primary">{{ $t('booking.successTitle') }}</h2>
      <p class="text-lg text-text-muted mb-10 max-w-md">{{ $t('booking.success') }}</p>

      <button @click="resetBooking" class="px-8 py-3 border-2 border-primary text-primary font-bold rounded-xl transition-all hover:bg-primary hover:text-white">
        <i class="pi pi-calendar-plus mr-2"></i> {{ $t('booking.newBooking') }}
      </button>
    </div>

    <div v-else class="bg-surface rounded-2xl shadow-lg p-6 border border-text/10">
      <Stepper v-model:value="activeStep">

        <StepList>
          <Step value="1">{{ $t('booking.stepServices') }}</Step>
          <Step value="2">{{ $t('booking.stepDetails') }}</Step>
          <Step value="3">{{ $t('booking.stepSummary') }}</Step>
        </StepList>

        <StepPanels>
          <StepPanel value="1" v-slot="{ activateCallback }">
            <StepServices v-model:selectedVariants="bookingData.serviceVariantIds"
                          :onNext="() => activateCallback('2')" />
          </StepPanel>

          <StepPanel value="2" v-slot="{ activateCallback }">
            <StepDetails v-model="bookingData"
                         :onNext="() => activateCallback('3')"
                         :onPrev="() => activateCallback('1')" />
          </StepPanel>

          <StepPanel value="3" v-slot="{ activateCallback }">
            <StepDateTime v-model:employeeId="bookingData.employeeId"
                          v-model:selectedDateTime="selectedDateTime"
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
  import { ref, inject } from 'vue';
  import { useI18n } from 'vue-i18n';
  import bookingApi from '@/services/bookingApi';

  import StepServices from '@/components/booking/StepServices.vue';
  import StepDetails from '@/components/booking/StepDetails.vue';
  import StepDateTime from '@/components/booking/StepDateTime.vue';

  import Stepper from 'primevue/stepper';
  import StepList from 'primevue/steplist';
  import StepPanels from 'primevue/steppanels';
  import Step from 'primevue/step';
  import StepPanel from 'primevue/steppanel';

  const { t } = useI18n();
  const company = inject('company', ref(null));
  const activeStep = ref('1');

  // ÚJ: Állapot a sikeres foglalásnak
  const isSuccess = ref(false);

  const getInitialBookingData = () => ({
    fullName: '',
    email: '',
    phone: '',
    attributes: {},
    notes: '',
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
        startDateTime: new Date(selectedDateTime.value).toISOString()
      };
      await bookingApi.createGuestBooking(payload);

      // JAVÍTVA: alert() és reload() törölve! Csak az állapotot váltjuk.
      isSuccess.value = true;

    } catch (error) {
      if (error.response && error.response.status === 400) {
        errorMessage.value = error.response.data;
      } else if (error.response && error.response.status === 500) {
        const serverError = typeof error.response.data === 'string' ? error.response.data : JSON.stringify(error.response.data);
        errorMessage.value = `${t('booking.errorServer')}: ${serverError}`;
      } else {
        errorMessage.value = t('booking.errorNetwork');
      }
    } finally {
      isSubmitting.value = false;
    }
  };

  // ÚJ: Függvény, ami mindent alaphelyzetbe állít, ha új foglalást akar kezdeni
  const resetBooking = () => {
    bookingData.value = getInitialBookingData();
    selectedDateTime.value = '';
    activeStep.value = '1';
    errorMessage.value = '';
    isSuccess.value = false;
  };
</script>

<style scoped>
  /* Egy pici animáció, hogy elegáns legyen a megjelenés */
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
