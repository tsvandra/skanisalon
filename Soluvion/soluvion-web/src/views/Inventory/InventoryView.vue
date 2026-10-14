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

        <div class="grid grid-cols-2 sm:grid-cols-3 lg:flex items-center gap-2 w-full md:w-auto">
          <button 
            @click="openAiScanner" 
            class="h-[44px] px-2.5 sm:px-3.5 bg-primary/15 text-primary border border-primary/30 font-bold text-xs sm:text-sm rounded-xl shadow-2xs hover:bg-primary hover:text-white active:scale-95 transition-all flex items-center justify-center gap-1.5 whitespace-nowrap"
            :title="$t('inventory.view.aiScan')"
          >
            <i class="pi pi-sparkles text-xs sm:text-sm"></i>
            <span class="truncate">{{ $t('inventory.view.aiScan') }}</span>
          </button>

          <button 
            @click="openDeliveryScanner" 
            class="h-[44px] px-2.5 sm:px-3.5 bg-sky-500/15 text-sky-400 border border-sky-500/30 font-bold text-xs sm:text-sm rounded-xl shadow-2xs hover:bg-sky-500 hover:text-white active:scale-95 transition-all flex items-center justify-center gap-1.5 whitespace-nowrap"
            :title="$t('inventory.view.deliveryScan')"
          >
            <i class="pi pi-file-arrow-up text-xs sm:text-sm"></i>
            <span class="truncate">{{ $t('inventory.view.deliveryScan') }}</span>
          </button>

          <button 
            @click="openDocumentsList" 
            class="h-[44px] px-2.5 sm:px-3.5 bg-surface text-text border border-text/20 font-bold text-xs sm:text-sm rounded-xl shadow-sm hover:border-primary hover:text-primary active:scale-95 transition-all flex items-center justify-center gap-1.5 whitespace-nowrap"
            :title="$t('inventory.view.documentsTitle')"
          >
            <i class="pi pi-folder-open text-xs sm:text-sm"></i>
            <span class="truncate">{{ $t('inventory.view.documentsBtn') }}</span>
          </button>

          <button 
            @click="openStockDialog" 
            class="h-[44px] px-2.5 sm:px-3.5 bg-surface text-text border border-text/20 font-bold text-xs sm:text-sm rounded-xl shadow-sm hover:border-primary hover:text-primary active:scale-95 transition-all flex items-center justify-center gap-1.5 whitespace-nowrap"
          >
            <i class="pi pi-arrow-right-arrow-left text-xs sm:text-sm"></i>
            <span class="truncate">{{ $t('inventory.view.stockMovement') }}</span>
          </button>
          
          <button 
            @click="openProductDialog" 
            class="col-span-2 sm:col-span-1 h-[44px] px-2.5 sm:px-4 bg-primary text-white font-bold text-xs sm:text-sm rounded-xl shadow-md hover:brightness-110 active:scale-95 transition-all flex items-center justify-center gap-1.5 whitespace-nowrap"
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

      <!-- Frissen módosított tételek információs sáv -->
      <div v-if="hasRecentlyReceived" class="p-3 sm:p-4 rounded-2xl bg-surface border border-text/15 flex items-center justify-between gap-3 shadow-xs">
        <div class="flex items-center gap-2.5 min-w-0">
          <span class="w-8 h-8 rounded-xl bg-primary/10 text-primary border border-primary/20 flex items-center justify-center shrink-0">
            <i class="pi pi-history text-sm"></i>
          </span>
          <div class="min-w-0">
            <div class="font-bold text-xs sm:text-sm text-text truncate">
              {{ $t('inventory.view.recentlyModified') }} ({{ recentlyReceivedCount }})
            </div>
            <div class="text-[11px] text-text-muted truncate">
              {{ $t('inventory.view.recentlyModifiedHint') }}
            </div>
          </div>
        </div>
        <button 
          type="button" 
          @click="clearRecentlyReceived" 
          class="px-3.5 py-2 rounded-xl text-xs font-bold bg-emerald-500/15 hover:bg-emerald-500/25 text-emerald-400 border border-emerald-500/40 transition-all whitespace-nowrap active:scale-95 shrink-0 inline-flex items-center gap-1.5 shadow-2xs"
        >
          <i class="pi pi-check text-xs"></i>
          <span>{{ $t('inventory.view.clearRecentlyReceived') }}</span>
        </button>
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
          {{ $t('inventory.view.resetFilters') }}
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
          class="rounded-xl p-3 sm:p-3.5 shadow-2xs border transition-all flex items-center justify-between gap-3 group"
          :class="[
            isRecentReceipt(product.id)
              ? 'bg-purple-500/5 border-purple-500/40 ring-1 ring-purple-500/30'
              : isRecentIssue(product.id)
                ? 'bg-amber-500/5 border-amber-500/40 ring-1 ring-amber-500/30'
                : 'bg-surface border-text/10 hover:border-primary/40'
          ]"
        >
          <!-- Termékkép miniatűr (kattintásra lightbox-ban kinyílik) -->
          <div 
            v-if="product.imageUrl" 
            class="relative w-12 h-12 rounded-xl overflow-hidden border border-text/15 bg-background shrink-0 cursor-pointer hover:opacity-90 group/img transition-all shadow-2xs"
            @click.stop="openImagePreview(product)"
            :title="$t('inventory.view.previewImageTitle')"
          >
            <img :src="product.imageUrl" class="w-full h-full object-cover group-hover/img:scale-110 transition-transform duration-300" alt="Termékkép" />
            <div class="absolute inset-0 bg-black/30 opacity-0 group-hover/img:opacity-100 transition-opacity flex items-center justify-center text-white text-xs">
              <i class="pi pi-search-plus"></i>
            </div>
          </div>
          <div 
            v-else 
            class="w-12 h-12 rounded-xl border border-text/10 bg-background/50 flex items-center justify-center shrink-0 text-text-muted/30"
          >
            <i class="pi pi-image text-lg"></i>
          </div>

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
              <!-- Frissen bevételezett badge (lila) -->
              <span 
                v-if="isRecentReceipt(product.id)" 
                class="text-[10px] bg-purple-500/20 text-purple-300 px-1.5 py-0.2 rounded font-bold border border-purple-500/40 shrink-0 flex items-center gap-1"
              >
                ✨ {{ $t('inventory.view.recentlyReceivedBadge') }}
              </span>

              <!-- Frissen levett badge (sárgásbarna) -->
              <span 
                v-else-if="isRecentIssue(product.id)" 
                class="text-[10px] bg-amber-500/20 text-amber-300 px-1.5 py-0.2 rounded font-bold border border-amber-500/40 shrink-0 flex items-center gap-1"
              >
                📉 {{ $t('inventory.view.recentlyIssuedBadge') }}
              </span>

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

          <!-- Jobb oldal: Előzmények (História) gomb + Érintésbarát Készlet szám -->
          <div class="flex items-center gap-1.5 shrink-0">
            <button
              type="button"
              @click.stop="openProductHistory(product)"
              class="w-10 h-10 rounded-xl bg-background border border-text/15 text-text-muted hover:text-primary hover:border-primary/40 active:scale-95 transition-all flex items-center justify-center shadow-2xs"
              :title="$t('inventory.view.historyTitle')"
            >
              <i class="pi pi-history text-sm"></i>
            </button>

            <button 
              @click.stop="openQuickStock(product)"
              class="h-10 px-3 rounded-xl border transition-all flex items-center gap-1.5 active:scale-95 shadow-2xs"
              :class="[
                isRecentReceipt(product.id)
                  ? 'bg-purple-500/20 border-purple-500/50 text-purple-300 hover:bg-purple-500/30'
                  : isRecentIssue(product.id)
                    ? 'bg-amber-500/20 border-amber-500/50 text-amber-300 hover:bg-amber-500/30'
                    : product.currentStock <= 0 
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
              <span 
                v-if="isRecentReceipt(product.id)" 
                class="text-xs font-black text-purple-300"
              >
                (+{{ getRecentQty(product.id) }})
              </span>
              <span 
                v-else-if="isRecentIssue(product.id)" 
                class="text-xs font-black text-amber-300"
              >
                (-{{ getRecentQty(product.id) }})
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
              <div class="flex items-center gap-2.5">
                <div 
                  v-if="slotProps.data.imageUrl" 
                  class="w-9 h-9 rounded-lg overflow-hidden border border-text/15 bg-background shrink-0 cursor-pointer hover:opacity-80"
                  @click.stop="openImagePreview(slotProps.data)"
                  :title="$t('inventory.view.previewImageTitle')"
                >
                  <img :src="slotProps.data.imageUrl" class="w-full h-full object-cover" alt="Kép" />
                </div>
                <div v-else class="w-9 h-9 rounded-lg border border-text/10 bg-background/40 flex items-center justify-center shrink-0 text-text-muted/30">
                  <i class="pi pi-image text-xs"></i>
                </div>

                <div class="flex items-center gap-2 flex-wrap">
                  <span 
                    class="cursor-pointer text-primary font-bold hover:underline" 
                    @click="editProduct(slotProps.data)" 
                    :title="$t('inventory.view.editProductTitle')"
                  >
                    {{ slotProps.data.name }}
                  </span>
                  <span 
                    v-if="recentlyReceivedMap[slotProps.data.id]"
                    class="text-[10px] font-bold px-1.5 py-0.2 rounded bg-purple-500/20 text-purple-300 border border-purple-500/40 shrink-0"
                  >
                    ✨ {{ $t('inventory.view.recentlyReceivedBadge') }}
                  </span>
                  <span 
                    v-if="slotProps.data.shade" 
                    class="text-[11px] font-black bg-primary/15 text-primary border border-primary/30 px-1.5 py-0.2 rounded-md shrink-0"
                  >
                    {{ slotProps.data.shade }}
                  </span>
                </div>
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
                  isRecentReceipt(slotProps.data.id)
                    ? 'text-purple-300 font-black'
                    : isRecentIssue(slotProps.data.id)
                      ? 'text-amber-300 font-black'
                      : slotProps.data.currentStock <= 0 
                        ? 'text-red-400 font-black' 
                        : slotProps.data.currentStock <= slotProps.data.lowStockThreshold 
                          ? 'text-amber-400 font-black' 
                          : 'text-emerald-400 font-bold'
                ]"
                @click="openQuickStock(slotProps.data)"
                :title="$t('inventory.view.quickStockTitle')"
              >
                <span>{{ slotProps.data.currentStock }}</span>
                <span 
                  v-if="isRecentReceipt(slotProps.data.id)"
                  class="text-xs font-bold text-purple-300 bg-purple-500/20 border border-purple-500/40 px-1.5 py-0.5 rounded-md"
                >
                  (+{{ getRecentQty(slotProps.data.id) }})
                </span>
                <span 
                  v-else-if="isRecentIssue(slotProps.data.id)"
                  class="text-xs font-bold text-amber-300 bg-amber-500/20 border border-amber-500/40 px-1.5 py-0.5 rounded-md"
                >
                  (-{{ getRecentQty(slotProps.data.id) }})
                </span>
                <span class="text-xs opacity-75">{{ $t('inventory.units.short.pcs') }}</span>
                <i class="pi pi-pencil text-xs opacity-50"></i>
              </div>
            </template>
          </Column>

          <Column :header="$t('inventory.view.columns.actions')" headerClass="text-right" bodyClass="text-right">
            <template #body="slotProps">
              <div class="inline-flex items-center justify-end gap-1.5">
                <button 
                  @click="openProductHistory(slotProps.data)"
                  class="h-8 px-2.5 rounded-lg bg-primary/10 text-primary hover:bg-primary hover:text-white transition-colors inline-flex items-center justify-center gap-1.5 text-xs font-bold"
                  :title="$t('inventory.view.historyTitle')"
                >
                  <i class="pi pi-history text-xs"></i>
                  <span class="hidden xl:inline">{{ $t('inventory.view.historyBtn') }}</span>
                </button>
                <button 
                  @click="editProduct(slotProps.data)"
                  class="w-8 h-8 rounded-lg bg-blue-500/10 text-blue-400 hover:bg-blue-500 hover:text-white transition-colors inline-flex items-center justify-center"
                  :title="$t('inventory.view.editProductTitle')"
                >
                  <i class="pi pi-pencil text-xs"></i>
                </button>
              </div>
            </template>
          </Column>
        </DataTable>
      </div>

      <!-- Modális ablakok -->
      <ProductDialog 
        v-model:visible="productDialogVisible" 
        :productData="selectedProduct" 
        @saved="onProductSaved" 
      />
      <StockAdjustmentDialog 
        v-model:visible="stockDialogVisible" 
        :products="products"
        @saved="onStockAdjustmentSaved" 
      />
      <QuickStockDialog
        v-model:visible="quickStockVisible"
        :product="selectedProductForStock"
        @saved="onQuickStockSaved" 
      />
      <AiProductScannerModal 
        v-model:visible="aiScannerVisible" 
        @scanned="onAiScanned" 
      />
      <DeliveryNoteScannerModal
        v-model:visible="deliveryScannerVisible"
        :products="products"
        @saved="onDeliveryNoteSaved"
      />
      <ProductHistoryModal
        ref="productHistoryModalRef"
        v-model:visible="productHistoryVisible"
        :product="selectedProductForHistory"
        @open-document="onOpenDocumentFromHistory"
      />
      <InventoryDocumentModal
        v-model:visible="documentModalVisible"
        :documentId="selectedDocumentId"
        :highlightProductId="highlightProductIdInDoc"
        @note-updated="onDocumentNoteUpdated"
        @open-product-history="openProductHistoryFromDoc"
      />
      <InventoryDocumentsListModal
        ref="documentsListModalRef"
        v-model:visible="documentsListVisible"
        @open-document="onOpenDocumentFromHistory"
      />

      <!-- Nagyított termékkép megtekintése (Lightbox) -->
      <Dialog 
        v-model:visible="imagePreviewVisible" 
        :modal="true" 
        :dismissableMask="true"
        :style="{ width: '92vw', maxWidth: '540px' }"
        :pt="{
          root: { class: 'bg-surface border border-text/10 rounded-2xl overflow-hidden shadow-2xl' },
          header: { class: 'px-4 py-3 border-b border-text/10 bg-background/50' },
          content: { class: 'p-3 sm:p-4 bg-background flex flex-col items-center justify-center' },
          closeButton: { class: 'hover:bg-text/10 p-2 rounded-full transition-colors w-8 h-8 flex items-center justify-center text-text-muted' }
        }"
      >
        <template #header>
          <div class="min-w-0 pr-4">
            <h3 class="font-bold text-sm sm:text-base text-text truncate">{{ previewProduct?.name }}</h3>
            <p v-if="previewProduct?.shade" class="text-xs font-mono text-primary font-bold">[{{ previewProduct.shade }}]</p>
          </div>
        </template>
        <div class="w-full max-h-[70vh] flex items-center justify-center rounded-xl overflow-hidden bg-black/20">
          <img 
            v-if="previewProduct?.imageUrl" 
            :src="previewProduct.imageUrl" 
            class="max-w-full max-h-[68vh] object-contain rounded-lg shadow-lg"
            :alt="previewProduct?.name"
          />
        </div>
      </Dialog>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Badge from 'primevue/badge';
