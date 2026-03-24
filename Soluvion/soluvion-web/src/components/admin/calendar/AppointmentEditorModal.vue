<template>
  <div v-if="isOpen" class="fixed inset-0 bg-black/60 backdrop-blur-sm z-50 flex items-center justify-center p-2 md:p-4">
    <div class="bg-surface w-full max-w-2xl rounded-2xl shadow-2xl overflow-hidden border border-text/10 flex flex-col max-h-[95vh] md:max-h-[90vh]">

      <div class="p-4 md:p-6 border-b border-text/10 flex justify-between items-center bg-background/50">
        <h2 class="text-lg md:text-xl font-bold text-text flex items-center gap-2">
          <i class="pi" :class="isEditing ? 'pi-pencil text-primary' : 'pi-plus-circle text-primary'"></i>
          {{ isEditing ? 'Foglalás szerkesztése' : 'Új foglalás rögzítése' }}
        </h2>
        <button @click="close" class="w-8 h-8 flex items-center justify-center rounded-full hover:bg-text/10 text-text-muted transition-colors">
          <i class="pi pi-times"></i>
        </button>
      </div>

      <div class="p-4 md:p-6 overflow-y-auto space-y-6">

        <div>
          <label class="block text-[10px] md:text-xs font-bold text-text-muted mb-1.5 uppercase flex items-center gap-1"><i class="pi pi-user"></i> 1. Ügyfél</label>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div class="relative">
              <select v-model="form.customerId" @change="handleCustomerSelect" class="w-full h-[44px] bg-background border border-text/20 rounded-lg px-3 text-sm text-text font-bold focus:outline-none focus:border-primary appearance-none cursor-pointer">
                <option value="" disabled>Válassz a listából...</option>
                <option value="new" class="text-primary font-bold">+ Új ügyfél rögzítése</option>
                <option v-for="c in customersList" :key="c.id" :value="c.id">{{ c.name }}</option>
              </select>
              <i class="pi pi-chevron-down absolute right-3 top-1/2 -translate-y-1/2 text-text-muted pointer-events-none text-sm"></i>
            </div>

            <div v-if="form.customerId === 'new'" class="flex flex-col gap-2">
              <input type="text" v-model="form.customerFullName" placeholder="Teljes név (opcionális)..." class="w-full h-[44px] bg-background border border-text/20 rounded-lg px-3 text-sm text-text focus:outline-none focus:border-primary">
              <input type="tel" v-model="form.customerPhone" placeholder="Telefonszám (opcionális)..." class="w-full h-[44px] bg-background border border-text/20 rounded-lg px-3 text-sm text-text focus:outline-none focus:border-primary">
              <span class="text-[10px] text-text-muted leading-tight">Legalább az egyik mező kitöltése kötelező!</span>
            </div>
            <div v-else-if="form.customerId">
              <input type="text" v-model="form.customerFullName" disabled class="w-full h-[44px] bg-background border border-text/20 rounded-lg px-3 text-sm text-text focus:outline-none focus:border-primary opacity-50 cursor-not-allowed">
            </div>
          </div>
        </div>

        <div class="grid grid-cols-2 gap-3 border-t border-text/10 pt-4">
          <div>
            <label class="block text-[10px] md:text-xs font-bold text-text-muted mb-1.5 uppercase flex items-center gap-1"><i class="pi pi-calendar"></i> Dátum</label>
            <input type="date" v-model="form.date" class="w-full h-[44px] bg-background border border-text/20 rounded-lg px-3 text-sm text-text font-bold focus:outline-none focus:border-primary">
          </div>
          <div>
            <label class="block text-[10px] md:text-xs font-bold text-text-muted mb-1.5 uppercase flex items-center gap-1"><i class="pi pi-clock"></i> Kezdés</label>
            <input type="time" v-model="form.time" class="w-full h-[44px] bg-background border border-text/20 rounded-lg px-3 text-sm text-text font-bold focus:outline-none focus:border-primary">
          </div>
        </div>

        <div class="border-t border-text/10 pt-4">
          <label class="block text-[10px] md:text-xs font-bold text-text-muted mb-2 uppercase flex items-center gap-1"><i class="pi pi-sparkles"></i> Szolgáltatások</label>

          <div class="bg-primary/5 p-3 md:p-4 rounded-xl border border-primary/20 space-y-3 mb-4">
            <h4 class="text-sm font-bold text-primary flex items-center gap-2"><i class="pi pi-plus-circle"></i> Tétel hozzáadása</h4>

            <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div class="relative">
                <select v-model="builder.category" @change="resetBuilder(1)" class="w-full h-[44px] bg-background border border-text/10 rounded-lg px-3 text-text focus:outline-none focus:border-primary appearance-none text-xs md:text-sm">
                  <option value="" disabled>1. Kategória...</option>
                  <option v-for="cat in availableCategories" :key="cat" :value="cat">{{ cat }}</option>
                </select>
                <i class="pi pi-chevron-down absolute right-3 top-1/2 -translate-y-1/2 text-text-muted text-xs pointer-events-none"></i>
              </div>

              <div class="relative">
                <select v-model="builder.serviceId" @change="resetBuilder(2)" :disabled="!builder.category" class="w-full h-[44px] bg-background border border-text/10 rounded-lg px-3 text-text focus:outline-none focus:border-primary appearance-none text-xs md:text-sm disabled:opacity-50">
                  <option value="" disabled>2. Szolgáltatás...</option>
                  <option v-for="srv in availableServicesInCategory" :key="srv.id" :value="srv.id">{{ getLocText(srv.name) }}</option>
                </select>
                <i class="pi pi-chevron-down absolute right-3 top-1/2 -translate-y-1/2 text-text-muted text-xs pointer-events-none"></i>
              </div>
            </div>

            <div v-if="builder.serviceId" class="relative">
              <select v-model="builder.variantId" @change="onVariantSelected" class="w-full h-[44px] bg-background border border-primary/30 rounded-lg px-3 text-text font-bold focus:outline-none focus:border-primary appearance-none text-xs md:text-sm">
                <option value="" disabled>3. Pontos Variáns kiválasztása...</option>
                <option v-for="v in availableVariantsInService" :key="v.id" :value="v.id">{{ getLocText(v.variantName) }} ({{ v.price }} EUR)</option>
              </select>
              <i class="pi pi-chevron-down absolute right-3 top-1/2 -translate-y-1/2 text-primary text-xs pointer-events-none"></i>
            </div>

            <div v-if="builder.variantId" class="flex flex-col md:flex-row md:items-end gap-3 pt-2">
              <div class="w-full md:w-1/3 flex flex-col">
                <label class="text-[10px] font-bold text-text-muted uppercase mb-1">Időtartam (Húzd)</label>
                <ScrubbableInput v-model="builder.duration" :min="5" :max="480" :step="5" :sensitivity="10" suffix="perc" class="h-[44px]" />
              </div>

              <div class="w-full md:w-2/3">
                <button @click="addServiceToForm" class="w-full h-[44px] bg-primary text-white font-bold rounded-lg shadow-sm hover:brightness-110 active:scale-95 transition-all flex items-center justify-center gap-2">
                  <i class="pi pi-check"></i> Listához ad
                </button>
              </div>
            </div>
          </div>

          <div v-if="form.items.length > 0" class="space-y-2 mt-2">
            <div v-for="(item, idx) in form.items" :key="idx" class="flex items-center justify-between bg-surface border border-text/10 p-2 md:p-3 rounded-lg shadow-sm">
              <div class="flex flex-col">
                <span class="font-bold text-xs md:text-sm text-text">{{ item.name }}</span>
                <span class="text-[10px] md:text-xs text-text-muted font-medium">{{ item.duration }} perc • {{ item.price }} EUR</span>
              </div>
              <button @click="removeFormItem(idx)" class="w-8 h-8 flex items-center justify-center bg-red-500/10 text-red-500 rounded-full hover:bg-red-500 hover:text-white transition-colors">
                <i class="pi pi-trash text-xs"></i>
              </button>
            </div>
          </div>
          <div v-else class="text-[10px] md:text-xs text-center py-2 text-text-muted border border-dashed border-text/10 rounded-lg">
            Még nincs szolgáltatás hozzáadva.
          </div>

        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-4 border-t border-text/10 pt-4">
          <div>
            <label class="block text-[10px] md:text-xs font-bold text-text-muted mb-2 uppercase">Állapot</label>
            <div class="flex gap-2">
              <button @click="form.status = 0" class="flex-1 h-[44px] rounded-lg font-bold text-xs md:text-sm border transition-all" :class="form.status === 0 ? 'bg-red-500/10 border-red-500 text-red-500 shadow-sm' : 'border-text/20 text-text hover:bg-text/5'">Függőben</button>
              <button @click="form.status = 1" class="flex-1 h-[44px] rounded-lg font-bold text-xs md:text-sm border transition-all" :class="form.status === 1 ? 'bg-green-500/10 border-green-500 text-green-500 shadow-sm' : 'border-text/20 text-text hover:bg-text/5'">Jóváhagyva</button>
            </div>
          </div>
          <div>
            <label class="block text-[10px] md:text-xs font-bold text-text-muted mb-1.5 uppercase">Belső Megjegyzés</label>
            <textarea v-model="form.notes" rows="2" placeholder="Opcionális feljegyzés..." class="w-full bg-background border border-text/20 rounded-lg p-2.5 text-xs md:text-sm text-text focus:outline-none focus:border-primary resize-none"></textarea>
          </div>
        </div>

      </div>

      <div class="p-3 md:p-4 border-t border-text/10 bg-background/50 flex justify-between gap-2 md:gap-3 mt-auto">
        <button v-if="isEditing" @click="handleDelete" class="px-3 md:px-4 h-[44px] text-red-500 font-bold text-sm md:text-base rounded-lg border border-red-500/30 hover:bg-red-500/10 transition-colors">
          Törlés
        </button>
        <div v-else></div>

        <div class="flex gap-2">
          <button @click="close" class="px-3 md:px-4 h-[44px] text-text text-sm md:text-base font-bold rounded-lg hover:bg-text/10 transition-colors">
            Mégsem
          </button>
          <button @click="handleSave" :disabled="!isFormValid" class="px-4 md:px-6 h-[44px] bg-primary text-white text-sm md:text-base font-bold rounded-lg hover:brightness-110 shadow-md transition-transform active:scale-95 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-1 md:gap-2">
            <i class="pi pi-save"></i> Mentés
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import bookingApi from '@/services/bookingApi';
import { useAppointmentStore } from '@/stores/appointmentStore';
import ScrubbableInput from '@/components/common/ScrubbableInput.vue';

