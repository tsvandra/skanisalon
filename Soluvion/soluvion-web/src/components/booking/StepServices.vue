<template>
  <div class="flex flex-col h-full">
    <div v-if="loading" class="text-center py-16 text-text-muted">
      <i class="pi pi-spin pi-spinner text-4xl mb-3 block text-primary"></i>
      <span class="font-medium">{{ $t('common.loading') }}</span>
    </div>

    <div v-else-if="categories.length === 0" class="text-center py-16 text-text-muted border border-dashed border-text/15 rounded-2xl bg-background/40">
      <i class="pi pi-Sparkles text-4xl mb-3 block text-primary/60"></i>
      <p class="font-medium">{{ $t('booking.noBookableServices') }}</p>
    </div>

    <div v-else class="flex flex-col gap-6">
      <!-- 1. BENTO KATEGÓRIA CSEMPÉK NAGY PIKTOGRAMOKKAL -->
      <div>
        <div class="flex items-center justify-between mb-3">
          <h3 class="text-xs font-bold uppercase tracking-widest text-text-muted">
            {{ $t('booking.categoriesTitle') }}
          </h3>
          <span v-if="selectedVariants.length > 0" class="text-xs font-bold text-primary bg-primary/10 px-2.5 py-1 rounded-full">
            {{ $t('booking.selectedCount', { count: selectedVariants.length }) }}
          </span>
        </div>

        <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3">
          <button v-for="group in categories"
                  :key="group.id"
                  type="button"
                  @click="selectCategory(group.id)"
                  class="group relative flex flex-col items-start justify-between p-4 rounded-2xl border text-left transition-all duration-300 cursor-pointer min-h-[116px] overflow-hidden"
                  :class="activeCategory === group.id
                    ? 'bg-primary/10 border-primary shadow-md ring-1 ring-primary/40 -translate-y-0.5'
                    : 'bg-background/80 border-text/10 hover:border-primary/40 hover:bg-background'">

            <!-- Háttér dekoratív nagy piktogram vízjel -->
            <div class="absolute -right-3 -bottom-3 opacity-[0.06] group-hover:opacity-[0.12] transition-opacity pointer-events-none text-primary">
              <ServicePictogram :svg="group.categoryPictogram" sizeClass="w-20 h-20" />
            </div>

            <div class="w-full flex items-start justify-between gap-2 mb-3 relative z-10">
              <div class="w-11 h-11 rounded-xl flex items-center justify-center transition-all duration-300 shrink-0"
                   :class="activeCategory === group.id
                     ? 'bg-primary text-surface shadow-sm scale-105'
                     : 'bg-primary/10 text-primary group-hover:bg-primary/20'">
                <ServicePictogram :svg="group.categoryPictogram" sizeClass="w-6 h-6" />
              </div>

              <span v-if="getSelectedCount(group) > 0"
                    class="bg-primary text-surface text-xs font-extrabold px-2.5 py-0.5 rounded-full shadow-sm">
                {{ getSelectedCount(group) }}
              </span>
            </div>

            <div class="relative z-10 w-full">
              <div class="font-bold text-sm md:text-base leading-snug line-clamp-2 transition-colors"
                   :class="activeCategory === group.id ? 'text-primary' : 'text-text'">
                {{ getLocText(group.categoryName) }}
              </div>
              <div class="text-[11px] text-text-muted mt-0.5 font-medium">
                {{ $t('booking.serviceCount', { count: group.items.length }) }}
              </div>
            </div>
          </button>
        </div>
      </div>

      <!-- 2. AKTÍV KATEGÓRIA SZOLGÁLTATÁSAI ÉS VARIÁNS KÁRTYÁI -->
      <div v-if="activeGroup" class="space-y-3 transition-all duration-300">
        <div class="flex items-center gap-2.5 pb-2 border-b border-text/10">
          <div class="w-8 h-8 rounded-lg bg-primary/10 text-primary flex items-center justify-center shrink-0">
            <ServicePictogram :svg="activeGroup.categoryPictogram" sizeClass="w-4 h-4" />
          </div>
          <h4 class="font-bold text-lg text-text tracking-wide m-0">
            {{ getLocText(activeGroup.categoryName) }}
          </h4>
        </div>

        <div class="space-y-3 max-h-[460px] overflow-y-auto pr-1 [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-primary/30 [&::-webkit-scrollbar-thumb]:rounded-full">
          <div v-for="service in activeGroup.items"
               :key="service.id"
               class="p-4 rounded-2xl border transition-all duration-200"
               :class="isServiceSelected(service)
                 ? 'bg-primary/5 border-primary/40 shadow-sm'
                 : 'bg-background/70 border-text/10 hover:border-text/20'">

            <div class="flex items-start gap-3 mb-3">
              <div class="w-9 h-9 rounded-xl flex items-center justify-center shrink-0 mt-0.5 transition-colors"
                   :class="isServiceSelected(service) ? 'bg-primary text-surface' : 'bg-primary/10 text-primary'">
                <ServicePictogram :svg="service.pictogramLink || activeGroup.categoryPictogram" sizeClass="w-5 h-5" />
              </div>

              <div class="flex-1 min-w-0">
                <div class="font-bold text-base text-text leading-snug">
                  {{ getLocText(service.name) }}
                </div>
                <p v-if="getLocText(service.description, '')"
                   class="text-xs text-text-muted italic mt-1 leading-relaxed m-0">
                  {{ getLocText(service.description, '') }}
                </p>
              </div>
            </div>

            <!-- Variáns választó kártyák -->
            <div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-2.5 pl-0 sm:pl-12">
              <label v-for="variant in service.variants"
                     :key="variant.id"
                     class="group/var relative flex items-center justify-between gap-2 px-3.5 py-2.5 rounded-xl border cursor-pointer transition-all duration-200 min-h-[48px]"
                     :class="selectedVariants.includes(variant.id)
                       ? 'bg-primary/15 border-primary ring-1 ring-primary/40 shadow-sm'
                       : 'bg-surface border-text/10 hover:border-primary/40'">

                <input type="checkbox"
                       :value="variant.id"
                       :checked="selectedVariants.includes(variant.id)"
                       @change="toggleVariant(variant.id)"
                       class="hidden" />

                <div class="flex items-center gap-2.5 min-w-0">
                  <div class="w-5 h-5 rounded-md border flex items-center justify-center shrink-0 transition-colors"
                       :class="selectedVariants.includes(variant.id)
                         ? 'bg-primary border-primary text-surface'
                         : 'border-text/25 bg-background group-hover/var:border-primary/60'">
                    <i v-if="selectedVariants.includes(variant.id)" class="pi pi-check text-[10px] font-bold"></i>
                  </div>

                  <div class="flex flex-col min-w-0">
                    <span class="font-semibold text-sm truncate transition-colors"
                          :class="selectedVariants.includes(variant.id) ? 'text-primary' : 'text-text'">
                      {{ getLocText(variant.variantName) }}
                    </span>
                    <span v-if="variant.duration" class="text-[11px] text-text-muted flex items-center gap-1">
                      <i class="pi pi-clock text-[10px]"></i>
                      {{ $t('booking.durationMinutes', { min: variant.duration }) }}
                    </span>
                  </div>
                </div>

                <span class="text-sm font-extrabold shrink-0"
                      :class="selectedVariants.includes(variant.id) ? 'text-primary' : 'text-text/80'">
                  {{ formatCurrency(variant.price) }}
                </span>
              </label>
            </div>
          </div>
        </div>
      </div>

      <!-- 3. ÉLŐ FOGLALÁSI ÖSSZEGZŐ SÁV (BOUTIQUE CONCIERGE) -->
      <div class="pt-4 mt-2 border-t border-text/10 sticky bottom-0 bg-surface/95 backdrop-blur-md z-20">
        <!-- Kiválasztott szolgáltatás chipek -->
        <div v-if="selectedItemDetails.length > 0" class="mb-3">
          <div class="flex flex-wrap gap-1.5 max-h-[84px] overflow-y-auto pb-1">
            <div v-for="item in selectedItemDetails"
                 :key="item.variantId"
                 class="inline-flex items-center gap-1.5 pl-2.5 pr-1.5 py-1 rounded-full bg-primary/10 border border-primary/30 text-xs font-semibold text-text">
              <ServicePictogram :svg="item.pictogram" sizeClass="w-3.5 h-3.5" class="text-primary shrink-0" />
              <span class="truncate max-w-[180px] sm:max-w-[240px]">{{ item.serviceName }} · {{ item.variantName }}</span>
              <span v-if="item.price" class="text-primary font-bold ml-0.5">{{ formatCurrency(item.price) }}</span>
              <button type="button"
                      @click="toggleVariant(item.variantId)"
                      class="w-4 h-4 rounded-full hover:bg-primary/20 text-text-muted hover:text-text flex items-center justify-center ml-0.5 cursor-pointer border-none bg-transparent">
                <i class="pi pi-times text-[9px]"></i>
              </button>
            </div>
          </div>
        </div>

        <div class="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3">
          <div v-if="selectedItemDetails.length > 0" class="flex items-center justify-between sm:justify-start gap-5 px-1">
            <div>
              <div class="text-[11px] uppercase tracking-wider text-text-muted font-semibold">
                {{ $t('booking.estimatedDuration') }}
              </div>
              <div class="text-sm font-bold text-text flex items-center gap-1.5">
                <i class="pi pi-clock text-primary text-xs"></i>
                ~{{ $t('booking.durationMinutes', { min: totalDuration }) }}
              </div>
            </div>
            <div class="h-7 w-[1px] bg-text/10"></div>
            <div>
              <div class="text-[11px] uppercase tracking-wider text-text-muted font-semibold">
                {{ $t('booking.estimatedTotal') }}
              </div>
              <div class="text-lg font-extrabold text-primary">
                {{ formatCurrency(totalPrice) }}
              </div>
            </div>
          </div>
          <div v-else class="text-sm text-text-muted italic px-1">
            {{ $t('booking.noServiceSelectedYet') }}
          </div>

          <button type="button"
                  @click="onNext"
                  :disabled="selectedVariants.length === 0"
                  class="min-h-[52px] px-7 flex items-center justify-center gap-2.5 bg-primary text-surface font-bold text-base rounded-xl disabled:opacity-40 disabled:cursor-not-allowed transition-all hover:brightness-110 active:scale-95 shadow-md cursor-pointer shrink-0">
            <span>{{ $t('booking.next') }}</span>
            <span v-if="selectedVariants.length > 0" class="bg-surface/20 px-2 py-0.5 rounded-full text-xs">
              {{ selectedVariants.length }}
            </span>
            <i class="pi pi-arrow-right text-sm"></i>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, computed, watch, onMounted } from 'vue';
  import { useI18n } from 'vue-i18n';
  import { useLocalizedText } from '@/composables/useLocalizedText';
  import ServicePictogram from '@/components/common/ServicePictogram.vue';
  import bookingApi from '@/services/bookingApi';

  const props = defineProps({
    selectedVariants: { type: Array, required: true },
    onNext: { type: Function, required: true }
  });

  const emit = defineEmits(['update:selectedVariants', 'update:selectedItems']);
  const { locale } = useI18n();
  const { getLocText } = useLocalizedText();

  const loading = ref(true);
  const categories = ref([]);
  const activeCategory = ref(null);

  const activeGroup = computed(() => {
    return categories.value.find(g => g.id === activeCategory.value) || categories.value[0] || null;
  });

  const formatCurrency = (val) => {
    if (val == null || val === 0) return '';
    return val.toLocaleString(locale.value, { style: 'currency', currency: 'EUR', maximumFractionDigits: 0 });
  };

  const selectCategory = (categoryId) => {
    activeCategory.value = categoryId;
  };

  const isServiceSelected = (service) => {
    return (service.variants || []).some(v => props.selectedVariants.includes(v.id));
  };

  const getSelectedCount = (group) => {
    let count = 0;
    group.items.forEach(service => {
      (service.variants || []).forEach(variant => {
        if (props.selectedVariants.includes(variant.id)) count++;
      });
    });
    return count;
  };

  const selectedItemDetails = computed(() => {
    const items = [];
    categories.value.forEach(group => {
      group.items.forEach(service => {
        (service.variants || []).forEach(variant => {
          if (props.selectedVariants.includes(variant.id)) {
            items.push({
              variantId: variant.id,
              serviceName: getLocText(service.name),
              variantName: getLocText(variant.variantName),
              price: variant.price || 0,
              duration: variant.duration || 30,
              pictogram: service.pictogramLink || group.categoryPictogram || null
            });
          }
        });
      });
    });
    return items;
  });

  const totalPrice = computed(() => {
    return selectedItemDetails.value.reduce((sum, item) => sum + (Number(item.price) || 0), 0);
  });

  const totalDuration = computed(() => {
    return selectedItemDetails.value.reduce((sum, item) => sum + (Number(item.duration) || 30), 0);
  });

  watch(selectedItemDetails, (val) => {
    emit('update:selectedItems', val);
  }, { immediate: true });

  const toggleVariant = (id) => {
    const newVariants = [...props.selectedVariants];
    const index = newVariants.indexOf(id);
    if (index > -1) newVariants.splice(index, 1);
    else newVariants.push(id);
    emit('update:selectedVariants', newVariants);
  };

  const fetchServices = async () => {
    try {
      const response = await bookingApi.getPublicServices();
      const flatServices = (response.data.$values || response.data || []).filter(s => s.isBookable !== false);
      const groups = {};

      flatServices.forEach(s => {
        const catDict = s.category || {};
        const catName = getLocText(catDict, 'Egyéb');
        if (!groups[catName]) {
          groups[catName] = {
            id: catName,
            categoryName: catDict,
            categoryPictogram: s.categoryPictogram || null,
            items: []
          };
        } else if (!groups[catName].categoryPictogram && s.categoryPictogram) {
          groups[catName].categoryPictogram = s.categoryPictogram;
        }
        groups[catName].items.push(s);
      });

      categories.value = Object.values(groups);

      if (categories.value.length > 0) {
        activeCategory.value = categories.value[0].id;
      }
    } catch (error) {
      console.error('Failed to load public booking services:', error);
    } finally {
      loading.value = false;
    }
  };

  onMounted(() => fetchServices());
</script>
