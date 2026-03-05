<script setup>
  import { computed } from 'vue';
  import Button from 'primevue/button';
  import Card from 'primevue/card';
  import { useRouter } from 'vue-router';
  import { useCompanyStore } from '@/stores/companyStore';

  const router = useRouter();
  const companyStore = useCompanyStore();

  const company = computed(() => companyStore.company);

  const heroImageUrl = computed(() => {
    return company.value?.heroImageUrl || 'https://images.unsplash.com/photo-1560066984-138dadb4c035?ixlib=rb-1.2.1&auto=format&fit=crop&w=1920&q=80';
  });

  const goToServices = () => { router.push('/szolgaltatasok'); }
  const goToContact = () => { router.push('/kapcsolat'); }
  const goToGallery = () => { router.push('/galeria'); }
</script>

<template>
  <div class="w-full pb-24 md:pb-0 relative">

    <div class="bg-cover bg-center h-[350px] md:h-[450px] flex items-center justify-center text-center text-white rounded-b-3xl mb-10 shadow-lg mx-auto w-full"
         :style="{ backgroundImage: `linear-gradient(rgba(0, 0, 0, 0.5), rgba(0, 0, 0, 0.5)), url(${heroImageUrl})` }">
      <div class="px-4 w-full max-w-4xl">
        <div class="flex flex-col sm:flex-row gap-4 justify-center items-center mt-10">
          <Button :label="$t('nav.services')"
                  class="!bg-primary !border-primary hover:!bg-primary-emphasis font-bold !px-8 !py-3.5 !rounded-xl hover:!scale-105 transition-transform shadow-md w-full sm:w-auto !min-h-[50px]"
                  @click="goToServices" />
          <Button :label="$t('nav.contact')"
                  icon="pi pi-calendar"
                  class="!bg-white !text-gray-900 !border-none font-bold !px-8 !py-3.5 !rounded-xl hover:!scale-105 transition-transform shadow-md w-full sm:w-auto !min-h-[50px]"
                  @click="goToContact" />
        </div>
      </div>
    </div>

    <div class="max-w-5xl mx-auto px-4 w-full mb-8">
      <Card class="w-full !bg-text/5 !border !border-text/10 shadow-md !rounded-2xl overflow-hidden text-text">
        <template #title>
          <span class="text-primary text-2xl md:text-3xl font-light tracking-wide block pt-2 px-2">
            {{ $t('home.welcome', { company: company?.name || 'Szalon' }) }}
          </span>
        </template>
        <template #content>
          <div class="px-2 pb-2">
            <div class="leading-relaxed text-base md:text-lg text-text-muted">
              <p class="text-primary text-xl mb-4 font-bold">{{ $t('home.introTitle') }}</p>
              {{ $t('home.introText') }}
            </div>

            <button @click="goToGallery"
                    class="w-full mt-6 p-4 bg-text/5 rounded-xl border-l-4 border-primary text-text-muted cursor-pointer transition-all duration-200 hover:bg-text/10 hover:text-text hover:shadow-sm flex items-center min-h-[60px] text-left">
              <i class="pi pi-camera mr-4 text-2xl text-primary"></i>
              <div>
                <span class="block font-bold text-text">{{ $t('nav.gallery') }}</span>
                <span class="text-sm opacity-80">Tekintsd meg legújabb munkáinkat</span>
              </div>
              <i class="pi pi-chevron-right ml-auto text-text/50"></i>
            </button>
          </div>
        </template>
      </Card>
    </div>

    <div class="md:hidden fixed bottom-0 left-0 right-0 p-4 bg-surface/95 backdrop-blur-md border-t border-primary/20 shadow-[0_-5px_15px_rgba(0,0,0,0.2)] z-50">
      <Button :label="$t('nav.contact') + ' / Időpont'"
              icon="pi pi-calendar"
              class="w-full !bg-primary !border-none !text-white !font-bold !text-lg !rounded-xl !min-h-[56px] shadow-lg"
              @click="goToContact" />
    </div>

  </div>
</template>
