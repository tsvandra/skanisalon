<script setup>
  import { ref, computed } from 'vue';
  import { useCompanyStore } from '@/stores/companyStore';
  import { useI18n } from 'vue-i18n';
  import api from '@/services/api';

  import Card from 'primevue/card';
  import InputText from 'primevue/inputtext';
  import Textarea from 'primevue/textarea';
  import Button from 'primevue/button';
  import Message from 'primevue/message';

  const { t } = useI18n();
  const companyStore = useCompanyStore();
  const company = computed(() => companyStore.company);

  const name = ref('');
  const email = ref('');
  const messageText = ref('');
  const submitted = ref(false);
  const isLoading = ref(false);

  const sendMessage = async () => {
    if (!name.value || !email.value || !messageText.value) {
      alert(t('contact.form.validationError'));
      return;
    }

    isLoading.value = true;

    try {
      await api.post('/api/Contact', {
        name: name.value,
        email: email.value,
        message: messageText.value
      });

      submitted.value = true;
      name.value = '';
      email.value = '';
      messageText.value = '';

      setTimeout(() => { submitted.value = false; }, 5000);

    } catch (error) {
      console.error("Hiba az üzenet küldésekor:", error);
      alert(t('contact.form.submitError'));
    } finally {
      isLoading.value = false;
    }
  }
</script>

