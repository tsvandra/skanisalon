<script setup>
  import { ref, onMounted } from 'vue';
  import api from '@/services/api';

  // --- VÁLTOZÓK DEFINIÁLÁSA (Ez hiányzott az 'isSaving' hibához) ---
  const isLoading = ref(false);
  const isSaving = ref(false); // <--- EZ A HIÁNYZÓ SOR!
  const message = ref('');
  const errorMsg = ref('');

  // Az űrlap adatai
  const form = ref({
    name: '',
    email: '',
    phone: '',
    address: '',
    primaryColor: '#d4af37',
    secondaryColor: '#1a1a1a',
    logoUrl: ''
  });

  // Token dekódoló segédfüggvény
  const getCompanyIdFromToken = () => {
    try {
      const token = localStorage.getItem('salon_token');
      if (!token) return null;
      const decoded = JSON.parse(atob(token.split('.')[1]));
      return decoded?.CompanyId || decoded?.companyId;
    } catch (e) {
      return null;
    }
  };

  // 1. ADATOK BETÖLTÉSE
  const loadCompanyData = async () => {
    const companyId = getCompanyIdFromToken();
    if (!companyId) {
      errorMsg.value = "Nem található cégazonosító. Jelentkezz be újra!";
      return;
    }

    isLoading.value = true;
    try {
      // --- JAVÍTÁS: A 'getCompanyDetails' HELYETT EZ KELL: ---
      const res = await api.get(`/api/Company/${companyId}`);

      // Adatok betöltése az űrlapba
      // A '...' spread operatorral másoljuk, hogy ne legyen referencia hiba
      form.value = { ...res.data };

    } catch (err) {
      console.error("Hiba a betöltéskor:", err);
      errorMsg.value = "Nem sikerült betölteni a cég adatait.";
    } finally {
      isLoading.value = false;
    }
  };

  // 2. MENTÉS
  const saveSettings = async () => {
    const companyId = getCompanyIdFromToken();
    if (!companyId) return;

    isSaving.value = true; // Most már definiálva van, nem lesz ReferenceError
    message.value = '';
    errorMsg.value = '';

    try {
      await api.put(`/api/Company/${companyId}`, form.value);

      message.value = "Sikeres mentés!";

      // Azonnali színfrissítés
      if (form.value.primaryColor) {
        document.documentElement.style.setProperty('--primary-color', form.value.primaryColor);
        document.documentElement.style.setProperty('--secondary-color', form.value.secondaryColor);
      }

      // Oldal frissítése (hogy a fejléc is frissüljön)
      setTimeout(() => window.location.reload(), 1000);

    } catch (err) {
      console.error("Mentési hiba:", err);
      errorMsg.value = "Hiba történt a mentés során.";
    } finally {
      isSaving.value = false;
    }
  };

  onMounted(() => {
    loadCompanyData();
  });
</script>

<template>
  <div class="settings-container">
    <h2>Szalon Beállítások</h2>

    <div v-if="isLoading" class="loading-box">
      Adatok betöltése...
    </div>

    <div v-else-if="errorMsg" class="error-box">{{ errorMsg }}</div>

    <form v-else @submit.prevent="saveSettings" class="settings-form">

      <div class="form-group">
        <label>Szalon neve</label>
        <input v-model="form.name" type="text" required />
      </div>

      <div class="form-group">
        <label>Email cím</label>
        <input v-model="form.email" type="email" />
      </div>

      <div class="form-group">
        <label>Telefonszám</label>
        <input v-model="form.phone" type="text" />
      </div>

      <div class="form-group">
        <label>Cím</label>
        <input v-model="form.address" type="text" />
      </div>

      <div class="color-section">
        <h3>Színek testreszabása</h3>
        <div class="color-row">
          <div class="form-group color-group">
            <label>Fő szín</label>
            <div class="color-wrapper">
              <input v-model="form.primaryColor" type="color" />
              <span>{{ form.primaryColor }}</span>
            </div>
          </div>

          <div class="form-group color-group">
            <label>Másodlagos szín</label>
            <div class="color-wrapper">
              <input v-model="form.secondaryColor" type="color" />
              <span>{{ form.secondaryColor }}</span>
            </div>
          </div>
        </div>
      </div>

      <div v-if="message" class="success-msg">{{ message }}</div>

      <button type="submit" :disabled="isSaving" class="save-btn">
        {{ isSaving ? 'Mentés folyamatban...' : 'Mentés' }}
      </button>
    </form>
  </div>
</template>

<style scoped>
  .settings-container {
    max-width: 600px;
    margin: 2rem auto;
    padding: 2rem;
    background: white;
    border-radius: 12px;
    box-shadow: 0 4px 20px rgba(0,0,0,0.08);
    border-top: 4px solid var(--primary-color);
  }

  h2 {
    text-align: center;
    margin-bottom: 2rem;
    color: #333;
  }

  h3 {
    margin-top: 0;
    font-size: 1.1rem;
    color: #666;
  }

  .loading-box {
    text-align: center;
    padding: 2rem;
    color: #666;
  }

  .form-group {
    margin-bottom: 1.2rem;
  }

  label {
    display: block;
    margin-bottom: 0.5rem;
    font-weight: 600;
    color: #444;
    font-size: 0.95rem;
  }

  input[type="text"],
  input[type="email"] {
    width: 100%;
    padding: 12px;
    border: 1px solid #ddd;
    border-radius: 6px;
    font-size: 1rem;
    transition: border-color 0.3s;
  }

  input:focus {
    outline: none;
    border-color: var(--primary-color);
  }

  .color-section {
    background: #f9f9f9;
    padding: 1.5rem;
    border-radius: 8px;
    margin-bottom: 1.5rem;
  }

  .color-row {
    display: flex;
    gap: 2rem;
  }

  .color-group {
    flex: 1;
  }

  .color-wrapper {
    display: flex;
    align-items: center;
    gap: 10px;
    background: white;
    padding: 5px;
    border: 1px solid #ddd;
    border-radius: 6px;
  }

  input[type="color"] {
    height: 35px;
    width: 50px;
    cursor: pointer;
    border: none;
    background: none;
    padding: 0;
  }

  .save-btn {
    width: 100%;
    padding: 14px;
    background-color: var(--primary-color);
    color: white; /* Mindig fehér szöveg a gombon */
    border: none;
    font-weight: bold;
    font-size: 1rem;
    border-radius: 6px;
    cursor: pointer;
    margin-top: 1rem;
    transition: opacity 0.3s;
  }

    .save-btn:hover {
      opacity: 0.9;
    }

    .save-btn:disabled {
      background-color: #ccc;
      cursor: not-allowed;
    }

  .success-msg {
    background-color: #d4edda;
    color: #155724;
    padding: 10px;
    border-radius: 6px;
    text-align: center;
    margin-bottom: 1rem;
  }

  .error-box {
    color: #721c24;
    background-color: #f8d7da;
    padding: 1rem;
    border-radius: 6px;
    text-align: center;
  }
</style>