const props = defineProps({
  isOpen: {
    type: Boolean,
    required: true
  },
  editData: {
    type: Object,
    default: null
  },
  defaultDate: {
    type: Date,
    default: () => new Date()
  }
});

const emit = defineEmits(['close', 'saved', 'deleted']);

const { locale } = useI18n();
const currentLang = computed(() => locale.value || 'hu-HU');
const store = useAppointmentStore();

const availableServices = ref([]);
const customersList = ref([]);
const isEditing = ref(false);

const form = ref({
  id: null, customerId: '', customerFullName: '', customerPhone: '', employeeId: 1,
  date: '', time: '08:00', status: 1, notes: '', items: []
});

const builder = ref({ category: '', serviceId: '', variantId: '', duration: 0 });

// API Hívások az űrlaphoz (Szolgáltatások és Ügyfelek listája)
const fetchServicesForAdmin = async () => {
  try {
    const response = await bookingApi.getPublicServices();
    const rawServices = response.data.$values || response.data || [];
    availableServices.value = rawServices.map(s => {
      const vars = s.variants?.$values || s.variants || [];
      return { ...s, variants: vars.filter(v => v.price != null && v.price > 0) };
    }).filter(s => s.variants.length > 0);
  } catch (error) { console.error('Hiba a szolgáltatások betöltésekor:', error); }
};

