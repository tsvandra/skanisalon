<script setup>
  import { ref, onMounted } from 'vue';
  import { useTranslationStore } from '@/stores/translationStore';
  import { useToast } from 'primevue/usetoast';
  import DataTable from 'primevue/datatable';
  import Column from 'primevue/column';
  import Button from 'primevue/button';
  import Dialog from 'primevue/dialog';
  import InputText from 'primevue/inputtext';
  import Tag from 'primevue/tag';
  import Checkbox from 'primevue/checkbox'; // <--- ÚJ IMPORT
  import { jwtDecode } from "jwt-decode";

  const store = useTranslationStore();
  const toast = useToast();

  const isDialogVisible = ref(false);
  const newLangCode = ref('');
  const useAi = ref(true); // <--- ÚJ STATE: Alapból bekapcsolva
  const loading = ref(false);

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

  onMounted(() => {
    if (companyId) {
      store.fetchLanguages(companyId);
    }
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
    if (!newLangCode.value) return;

    loading.value = true;
    try {
      // Átadjuk a useAi.value-t is
      await store.addLanguage(companyId, newLangCode.value.toLowerCase(), useAi.value);

      const detailMsg = useAi.value ? 'Fordítás elindítva a háttérben!' : 'Nyelv létrehozva kézi szerkesztéshez.';
      toast.add({ severity: 'success', summary: 'Siker', detail: detailMsg, life: 3000 });

      isDialogVisible.value = false;
      newLangCode.value = '';
      useAi.value = true; // Visszaállítás alaphelyzetbe
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

  // ÚJ: Törlés funkció
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
      <Column field="languageCode" header="Nyelvkód">
        <template #body="slotProps">
          <span class="font-bold uppercase">{{ slotProps.data.languageCode }}</span>
          <span v-if="slotProps.data.isDefault" class="ml-2 text-xs text-gray-500">(Alapértelmezett)</span>
        </template>
      </Column>

      <Column field="status" header="Státusz">
        <template #body="slotProps">
          <Tag :value="getStatusLabel(slotProps.data.status)" :severity="getSeverity(slotProps.data.status)" />
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

            <span v-else-if="slotProps.data.status === 'Translating'" class="text-sm text-gray-500 flex items-center">
              <i class="pi pi-spin pi-spinner mr-1"></i> Dolgozunk...
            </span>

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
          <label for="langCode" class="font-semibold">Nyelvkód (pl. sk, en, de)</label>
          <InputText id="langCode" v-model="newLangCode" class="flex-auto" autocomplete="off" maxlength="2" placeholder="sk" />
        </div>

        <div class="flex items-center gap-2 mt-2">
          <Checkbox v-model="useAi" :binary="true" inputId="aiCheck" />
          <label for="aiCheck" class="cursor-pointer select-none">
            Automatikus AI fordítás kérése
          </label>
        </div>

        <small v-if="useAi" class="text-gray-500">
          A rendszer lefordítja a szolgáltatásokat és a gombokat. Ez pár percet igénybe vehet.
        </small>
        <small v-else class="text-gray-500">
          Létrejön egy üres nyelvi profil, amit neked kell feltöltened tartalommal.
        </small>

      </div>
      <div class="flex justify-end gap-2">
        <Button type="button" label="Mégsem" severity="secondary" @click="isDialogVisible = false"></Button>
        <Button type="button" label="Hozzáadás" @click="onAddLanguage" :loading="loading"></Button>
      </div>
    </Dialog>
  </div>
</template>

<style scoped>
  /* Tailwind utility classes fallback, ha nincs telepítve */
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

  .ml-2 {
    margin-left: 0.5rem;
  }

  .mr-1 {
    margin-right: 0.25rem;
  }

  .cursor-pointer {
    cursor: pointer;
  }

  .select-none {
    user-select: none;
  }
</style>
