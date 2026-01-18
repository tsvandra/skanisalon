<script setup>
  import { ref, onMounted } from 'vue';
  // Importáljuk a táblázat komponenseket a PrimeVue-ból
  import DataTable from 'primevue/datatable';
  import Column from 'primevue/column';

  const services = ref([]);
  const loading = ref(true); // Töltés állapot jelző

  onMounted(async () => {
    try {
      const response = await fetch('https://localhost:7113/api/Service?companyId=1');
      services.value = await response.json();
    } catch (error) {
      console.error('Hiba a szolgáltatások betöltésekor:', error);
    } finally {
      loading.value = false; // Akár sikerült, akár nem, a töltésnek vége
    }
  });

  // Pénznem formázó függvény (hogy szép legyen az ár)
  const formatCurrency = (value) => {

    if (value === undefined || value === null) return '0,00';
    return value.toLocaleString('hu-HU', { style: 'currency', currency: 'EUR' });
  };
</script>

<template>
  <div class="card" style="max-width: 800px; margin: 0 auto; padding: 20px;">

    <h2 style="margin-bottom: 20px; color: #333;">Szolgáltatásaink</h2>

    <DataTable :value="services" :loading="loading" showGridlines stripedRows tableStyle="min-width: 50rem">

      <template #empty>
        Nincsenek elérhető szolgáltatások.
      </template>

      <Column field="name" header="Megnevezés" sortable></Column>

      <Column field="defaultPrice" header="Ár" sortable style="font-weight: bold;">
        <template #body="slotProps">
          {{ formatCurrency(slotProps.data.defaultPrice) }}
        </template>
      </Column>

    </DataTable>

  </div>
</template>
