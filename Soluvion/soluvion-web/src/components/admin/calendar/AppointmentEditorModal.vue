<template>
  <div v-if="isOpen" class="fixed inset-0 bg-black/60 backdrop-blur-sm z-50 flex items-center justify-center p-2 md:p-4" @click="closeAllDropdowns">
    <div class="bg-surface w-full max-w-2xl rounded-2xl shadow-2xl overflow-hidden border border-text/10 flex flex-col max-h-[95vh] md:max-h-[90vh]" @click.stop>

      <div class="p-4 md:p-6 border-b border-text/10 flex justify-between items-center bg-background/50">
        <h2 class="text-lg md:text-xl font-bold text-text flex items-center gap-2">
          <i class="pi" :class="isEditing ? 'pi-pencil text-primary' : 'pi-plus-circle text-primary'"></i>
          {{ isEditing ? $t('calendar.editor.editTitle') : $t('calendar.editor.newTitle') }}
        </h2>
        <button @click="close" class="w-8 h-8 flex items-center justify-center rounded-full hover:bg-text/10 text-text-muted transition-colors">
          <i class="pi pi-times"></i>
        </button>
      </div>

      <div class="p-4 md:p-6 overflow-y-auto space-y-6">

        <CustomerPicker v-model="form.customerId"
                        v-model:customerFullName="form.customerFullName"
                        v-model:customerPhone="form.customerPhone"
                        :customersList="customersList" />

        <div class="grid grid-cols-2 gap-3 border-t border-text/10 pt-4">
          <div>
            <label class="block text-[10px] md:text-xs font-bold text-text-muted mb-1.5 uppercase flex items-center gap-1"><i class="pi pi-calendar"></i> {{ $t('calendar.editor.date') }}</label>
            <input type="date" v-model="form.date" class="w-full h-[44px] bg-background border border-text/20 rounded-lg px-3 text-sm text-text font-bold focus:outline-none focus:border-primary">
          </div>
          <div>
            <label class="block text-[10px] md:text-xs font-bold text-text-muted mb-1.5 uppercase flex items-center gap-1"><i class="pi pi-clock"></i> {{ $t('calendar.editor.startTime') }}</label>
            <input type="time" v-model="form.time" class="w-full h-[44px] bg-background border border-text/20 rounded-lg px-3 text-sm text-text font-bold focus:outline-none focus:border-primary">
          </div>
        </div>

        <div class="border-t border-text/10 pt-4">
          <label class="block text-[10px] md:text-xs font-bold text-text-muted mb-2 uppercase flex items-center gap-1"><i class="pi pi-list"></i> {{ $t('calendar.editor.services') }}</label>

          <ServicePicker :available-services="availableServices"
                         v-model:open-dropdown-id="openDropdownId"
                         @add-items="handleNewItems"
                         class="mb-4" />

          <AppointmentCart :items="form.items"
                           @remove="removeFormItem" />
        </div>

        <div class="border-t border-text/10 pt-4">
          <label class="block text-[10px] md:text-xs font-bold text-text-muted mb-2 uppercase flex items-center gap-1"><i class="pi pi-box"></i> {{ $t('orders.calendar.editor.extraMaterials') }}</label>
          <div class="bg-background border border-text/10 rounded-xl p-3 flex flex-col gap-2 mb-4">
            <div v-for="(em, idx) in form.extraMaterials" :key="idx" class="flex justify-between items-center bg-surface p-2 rounded-lg border border-text/5">
              <div class="flex flex-col">
                <span class="text-xs font-bold text-text">{{ em.name }}</span>
                <span v-if="em.saveAsDefault" class="text-[10px] text-green-500 font-bold"><i class="pi pi-check"></i> {{ $t('orders.calendar.editor.savedAsCustomerDefault') }}</span>
              </div>
              <div class="flex items-center gap-3">
                <span class="text-sm font-black text-orange-500">{{ em.quantity }} {{ em.unit || $t('orders.calendar.editor.unitPiece') }}</span>
                <button @click="removeExtraProduct(idx)" class="text-red-500 hover:bg-red-500/10 w-6 h-6 rounded flex items-center justify-center transition-colors"><i class="pi pi-trash text-xs"></i></button>
              </div>
            </div>
            <div v-if="showAddExtraProduct" class="bg-surface p-3 rounded-lg border border-primary/20 flex flex-col gap-3 mt-2">
              <select v-model="newExtraProduct" class="w-full h-[40px] bg-background border border-text/20 rounded-lg px-2 text-sm font-bold text-text">
                <option :value="null" disabled>{{ $t('orders.calendar.editor.selectProduct') }}</option>
                <option v-for="p in allProducts" :key="p.id" :value="p.id">{{ $t('orders.calendar.editor.productInStock', { name: p.name, stock: p.currentStock, unit: p.unit || $t('orders.calendar.editor.unitPiece') }) }}</option>
              </select>
              <div class="flex items-center gap-2">
                <input type="number" v-model="newExtraQty" class="w-20 h-[40px] bg-background border border-text/20 rounded-lg px-2 text-sm text-center font-bold text-text" min="1">
                <span class="text-xs text-text-muted font-bold">{{ newExtraProduct ? (allProducts.find(p => p.id === newExtraProduct)?.unit || $t('orders.calendar.editor.unitPiece')) : $t('orders.calendar.editor.quantity') }}</span>
              </div>
              <label class="flex items-center gap-2 cursor-pointer mt-1">
                <input type="checkbox" v-model="newExtraSaveDefault" class="w-4 h-4 text-primary rounded border-text/30 focus:ring-primary">
                <span class="text-xs font-bold text-text">{{ $t('orders.calendar.editor.addProductToCustomerDefaults') }}</span>
              </label>
              <div class="flex justify-end gap-2 mt-2">
                <button @click="showAddExtraProduct = false" class="px-3 h-[32px] rounded-lg text-xs font-bold text-text hover:bg-text/10">{{ $t('common.cancel') }}</button>
                <button @click="addExtraProduct" :disabled="!newExtraProduct" class="px-4 h-[32px] rounded-lg text-xs font-bold bg-primary text-white hover:brightness-110 disabled:opacity-50">{{ $t('orders.calendar.editor.add') }}</button>
              </div>
            </div>
            <button v-else @click="showAddExtraProduct = true" class="text-xs font-bold text-primary hover:brightness-110 flex items-center justify-center gap-1 py-2 border border-dashed border-primary/30 rounded-lg hover:bg-primary/5 transition-colors">
              <i class="pi pi-plus"></i> {{ $t('orders.calendar.editor.addNewMaterial') }}
            </button>
          </div>
        </div>

