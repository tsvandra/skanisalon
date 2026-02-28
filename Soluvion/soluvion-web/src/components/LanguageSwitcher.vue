<script setup>
  import { computed } from 'vue';
  import { useTranslationStore } from '@/stores/translationStore';
  import { useI18n } from 'vue-i18n';

  const store = useTranslationStore();
  const { locale } = useI18n();

  const changeLanguage = (newLang) => {
    locale.value = newLang; // Vue-i18n nyelv beállítása
    localStorage.setItem('user-locale', newLang); // Mentés a böngészőbe!
  };

  // Props
  const props = defineProps({
    adminMode: {
      type: Boolean,
      default: false
    }
  });

  // Mely nyelvek jeleníthetőek meg?
  const availableLanguages = computed(() => {
    // Biztonsági ellenőrzés: ha a store még üres, ne omoljon össze
    const list = props.adminMode ? store.languages : store.publishedLanguages;
    return list || [];
  });

  const currentLang = computed(() => locale.value);

  const switchLang = async (code) => {
    console.log(`[LanguageSwitcher] Gomb megnyomva: ${code}`);

    if (!code) {
      console.error("Hiba: Érvénytelen nyelvkód!");
      return;
    }

    try {
      await store.setLanguage(code);
      console.log(`[LanguageSwitcher] Sikeres váltás: ${code}`);
    } catch (err) {
      console.error("[LanguageSwitcher] Hiba a váltáskor:", err);
      // Ha hiba van (pl. nem töltődnek be az override-ok),
      // akkor is kényszerítsük át a nyelvet a UI-on, hogy ne tűnjön halottnak a gomb
      locale.value = code;
    }
  };
</script>

<template>
  <div class="language-switcher">
    <template v-if="availableLanguages.length > 0">
      <button v-for="lang in availableLanguages"
              :key="lang.languageCode"
              @click="switchLang(lang.languageCode)"
              class="lang-btn"
              :class="{ active: currentLang === lang.languageCode }"
              :title="lang.status !== 'Published' ? `Státusz: ${lang.status}` : ''">

        {{ lang.languageCode ? lang.languageCode.toUpperCase() : '?' }}

        <span v-if="adminMode && lang.status !== 'Published'"
              class="status-dot"
              :class="lang.status === 'Error' ? 'error' : 'pending'">
          ●
        </span>
      </button>
    </template>

    <div v-else-if="adminMode" style="font-size: 0.7rem; color: #888;">
      (Nincs adat)
    </div>
  </div>
</template>

<style scoped>
  .language-switcher {
    display: flex;
    gap: 5px;
    align-items: center;
  }

  .lang-btn {
    background: transparent;
    border: 1px solid rgba(255,255,255,0.3);
    color: #fff;
    padding: 4px 8px;
    border-radius: 4px;
    cursor: pointer;
    font-size: 0.8rem;
    font-weight: bold;
    transition: all 0.2s;
    position: relative;
    min-width: 32px; /* Hogy biztosan kattintható legyen */
  }

    .lang-btn:hover {
      background: rgba(255,255,255,0.1);
      border-color: var(--primary-color);
      color: var(--primary-color);
    }

    .lang-btn.active {
      background: var(--primary-color);
      color: var(--secondary-color);
      border-color: var(--primary-color);
    }

  .status-dot {
    position: absolute;
    top: -3px;
    right: -3px;
    font-size: 10px;
    line-height: 1;
  }

    .status-dot.pending {
      color: orange;
    }

    .status-dot.error {
      color: red;
    }
</style>
