<template>
  <Dialog 
    :visible="visible" 
    @update:visible="hideDialog" 
    :style="{ width: '95vw', maxWidth: step === 2 ? '900px' : '560px' }" 
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

    <!-- 1. LÉPÉS: FOTÓK ÉS DOKUMENTUMOK (PDF, EXCEL, WORD) FELTÖLTÉSE -->
    <div v-if="step === 1" class="space-y-4">
      <!-- Rejtett fájl beviteli mezők -->
      <input 
        ref="cameraInput" 
        type="file" 
        accept="image/*" 
        capture="environment" 
        class="hidden" 
        @change="handleFilesSelected" 
      />
      <input 
        ref="fileInput" 
        type="file" 
        accept="image/*,.pdf,.xlsx,.xls,.docx,.doc,application/pdf,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,application/vnd.ms-excel,application/vnd.openxmlformats-officedocument.wordprocessingml.document,application/msword" 
        multiple 
        class="hidden" 
        @change="handleFilesSelected" 
      />

      <!-- Ha MÉG NINCS fájl feltöltve -->
      <div v-if="files.length === 0" class="border-2 border-dashed border-text/20 rounded-2xl p-6 text-center space-y-4 bg-background/30">
        <div class="w-14 h-14 rounded-2xl bg-primary/10 text-primary mx-auto flex items-center justify-center text-2xl shadow-xs">
          <i class="pi pi-file-edit"></i>
        </div>
        
        <div class="space-y-1">
          <div class="text-sm font-bold text-text">{{ $t('inventory.deliveryNote.uploadTitle') }}</div>
          <div class="text-xs text-text-muted leading-relaxed max-w-md mx-auto">
            {{ $t('inventory.deliveryNote.uploadHint') }}
          </div>
        </div>

        <div class="flex flex-col sm:flex-row gap-2.5 justify-center pt-2">
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
            @click="triggerFileBrowser"
            class="h-11 px-4 rounded-xl bg-background border border-text/20 text-text font-bold text-sm hover:border-primary active:scale-95 transition-all flex items-center justify-center gap-2"
          >
            <i class="pi pi-folder-open text-sm text-primary"></i>
            <span>{{ $t('inventory.deliveryNote.chooseFile') }}</span>
          </button>
        </div>
      </div>

      <!-- Ha MÁR VAN feltöltött fájl vagy oldal -->
      <div v-else class="space-y-3">
        <div class="flex items-center justify-between text-xs font-bold text-text-muted">
          <span>{{ $t('inventory.deliveryNote.filesRecorded', { count: files.length }) }}</span>
          <button type="button" @click="clearAllFiles" class="text-red-400 hover:underline">
            {{ $t('inventory.deliveryNote.clearAll') }}
          </button>
        </div>

        <div class="grid grid-cols-2 sm:grid-cols-3 gap-2.5">
          <div 
            v-for="(item, idx) in files" 
            :key="idx" 
            class="relative aspect-[3/4] rounded-xl overflow-hidden border border-text/20 bg-background shadow-xs group flex flex-col items-center justify-center p-2"
          >
            <!-- Kép esetén: előnézeti kép -->
            <img 
              v-if="item.isImage && item.previewUrl" 
              :src="item.previewUrl" 
              class="w-full h-full object-cover" 
              :alt="item.name" 
            />

            <!-- PDF esetén: ikon és név -->
            <div v-else-if="item.typeCategory === 'pdf'" class="w-full h-full flex flex-col items-center justify-center p-3 text-center bg-red-500/5">
              <i class="pi pi-file-pdf text-4xl mb-2 text-red-500"></i>
              <span class="text-xs font-bold text-text truncate max-w-full px-1">{{ item.name }}</span>
              <span class="text-[10px] text-text-muted mt-1">{{ item.sizeText }}</span>
            </div>

            <!-- Excel esetén: ikon és név -->
            <div v-else-if="item.typeCategory === 'excel'" class="w-full h-full flex flex-col items-center justify-center p-3 text-center bg-emerald-500/5">
              <i class="pi pi-file-excel text-4xl mb-2 text-emerald-500"></i>
              <span class="text-xs font-bold text-text truncate max-w-full px-1">{{ item.name }}</span>
              <span class="text-[10px] text-text-muted mt-1">{{ item.sizeText }}</span>
            </div>

            <!-- Word esetén: ikon és név -->
            <div v-else-if="item.typeCategory === 'word'" class="w-full h-full flex flex-col items-center justify-center p-3 text-center bg-blue-500/5">
              <i class="pi pi-file-word text-4xl mb-2 text-blue-500"></i>
              <span class="text-xs font-bold text-text truncate max-w-full px-1">{{ item.name }}</span>
              <span class="text-[10px] text-text-muted mt-1">{{ item.sizeText }}</span>
            </div>

            <!-- Egyéb fájl -->
            <div v-else class="w-full h-full flex flex-col items-center justify-center p-3 text-center bg-background/50">
              <i class="pi pi-file text-4xl mb-2 text-text-muted"></i>
              <span class="text-xs font-bold text-text truncate max-w-full px-1">{{ item.name }}</span>
              <span class="text-[10px] text-text-muted mt-1">{{ item.sizeText }}</span>
            </div>

            <!-- Törlés gomb -->
            <button 
              type="button" 
              @click="removeFile(idx)"
              class="absolute top-1.5 right-1.5 w-6 h-6 rounded-full bg-black/75 text-white flex items-center justify-center text-xs hover:bg-red-500 transition-colors shadow-sm z-10"
            >
              <i class="pi pi-times text-[10px]"></i>
            </button>

            <!-- Sorszám címke -->
            <span class="absolute bottom-1.5 left-1.5 px-2 py-0.5 rounded bg-black/70 text-white text-[11px] font-bold z-10">
              {{ idx + 1 }}.
            </span>
          </div>

          <!-- Új fájl hozzáadása gomb -->
          <button 
            v-if="files.length < 6" 
            type="button" 
            @click="triggerFileBrowser"
            class="aspect-[3/4] rounded-xl border-2 border-dashed border-text/20 hover:border-primary text-text-muted hover:text-primary flex flex-col items-center justify-center gap-1.5 transition-all active:scale-95 bg-background/40"
          >
            <i class="pi pi-plus text-lg"></i>
            <span class="text-xs font-bold">{{ $t('inventory.deliveryNote.addFile') }}</span>
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

    <!-- 2. LÉPÉS: TÉTELEK ÁTTEKINTÉSE, PÁROSÍTÁSA, ÁROSZLOP VÁLASZTÁS ÉS BEVÉTELEZÉSE -->
    <div v-else-if="step === 2" class="space-y-4">
      <!-- Fejléc adatok: Sorszám, Beszállító és Használt Ároszlop -->
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-3 p-3 bg-background/50 border border-text/10 rounded-xl text-xs">
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

        <!-- Ároszlop kiválasztó legördülő menü -->
        <div v-if="availablePriceColumns.length > 0">
          <label class="block font-bold text-text-muted mb-1 flex items-center gap-1">
            <i class="pi pi-tag text-primary"></i>
            <span>{{ $t('inventory.deliveryNote.priceColumn') }}</span>
          </label>
          <select 
            v-model="selectedPriceColumn" 
            @change="onPriceColumnChanged"
            class="w-full bg-background border border-text/20 rounded-lg p-2 font-bold text-primary focus:outline-none focus:border-primary text-xs"
          >
            <option v-for="col in availablePriceColumns" :key="col.id" :value="col.id">
              {{ col.label }} {{ col.isDefault ? '⭐' : '' }}
            </option>
          </select>
        </div>
      </div>

      <!-- Tájékoztató sáv -->
      <div class="p-2.5 bg-sky-500/10 border border-sky-500/20 text-sky-400 rounded-xl text-xs flex items-start gap-2">
        <i class="pi pi-info-circle text-sm shrink-0 mt-0.5"></i>
        <span>{{ $t('inventory.deliveryNote.matchingHint') }}</span>
      </div>

      <!-- Tételek listája -->
      <div class="space-y-3">
        <div 
          v-for="(row, idx) in processedItems" 
          :key="idx" 
          class="p-3.5 rounded-xl border transition-all space-y-2.5"
          :class="[
            row.action === 'skip' ? 'bg-background/30 border-text/10 opacity-50' :
            row.action === 'create' ? 'bg-emerald-500/5 border-emerald-500/30' :
            'bg-amber-500/5 border-amber-500/30'
          ]"
        >
          <!-- Felső sor: Kiolvasott név, Kód, Árnyalat és Kihagyás kapcsoló -->
          <div class="flex items-start justify-between gap-2">
            <div class="min-w-0 flex-1">
              <div class="flex items-center gap-1.5 flex-wrap">
                <!-- Név (szerkeszthető egy ceruza ikonra kattintva vagy duplaklikk) -->
                <div v-if="row.isEditingName" class="flex items-center gap-1">
                  <input 
                    v-model="row.name" 
                    @input="onNameChanged(row)"
                    @blur="row.isEditingName = false"
                    @keydown.enter="row.isEditingName = false"
                    class="text-xs font-bold text-text bg-surface border border-primary rounded-lg px-2 py-0.5 focus:outline-none"
                    autofocus
                  />
                  <button type="button" @click="row.isEditingName = false" class="text-xs text-primary font-bold px-1 hover:underline">OK</button>
                </div>
                <div v-else class="flex items-center gap-1 group/name">
                  <span class="text-xs font-bold text-text">{{ row.name }}</span>
                  <button 
                    type="button" 
                    @click="row.isEditingName = true" 
                    class="text-[10px] text-text-muted hover:text-primary opacity-40 group-hover/name:opacity-100 transition-opacity p-0.5"
                    :title="$t('inventory.deliveryNote.editName')"
                  >
                    <i class="pi pi-pencil"></i>
                  </button>
                </div>

                <!-- Árnyalat / Kód Badge (szerkeszthető + törölhető) vagy Hozzáadás gomb -->
                <div v-if="row.showShadeInput || cleanShade(row.shade)" class="inline-flex items-center gap-1 px-1.5 py-0.5 rounded-lg bg-primary/10 border border-primary/30 text-primary text-xs font-mono font-bold group/shade">
                  <span class="text-[10px] text-primary/70 select-none">[</span>
                  <input 
                    v-model="row.shade" 
                    @input="onShadeChanged(row)" 
                    :ref="el => setShadeInputRef(el, idx)"
                    class="bg-transparent border-none text-primary font-mono font-bold text-xs p-0 focus:outline-none min-w-[50px] max-w-[140px] placeholder:text-primary/40 placeholder:font-normal"
                    :placeholder="$t('inventory.deliveryNote.shadeInputPlaceholder')"
                  />
                  <span class="text-[10px] text-primary/70 select-none">]</span>
                  <button 
                    type="button" 
                    @click="removeRowShade(row)" 
                    class="text-primary/50 hover:text-red-400 p-0.5 transition-colors"
                    :title="$t('inventory.deliveryNote.removeShade')"
                  >
                    <i class="pi pi-times text-[10px]"></i>
                  </button>
                </div>
                <button 
                  v-else 
                  type="button" 
                  @click="addRowShade(row, idx)" 
                  class="inline-flex items-center gap-1 text-[11px] font-mono px-2 py-0.5 rounded-md border border-dashed border-primary/40 text-primary/80 hover:text-primary hover:border-primary hover:bg-primary/5 transition-all"
                >
                  <i class="pi pi-plus text-[9px]"></i>
                  <span>{{ $t('inventory.deliveryNote.addShadeTag') }}</span>
                </button>

                <span v-if="row.code" class="text-[10px] text-text-muted font-mono">
                  #{{ row.code }}
                </span>
              </div>
              <div v-if="row.rawName && row.rawName !== row.name" class="text-[10px] text-text-muted truncate mt-0.5">
                {{ row.rawName }}
              </div>
            </div>

            <!-- Kihagyás / bevételezés gomb -->
            <button 
              type="button" 
              @click="toggleRowSkip(row)" 
              class="text-xs px-2.5 py-1 rounded-lg border transition-all shrink-0 font-medium"
              :class="row.action === 'skip' ? 'bg-background text-text-muted border-text/20 hover:text-text' : 'bg-red-500/10 text-red-400 border-red-500/20 hover:bg-red-500/20'"
            >
              {{ row.action === 'skip' ? $t('inventory.deliveryNote.toggleRestore') : $t('inventory.deliveryNote.toggleSkip') }}
            </button>
          </div>

          <div v-if="row.action !== 'skip'" class="space-y-2.5 pt-1 border-t border-text/10">
            <!-- Párosítás Dropdown (Meglévő termék keresés hasonlóság szerint vagy Új) -->
            <div>
              <div class="flex items-center justify-between mb-1">
                <label class="block text-[11px] font-bold text-text-muted">
                  {{ $t('inventory.deliveryNote.matchWithStock') }}
                </label>
                <span 
                  v-if="row.action === 'create'" 
                  class="text-[10px] font-bold px-1.5 py-0.5 rounded bg-emerald-500/15 text-emerald-400 border border-emerald-500/30 flex items-center gap-1"
                >
                  <i class="pi pi-plus text-[9px]"></i> {{ $t('inventory.deliveryNote.actionCreate') }}
                </span>
                <span 
                  v-else-if="row.action === 'match'" 
                  class="text-[10px] font-bold px-1.5 py-0.5 rounded bg-amber-500/15 text-amber-400 border border-amber-500/30 flex items-center gap-1"
                >
                  <i class="pi pi-link text-[9px]"></i> {{ $t('inventory.deliveryNote.actionMatch') }}
                </span>
              </div>
              <select 
                v-model="row.selectedTarget" 
                @change="onTargetChanged(row)"
                class="w-full bg-surface border border-text/20 rounded-lg p-2 text-xs text-text focus:outline-none focus:border-primary font-medium"
              >
                <option value="__CREATE_NEW__">
                  {{ $t('inventory.deliveryNote.createNewProduct') }}
                </option>
                <optgroup :label="$t('inventory.deliveryNote.existingProducts')">
                  <option 
                    v-for="cand in row.candidates" 
                    :key="cand.product.id" 
                    :value="cand.product.id"
                  >
                    {{ cand.score >= 50 ? '⭐ ' : '' }}{{ cand.product.name }} {{ cleanShade(cand.product.shade) ? `[${cleanShade(cand.product.shade)}]` : '' }} {{ $t('inventory.deliveryNote.matchDetails', { score: cand.score, stock: cand.product.currentStock }) }}
                  </option>
                </optgroup>
              </select>
            </div>

            <!-- Mennyiség, Kiszerelés, Egységár és Összesen Ár mezők (4-es grid) -->
            <div class="grid grid-cols-2 sm:grid-cols-4 gap-2 text-xs">
              <!-- 1. Mennyiség / Darabszám -->
              <div>
                <label class="block text-[10px] font-bold text-text-muted mb-0.5">
                  {{ $t('inventory.deliveryNote.quantityLabel') }}
                </label>
                <div class="flex items-center gap-1">
                  <input 
                    type="number" 
                    v-model.number="row.quantity" 
                    @input="onQuantityChanged(row)"
                    step="1" 
                    min="1" 
                    class="w-full bg-surface border border-text/20 rounded-lg p-1.5 text-xs text-center font-bold text-text focus:outline-none focus:border-primary"
                  />
                  <span class="text-[10px] text-text-muted shrink-0 font-medium">{{ $t('inventory.units.short.pcs') }}</span>
                </div>
              </div>

              <!-- 2. Kiszerelés & Mértékegység -->
              <div>
                <label class="block text-[10px] font-bold text-text-muted mb-0.5">
                  {{ $t('inventory.deliveryNote.packageSizeLabel') }}
                </label>
                <div class="flex items-center gap-1">
                  <input 
                    type="number" 
                    v-model.number="row.packageSize" 
                    step="any" 
                    min="0.1" 
                    class="w-full bg-surface border border-text/20 rounded-lg p-1.5 text-xs text-center font-bold text-text focus:outline-none focus:border-primary"
                  />
                  <select 
                    v-model.number="row.unit"
                    class="bg-surface border border-text/20 rounded-lg p-1 text-[11px] text-text font-bold focus:outline-none focus:border-primary shrink-0"
                  >
                    <option :value="0">{{ $t('inventory.units.short.ml') }}</option>
                    <option :value="1">{{ $t('inventory.units.short.g') }}</option>
                    <option :value="2">{{ $t('inventory.units.short.pcs') }}</option>
                    <option :value="3">{{ $t('inventory.units.short.m') }}</option>
                    <option :value="4">{{ $t('inventory.units.short.cm') }}</option>
                  </select>
                </div>
              </div>

              <!-- 3. Beszerzési Egységár -->
              <div>
                <label class="block text-[10px] font-bold text-text-muted mb-0.5">
                  {{ $t('inventory.deliveryNote.unitPriceLabel') }}
                </label>
                <div class="flex items-center gap-1">
                  <input 
                    type="number" 
                    v-model.number="row.unitPrice" 
                    @input="onUnitPriceChanged(row)"
                    step="0.01" 
                    min="0" 
                    class="w-full bg-surface border border-text/20 rounded-lg p-1.5 text-xs text-right font-bold text-text focus:outline-none focus:border-primary"
                  />
                  <span class="text-[10px] text-text-muted shrink-0 font-medium">€</span>
                </div>
              </div>

              <!-- 4. Összesen Ár (Tétel) -->
              <div>
                <label class="block text-[10px] font-bold text-text-muted mb-0.5">
                  {{ $t('inventory.deliveryNote.totalPriceLabel') }}
                </label>
                <div class="flex items-center gap-1">
                  <input 
                    type="number" 
                    v-model.number="row.totalPrice" 
                    @input="onTotalPriceChanged(row)"
                    step="0.01" 
                    min="0" 
                    class="w-full bg-surface border border-text/20 rounded-lg p-1.5 text-xs text-right font-bold text-primary focus:outline-none focus:border-primary"
                  />
                  <span class="text-[10px] text-text-muted shrink-0 font-medium">€</span>
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
        :disabled="files.length === 0 || scanning || compressing" 
        class="h-10 px-5 rounded-xl text-xs md:text-sm font-bold bg-primary text-white shadow-md hover:brightness-110 active:scale-95 transition-all disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
      >
        <i v-if="scanning || compressing" class="pi pi-spinner pi-spin text-sm"></i>
        <i v-else class="pi pi-sparkles text-sm"></i>
        <span>{{ compressing ? $t('inventory.deliveryNote.optimizing') : (scanning ? $t('inventory.deliveryNote.analyzingBtn') : $t('inventory.deliveryNote.startScanBtn')) }}</span>
      </button>

      <!-- 2. Lépésben: Vissza gomb & Végleges Bevételezés gomb -->
      <div v-else-if="step === 2" class="flex gap-2">
        <button 
          type="button" 
          @click="step = 1" 
          :disabled="importing" 
          class="h-10 px-3 rounded-xl text-xs md:text-sm font-bold bg-background text-text border border-text/20 hover:bg-text/5 active:scale-95 transition-all"
        >
          {{ $t('inventory.deliveryNote.retakeFiles') }}
        </button>

        <button 
          type="button" 
          @click="confirmImport" 
          :disabled="importing || activeItemsCount === 0" 
          class="h-10 px-5 rounded-xl text-xs md:text-sm font-bold bg-primary text-white shadow-md hover:brightness-110 active:scale-95 transition-all disabled:opacity-50 flex items-center gap-2"
        >
          <i v-if="importing" class="pi pi-spinner pi-spin text-sm"></i>
          <i v-else class="pi pi-check text-sm"></i>
          <span>{{ importing ? $t('inventory.deliveryNote.saving') : $t('inventory.deliveryNote.confirmImportCount', { count: activeItemsCount }) }}</span>
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
import { compressImages } from '@/utils/imageCompressor';

