<script setup>
  import { ref, onMounted, onUnmounted, computed } from 'vue';
  import { useTranslationStore } from '@/stores/translationStore';
  import { useToast } from 'primevue/usetoast';
  import DataTable from 'primevue/datatable';
  import Column from 'primevue/column';
  import Button from 'primevue/button';
  import Dialog from 'primevue/dialog';
  import Select from 'primevue/select'; 
  import Tag from 'primevue/tag';
  import Checkbox from 'primevue/checkbox';
  import { jwtDecode } from "jwt-decode";
  import ProgressBar from 'primevue/progressbar';

  const store = useTranslationStore();
  const toast = useToast();

  const isDialogVisible = ref(false);
  const selectedLangCode = ref(null); 
  const useAi = ref(true);
  const loading = ref(false);
  let pollingInterval = null; 

  const allLanguages = [
    { label: 'Magyar', value: 'hu' },
    { label: 'Slovenský (Szlovák)', value: 'sk' },
    { label: 'English (Angol)', value: 'en' },
    { label: 'Deutsch (Német)', value: 'de' },
    { label: 'Română (Román)', value: 'ro' },
    { label: 'Українська (Ukrán)', value: 'ua' },
    { label: 'Français (Francia)', value: 'fr' },
    { label: 'Italiano (Olasz)', value: 'it' },
    { label: 'Español (Spanyol)', value: 'es' },
    { label: 'Pусский (Orosz)', value: 'ru' }
  ];

  const availableLanguagesOptions = computed(() => {
    return allLanguages.filter(lang =>
      lang.value !== store.activeCompanyDefaultLang &&
      !store.languages.some(added => added.languageCode === lang.value)
    );
  });

  const getCompanyId = () => {
    const token = localStorage.getItem('salon_token');
    if (!token) return 0;
    try {
      const decoded = jwtDecode(token);
      return parseInt(decoded.CompanyId || decoded.companyId || 0);
    } catch (e) {
      return 0;
    }
  };

  const companyId = getCompanyId();

  const startPolling = () => {
    if (pollingInterval) return; 

    pollingInterval = setInterval(async () => {
      await store.fetchLanguages(companyId);
      const isTranslating = store.languages.some(l => l.status === 'Translating');

      if (!isTranslating) {
        stopPolling();
        toast.add({ severity: 'success', summary: 'Kész!', detail: 'A fordítás befejeződött.', life: 3000 });
      }
    }, 3000); 
  };

  const stopPolling = () => {
    if (pollingInterval) {
      clearInterval(pollingInterval);
      pollingInterval = null;
    }
  };

  onMounted(async () => {
    if (companyId) {
      await store.fetchLanguages(companyId);
      if (store.languages.some(l => l.status === 'Translating')) {
        startPolling();
      }
    }
  });

  onUnmounted(() => {
    stopPolling(); 
  });

  const getSeverity = (status) => {
    switch (status) {
      case 'Published': return 'success';
      case 'ReviewPending': return 'warn';
      case 'Translating': return 'info';
      case 'Error': return 'danger';
      default: return 'secondary';
    }
  };

  const getStatusLabel = (status) => {
    switch (status) {
      case 'Published': return 'Publikus';
      case 'ReviewPending': return 'Ellenőrzésre vár';
      case 'Translating': return 'Fordítás alatt...';
      case 'Error': return 'Hiba';
      case 'Created': return 'Létrehozva';
      default: return status;
    }
  };

  const onAddLanguage = async () => {
    if (!selectedLangCode.value) return;

    loading.value = true;
    try {
      await store.addLanguage(companyId, selectedLangCode.value, useAi.value);

      isDialogVisible.value = false;
      selectedLangCode.value = null;
      useAi.value = true;

      if (useAi.value) {
        toast.add({ severity: 'info', summary: 'Folyamatban', detail: 'Fordítás elindítva, kérlek várj...', life: 3000 });
        startPolling(); 
      } else {
        toast.add({ severity: 'success', summary: 'Siker', detail: 'Nyelv létrehozva.', life: 3000 });
      }

    } catch (error) {
      toast.add({ severity: 'error', summary: 'Hiba', detail: 'Nem sikerült hozzáadni a nyelvet.', life: 3000 });
    } finally {
      loading.value = false;
    }
  };

  const onPublish = async (langCode) => {
    try {
      await store.publishLanguage(companyId, langCode);
      toast.add({ severity: 'success', summary: 'Publikálva', detail: 'A nyelv mostantól elérhető.', life: 3000 });
    } catch (error) {
      toast.add({ severity: 'error', summary: 'Hiba', detail: 'Hiba a publikálás során.', life: 3000 });
    }
  };

  const onDelete = async (langCode) => {
    if (!confirm(`Biztosan törölni szeretnéd a(z) ${langCode.toUpperCase()} nyelvet? Ez a művelet nem vonható vissza.`)) return;

    try {
      await store.deleteLanguage(companyId, langCode);
      toast.add({ severity: 'success', summary: 'Törölve', detail: 'A nyelv sikeresen törölve.', life: 3000 });
    } catch (error) {
      toast.add({ severity: 'error', summary: 'Hiba', detail: 'Nem sikerült törölni a nyelvet.', life: 3000 });
    }
  };