import Dialog from 'primevue/dialog';

import productApi from '@/services/productApi';
import ProductDialog from './components/ProductDialog.vue';
import StockAdjustmentDialog from './components/StockAdjustmentDialog.vue';
import QuickStockDialog from './components/QuickStockDialog.vue';
import AiProductScannerModal from './components/AiProductScannerModal.vue';
import DeliveryNoteScannerModal from './components/DeliveryNoteScannerModal.vue';
import ProductHistoryModal from './components/ProductHistoryModal.vue';
import InventoryDocumentModal from './components/InventoryDocumentModal.vue';
import InventoryDocumentsListModal from './components/InventoryDocumentsListModal.vue';
import { useI18n } from 'vue-i18n';
import { useToast } from 'primevue/usetoast';

const { t } = useI18n();
const toast = useToast();

const products = ref([]);
const loading = ref(false);
const searchQuery = ref('');
const activeFilter = ref('all'); // 'all' | 'lowStock' | 'professional' | 'retail'
const viewMode = ref('cards'); // 'cards' alapértelmezetten (mobilon tökéletes)

const productDialogVisible = ref(false);
const stockDialogVisible = ref(false);
const quickStockVisible = ref(false);
const aiScannerVisible = ref(false);
const deliveryScannerVisible = ref(false);
const imagePreviewVisible = ref(false);
const previewProduct = ref(null);