const { t } = useI18n();

const props = defineProps({
  visible: Boolean,
  products: {
    type: Array,
    default: () => []
  }
});

const emit = defineEmits(['update:visible', 'saved']);

const step = ref(1); // 1 = Fájlok/Fotók feltöltése, 2 = Ellenőrzés & párosítás
const cameraInput = ref(null);
const fileInput = ref(null);

const files = ref([]); // { file: File, previewUrl: string | null, name: string, sizeText: string, typeCategory: string, isImage: boolean }
const scanning = ref(false);
const compressing = ref(false);
const importing = ref(false);
const errorMessage = ref('');
const importError = ref('');

const deliveryData = ref({
  documentNumber: '',
  supplier: '',
  issueDate: '',
  items: []
});

const availablePriceColumns = ref([]);
const selectedPriceColumn = ref('');
const processedItems = ref([]);

const triggerCamera = () => {
  if (cameraInput.value) cameraInput.value.click();
};

const triggerFileBrowser = () => {
  if (fileInput.value) fileInput.value.click();
};

const getFileTypeCategory = (file) => {
  const name = file.name?.toLowerCase() || '';
  const mime = file.type?.toLowerCase() || '';
  if (mime.startsWith('image/') || /\.(jpg|jpeg|png|webp|heic|gif)$/i.test(name)) return 'image';
  if (mime === 'application/pdf' || name.endsWith('.pdf')) return 'pdf';
  if (mime.includes('spreadsheet') || mime.includes('excel') || /\.(xlsx|xls|csv)$/i.test(name)) return 'excel';
  if (mime.includes('word') || /\.(docx|doc)$/i.test(name)) return 'word';
  return 'file';
};

