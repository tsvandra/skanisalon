<script setup>
  import { ref, computed, onMounted, watch } from 'vue';
  import { useTranslationStore } from '@/stores/translationStore';
  import { translationApi } from '@/services/translationApi'; // ÚJ: API import
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
  const { messages, t } = useI18n();
  const toast = useToast();

  const selectedLang = ref('hu');
  const overrides = ref({});
  const isLoading = ref(false);

  // Állapotok a töltésjelzőkhöz (spinners)
  const translatingItems = ref({});
  const translatingGroups = ref({});

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

  const searchQuery = ref('');

  const getTranslatedValue = (key) => {
    if (overrides.value && overrides.value[key] !== undefined && overrides.value[key] !== '') {
      return overrides.value[key];
    }
    const langMsgs = messages.value[selectedLang.value] || {};
    const keys = key.split('.');
    let curr = langMsgs;
    for (const k of keys) {
      if (curr && typeof curr === 'object' && k in curr) {
        curr = curr[k];
      } else {
        return '';
      }
    }
    return typeof curr === 'string' ? curr : '';
  };

  const filteredGroupedKeys = computed(() => {
    const q = searchQuery.value.trim().toLowerCase();
    const groups = {};

    Object.keys(baseKeys.value).forEach(key => {
      const groupName = key.split('.')[0];
      const originalText = String(baseKeys.value[key] || '');
      const currentOverride = String(overrides.value[key] || '');
      const currentTranslated = String(getTranslatedValue(key) || '');

      let matches = true;
      if (q) {
        matches = key.toLowerCase().includes(q) ||
                  originalText.toLowerCase().includes(q) ||
                  currentOverride.toLowerCase().includes(q) ||
                  currentTranslated.toLowerCase().includes(q);
      }

      if (matches) {
        if (!groups[groupName]) groups[groupName] = [];
        groups[groupName].push({
          key: key,
          original: originalText,
          currentValue: currentTranslated
        });
      }
    });

    return groups;
  });

  const totalMatchesCount = computed(() => {
    return Object.values(filteredGroupedKeys.value).reduce((acc, items) => acc + items.length, 0);
  });

  const activePanels = ref(['home', 'nav']);
  const accordionValue = computed({
    get() {
      if (searchQuery.value.trim().length > 0) {
        return Object.keys(filteredGroupedKeys.value);
      }
      return activePanels.value;
    },
    set(val) {
      if (searchQuery.value.trim().length === 0) {
        activePanels.value = val;
      }
    }
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
      toast.add({ severity: 'success', summary: t('adminSettings.uiTranslations.toast.savedSummary'), detail: t('adminSettings.uiTranslations.toast.savedDetail'), life: 2000 });
    } catch (err) {
      toast.add({ severity: 'error', summary: t('adminSettings.uiTranslations.toast.errorSummary'), detail: t('adminSettings.uiTranslations.toast.saveFailed'), life: 3000 });
    }
  };

  // ÚJ: EGYEDI ELEM FORDÍTÁSA
  const autoTranslateItem = async (item) => {
    if (!store.activeCompanyId || selectedLang.value === 'hu') return;

    translatingItems.value[item.key] = true;
    try {
      const res = await translationApi.translateSingleText(item.original, selectedLang.value, 'ui');
      if (res.data && res.data.translatedText) {
        overrides.value[item.key] = res.data.translatedText;
        // Automatikusan el is mentjük a fordítást
        await saveOverride(item.key, res.data.translatedText);
      }
    } catch (err) {
      const serverMessage = typeof err.response?.data === 'string' ? err.response.data : err.response?.data?.message;
      console.error(`Fordítási hiba (${item.key}):`, err.response?.status, serverMessage || err);
      toast.add({
        severity: 'error',
        summary: t('adminSettings.uiTranslations.toast.errorSummary'),
        detail: serverMessage ? t('adminSettings.uiTranslations.toast.translateFailedWithMessage', { message: serverMessage }) : t('adminSettings.uiTranslations.toast.translateFailed'),
        life: 8000
      });
    } finally {
      translatingItems.value[item.key] = false;
    }
  };

  // ÚJ: TELJES CSOPORT FORDÍTÁSA (pl. "booking")
  const autoTranslateGroup = async (groupName, items, event) => {
    event.stopPropagation(); // Megakadályozza, hogy az Accordion kinyíljon/becsukódjon a gombnyomásra
    if (!store.activeCompanyId || selectedLang.value === 'hu') return;

    translatingGroups.value[groupName] = true;
    toast.add({ severity: 'info', summary: t('adminSettings.uiTranslations.toast.groupStartedSummary'), detail: t('adminSettings.uiTranslations.toast.groupStartedDetail', { group: groupName.toUpperCase() }), life: 3000 });

    try {
      // Egymás után fordítunk, hogy ne terheljük túl az OpenAI Rate Limitjét
      for (const item of items) {
        // Csak azt fordítjuk, ami még nincs lefordítva (vagy üres)
        if (!overrides.value[item.key] || overrides.value[item.key].trim() === '') {
          translatingItems.value[item.key] = true;
          try {
            const res = await translationApi.translateSingleText(item.original, selectedLang.value, 'ui');
            if (res.data && res.data.translatedText) {
              overrides.value[item.key] = res.data.translatedText;
              // Csak az Inputba tesszük be, menteni egyben is lehetne, de a biztonság kedvéért soronként mentjük
              await api.post('/api/Translation/save-override', {
                companyId: store.activeCompanyId,
                languageCode: selectedLang.value,
                key: item.key,
                value: res.data.translatedText
              });
            }
          } catch (e) {
            console.error(`Hiba a ${item.key} elem fordításakor`);
          } finally {
            translatingItems.value[item.key] = false;
          }
        }
      }
      // Újratöltjük a store-t a végén, hogy mindenhol frissüljön
      await store.loadOverrides(store.activeCompanyId, selectedLang.value);
      toast.add({ severity: 'success', summary: t('adminSettings.uiTranslations.toast.groupDoneSummary'), detail: t('adminSettings.uiTranslations.toast.groupDoneDetail', { group: groupName.toUpperCase() }), life: 3000 });
    } finally {
      translatingGroups.value[groupName] = false;
    }
  };

  const isResetting = ref(false);

  const resetAllOverrides = async () => {
    if (!confirm(t('adminSettings.uiTranslations.confirmReset', { lang: selectedLang.value.toUpperCase() }))) return;

    isResetting.value = true;
    try {
      await store.resetOverrides(store.activeCompanyId, selectedLang.value);
      overrides.value = {};
      toast.add({
        severity: 'success',
        summary: t('adminSettings.uiTranslations.toast.resetSuccessSummary'),
        detail: t('adminSettings.uiTranslations.toast.resetSuccessDetail'),
        life: 3000
      });
    } catch (err) {
      toast.add({
        severity: 'error',
        summary: t('adminSettings.uiTranslations.toast.errorSummary'),
        detail: t('adminSettings.uiTranslations.toast.resetFailed'),
        life: 3000
      });
    } finally {
      isResetting.value = false;
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
        <h3 class="text-2xl font-light tracking-wide text-primary m-0 mb-1">{{ $t('adminSettings.uiTranslations.title') }}</h3>
        <p class="text-text-muted text-sm m-0 leading-snug">{{ $t('adminSettings.uiTranslations.subtitle') }}</p>
      </div>

      <div class="flex items-center gap-3 w-full sm:w-auto">
        <Button v-if="Object.keys(overrides).length > 0"
                icon="pi pi-history"
                :label="$t('adminSettings.uiTranslations.resetToDefaults')"
                @click="resetAllOverrides"
                :loading="isResetting"
                class="!bg-transparent !text-text-muted hover:!text-red-400 !border !border-text/10 hover:!border-red-400/30 !rounded-xl !px-3 !py-2 !text-xs transition-colors shrink-0" />

        <Select v-model="selectedLang"
                :options="languageOptions"
                optionLabel="label"
                optionValue="value"
                :placeholder="$t('adminSettings.uiTranslations.selectLanguage')"
                class="w-full sm:w-[180px] !bg-background !border-text/20 !text-text hover:!border-primary focus:!border-primary focus:!ring-1 focus:!ring-primary transition-colors shadow-sm"
                pt:panel:class="!bg-surface !border !border-text/10 !text-text !shadow-xl !rounded-lg"
                pt:list:class="!p-1"
                pt:option:class="hover:!bg-text/5 !text-text !rounded-md !transition-colors !m-0.5"
                pt:optionLabel:class="!font-medium" />
      </div>
    </div>

    <!-- KERESŐ SÁV ÉS TALÁLATSZÁMLÁLÓ -->
    <div class="mb-6 flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3 bg-text/5 p-3 rounded-xl border border-text/10">
      <div class="relative flex-1">
        <i class="pi pi-search absolute left-3.5 top-1/2 -translate-y-1/2 text-text-muted text-sm pointer-events-none"></i>
        <InputText v-model="searchQuery"
                   :placeholder="$t('adminSettings.uiTranslations.searchPlaceholder')"
                   class="w-full !pl-10 !pr-10 !py-2 !bg-background !border-text/10 !text-text !rounded-lg focus:!border-primary focus:!ring-1 focus:!ring-primary transition-all text-sm" />
        <button v-if="searchQuery"
                @click="searchQuery = ''"
                type="button"
                class="absolute right-3 top-1/2 -translate-y-1/2 text-text-muted hover:text-text p-1 transition-colors">
          <i class="pi pi-times text-xs"></i>
        </button>
      </div>

      <div class="flex items-center justify-between sm:justify-end gap-3 text-xs text-text-muted px-1">
        <span v-if="searchQuery.trim()" class="font-medium text-primary flex items-center gap-1.5">
          <i class="pi pi-filter"></i>
          {{ $t('adminSettings.uiTranslations.searchResults', { count: totalMatchesCount }) }}
        </span>
        <span v-else class="text-text-muted/60">
          {{ Object.keys(baseKeys).length }} {{ $t('adminSettings.uiTranslations.totalPhrases') }}
        </span>
      </div>
    </div>

    <div v-if="isLoading" class="flex justify-center items-center py-8">
      <i class="pi pi-spin pi-spinner text-3xl text-primary opacity-50"></i>
    </div>

    <!-- NINCS TALÁLAT ÁLLAPOT -->
    <div v-else-if="totalMatchesCount === 0" class="flex flex-col items-center justify-center py-12 text-center text-text-muted bg-text/5 rounded-xl border border-text/10">
      <i class="pi pi-search text-3xl mb-3 text-primary opacity-60"></i>
      <p class="font-medium text-sm m-0">{{ $t('adminSettings.uiTranslations.noResults') }}</p>
      <button @click="searchQuery = ''" type="button" class="mt-3 text-xs text-primary font-bold hover:underline transition-colors">
        {{ $t('adminSettings.uiTranslations.clearSearch') }}
      </button>
    </div>

    <!-- ACCORDION CSOPORTOK -->
    <Accordion v-else multiple :value="accordionValue" class="flex flex-col gap-3">
      <AccordionPanel v-for="(items, groupName) in filteredGroupedKeys" :key="groupName" :value="groupName"
                      pt:root:class="!border !border-text/10 !rounded-xl overflow-hidden !bg-transparent">

        <AccordionHeader pt:root:class="!bg-text/5 !border-b-0 hover:!bg-text/10 !text-text !px-5 !py-4 transition-colors">
          <div class="flex justify-between items-center w-full">
            <div class="flex items-center gap-2">
              <span class="font-bold text-lg tracking-widest text-primary">{{ groupName.toUpperCase() }}</span>
              <span class="text-xs text-text-muted/60 px-2 py-0.5 rounded-full bg-text/5 font-semibold">
                {{ items.length }}
              </span>
            </div>

            <Button v-if="selectedLang !== 'hu'"
                    icon="pi pi-sparkles"
                    :label="$t('adminSettings.uiTranslations.groupAiTranslate')"
                    @click="(e) => autoTranslateGroup(groupName, items, e)"
                    :loading="translatingGroups[groupName]"
                    class="!bg-primary/10 !text-primary !border-none !py-1 !px-3 !text-xs hover:!bg-primary hover:!text-black transition-colors" />
          </div>
        </AccordionHeader>

        <AccordionContent pt:content:class="!bg-background !text-text !p-0 !border-t !border-text/10">

          <div v-for="item in items" :key="item.key" class="flex flex-col md:flex-row justify-between items-start md:items-center p-4 md:p-5 border-b border-text/5 hover:bg-text/5 transition-colors gap-4">

            <div class="flex flex-col w-full md:w-5/12 shrink-0">
              <div class="flex items-center gap-2 flex-wrap">
                <span class="font-bold text-sm text-text-muted tracking-wide break-all">{{ item.key }}</span>
                <span v-if="overrides[item.key]" class="text-[10px] font-bold px-1.5 py-0.5 rounded bg-primary/20 text-primary border border-primary/30 uppercase tracking-wider">
                  {{ $t('adminSettings.uiTranslations.customBadge') }}
                </span>
              </div>
              <small class="text-xs text-text/50 mt-1 italic leading-tight">{{ $t('adminSettings.uiTranslations.original', { text: item.original }) }}</small>
            </div>

            <div class="w-full md:w-7/12 flex-grow">
              <div class="flex w-full shadow-sm rounded-lg overflow-hidden border border-text/10 focus-within:border-primary focus-within:ring-1 focus-within:ring-primary transition-all">

                <InputText v-model="overrides[item.key]"
                           :placeholder="item.currentValue || item.original"
                           class="w-full !border-none !bg-background !text-text !p-3 focus:!outline-none placeholder:italic placeholder:text-text/30" />

                <Button v-if="selectedLang !== 'hu'"
                        icon="pi pi-sparkles"
                        @click="autoTranslateItem(item)"
                        :loading="translatingItems[item.key]"
                        class="!bg-surface !text-primary !border-none border-l border-text/10 !rounded-none !w-12 hover:!bg-primary/20 transition-all shrink-0"
                        :title="$t('adminSettings.uiTranslations.aiTranslate')" />

                <Button icon="pi pi-check"
                        @click="saveOverride(item.key, overrides[item.key])"
                        class="!bg-primary !text-black !border-none !rounded-none !w-12 hover:!brightness-110 transition-all shrink-0"
                        :title="$t('adminSettings.uiTranslations.save')" />
              </div>
            </div>

          </div>

        </AccordionContent>
      </AccordionPanel>
    </Accordion>

  </div>
</template>