const productHistoryVisible = ref(false);
const selectedProductForHistory = ref(null);
const productHistoryModalRef = ref(null);

const documentModalVisible = ref(false);
const selectedDocumentId = ref(null);
const highlightProductIdInDoc = ref(null);

const documentsListVisible = ref(false);
const documentsListModalRef = ref(null);

const selectedProduct = ref(null);
const selectedProductForStock = ref(null);

const RECENT_STORAGE_KEY = 'soluvion_recent_receipts';

const cleanStr = (s) => (s ? String(s).trim().toLowerCase() : '');

const loadRecentlyReceived = () => {
  try {
    const raw = sessionStorage.getItem(RECENT_STORAGE_KEY);
    if (raw) {
      const parsed = JSON.parse(raw);
      const normalized = {};
      for (const [k, val] of Object.entries(parsed)) {
        const qty = Number(val?.quantity ?? val?.quantityAdded) || 0;
        if (qty > 0) {
          normalized[String(k)] = {
            type: val.type === 'issue' ? 'issue' : 'receipt',
            quantity: qty,
            originalStock: val.originalStock !== undefined ? Number(val.originalStock) : undefined,
            isNewProduct: !!val.isNewProduct
          };
        }
      }
      return normalized;
    }
  } catch (e) {
    console.warn('Nem sikerült betölteni a korábbi bevételezéseket:', e);
  }
  return {};
};