const formatFileSize = (bytes) => {
  if (!bytes) return '0 B';
  if (bytes < 1024) return bytes + ' B';
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(0) + ' KB';
  return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
};

const handleFilesSelected = async (event) => {
  errorMessage.value = '';
  const selectedFiles = Array.from(event.target.files || []);
  if (!selectedFiles.length) return;

  const remaining = 6 - files.value.length;
  const toAdd = selectedFiles.slice(0, remaining);

  const imagesToCompress = [];
  const otherFiles = [];

  for (const f of toAdd) {
    const cat = getFileTypeCategory(f);
    if (cat === 'image') {
      imagesToCompress.push(f);
    } else {
      otherFiles.push(f);
    }
  }

  // 1. Dokumentumok (PDF, Excel, Word) hozzáadása közvetlenül tömörítés nélkül
  otherFiles.forEach(f => {
    files.value.push({
      file: f,
      previewUrl: null,
      name: f.name,
      sizeText: formatFileSize(f.size),
      typeCategory: getFileTypeCategory(f),
      isImage: false
    });
  });

  // 2. Képek kliensoldali tömörítése
  if (imagesToCompress.length > 0) {
    compressing.value = true;
    try {
      const compressed = await compressImages(imagesToCompress, {
        maxWidth: 1800,
        maxHeight: 1800,
        quality: 0.82
      });

      compressed.forEach(f => {
        files.value.push({
          file: f,
          previewUrl: URL.createObjectURL(f),
          name: f.name,
          sizeText: formatFileSize(f.size),
          typeCategory: 'image',
          isImage: true
        });
      });
    } catch (err) {
      console.error('Képtömörítési hiba, eredeti fájlok használata:', err);
      imagesToCompress.forEach(f => {
        files.value.push({
          file: f,
          previewUrl: URL.createObjectURL(f),
          name: f.name,
          sizeText: formatFileSize(f.size),
          typeCategory: 'image',
          isImage: true
        });
      });
    } finally {
      compressing.value = false;
    }
  }

  event.target.value = '';
};