const fetchCustomers = async () => {
  try {
    const response = await bookingApi.getCustomers();
    customersList.value = response.data.$values || response.data || [];
  } catch (error) { console.error('Hiba az ügyfelek betöltésekor:', error); }
};

// Segédfüggvények
const getLocText = (dict) => dict ? (dict[currentLang.value] || dict['hu'] || '') : '';
const isPending = (status) => status === 0 || status === '0' || (typeof status === 'string' && status.toLowerCase() === 'pending');
const getVariantFullName = (variantId) => {
  for (const s of availableServices.value) {
    const v = s.variants?.find(vx => vx.id === variantId);
    if (v) return `${getLocText(s.name)} - ${getLocText(v.variantName)}`;
  } return 'Ismeretlen';
};

// Dinamikus listák a Builder-hez
const availableCategories = computed(() => {
  const cats = new Set();
  availableServices.value.forEach(s => cats.add(getLocText(s.category) || 'Egyéb'));
  return Array.from(cats).sort();
});

const availableServicesInCategory = computed(() => {
  return availableServices.value.filter(s => (getLocText(s.category) || 'Egyéb') === builder.value.category);
});

const availableVariantsInService = computed(() => {
  const s = availableServices.value.find(s => s.id === builder.value.serviceId);
  return s ? s.variants : [];
});

