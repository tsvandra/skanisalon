<script setup>
import { ref, onMounted, computed } from 'vue';
import { useTranslationStore } from '@/stores/translationStore';
import { useToast } from 'primevue/usetoast';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import Tag from 'primevue/tag';
import { jwtDecode } from "jwt-decode"; // Ha nincs telepítve: npm install jwt-decode

const store = useTranslationStore();
const toast = useToast();

const isDialogVisible = ref(false);
const newLangCode = ref('');
const loading = ref(false);

// CompanyId kinyerése a tokenből (egyszerűsített verzió)
const getCompanyId = () => {
  const token = localStorage.getItem('salon_token');
  if (!token) return 0;
  try {
    const decoded = jwtDecode(token);
    // A backend "CompanyId" claim-et küld (stringként lehet benne)
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

// Státusz megjelenítés helper
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

// Új nyelv hozzáadása
const onAddLanguage = async () => {
    if (!newLangCode.value) return;

    loading.value = true;
    try {
        await store.addLanguage(companyId, newLangCode.value.toLowerCase());
        toast.add({ severity: 'success', summary: 'Siker', detail: 'Fordítás elindítva a háttérben!', life: 3000 });
        isDialogVisible.value = false;
        newLangCode.value = '';
    } catch (error) {
        toast.add({ severity: 'error', summary: 'Hiba', detail: 'Nem sikerült hozzáadni a nyelvet.', life: 3000 });
    } finally {
        loading.value = false;
    }
};

// Publikálás
const onPublish = async (langCode) => {
    try {
        await store.publishLanguage(companyId, langCode);
        toast.add({ severity: 'success', summary: 'Publikálva', detail: 'A nyelv mostantól elérhető a vendégeknek.', life: 3000 });
    } catch (error) {
        toast.add({ severity: 'error', summary: 'Hiba', detail: 'Hiba a publikálás során.', life: 3000 });
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
          <Button v-if="slotProps.data.status === 'ReviewPending'"
                  label="Publikálás"
                  icon="pi pi-check"
                  severity="success"
                  size="small"
                  @click="onPublish(slotProps.data.languageCode)" />
          <span v-else-if="slotProps.data.status === 'Translating'" class="text-sm text-gray-500">
            <i class="pi pi-spin pi-spinner mr-1"></i> Dolgozunk rajta...
          </span>
        </template>
      </Column>
    </DataTable>

    <Dialog v-model:visible="isDialogVisible" modal header="Új nyelv hozzáadása" :style="{ width: '25rem' }">
      <div class="flex flex-col gap-4 mb-4">
        <label for="langCode" class="font-semibold">Nyelvkód (pl. sk, en, de)</label>
        <InputText id="langCode" v-model="newLangCode" class="flex-auto" autocomplete="off" maxlength="2" placeholder="sk" />
        <small class="text-gray-500">A hozzáadás után az AI automatikusan elkezdi lefordítani az összes tartalmat.</small>
      </div>
      <div class="flex justify-end gap-2">
        <Button type="button" label="Mégsem" severity="secondary" @click="isDialogVisible = false"></Button>
        <Button type="button" label="Hozzáadás" @click="onAddLanguage" :loading="loading"></Button>
      </div>
    </Dialog>
  </div>
</template>
