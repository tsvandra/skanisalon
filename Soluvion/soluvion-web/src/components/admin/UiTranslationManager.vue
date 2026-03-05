<script setup>
  import { ref, computed, onMounted, watch } from 'vue';
  import { useTranslationStore } from '@/stores/translationStore';
  import { useI18n } from 'vue-i18n';
  import api from '@/services/api';

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

  const flattenObject = (obj, prefix = '') => {
    return Object.keys(obj).reduce((acc, k) => {
      const pre = prefix.length ? prefix + '.' : '';
      if (typeof obj[k] === 'object' && obj[k] !== null && !Array.isArray(obj[k])) {
        Object.assign(acc, flattenObject(obj[k], pre + k));
      } else {
        acc[pre + k] = String(obj[k]);
      }
      return acc;
    }, {});
  };

  const baseKeys = computed(() => {
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
        original: baseKeys.value[key]
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
  <div class="bg-surface border border-text/10 rounded-2xl p-4 md:p-6 shadow-lg">

    <div class="flex flex-col sm:flex-row justify-between items-start sm:items-center mb-6 gap-4 border-b border-text/10 pb-4">
      <div>
        <h3 class="text-2xl font-light tracking-wide text-primary m-0 mb-1">Szövegek testreszabása</h3>
        <p class="text-text-muted text-sm m-0 leading-snug">Itt írhatod át a weboldal fix szövegeit. Töröld ki a mezőt az alapértelmezett érték visszaállításához.</p>
      </div>

      <Select v-model="selectedLang"
              :options="languageOptions"
              optionLabel="label"
              optionValue="value"
              placeholder="Nyelv választása"
              class="w-full sm:w-[200px] !bg-background !border-text/20 !text-text hover:!border-primary focus:!border-primary focus:!ring-1 focus:!ring-primary transition-colors shadow-sm"
              pt:panel:class="!bg-surface !border !border-text/10 !text-text !shadow-xl !rounded-lg"
              pt:list:class="!p-1"
              pt:option:class="hover:!bg-text/5 !text-text !rounded-md !transition-colors !m-0.5"
              pt:optionLabel:class="!font-medium" />
    </div>

    <div v-if="isLoading" class="flex justify-center items-center py-8">
      <i class="pi pi-spin pi-spinner text-3xl text-primary opacity-50"></i>
    </div>

    <Accordion v-else multiple :value="['0']" class="flex flex-col gap-3">
      <AccordionPanel v-for="(items, groupName) in groupedKeys" :key="groupName" :value="groupName"
                      pt:root:class="!border !border-text/10 !rounded-xl overflow-hidden !bg-transparent">

        <AccordionHeader pt:root:class="!bg-text/5 !border-b-0 hover:!bg-text/10 !text-text !px-5 !py-4 transition-colors">
          <span class="font-bold text-lg tracking-widest text-primary">{{ groupName.toUpperCase() }}</span>
        </AccordionHeader>

        <AccordionContent pt:content:class="!bg-background !text-text !p-0 !border-t !border-text/10">

          <div v-for="item in items" :key="item.key" class="flex flex-col md:flex-row justify-between items-start md:items-center p-4 md:p-5 border-b border-text/5 hover:bg-text/5 transition-colors gap-4">

            <div class="flex flex-col w-full md:w-5/12 shrink-0">
              <span class="font-bold text-sm text-text-muted tracking-wide break-all">{{ item.key }}</span>
              <small class="text-xs text-text/50 mt-1 italic leading-tight">Alap (HU): {{ item.original }}</small>
            </div>

            <div class="w-full md:w-7/12 flex-grow">
              <div class="flex w-full shadow-sm rounded-lg overflow-hidden border border-text/10 focus-within:border-primary focus-within:ring-1 focus-within:ring-primary transition-all">

                <InputText v-model="overrides[item.key]"
                           :placeholder="item.original"
                           class="w-full !border-none !bg-background !text-text !p-3 focus:!outline-none placeholder:italic placeholder:text-text/30" />

                <Button icon="pi pi-check"
                        @click="saveOverride(item.key, overrides[item.key])"
                        class="!bg-primary !text-black !border-none !rounded-none !w-12 hover:!brightness-110 transition-all shrink-0"
                        title="Mentés" />
              </div>
            </div>

          </div>

        </AccordionContent>
      </AccordionPanel>
    </Accordion>

  </div>
</template>