const resetBuilder = (level) => {
  if (level === 1) {
    builder.value.serviceId = ''; builder.value.variantId = ''; builder.value.duration = 0;
    const services = availableServicesInCategory.value;
    if (services.length === 1) { builder.value.serviceId = services[0].id; resetBuilder(2); }
  }
  if (level === 2) {
    builder.value.variantId = ''; builder.value.duration = 0;
    const variants = availableVariantsInService.value;
    if (variants.length === 1) { builder.value.variantId = variants[0].id; onVariantSelected(); }
  }
};

const onVariantSelected = () => {
  const v = availableVariantsInService.value.find(vx => vx.id === builder.value.variantId);
  if (v) builder.value.duration = v.duration || 30;
};

const addServiceToForm = () => {
  const s = availableServices.value.find(x => x.id === builder.value.serviceId);
  const v = availableVariantsInService.value.find(x => x.id === builder.value.variantId);
  if (s && v) {
    form.value.items.push({
      variantId: v.id, name: `${getLocText(s.name)} - ${getLocText(v.variantName)}`,
      duration: builder.value.duration, price: v.price
    });
    builder.value = { category: '', serviceId: '', variantId: '', duration: 0 };
  }
};

const removeFormItem = (idx) => form.value.items.splice(idx, 1);

const handleCustomerSelect = () => {
  if (form.value.customerId === 'new') {
    form.value.customerFullName = ''; form.value.customerPhone = '';
  } else {
    const c = customersList.value.find(x => x.id === form.value.customerId);
    if (c) form.value.customerFullName = c.name;
  }
};

// Form inicializálása új vagy szerkesztés esetén
const initForm = () => {
  if (!props.editData) {
    isEditing.value = false;
    const d = new Date(props.defaultDate.getTime() - (props.defaultDate.getTimezoneOffset() * 60000));
    form.value = { id: null, customerId: '', customerFullName: '', customerPhone: '', employeeId: 1, date: d.toISOString().split('T')[0], time: '08:00', status: 1, notes: '', items: [] };
  } else {
    isEditing.value = true;
    const app = props.editData;
    const d = new Date(app.startDateTime);
    const mappedItems = app.items?.map(i => ({
      variantId: i.serviceVariantId, name: getVariantFullName(i.serviceVariantId), duration: i.calculatedDurationMinutes || 30, price: i.price
    })) || [];
    const c = customersList.value.find(x => x.id === app.customerId);

    form.value = {
      id: app.id, customerId: app.customerId, customerFullName: c ? c.name : '', customerPhone: '', employeeId: app.employeeId,
      date: new Date(d.getTime() - (d.getTimezoneOffset() * 60000)).toISOString().split('T')[0],
      time: d.toTimeString().substring(0, 5), status: isPending(app.status) ? 0 : 1, notes: app.notes || '', items: mappedItems
    };
  }
  builder.value = { category: '', serviceId: '', variantId: '', duration: 0 };
};