</script>

<template>
  <div class="bg-surface border border-text/10 rounded-2xl p-4 md:p-6 shadow-lg mb-8">
    
    <div class="flex flex-col sm:flex-row justify-between items-start sm:items-center mb-6 gap-4 border-b border-text/10 pb-4">
      <div>
        <h2 class="text-2xl font-light tracking-wide text-primary m-0">Nyelvek kezelése</h2>
        <p class="text-text-muted text-sm mt-1 m-0">Telepített nyelvek és automatikus fordítások kezelése.</p>
      </div>
      <Button label="Új nyelv hozzáadása" icon="pi pi-plus" @click="isDialogVisible = true" 
              class="!bg-primary !text-black !font-bold !border-none !rounded-xl !px-5 !py-2.5 hover:!scale-105 transition-transform shadow-md" />
    </div>

    <div class="overflow-hidden rounded-xl border border-text/5">
      <DataTable :value="store.languages" 
                 class="w-full
                 [&_.p-datatable-header]:bg-transparent
                 [&_.p-datatable-thead>tr>th]:bg-text/5 [&_.p-datatable-thead>tr>th]:text-text-muted [&_.p-datatable-thead>tr>th]:font-semibold [&_.p-datatable-thead>tr>th]:border-b [&_.p-datatable-thead>tr>th]:border-text/10 [&_.p-datatable-thead>tr>th]:py-3
                 [&_.p-datatable-tbody>tr]:bg-transparent [&_.p-datatable-tbody>tr]:text-text [&_.p-datatable-tbody>tr]:border-b [&_.p-datatable-tbody>tr]:border-text/5 hover:[&_.p-datatable-tbody>tr]:bg-text/5 [&_.p-datatable-tbody>tr]:transition-colors
                 [&_.p-datatable-empty-message>td]:bg-transparent [&_.p-datatable-empty-message>td]:text-text-muted [&_.p-datatable-empty-message>td]:text-center [&_.p-datatable-empty-message>td]:py-8">
        
        <Column field="languageCode" header="Nyelv">
          <template #body="slotProps">
            <div class="flex items-center">
              <span class="font-bold text-lg uppercase text-primary tracking-wider">{{ slotProps.data.languageCode }}</span>
              <span v-if="slotProps.data.isDefault" class="ml-3 text-xs font-semibold px-2 py-1 bg-text/10 text-text-muted rounded-full tracking-wide">Alapértelmezett</span>
            </div>
          </template>
        </Column>

        <Column field="status" header="Státusz" style="min-width: 200px;">
          <template #body="slotProps">
            <div class="flex flex-col gap-1.5 w-full max-w-[250px]">
              
              <div v-if="slotProps.data.status === 'Translating'" class="w-full">
                <div class="flex justify-between items-center mb-1.5">
                  <span class="text-xs text-primary font-bold tracking-wide uppercase"><i class="pi pi-spin pi-cog mr-1"></i> Fordítás...</span>
                  <span class="text-xs text-text-muted font-bold">{{ slotProps.data.progress || 0 }}%</span>
                </div>
                <ProgressBar :value="slotProps.data.progress || 0" :showValue="false" 
                             pt:root:class="!bg-text/10 !h-1.5 !rounded-full !border-none" 
                             pt:value:class="!bg-primary !rounded-full" />
              </div>

              <Tag v-else :value="getStatusLabel(slotProps.data.status)" :severity="getSeverity(slotProps.data.status)" class="w-max" />

            </div>
          </template>
        </Column>

        <Column header="Műveletek" alignFrozen="right" style="width: 150px;">
          <template #body="slotProps">
            <div class="flex gap-2 justify-end">
              <Button v-if="slotProps.data.status === 'ReviewPending'"
                      label="Publikálás"
                      icon="pi pi-check"
                      class="!bg-green-500/20 !text-green-500 !border-none !font-bold hover:!bg-green-500/30 !text-xs !px-3 !py-1.5 !whitespace-nowrap"
                      @click="onPublish(slotProps.data.languageCode)" />

              <Button v-if="!slotProps.data.isDefault"
                      icon="pi pi-trash"
                      class="!bg-transparent !text-text-muted !border-none hover:!bg-red-500/10 hover:!text-red-500 !p-2 !w-8 !h-8 transition-colors rounded-full"
                      aria-label="Törlés"
                      @click="onDelete(slotProps.data.languageCode)" />
            </div>
          </template>
        </Column>

      </DataTable>
    </div>

    <Dialog v-model:visible="isDialogVisible" modal header="Új nyelv hozzáadása" 
            pt:root:class="!bg-surface !border !border-text/10 !rounded-2xl shadow-2xl overflow-hidden max-w-[95vw] w-[28rem]"
            pt:header:class="!bg-text/5 !text-text !border-b !border-text/10 !px-6 !py-4"
            pt:title:class="!text-xl !font-light !text-primary !tracking-wide"
            pt:content:class="!bg-transparent !text-text !px-6 !py-6"
            pt:footer:class="!bg-transparent !border-t !border-text/10 !px-6 !py-4">
      
      <div class="flex flex-col gap-6">

        <div class="flex flex-col gap-2">
          <label for="langSelect" class="font-bold text-text-muted text-sm uppercase tracking-wide">Válassz nyelvet</label>
          <Select id="langSelect"
                  v-model="selectedLangCode"
                  :options="availableLanguagesOptions"
                  optionLabel="label"
                  optionValue="value"
                  placeholder="Válassz a listából..."
                  class="w-full !bg-background !border-text/20 !text-text hover:!border-primary focus:!border-primary focus:!ring-1 focus:!ring-primary transition-colors"
                  pt:panel:class="!bg-surface !border !border-text/10 !text-text !shadow-xl !rounded-lg"
                  pt:list:class="!p-1"
                  pt:option:class="hover:!bg-text/5 !text-text !rounded-md !transition-colors !m-0.5"
                  pt:optionLabel:class="!font-medium" />
        </div>

        <div class="bg-text/5 border border-text/10 p-4 rounded-xl">
          <div class="flex items-start gap-3 mt-1">

            <Checkbox v-model="useAi" :binary="true" inputId="aiCheck"
                      :pt="{
                      box: ({ context })=>
              ({
              class: [
              'border-2 w-6 h-6 flex items-center justify-center rounded transition-colors cursor-pointer',
              context.checked ? '!bg-primary !border-primary' : '!bg-background !border-text/30 hover:!border-primary'
              ]
              }),
              icon: { class: '!text-black w-4 h-4 font-bold' }
              }" />

              <div class="flex flex-col">
                <label for="aiCheck" class="cursor-pointer select-none font-bold text-primary mb-1">
                  Automatikus AI fordítás kérése
                </label>
                <span v-if="useAi" class="text-text-muted text-sm leading-snug">
                  A rendszer lefordítja a szolgáltatásokat és a gombokat. Ez 1-2 percet vehet igénybe.
                </span>
                <span v-else class="text-text-muted text-sm leading-snug">
                  Létrejön egy üres nyelvi profil, amit neked kell feltöltened tartalommal.
                </span>
              </div>
          </div>
        </div>

      </div>

      <template #footer>
        <div class="flex justify-end gap-3 w-full">
          <Button type="button" label="Mégsem" @click="isDialogVisible = false" 
                  class="!bg-transparent !text-text-muted !border-none hover:!bg-text/5 !px-4 !py-2 !font-bold !rounded-lg transition-colors"></Button>
          <Button type="button" label="Hozzáadás" @click="onAddLanguage" :loading="loading" :disabled="!selectedLangCode"
                  class="!bg-primary !text-black !font-bold !border-none !px-6 !py-2 !rounded-lg hover:!scale-105 transition-transform disabled:!bg-gray-600 disabled:!text-gray-400 disabled:!scale-100 disabled:!cursor-not-allowed shadow-md"></Button>
        </div>
      </template>

    </Dialog>
  </div>
</template>
