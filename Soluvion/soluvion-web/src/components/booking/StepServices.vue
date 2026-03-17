<template>
  <div>
    <h3 class="text-xl font-bold mb-4">{{ $t('booking.selectService') }}</h3>

    <div v-if="loading" class="text-center py-8 text-text-muted">
      <i class="pi pi-spin pi-spinner text-3xl mb-2 block"></i>
      {{ $t('common.loading') }}
    </div>

    <div v-else class="space-y-4 max-h-[50vh] overflow-y-auto pr-2">
      <div v-for="group in categories" :key="group.id" class="border border-text/10 rounded-lg p-4 bg-background">
        <h4 class="font-bold text-primary uppercase tracking-wider mb-3">{{ group.categoryName[currentLang] || group.categoryName['hu'] }}</h4>
        <div v-for="service in group.items" :key="service.id" class="mb-4 last:mb-0">
          <div class="font-medium text-lg mb-2">{{ service.name[currentLang] || service.name['hu'] }}</div>
          <div class="flex flex-wrap gap-2">
            <label v-for="variant in service.variants" :key="variant.id"
                   class="flex items-center gap-2 p-2 rounded border cursor-pointer transition-colors"
                   :class="selectedVariants.includes(variant.id) ? 'bg-primary/10 border-primary text-primary' : 'border-text/20 hover:border-primary/50'">
              <input type="checkbox" :value="variant.id" :checked="selectedVariants.includes(variant.id)" @change="toggleVariant(variant.id)" class="hidden" />
              <span class="font-bold">{{ variant.variantName[currentLang] || variant.variantName['hu'] }}</span>
              <span class="text-sm">({{ formatCurrency(variant.price) }})</span>
            </label>
          </div>
        </div>
      </div>
    </div>

    <div class="mt-6 flex justify-end">
      <button @click="onNext" :disabled="selectedVariants.length === 0"
              class="px-6 py-2 bg-primary text-white font-bold rounded-lg disabled:opacity-50 transition-colors">
        {{ $t('booking.next') }} <i class="pi pi-arrow-right ml-2"></i>
      </button>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import bookingApi from '@/services/bookingApi';

const props = defineProps({
  selectedVariants: { type: Array, required: true },
  onNext: { type: Function, required: true }
});

const emit = defineEmits(['update:selectedVariants']);
const { locale } = useI18n();
const currentLang = computed(() => locale.value);

const loading = ref(true);
const categories = ref([]);

const formatCurrency = (val) => {
  if (val == null) return '';
  return val.toLocaleString('hu-HU', { style: 'currency', currency: 'EUR', maximumFractionDigits: 0 });
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
      const catName = catDict[currentLang.value] || catDict['hu'] || 'Egyéb';
      if (!groups[catName]) groups[catName] = { id: catName, categoryName: catDict, items: [] };
      groups[catName].items.push(s);
    });
    categories.value = Object.values(groups);
  } catch (error) {
    console.error('Hiba a szolgáltatások betöltésekor', error);
  } finally {
    loading.value = false;
  }
};

onMounted(() => fetchServices());
</script>
