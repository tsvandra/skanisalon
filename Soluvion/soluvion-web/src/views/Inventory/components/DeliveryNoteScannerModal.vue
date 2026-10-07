<template>
  <Dialog 
    :visible="visible" 
    @update:visible="hideDialog" 
    :style="{ width: '95vw', maxWidth: step === 2 ? '850px' : '540px' }" 
    :modal="true" 
    class="p-fluid"
    :pt="{
      root: { class: 'bg-surface border border-text/10 rounded-2xl overflow-hidden shadow-2xl transition-all' },
      header: { class: 'px-5 py-4 border-b border-text/10 bg-background/50' },
      title: { class: 'text-lg md:text-xl font-bold text-text flex items-center gap-2' },
      content: { class: 'p-4 sm:p-5 bg-surface max-h-[82vh] overflow-y-auto space-y-4' },
      footer: { class: 'p-4 border-t border-text/10 bg-background/50 flex justify-end gap-2' },
      closeButton: { class: 'hover:bg-text/10 p-2 rounded-full transition-colors w-8 h-8 flex items-center justify-center text-text-muted' }
    }"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div class="w-9 h-9 rounded-xl bg-primary/10 text-primary flex items-center justify-center shadow-xs">
          <i class="pi pi-file-arrow-up text-base"></i>
        </div>
        <div>
          <h2 class="text-base sm:text-lg font-bold text-text leading-tight">
            {{ $t('inventory.deliveryNote.title') }}
          </h2>
          <p class="text-xs text-text-muted">
            {{ step === 1 ? $t('inventory.deliveryNote.subtitleUpload') : $t('inventory.deliveryNote.subtitleReview') }}
          </p>
        </div>
      </div>
    </template>

    <!-- 1. LÉPÉS: KÉPEK FELTÖLTÉSE / FOTÓZÁSA -->
    <div v-if="step === 1" class="space-y-4">
      <input 
        ref="cameraInput" 
        type="file" 
        accept="image/*" 
        capture="environment" 
        class="hidden" 
        @change="handleFilesSelected" 
      />
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
          <i class="pi pi-file-edit"></i>
        </div>
        
        <div class="space-y-1">
          <div class="text-sm font-bold text-text">{{ $t('inventory.deliveryNote.uploadTitle') }}</div>
          <div class="text-xs text-text-muted leading-relaxed">
            {{ $t('inventory.deliveryNote.uploadHint') }}
          </div>
        </div>

        <div class="flex flex-col sm:flex-row gap-2 justify-center pt-2">
          <button 
            type="button" 
            @click="triggerCamera"
            class="h-11 px-5 rounded-xl bg-primary text-white font-bold text-sm shadow-md hover:brightness-110 active:scale-95 transition-all flex items-center justify-center gap-2"
          >
            <i class="pi pi-camera text-sm"></i>
            <span>{{ $t('inventory.deliveryNote.takePhoto') }}</span>
          </button>

          <button 
            type="button" 
            @click="triggerGallery"
            class="h-11 px-4 rounded-xl bg-background border border-text/20 text-text font-bold text-sm hover:border-primary active:scale-95 transition-all flex items-center justify-center gap-2"
          >
            <i class="pi pi-images text-sm"></i>
            <span>{{ $t('inventory.deliveryNote.chooseGallery') }}</span>
          </button>
        </div>
      </div>

      <!-- Ha MÁR VAN feltöltött oldal -->
      <div v-else class="space-y-3">
        <div class="flex items-center justify-between text-xs font-bold text-text-muted">
          <span>{{ images.length }} oldal rögzítve (maximum 4 oldal)</span>
          <button type="button" @click="clearAllImages" class="text-red-400 hover:underline">
            Összes törlése
          </button>
        </div>

        <div class="grid grid-cols-2 sm:grid-cols-3 gap-2.5">
          <div 
            v-for="(item, idx) in images" 
            :key="idx" 
            class="relative aspect-[3/4] rounded-xl overflow-hidden border border-text/20 bg-background shadow-xs group"
          >
            <img :src="item.previewUrl" class="w-full h-full object-cover" alt="Szállítólevél oldal" />
            <button 
              type="button" 
              @click="removeImage(idx)"
              class="absolute top-1.5 right-1.5 w-6 h-6 rounded-full bg-black/75 text-white flex items-center justify-center text-xs hover:bg-red-500 transition-colors shadow-sm"
            >
              <i class="pi pi-times text-[10px]"></i>
            </button>
            <span class="absolute bottom-1.5 left-1.5 px-2 py-0.5 rounded bg-black/70 text-white text-[11px] font-bold">
              {{ idx + 1 }}. oldal
            </span>
          </div>

          <button 
            v-if="images.length < 4" 
            type="button" 
            @click="triggerCamera"
            class="aspect-[3/4] rounded-xl border-2 border-dashed border-text/20 hover:border-primary text-text-muted hover:text-primary flex flex-col items-center justify-center gap-1.5 transition-all active:scale-95 bg-background/40"
          >
            <i class="pi pi-plus text-lg"></i>
            <span class="text-xs font-bold">+ Új oldal</span>
          </button>
        </div>
      </div>

      <!-- Töltési állapot az elemzéskor -->
      <div v-if="scanning" class="p-5 bg-primary/10 border border-primary/30 rounded-2xl text-center space-y-2 animate-pulse">
        <i class="pi pi-spinner pi-spin text-2xl text-primary"></i>
        <div class="text-sm font-bold text-primary">{{ $t('inventory.deliveryNote.analyzing') }}</div>
        <div class="text-xs text-text-muted">{{ $t('inventory.deliveryNote.analyzingSub') }}</div>
      </div>

      <!-- Hibaüzenet -->
      <div v-if="errorMessage" class="p-3.5 bg-red-500/10 border border-red-500/30 text-red-400 rounded-xl text-xs space-y-1">
        <div class="font-bold flex items-center gap-1.5">
          <i class="pi pi-exclamation-triangle"></i>
          <span>{{ $t('inventory.deliveryNote.errorTitle') }}</span>
        </div>
        <div>{{ errorMessage }}</div>
      </div>
    </div>

    <!-- 2. LÉPÉS: TÉTELEK ÁTTEKINTÉSE, PÁROSÍTÁSA ÉS BEVÉTELEZÉSE -->
    <div v-else-if="step === 2" class="space-y-4">
      <!-- Fejléc adatok: Sorszám & Beszállító -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 p-3 bg-background/50 border border-text/10 rounded-xl text-xs">
        <div>
          <label class="block font-bold text-text-muted mb-1">{{ $t('inventory.deliveryNote.docNumber') }}</label>
          <input 
            type="text" 
            v-model="deliveryData.documentNumber" 
            placeholder="pl. DL2026/0451" 
            class="w-full bg-background border border-text/20 rounded-lg p-2 font-bold text-text focus:outline-none focus:border-primary text-xs"
          />
        </div>
        <div>
          <label class="block font-bold text-text-muted mb-1">{{ $t('inventory.deliveryNote.supplier') }}</label>
          <input 
            type="text" 
            v-model="deliveryData.supplier" 
            placeholder="pl. L'Oréal Slovensko" 
            class="w-full bg-background border border-text/20 rounded-lg p-2 font-bold text-text focus:outline-none focus:border-primary text-xs"
          />
        </div>
      </div>

      <!-- Figyelmeztetés és magyarázat -->
      <div class="p-2.5 bg-sky-500/10 border border-sky-500/20 text-sky-400 rounded-xl text-xs flex items-start gap-2">
        <i class="pi pi-info-circle text-sm shrink-0 mt-0.5"></i>
        <span>{{ $t('inventory.deliveryNote.matchingHint') }}</span>
      </div>

      <!-- Tételek listája -->
      <div class="space-y-2.5">
        <div 
          v-for="(row, idx) in processedItems" 
          :key="idx" 
          class="p-3 rounded-xl border transition-all"
          :class="[
            row.action === 'skip' ? 'bg-background/30 border-text/10 opacity-50' :
            row.action === 'create' ? 'bg-emerald-500/5 border-emerald-500/30' :
            'bg-background border-text/15'
          ]"
        >
          <!-- Felső sor: Kiolvasott név, Kód, és Kihagyás kapcsoló -->
          <div class="flex items-start justify-between gap-2 mb-2">
            <div class="min-w-0 flex-1">
              <div class="flex items-center gap-1.5 flex-wrap">
                <span class="text-xs font-bold text-text">{{ row.name }}</span>
                <span v-if="row.shade" class="text-[11px] font-mono px-1.5 py-0.5 rounded bg-primary/10 text-primary border border-primary/20 font-bold">
                  [{{ row.shade }}]
                </span>
                <span v-if="row.packageSize && row.packageSize > 1" class="text-[10px] text-text-muted">
                  ({{ row.packageSize }} {{ getUnitShort(row.unit) }})
                </span>
              </div>
              <div v-if="row.rawName && row.rawName !== row.name" class="text-[10px] text-text-muted truncate">
                {{ row.rawName }}
              </div>
            </div>

            <!-- Kihagyás / bevételezés gomb -->
            <button 
              type="button" 
              @click="toggleRowSkip(row)" 
              class="text-xs px-2 py-1 rounded-lg border transition-all shrink-0 font-medium"
              :class="row.action === 'skip' ? 'bg-background text-text-muted border-text/20 hover:text-text' : 'bg-red-500/10 text-red-400 border-red-500/20 hover:bg-red-500/20'"
            >
              {{ row.action === 'skip' ? 'Visszaállítás' : 'Kihagyás' }}
            </button>
          </div>

          <div v-if="row.action !== 'skip'" class="space-y-2 pt-1 border-t border-text/10">
            <!-- Párosítás Dropdown (Meglévő termék keresés hasonlóság szerint vagy Új) -->
            <div>
              <label class="block text-[11px] font-bold text-text-muted mb-1">
                Párosítás raktári termékkel:
              </label>
              <select 
                v-model="row.selectedTarget" 
                @change="onTargetChanged(row)"
                class="w-full bg-surface border border-text/20 rounded-lg p-2 text-xs text-text focus:outline-none focus:border-primary font-medium"
              >
                <option value="__CREATE_NEW__">
                  ✨ [+ Új termékként létrehozás a raktárban]
                </option>
                <optgroup label="Meglévő termékek (egyezés szerint rendezve)">
                  <option 
                    v-for="cand in row.candidates" 
                    :key="cand.product.id" 
                    :value="cand.product.id"
                  >
                    {{ cand.score >= 50 ? '⭐ ' : '' }}{{ cand.product.name }} {{ cand.product.shade ? `[${cand.product.shade}]` : '' }} (Egyezés: {{ cand.score }}% - Készlet: {{ cand.product.currentStock }})
                  </option>
                </optgroup>
              </select>
            </div>

            <!-- Mennyiség és Ár mezők -->
            <div class="grid grid-cols-2 gap-2 text-xs">
              <div>
                <label class="block text-[10px] font-bold text-text-muted mb-0.5">Bevételezendő db:</label>
                <div class="flex items-center gap-1">
                  <input 
                    type="number" 
                    v-model.number="row.quantity" 
                    step="1" 
                    min="1" 
                    class="w-full bg-surface border border-text/20 rounded-lg p-1.5 text-xs text-center font-bold text-text focus:outline-none focus:border-primary"
                  />
                  <span class="text-[11px] text-text-muted">db</span>
                </div>
              </div>

              <div>
                <label class="block text-[10px] font-bold text-text-muted mb-0.5">Beszerzési egységár:</label>
                <div class="flex items-center gap-1">
                  <input 
                    type="number" 
                    v-model.number="row.costPrice" 
                    step="0.01" 
                    min="0" 
                    class="w-full bg-surface border border-text/20 rounded-lg p-1.5 text-xs text-right font-bold text-text focus:outline-none focus:border-primary"
                  />
                  <span class="text-[11px] text-text-muted">€</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Összesítés & Mentési hiba banner -->
      <div v-if="importError" class="p-3 bg-red-500/10 border border-red-500/30 text-red-400 rounded-xl text-xs">
        {{ importError }}
      </div>
    </div>

    <!-- LÁBLÉC GOMBOK -->
    <template #footer>
      <button 
        type="button" 
        @click="hideDialog" 
        :disabled="scanning || importing" 
        class="h-10 px-4 rounded-xl text-xs md:text-sm font-bold bg-background text-text border border-text/20 hover:bg-text/5 active:scale-95 transition-all disabled:opacity-50"
      >
        {{ $t('inventory.deliveryNote.cancel') }}
      </button>

      <!-- 1. Lépésben: Elemzés gomb -->
      <button 
        v-if="step === 1" 
        type="button" 
        @click="startScan" 
        :disabled="images.length === 0 || scanning" 
        class="h-10 px-5 rounded-xl text-xs md:text-sm font-bold bg-primary text-white shadow-md hover:brightness-110 active:scale-95 transition-all disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
      >
        <i v-if="scanning" class="pi pi-spinner pi-spin text-sm"></i>
        <i v-else class="pi pi-sparkles text-sm"></i>
        <span>{{ scanning ? $t('inventory.deliveryNote.analyzingBtn') : $t('inventory.deliveryNote.startScanBtn') }}</span>
      </button>

      <!-- 2. Lépésben: Vissza gomb & Végleges Bevételezés gomb -->
      <div v-else-if="step === 2" class="flex gap-2">
        <button 
          type="button" 
          @click="step = 1" 
          :disabled="importing" 
          class="h-10 px-3 rounded-xl text-xs md:text-sm font-bold bg-background text-text border border-text/20 hover:bg-text/5 active:scale-95 transition-all"
        >
          Fotók újra
        </button>

        <button 
          type="button" 
          @click="confirmImport" 
          :disabled="importing || activeItemsCount === 0" 
          class="h-10 px-5 rounded-xl text-xs md:text-sm font-bold bg-primary text-white shadow-md hover:brightness-110 active:scale-95 transition-all disabled:opacity-50 flex items-center gap-2"
        >
          <i v-if="importing" class="pi pi-spinner pi-spin text-sm"></i>
          <i v-else class="pi pi-check text-sm"></i>
          <span>{{ importing ? 'Mentés...' : `Jóváhagyás (${activeItemsCount} tétel)` }}</span>
        </button>
      </div>
    </template>
  </Dialog>