<template>
  <div class="max-w-screen-xl w-full mx-auto px-4 py-8 md:p-8">

    <div class="text-center mb-10">
      <h1 class="text-3xl md:text-4xl text-primary mb-3 font-light tracking-wide">{{ $t('contact.title') }}</h1>
      <p class="text-text-muted text-lg">{{ $t('contact.subtitle') }}</p>
    </div>

    <div class="grid grid-cols-1 lg:grid-cols-[1fr_1.5fr] gap-6 md:gap-8">

      <div class="flex flex-col gap-6">

        <Card class="shadow-md !bg-surface !border !border-text/10 !rounded-2xl">
          <template #title>
            <span class="text-primary font-bold text-xl">{{ $t('contact.myContacts') }}</span>
          </template>
          <template #content>
            <ul class="list-none p-0 m-0" v-if="company">

              <li v-if="company.city" class="flex items-start gap-4 mb-5 text-lg">
                <i class="pi pi-map-marker text-2xl text-primary mt-1 shrink-0"></i>
                <div class="flex-grow">
                  <strong class="text-text block mb-1">{{ $t('contact.address') }}:</strong>
                  <span class="text-text-muted leading-relaxed block py-1">
                    {{ company.postalCode }} {{ company.city }}<br>
                    {{ company.streetName }} {{ company.houseNumber }}.
                  </span>
                </div>
              </li>

              <li v-if="company.phone" class="flex items-start gap-4 mb-5 text-lg">
                <i class="pi pi-phone text-2xl text-primary mt-1 shrink-0"></i>
                <div class="flex-grow">
                  <strong class="text-text block mb-1">{{ $t('contact.phone') }}:</strong>
                  <a :href="`tel:${company.phone}`" class="text-text-muted no-underline transition-colors duration-200 hover:text-primary block py-2 md:py-1 min-h-[44px] flex items-center md:min-h-0">
                    {{ company.phone }}
                  </a>
                </div>
              </li>

              <li v-if="company.email" class="flex items-start gap-4 mb-2 text-lg">
                <i class="pi pi-envelope text-2xl text-primary mt-1 shrink-0"></i>
                <div class="flex-grow">
                  <strong class="text-text block mb-1">{{ $t('contact.email') }}:</strong>
                  <a :href="`mailto:${company.email}`" class="text-text-muted no-underline transition-colors duration-200 hover:text-primary block py-2 md:py-1 min-h-[44px] flex items-center md:min-h-0 break-all">
                    {{ company.email }}
                  </a>
                </div>
              </li>

            </ul>
            <div v-else class="text-text-muted flex items-center min-h-[44px]"><i class="pi pi-spin pi-spinner mr-2"></i>{{ $t('common.loading') }}</div>
          </template>
        </Card>

        <Card class="shadow-md !bg-surface !border !border-text/10 !rounded-2xl">
          <template #title>
            <span class="text-primary font-bold text-xl">{{ $t('contact.openingHours') }}</span>
          </template>
          <template #content>
            <div class="text-center" v-if="company">

              <div class="mb-4">
                <i class="pi pi-calendar-clock text-4xl text-primary mb-3"></i>
                <h3 class="m-0 text-text font-bold text-lg">
                  {{ company.openingHoursTitle?.[$i18n.locale] || company.openingHoursTitle?.['hu'] || $t('contact.openingHours') }}
                </h3>
              </div>

              <p v-if="company.openingHoursDescription?.[$i18n.locale] || company.openingHoursDescription?.['hu']" class="leading-relaxed text-text-muted mb-4">
                {{ company.openingHoursDescription?.[$i18n.locale] || company.openingHoursDescription?.['hu'] }}
              </p>

              <div v-if="company.openingTimeSlots?.[$i18n.locale] || company.openingTimeSlots?.['hu']"
                   class="bg-background text-text my-4 p-4 rounded-xl border-l-4 border-primary shadow-sm text-left"
                   v-html="company.openingTimeSlots?.[$i18n.locale] || company.openingTimeSlots?.['hu']">
              </div>

              <p v-if="company.openingExtraInfo?.[$i18n.locale] || company.openingExtraInfo?.['hu']" class="text-sm mt-5 text-text-muted italic">
                {{ company.openingExtraInfo?.[$i18n.locale] || company.openingExtraInfo?.['hu'] }}
              </p>

              <div class="mt-6 flex flex-col sm:flex-row gap-3 justify-center">
                <a v-if="company.facebookUrl" :href="company.facebookUrl" target="_blank" class="no-underline flex-1">
                  <Button label="Facebook" icon="pi pi-facebook" class="w-full !text-primary !border-primary hover:!bg-primary/10 !py-2 !min-h-[44px] !rounded-xl transition-all font-bold" outlined />
                </a>
                <a v-if="company.instagramUrl" :href="company.instagramUrl" target="_blank" class="no-underline flex-1">
                  <Button label="Instagram" icon="pi pi-instagram" class="w-full !text-primary !border-primary hover:!bg-primary/10 !py-2 !min-h-[44px] !rounded-xl transition-all font-bold" outlined />
                </a>
                <a v-if="company.tikTokUrl" :href="company.tikTokUrl" target="_blank" class="no-underline flex-1">
                  <Button label="TikTok" icon="pi pi-video" class="w-full !text-primary !border-primary hover:!bg-primary/10 !py-2 !min-h-[44px] !rounded-xl transition-all font-bold" outlined />
                </a>
              </div>
            </div>
          </template>
        </Card>

      </div>

      <div class="flex flex-col gap-6">

        <Card class="shadow-md !bg-surface !border !border-text/10 !rounded-2xl">
          <template #title>
            <span class="text-primary font-bold text-xl">{{ $t('contact.writeUs') }}</span>
          </template>
          <template #content>
            <div v-if="submitted" class="mb-5">
              <Message severity="success" :closable="false" class="!rounded-xl">{{ $t('contact.form.success') }}</Message>
            </div>

            <div class="mb-5">
              <label class="block mb-2 font-bold text-text text-sm">{{ $t('contact.form.name') }}</label>
              <InputText v-model="name" :placeholder="$t('contact.form.namePlaceholder')" class="w-full !min-h-[44px] !rounded-xl !bg-background !border-text/20 !text-text focus:!border-primary focus:!ring-1 focus:!ring-primary transition-colors" />
            </div>

            <div class="mb-5">
              <label class="block mb-2 font-bold text-text text-sm">{{ $t('contact.form.email') }}</label>
              <InputText v-model="email" type="email" :placeholder="$t('contact.form.emailPlaceholder')" class="w-full !min-h-[44px] !rounded-xl !bg-background !border-text/20 !text-text focus:!border-primary focus:!ring-1 focus:!ring-primary transition-colors" />
            </div>

            <div class="mb-6">
              <label class="block mb-2 font-bold text-text text-sm">{{ $t('contact.form.message') }}</label>
              <Textarea v-model="messageText" rows="5" :placeholder="$t('contact.form.messagePlaceholder')" class="w-full !rounded-xl !bg-background !border-text/20 !text-text focus:!border-primary focus:!ring-1 focus:!ring-primary transition-colors p-3" />
            </div>

            <Button :label="isLoading ? $t('contact.form.sending') : $t('contact.form.send')"
                    :icon="isLoading ? 'pi pi-spin pi-spinner' : 'pi pi-send'"
                    :disabled="isLoading"
                    @click="sendMessage"
                    class="w-full sm:w-auto !bg-primary !border-primary hover:!bg-primary-emphasis !text-white font-bold !px-8 !py-3 !min-h-[48px] !rounded-xl shadow-md transition-transform hover:!scale-[1.02]" />
          </template>
        </Card>

        <div class="shadow-md rounded-2xl overflow-hidden border border-text/10 bg-surface" v-if="company?.mapEmbedUrl">
          <iframe :src="company.mapEmbedUrl"
                  width="100%"
                  height="350"
                  class="border-0 w-full"
                  allowfullscreen=""
                  loading="lazy"
                  title="Térkép">
          </iframe>
        </div>

      </div>
    </div>
  </div>
</template>
