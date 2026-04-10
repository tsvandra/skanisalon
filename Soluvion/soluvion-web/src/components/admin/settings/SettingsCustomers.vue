<template>
  <div class="space-y-6">
    <div class="flex justify-between items-center bg-surface p-4 md:p-6 rounded-2xl shadow-sm border border-text/10">
      <div>
        <h2 class="text-xl md:text-2xl font-black text-primary flex items-center gap-2">
          <i class="pi pi-tags"></i> Vendég Jellemzők (Attribútumok)
        </h2>
        <p class="text-text-muted text-sm mt-1">
          Itt állíthatod be, milyen egyedi adatokat (pl. Hajhossz, Allergia) szeretnél tárolni a vendégeidről.
        </p>
      </div>
      <button @click="openModal()" class="h-10 px-4 bg-primary text-white font-bold rounded-xl shadow-md hover:brightness-110 transition-all flex items-center gap-2">
        <i class="pi pi-plus"></i> Új jellemző
      </button>
    </div>

    <div v-if="loading" class="text-center py-8 text-primary font-bold animate-pulse">
      <i class="pi pi-spinner pi-spin text-2xl mb-2"></i><br>Betöltés...
    </div>

    <div v-else-if="attributes.length > 0" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      <div v-for="attr in attributes" :key="attr.id"
           class="bg-surface border border-text/10 rounded-xl p-4 shadow-sm hover:border-primary/40 transition-colors relative group">

        <div class="absolute top-3 right-3 flex gap-1 opacity-100 lg:opacity-0 group-hover:opacity-100 transition-opacity">
          <button @click="openModal(attr)" class="w-7 h-7 rounded-full bg-blue-500/10 text-blue-500 flex items-center justify-center hover:bg-blue-500 hover:text-white transition-colors">
            <i class="pi pi-pencil text-xs"></i>
          </button>
          <button @click="deleteAttribute(attr.id)" class="w-7 h-7 rounded-full bg-red-500/10 text-red-500 flex items-center justify-center hover:bg-red-500 hover:text-white transition-colors">
            <i class="pi pi-trash text-xs"></i>
          </button>
        </div>

        <h3 class="font-bold text-text text-lg pr-12">{{ attr.label }}</h3>
        <p class="text-xs text-text-muted font-mono mb-3">Belső kulcs: {{ attr.key }}</p>

        <div class="flex flex-wrap gap-2 text-[10px] font-bold uppercase">
          <span class="bg-surface-alt px-2 py-1 rounded border border-text/5 text-text-muted">
            <i class="pi" :class="attr.dataType === 'select' ? 'pi-list' : 'pi-align-left'"></i>
            {{ attr.dataType === 'select' ? 'Legördülő' : 'Szöveges' }}
          </span>
          <span v-if="attr.isRequired" class="bg-red-500/10 text-red-500 px-2 py-1 rounded border border-red-500/20">
            Kötelező
          </span>
        </div>

        <div v-if="attr.dataType === 'select' && attr.options?.length" class="mt-3 pt-3 border-t border-text/5 flex flex-wrap gap-1">
          <span v-for="(opt, idx) in attr.options" :key="idx" class="text-[10px] bg-primary/5 text-primary px-1.5 py-0.5 rounded">
            {{ opt }}
          </span>
        </div>
      </div>
    </div>

    <div v-else class="text-center py-12 bg-surface rounded-2xl border border-dashed border-text/20 text-text-muted">
      <i class="pi pi-id-card text-4xl text-text/30 mb-3"></i>
      <p class="font-medium text-lg">Még nincsenek egyedi jellemzők beállítva.</p>
    </div>

    <div v-if="isModalOpen" class="fixed inset-0 bg-black/60 backdrop-blur-sm z-50 flex items-center justify-center p-4">
      <div class="bg-surface w-full max-w-md rounded-2xl shadow-xl overflow-hidden flex flex-col">

        <div class="p-5 border-b border-text/10 flex justify-between items-center bg-background/50">
          <h3 class="font-bold text-lg text-text">
            {{ form.id ? 'Jellemző szerkesztése' : 'Új jellemző hozzáadása' }}
          </h3>
          <button @click="isModalOpen = false" class="text-text-muted hover:text-text">
            <i class="pi pi-times"></i>
          </button>
        </div>

        <div class="p-5 space-y-4 overflow-y-auto max-h-[70vh]">

          <div>
            <label class="block text-xs font-bold text-text-muted mb-1 uppercase">Megnevezés (pl. Hajhossz) *</label>
            <input type="text" v-model="form.label" @input="autoGenerateKey" placeholder="A vendég kártyán megjelenő név"
                   class="w-full h-10 px-3 rounded-lg border border-text/20 bg-background text-sm focus:border-primary focus:outline-none">
          </div>

          <div>
            <label class="block text-xs font-bold text-text-muted mb-1 uppercase">Rendszer Kulcs *</label>
            <input type="text" v-model="form.key" :disabled="!!form.id" placeholder="pl. hajhossz (csak betűk és alulvonás)"
                   class="w-full h-10 px-3 rounded-lg border border-text/20 bg-background text-sm focus:border-primary focus:outline-none disabled:opacity-50">
            <p v-if="!!form.id" class="text-[10px] text-text-muted mt-1">A kulcs mentés után már nem módosítható!</p>
          </div>

          <div>
            <label class="block text-xs font-bold text-text-muted mb-1 uppercase">Típus *</label>
            <select v-model="form.dataType" class="w-full h-10 px-3 rounded-lg border border-text/20 bg-background text-sm focus:border-primary focus:outline-none">
              <option value="text">Szabadon beírható szöveg</option>
              <option value="select">Legördülő lista (Választó)</option>
            </select>
          </div>

          <div v-if="form.dataType === 'select'" class="bg-background/50 p-3 rounded-lg border border-text/10">
            <label class="block text-xs font-bold text-text-muted mb-2 uppercase">Választható Opciók</label>
            <div class="space-y-2 mb-2">
              <div v-for="(opt, idx) in form.options" :key="idx" class="flex gap-2">
                <input type="text" v-model="form.options[idx]" placeholder="pl. Rövid" class="flex-1 h-8 px-2 rounded-md border border-text/20 text-xs">
                <button @click="form.options.splice(idx, 1)" class="w-8 h-8 flex items-center justify-center text-red-500 hover:bg-red-500/10 rounded-md">
                  <i class="pi pi-times"></i>
                </button>
              </div>
            </div>
            <button @click="form.options.push('')" class="text-xs font-bold text-primary flex items-center gap-1 hover:underline">
              <i class="pi pi-plus"></i> Új opció hozzáadása
            </button>
          </div>

          <div class="flex items-center gap-3 pt-2">
            <input type="checkbox" v-model="form.isRequired" id="req" class="w-4 h-4 text-primary rounded border-text/20">
            <label for="req" class="text-sm font-bold text-text cursor-pointer">Kötelező kitölteni</label>
          </div>

        </div>

        <div class="p-5 border-t border-text/10 flex justify-end gap-3 bg-background/50">
          <button @click="isModalOpen = false" class="px-4 h-10 font-bold text-text-muted hover:text-text transition-colors">
            Mégsem
          </button>
          <button @click="saveAttribute" :disabled="saving || !form.label || !form.key"
                  class="px-6 h-10 bg-primary text-white font-bold rounded-lg hover:brightness-110 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2">
            <i class="pi" :class="saving ? 'pi-spinner pi-spin' : 'pi-save'"></i> Mentés
          </button>
        </div>

      </div>
    </div>

  </div>