</template>

<script setup>
import { ref, computed } from 'vue';
import Dialog from 'primevue/dialog';
import { useI18n } from 'vue-i18n';
import inventoryApi from '@/services/inventoryApi';

const { t } = useI18n();

const props = defineProps({
  visible: Boolean,
  products: {
    type: Array,
    default: () => []
  }
});

const emit = defineEmits(['update:visible', 'saved']);

const step = ref(1); // 1 = Fotózás/feltöltés, 2 = Ellenőrzés & párosítás
const cameraInput = ref(null);
const galleryInput = ref(null);

const images = ref([]); // { file: File, previewUrl: string }
const scanning = ref(false);
const importing = ref(false);
const errorMessage = ref('');
const importError = ref('');

const deliveryData = ref({
  documentNumber: '',
  supplier: '',
  issueDate: '',
  items: []
});

const processedItems = ref([]);

const triggerCamera = () => {
  if (cameraInput.value) cameraInput.value.click();
};

const triggerGallery = () => {
  if (galleryInput.value) galleryInput.value.click();
};

const handleFilesSelected = (event) => {
  errorMessage.value = '';
  const files = Array.from(event.target.files || []);
  if (!files.length) return;

  const remaining = 4 - images.value.length;
  const toAdd = files.slice(0, remaining);

  toAdd.forEach(file => {
    images.value.push({
      file,
      previewUrl: URL.createObjectURL(file)
    });
  });

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
  if (!scanning.value && !importing.value) {
    emit('update:visible', false);
  }
};

