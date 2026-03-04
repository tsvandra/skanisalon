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
  <div class="settings-contact">
    <div class="grid grid-cols-1 md:grid-cols-2 gap-6">

      <div class="flex flex-col gap-4">
        <div class="flex flex-col gap-2">
          <label class="font-bold text-surface-700 dark:text-surface-0/80">Cégnév (Weboldalon megjelenő)</label>
          <InputText v-model="companyData.name" class="w-full" />
        </div>

        <div class="flex flex-col gap-2">
          <label class="font-bold text-surface-700 dark:text-surface-0/80">Email</label>
          <InputText v-model="companyData.email" class="w-full" />
        </div>

        <div class="flex flex-col gap-2">
          <label class="font-bold text-surface-700 dark:text-surface-0/80">Telefonszám</label>
          <InputText v-model="companyData.phone" class="w-full" />
        </div>
      </div>

      <div class="flex flex-col gap-4">
        <h3 class="text-xl font-semibold m-0 text-surface-600 dark:text-surface-0/70 border-b border-surface-200 dark:border-surface-700 pb-2">Cím</h3>

        <div class="grid grid-cols-2 gap-4">
          <div class="flex flex-col gap-2">
            <label class="font-bold text-surface-700 dark:text-surface-0/80">Irányítószám</label>
            <InputText v-model="companyData.postalCode" class="w-full" />
          </div>
          <div class="flex flex-col gap-2">
            <label class="font-bold text-surface-700 dark:text-surface-0/80">Város</label>
            <InputText v-model="companyData.city" class="w-full" />
          </div>
        </div>

        <div class="grid grid-cols-3 gap-4">
          <div class="col-span-2 flex flex-col gap-2">
            <label class="font-bold text-surface-700 dark:text-surface-0/80">Utca</label>
            <InputText v-model="companyData.streetName" class="w-full" />
          </div>
          <div class="flex flex-col gap-2">
            <label class="font-bold text-surface-700 dark:text-surface-0/80">Házszám</label>
            <InputText v-model="companyData.houseNumber" class="w-full" />
          </div>
        </div>
      </div>

    </div>
  </div>
</template>

<style scoped>
  /* Teljesen üresre törölhető, minden stílust a Tailwind osztályok kezelnek! */
</style>