const removeFile = (index) => {
  const removed = files.value.splice(index, 1)[0];
  if (removed && removed.previewUrl) {
    URL.revokeObjectURL(removed.previewUrl);
  }
};

const clearAllFiles = () => {
  files.value.forEach(item => {
    if (item.previewUrl) URL.revokeObjectURL(item.previewUrl);
  });
  files.value = [];
  errorMessage.value = '';
};

const hideDialog = () => {
  if (!scanning.value && !importing.value) {
    emit('update:visible', false);
  }
};

const cleanShade = (s) => {
  if (!s) return null;
  const str = String(s).trim();
  if (['null', 'undefined', 'none', 'n/a', '-', 'false', 'ismeretlen'].includes(str.toLowerCase())) return null;
  return str;
};

// Szövegtisztítás fuzzy kereséshez
const normalizeStr = (str) => {
  if (!str) return '';
  return str
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]/g, ' ')
    .replace(/\s+/g, ' ')
    .trim();
};

const diceCoefficient = (s1, s2) => {
  if (!s1 || !s2) return 0;
  if (s1 === s2) return 1.0;
  if (s1.length < 2 || s2.length < 2) return s1 === s2 ? 1.0 : 0;

  const bigrams1 = new Map();
  for (let i = 0; i < s1.length - 1; i++) {
    const bg = s1.substr(i, 2);
    bigrams1.set(bg, (bigrams1.get(bg) || 0) + 1);
  }

  let intersection = 0;
  for (let i = 0; i < s2.length - 1; i++) {
    const bg = s2.substr(i, 2);
    const count = bigrams1.get(bg) || 0;
    if (count > 0) {
      bigrams1.set(bg, count - 1);
      intersection++;
    }
  }

  return (2.0 * intersection) / ((s1.length - 1) + (s2.length - 1));
};