const getUnitShort = (unitEnum) => {
  const map = { 0: 'ml', 1: 'g', 2: 'db', 3: 'm', 4: 'cm' };
  return map[unitEnum] || 'db';
};

// Intelligens hasonlóság számítás egy kiolvasott tétel és egy létező raktári termék között
const calculateSimilarity = (item, prod) => {
  let score = 0;

  // 1. EAN egyezés
  if (item.ean && prod.ean && item.ean.trim() === prod.ean.trim()) {
    return 100;
  }

  // 2. Kód / Árnyalat egyezés
  const iShade = (item.shade || item.code || '').trim().toLowerCase();
  const pShade = (prod.shade || '').trim().toLowerCase();
  if (iShade && pShade) {
    if (iShade === pShade) {
      score += 45;
    } else if (iShade.includes(pShade) || pShade.includes(iShade)) {
      score += 30;
    }
  }

  // 3. Név egyezés (szó alapú átfedés)
  const cleanTokens = (str) => (str || '')
    .toLowerCase()
    .replace(/[^a-z0-9áéíóöőúüű]/gi, ' ')
    .split(/\s+/)
    .filter(w => w.length > 1);

  const itemTokens = cleanTokens(`${item.name} ${item.rawName || ''}`);
  const prodTokens = cleanTokens(prod.name);

  if (itemTokens.length > 0 && prodTokens.length > 0) {
    let matchCount = 0;
    prodTokens.forEach(pt => {
      if (itemTokens.some(it => it.includes(pt) || pt.includes(it))) {
        matchCount++;
      }
    });
    const tokenScore = (matchCount / Math.max(prodTokens.length, 1)) * 50;
    score += tokenScore;
  }

  return Math.min(Math.round(score), 99);
};

