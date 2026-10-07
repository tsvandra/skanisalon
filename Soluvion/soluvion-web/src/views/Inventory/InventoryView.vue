<template>
  <div class="min-h-screen bg-background text-text p-3 sm:p-4 md:p-8">
    <div class="max-w-7xl mx-auto space-y-4 md:space-y-6">

      <!-- Felső Fejléc & Fő Műveletek -->
      <div class="flex flex-col md:flex-row justify-between items-start md:items-center gap-4 bg-surface p-4 sm:p-5 md:p-6 rounded-2xl shadow-sm border border-text/10">
        <div>
          <h1 class="text-2xl md:text-3xl font-black text-primary flex items-center gap-2.5">
            <i class="pi pi-box"></i> {{ $t('inventory.view.title') }}
          </h1>
          <div class="flex flex-wrap items-center gap-2 mt-1">
            <p class="text-text-muted text-xs md:text-sm">
              {{ $t('inventory.view.totalProducts', { count: products.length }) }}
            </p>
            <span 
              v-if="lowStockCount > 0" 
              @click="activeFilter = 'lowStock'"
              class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-bold bg-amber-500/10 text-amber-400 border border-amber-500/30 cursor-pointer hover:bg-amber-500/20 transition-colors"
              title="Kattints az elfogyóban lévő termékek szűréséhez"
            >
              <i class="pi pi-exclamation-triangle text-[11px]"></i>
              {{ $t('inventory.view.lowStockWarning', { count: lowStockCount }) }}
            </span>
          </div>
        </div>

        <div class="grid grid-cols-2 sm:flex items-center gap-2.5 w-full md:w-auto">
          <button 
            @click="openStockDialog" 
            class="h-[44px] px-3.5 sm:px-5 bg-surface text-text border border-text/20 font-bold text-xs sm:text-sm rounded-xl shadow-sm hover:border-primary hover:text-primary active:scale-95 transition-all flex items-center justify-center gap-2 whitespace-nowrap"
          >
            <i class="pi pi-arrow-right-arrow-left text-xs sm:text-sm"></i>
            <span class="truncate">{{ $t('inventory.view.stockMovement') }}</span>
          </button>
          
          <button 
            @click="openProductDialog" 
            class="h-[44px] px-3.5 sm:px-5 bg-primary text-white font-bold text-xs sm:text-sm rounded-xl shadow-md hover:brightness-110 active:scale-95 transition-all flex items-center justify-center gap-2 whitespace-nowrap"
          >
            <i class="pi pi-plus text-xs sm:text-sm"></i>
            <span class="truncate">{{ $t('inventory.view.newProduct') }}</span>
          </button>
        </div>
      </div>

      <!-- Kereső, Szűrők & Nézetváltó Sáv -->
      <div class="bg-surface p-3 sm:p-4 rounded-2xl shadow-sm border border-text/10 flex flex-col md:flex-row gap-3 items-stretch md:items-center justify-between">
        
        <!-- Élő kereső -->
        <div class="relative w-full md:w-80">
          <i class="pi pi-search absolute left-3 top-1/2 -translate-y-1/2 text-text-muted"></i>
          <input 
            type="text" 
            v-model="searchQuery" 
            :placeholder="$t('inventory.view.searchPlaceholder')"
            class="w-full h-[42px] pl-10 pr-9 bg-background border border-text/20 rounded-xl text-sm focus:outline-none focus:border-primary transition-colors text-text"
          />
          <button 
            v-if="searchQuery" 
            @click="searchQuery = ''" 
            class="absolute right-3 top-1/2 -translate-y-1/2 text-text-muted hover:text-text transition-colors"
          >
            <i class="pi pi-times text-xs"></i>
          </button>
        </div>

        <!-- Szűrő fülek (mobilon vízszintesen görgethető gombok) -->
        <div class="flex items-center gap-1.5 overflow-x-auto no-scrollbar py-0.5">
          <button 
            @click="activeFilter = 'all'"
            :class="activeFilter === 'all' ? 'bg-primary text-white border-primary shadow-xs' : 'bg-background text-text-muted border-text/10 hover:text-text hover:border-text/30'"
            class="px-3 py-1.5 rounded-xl text-xs font-bold whitespace-nowrap transition-all border flex items-center gap-1.5 active:scale-95"
          >
            <span>{{ $t('inventory.view.all') }}</span>
            <span class="opacity-70 text-[11px]">({{ products.length }})</span>
          </button>

          <button 
            @click="activeFilter = 'lowStock'"
            :class="activeFilter === 'lowStock' ? 'bg-amber-500 text-black border-amber-500 font-black shadow-xs' : 'bg-background text-text-muted border-text/10 hover:text-text hover:border-text/30'"
            class="px-3 py-1.5 rounded-xl text-xs font-bold whitespace-nowrap transition-all border flex items-center gap-1.5 active:scale-95"
          >
            <i class="pi pi-exclamation-triangle text-[11px]"></i>
            <span>{{ $t('inventory.view.lowStockFilter') }}</span>
            <span v-if="lowStockCount > 0" class="px-1.5 py-0.2 rounded-full text-[10px] bg-black/20">
              {{ lowStockCount }}
            </span>
          </button>

          <button 
            @click="activeFilter = 'professional'"
            :class="activeFilter === 'professional' ? 'bg-sky-500 text-white border-sky-500 shadow-xs' : 'bg-background text-text-muted border-text/10 hover:text-text hover:border-text/30'"
            class="px-3 py-1.5 rounded-xl text-xs font-bold whitespace-nowrap transition-all border flex items-center gap-1.5 active:scale-95"
          >
            <span>{{ $t('inventory.view.professional') }}</span>
            <span class="opacity-70 text-[11px]">({{ professionalCount }})</span>
          </button>

          <button 
            @click="activeFilter = 'retail'"
            :class="activeFilter === 'retail' ? 'bg-emerald-600 text-white border-emerald-600 shadow-xs' : 'bg-background text-text-muted border-text/10 hover:text-text hover:border-text/30'"
            class="px-3 py-1.5 rounded-xl text-xs font-bold whitespace-nowrap transition-all border flex items-center gap-1.5 active:scale-95"
          >
            <span>{{ $t('inventory.view.retail') }}</span>
            <span class="opacity-70 text-[11px]">({{ retailCount }})</span>
          </button>
        </div>

        <!-- Nézet kapcsoló (Kártya / Táblázat) -->
        <div class="hidden sm:flex items-center gap-1 bg-background p-1 rounded-xl border border-text/10 shrink-0">
          <button 
            @click="viewMode = 'cards'" 
            :class="viewMode === 'cards' ? 'bg-primary text-white shadow-xs' : 'text-text-muted hover:text-text'"
            class="px-2.5 py-1.5 rounded-lg text-xs font-bold flex items-center gap-1.5 transition-all"
            :title="$t('inventory.view.viewCards')"
          >
            <i class="pi pi-th-large text-xs"></i>
            <span>{{ $t('inventory.view.viewCards') }}</span>
          </button>
          <button 
            @click="viewMode = 'table'" 
            :class="viewMode === 'table' ? 'bg-primary text-white shadow-xs' : 'text-text-muted hover:text-text'"
            class="px-2.5 py-1.5 rounded-lg text-xs font-bold flex items-center gap-1.5 transition-all"
            :title="$t('inventory.view.viewTable')"
          >
            <i class="pi pi-list text-xs"></i>
            <span>{{ $t('inventory.view.viewTable') }}</span>
          </button>
        </div>

      </div>

      <!-- Betöltés állapot -->
      <div v-if="loading" class="text-center py-16 text-text-muted font-bold animate-pulse">
        <i class="pi pi-spinner pi-spin text-3xl text-primary mb-3"></i>
        <p>{{ $t('common.loading') }}</p>
      </div>

      <!-- Üres találati lista -->
      <div v-else-if="filteredProducts.length === 0" class="text-center py-16 bg-surface rounded-2xl border border-dashed border-text/20 text-text-muted space-y-3">
        <i class="pi pi-inbox text-4xl text-text/30"></i>
        <p class="font-medium text-base">
          {{ searchQuery || activeFilter !== 'all' ? $t('inventory.view.noResults') : $t('inventory.view.empty') }}
        </p>
        <button 
          v-if="searchQuery || activeFilter !== 'all'" 
          @click="resetFilters" 
          class="px-4 py-2 bg-background border border-text/20 rounded-xl text-xs font-bold text-text hover:border-primary hover:text-primary active:scale-95 transition-all"
        >
          Szűrők törlése
        </button>
        <button 
          v-else 
          @click="openProductDialog" 
          class="px-5 py-2.5 bg-primary text-white rounded-xl text-sm font-bold shadow-md hover:brightness-110 active:scale-95 transition-all inline-flex items-center gap-2"
        >
          <i class="pi pi-plus"></i> {{ $t('inventory.view.newProduct') }}
        </button>
      </div>

      <!-- KÁRTYÁS NÉZET (Kompakt, áttekinthető mobil- és asztali lista) -->
      <div v-else-if="viewMode === 'cards'" class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-2.5 sm:gap-3">
        <div 
          v-for="product in filteredProducts" 
          :key="product.id"
          class="bg-surface rounded-xl p-3 sm:p-3.5 shadow-2xs border border-text/10 hover:border-primary/40 transition-all flex items-center justify-between gap-3 group"
        >
          <!-- Bal oldal: Név, Címkék, Kiszerelés, Ár (kattintásra termékszerkesztés) -->
          <div class="flex-1 min-w-0 cursor-pointer" @click="editProduct(product)">
            <!-- 1. sor: Név teljes terjedelmében + Kiemelt Kód / Árnyalat plakett -->
            <div class="flex items-center gap-1.5 flex-wrap">
              <h3 
                class="font-bold text-sm sm:text-base text-text group-hover:text-primary transition-colors leading-snug line-clamp-2 break-words"
                :title="product.name"
              >
                {{ product.name }}
              </h3>

              <!-- Kiemelt Kód / Árnyalat (pl. festékek színkódja: 15.2, 7.1) -->
              <span 
                v-if="product.shade" 
                class="text-xs font-black bg-primary/15 text-primary border border-primary/30 px-2 py-0.5 rounded-lg shrink-0 tracking-wide"
                :title="$t('inventory.view.columns.shade')"
              >
                {{ product.shade }}
              </span>
            </div>

            <!-- 2. sor: Kiszerelés, Típus badge, Ár, Készlet figyelmeztetés -->
            <div class="flex items-center gap-2 mt-1 text-xs text-text-muted flex-wrap">
              <!-- Kompakt Típus címkék a 2. sorban -->
              <span 
                v-if="product.isProfessional" 
                class="text-[10px] bg-sky-500/10 text-sky-400 px-1.5 py-0.2 rounded font-semibold border border-sky-500/20 shrink-0"
              >
                {{ $t('inventory.view.professional') }}
              </span>
              <span 
                v-if="product.isRetail" 
                class="text-[10px] bg-emerald-500/10 text-emerald-400 px-1.5 py-0.2 rounded font-semibold border border-emerald-500/20 shrink-0"
              >
                {{ $t('inventory.view.retail') }}
              </span>

              <span class="font-medium text-text-muted">
                {{ product.packageSize }} {{ getUnitName(product.unit) }}
              </span>

              <template v-if="product.retailPrice > 0">
                <span class="text-text-muted/40">•</span>
                <span class="text-emerald-400 font-semibold">
                  {{ formatCurrency(product.retailPrice) }}
                </span>
              </template>

              <!-- Figyelmeztetés CSAK ha kevés a készlet vagy kifogyott -->
              <template v-if="product.currentStock <= 0">
                <span class="text-text-muted/40">•</span>
                <span class="text-red-400 font-bold flex items-center gap-1 text-[11px]">
                  <i class="pi pi-times-circle text-[10px]"></i> {{ $t('inventory.view.outOfStock') }}
                </span>
              </template>
              <template v-else-if="product.currentStock <= product.lowStockThreshold">
                <span class="text-text-muted/40">•</span>
                <span class="text-amber-400 font-bold flex items-center gap-1 text-[11px]">
                  <i class="pi pi-exclamation-triangle text-[10px]"></i> Min: {{ product.lowStockThreshold }} {{ $t('inventory.units.short.pcs') }}
                </span>
              </template>
            </div>
          </div>

          <!-- Jobb oldal: Érintésbarát Készlet szám (színkódolt: zöld/sárga/piros) -->
          <div class="flex items-center shrink-0">
            <button 
              @click.stop="openQuickStock(product)"
              class="h-10 px-3 rounded-xl border transition-all flex items-center gap-1.5 active:scale-95 shadow-2xs"
              :class="[
                product.currentStock <= 0 
                  ? 'bg-red-500/15 border-red-500/40 text-red-400 hover:bg-red-500/25' 
                  : product.currentStock <= product.lowStockThreshold 
                    ? 'bg-amber-500/15 border-amber-500/40 text-amber-400 hover:bg-amber-500/25' 
                    : 'bg-emerald-500/10 border-emerald-500/25 text-emerald-400 hover:bg-emerald-500/20'
              ]"
              :title="$t('inventory.view.quickStockTitle')"
            >
              <span class="text-base font-black">
                {{ product.currentStock }}
              </span>
              <span class="text-xs font-semibold opacity-75">
                {{ $t('inventory.units.short.pcs') }}
              </span>
              <i class="pi pi-pencil text-[10px] opacity-60 ml-0.5"></i>
            </button>
          </div>

        </div>
      </div>

      <!-- TÁBLÁZAT NÉZET (Asztali / Részletes nézet) -->
      <div v-else class="bg-surface rounded-2xl border border-text/10 shadow-sm overflow-hidden">
        <DataTable 
          :value="filteredProducts" 
          responsiveLayout="scroll" 
          class="p-datatable-sm"
          :pt="{
            table: { class: 'w-full text-left border-collapse' },
            thead: { class: 'bg-background/80 border-b border-text/10 text-xs font-bold text-text-muted uppercase' },
            headerRow: { class: 'border-b border-text/10' },
            tbody: { class: 'divide-y divide-text/10 text-sm' }
          }"
        >
          <Column :header="$t('inventory.view.columns.name')" :sortable="true" field="name">
            <template #body="slotProps">
              <div class="flex items-center gap-2">
                <span 
                  class="cursor-pointer text-primary font-bold hover:underline" 
                  @click="editProduct(slotProps.data)" 
                  :title="$t('inventory.view.editProductTitle')"
                >
                  {{ slotProps.data.name }}
                </span>
                <span 
                  v-if="slotProps.data.shade" 
                  class="text-[11px] font-black bg-primary/15 text-primary border border-primary/30 px-1.5 py-0.2 rounded-md shrink-0"
                >
                  {{ slotProps.data.shade }}
                </span>
              </div>
            </template>
          </Column>

          <Column field="ean" :header="$t('inventory.view.columns.ean')" :sortable="true" headerClass="hidden lg:table-cell" bodyClass="hidden lg:table-cell">
            <template #body="slotProps">
              <span class="font-mono text-xs text-text-muted">{{ slotProps.data.ean || '-' }}</span>
            </template>
          </Column>

          <Column :header="$t('inventory.view.columns.type')">
            <template #body="slotProps">
              <Badge v-if="slotProps.data.isProfessional" :value="$t('inventory.view.professional')" severity="info" class="mr-1.5 px-2 py-0.5 text-xs" />
              <Badge v-if="slotProps.data.isRetail" :value="$t('inventory.view.retail')" severity="success" class="px-2 py-0.5 text-xs" />
            </template>
          </Column>

          <Column field="packageSize" :header="$t('inventory.view.columns.packageSize')">
            <template #body="slotProps">
              {{ slotProps.data.packageSize }} {{ getUnitName(slotProps.data.unit) }}
            </template>
          </Column>

          <Column field="currentStock" :header="$t('inventory.view.columns.stock')" :sortable="true">
            <template #body="slotProps">
              <div 
                class="cursor-pointer inline-flex items-center gap-1.5 px-2.5 py-1 rounded-xl hover:bg-text/5 transition-colors border border-transparent hover:border-text/10" 
                :class="[
                  slotProps.data.currentStock <= 0 
                    ? 'text-red-400 font-black' 
                    : slotProps.data.currentStock <= slotProps.data.lowStockThreshold 
                      ? 'text-amber-400 font-black' 
                      : 'text-emerald-400 font-bold'
                ]"
                @click="openQuickStock(slotProps.data)"
                :title="$t('inventory.view.quickStockTitle')"
              >
                <span>{{ slotProps.data.currentStock }} {{ $t('inventory.units.short.pcs') }}</span>
                <i class="pi pi-pencil text-xs opacity-50"></i>
              </div>
            </template>
          </Column>

          <Column :header="$t('inventory.view.columns.actions')" headerClass="text-right" bodyClass="text-right">
            <template #body="slotProps">
              <button 
                @click="editProduct(slotProps.data)"
                class="w-8 h-8 rounded-lg bg-blue-500/10 text-blue-400 hover:bg-blue-500 hover:text-white transition-colors inline-flex items-center justify-center"
                :title="$t('inventory.view.editProductTitle')"
              >
                <i class="pi pi-pencil text-xs"></i>
              </button>
            </template>
          </Column>
        </DataTable>
      </div>

      <!-- Modális ablakok -->
      <ProductDialog 
        v-model:visible="productDialogVisible" 
        :productData="selectedProduct" 
        @saved="fetchProducts" 
      />
      <StockAdjustmentDialog 
        v-model:visible="stockDialogVisible" 
        :products="products"
        @saved="fetchProducts" 
      />
      <QuickStockDialog
        v-model:visible="quickStockVisible"
        :product="selectedProductForStock"
        @saved="fetchProducts" 
      />
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Badge from 'primevue/badge';