// Segédfüggvény: Kinyeri az árnyalat kódját és a leíró nevét
const parseShadeTokens = (shadeStr) => {
  if (!shadeStr) return { raw: '', code: '', name: '', tokens: [] };
  const raw = String(shadeStr).trim();
  const clean = raw.replace(/,/g, '.');
  
  // Minden különálló token leválasztása (szóköz, kötőjel, alávonás, zárójelek mentén)
  const tokens = clean
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .split(/[\s\-_()[\]{}]+/)
    .filter(Boolean);

  // Keresünk számot vagy standard nuance jelölést tartalmazó tokent (pl. "5.0", "2.n", "7.1", "6rr", "0/00")
  const codeToken = tokens.find(t => /\d/.test(t) || ['clear', 'transp', 'neutral'].includes(t)) || '';
  const nameTokens = tokens.filter(t => t !== codeToken);

  return {
    raw,
    code: codeToken,
    name: nameTokens.join(' '),
    tokens
  };
};

const getShadeSimilarity = (itemShadeStr, prodShadeStr, sItemRaw = '', sItemName = '') => {
  const iClean = cleanShade(itemShadeStr);
  const pClean = cleanShade(prodShadeStr);

  if (!iClean && !pClean) return 1.0; // Egyiknek sincs árnyalata (pl. sampon)

  // Ha a raktári terméknek van árnyalata, de a tételnek nincs közvetlen shade-je:
  if (!iClean && pClean) {
    const sP = normalizeStr(pClean);
    if (sItemRaw.includes(sP) || sItemName.includes(sP)) {
      return 0.95; // A nyers tétel szövegében benne van a termék árnyalata!
    }
    const pTokens = parseShadeTokens(pClean);
    if (pTokens.code && (sItemRaw.includes(pTokens.code) || sItemName.includes(pTokens.code))) {
      return 0.90;
    }
    return 0.50;
  }

  // Ha a tételnek van árnyalata, de a raktári terméknek nincs:
  if (iClean && !pClean) {
    return 0.35;
  }

  // Mindkettőnek van árnyalata:
  const s1 = normalizeStr(iClean);
  const s2 = normalizeStr(pClean);
  if (s1 === s2) return 1.0; // Pontos szöveges egyezés

  const p1 = parseShadeTokens(iClean);
  const p2 = parseShadeTokens(pClean);

  // 1. Ha mindkettőben van kód (pl. "5.0", "2.n", "7.1", "7/1"):
  if (p1.code && p2.code) {
    const c1 = p1.code.replace('/', '.');
    const c2 = p2.code.replace('/', '.');
    if (c1 === c2) {
      return 0.98; // A kód MEGEGYEZIK! (pl. "5.0 világosbarna" vs "5.0")
    }
    return 0.15; // Különböző kódok! (pl. "5.0" vs "6.0") -> Biztosan nem ugyanaz a termék
  }

  // 2. Ha az egyik tartalmazza a másikat (pl. "5.0 Világosbarna" vs "Világosbarna"):
  if (s1.includes(s2) || s2.includes(s1)) {
    return 0.92;
  }

  // 3. Ha van közös név-token:
  const common = p1.tokens.filter(t => p2.tokens.includes(t));
  if (common.length > 0) {
    return 0.80;
  }

  return 0.20;
};