const startScan = async () => {
  if (images.value.length === 0 || scanning.value) return;

  scanning.value = true;
  errorMessage.value = '';

  try {
    const rawFiles = images.value.map(i => i.file);
    const response = await inventoryApi.scanDeliveryNote(rawFiles);
    const data = response.data;

    deliveryData.value = {
      documentNumber: data.documentNumber || '',
      supplier: data.supplier || '',
      issueDate: data.issueDate || '',
      items: data.items || []
    };

    // Tételek feldolgozása és hasonlósági rendezése
    const allProds = props.products || [];
    
    processedItems.value = (data.items || []).map(item => {
      // Minden meglévő termékhez kiszámoljuk a pontszámot
      const candidates = allProds.map(p => ({
        product: p,
        score: calculateSimilarity(item, p)
      })).sort((a, b) => b.score - a.score);

      const topCandidate = candidates[0];
      const hasStrongMatch = topCandidate && topCandidate.score >= 50;

      return {
        ...item,
        costPrice: item.unitPrice || 0,
        quantity: item.quantity || 1,
        candidates,
        action: hasStrongMatch ? 'match' : 'create',
        selectedTarget: hasStrongMatch ? topCandidate.product.id : '__CREATE_NEW__'
      };
    });

    step.value = 2;
  } catch (error) {
    console.error('Hiba a szállítólevél beolvasásakor:', error);
    errorMessage.value = error.response?.data?.message || 
                         error.message || 
                         'Nem sikerült kiolvasni a szállítólevelet. Kérlek ellenőrizd a képek minőségét vagy az AI beállításokat.';
  } finally {
    scanning.value = false;
  }
};

