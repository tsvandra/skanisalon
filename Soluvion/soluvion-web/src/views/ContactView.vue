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
  <div class="max-w-screen-xl mx-auto p-5">

    <div class="text-center mb-10">
      <h1 class="text-4xl text-primary mb-2.5">{{ $t('contact.title') }}</h1>
      <p class="text-text-muted">{{ $t('contact.subtitle') }}</p>
    </div>

    <div class="grid grid-cols-1 md:grid-cols-[1fr_1.5fr] gap-8">

      <div class="flex flex-col gap-5">

        <Card class="mb-5 shadow-sm">
          <template #title>
            {{ $t('contact.myContacts') }}
          </template>
          <template #content>
            <ul class="list-none p-0 m-0" v-if="company">
              <li v-if="company.city" class="flex items-start mb-5 text-lg">
                <i class="pi pi-map-marker text-2xl text-primary mr-4 mt-1"></i>
                <div>
                  <strong class="text-text-muted">{{ $t('contact.address') }}:</strong><br>
                  <span class="text-gray-500">
                    {{ company.postalCode }} {{ company.city }}<br>
                    {{ company.streetName }} {{ company.houseNumber }}.
                  </span>
                </div>
              </li>
              <li v-if="company.phone" class="flex items-start mb-5 text-lg">
                <i class="pi pi-phone text-2xl text-primary mr-4 mt-1"></i>
                <div>
                  <strong class="text-text-muted">{{ $t('contact.phone') }}:</strong><br>
                  <a :href="`tel:${company.phone}`" class="text-gray-500 no-underline transition-colors duration-200 hover:text-primary">
                    {{ company.phone }}
                  </a>
                </div>
              </li>
              <li v-if="company.email" class="flex items-start mb-5 text-lg">
                <i class="pi pi-envelope text-2xl text-primary mr-4 mt-1"></i>
                <div>
                  <strong class="text-text-muted">{{ $t('contact.email') }}:</strong><br>
                  <a :href="`mailto:${company.email}`" class="text-gray-500 no-underline transition-colors duration-200 hover:text-primary">
                    {{ company.email }}
                  </a>
                </div>
              </li>
            </ul>
            <div v-else class="text-gray-500">{{ $t('common.loading') }}</div>
          </template>
        </Card>

        <Card class="shadow-sm">
          <template #title>
            {{ $t('contact.openingHours') }}
          </template>
          <template #content>
            <div class="text-center" v-if="company">

              <div class="mb-5">
                <i class="pi pi-calendar-clock text-4xl text-primary mb-2.5"></i>
                <h3 class="m-0 text-gray-500 font-bold">
                  {{ company.openingHoursTitle?.[$i18n.locale] || company.openingHoursTitle?.['hu'] || $t('contact.openingHours') }}
                </h3>
              </div>

              <p v-if="company.openingHoursDescription?.[$i18n.locale] || company.openingHoursDescription?.['hu']" class="leading-relaxed text-gray-500">
                {{ company.openingHoursDescription?.[$i18n.locale] || company.openingHoursDescription?.['hu'] }}
              </p>

              <div v-if="company.openingTimeSlots?.[$i18n.locale] || company.openingTimeSlots?.['hu']"
                   class="bg-surface text-text my-4 p-4 rounded-lg border-l-4 border-primary"
                   v-html="company.openingTimeSlots?.[$i18n.locale] || company.openingTimeSlots?.['hu']">
              </div>

              <p v-if="company.openingExtraInfo?.[$i18n.locale] || company.openingExtraInfo?.['hu']" class="text-sm mt-5 text-gray-500">
                {{ company.openingExtraInfo?.[$i18n.locale] || company.openingExtraInfo?.['hu'] }}
              </p>

              <div class="mt-4 flex gap-2.5 flex-wrap justify-center">
                <a v-if="company.facebookUrl" :href="company.facebookUrl" target="_blank" class="no-underline flex-1 min-w-[120px]">
                  <Button label="Facebook" icon="pi pi-facebook" class="w-full !text-primary !border-primary hover:!bg-primary/10 !py-2 !rounded-lg transition-all" outlined />
                </a>
                <a v-if="company.instagramUrl" :href="company.instagramUrl" target="_blank" class="no-underline flex-1 min-w-[120px]">
                  <Button label="Instagram" icon="pi pi-instagram" class="w-full !text-primary !border-primary hover:!bg-primary/10 !py-2 !rounded-lg transition-all" outlined />
                </a>
                <a v-if="company.tikTokUrl" :href="company.tikTokUrl" target="_blank" class="no-underline flex-1 min-w-[120px]">
                  <Button label="TikTok" icon="pi pi-video" class="w-full !text-primary !border-primary hover:!bg-primary/10 !py-2 !rounded-lg transition-all" outlined />
                </a>
              </div>
            </div>
          </template>
        </Card>

      </div>

      <div class="flex flex-col gap-5">

        <Card class="shadow-sm">
          <template #title>
            {{ $t('contact.writeUs') }}
          </template>
          <template #content>
            <div v-if="submitted" class="mb-4">
              <Message severity="success" :closable="false">{{ $t('contact.form.success') }}</Message>
            </div>

            <div class="mb-5">
              <label class="block mb-2 font-bold text-gray-500">{{ $t('contact.form.name') }}</label>
              <InputText v-model="name" :placeholder="$t('contact.form.namePlaceholder')" class="w-full focus:!border-primary focus:!ring-1 focus:!ring-primary" />
            </div>

            <div class="mb-5">
              <label class="block mb-2 font-bold text-gray-500">{{ $t('contact.form.email') }}</label>
              <InputText v-model="email" :placeholder="$t('contact.form.emailPlaceholder')" class="w-full focus:!border-primary focus:!ring-1 focus:!ring-primary" />
            </div>

            <div class="mb-5">
              <label class="block mb-2 font-bold text-gray-500">{{ $t('contact.form.message') }}</label>
              <Textarea v-model="messageText" rows="4" :placeholder="$t('contact.form.messagePlaceholder')" class="w-full focus:!border-primary focus:!ring-1 focus:!ring-primary" />
            </div>

            <Button :label="isLoading ? $t('contact.form.sending') : $t('contact.form.send')"
                    :icon="isLoading ? 'pi pi-spin pi-spinner' : 'pi pi-send'"
                    :disabled="isLoading"
                    @click="sendMessage"
                    class="!bg-primary !border-primary !text-white hover:!brightness-90 font-bold !px-6 !py-3 !rounded-lg shadow-md transition-all" />
          </template>
        </Card>

        <div class="shadow-md rounded-xl overflow-hidden" v-if="company?.mapEmbedUrl">
          <iframe :src="company.mapEmbedUrl"
                  width="100%"
                  height="300"
                  class="border-0"
                  allowfullscreen=""
                  loading="lazy">
          </iframe>
        </div>

      </div>
    </div>
  </div>
</template>