<div class="grid grid-cols-1 md:grid-cols-2 gap-4 border-t border-text/10 pt-4">
          <div>
            <label class="block text-[10px] md:text-xs font-bold text-text-muted mb-2 uppercase">{{ $t('calendar.editor.status') }}</label>
            <div class="flex flex-wrap gap-2">
              <button @click="form.status = 0" class="flex-1 h-[44px] rounded-lg font-bold text-xs md:text-sm border transition-all" :class="form.status === 0 ? 'bg-red-500/10 border-red-500 text-red-500 shadow-sm' : 'border-text/20 text-text hover:bg-text/5'">{{ $t('calendar.editor.statusPending') }}</button>
              <button @click="form.status = 1" class="flex-1 h-[44px] rounded-lg font-bold text-xs md:text-sm border transition-all" :class="form.status === 1 ? 'bg-green-500/10 border-green-500 text-green-500 shadow-sm' : 'border-text/20 text-text hover:bg-text/5'">{{ $t('calendar.editor.statusApproved') }}</button>
              <button @click="form.status = 2" class="flex-1 h-[44px] rounded-lg font-bold text-xs md:text-sm border transition-all" :class="form.status === 2 ? 'bg-primary/10 border-primary text-primary shadow-sm' : 'border-text/20 text-text hover:bg-text/5'">{{ $t('orders.calendar.editor.statusCompleted') }}</button>
              <button @click="form.status = 4" class="flex-1 h-[44px] px-2 rounded-lg font-bold text-xs md:text-sm border transition-all whitespace-nowrap" :class="form.status === 4 ? 'bg-gray-500/10 border-gray-500 text-gray-500 shadow-sm' : 'border-text/20 text-text hover:bg-text/5'">{{ $t('calendar.editor.statusNoShow') }}</button>
            </div>
          </div>
          <div>
            <label class="block text-[10px] md:text-xs font-bold text-text-muted mb-1.5 uppercase">{{ $t('calendar.editor.internalNote') }}</label>
            <textarea v-model="form.notes" rows="2" :placeholder="$t('calendar.editor.notePlaceholder')" class="w-full bg-background border border-text/20 rounded-lg p-2.5 text-xs md:text-sm text-text focus:outline-none focus:border-primary resize-none"></textarea>
          </div>
        </div>

      </div>

      <div class="p-3 md:p-4 border-t border-text/10 bg-background/50 flex justify-between gap-2 md:gap-3 mt-auto">
        <div v-if="isEditing" class="flex gap-2">
          <button @click="handleDelete" class="px-3 md:px-4 h-[44px] text-red-500 font-bold text-sm md:text-base rounded-lg border border-red-500/30 hover:bg-red-500/10 transition-colors">
            {{ $t('common.delete') }}
          </button>
          <button v-if="editData?.materialUsageRecorded" @click="handleReverseClosing" class="px-3 md:px-4 h-[44px] text-orange-500 font-bold text-sm md:text-base rounded-lg border border-orange-500/30 hover:bg-orange-500/10 transition-colors flex items-center gap-1 md:gap-2">
            <i class="pi pi-undo"></i> {{ $t('calendar.editor.reverseClosing') }}
          </button>
        </div>
        <div v-else></div>

        <div class="flex gap-2">
          <button @click="close" class="px-3 md:px-4 h-[44px] text-text text-sm md:text-base font-bold rounded-lg hover:bg-text/10 transition-colors">
            {{ $t('common.cancel') }}
          </button>
          <button @click="handleSave()" :disabled="!isFormValid || isSaving" class="px-4 md:px-6 h-[44px] bg-primary text-white text-sm md:text-base font-bold rounded-lg hover:brightness-110 shadow-md transition-transform active:scale-95 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-1 md:gap-2">
            <i class="pi" :class="isSaving ? 'pi-spin pi-spinner' : 'pi-save'"></i> {{ $t('common.save') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, computed, onMounted, watch } from 'vue';
  import { useI18n } from 'vue-i18n';
  import { useLocalizedText } from '@/composables/useLocalizedText';
  import bookingApi from '@/services/bookingApi';
import productApi from '@/services/productApi';
import apiClient from '@/services/api';
import inventoryApi from '@/services/inventoryApi';
  import { useAppointmentStore } from '@/stores/appointmentStore';

  // KISZERVEZETT KOMPONENSEK BEHÚZÁSA
  import CustomerPicker from './CustomerPicker.vue';
  import ServicePicker from './ServicePicker.vue';
  import AppointmentCart from './AppointmentCart.vue';

  const props = defineProps({
    isOpen: { type: Boolean, required: true },
    editData: { type: Object, default: null },
    defaultDate: { type: Date, default: () => new Date() }
  });

  const emit = defineEmits(['close', 'saved', 'deleted']);

  const { t, locale } = useI18n();
  const currentLang = computed(() => locale.value || 'hu-HU');
  const store = useAppointmentStore();

  const availableServices = ref([]);
  const customersList = ref([]);
  const isEditing = ref(false);
  const allProducts = ref([]);
  const showAddExtraProduct = ref(false);
  const newExtraProduct = ref(null);
  const newExtraQty = ref(1);
  const newExtraSaveDefault = ref(false);

  const openDropdownId = ref(null); // ServicePicker vezérli ezen keresztül

  const form = ref({
    id: null, customerId: '', customerFullName: '', customerPhone: '', employeeId: 0,
    date: '', time: '08:00', status: 1, notes: '',
  extraMaterials: [], items: []
  });

  const closeAllDropdowns = () => {
    openDropdownId.value = null;
  };

  // Kosár logikák
  const addExtraProduct = () => {
  if (newExtraProduct.value) {
    const prod = allProducts.value.find(p => p.id === newExtraProduct.value);
    form.value.extraMaterials.push({ productId: prod.id, name: prod.name, quantity: newExtraQty.value, unit: prod.unit, saveAsDefault: newExtraSaveDefault.value });
    newExtraProduct.value = null; newExtraQty.value = 1; newExtraSaveDefault.value = false; showAddExtraProduct.value = false;
  }
};
const removeExtraProduct = (idx) => { form.value.extraMaterials.splice(idx, 1); };
  const handleNewItems = (newItems) => {
    form.value.items.push(...newItems);
  };

  const removeFormItem = (idx) => form.value.items.splice(idx, 1);


  // --- Adatbetöltés és Init ---
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

  const isPending = (status) => status === 0 || status === '0' || (typeof status === 'string' && status.toLowerCase() === 'pending');
  const isNoShow = (status) => status === 4 || status === '4' || (typeof status === 'string' && status.toLowerCase() === 'noshow');
  const { getLocText } = useLocalizedText();
  const getVariantFullName = (variantId) => {
    for (const s of availableServices.value) {
      const v = s.variants?.find(vx => vx.id === variantId);
      if (v) return `${getLocText(s.name)} - ${getLocText(v.variantName)}`;
    } return t('calendar.unknown');
  };

  const initForm = () => {
    if (!props.editData) {
      isEditing.value = false;
      const d = new Date(props.defaultDate.getTime() - (props.defaultDate.getTimezoneOffset() * 60000));
      form.value = { id: null, customerId: '', customerFullName: '', customerPhone: '', employeeId: 0, date: d.toISOString().split('T')[0], time: '08:00', status: 1, notes: '', extraMaterials: [], items: [] };
    } else {
      isEditing.value = true;
      const app = props.editData;
      const d = new Date(app.startDateTime);
      const mappedItems = app.items?.map(i => ({
        variantId: i.serviceVariantId, name: getVariantFullName(i.serviceVariantId), duration: i.calculatedDurationMinutes || 30, price: i.price
      })) || [];
      const c = customersList.value.find(x => x.id === app.customerId);

      let existingExtras = [];
      try {
        const raw = typeof app.extraMaterials === 'string' ? JSON.parse(app.extraMaterials) : app.extraMaterials;
        if (Array.isArray(raw)) {
          existingExtras = raw.map(em => ({
            productId: em.productId,
            name: em.name || allProducts.value.find(p => p.id === em.productId)?.name || t('orders.calendar.editor.unknownProduct'),
            quantity: em.quantity,
            unit: allProducts.value.find(p => p.id === em.productId)?.unit,
            saveAsDefault: false
          }));
        }
      } catch (e) { existingExtras = []; }

      form.value = {
        id: app.id, customerId: app.customerId, customerFullName: c ? c.name : '', customerPhone: '', employeeId: app.employeeId,
        date: new Date(d.getTime() - (d.getTimezoneOffset() * 60000)).toISOString().split('T')[0],
        time: d.toTimeString().substring(0, 5), status: (app.status === 'Completed' || app.status === 2 || app.status === '2') ? 2 : (isNoShow(app.status) ? 4 : (isPending(app.status) ? 0 : 1)), notes: app.notes || '', extraMaterials: existingExtras, items: mappedItems
      };
    }
    openDropdownId.value = null;
  };

  const isFormValid = computed(() => {
    if (!isEditing.value && form.value.items.length === 0) return false;
    if (!form.value.customerId) return false;
    if (form.value.customerId === 'new') {
      const hasName = form.value.customerFullName && form.value.customerFullName.trim() !== '';
      const hasPhone = form.value.customerPhone && form.value.customerPhone.trim() !== '';
      if (!hasName && !hasPhone) return false;
    }
    return true;
  });

  const isSaving = ref(false);

  const handleSave = async (forceParam = false) => {
    const isForced = typeof forceParam === 'boolean' ? forceParam : false;

    // Dupla kattintás / párhuzamos mentés elleni védelem
    if (isSaving.value) return;
    isSaving.value = true;
    let retryForced = false;

    try {
      let finalCustId = form.value.customerId;
      if (form.value.customerId === 'new') {
        const nameVal = form.value.customerFullName?.trim() || '';
        const phoneVal = form.value.customerPhone?.trim() || '';
        const newCustomerResponse = await bookingApi.createCustomer({ fullName: nameVal, phone: phoneVal });
        finalCustId = newCustomerResponse.data.id;
        customersList.value.push(newCustomerResponse.data);
        // Ha a mentés később hibázik, újrapróbálkozáskor ne hozzunk létre még egy ügyfelet
        form.value.customerId = finalCustId;
      } else {
        finalCustId = parseInt(form.value.customerId);
      }

            // Update customer default formulas if requested
      const defaultsToAdd = form.value.extraMaterials.filter(em => em.saveAsDefault);
      if (defaultsToAdd.length > 0 && finalCustId && finalCustId !== 'new') {
        try {
          const custRes = await bookingApi.getCustomerById(finalCustId);
          const customer = custRes.data;
          let formula = [];
          if (customer.attributes && customer.attributes.FormulaList) {
             try { formula = JSON.parse(customer.attributes.FormulaList); } catch(e){}
          }
          defaultsToAdd.forEach(d => {
             const existing = formula.find(f => f.productId === d.productId);
             if (existing) { existing.quantity += d.quantity; }
             else { formula.push({ productId: d.productId, quantity: d.quantity, notes: 'Hozzáadva foglalás szerkesztésből' }); }
          });
          if (!customer.attributes) customer.attributes = {};
          customer.attributes.FormulaList = JSON.stringify(formula);
          await apiClient.put('/api/CompanyAttributes/customer-attributes/' + customer.id, customer.attributes);
        } catch(err) {
          console.error('Nem sikerült menteni az ügyfél alapértelmezett anyagait', err);
        }
      }

      const basePayload = {
        extraMaterials: JSON.stringify(form.value.extraMaterials.map(em => ({ productId: em.productId, quantity: em.quantity, name: em.name }))),
        customerId: finalCustId,
        startDateTime: new Date(`${form.value.date}T${form.value.time}:00`).toISOString(),
        items: form.value.items.map(i => ({ serviceVariantId: parseInt(i.variantId), durationMinutes: parseInt(i.duration) })),
        status: parseInt(form.value.status),
        notes: form.value.notes,
        force: isForced
      };

      let payload;
      if (form.value.id) {
        payload = { ...basePayload };
      } else {
        payload = { ...basePayload, employeeId: parseInt(form.value.employeeId) };
      }

      await store.saveAppointment(payload, form.value.id);
      emit('saved');
    } catch (error) {
      const status = error.response?.status;
      const data = error.response?.data;
      const isConflictError = status === 409 || data?.errorCode === 'OVERLAP';

      if (!isForced && isConflictError) {
        if (confirm(t('calendar.editor.overlapWarning'))) {
          retryForced = true;
        }
      } else {
        let errorDetails = data?.message || error.message;
        if (status === 400 && data?.errors) {
          errorDetails = Object.entries(data.errors)
            .map(([field, messages]) => `${field}: ${messages.join(', ')}`)
            .join('\n');
        }
        alert(t('calendar.editor.saveError') + "\n" + errorDetails);
      }
    } finally {
      isSaving.value = false;
    }

    if (retryForced) await handleSave(true);
  };

  const handleDelete = async () => {
    if (confirm(t('calendar.editor.confirmDelete'))) {
      try {
        await store.deleteAppointment(form.value.id);
        emit('deleted');
      } catch (error) { alert(t('calendar.editor.deleteError')); }
    }
  };

  const handleReverseClosing = async () => {
    if (!confirm(t('calendar.editor.confirmReverseClosing'))) return;
    try {
      await inventoryApi.reverseAppointmentClosing(form.value.id);
      emit('saved');
    } catch (error) {
      const data = error.response?.data;
      alert(t('calendar.editor.reverseClosingError') + '\n' + (data?.Error || data?.error || error.message));
    }
  };

  const close = () => { emit('close'); };

  watch(() => props.isOpen, (newVal) => {
    if (newVal) {
      if (availableServices.value.length === 0) fetchServicesForAdmin();
      if (customersList.value.length === 0) fetchCustomers();
      initForm();
    }
  });

  onMounted(() => {
    productApi.getAllProducts().then(res => allProducts.value = res.data || []).catch(e => console.error(e));
    fetchServicesForAdmin();
    fetchCustomers();
  });
</script>
