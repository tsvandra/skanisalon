<script setup>
  import { ref, computed, onMounted, watch } from 'vue';
  import { useTranslationStore } from '@/stores/translationStore';
  import { useI18n } from 'vue-i18n';
  import api from '@/services/api';
  // PrimeVue komponensek
  import Select from 'primevue/select';
  import InputText from 'primevue/inputtext';
  import Button from 'primevue/button';
  import Accordion from 'primevue/accordion';
  import AccordionPanel from 'primevue/accordionpanel';
  import AccordionHeader from 'primevue/accordionheader';
  import AccordionContent from 'primevue/accordioncontent';
  import { useToast } from 'primevue/usetoast';

  const store = useTranslationStore();
  const { messages } = useI18n();
  const toast = useToast();

  const selectedLang = ref('hu');
  const overrides = ref({});
  const isLoading = ref(false);

  const languageOptions = computed(() => {
    return store.languages
      .filter(l => l.status !== 'Created')
      .map(l => ({ label: l.languageCode.toUpperCase(), value: l.languageCode }));
  });

  // Biztonságos Flatten
  const flattenObject = (obj, prefix = '') => {
    return Object.keys(obj).reduce((acc, k) => {
      const pre = prefix.length ? prefix + '.' : '';
      if (typeof obj[k] === 'object' && obj[k] !== null && !Array.isArray(obj[k])) {
        Object.assign(acc, flattenObject(obj[k], pre + k));
      } else {
        acc[pre + k] = String(obj[k]); // Kényszerített string konverzió a biztonság kedvéért
      }
      return acc;
    }, {});
  };

  const baseKeys = computed(() => {
    // Mindig a betöltött magyar üzenetekből indulunk ki
    const huMessages = messages.value['hu'] || {};
    return flattenObject(huMessages);
  });

  const groupedKeys = computed(() => {
    const groups = {};
    Object.keys(baseKeys.value).forEach(key => {
      const groupName = key.split('.')[0];
      if (!groups[groupName]) groups[groupName] = [];
      groups[groupName].push({
        key: key,
        original: baseKeys.value[key] // Ez biztosan string
      });
    });
    return groups;
  });

  const loadOverrides = async () => {
    if (!store.activeCompanyId) return;
    isLoading.value = true;
    try {
      const res = await api.get(`/api/Translation/overrides/${store.activeCompanyId}/${selectedLang.value}`);
      overrides.value = res.data || {};
    } catch (err) {
      console.error("Hiba a felülírások betöltésekor", err);
    } finally {
      isLoading.value = false;
    }
  };

  const saveOverride = async (key, newValue) => {
    if (!store.activeCompanyId) return;

    try {
      await api.post('/api/Translation/save-override', {
        companyId: store.activeCompanyId,
        languageCode: selectedLang.value,
        key: key,
        value: newValue
      });

      await store.loadOverrides(store.activeCompanyId, selectedLang.value);

      toast.add({ severity: 'success', summary: 'Mentve', detail: 'A szöveg frissült.', life: 2000 });
    } catch (err) {
      toast.add({ severity: 'error', summary: 'Hiba', detail: 'Nem sikerült menteni.', life: 3000 });
    }
  };

  watch(selectedLang, () => {
    loadOverrides();
  });

  onMounted(() => {
    if (store.activeCompanyId) {
      loadOverrides();
    }
  });
</script>

<template>
  <div class="ui-translator">
    <div class="header">
      <h3>Szövegek testreszabása</h3>
      <div class="controls">
        <Select v-model="selectedLang" :options="languageOptions" optionLabel="label" optionValue="value" placeholder="Nyelv választása" />
      </div>
    </div>

    <p class="info-text">Itt írhatod át a weboldal fix szövegeit. Töröld ki a mezőt az alapértelmezett érték visszaállításához.</p>

    <div v-if="isLoading">Betöltés...</div>

    <Accordion v-else multiple :value="['0']">
      <AccordionPanel v-for="(items, groupName) in groupedKeys" :key="groupName" :value="groupName">
        <AccordionHeader>{{ groupName.toUpperCase() }}</AccordionHeader>
        <AccordionContent>

          <div v-for="item in items" :key="item.key" class="translation-row">
            <div class="key-info">
              <span class="key-label">{{ item.key }}</span>
              <small class="original-text">Alap (HU): {{ item.original }}</small>
            </div>

            <div class="input-wrapper">
              <div class="p-inputgroup">
                <InputText v-model="overrides[item.key]"
                           :placeholder="item.original"
                           class="w-full" />
                <Button icon="pi pi-check" @click="saveOverride(item.key, overrides[item.key])" />
              </div>
            </div>
          </div>

        </AccordionContent>
      </AccordionPanel>
    </Accordion>
  </div>
</template>

<style scoped>
  /* Stílusok változatlanok */
  .ui-translator {
    margin-top: 2rem;
    background: #fff;
    padding: 1.5rem;
    border-radius: 8px;
    box-shadow: 0 2px 10px rgba(0,0,0,0.05);
  }

  .header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1rem;
  }

  h3 {
    margin: 0;
    color: #333;
  }

  .info-text {
    color: #666;
    margin-bottom: 1.5rem;
    font-size: 0.9rem;
  }

  .translation-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 10px 0;
    border-bottom: 1px solid #eee;
  }

    .translation-row:last-child {
      border-bottom: none;
    }

  .key-info {
    display: flex;
    flex-direction: column;
    max-width: 40%;
  }

  .key-label {
    font-weight: bold;
    font-size: 0.9rem;
    color: #444;
  }

  .original-text {
    color: #888;
    font-size: 0.8rem;
  }

  .input-wrapper {
    flex-grow: 1;
    max-width: 50%;
  }

  .p-inputgroup button {
    background: var(--primary-color);
    border-color: var(--primary-color);
  }

  .w-full {
    width: 100%;
  }
</style>