const recentlyReceivedMap = ref(loadRecentlyReceived());
const hasRecentlyReceived = computed(() => Object.keys(recentlyReceivedMap.value).length > 0);
const recentlyReceivedCount = computed(() => Object.keys(recentlyReceivedMap.value).length);

const getRecentItem = (productId) => {
  if (productId == null) return null;
  return recentlyReceivedMap.value[String(productId)] || recentlyReceivedMap.value[productId] || null;
};
const isRecent = (productId) => !!getRecentItem(productId);
const isRecentReceipt = (productId) => {
  const item = getRecentItem(productId);
  return !!item && item.type !== 'issue';
};
const isRecentIssue = (productId) => {
  const item = getRecentItem(productId);
  return !!item && item.type === 'issue';
};
const getRecentQty = (productId) => {
  const item = getRecentItem(productId);
  if (!item) return 0;
  return Number(item.quantity ?? item.quantityAdded) || 0;
};

const saveRecentlyReceived = (map) => {
  recentlyReceivedMap.value = map;
  try {
    if (Object.keys(map).length > 0) {
      sessionStorage.setItem(RECENT_STORAGE_KEY, JSON.stringify(map));
    } else {
      sessionStorage.removeItem(RECENT_STORAGE_KEY);
    }
  } catch (e) {
    console.warn('Nem sikerült menteni a sessionStorage-ba:', e);
  }
};

