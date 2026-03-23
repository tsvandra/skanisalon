<template>
  <div class="flex flex-col h-full">
    <h3 class="text-xl font-bold mb-6 text-text">{{ $t('booking.selectService') }}</h3>

    <div v-if="loading" class="text-center py-12 text-text/60">
      <i class="pi pi-spin pi-spinner text-4xl mb-3 block"></i>
      <span class="font-medium">{{ $t('common.loading') }}</span>
    </div>

    <div v-else class="flex-1 overflow-y-auto space-y-3 pr-2 [&::-webkit-scrollbar]:w-2 [&::-webkit-scrollbar-thumb]:bg-primary/30 [&::-webkit-scrollbar-thumb]:rounded-full">
      <div v-for="group in categories" :key="group.id"
           class="border border-text/10 rounded-xl bg-surface overflow-hidden transition-all duration-300 shadow-sm">

        <button @click="toggleCategory(group.id)"
                class="w-full min-h-[56px] px-4 flex items-center justify-between bg-background hover:bg-surface transition-colors focus:outline-none">
          <span class="font-bold text-lg text-text">
            {{ getLocText(group.categoryName) }}
          </span>
          <div class="flex items-center gap-3">
            <span v-if="getSelectedCount(group) > 0"
                  class="bg-primary text-surface text-xs font-bold px-2 py-1 rounded-full">
              {{ getSelectedCount(group) }}
            </span>
            <i class="pi transition-transform duration-300 text-text/50"
               :class="activeCategory === group.id ? 'pi-chevron-up' : 'pi-chevron-down'"></i>
          </div>
        </button>

        <div v-show="activeCategory === group.id" class="p-4 border-t border-text/5 bg-background/50">
          <div v-for="service in group.items" :key="service.id" class="mb-6 last:mb-0">
            <div class="font-medium text-text/80 mb-3 ml-1">{{ getLocText(service.name) }}</div>

            <div class="flex flex-col gap-2">
              <label v-for="variant in service.variants" :key="variant.id"
                     class="flex items-center justify-between p-3 rounded-xl border cursor-pointer transition-all min-h-[48px]"
                     :class="selectedVariants.includes(variant.id) ? 'bg-primary/10 border-primary ring-1 ring-primary/50' : 'border-text/10 bg-surface hover:border-primary/50'">

                <div class="flex items-center gap-3">
                  <div class="w-5 h-5 rounded border flex items-center justify-center transition-colors"
                       :class="selectedVariants.includes(variant.id) ? 'bg-primary border-primary text-surface' : 'border-text/30 bg-background'">
                    <i v-if="selectedVariants.includes(variant.id)" class="pi pi-check text-[10px] font-bold"></i>
                  </div>

                  <input type="checkbox" :value="variant.id" :checked="selectedVariants.includes(variant.id)" @change="toggleVariant(variant.id)" class="hidden" />

                  <span class="font-semibold text-text" :class="{ 'text-primary': selectedVariants.includes(variant.id) }">
                    {{ getLocText(variant.variantName) }}
                  </span>
                </div>
                <span class="text-sm font-bold text-text/70">{{ formatCurrency(variant.price) }}</span>
              </label>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div class="pt-4 mt-6 border-t border-text/10 sticky bottom-0 bg-background">
      <button @click="onNext" :disabled="selectedVariants.length === 0"
              class="w-full min-h-[56px] flex items-center justify-center gap-2 bg-primary text-surface font-bold text-lg rounded-xl disabled:opacity-50 disabled:cursor-not-allowed transition-all hover:brightness-110 active:scale-95 shadow-md">
        {{ $t('booking.next') }}
        <span v-if="selectedVariants.length > 0" class="bg-surface/20 px-2.5 py-0.5 rounded-full text-sm ml-2">
          {{ selectedVariants.length }}
        </span>
        <i class="pi pi-arrow-right ml-1"></i>
      </button>
    </div>
  </div>
</template>

<script setup>
  import { ref, computed, onMounted } from 'vue';
  import { useI18n } from 'vue-i18n';
  import { useCompanyStore } from '@/stores/companyStore';
  import bookingApi from '@/services/bookingApi';

  const props = defineProps({
    selectedVariants: { type: Array, required: true },
    onNext: { type: Function, required: true }
  });

  const emit = defineEmits(['update:selectedVariants']);
  const { locale } = useI18n();

  // Store-ok inicializálása
  const companyStore = useCompanyStore();
  const currentLang = computed(() => locale.value);
  const fallbackLang = computed(() => companyStore.company?.defaultLanguage || 'hu');

  const loading = ref(true);
  const categories = ref([]);
  const activeCategory = ref(null);

  // Biztonságos nyelvi feloldó függvény (Zéró Hardcode elv)
  const getLocText = (dict) => {
    if (!dict) return '';
    return dict[currentLang.value] || dict[fallbackLang.value] || dict['hu'] || '';
  };

  const formatCurrency = (val) => {
    if (val == null) return '';
    return val.toLocaleString('hu-HU', { style: 'currency', currency: 'EUR', maximumFractionDigits: 0 });
  };

  const toggleCategory = (categoryId) => {
    if (activeCategory.value === categoryId) {
      activeCategory.value = null;
    } else {
      activeCategory.value = categoryId;
    }
  };

  const getSelectedCount = (group) => {
    let count = 0;
    group.items.forEach(service => {
      service.variants.forEach(variant => {
        if (props.selectedVariants.includes(variant.id)) count++;
      });
    });
    return count;
  };

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
      const flatServices = response.data.$values || response.data;
      const groups = {};

      flatServices.forEach(s => {
        const catDict = s.category || {};
        const catName = catDict[currentLang.value] || catDict[fallbackLang.value] || catDict['hu'] || 'Egyéb';
        if (!groups[catName]) groups[catName] = { id: catName, categoryName: catDict, items: [] };
        groups[catName].items.push(s);
      });

      categories.value = Object.values(groups);

      if (categories.value.length > 0) {
        activeCategory.value = categories.value[0].id;
      }
    } catch (error) {
      console.error('Hiba a szolgáltatások betöltésekor', error);
    } finally {
      loading.value = false;
    }
  };

  onMounted(() => fetchServices());
</script>
