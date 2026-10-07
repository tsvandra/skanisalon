<template>
  <Dialog 
    :visible="visible" 
    @update:visible="$emit('update:visible', $event)" 
    :style="{ width: '95vw', maxWidth: '520px' }" 
    :modal="true" 
    class="p-fluid"
    :pt="{
      root: { class: 'bg-surface border border-text/10 rounded-2xl overflow-hidden shadow-2xl' },
      header: { class: 'px-5 py-4 border-b border-text/10 bg-background/50' },
      title: { class: 'text-lg md:text-xl font-bold text-text flex items-center gap-2' },
      content: { class: 'p-4 sm:p-5 bg-surface max-h-[80vh] overflow-y-auto space-y-4' },
      footer: { class: 'p-4 border-t border-text/10 bg-background/50 flex justify-end gap-2' },
      closeButton: { class: 'hover:bg-text/10 p-2 rounded-full transition-colors w-8 h-8 flex items-center justify-center text-text-muted' }
    }"
  >
    <template #header>
      <div class="flex items-center gap-2">
        <div class="w-8 h-8 rounded-lg bg-primary/10 text-primary flex items-center justify-center">
          <i class="pi pi-sparkles text-sm"></i>
        </div>
        <div>
          <h2 class="text-base sm:text-lg font-bold text-text leading-tight">
            {{ $t('inventory.aiScanner.title') }}
          </h2>
        </div>
      </div>
    </template>

    <div class="space-y-4">
      <!-- Magyarázó szöveg és tippek -->
      <p class="text-xs text-text-muted leading-relaxed">
        {{ $t('inventory.aiScanner.subtitle') }}
      </p>

      <!-- Tipp chipek a sikeres beolvasáshoz -->
      <div class="grid grid-cols-3 gap-1.5 text-center">
        <div class="p-2 bg-background/60 border border-text/10 rounded-xl text-[11px] flex flex-col items-center gap-1">
          <i class="pi pi-tag text-primary text-xs"></i>
          <span class="font-bold text-text">{{ $t('inventory.aiScanner.tipFront') }}</span>
        </div>
        <div class="p-2 bg-background/60 border border-text/10 rounded-xl text-[11px] flex flex-col items-center gap-1">
          <i class="pi pi-box text-sky-400 text-xs"></i>
          <span class="font-bold text-text">{{ $t('inventory.aiScanner.tipBack') }}</span>
        </div>
        <div class="p-2 bg-background/60 border border-text/10 rounded-xl text-[11px] flex flex-col items-center gap-1">
          <i class="pi pi-barcode text-emerald-400 text-xs"></i>
          <span class="font-bold text-text">{{ $t('inventory.aiScanner.tipShade') }}</span>
        </div>
      </div>

      <!-- Rejtett fájl bemenetek -->
      <!-- 1: Kamera natív megnyitása mobilon (capture="environment") -->
      <input 
        ref="cameraInput" 
        type="file" 
        accept="image/*" 
        capture="environment" 
        class="hidden" 
        @change="handleFilesSelected" 
      />
      <!-- 2: Galériából választás (több kép egyszerre) -->
      <input 
        ref="galleryInput" 
        type="file" 
        accept="image/*" 
        multiple 
        class="hidden" 
        @change="handleFilesSelected" 
      />

      <!-- Ha MÉG NINCS kép feltöltve -->
      <div v-if="images.length === 0" class="border-2 border-dashed border-text/20 rounded-2xl p-6 text-center space-y-4 bg-background/30">
        <div class="w-14 h-14 rounded-2xl bg-primary/10 text-primary mx-auto flex items-center justify-center text-2xl shadow-xs">
          <i class="pi pi-camera"></i>
        </div>
        
        <div class="space-y-1">
          <div class="text-sm font-bold text-text">Készíts fotókat a termékről</div>
          <div class="text-xs text-text-muted">A telefon kamerájával vagy a galériádból</div>
        </div>

        <div class="flex flex-col sm:flex-row gap-2 justify-center pt-2">
          <button 
            type="button" 
            @click="triggerCamera"
            class="h-11 px-5 rounded-xl bg-primary text-white font-bold text-sm shadow-md hover:brightness-110 active:scale-95 transition-all flex items-center justify-center gap-2"
          >
            <i class="pi pi-camera text-sm"></i>
            <span>{{ $t('inventory.aiScanner.takePhoto') }}</span>
          </button>

          <button 
            type="button" 
            @click="triggerGallery"
            class="h-11 px-4 rounded-xl bg-background border border-text/20 text-text font-bold text-sm hover:border-primary active:scale-95 transition-all flex items-center justify-center gap-2"
          >
            <i class="pi pi-images text-sm"></i>
            <span>{{ $t('inventory.aiScanner.chooseFromGallery') }}</span>
          </button>
        </div>
      </div>

      <!-- Ha MÁR VAN fotó rögzítve -->
      <div v-else class="space-y-3">
        <div class="flex items-center justify-between text-xs font-bold text-text-muted">
          <span>{{ $t('inventory.aiScanner.imagesCount', { count: images.length }) }}</span>
          <span v-if="images.length >= 5" class="text-amber-400">Elérted a maximumot (5 fotó)</span>
        </div>

        <!-- Bélyegkép sáv -->
        <div class="grid grid-cols-3 sm:grid-cols-4 gap-2">
          <div 
            v-for="(item, idx) in images" 
            :key="idx" 
            class="relative aspect-square rounded-xl overflow-hidden border border-text/20 group bg-background shadow-2xs"
          >
            <img :src="item.previewUrl" class="w-full h-full object-cover" alt="Fotó" />
            
            <button 
              type="button" 
              @click="removeImage(idx)"
              class="absolute top-1 right-1 w-6 h-6 rounded-full bg-black/75 text-white flex items-center justify-center text-xs hover:bg-red-500 transition-colors shadow-sm"
              title="Törlés"
            >
              <i class="pi pi-times text-[10px]"></i>
            </button>

            <span class="absolute bottom-1 left-1 px-1.5 py-0.2 rounded bg-black/60 text-white text-[10px] font-bold">
              #{{ idx + 1 }}
            </span>
          </div>

          <!-- Újabb fotó hozzáadása gomb (ha < 5) -->
          <button 
            v-if="images.length < 5" 
            type="button" 
            @click="triggerCamera"
            class="aspect-square rounded-xl border-2 border-dashed border-text/20 hover:border-primary text-text-muted hover:text-primary flex flex-col items-center justify-center gap-1 transition-all active:scale-95 bg-background/40"
          >
            <i class="pi pi-plus text-base"></i>
            <span class="text-[10px] font-bold">+ Fotó</span>
          </button>
        </div>

        <!-- További feltöltési opciók -->
        <div class="flex gap-2 pt-1">
          <button 
            v-if="images.length < 5" 
            type="button" 
            @click="triggerGallery" 
            class="text-xs text-primary font-bold hover:underline flex items-center gap-1"
          >
            <i class="pi pi-images text-[11px]"></i>
            <span>{{ $t('inventory.aiScanner.chooseFromGallery') }}</span>
          </button>

          <button 
            type="button" 
            @click="clearAllImages" 
            class="text-xs text-red-400 font-bold hover:underline ml-auto"
          >
            Összes törlése
          </button>
        </div>
      </div>

      <!-- Töltési animáció & állapotjelző -->
      <div v-if="scanning" class="p-4 bg-primary/10 border border-primary/30 rounded-2xl text-center space-y-2 animate-pulse">
        <i class="pi pi-spinner pi-spin text-2xl text-primary"></i>
        <div class="text-sm font-bold text-primary">{{ $t('inventory.aiScanner.analyzing') }}</div>
        <div class="text-xs text-text-muted">{{ $t('inventory.aiScanner.analyzingSub') }}</div>
      </div>

      <!-- Hibaüzenet banner -->
      <div v-if="errorMessage" class="p-3.5 bg-red-500/10 border border-red-500/30 text-red-400 rounded-xl text-xs space-y-1">
        <div class="font-bold flex items-center gap-1.5">
          <i class="pi pi-exclamation-triangle"></i>
          <span>{{ $t('inventory.aiScanner.errorTitle') }}</span>
        </div>
        <div class="leading-relaxed">{{ errorMessage }}</div>
      </div>
    </div>

    <template #footer>
      <button 
        type="button" 
        @click="hideDialog" 
        :disabled="scanning" 
        class="h-10 px-4 rounded-xl text-xs md:text-sm font-bold bg-background text-text border border-text/20 hover:bg-text/5 active:scale-95 transition-all disabled:opacity-50"
      >
        {{ $t('inventory.aiScanner.cancel') }}
      </button>

      <button 
        type="button" 
        @click="startScan" 
        :disabled="images.length === 0 || scanning" 
        class="h-10 px-5 rounded-xl text-xs md:text-sm font-bold bg-primary text-white shadow-md hover:brightness-110 active:scale-95 transition-all disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
      >
        <i v-if="scanning" class="pi pi-spinner pi-spin text-xs"></i>
        <i v-else class="pi pi-sparkles text-xs"></i>
        <span>{{ $t('inventory.aiScanner.analyzeBtn') }}</span>
      </button>
    </template>
  </Dialog>