const calculateSimilarity = (item, prod) => {
  const itemEan = cleanShade(item.ean);
  const prodEan = cleanShade(prod.ean);
  if (itemEan && prodEan && itemEan === prodEan) {
    return 100;
  }

  const sItemName = normalizeStr(item.name || '');
  const sItemRaw = normalizeStr(item.rawName || '');
  const sProdName = normalizeStr(prod.name || '');

  const diceName = Math.max(
    diceCoefficient(sItemName, sProdName),
    diceCoefficient(sItemRaw, sProdName)
  );

  const itemTokens = Array.from(new Set(`${sItemName} ${sItemRaw}`.split(' ').filter(w => w.length > 1)));
  const prodTokens = Array.from(new Set(sProdName.split(' ').filter(w => w.length > 1)));

  let tokenMatchSum = 0;
  if (prodTokens.length > 0 && itemTokens.length > 0) {
    for (const pToken of prodTokens) {
      let bestWordSim = 0;
      for (const iToken of itemTokens) {
        if (pToken === iToken) {
          bestWordSim = 1.0;
          break;
        }
        if (pToken.includes(iToken) || iToken.includes(pToken)) {
          bestWordSim = Math.max(bestWordSim, 0.9);
        } else {
          const wordDice = diceCoefficient(pToken, iToken);
          if (wordDice > bestWordSim) bestWordSim = wordDice;
        }
      }
      tokenMatchSum += bestWordSim;
    }
  }
  const tokenSim = prodTokens.length > 0 ? (tokenMatchSum / prodTokens.length) : 0;
  let nameScore = Math.round(Math.max(diceName, tokenSim) * 100);

  const shadeSim = getShadeSimilarity(item.shade || item.code, prod.shade, sItemRaw, sItemName);

  if (shadeSim >= 0.90) {
    if (nameScore >= 40) {
      nameScore = Math.max(nameScore, Math.round(85 + (nameScore * 0.14)));
    }
  } else if (shadeSim <= 0.25) {
    nameScore = Math.round(nameScore * 0.25);
  }

  return Math.max(0, Math.min(nameScore, 100));
};