const clearRecentlyReceived = () => {
  saveRecentlyReceived({});
};

// Kiszámítja a termék munkamenet eleji nyitókészletéhez (originalStock) viszonyított nettó változást:
const applySessionDelta = (map, productId, previousStock, newStock, isNewProduct = false) => {
  if (productId == null) return;
  const key = String(productId);
  const existing = map[key];

  let originalStock;
  if (existing) {
    if (existing.originalStock !== undefined) {
      originalStock = Number(existing.originalStock);
    } else {
      const prevQty = Number(existing.quantity ?? existing.quantityAdded) || 0;
      originalStock = existing.type === 'issue'
        ? Number(previousStock) + prevQty
        : Math.max(0, Number(previousStock) - prevQty);
    }
  } else {
    originalStock = Number(previousStock);
  }

  const current = Number(newStock);
  const netDiff = current - originalStock;

  if (netDiff > 0) {
    map[key] = {
      type: 'receipt',
      quantity: netDiff,
      originalStock: originalStock,
      isNewProduct: isNewProduct || !!existing?.isNewProduct
    };
  } else if (netDiff < 0) {
    map[key] = {
      type: 'issue',
      quantity: Math.abs(netDiff),
      originalStock: originalStock,
      isNewProduct: false
    };
  } else {
    // Ha a készlet visszatért pontosan a munkamenet eleji nyitókészletre (netDiff === 0), levesszük a kiemelést
    delete map[key];
  }
};

