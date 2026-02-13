<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import { useTranslationStore } from '@/stores/translationStore';
import { useI18n } from 'vue-i18n';
import api from '@/services/api'; // Axios instance
import Dropdown from 'primevue/dropdown';
import InputText from 'primevue/inputtext';
import Button from 'primevue/button';
import Accordion from 'primevue/accordion';
import AccordionTab from 'primevue/accordiontab';
import { useToast } from 'primevue/usetoast';

const store = useTranslationStore();
const { messages } = useI18n();
const toast = useToast();

const selectedLang = ref('hu');
const overrides = ref({}); // Amit a DB-ből töltünk be
const isLoading = ref(false);

// Elérhető nyelvek (kivéve ami épp létrejön)
const languageOptions = computed(() => {
    return store.languages
        .filter(l => l.status !== 'Created')
        .map(l => ({ label: l.languageCode.toUpperCase(), value: l.languageCode }));
});

// JSON lapítása (common.save -> "Mentés")
const flattenObject = (obj, prefix = '') => {
    return Object.keys(obj).reduce((acc, k) => {
        const pre = prefix.length ? prefix + '.' : '';
        if (typeof obj[k] === 'object' && obj[k] !== null) {
            Object.assign(acc, flattenObject(obj[k], pre + k));
        } else {
            acc[pre + k] = obj[k];
        }
        return acc;
    }, {});
};

// Az alap (magyar) kulcsok alapján építjük fel a listát
const baseKeys = computed(() => {
    const huMessages = messages.value['hu']; // Mindig a magyar a bázis
    return flattenObject(huMessages);
});

// Csoportosítás (pl. "nav", "common", "home")
const groupedKeys = computed(() => {
    const groups = {};
    Object.keys(baseKeys.value).forEach(key => {
        const groupName = key.split('.')[0]; // "nav.home" -> "nav"
        if (!groups[groupName]) groups[groupName] = [];
        groups[groupName].push({
            key: key,
            original: baseKeys.value[key]
        });
    });
    return groups;
});

// Betöltjük a DB-ből a mentett felülírásokat
const loadOverrides = async () => {
    if (!store.activeCompanyId) return;
    isLoading.value = true;
    try {
        const res = await api.get(`/api/Translation/overrides/${store.activeCompanyId}/${selectedLang.value}`);
        overrides.value = res.data; // { "nav.home": "Kezdőlap", ... }
    } catch (err) {
        console.error("Hiba a felülírások betöltésekor", err);
    } finally {
        isLoading.value = false;
    }
};

// Mentés
const saveOverride = async (key, newValue) => {
    if (!store.activeCompanyId) return;

    // Ha üresre törli, akkor visszaáll az eredetire (backend logika kérdése, de most küldjük el üresen vagy töröljük)
    // Most upsert logic: ha van érték, mentjük.

    try {
        await api.post('/api/Translation/save-override', {
            companyId: store.activeCompanyId,
            languageCode: selectedLang.value,
            key: key,
            value: newValue
        });

        // Lokális update a store-ban, hogy azonnal látszódjon váltás nélkül is
        await store.loadOverrides(store.activeCompanyId, selectedLang.value);

        toast.add({ severity: 'success', summary: 'Mentve', detail: 'A szöveg frissült.', life: 2000 });
    } catch (err) {
        toast.add({ severity: 'error', summary: 'Hiba', detail: 'Nem sikerült menteni.', life: 3000 });
    }
};

// Ha váltunk nyelvet a dropdownban, töltsük újra az adatokat
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
        <span class="p-input-icon-left">
          <Dropdown v-model="selectedLang" :options="languageOptions" optionLabel="label" optionValue="value" placeholder="Nyelv választása" />
        </span>
      </div>
    </div>

    <p class="info-text">Itt írhatod át a weboldal fix szövegeit (gombok, menük). Az alapértelmezett értékhez töröld ki a mező tartalmát.</p>

    <div v-if="isLoading">Betöltés...</div>

    <Accordion v-else :multiple="true" :activeIndex="[0]">
      <AccordionTab v-for="(items, groupName) in groupedKeys" :key="groupName" :header="groupName.toUpperCase()">

        <div v-for="item in items" :key="item.key" class="translation-row">
          <div class="key-info">
            <span class="key-label">{{ item.key }}</span>
            <small class="original-text">Eredeti (HU): {{ item.original }}</small>
          </div>

          <div class="input-wrapper">
            <div class="p-inputgroup">
              <InputText v-model="overrides[item.key]"
                         :placeholder="messages[selectedLang]?.[item.key.split('.')[0]]?.[item.key.split('.')[1]] || item.original" />
              <Button icon="pi pi-check" @click="saveOverride(item.key, overrides[item.key])" />
            </div>
          </div>
        </div>

      </AccordionTab>
    </Accordion>
  </div>
</template>

<style scoped>
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
</style>
