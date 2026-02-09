<script setup>
  import { inject, ref, computed } from 'vue';
  import api from '@/services/api';

  const company = inject('company');
  const isLoggedIn = inject('isLoggedIn');
  const isUploading = ref(false);
  const footerInputRef = ref(null);

  // Stílus generálása a magasság (slider) alapján
  const footerStyle = computed(() => {
    // Alapértelmezett magasság 250px, ha nincs beállítva
    const height = company.value?.footerHeight || 250;

    const baseStyle = {
      minHeight: `${height}px`, // Itt használjuk a csúszka értékét
      transition: 'min-height 0.2s ease' // Finom animáció
    };

    if (company.value?.footerImageUrl) {
      return {
        ...baseStyle,
        backgroundImage: `url(${company.value.footerImageUrl})`,
        backgroundRepeat: 'repeat-x',
        backgroundPosition: 'center bottom',
        backgroundSize: 'auto 100%'
      };
    }
    return { ...baseStyle, background: '#2c2c2c' };
  });

  // --- KÉPFELTÖLTÉS ---
  const triggerUpload = () => footerInputRef.value.click();

  const onFileSelected = async (event) => {
    const file = event.target.files[0];
    if (!file) return;

    isUploading.value = true;
    const formData = new FormData();
    formData.append('file', file);

    try {
      const res = await api.post('/api/Company/upload/footer', formData, {
        headers: { 'Content-Type': undefined }
      });
      if (company.value) company.value.footerImageUrl = res.data.url;
    } catch (err) {
      console.error(err);
      alert("Hiba a feltöltéskor");
    } finally {
      isUploading.value = false;
    }
  };

  // --- MAGASSÁG MENTÉSE ---
  // Csak akkor hívjuk meg, ha elengedte a csúszkát (@change), hogy kíméljük a szervert
  const saveHeight = async () => {
    if (!company.value) return;
    try {
      // A CompanyController.UpdateCompany-t használjuk
      // Fontos: Elküldjük a teljes objektumot, vagy csak amit kell.
      // A jelenlegi backend UpdateCompany mindent felülír, amit kap,
      // ezért küldjük a company.value-t, amiben a v-model már frissítette a footerHeight-et.
      await api.put(`/api/Company/${company.value.id}`, company.value);
      console.log("Magasság mentve:", company.value.footerHeight);
    } catch (err) {
      console.error("Nem sikerült menteni a magasságot", err);
    }
  };
</script>

<template>
  <footer class="app-footer" :style="footerStyle">
    <div class="overlay"></div>

    <div class="content">
      <div class="footer-info">
        <h3>{{ company?.name || 'Soluvion Salon' }}</h3>
        <p>&copy; {{ new Date().getFullYear() }} Minden jog fenntartva.</p>
      </div>

      <div v-if="isLoggedIn" class="footer-admin-panel">

        <button @click="triggerUpload" class="btn-admin">
          <i v-if="isUploading" class="pi pi-spin pi-spinner"></i>
          <i v-else class="pi pi-camera"></i> Képcsere
        </button>
        <input type="file" ref="footerInputRef" @change="onFileSelected" accept="image/*" hidden />

        <div class="height-control">
          <i class="pi pi-arrows-v"></i>
          <input type="range"
                 v-model="company.footerHeight"
                 min="100"
                 max="600"
                 step="10"
                 @change="saveHeight"
                 title="Lábléc magassága" />
        </div>

      </div>
    </div>
  </footer>
</template>

<style scoped>
  .app-footer {
    position: relative;
    /* A min-height-et most már a style binding kezeli */
    display: flex;
    align-items: flex-end;
    justify-content: center;
    color: #fff;
    margin-top: auto;
    background-color: #2c2c2c;
  }

  .overlay {
    position: absolute;
    inset: 0;
    background: linear-gradient(to top, rgba(0,0,0,0.95), rgba(0,0,0,0.1));
    pointer-events: none; /* Átkattintható legyen, hogy a gombok működjenek alatta */
  }

  .content {
    position: relative;
    z-index: 2; /* Magasabb z-index, hogy kattintható legyen */
    text-align: center;
    width: 100%;
    padding: 2rem;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 1rem;
  }

  .footer-info h3 {
    color: var(--primary-color, #d4af37);
    margin-bottom: 0.5rem;
    font-size: 1.5rem;
    text-shadow: 0 2px 4px rgba(0,0,0,0.8);
  }

  /* ADMIN PANEL STÍLUS */
  .footer-admin-panel {
    display: flex;
    align-items: center;
    gap: 15px;
    background: rgba(0, 0, 0, 0.7);
    padding: 8px 15px;
    border-radius: 20px;
    border: 1px solid #444;
    margin-top: 10px;
  }

  .btn-admin {
    background: transparent;
    border: 1px solid #aaa;
    color: #ddd;
    padding: 5px 10px;
    border-radius: 4px;
    cursor: pointer;
    font-size: 0.8rem;
    display: flex;
    align-items: center;
    gap: 5px;
  }

    .btn-admin:hover {
      border-color: var(--primary-color);
      color: #fff;
      background: rgba(255,255,255,0.1);
    }

  .height-control {
    display: flex;
    align-items: center;
    gap: 5px;
    color: #aaa;
  }

  /* Csúszka stílus */
  input[type=range] {
    width: 100px;
    cursor: pointer;
  }
</style>
