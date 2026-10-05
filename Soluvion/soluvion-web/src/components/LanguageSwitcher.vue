<script setup>
  import { computed } from 'vue';
  import { useRoute, useRouter } from 'vue-router';
  import { useTranslationStore } from '@/stores/translationStore';
  import { useI18n } from 'vue-i18n';
  import { localizedLocation } from '@/router/routeSlugs';

  const store = useTranslationStore();
  const { locale } = useI18n();
  const route = useRoute();
  const router = useRouter();

  const props = defineProps({
    adminMode: {
      type: Boolean,
      default: false
    }
  });

  const availableLanguages = computed(() => {
    const list = props.adminMode ? store.languages : store.publishedLanguages;
    return list || [];
  });

  const currentLang = computed(() => locale.value);

  const switchLang = async (code) => {
    if (!code) return;
    try {
      await store.setLanguage(code);
    } catch (err) {
      console.error("[LanguageSwitcher] Hiba a váltáskor:", err);
      locale.value = code;
    }

    // Az URL követi a nyelvet: /hu/ugyfelek -> /sk/zakaznici
    if (route.name && route.params.lang) {
      router.replace(localizedLocation(route.name, code, { query: route.query, hash: route.hash }));
    }
  };
</script>

<template>
  <div class="flex items-center gap-1.5">

    <template v-if="availableLanguages.length > 0">
      <button v-for="lang in availableLanguages"
              :key="lang.languageCode"
              @click="switchLang(lang.languageCode)"
              :class="[
                'px-2 py-1 rounded-md text-xs font-bold border transition-all duration-200 relative min-w-[32px] flex items-center justify-center shadow-sm',
                currentLang === lang.languageCode
                  ? '!bg-primary !border-primary !text-black scale-105'
                  : 'bg-transparent border-text/30 text-text hover:bg-text/10 hover:!border-primary hover:text-primary'
              ]"
              :title="lang.status !== 'Published' ? $t('appShell.languageSwitcher.status', { status: lang.status }) : ''">

        {{ lang.languageCode ? lang.languageCode.toUpperCase() : '?' }}

        <span v-if="adminMode && lang.status !== 'Published'"
              :class="[
                'absolute -top-1.5 -right-1.5 text-[10px] leading-none drop-shadow-md',
                lang.status === 'Error' ? 'text-red-500' : 'text-orange-400'
              ]">
          ●
        </span>
      </button>
    </template>

    <div v-else-if="adminMode" class="text-xs text-text-muted italic">
      {{ $t('appShell.languageSwitcher.noData') }}
    </div>

  </div>
</template>
