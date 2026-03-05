<script setup>
  import LanguageManager from '@/components/admin/LanguageManager.vue';
  import UiTranslationManager from '@/components/admin/UiTranslationManager.vue';
</script>

<template>
  <div class="p-2 md:p-4">
    <LanguageManager />

    <hr class="border-0 border-t border-text/10 my-12" />

    <UiTranslationManager />
  </div>
</template>
