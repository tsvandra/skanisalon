<template>
  <div class="flex h-[280px] bg-background border border-text/10 rounded-xl overflow-hidden shadow-sm relative">

    <div class="w-1/3 md:w-1/4 bg-surface border-r border-text/10 overflow-y-auto [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-text/20">
      <button v-for="cat in availableCategories" :key="cat"
              @click="activeCategory = cat"
              class="w-full text-left px-3 py-3 text-xs md:text-sm font-bold transition-all border-l-4"
              :class="activeCategory === cat ? 'bg-primary/10 border-primary text-primary shadow-inner' : 'border-transparent text-text hover:bg-text/5'">
        {{ cat }}
      </button>
    </div>

    <div class="w-2/3 md:w-3/4 p-3 overflow-y-auto bg-background/50 flex flex-col relative [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-text/20">

      <div v-if="missingVariantsMode" class="mb-3 p-2 bg-orange-500/10 border border-orange-500/30 text-orange-600 rounded-lg text-xs font-bold flex items-center gap-2 animate-fade-in">
        <i class="pi pi-exclamation-triangle shrink-0"></i>
        {{ $t('calendar.editor.validationMissingVariants') }}
      </div>

      <div class="flex-1 space-y-1.5 pb-12">
        <div v-for="srv in servicesToDisplay" :key="srv.id"
             class="group relative flex items-center justify-between p-3 rounded-lg transition-all cursor-pointer border"
             :class="stagedServices.includes(srv.id) ? 'bg-primary/10 border-primary/40 shadow-sm' : 'bg-surface border-transparent hover:border-text/10 hover:bg-surface/80'"
             @click="handleRowClick(srv.id)">

          <div class="flex-1 pr-2 flex items-center gap-2">
            <button v-if="missingVariantsMode"
                    @click.stop="toggleStagedService(srv.id)"
                    class="w-5 h-5 flex items-center justify-center rounded bg-red-500/10 text-red-500 hover:bg-red-500 hover:text-white transition-colors shrink-0"
                    :title="$t('common.delete')">
              <i class="pi pi-times text-[10px] font-bold"></i>
            </button>
            <span class="text-xs md:text-sm font-bold transition-colors"
                  :class="stagedServices.includes(srv.id) ? 'text-primary' : 'text-text'">
              {{ getLocText(srv.name) }}
            </span>
          </div>

          <div class="w-32 md:w-48 shrink-0">
            <InlineDropdown :model-value="stagedVariants[srv.id]"
                            :options="getDropdownOptions(srv)"
                            :placeholder="$t('calendar.editor.selectPlaceholder')"
                            :is-open="openDropdownId === srv.id"
                            @toggle="toggleDropdown(srv.id)"
                            @select="(val) => selectVariant(srv.id, val)" />
          </div>
        </div>

        <div v-if="servicesToDisplay.length === 0" class="text-center text-text-muted text-xs py-4">
          {{ $t('calendar.editor.noServicesToDisplay') }}
        </div>
      </div>

      <div class="pt-3 mt-auto sticky bottom-0 bg-background/90 backdrop-blur-sm border-t border-text/5 z-10">
        <button @click="handleAddClick"
                :disabled="stagedServices.length === 0"
                class="w-full h-[36px] text-white text-xs md:text-sm font-bold rounded-lg disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2 transition-all shadow-sm"
                :class="missingVariantsMode ? 'bg-orange-500 hover:bg-orange-600' : 'bg-primary hover:brightness-110'">
          <i class="pi" :class="missingVariantsMode ? 'pi-exclamation-circle' : 'pi-plus-circle'"></i>
          {{ missingVariantsMode ? $t('calendar.editor.addMissingAndSubmit') : $t('calendar.editor.addSelectedServices') }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import InlineDropdown from '@/components/common/InlineDropdown.vue';

const props = defineProps({
  availableServices: { type: Array, required: true },
  openDropdownId: { type: [Number, String, null], default: null }
});

const emit = defineEmits(['add-items', 'update:openDropdownId']);

const { locale } = useI18n();
const currentLang = computed(() => locale.value || 'hu-HU');

const activeCategory = ref('');
const stagedServices = ref([]);
const stagedVariants = ref({});
const missingVariantsMode = ref(false);

const getLocText = (dict) => dict ? (dict[currentLang.value] || dict['hu'] || '') : '';

const getDropdownOptions = (srv) => {
  return srv.variants.map(v => ({
    value: v.id,
    label: getLocText(v.variantName),
    hint: `+${v.price}€`
  }));
};

const availableCategories = computed(() => {
  const cats = new Set();
  props.availableServices.forEach(s => cats.add(getLocText(s.category) || 'Egyéb'));
  return Array.from(cats).sort();
});

const availableServicesInCategory = computed(() => {
  return props.availableServices.filter(s => (getLocText(s.category) || 'Egyéb') === activeCategory.value);
});

// Init első kategória
onMounted(() => {
  if (availableCategories.value.length > 0) activeCategory.value = availableCategories.value[0];
});

watch(() => props.availableServices, (newVal) => {
  if (newVal.length > 0 && !activeCategory.value) {
    activeCategory.value = availableCategories.value[0];
  }
}, { deep: true });

watch(activeCategory, () => {
  stagedServices.value = [];
  stagedVariants.value = {};
  missingVariantsMode.value = false;
  emit('update:openDropdownId', null);
});

const servicesToDisplay = computed(() => {
  if (missingVariantsMode.value) {
    return availableServicesInCategory.value.filter(s => stagedServices.value.includes(s.id) && !stagedVariants.value[s.id]);
  }
  return availableServicesInCategory.value;
});

const toggleDropdown = (sId) => {
  emit('update:openDropdownId', props.openDropdownId === sId ? null : sId);
};

const handleRowClick = (sId) => {
  if (missingVariantsMode.value) {
    emit('update:openDropdownId', sId);
  } else {
    toggleStagedService(sId);
    emit('update:openDropdownId', null);
  }
};

const toggleStagedService = (sId) => {
  const idx = stagedServices.value.indexOf(sId);
  if (idx > -1) {
    stagedServices.value.splice(idx, 1);
    delete stagedVariants.value[sId];

    if (missingVariantsMode.value) {
      const stillMissing = stagedServices.value.some(id => !stagedVariants.value[id]);
      if (!stillMissing) missingVariantsMode.value = false;
    }
  } else {
    stagedServices.value.push(sId);
    const s = props.availableServices.find(x => x.id === sId);
    if (s && s.variants.length === 1) {
      stagedVariants.value[sId] = s.variants[0].id;
    }
  }
};

const syncVariants = (sourceServiceId, selectedVariantId) => {
  if (!stagedServices.value.includes(sourceServiceId)) {
    stagedServices.value.push(sourceServiceId);
  }

  const sourceService = props.availableServices.find(x => x.id === sourceServiceId);
  const sourceVariant = sourceService?.variants.find(v => v.id === selectedVariantId);
  if (!sourceVariant) return;

  const variantName = getLocText(sourceVariant.variantName);

  stagedServices.value.forEach(sId => {
    if (sId === sourceServiceId) return;
    const s = props.availableServices.find(x => x.id === sId);
    if (s) {
      const match = s.variants.find(v => getLocText(v.variantName) === variantName);
      if (match) {
        stagedVariants.value[sId] = match.id;
      }
    }
  });

  if (missingVariantsMode.value) {
    const stillMissing = stagedServices.value.some(id => !stagedVariants.value[id]);
    if (!stillMissing) missingVariantsMode.value = false;
  }
};

const selectVariant = (sId, vId) => {
  stagedVariants.value[sId] = vId;
  syncVariants(sId, vId);
  emit('update:openDropdownId', null);
};

const handleAddClick = () => {
  const missing = stagedServices.value.filter(sId => !stagedVariants.value[sId]);

  if (missing.length > 0) {
    missingVariantsMode.value = true;
    return;
  }

  const newItems = [];
  stagedServices.value.forEach(sId => {
    const s = props.availableServices.find(x => x.id === sId);
    const vId = stagedVariants.value[sId];
    const v = s.variants.find(x => x.id === vId);
    if (s && v) {
      newItems.push({
        variantId: v.id,
        name: `${getLocText(s.name)} - ${getLocText(v.variantName)}`,
        duration: v.duration || 30,
        price: v.price
      });
    }
  });

  emit('add-items', newItems);

  stagedServices.value = [];
  stagedVariants.value = {};
  missingVariantsMode.value = false;
};
</script>

<style scoped>
  .animate-fade-in {
    animation: fadeIn 0.3s ease-out;
  }

  @keyframes fadeIn {
    from {
      opacity: 0;
      transform: translateY(-5px);
    }

    to {
      opacity: 1;
      transform: translateY(0);
    }
  }
</style>
