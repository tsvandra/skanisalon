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
  const goToGallery = () => { router.push('/galeria'); } // ÚJ: Navigáció a galériába
</script>

<template>
  <div class="w-full">

    <div class="bg-cover bg-center h-[350px] md:h-[450px] flex items-center justify-center text-center text-white rounded-b-2xl mb-10 shadow-lg"
         :style="{ backgroundImage: `linear-gradient(rgba(0, 0, 0, 0.5), rgba(0, 0, 0, 0.5)), url(${heroImageUrl})` }">
      <div class="px-4 w-full">
        <div class="mt-5 flex flex-col md:flex-row justify-center gap-4">
          <Button :label="$t('home.viewServices')"
                  icon="pi pi-list"
                  class="!bg-primary !border-none !text-black font-bold !px-8 !py-3.5 !rounded-xl hover:!scale-105 transition-transform shadow-md"
                  @click="goToServices" />

          <Button :label="$t('home.bookAppointment')"
                  icon="pi pi-calendar"
                  class="!bg-white !text-gray-900 !border-none font-bold !px-8 !py-3.5 !rounded-xl hover:!scale-105 transition-transform shadow-md"
                  @click="goToContact" />
        </div>
      </div>
    </div>

    <div class="max-w-[900px] mx-auto p-5">
      <Card class="w-full mb-8 !bg-text/5 !border !border-text/10 shadow-md !rounded-2xl overflow-hidden text-text">
        <template #title>
          <span class="text-primary text-3xl font-light tracking-wide block pt-2 px-2">
            {{ $t('home.welcome', { company: company?.name || 'Szalon' }) }}
          </span>
        </template>
        <template #content>
          <div class="px-2 pb-2">
            <div class="leading-relaxed text-lg text-text-muted">
              <p class="text-primary text-xl mb-4 font-bold">{{ $t('home.introTitle') }}</p>
              {{ $t('home.introText') }}
            </div>

            <div @click="goToGallery"
                 class="mt-6 p-4 bg-text/5 rounded-lg border-l-4 border-primary text-text-muted cursor-pointer transition-all duration-200 hover:bg-text/10 hover:text-text hover:shadow-sm flex items-center">
              <i class="pi pi-camera mr-3 text-primary text-xl"></i>
              <span class="font-medium">{{ $t('home.checkGallery') }}</span>
              <i class="pi pi-arrow-right ml-auto text-primary/50"></i>
            </div>
          </div>
        </template>
      </Card>
    </div>

  </div>
</template>
