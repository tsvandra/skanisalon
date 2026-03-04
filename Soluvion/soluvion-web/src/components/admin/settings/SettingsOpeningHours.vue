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

  // Biztosítjuk, hogy az értékek szótárak (objektumok) legyenek, ne sima stringek vagy null-ok
  const ensureDict = (field) => {
    if (field && typeof field === 'object' && field !== null) return field;
    return { [currentLang.value]: field || "" };
  };

  // Figyeljük a companyData változását (amikor a szülő betölti az API-ból), és inicializáljuk a szótárakat
  watch(() => props.companyData, (newData) => {
    if (newData && Object.keys(newData).length > 0) {
      newData.openingHoursTitle = ensureDict(newData.openingHoursTitle);
      newData.openingHoursDescription = ensureDict(newData.openingHoursDescription);
      newData.openingTimeSlots = ensureDict(newData.openingTimeSlots);
      newData.openingExtraInfo = ensureDict(newData.openingExtraInfo);
    }
  }, { immediate: true, deep: true });

  // Közös fordító hívás
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
  <div class="settings-opening-hours" v-if="companyData.openingHoursTitle">

    <div class="field">
      <label>Címsor (Pl: Bejelentkezés alapján)</label>
      <div class="input-with-tools">
        <InputText v-model="companyData.openingHoursTitle[currentLang]" class="w-full" />
        <button @click="triggerTranslation('openingHoursTitle')" class="magic-btn" title="Fordítás">
          <i v-if="translatingField === `opening-openingHoursTitle-${currentLang}`" class="pi pi-spin pi-spinner"></i>
          <i v-else class="pi pi-sparkles"></i>
        </button>
      </div>
    </div>

    <div class="field">
      <label>Leírás (Pl: Jelenleg kizárólag...)</label>
      <div class="input-with-tools">
        <Textarea v-model="companyData.openingHoursDescription[currentLang]" rows="2" class="w-full" />
        <button @click="triggerTranslation('openingHoursDescription')" class="magic-btn" title="Fordítás">
          <i v-if="translatingField === `opening-openingHoursDescription-${currentLang}`" class="pi pi-spin pi-spinner"></i>
          <i v-else class="pi pi-sparkles"></i>
        </button>
      </div>
    </div>

    <div class="field">
      <label>Időpontok (HTML engedélyezett, pl: &lt;br&gt;)</label>
      <div class="input-with-tools">
        <Textarea v-model="companyData.openingTimeSlots[currentLang]" rows="4" class="w-full" />
        <button @click="triggerTranslation('openingTimeSlots')" class="magic-btn" title="Fordítás">
          <i v-if="translatingField === `opening-openingTimeSlots-${currentLang}`" class="pi pi-spin pi-spinner"></i>
          <i v-else class="pi pi-sparkles"></i>
        </button>
      </div>
      <small>Tipp: Használja a &lt;br&gt; kódot új sor kezdéséhez!</small>
    </div>

    <div class="field">
      <label>Extra infó (Pl: Facebookon tesszük közzé...)</label>
      <div class="input-with-tools">
        <Textarea v-model="companyData.openingExtraInfo[currentLang]" rows="2" class="w-full" />
        <button @click="triggerTranslation('openingExtraInfo')" class="magic-btn" title="Fordítás">
          <i v-if="translatingField === `opening-openingExtraInfo-${currentLang}`" class="pi pi-spin pi-spinner"></i>
          <i v-else class="pi pi-sparkles"></i>
        </button>
      </div>
    </div>

  </div>
</template>

<style scoped>
  .field {
    margin-bottom: 1.5rem;
  }

    .field label {
      display: block;
      margin-bottom: 0.5rem;
      font-weight: bold;
      color: #333;
    }

  .w-full {
    width: 100%;
  }

  /* Varázspálca UI */
  .input-with-tools {
    position: relative;
    display: flex;
    align-items: flex-start;
  }

  .magic-btn {
    opacity: 0.3;
    background: none;
    border: none;
    color: var(--primary-color, #d4af37);
    cursor: pointer;
    margin-left: 8px;
    margin-top: 10px;
    font-size: 1.2rem;
    transition: opacity 0.2s, transform 0.2s;
  }

  .input-with-tools:hover .magic-btn {
    opacity: 1;
  }

  .magic-btn:hover {
    transform: scale(1.1);
    text-shadow: 0 0 5px var(--primary-color, #d4af37);
  }
</style>
