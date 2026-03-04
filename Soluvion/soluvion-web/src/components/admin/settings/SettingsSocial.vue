<script setup>
  import InputText from 'primevue/inputtext';

  defineProps({
    companyData: {
      type: Object,
      required: true
    }
  });
</script>

<template>
  <div class="settings-social">
    <div class="grid grid-cols-1 md:grid-cols-2 gap-6">

      <div class="flex flex-col gap-4">
        <div class="flex flex-col gap-2">
          <label class="font-bold text-surface-700 dark:text-surface-0/80">Facebook Link (Teljes URL)</label>
          <InputText v-model="companyData.facebookUrl" class="w-full" placeholder="https://facebook.com/yourpage" />
        </div>

        <div class="flex flex-col gap-2">
          <label class="font-bold text-surface-700 dark:text-surface-0/80">Instagram Link</label>
          <InputText v-model="companyData.instagramUrl" class="w-full" placeholder="https://instagram.com/yourprofile" />
        </div>

        <div class="flex flex-col gap-2">
          <label class="font-bold text-surface-700 dark:text-surface-0/80">TikTok Link</label>
          <InputText v-model="companyData.tikTokUrl" class="w-full" placeholder="https://tiktok.com/@youraccount" />
        </div>
      </div>

      <div class="flex flex-col gap-4">
        <div class="flex flex-col gap-2">
          <label class="font-bold text-surface-700 dark:text-surface-0/80">Google Maps Embed URL</label>
          <InputText v-model="companyData.mapEmbedUrl" class="w-full" placeholder="https://www.google.com/maps/embed?..." />
          <small class="text-surface-500 dark:text-surface-400 italic">
            Tipp: Másolja be a Google Maps "Megosztás -> Térkép beágyazása" ablakból csak az <strong>src</strong> attribútum értékét.
          </small>
        </div>

        <div v-if="!companyData.mapEmbedUrl" class="p-4 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg">
          <p class="text-sm text-blue-700 dark:text-blue-300 m-0">
            <i class="pi pi-info-circle mr-2"></i>
            A beágyazott térkép segít a vendégeknek könnyebben megtalálni a szalont.
          </p>
        </div>
      </div>

    </div>
  </div>
</template>

<style scoped>
  /* Teljesen üres, Tailwind utility osztályokat használunk helyette */
</style>
