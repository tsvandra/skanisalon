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
  <div class="p-2 md:p-4 animate-fade-in">

    <div class="grid grid-cols-1 md:grid-cols-2 gap-6">

      <div>
        <label class="flex items-center gap-2 mb-2 font-bold text-text-muted text-xs uppercase tracking-wider">
          <i class="pi pi-facebook text-blue-500 text-base"></i> Facebook Link (Teljes URL)
        </label>
        <InputText v-model="companyData.facebookUrl"
                   class="w-full !bg-background !border-text/20 !text-text hover:!border-primary focus:!border-primary focus:!ring-1 focus:!ring-primary transition-colors rounded-lg p-3 shadow-sm" />
      </div>

      <div>
        <label class="flex items-center gap-2 mb-2 font-bold text-text-muted text-xs uppercase tracking-wider">
          <i class="pi pi-instagram text-pink-500 text-base"></i> Instagram Link
        </label>
        <InputText v-model="companyData.instagramUrl"
                   class="w-full !bg-background !border-text/20 !text-text hover:!border-primary focus:!border-primary focus:!ring-1 focus:!ring-primary transition-colors rounded-lg p-3 shadow-sm" />
      </div>

      <div>
        <label class="flex items-center gap-2 mb-2 font-bold text-text-muted text-xs uppercase tracking-wider">
          <i class="pi pi-tiktok text-text text-base"></i> TikTok Link
        </label>
        <InputText v-model="companyData.tikTokUrl"
                   class="w-full !bg-background !border-text/20 !text-text hover:!border-primary focus:!border-primary focus:!ring-1 focus:!ring-primary transition-colors rounded-lg p-3 shadow-sm" />
      </div>

      <div class="md:col-span-2 mt-6">
        <h3 class="text-lg font-light text-primary mb-6 uppercase tracking-widest border-b border-text/10 pb-2">Térkép Integráció</h3>

        <label class="flex items-center gap-2 mb-2 font-bold text-text-muted text-xs uppercase tracking-wider">
          <i class="pi pi-map-marker text-red-500 text-base"></i> Google Maps Embed URL (Beágyazási link)
        </label>
        <InputText v-model="companyData.mapEmbedUrl"
                   class="w-full !bg-background !border-text/20 !text-text hover:!border-primary focus:!border-primary focus:!ring-1 focus:!ring-primary transition-colors rounded-lg p-3 shadow-sm" />
        <small class="text-xs text-text/50 mt-2 block italic font-medium">Másolja be a Google Maps "Megosztás -> Beágyazás" src linkjét.</small>
      </div>

    </div>

  </div>
</template>
