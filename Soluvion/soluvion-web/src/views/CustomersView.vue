<template>
  <div class="min-h-screen bg-background text-text p-4 md:p-8">
    <div class="max-w-7xl mx-auto space-y-6">

      <div class="flex flex-col md:flex-row justify-between items-start md:items-center gap-4 bg-surface p-4 md:p-6 rounded-2xl shadow-sm border border-text/10">
        <div>
          <h1 class="text-2xl md:text-3xl font-black text-primary flex items-center gap-3">
            <i class="pi pi-users"></i> Ügyfelek kezelése
          </h1>
          <p class="text-text-muted text-sm md:text-base mt-1">Vendégek adatainak, jellemzőinek és megjegyzéseinek karbantartása.</p>
        </div>

        <div class="flex flex-col sm:flex-row w-full md:w-auto gap-3">
          <div class="relative w-full sm:w-64">
            <i class="pi pi-search absolute left-3 top-1/2 -translate-y-1/2 text-text-muted"></i>
            <input type="text" v-model="searchQuery" placeholder="Keresés név alapján..."
                   class="w-full h-[44px] pl-10 pr-4 bg-background border border-text/20 rounded-xl text-sm focus:outline-none focus:border-primary transition-colors">
          </div>
          <button @click="openCreateModal" class="h-[44px] px-5 bg-primary text-white font-bold rounded-xl shadow-md hover:brightness-110 active:scale-95 transition-all flex items-center justify-center gap-2 whitespace-nowrap">
            <i class="pi pi-plus"></i> Új ügyfél
          </button>
        </div>
      </div>

      <div v-if="loading" class="text-center py-12 text-text-muted font-bold animate-pulse">
        <i class="pi pi-spinner pi-spin text-2xl text-primary mb-2"></i><br>Adatok betöltése...
      </div>

      <div v-else-if="filteredCustomers.length === 0" class="text-center py-16 bg-surface rounded-2xl border border-dashed border-text/20 text-text-muted">
        <i class="pi pi-user-minus text-4xl text-text/30 mb-3"></i>
        <p class="font-medium text-lg">Nincs találat vagy még nincsenek ügyfelek.</p>
      </div>

      <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
        <div v-for="customer in filteredCustomers" :key="customer.id"
             class="bg-surface rounded-2xl p-5 shadow-sm border border-text/10 flex flex-col gap-4 hover:border-primary/40 transition-colors relative group">

          <div class="absolute top-4 right-4 flex gap-2 opacity-100 md:opacity-0 group-hover:opacity-100 transition-opacity">
            <button @click="openEditModal(customer)" class="w-8 h-8 rounded-full bg-blue-500/10 text-blue-500 flex items-center justify-center hover:bg-blue-500 hover:text-white transition-colors" title="Szerkesztés">
              <i class="pi pi-pencil text-sm"></i>
            </button>
            <button @click="confirmDelete(customer)" class="w-8 h-8 rounded-full bg-red-500/10 text-red-500 flex items-center justify-center hover:bg-red-500 hover:text-white transition-colors" title="Törlés">
              <i class="pi pi-trash text-sm"></i>
            </button>
          </div>

          <div class="flex items-center gap-4">
            <div class="w-12 h-12 rounded-full flex items-center justify-center text-lg font-black text-gray-800 shadow-sm border border-text/5 shrink-0"
                 :style="{ backgroundColor: getCustomerColor(customer.id) }">
              {{ getInitials(customer.name) }}
            </div>
            <div class="overflow-hidden">
              <h3 class="font-bold text-lg text-text truncate pr-16" :title="customer.name">{{ customer.name }}</h3>
              <p class="text-xs text-text-muted font-mono">ID: #{{ customer.id }}</p>
            </div>
          </div>

          <div class="space-y-2 mt-2">
            <div class="flex items-center gap-2 text-sm text-text-muted">
              <i class="pi pi-phone text-primary/70 shrink-0"></i>
              <span class="truncate">{{ customer.phone || 'Nincs megadva telefonszám' }}</span>
            </div>
            <div class="flex items-center gap-2 text-sm text-text-muted">
              <i class="pi pi-envelope text-primary/70 shrink-0"></i>
              <span class="truncate">{{ customer.email || 'Nincs megadva email' }}</span>
            </div>
          </div>

          <div v-if="customer.attributes && Object.keys(customer.attributes).length > 0" class="flex flex-wrap gap-1.5 mt-2">
            <span v-for="(val, key) in customer.attributes" :key="key" class="text-[10px] bg-primary/10 text-primary px-2 py-0.5 rounded-md font-bold border border-primary/20">
              {{ key }}: {{ val }}
            </span>
          </div>

          <div v-if="customer.notes" class="mt-auto pt-4 border-t border-text/5">
            <p class="text-xs text-text-muted italic line-clamp-2" :title="customer.notes">
              <i class="pi pi-info-circle mr-1"></i> {{ customer.notes }}
            </p>
          </div>
        </div>
      </div>

    </div>

    <div v-if="isModalOpen" class="fixed inset-0 bg-black/60 backdrop-blur-sm z-[1100] flex items-center justify-center p-4">
      <div class="bg-surface w-full max-w-lg rounded-2xl shadow-2xl overflow-hidden border border-text/10 flex flex-col max-h-[90vh]">

        <div class="p-5 border-b border-text/10 flex justify-between items-center bg-background/50 shrink-0">
          <h2 class="text-xl font-bold text-text flex items-center gap-2">
            <i class="pi" :class="isEditing ? 'pi-user-edit text-primary' : 'pi-user-plus text-primary'"></i>
            {{ isEditing ? 'Ügyfél szerkesztése' : 'Új ügyfél felvétele' }}
          </h2>
          <button @click="closeModal" class="w-8 h-8 flex items-center justify-center rounded-full hover:bg-text/10 text-text-muted transition-colors">
            <i class="pi pi-times"></i>
          </button>
        </div>

        <div class="p-6 space-y-6 overflow-y-auto [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-text/20">

          <div class="space-y-4">
            <div>
              <label class="block text-xs font-bold text-text-muted mb-1.5 uppercase">Teljes Név <span class="text-red-500">*</span></label>
              <input type="text" v-model="form.name" name="cust_name" autocomplete="off" data-1p-ignore data-lpignore="true" placeholder="Pl. Kiss Katalin" class="w-full h-[44px] bg-background border border-text/20 rounded-xl px-4 text-sm focus:outline-none focus:border-primary font-medium">
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-bold text-text-muted mb-1.5 uppercase">Telefonszám</label>
                <input type="tel" v-model="form.phone" name="cust_phone" autocomplete="off" data-1p-ignore data-lpignore="true" placeholder="+36 30 123 4567" class="w-full h-[44px] bg-background border border-text/20 rounded-xl px-4 text-sm focus:outline-none focus:border-primary font-medium">
              </div>

              <div>
                <label class="block text-xs font-bold text-text-muted mb-1.5 uppercase">Email cím</label>
                <input type="email" v-model="form.email" name="cust_email" autocomplete="off" data-1p-ignore data-lpignore="true" placeholder="katalin@example.com" class="w-full h-[44px] bg-background border border-text/20 rounded-xl px-4 text-sm focus:outline-none focus:border-primary font-medium">
              </div>
            </div>
          </div>

          <div class="h-px bg-text/10"></div>

          <div>
            <label class="block text-xs font-bold text-text-muted mb-2 uppercase flex items-center gap-1">
              <i class="pi pi-tags"></i> Jellemzők (Attribútumok)
            </label>

            <div v-if="Object.keys(form.attributes).length > 0" class="space-y-2 mb-3">
              <div v-for="(val, key) in form.attributes" :key="key" class="flex gap-2 items-center bg-background border border-text/10 rounded-lg p-1.5">
                <div class="w-1/3 px-2 text-xs font-bold text-text-muted truncate" :title="key">{{ key }}</div>
                <input type="text" v-model="form.attributes[key]" class="flex-1 bg-surface border border-text/20 rounded-md p-2 text-xs font-bold focus:outline-none focus:border-primary transition-colors">
                <button @click="removeAttribute(key)" class="w-8 h-8 flex items-center justify-center text-red-500 hover:bg-red-500/10 rounded-md transition-colors">
                  <i class="pi pi-trash text-xs"></i>
                </button>
              </div>
            </div>

            <div class="flex gap-2 items-center bg-primary/5 border border-primary/20 p-2 rounded-lg">
              <datalist id="common-attributes">
                <option value="Hajhossz"></option>
                <option value="Hajtípus"></option>
                <option value="Allergia"></option>
                <option value="Ital"></option>
              </datalist>
              <input type="text" v-model="newAttrKey" list="common-attributes" placeholder="Kulcs (pl. Hajhossz)" class="w-1/3 bg-background border border-text/20 rounded-md p-2 text-xs focus:outline-none focus:border-primary font-bold">
              <input type="text" v-model="newAttrValue" @keydown.enter="addAttribute" placeholder="Érték (pl. Rövid)" class="flex-1 bg-background border border-text/20 rounded-md p-2 text-xs focus:outline-none focus:border-primary font-bold">
              <button @click="addAttribute" :disabled="!newAttrKey || !newAttrValue" class="w-8 h-8 flex items-center justify-center bg-primary text-white rounded-md hover:brightness-110 disabled:opacity-50 transition-all shadow-sm">
                <i class="pi pi-plus text-xs"></i>
              </button>
            </div>
          </div>

          <div class="h-px bg-text/10"></div>

          <div>
            <label class="block text-xs font-bold text-text-muted mb-1.5 uppercase flex items-center gap-1">
              <i class="pi pi-align-left"></i> Általános Megjegyzés
            </label>
            <textarea v-model="form.notes" rows="2" placeholder="Olyan információ, ami nem fér be jellemzőként (pl. Általában késik 10 percet...)" class="w-full bg-background border border-text/20 rounded-xl p-3 text-sm focus:outline-none focus:border-primary resize-none font-medium"></textarea>
          </div>
        </div>

        <div class="p-5 border-t border-text/10 bg-background/50 flex justify-end gap-3 shrink-0">
          <button @click="closeModal" class="px-5 h-[44px] text-text text-sm font-bold rounded-xl hover:bg-text/10 transition-colors">
            Mégsem
          </button>
          <button @click="saveCustomer" :disabled="!isFormValid || saving" class="px-6 h-[44px] bg-primary text-white text-sm font-bold rounded-xl hover:brightness-110 shadow-md transition-transform active:scale-95 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2">
            <i class="pi" :class="saving ? 'pi-spinner pi-spin' : 'pi-save'"></i> Mentés
          </button>
        </div>

      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, computed, onMounted } from 'vue';
  import bookingApi from '@/services/bookingApi';
  import { getCustomerColor } from '@/utils/colorUtils';

  const customers = ref([]);
  const loading = ref(true);
  const saving = ref(false);
  const searchQuery = ref('');

  const isModalOpen = ref(false);
  const isEditing = ref(false);

  const form = ref({
    id: null,
    name: '',
    phone: '',
    email: '',
    notes: '',
    attributes: {}
  });

  // Dinamikus Jellemzők (Attributes) logikája
  const newAttrKey = ref('');
  const newAttrValue = ref('');

  const addAttribute = () => {
    const key = newAttrKey.value.trim();
    const val = newAttrValue.value.trim();
    if (key && val) {
      form.value.attributes[key] = val;
      newAttrKey.value = '';
      newAttrValue.value = '';
    }
  };

  const removeAttribute = (key) => {
    delete form.value.attributes[key];
  };

  // Szűrő a kereséshez
  const filteredCustomers = computed(() => {
    if (!searchQuery.value) return customers.value;
    const q = searchQuery.value.toLowerCase();
    return customers.value.filter(c =>
      (c.name && c.name.toLowerCase().includes(q)) ||
      (c.phone && c.phone.includes(q)) ||
      (c.email && c.email.toLowerCase().includes(q))
    );
  });

  const isFormValid = computed(() => {
    return form.value.name && form.value.name.trim().length > 0;
  });

  // Adatok betöltése
  const fetchCustomers = async () => {
    loading.value = true;
    try {
      const response = await bookingApi.getCustomers();
      customers.value = response.data.$values || response.data || [];
    } catch (error) {
      console.error("Hiba az ügyfelek lekérésekor:", error);
      alert("Nem sikerült betölteni az ügyfeleket.");
    } finally {
      loading.value = false;
    }
  };

  // Modal kezelés
  const openCreateModal = () => {
    isEditing.value = false;
    form.value = { id: null, name: '', phone: '', email: '', notes: '', attributes: {} };
    newAttrKey.value = '';
    newAttrValue.value = '';
    isModalOpen.value = true;
  };

  const openEditModal = (customer) => {
    isEditing.value = true;
    form.value = {
      id: customer.id,
      name: customer.name || '',
      phone: customer.phone || '',
      email: customer.email || '',
      notes: customer.notes || '',
      // Clone-ozzuk az objektumot, hogy ne módosítsuk a kártyát mentés előtt
      attributes: customer.attributes ? { ...customer.attributes } : {}
    };
    newAttrKey.value = '';
    newAttrValue.value = '';
    isModalOpen.value = true;
  };

  const closeModal = () => {
    isModalOpen.value = false;
  };

  // Mentés (Létrehozás vagy Frissítés)
  const saveCustomer = async () => {
    if (!isFormValid.value) return;
    saving.value = true;

    try {
      const payload = {
        fullName: form.value.name.trim(),
        phone: form.value.phone?.trim() || null,
        email: form.value.email?.trim() || null,
        notes: form.value.notes?.trim() || null,
        attributes: form.value.attributes
      };

      if (isEditing.value) {
        await bookingApi.updateCustomer(form.value.id, payload);
      } else {
        await bookingApi.createCustomer(payload);
      }

      await fetchCustomers(); // Újratöltjük a listát
      closeModal();
    } catch (error) {
      console.error("Hiba a mentés során:", error);
      alert("Hiba történt a mentés során. Lehet, hogy a szerver nem támogatja még a frissítést.");
    } finally {
      saving.value = false;
    }
  };

  // Törlés
  const confirmDelete = async (customer) => {
    if (confirm(`Biztosan törlöd a következő ügyfelet: ${customer.name}?\nEzzel a hozzá tartozó foglalások állapota is változhat!`)) {
      try {
        await bookingApi.deleteCustomer(customer.id);
        await fetchCustomers();
      } catch (error) {
        console.error("Hiba a törlés során:", error);
        alert("Hiba történt a törlés során. Lehet, hogy folyamatban lévő foglalása van, ami miatt nem törölhető.");
      }
    }
  };

  const getInitials = (name) => {
    if (!name || name.startsWith('Vendég #')) return '?';
    return name.split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase();
  };

  onMounted(() => {
    fetchCustomers();
  });
</script>