</template>

<script setup>
import { ref } from 'vue';
import Dialog from 'primevue/dialog';
import productApi from '@/services/productApi';

const props = defineProps({
  visible: Boolean
});

const emit = defineEmits(['update:visible', 'scanned']);

const cameraInput = ref(null);
const galleryInput = ref(null);

const images = ref([]); // { file: File, previewUrl: string }
const scanning = ref(false);
const errorMessage = ref('');

const triggerCamera = () => {
  if (cameraInput.value) {
    cameraInput.value.click();
  }
};

const triggerGallery = () => {
  if (galleryInput.value) {
    galleryInput.value.click();
  }
};

const handleFilesSelected = (event) => {
  errorMessage.value = '';
  const files = Array.from(event.target.files || []);
  if (!files.length) return;

  const remainingSlots = 5 - images.value.length;
  const toAdd = files.slice(0, remainingSlots);

  toAdd.forEach(file => {
    images.value.push({
      file,
      previewUrl: URL.createObjectURL(file)
    });
  });

  // Input mező alaphelyzetbe állítása, hogy ugyanazt a fájlt is újra lehessen választani ha kell
  event.target.value = '';
};

const removeImage = (index) => {
  const removed = images.value.splice(index, 1)[0];
  if (removed && removed.previewUrl) {
    URL.revokeObjectURL(removed.previewUrl);
  }
};

const clearAllImages = () => {
  images.value.forEach(img => URL.revokeObjectURL(img.previewUrl));
  images.value = [];
  errorMessage.value = '';
};

const hideDialog = () => {
  if (!scanning.value) {
    emit('update:visible', false);
  }
};

const startScan = async () => {
  if (images.value.length === 0 || scanning.value) return;

  scanning.value = true;
  errorMessage.value = '';

  try {
    const rawFiles = images.value.map(i => i.file);
    const response = await productApi.aiScan(rawFiles);
    
    // Sikeres kiolvasás
    const productData = response.data;
    emit('scanned', productData);
    
    // Képek felszabadítása és ablak bezárása
    clearAllImages();
    emit('update:visible', false);
  } catch (error) {
    console.error('Hiba az AI termékfelismeréskor:', error);
    errorMessage.value = error.response?.data?.message || 
                         error.message || 
                         'Nem sikerült kiolvasni a termékadatokat. Kérlek ellenőrizd az internetkapcsolatot vagy az AI beállításokat.';
  } finally {
    scanning.value = false;
  }
};
</script>