const recordRecentAddition = (productId, quantity, isNewProduct = false) => {
  if (!productId || quantity <= 0) return;
  const map = { ...recentlyReceivedMap.value };
  const found = products.value.find(p => String(p.id) === String(productId));
  const currentStock = found ? Number(found.currentStock) || 0 : Number(quantity);
  const previousStock = isNewProduct ? 0 : Math.max(0, currentStock - Number(quantity));
  applySessionDelta(map, productId, previousStock, currentStock, isNewProduct);
  saveRecentlyReceived(map);
};

const onDeliveryNoteSaved = async (resultData) => {
  await fetchProducts(false);
  const items = resultData?.receivedItems || [];
  if (items.length > 0) {
    const map = { ...recentlyReceivedMap.value };
    items.forEach(item => {
      let prod = item.productId
        ? products.value.find(p => String(p.id) === String(item.productId))
        : null;

      if (!prod && item.name) {
        prod = products.value.find(p => 
          cleanStr(p.name) === cleanStr(item.name) && 
          (cleanStr(p.shade) === cleanStr(item.shade) || (!p.shade && !item.shade))
        );
      }

      const qty = Number(item.quantityAdded ?? item.quantity) || 0;
      if (prod && qty > 0) {
        const currentStock = Number(prod.currentStock) || 0;
        const previousStock = item.isNewProduct ? 0 : Math.max(0, currentStock - qty);
        applySessionDelta(map, prod.id, previousStock, currentStock, !!item.isNewProduct);
      }
    });
    saveRecentlyReceived(map);
  }
};

const onStockAdjustmentSaved = async (resultData) => {
  const map = { ...recentlyReceivedMap.value };
  if (resultData?.type === 0 && resultData?.receivedItems?.length) {
    resultData.receivedItems.forEach(item => {
      const p = products.value.find(prod => String(prod.id) === String(item.productId));
      const qty = Number(item.quantityAdded) || 0;
      if (p && qty > 0) {
        const prevStock = Number(p.currentStock) || 0;
        const nextStock = prevStock + qty;
        p.currentStock = nextStock;
        applySessionDelta(map, p.id, prevStock, nextStock, false);
      }
    });
    saveRecentlyReceived(map);
  } else if (resultData?.type === 1 && resultData?.items?.length) {
    resultData.items.forEach(item => {
      const p = products.value.find(prod => String(prod.id) === String(item.productId));
      const qty = Number(item.quantityRemoved) || 0;
      if (p && qty > 0) {
        const prevStock = Number(p.currentStock) || 0;
        const nextStock = Math.max(0, prevStock - qty);
        p.currentStock = nextStock;
        applySessionDelta(map, p.id, prevStock, nextStock, false);
      }
    });
    saveRecentlyReceived(map);
  }
};

const onStockAdjustmentRollback = async () => {
  await fetchProducts(true);
  toast.add({
    severity: 'error',
    summary: t('common.error') || 'Hiba',
    detail: t('inventory.stockDialog.saveError') || 'Nem sikerült menteni a készletmozgást.',
    life: 4000
  });
};