</template>

<script setup>
  import { ref, onMounted } from 'vue';
  import api from '@/services/api'; // <--- Ide beírjuk a meglévő fő API hívódat

  const attributes = ref([]);
  const loading = ref(true);
  const saving = ref(false);
  const isModalOpen = ref(false);

  const form = ref({
    id: null,
    label: '',
    key: '',
    dataType: 'text',
    options: [],
    isRequired: false,
    showOnPublicBooking: false,
    isActive: true
  });

  const loadAttributes = async () => {
    loading.value = true;
    try {
      const response = await api.get('/api/company-attributes');
      attributes.value = response.data.$values || response.data || [];
    } catch (error) {
      console.error("Hiba az attribútumok lekérésekor:", error);
    } finally {
      loading.value = false;
    }
  };

  const autoGenerateKey = () => {
    if (!form.value.id) {
      form.value.key = form.value.label
        .normalize("NFD").replace(/[\u0300-\u036f]/g, "")
        .toLowerCase()
        .replace(/[^a-z0-9]/g, '_')
        .replace(/_+/g, '_')
        .replace(/^_|_$/g, '');
    }
  };

  const openModal = (attr = null) => {
    if (attr) {
      form.value = { ...attr, options: attr.options ? [...attr.options] : [] };
    } else {
      form.value = { id: null, label: '', key: '', dataType: 'text', options: [], isRequired: false, showOnPublicBooking: false, isActive: true };
    }
    isModalOpen.value = true;
  };

  const saveAttribute = async () => {
    saving.value = true;

    if (form.value.dataType === 'select') {
      form.value.options = form.value.options.filter(o => o.trim() !== '');
    } else {
      form.value.options = [];
    }

    try {
      if (form.value.id) {
        await api.put(`/api/company-attributes/${form.value.id}`, form.value);
      } else {
        await api.post('/api/company-attributes', form.value);
      }
      await loadAttributes();
      isModalOpen.value = false;
    } catch (error) {
      console.error("Mentési hiba:", error);
      alert(error.response?.data?.message || "Hiba történt a mentés során.");
    } finally {
      saving.value = false;
    }
  };

  const deleteAttribute = async (id) => {
    if (confirm("Biztosan törlöd ezt az attribútumot? A meglévő vendégek adatai a háttérben megmaradnak, de az űrlapon nem fognak többé megjelenni.")) {
      try {
        await api.delete(`/api/company-attributes/${id}`);
        await loadAttributes();
      } catch (error) {
        console.error("Törlési hiba:", error);
      }
    }
  };

  onMounted(() => {
    loadAttributes();
  });
</script>
