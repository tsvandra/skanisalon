<script setup>
  import { computed, inject, watch, ref } from 'vue';
  import { useI18n } from 'vue-i18n';
  import { useTranslation } from '@/composables/useTranslation';
  import InputText from 'primevue/inputtext';
  import Textarea from 'primevue/textarea';

  const props = defineProps({
    companyData: {
      type: Object,
      required: true
    }
  });

  const { locale } = useI18n();
  const currentLang = computed(() => locale.value);
  const globalCompany = inject('company', ref(null));

  const { translatingField, translateField } = useTranslation();

  const ensureDict = (field) => {
    if (field && typeof field === 'object' && field !== null) return field;
    return { [currentLang.value]: field || "" };
  };

  watch(() => props.companyData, (newData) => {
    if (newData && Object.keys(newData).length > 0) {
      newData.openingHoursTitle = ensureDict(newData.openingHoursTitle);
      newData.openingHoursDescription = ensureDict(newData.openingHoursDescription);
      newData.openingTimeSlots = ensureDict(newData.openingTimeSlots);
      newData.openingExtraInfo = ensureDict(newData.openingExtraInfo);
    }
  }, { immediate: true, deep: true });

  const triggerTranslation = async (fieldName) => {
    await translateField({
      obj: props.companyData,
      fieldName: fieldName,
      targetLang: currentLang.value,
      defaultLang: globalCompany.value?.defaultLanguage || 'hu',
      loadingKey: `opening-${fieldName}-${currentLang.value}`
    });
  };
</script>

<template>
  <div class="p-2 md:p-4 animate-fade-in" v-if="companyData.openingHoursTitle">

    <div class="mb-6">
      <label class="block mb-2 font-bold text-text-muted text-xs uppercase tracking-wider">Címsor (Pl: Bejelentkezés alapján)</label>
      <div class="relative flex items-start w-full group/tools">
        <InputText v-model="companyData.openingHoursTitle[currentLang]"
                   class="w-full !bg-background !border-text/20 !text-text hover:!border-primary focus:!border-primary focus:!ring-1 focus:!ring-primary transition-colors rounded-lg p-3 shadow-sm" />

        <button @click="triggerTranslation('openingHoursTitle')"
                class="opacity-30 bg-transparent border-none text-primary cursor-pointer ml-3 mt-3.5 text-xl transition-all duration-200 group-hover/tools:opacity-100 hover:scale-110" title="Fordítás">
          <i v-if="translatingField === `opening-openingHoursTitle-${currentLang}`" class="pi pi-spin pi-spinner"></i>
          <i v-else class="pi pi-sparkles"></i>
        </button>
      </div>
    </div>

    <div class="mb-6">
      <label class="block mb-2 font-bold text-text-muted text-xs uppercase tracking-wider">Leírás (Pl: Jelenleg kizárólag...)</label>
      <div class="relative flex items-start w-full group/tools">
        <Textarea v-model="companyData.openingHoursDescription[currentLang]" rows="2"
                  class="w-full !bg-background !border-text/20 !text-text hover:!border-primary focus:!border-primary focus:!ring-1 focus:!ring-primary transition-colors rounded-lg p-3 shadow-sm resize-none" />

        <button @click="triggerTranslation('openingHoursDescription')"
                class="opacity-30 bg-transparent border-none text-primary cursor-pointer ml-3 mt-3.5 text-xl transition-all duration-200 group-hover/tools:opacity-100 hover:scale-110" title="Fordítás">
          <i v-if="translatingField === `opening-openingHoursDescription-${currentLang}`" class="pi pi-spin pi-spinner"></i>
          <i v-else class="pi pi-sparkles"></i>
        </button>
      </div>
    </div>

    <div class="mb-6">
      <label class="block mb-2 font-bold text-text-muted text-xs uppercase tracking-wider">Időpontok (HTML engedélyezett, pl: &lt;br&gt;)</label>
      <div class="relative flex items-start w-full group/tools">
        <Textarea v-model="companyData.openingTimeSlots[currentLang]" rows="4"
                  class="w-full !bg-background !border-text/20 !text-text hover:!border-primary focus:!border-primary focus:!ring-1 focus:!ring-primary transition-colors rounded-lg p-3 shadow-sm resize-none" />

        <button @click="triggerTranslation('openingTimeSlots')"
                class="opacity-30 bg-transparent border-none text-primary cursor-pointer ml-3 mt-3.5 text-xl transition-all duration-200 group-hover/tools:opacity-100 hover:scale-110" title="Fordítás">
          <i v-if="translatingField === `opening-openingTimeSlots-${currentLang}`" class="pi pi-spin pi-spinner"></i>
          <i v-else class="pi pi-sparkles"></i>
        </button>
      </div>
      <small class="text-xs text-text/50 mt-2 block italic font-medium">Tipp: Használja a &lt;br&gt; kódot új sor kezdéséhez!</small>
    </div>

    <div class="mb-6">
      <label class="block mb-2 font-bold text-text-muted text-xs uppercase tracking-wider">Extra infó (Pl: Facebookon tesszük közzé...)</label>
      <div class="relative flex items-start w-full group/tools">
        <Textarea v-model="companyData.openingExtraInfo[currentLang]" rows="2"
                  class="w-full !bg-background !border-text/20 !text-text hover:!border-primary focus:!border-primary focus:!ring-1 focus:!ring-primary transition-colors rounded-lg p-3 shadow-sm resize-none" />

        <button @click="triggerTranslation('openingExtraInfo')"
                class="opacity-30 bg-transparent border-none text-primary cursor-pointer ml-3 mt-3.5 text-xl transition-all duration-200 group-hover/tools:opacity-100 hover:scale-110" title="Fordítás">
          <i v-if="translatingField === `opening-openingExtraInfo-${currentLang}`" class="pi pi-spin pi-spinner"></i>
          <i v-else class="pi pi-sparkles"></i>
        </button>
      </div>
    </div>

  </div>
</template>