const onQuickStockSaved = (result) => {
  if (!result || !result.productId) return;

  // 1. Azonnali helyi készletfrissítés a reaktív listában (oldal- és listafrissítés nélkül):
  const found = products.value.find(p => String(p.id) === String(result.productId));
  if (found && result.newStock !== undefined) {
    found.currentStock = result.newStock;
  }

  // 2. Nettó változás kiszámítása a munkamenet eleji nyitókészlethez képest:
  const map = { ...recentlyReceivedMap.value };
  applySessionDelta(map, result.productId, result.previousStock, result.newStock, false);
  saveRecentlyReceived(map);
};

const onQuickStockRollback = (payload) => {
  if (!payload || !payload.productId) return;
  const found = products.value.find(p => String(p.id) === String(payload.productId));
  if (found && payload.previousStock !== undefined) {
    found.currentStock = payload.previousStock;
  }
  toast.add({
    severity: 'error',
    summary: t('common.error') || 'Hiba',
    detail: t('inventory.quickStockDialog.saveError') || 'Nem sikerült menteni a készletmódosítást.',
    life: 4000
  });
};

const onProductSaved = async (result) => {
  await fetchProducts(false);
  if (result?.isNew && result?.product?.id && result?.initialStock > 0) {
    recordRecentAddition(result.product.id, result.initialStock, true);
  }
};

const fetchProducts = async (showLoading = false) => {
  // Csak akkor mutatunk teljes oldalas spinnert, ha a lista még üres (pl. legelső betöltés)
  // vagy ha kifejezetten kértük (showLoading === true)
  if (showLoading || products.value.length === 0) {
    loading.value = true;
  }
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
  let list = [...products.value];

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

  // Ha vannak a munkamenetben módosított tételek, azokat a lista elejére rendezzük
  if (hasRecentlyReceived.value) {
    list.sort((a, b) => {
      const aIsRecent = isRecent(a.id);
      const bIsRecent = isRecent(b.id);
      if (aIsRecent && !bIsRecent) return -1;
      if (!aIsRecent && bIsRecent) return 1;
      return 0;
    });
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

const openAiScanner = () => {
  aiScannerVisible.value = true;
};

const openDeliveryScanner = () => {
  deliveryScannerVisible.value = true;
};

const openImagePreview = (product) => {
  previewProduct.value = product;
  imagePreviewVisible.value = true;
};

const onAiScanned = (scannedData) => {
  selectedProduct.value = {
    name: scannedData.name || '',
    shade: scannedData.shade || '',
    ean: scannedData.ean || '',
    imageUrl: scannedData.imageUrl || null,
    packageSize: scannedData.packageSize || 1,
    unit: scannedData.unit ?? 0,
    isProfessional: scannedData.isProfessional ?? true,
    isRetail: scannedData.isRetail ?? false,
    costPrice: 0,
    retailPrice: scannedData.estimatedPrice || 0,
    lowStockThreshold: 0,
    initialStock: 1
  };
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

const openProductHistory = (product) => {
  selectedProductForHistory.value = { ...product };
  productHistoryVisible.value = true;
};

const openProductHistoryFromDoc = (productSummary) => {
  const fullProd = products.value.find(p => String(p.id) === String(productSummary.id));
  selectedProductForHistory.value = fullProd ? { ...fullProd } : { ...productSummary };
  documentModalVisible.value = false;
  productHistoryVisible.value = true;
};

const openDocumentsList = () => {
  documentsListVisible.value = true;
};

const onOpenDocumentFromHistory = ({ documentId, highlightProductId }) => {
  selectedDocumentId.value = documentId;
  highlightProductIdInDoc.value = highlightProductId || null;
  documentModalVisible.value = true;
};

const onDocumentNoteUpdated = () => {
  if (productHistoryVisible.value && productHistoryModalRef.value?.refresh) {
    productHistoryModalRef.value.refresh();
  }
  if (documentsListVisible.value && documentsListModalRef.value?.refresh) {
    documentsListModalRef.value.refresh();
  }
};

onMounted(() => {
  fetchProducts();
});
</script>
