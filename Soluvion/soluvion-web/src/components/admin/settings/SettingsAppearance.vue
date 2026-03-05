<script setup>
  import { ref } from 'vue';
  import Button from 'primevue/button';
  import api from '@/services/api';

  const props = defineProps({
    companyData: {
      type: Object,
      required: true
    }
  });

  const isUploading = ref(false);
  const logoInputRef = ref(null);
  const heroInputRef = ref(null);
  const footerInputRef = ref(null);

  const handleUpload = async (event, type) => {
    const file = event.target.files[0];
    if (!file) return;

    isUploading.value = true;
    const formData = new FormData();
    formData.append('file', file);

    let endpoint = '';
    if (type === 'logo') endpoint = '/api/Company/upload/logo';
    else if (type === 'hero') endpoint = '/api/Company/upload/hero';
    else endpoint = '/api/Company/upload/footer';

    try {
      const res = await api.post(endpoint, formData, {
        headers: { 'Content-Type': undefined }
      });

      if (type === 'logo') props.companyData.logoUrl = res.data.url;
      else if (type === 'hero') props.companyData.heroImageUrl = res.data.url;
      else props.companyData.footerImageUrl = res.data.url;

    } catch (err) {
      console.error(err);
      alert("Hiba a feltöltés során.");
    } finally {
      isUploading.value = false;
      event.target.value = "";
    }
  };
</script>