import productApi from '@/services/productApi';
import ProductDialog from './components/ProductDialog.vue';
import StockAdjustmentDialog from './components/StockAdjustmentDialog.vue';
import QuickStockDialog from './components/QuickStockDialog.vue';
import { useI18n } from 'vue-i18n';

const { t } = useI18n();

const products = ref([]);
const loading = ref(false);
const searchQuery = ref('');
const activeFilter = ref('all'); // 'all' | 'lowStock' | 'professional' | 'retail'
const viewMode = ref('cards'); // 'cards' alapértelmezetten (mobilon tökéletes)

const productDialogVisible = ref(false);
const stockDialogVisible = ref(false);
const quickStockVisible = ref(false);

const selectedProduct = ref(null);
const selectedProductForStock = ref(null);

const fetchProducts = async () => {
  loading.value = true;
  try {
    const res = await productApi.getAllProducts();
    products.value = res.data || [];
  } catch (error) {
    console.error('Hiba a termékek lekérésekor:', error);
  } finally {
    loading.value = false;
  }
};

const lowStockCount = computed(() => {
  return products.value.filter(p => p.currentStock <= p.lowStockThreshold).length;
});

const professionalCount = computed(() => {
  return products.value.filter(p => p.isProfessional).length;
});

const retailCount = computed(() => {
  return products.value.filter(p => p.isRetail).length;
});

