<script setup>
  import LanguageManager from '@/components/admin/LanguageManager.vue';
  import UiTranslationManager from '@/components/admin/UiTranslationManager.vue';
</script>

<template>
  <div class="settings-translations p-3">
    <LanguageManager />
    <hr class="separator" />
    <UiTranslationManager />
  </div>
</template>

<style scoped>
  .p-3 {
    padding: 1rem;
  }

  .separator {
    border: 0;
    border-top: 1px solid #eee;
    margin: 3rem 0;
  }
</style>