<template>
  <div class="p-2 md:p-4 animate-fade-in text-text">

    <div class="mb-10">
      <h3 class="text-lg font-light text-primary mb-6 uppercase tracking-widest border-b border-text/10 pb-2">Logó beállítások</h3>

      <div class="flex flex-col md:flex-row gap-6 items-start">
        <div class="w-full md:w-64 h-32 border border-dashed border-text/20 bg-text/5 rounded-xl flex items-center justify-center overflow-hidden relative shadow-inner">
          <img v-if="companyData.logoUrl" :src="companyData.logoUrl" :style="{ height: companyData.logoHeight + 'px' }" class="object-contain" />
          <span v-else class="text-text-muted text-sm font-medium tracking-wide">Nincs logó feltöltve</span>
        </div>

        <div class="flex-1 flex flex-col gap-4 w-full">
          <Button label="Logó feltöltése" icon="pi pi-upload" @click="logoInputRef.click()"
                  class="!bg-transparent !text-text !border !border-text/30 hover:!border-primary hover:!text-primary !rounded-lg !px-6 !py-2.5 transition-colors w-fit" :loading="isUploading" />
          <input type="file" ref="logoInputRef" hidden @change="(e) => handleUpload(e, 'logo')" accept="image/*" />

          <div class="mt-2 w-full max-w-xs">
            <div class="flex justify-between items-center mb-2">
              <label class="font-bold text-text-muted text-xs uppercase tracking-wider">Logó mérete</label>
              <span class="text-primary font-bold text-sm">{{ companyData.logoHeight }}px</span>
            </div>
            <input type="range" v-model="companyData.logoHeight" min="30" max="150" step="2" class="w-full cursor-pointer accent-primary" />
          </div>
        </div>
      </div>
    </div>

    <hr class="border-0 border-t border-text/10 my-10" />

    <div class="mb-10">
      <h3 class="text-lg font-light text-primary mb-6 uppercase tracking-widest border-b border-text/10 pb-2">Kezdőoldal Borítókép (Hero)</h3>

      <div class="flex flex-col md:flex-row gap-6 items-start">
        <div class="w-full md:w-80 h-40 border border-text/10 bg-text/5 rounded-xl flex items-center justify-center relative overflow-hidden shadow-sm bg-cover bg-center"
             :style="{ backgroundImage: `url(${companyData.heroImageUrl || 'https://via.placeholder.com/400x200?text=Alapertelmezett'})` }">
          <div class="absolute inset-0 bg-gradient-to-t from-black/70 to-transparent"></div>
          <span v-if="!companyData.heroImageUrl" class="text-white z-10 font-bold tracking-wide drop-shadow-md">Nincs egyedi kép</span>
        </div>

        <div class="flex-1 flex flex-col gap-2 w-full">
          <Button label="Borítókép cseréje" icon="pi pi-image" @click="heroInputRef.click()"
                  class="!bg-transparent !text-text !border !border-text/30 hover:!border-primary hover:!text-primary !rounded-lg !px-6 !py-2.5 transition-colors w-fit" :loading="isUploading" />
          <input type="file" ref="heroInputRef" hidden @change="(e) => handleUpload(e, 'hero')" accept="image/*" />
          <small class="text-xs text-text-muted mt-1 italic">Ajánlott méret: 1920x400px</small>
        </div>
      </div>
    </div>

    <hr class="border-0 border-t border-text/10 my-10" />

    <div class="mb-10">
      <h3 class="text-lg font-light text-primary mb-6 uppercase tracking-widest border-b border-text/10 pb-2">Lábléc Háttérkép</h3>

      <div class="flex flex-col md:flex-row gap-6 items-start">
        <div class="w-full md:w-80 h-32 border border-text/10 bg-text/5 rounded-xl flex items-center justify-center relative overflow-hidden shadow-sm bg-repeat-x bg-bottom bg-auto"
             :style="{ backgroundImage: `url(${companyData.footerImageUrl})` }">
          <div class="absolute inset-0 bg-gradient-to-t from-black/50 to-transparent"></div>
          <span v-if="!companyData.footerImageUrl" class="text-white z-10 font-bold tracking-wide drop-shadow-md">Nincs háttérkép</span>
        </div>

        <div class="flex-1 flex flex-col gap-4 w-full">
          <Button label="Lábléc cseréje" icon="pi pi-image" @click="footerInputRef.click()"
                  class="!bg-transparent !text-text !border !border-text/30 hover:!border-primary hover:!text-primary !rounded-lg !px-6 !py-2.5 transition-colors w-fit" :loading="isUploading" />
          <input type="file" ref="footerInputRef" hidden @change="(e) => handleUpload(e, 'footer')" accept="image/*" />

          <div class="mt-2 w-full max-w-xs">
            <div class="flex justify-between items-center mb-2">
              <label class="font-bold text-text-muted text-xs uppercase tracking-wider">Lábléc magassága</label>
              <span class="text-primary font-bold text-sm">{{ companyData.footerHeight }}px</span>
            </div>
            <input type="range" v-model="companyData.footerHeight" min="50" max="600" step="10" class="w-full cursor-pointer accent-primary" />
          </div>
        </div>
      </div>
    </div>

    <hr class="border-0 border-t border-text/10 my-10" />

    <div>
      <h3 class="text-lg font-light text-primary mb-6 uppercase tracking-widest border-b border-text/10 pb-2">Színek (SaaS Téma)</h3>

      <div class="grid grid-cols-1 sm:grid-cols-2 gap-8">
        <div>
          <label class="block mb-3 font-bold text-text-muted text-xs uppercase tracking-wider">Elsődleges Szín (Primary)</label>
          <div class="flex items-center gap-3 border border-text/20 bg-background rounded-lg p-2 w-fit shadow-sm hover:border-primary transition-colors">
            <input type="color" v-model="companyData.primaryColor" class="w-10 h-10 p-0 border-none rounded cursor-pointer bg-transparent" />
            <span class="font-mono text-text pr-3 tracking-widest">{{ companyData.primaryColor }}</span>
          </div>
        </div>

        <div>
          <label class="block mb-3 font-bold text-text-muted text-xs uppercase tracking-wider">Másodlagos Szín / Háttér (Secondary)</label>
          <div class="flex items-center gap-3 border border-text/20 bg-background rounded-lg p-2 w-fit shadow-sm hover:border-primary transition-colors">
            <input type="color" v-model="companyData.secondaryColor" class="w-10 h-10 p-0 border-none rounded cursor-pointer bg-transparent" />
            <span class="font-mono text-text pr-3 tracking-widest">{{ companyData.secondaryColor }}</span>
          </div>
        </div>
      </div>
    </div>

  </div>
</template>
