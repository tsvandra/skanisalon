<script setup>
import { computed } from 'vue';
import { useTranslationStore } from '@/stores/translationStore';
import { useI18n } from 'vue-i18n';

const store = useTranslationStore();
const { locale } = useI18n();

// Props: 'adminMode' - ha true, minden nyelvet mutat, ha false, csak a publikusat
const props = defineProps({
  adminMode: {
    type: Boolean,
    default: false
  }
});

// Mely nyelvekeleníthetőek meg?
const availableLanguages = computed(() => {
  if (props.adminMode) {
    // Admin látja az összeset (hogy tudja szerkeszteni őket)
    return store.languages;
  } else {
    // Vendég csak a publikusat
    return store.publishedLanguages;
  }
});

const currentLang = computed(() => locale.value);

const switchLang = (code) => {
  store.setLanguage(code);
};
</script>

<template>
  <div class="language-switcher">
    <button v-for="lang in availableLanguages"
            :key="lang.languageCode"
            @click="switchLang(lang.languageCode)"
            class="lang-btn"
            :class="{ active: currentLang === lang.languageCode }"
            :title="lang.status !== 'Published' ? `Státusz: ${lang.status}` : ''">
      {{ lang.languageCode.toUpperCase() }}
      <span v-if="adminMode && lang.status !== 'Published'" class="status-dot">●</span>
    </button>
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
    top: -2px;
    right: -2px;
    font-size: 8px;
    color: orange;
  }
</style>