const isFormValid = computed(() => {
  if (form.value.items.length === 0) return false;
  if (!form.value.customerId) return false;
  if (form.value.customerId === 'new') {
    const hasName = form.value.customerFullName && form.value.customerFullName.trim() !== '';
    const hasPhone = form.value.customerPhone && form.value.customerPhone.trim() !== '';
    if (!hasName && !hasPhone) return false;
  }
  return true;
});

  const handleSave = async (forceParam = false) => {
    // 1. JAVÍTÁS: Mivel a Vue @click átadja az Event objektumot, meg kell vizsgálnunk, 
    // hogy tényleg boolean (igaz/hamis) értéket kaptunk-e. Ha nem, akkor false.
    const isForced = typeof forceParam === 'boolean' ? forceParam : false;

    try {
      let finalCustId = form.value.customerId;
      if (form.value.customerId === 'new') {
        const nameVal = form.value.customerFullName?.trim() || '';
        const phoneVal = form.value.customerPhone?.trim() || '';
        const newCustomerResponse = await bookingApi.createCustomer({ fullName: nameVal, phone: phoneVal });
        finalCustId = newCustomerResponse.data.id;
        customersList.value.push(newCustomerResponse.data);
      } else {
        finalCustId = parseInt(form.value.customerId);
      }

      // Alap payload, ami mindkét DTO-ban szerepel
      const basePayload = {
        customerId: finalCustId,
        startDateTime: new Date(`${form.value.date}T${form.value.time}:00`).toISOString(),
        items: form.value.items.map(i => ({ serviceVariantId: parseInt(i.variantId), durationMinutes: parseInt(i.duration) })),
        status: parseInt(form.value.status),
        notes: form.value.notes,
        force: isForced
      };

      let payload;
      // 2. JAVÍTÁS: Különbséget teszünk Create és Update között a DTO-k alapján
      if (form.value.id) {
        // Ha van ID, akkor ez UPDATE -> Az UpdateAppointmentDto nem kér EmployeeId-t!
        payload = { ...basePayload };
      } else {
        // Ha nincs ID, akkor ez CREATE -> A CreateAppointmentDto kéri az EmployeeId-t!
        payload = { ...basePayload, employeeId: parseInt(form.value.employeeId) };
      }

      await store.saveAppointment(payload, form.value.id);
      emit('saved');
    } catch (error) {
      const status = error.response?.status;
      const data = error.response?.data;

      // TISZTA RENDSZERSZINTŰ ELLENŐRZÉS SZÖVEG NÉLKÜL!
      const isConflictError = status === 409 || data?.errorCode === 'OVERLAP';

      if (!isForced && isConflictError) {
        if (confirm("⚠️ Figyelem! Erre az időpontra már van aktív foglalás. Biztosan rá akarsz könyvelni (párhuzamos foglalás)?")) {
          handleSave(true);
        }
      } else {
        let errorDetails = data?.message || error.message;

        if (status === 400 && data?.errors) {
          errorDetails = Object.entries(data.errors)
            .map(([field, messages]) => `${field}: ${messages.join(', ')}`)
            .join('\n');
        }

        alert("Hiba mentéskor:\n" + errorDetails);
      }
    }
  };

const handleDelete = async () => {
  if (confirm("Biztosan véglegesen törlöd ezt a foglalást?")) {
    try {
      await store.deleteAppointment(form.value.id);
      emit('deleted');
    } catch (error) { alert("Hiba történt a törlés során."); }
  }
};

const close = () => { emit('close'); };

// Figyeljük a modal nyitását és ha nyílik, beállítjuk a formot
watch(() => props.isOpen, (newVal) => {
  if (newVal) {
    if (availableServices.value.length === 0) fetchServicesForAdmin();
    if (customersList.value.length === 0) fetchCustomers();
    initForm();
  }
});

onMounted(() => {
  fetchServicesForAdmin();
  fetchCustomers();
});
</script>
