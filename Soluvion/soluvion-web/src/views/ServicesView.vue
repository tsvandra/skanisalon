<script setup>
  import { ref, onMounted } from 'vue';
  import DataTable from 'primevue/datatable';
  import Column from 'primevue/column';

  const services = ref([]);
  const loading = ref(true);
  const isLoggedIn = ref(false); // Admin státusz

  // Új szolgáltatás adatai (űrlaphoz)
  const newService = ref({
    name: '',
    defaultPrice: 0
  });

  const API_URL = 'https://localhost:7113/api/Service'; // Ellenőrizd a portot!

  // Betöltés
  const fetchServices = async () => {
    loading.value = true;
    try {
      // Itt fixen az 1-es cég árait kérjük le (később ez dinamikus lesz a domain alapján)
      const response = await fetch(`${API_URL}?companyId=1`);
      services.value = await response.json();
    } catch (error) {
      console.error('Hiba:', error);
    } finally {
      loading.value = false;
    }
  };

  // Új szolgáltatás mentése
  const addService = async () => {
    if (!newService.value.name || newService.value.defaultPrice <= 0) {
      alert("Kérlek adj meg nevet és árat!");
      return;
    }

    try {
      const token = localStorage.getItem('salon_token');
      const res = await fetch(API_URL, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}` // Küldjük a kulcsot
        },
        body: JSON.stringify(newService.value)
      });

      if (res.ok) {
        // Siker! Ürítjük az űrlapot és frissítünk
        newService.value = { name: '', defaultPrice: 0 };
        await fetchServices();
      } else {
        alert("Hiba a mentésnél!");
      }
    } catch (err) {
      console.error(err);
    }
  };

  // Törlés
  const deleteService = async (id) => {
    if (!confirm("Biztosan törölni akarod?")) return;

    try {
      const token = localStorage.getItem('salon_token');
      const res = await fetch(`${API_URL}/${id}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });

      if (res.ok) {
        await fetchServices();
      }
    } catch (err) {
      console.error(err);
    }
  };

  // Pénznem formázó
  const formatCurrency = (value) => {
    if (value === undefined || value === null) return '0,00';
    return value.toLocaleString('hu-HU', { style: 'currency', currency: 'EUR' });
  };

  onMounted(() => {
    // Megnézzük, be van-e lépve
    const token = localStorage.getItem('salon_token');
    isLoggedIn.value = !!token;

    fetchServices();
  });
</script>

<template>
  <div class="card service-container">
    <h2 class="title">Szolgáltatásaink</h2>

    <div v-if="isLoggedIn" class="admin-panel">
      <h3>Új szolgáltatás hozzáadása</h3>
      <div class="form-row">
        <input v-model="newService.name" placeholder="Szolgáltatás neve (pl. Női vágás)" />
        <input v-model.number="newService.defaultPrice" type="number" placeholder="Ár (€)" />
        <button @click="addService">Hozzáadás</button>
      </div>
    </div>

    <DataTable :value="services" :loading="loading" showGridlines stripedRows tableStyle="min-width: 50rem">
      <template #empty>
        Nincsenek elérhető szolgáltatások.
      </template>

      <Column field="name" header="Megnevezés" sortable></Column>

      <Column field="defaultPrice" header="Ár" sortable style="font-weight: bold; width: 150px;">
        <template #body="slotProps">
          {{ formatCurrency(slotProps.data.defaultPrice) }}
        </template>
      </Column>

      <Column v-if="isLoggedIn" header="Művelet" style="width: 100px; text-align: center;">
        <template #body="slotProps">
          <button class="delete-btn" @click="deleteService(slotProps.data.id)">Törlés</button>
        </template>
      </Column>

    </DataTable>
  </div>
</template>

<style scoped>
  .service-container {
    max-width: 900px;
    margin: 0 auto;
    padding: 2rem;
  }

  .title {
    margin-bottom: 2rem;
    color: #333;
    text-align: center;
  }

  /* Admin panel stílusa */
  .admin-panel {
    background-color: #f8f9fa;
    border: 1px dashed #d4af37;
    padding: 1.5rem;
    margin-bottom: 2rem;
    border-radius: 8px;
  }

    .admin-panel h3 {
      margin-top: 0;
      color: #d4af37;
      font-size: 1.1rem;
    }

  .form-row {
    display: flex;
    gap: 10px;
    flex-wrap: wrap;
  }

    .form-row input {
      padding: 10px;
      border: 1px solid #ddd;
      border-radius: 4px;
      flex: 1;
    }

    .form-row button {
      background-color: #d4af37;
      color: white;
      border: none;
      padding: 10px 20px;
      border-radius: 4px;
      cursor: pointer;
      font-weight: bold;
    }

      .form-row button:hover {
        background-color: #b5952f;
      }

  /* Törlés gomb */
  .delete-btn {
    background-color: #ff4444;
    color: white;
    border: none;
    padding: 5px 10px;
    border-radius: 4px;
    cursor: pointer;
    font-size: 0.8rem;
  }

    .delete-btn:hover {
      background-color: #cc0000;
    }
</style>
