<template>
  <div class="flex flex-col h-full bg-surface p-4">
    <div class="flex justify-between items-center mb-4">
      <h2 class="text-xl font-bold text-text">Napi Naptár</h2>
      <div class="flex gap-2 items-center">
        <span class="text-sm text-text-muted">Szerepkör: <strong class="text-primary">{{ store.userRole }}</strong></span>
      </div>
    </div>

    <div class="flex-1 bg-background rounded-lg shadow overflow-y-auto">
      <div class="grid grid-cols-[80px_1fr] border-b border-gray-200" v-for="time in timeSlots" :key="time">

        <div class="p-2 text-right text-sm text-text-muted border-r border-gray-200">
          {{ time }}
        </div>

        <draggable v-model="groupedAppointments[time]"
                   group="appointments"
                   item-key="id"
                   class="p-2 min-h-[60px] relative transition-colors duration-200"
                   ghost-class="opacity-50"
                   @change="onAppointmentMove($event, time)">
          <template #item="{ element }">
            <div class="bg-primary text-white p-2 rounded shadow-sm text-xs cursor-move mb-1">
              <strong>Vendég ID: {{ element.customerId }}</strong><br>
              {{ formatTime(element.startDateTime) }} - {{ formatTime(element.endDateTime) }}
            </div>
          </template>
        </draggable>

      </div>
    </div>

    <div v-if="showForceModal" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div class="bg-white p-6 rounded-lg max-w-md w-full shadow-xl">
        <h3 class="text-lg font-bold text-red-600 mb-2">Időpont Ütközés!</h3>
        <p class="text-text mb-4">
          A kiválasztott időpont ütközik egy már létező foglalással.
        </p>
        <p v-if="store.canForceOverlap" class="text-sm text-text-muted mb-4">
          Mivel te {{ store.userRole }} vagy, engedélyezheted a kényszerített mentést. Biztosan ütközteted?
        </p>
        <p v-else class="text-sm text-red-500 mb-4 font-bold">
          Nincs jogosultságod ütköző időpontot rögzíteni. Kérlek válassz másik időpontot!
        </p>

        <div class="flex justify-end gap-2">
          <button @click="cancelMove" class="px-4 py-2 bg-gray-200 text-gray-800 rounded hover:bg-gray-300 transition">
            Mégsem
          </button>
          <button v-if="store.canForceOverlap" @click="confirmForceMove" class="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700 transition">
            Igen, mentés kényszerítése
          </button>
        </div>
      </div>
    </div>

  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import draggable from 'vuedraggable';
import { useAppointmentStore } from '@/stores/appointmentStore';

const store = useAppointmentStore();

// UI Állapotok
const showForceModal = ref(false);
const pendingMoveEvent = ref(null);

// Napi idősávok generálása (8:00 - 20:00, óránként a példa kedvéért)
const timeSlots = computed(() => {
  const slots = [];
  for (let i = 8; i <= 20; i++) {
    slots.push(`${i.toString().padStart(2, '0')}:00`);
  }
  return slots;
});

// A naptár felbontása sávokra
const groupedAppointments = computed(() => {
  const groups = {};
  timeSlots.value.forEach(t => groups[t] = []);

  store.appointments.forEach(app => {
    const date = new Date(app.startDateTime);
    const hour = `${date.getHours().toString().padStart(2, '0')}:00`;
    if (groups[hour]) {
      groups[hour].push(app);
    }
  });
  return groups;
});

// Inicializálás
onMounted(() => {
  store.initUserPermissions();
  // Ideiglenes teszt adatok bekérése
  const today = new Date();
  const tomorrow = new Date(today);
  tomorrow.setDate(tomorrow.getDate() + 1);
  store.fetchAppointments(today, tomorrow);
});

// Draggable Drop esemény
const onAppointmentMove = async (event, newTime) => {
  if (event.added) {
    const appointment = event.added.element;

    // Új dátum kiszámolása (a mai napra rakjuk a példa kedvéért, az óra:perc a drop zónából)
    const [hours, minutes] = newTime.split(':');
    const newStartDate = new Date(appointment.startDateTime);
    newStartDate.setHours(parseInt(hours), parseInt(minutes), 0);

    const updateData = {
      startDateTime: newStartDate.toISOString(),
      serviceVariantIds: [1], // Ezt a valóságban az item-ből olvassuk ki
      status: 1, // Confirmed
      force: false
    };

    try {
      await store.saveAppointment(updateData, appointment.id);
      await store.fetchAppointments(new Date(), new Date(new Date().setDate(new Date().getDate() + 1)));
    } catch (err) {
      if (err.response?.status === 400 && err.response.data.includes('ütközik')) {
        // Ütközés történt! Megnyitjuk a Modalt.
        pendingMoveEvent.value = { id: appointment.id, data: updateData };
        showForceModal.value = true;
      }
    }
  }
};

const cancelMove = async () => {
  showForceModal.value = false;
  pendingMoveEvent.value = null;
  // Visszaállítjuk a rácsot az eredeti állapotra
  const today = new Date();
  await store.fetchAppointments(today, new Date(today.getTime() + 86400000));
};

const confirmForceMove = async () => {
  if (pendingMoveEvent.value && store.canForceOverlap) {
    try {
      // Újraküldjük a kérést Force = true paraméterrel
      pendingMoveEvent.value.data.force = true;
      await store.saveAppointment(pendingMoveEvent.value.data, pendingMoveEvent.value.id);
      showForceModal.value = false;
      const today = new Date();
      await store.fetchAppointments(today, new Date(today.getTime() + 86400000));
    } catch (err) {
      console.error("Még force-szal is elszállt:", err);
    }
  }
};

const formatTime = (isoString) => {
  const date = new Date(isoString);
  return date.toLocaleTimeString('hu-HU', { hour: '2-digit', minute: '2-digit' });
};
</script>