const startScan = async () => {
  if (files.value.length === 0 || scanning.value) return;

  scanning.value = true;
  errorMessage.value = '';

  try {
    const rawFiles = files.value.map(i => i.file);
    const response = await inventoryApi.scanDeliveryNote(rawFiles);
    const data = response.data;

    deliveryData.value = {
      documentNumber: cleanShade(data.documentNumber) || '',
      supplier: cleanShade(data.supplier) || '',
      issueDate: cleanShade(data.issueDate) || '',
      items: data.items || []
    };

    availablePriceColumns.value = data.availablePriceColumns || [];
    selectedPriceColumn.value = data.defaultPriceColumnId || (availablePriceColumns.value[0]?.id || '');

    const allProds = props.products || [];
    
    processedItems.value = (data.items || []).map(item => {
      const initialQty = Number(item.quantity) > 0 ? Number(item.quantity) : 1;
      const initialUnitCost = Number(item.unitPrice) >= 0 ? Number(item.unitPrice) : 0;
      const initialTotal = Number(item.totalPrice) > 0 ? Number(item.totalPrice) : Math.round(initialQty * initialUnitCost * 100) / 100;
      const pkgSize = Number(item.packageSize) > 0 ? Number(item.packageSize) : 1;
      const unitVal = Number(item.unit) >= 0 ? Number(item.unit) : 0;

      const sanitizedItem = {
        ...item,
        name: cleanShade(item.name) || item.rawName || 'Termék',
        shade: cleanShade(item.shade),
        code: cleanShade(item.code),
        ean: cleanShade(item.ean),
        quantity: initialQty,
        packageSize: pkgSize,
        unit: unitVal,
        unitPrice: initialUnitCost,
        totalPrice: initialTotal,
        costPrice: initialUnitCost,
        availablePrices: item.availablePrices || {}
      };

      const candidates = allProds.map(p => ({
        product: p,
        score: calculateSimilarity(sanitizedItem, p)
      })).sort((a, b) => b.score - a.score);

      const topCandidate = candidates[0];
      const hasStrongMatch = topCandidate && topCandidate.score >= 50;

      return {
        ...sanitizedItem,
        showShadeInput: false,
        isEditingName: false,
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
                         'Nem sikerült kiolvasni a szállítólevelet. Kérlek ellenőrizd a fájlok formátumát vagy az AI beállításokat.';
  } finally {
    scanning.value = false;
  }
};

// Ároszlop változás kezelése (minden tétel egységárát és összegét újraszámolja)
const onPriceColumnChanged = () => {
  const colId = selectedPriceColumn.value;
  if (!colId) return;

  processedItems.value.forEach(row => {
    if (row.availablePrices && row.availablePrices[colId] != null) {
      const newPrice = Number(row.availablePrices[colId]) || 0;
      row.unitPrice = newPrice;
      row.costPrice = newPrice;
      row.totalPrice = Math.round((row.quantity * newPrice) * 100) / 100;
    }
  });
};

// Kétirányú reaktív mezőszámítások a tételeknél:
const onQuantityChanged = (row) => {
  const qty = Number(row.quantity) || 1;
  const unitP = Number(row.unitPrice) || 0;
  row.totalPrice = Math.round(qty * unitP * 100) / 100;
  row.costPrice = unitP;
};

const onUnitPriceChanged = (row) => {
  const qty = Number(row.quantity) || 1;
  const unitP = Number(row.unitPrice) || 0;
  row.costPrice = unitP;
  row.totalPrice = Math.round(qty * unitP * 100) / 100;
};

const onTotalPriceChanged = (row) => {
  const qty = Number(row.quantity) || 1;
  const tot = Number(row.totalPrice) || 0;
  if (qty > 0) {
    const calcUnit = Math.round((tot / qty) * 100) / 100;
    row.unitPrice = calcUnit;
    row.costPrice = calcUnit;
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

const shadeInputRefs = ref({});
const setShadeInputRef = (el, idx) => {
  if (el) shadeInputRefs.value[idx] = el;
};

const addRowShade = (row, idx) => {
  row.showShadeInput = true;
  if (!row.shade) row.shade = '';
  setTimeout(() => {
    if (shadeInputRefs.value[idx]) {
      shadeInputRefs.value[idx].focus();
    }
  }, 50);
};

const removeRowShade = (row) => {
  row.shade = null;
  row.showShadeInput = false;
  recalculateItemCandidates(row);
};

const onShadeChanged = (row) => {
  recalculateItemCandidates(row);
};

const onNameChanged = (row) => {
  recalculateItemCandidates(row);
};

const recalculateItemCandidates = (row) => {
  const allProds = props.products || [];
  const sanitizedItem = {
    ...row,
    name: cleanShade(row.name) || row.rawName || 'Termék',
    shade: cleanShade(row.shade),
    code: cleanShade(row.code),
    ean: cleanShade(row.ean)
  };

  const candidates = allProds.map(p => ({
    product: p,
    score: calculateSimilarity(sanitizedItem, p)
  })).sort((a, b) => b.score - a.score);

  row.candidates = candidates;
  const topCandidate = candidates[0];
  const hasStrongMatch = topCandidate && topCandidate.score >= 50;

  if (hasStrongMatch) {
    row.selectedTarget = topCandidate.product.id;
    row.action = 'match';
  } else {
    const currentStillValid = candidates.some(c => c.product.id === row.selectedTarget && c.score >= 40);
    if (!currentStillValid) {
      row.selectedTarget = '__CREATE_NEW__';
      row.action = 'create';
    }
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
        const qty = Number(item.quantity) || 1;
        const unitCost = Number(item.unitPrice) || 0;
        const pkgSize = Number(item.packageSize) || 1;
        const unitVal = Number(item.unit) >= 0 ? Number(item.unit) : 0;

        if (item.action === 'match') {
          return {
            action: 'match',
            matchedProductId: Number(item.selectedTarget),
            quantity: qty,
            costPrice: unitCost
          };
        } else {
          return {
            action: 'create',
            newProduct: {
              name: item.name || item.rawName || 'Új termék',
              shade: cleanShade(item.shade) || null,
              ean: item.ean || null,
              unit: unitVal,
              packageSize: pkgSize,
              costPrice: unitCost,
              retailPrice: unitCost > 0 ? Math.round(unitCost * 1.5 * 100) / 100 : 0,
              lowStockThreshold: 2,
              isProfessional: true,
              isRetail: false
            },
            quantity: qty,
            costPrice: unitCost
          };
        }
      });

    const payload = {
      documentNumber: deliveryData.value.documentNumber,
      supplier: deliveryData.value.supplier,
      note: 'AI szállítólevél import',
      items: payloadItems
    };

    const res = await inventoryApi.importDeliveryNote(payload);

    // Biztosítjuk, hogy a bevételezett tételek listája hiánytalanul átadásra kerüljön
    const receivedItems = (res.data && Array.isArray(res.data.receivedItems) && res.data.receivedItems.length > 0)
      ? res.data.receivedItems
      : payloadItems.map(p => ({
          productId: p.matchedProductId,
          name: p.newProduct?.name,
          shade: p.newProduct?.shade,
          quantityAdded: p.quantity,
          isNewProduct: p.action === 'create'
        }));

    emit('saved', { ...(res.data || {}), receivedItems });
    clearAllFiles();
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