const filteredProducts = computed(() => {
  let list = products.value;

  // Szűrés kategória/állapot szerint
  if (activeFilter.value === 'lowStock') {
    list = list.filter(p => p.currentStock <= p.lowStockThreshold);
  } else if (activeFilter.value === 'professional') {
    list = list.filter(p => p.isProfessional);
  } else if (activeFilter.value === 'retail') {
    list = list.filter(p => p.isRetail);
  }

  // Szűrés keresőszöveg szerint (Név, Árnyalat/Kód vagy EAN)
  if (searchQuery.value.trim()) {
    const q = searchQuery.value.trim().toLowerCase();
    list = list.filter(p => 
      (p.name && p.name.toLowerCase().includes(q)) ||
      (p.shade && p.shade.toLowerCase().includes(q)) ||
      (p.ean && p.ean.toLowerCase().includes(q))
    );
  }

  return list;
});

const resetFilters = () => {
  searchQuery.value = '';
  activeFilter.value = 'all';
};

const getUnitName = (unitEnum) => {
  const units = [
    t('inventory.units.short.ml'),
    t('inventory.units.short.g'),
    t('inventory.units.short.pcs'),
    t('inventory.units.short.m'),
    t('inventory.units.short.cm')
  ];
  return units[unitEnum] || '';
};

const formatCurrency = (val) => {
  if (val == null || isNaN(val)) return '-';
  return `${Number(val).toFixed(2)} €`;
};

const openProductDialog = () => {
  selectedProduct.value = null;
  productDialogVisible.value = true;
};

const editProduct = (product) => {
  selectedProduct.value = { ...product };
  productDialogVisible.value = true;
};

const openStockDialog = () => {
  stockDialogVisible.value = true;
};

const openQuickStock = (product) => {
  selectedProductForStock.value = { ...product };
  quickStockVisible.value = true;
};

onMounted(() => {
  fetchProducts();
});
</script>
