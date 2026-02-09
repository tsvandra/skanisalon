<script setup>
import { inject, ref, computed } from 'vue';
import api from '@/services/api';

const company = inject('company');
const isLoggedIn = inject('isLoggedIn');
const isUploading = ref(false);
const footerInputRef = ref(null);

const footerStyle = computed(() => {
  if (company.value?.footerImageUrl) {
    return {
      backgroundImage: `url(${company.value.footerImageUrl})`,
      backgroundRepeat: 'repeat-x',
      backgroundPosition: 'center bottom',
      backgroundSize: 'auto 100%'
    };
  }
  return { background: '#2c2c2c' }; // Fallback
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
    if (company.value) {
        company.value.footerImageUrl = res.data.url;
    }
  } catch (err) {
    console.error(err);
    alert("Hiba a lábléc feltöltésekor");
  } finally {
    isUploading.value = false;
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

      <div v-if="isLoggedIn" class="footer-admin">
        <button @click="triggerUpload" class="btn-upload">
          <i v-if="isUploading" class="pi pi-spin pi-spinner"></i>
          <i v-else class="pi pi-image"></i> Háttérkép cseréje
        </button>
        <input type="file" ref="footerInputRef" @change="onFileSelected" accept="image/*" hidden />
      </div>
    </div>
  </footer>
</template>

<style scoped>
  .app-footer {
    position: relative;
    min-height: 250px;
    display: flex;
    align-items: flex-end; /* Aljára igazítva a tartalom */
    justify-content: center;
    color: #fff;
    margin-top: auto;
    background-color: #2c2c2c; /* Fallback */
  }

  .overlay {
    position: absolute;
    inset: 0;
    background: linear-gradient(to top, rgba(0,0,0,0.95), rgba(0,0,0,0.2));
  }

  .content {
    position: relative;
    z-index: 1;
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
  }

  .footer-info p {
    color: #aaa;
    font-size: 0.9rem;
  }

  .btn-upload {
    background: rgba(0,0,0,0.6);
    border: 1px solid #555;
    color: #ddd;
    padding: 6px 12px;
    border-radius: 4px;
    cursor: pointer;
    font-size: 0.8rem;
    transition: all 0.2s;
    margin-top: 10px;
  }

    .btn-upload:hover {
      background: #000;
      border-color: var(--primary-color);
      color: #fff;
    }
</style>