const onTargetChanged = (row) => {
  if (row.selectedTarget === '__CREATE_NEW__') {
    row.action = 'create';
  } else {
    row.action = 'match';
  }
};

const toggleRowSkip = (row) => {
  if (row.action === 'skip') {
    row.action = row.selectedTarget === '__CREATE_NEW__' ? 'create' : 'match';
  } else {
    row.action = 'skip';
  }
};

const activeItemsCount = computed(() => {
  return processedItems.value.filter(i => i.action !== 'skip').length;
});

const confirmImport = async () => {
  importing.value = true;
  importError.value = '';

  try {
    const payloadItems = processedItems.value
      .filter(item => item.action !== 'skip')
      .map(item => {
        if (item.action === 'match') {
          return {
            action: 'match',
            matchedProductId: Number(item.selectedTarget),
            quantity: Number(item.quantity) || 1,
            costPrice: Number(item.costPrice) || 0
          };
        } else {
          return {
            action: 'create',
            newProduct: {
              name: item.name || item.rawName || 'Új termék',
              shade: item.shade || item.code || null,
              ean: item.ean || null,
              unit: item.unit ?? 2,
              packageSize: Number(item.packageSize) || 1,
              costPrice: Number(item.costPrice) || 0,
              retailPrice: Number(item.costPrice) > 0 ? Math.round(Number(item.costPrice) * 1.5 * 100) / 100 : 0,
              lowStockThreshold: 2,
              isProfessional: true,
              isRetail: false
            },
            quantity: Number(item.quantity) || 1,
            costPrice: Number(item.costPrice) || 0
          };
        }
      });

    const payload = {
      documentNumber: deliveryData.value.documentNumber,
      supplier: deliveryData.value.supplier,
      note: 'AI szállítólevél import',
      items: payloadItems
    };

    await inventoryApi.importDeliveryNote(payload);

    emit('saved');
    clearAllImages();
    step.value = 1;
    emit('update:visible', false);
  } catch (error) {
    console.error('Hiba a bevételezés során:', error);
    importError.value = error.response?.data?.message || error.message || 'Hiba történt a tételek bevételezése során.';
  } finally {
    importing.value = false;
  }
};
</script>
