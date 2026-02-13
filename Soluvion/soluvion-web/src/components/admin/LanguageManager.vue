<script setup>
  import { ref, onMounted, onUnmounted, computed, watch } from 'vue';
  import { useTranslationStore } from '@/stores/translationStore';
  import { useToast } from 'primevue/usetoast';
  import DataTable from 'primevue/datatable';
  import Column from 'primevue/column';
  import Button from 'primevue/button';
  import Dialog from 'primevue/dialog';
  import Select from 'primevue/select'; // Dropdown helyett
  import Tag from 'primevue/tag';
  import Checkbox from 'primevue/checkbox';
  import { jwtDecode } from "jwt-decode";
  import ProgressBar from 'primevue/progressbar';

  const store = useTranslationStore();
  const toast = useToast();

  const isDialogVisible = ref(false);
  const selectedLangCode = ref(null); // InputText helyett Select model
  const useAi = ref(true);
  const loading = ref(false);
  let pollingInterval = null; // Időzítő változó

  // --- 3. PROBLÉMA MEGOLDÁSA: Elérhető nyelvek listája ---
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

  // Szűrjük a listát: Ne mutassa a cég alapnyelvét és a már hozzáadottakat
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

  // --- 2. PROBLÉMA MEGOLDÁSA: POLLING (Folyamatos ellenőrzés) ---
  const startPolling = () => {
    if (pollingInterval) return; // Már fut

    pollingInterval = setInterval(async () => {
      // Frissítjük a listát
      await store.fetchLanguages(companyId);

      // Megnézzük, van-e még folyamatban lévő fordítás
      const isTranslating = store.languages.some(l => l.status === 'Translating');

      // Ha nincs már "Translating", leállítjuk a pollingot és jelezzük a sikert
      if (!isTranslating) {
        stopPolling();
        toast.add({ severity: 'success', summary: 'Kész!', detail: 'A fordítás befejeződött.', life: 3000 });
      }
    }, 3000); // 3 másodpercenként ellenőriz
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
      // Ha belépéskor van folyamatban lévő, indítjuk a pollingot
      if (store.languages.some(l => l.status === 'Translating')) {
        startPolling();
      }
    }
  });

  onUnmounted(() => {
    stopPolling(); // Takarítás kilépéskor
  });

  // Státusz kijelzés
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
        startPolling(); // Indítjuk a figyelést
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
  <div class="card">
    <div class="flex justify-between items-center mb-4">
      <h2 class="text-xl font-bold">Nyelvek kezelése</h2>
      <Button label="Új nyelv hozzáadása" icon="pi pi-plus" @click="isDialogVisible = true" />
    </div>

    <DataTable :value="store.languages" tableStyle="min-width: 50rem">
      <Column field="languageCode" header="Nyelv">
        <template #body="slotProps">
          <span class="font-bold text-lg uppercase">{{ slotProps.data.languageCode }}</span>
          <span v-if="slotProps.data.isDefault" class="ml-2 text-xs text-gray-500">(Alapértelmezett)</span>
        </template>
      </Column>

      <Column field="status" header="Státusz" style="min-width: 200px;">
        <template #body="slotProps">
          <div class="flex flex-col gap-2">

            <div v-if="slotProps.data.status === 'Translating'" class="w-full">
              <div class="flex justify-between mb-1">
                <span class="text-xs text-blue-600 font-bold">Fordítás...</span>
                <span class="text-xs text-gray-600">{{ slotProps.data.progress || 0 }}%</span>
              </div>
              <ProgressBar :value="slotProps.data.progress || 0" :showValue="false" style="height: 6px;"></ProgressBar>
            </div>

            <Tag v-else :value="getStatusLabel(slotProps.data.status)" :severity="getSeverity(slotProps.data.status)" />

          </div>
        </template>
      </Column>

      <Column header="Műveletek">
        <template #body="slotProps">
          <div class="flex gap-2">
            <Button v-if="slotProps.data.status === 'ReviewPending'"
                    label="Publikálás"
                    icon="pi pi-check"
                    severity="success"
                    size="small"
                    @click="onPublish(slotProps.data.languageCode)" />

            <Button v-if="!slotProps.data.isDefault"
                    icon="pi pi-trash"
                    severity="danger"
                    text
                    rounded
                    aria-label="Törlés"
                    @click="onDelete(slotProps.data.languageCode)" />
          </div>
        </template>
      </Column>
    </DataTable>

    <Dialog v-model:visible="isDialogVisible" modal header="Új nyelv hozzáadása" :style="{ width: '25rem' }">
      <div class="flex flex-col gap-4 mb-4">

        <div class="flex flex-col gap-2">
          <label for="langSelect" class="font-semibold">Válassz nyelvet</label>
          <Select id="langSelect"
                  v-model="selectedLangCode"
                  :options="availableLanguagesOptions"
                  optionLabel="label"
                  optionValue="value"
                  placeholder="Válassz a listából..."
                  class="w-full" />
        </div>

        <div class="flex items-center gap-2 mt-2">
          <Checkbox v-model="useAi" :binary="true" inputId="aiCheck" />
          <label for="aiCheck" class="cursor-pointer select-none">
            Automatikus AI fordítás kérése
          </label>
        </div>

        <small v-if="useAi" class="text-gray-500">
          A rendszer lefordítja a szolgáltatásokat és a gombokat. Ez 1-2 percet vehet igénybe.
        </small>
        <small v-else class="text-gray-500">
          Létrejön egy üres nyelvi profil, amit neked kell feltöltened tartalommal.
        </small>

      </div>
      <div class="flex justify-end gap-2">
        <Button type="button" label="Mégsem" severity="secondary" @click="isDialogVisible = false"></Button>
        <Button type="button" label="Hozzáadás" @click="onAddLanguage" :loading="loading" :disabled="!selectedLangCode"></Button>
      </div>
    </Dialog>
  </div>
</template>

<style scoped>
  .flex {
    display: flex;
  }

  .flex-col {
    flex-direction: column;
  }

  .gap-2 {
    gap: 0.5rem;
  }

  .gap-4 {
    gap: 1rem;
  }

  .justify-between {
    justify-content: space-between;
  }

  .justify-end {
    justify-content: flex-end;
  }

  .items-center {
    align-items: center;
  }

  .mb-4 {
    margin-bottom: 1rem;
  }

  .mt-2 {
    margin-top: 0.5rem;
  }

  .text-xl {
    font-size: 1.25rem;
  }

  .text-lg {
    font-size: 1.125rem;
  }

  .font-bold {
    font-weight: 700;
  }

  .font-semibold {
    font-weight: 600;
  }

  .text-xs {
    font-size: 0.75rem;
  }

  .text-sm {
    font-size: 0.875rem;
  }

  .text-gray-500 {
    color: #6b7280;
  }

  .text-blue-500 {
    color: #3b82f6;
  }

  .ml-2 {
    margin-left: 0.5rem;
  }

  .w-full {
    width: 100%;
  }

  .cursor-pointer {
    cursor: pointer;
  }

  .select-none {
    user-select: none;
  }
</style>
