<script setup>
  import { inject, ref, computed } from 'vue';
  import api from '@/services/api';

  const company = inject('company');
  const isLoggedIn = inject('isLoggedIn');
  const isUploading = ref(false);
  const footerInputRef = ref(null);

  const footerStyle = computed(() => {
    const height = company.value?.footerHeight || 250;

    const baseStyle = {
      minHeight: `${height}px`,
      // transition: 'min-height 0.1s ease-out' <-- TÖRÖLVE A SIMASÁGÉRT
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

  const saveHeight = async () => {
    if (!company.value) return;
    try {
      if (!company.value.footerHeight) company.value.footerHeight = 250;
      await api.put(`/api/Company/${company.value.id}`, company.value);
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

        <button @click="triggerUpload" class="btn-admin" title="Háttérkép cseréje">
          <i v-if="isUploading" class="pi pi-spin pi-spinner"></i>
          <i v-else class="pi pi-camera"></i>
        </button>
        <input type="file" ref="footerInputRef" @change="onFileSelected" accept="image/*" hidden />

        <div class="separator"></div>

        <div class="height-control">
          <i class="pi pi-arrows-v" style="font-size: 0.8rem"></i>
          <input type="range"
                 v-model="company.footerHeight"
                 min="50"
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
    display: flex;
    align-items: flex-end;
    justify-content: center;
    color: #fff;
    margin-top: auto;
    background-color: #2c2c2c;
    overflow: hidden;
  }

  .overlay {
    position: absolute;
    inset: 0;
    background: linear-gradient(to top, rgba(0,0,0,0.95), rgba(0,0,0,0.1));
    pointer-events: none;
  }

  .content {
    position: relative;
    z-index: 2;
    text-align: center;
    width: 100%;
    padding: 1.5rem;
    height: 100%; /* Kitölti a rendelkezésre álló magasságot */
    display: flex;
    flex-direction: column;
    justify-content: flex-end; /* Tartalom alulra */
    align-items: center;
  }

  .footer-info h3 {
    color: var(--primary-color, #d4af37);
    margin-bottom: 0.3rem;
    font-size: 1.4rem;
    text-shadow: 0 2px 4px rgba(0,0,0,0.8);
    margin-top: 0;
  }

  .footer-info p {
    color: #aaa;
    font-size: 0.85rem;
    margin: 0;
  }

  /* --- STABIL POZICIONÁLÁS --- */
  .footer-admin-panel {
    position: absolute;
    bottom: 15px;
    right: 15px;
    display: flex;
    align-items: center;
    gap: 10px;
    background: rgba(0, 0, 0, 0.8);
    padding: 5px 12px;
    border-radius: 20px;
    border: 1px solid #555;
    backdrop-filter: blur(4px);
    z-index: 100;
  }

  .separator {
    width: 1px;
    height: 20px;
    background: #555;
  }

  .btn-admin {
    background: transparent;
    border: none;
    color: #ddd;
    padding: 5px;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
  }

    .btn-admin:hover {
      color: var(--primary-color);
      transform: scale(1.1);
    }

  .height-control {
    display: flex;
    align-items: center;
    gap: 8px;
    color: #aaa;
  }

  input[type=range] {
    -webkit-appearance: none;
    width: 100px;
    height: 4px;
    background: #555;
    border-radius: 2px;
    outline: none;
    cursor: pointer;
  }

    input[type=range]::-webkit-slider-thumb {
      -webkit-appearance: none;
      width: 14px;
      height: 14px;
      border-radius: 50%;
      background: var(--primary-color, #d4af37);
      cursor: pointer;
    }

      input[type=range]::-webkit-slider-thumb:hover {
        transform: scale(1.2);
      }
</style>
