<script setup>
  import { ref, onMounted, inject, computed } from 'vue';
  import { useRouter } from 'vue-router';
  import api from '@/services/api';

  const company = inject('company');
  const router = useRouter();
  const isMenuOpen = ref(false);
  const isLoggedIn = ref(false);

  // Logó feltöltés state
  const isUploadingLogo = ref(false);
  const logoInputRef = ref(null);

  const toggleMenu = () => isMenuOpen.value = !isMenuOpen.value;

  const handleLogout = () => {
    localStorage.removeItem('salon_token');
    window.location.href = '/';
  };

  const getLogoUrl = (path) => {
    if (!path) return null;
    if (path.startsWith('http')) return path;
    const baseUrl = import.meta.env.VITE_API_URL;
    return `${baseUrl}${path}`;
  };

  // --- LOGÓ MÉRETEZÉS (COMPUTED STYLE) ---
  const logoStyle = computed(() => {
    const height = company.value?.logoHeight || 50;
    return {
      height: `${height}px`,
      width: 'auto', // A szélesség alkalmazkodik
      display: 'block',
      transition: 'height 0.1s ease-out'
    };
  });

  // --- LOGÓ FELTÖLTÉS ---
  const triggerLogoUpload = () => logoInputRef.value.click();

  const onLogoSelected = async (event) => {
    const file = event.target.files[0];
    if (!file) return;

    isUploadingLogo.value = true;
    const formData = new FormData();
    formData.append('file', file);

    try {
      const res = await api.post('/api/Company/upload/logo', formData, {
        headers: { 'Content-Type': undefined }
      });
      if (company.value) company.value.logoUrl = res.data.url;
    } catch (err) {
      console.error(err);
      alert("Hiba a logó feltöltésekor");
    } finally {
      isUploadingLogo.value = false;
    }
  };

  // --- LOGÓ MÉRET MENTÉSE (@change) ---
  const saveLogoHeight = async () => {
    if (!company.value) return;
    try {
      // Ha null lenne, default 50
      if (!company.value.logoHeight) company.value.logoHeight = 50;

      await api.put(`/api/Company/${company.value.id}`, company.value);
      console.log("Logó méret mentve:", company.value.logoHeight);
    } catch (err) {
      console.error("Nem sikerült menteni a logó méretét", err);
    }
  };

  onMounted(() => {
    isLoggedIn.value = !!localStorage.getItem('salon_token');
  });
</script>

<template>
  <header class="app-header">
    <div class="container">

      <div class="logo-area">

        <router-link to="/" class="logo-link">
          <img v-if="company?.logoUrl"
               :src="getLogoUrl(company.logoUrl)"
               :alt="company?.name"
               :style="logoStyle" />
          <span v-else class="text-logo">{{ company?.name || 'Szalon' }}</span>
        </router-link>

        <div v-if="isLoggedIn" class="logo-admin-tools">

          <button @click="triggerLogoUpload" class="tool-btn" title="Logó cseréje">
            <i v-if="isUploadingLogo" class="pi pi-spin pi-spinner"></i>
            <i v-else class="pi pi-camera"></i>
          </button>
          <input type="file" ref="logoInputRef" @change="onLogoSelected" accept="image/*" hidden />

          <div class="slider-wrapper" title="Logó mérete">
            <input type="range"
                   v-model="company.logoHeight"
                   min="30"
                   max="150"
                   step="5"
                   @change="saveLogoHeight" />
          </div>

        </div>
      </div>

      <button class="menu-toggle" @click="toggleMenu">☰</button>

      <nav :class="{ 'is-open': isMenuOpen }">
        <router-link to="/" @click="isMenuOpen = false">Kezdőlap</router-link>
        <router-link to="/szolgaltatasok" @click="isMenuOpen = false">Árlista</router-link>
        <router-link to="/galeria" @click="isMenuOpen = false">Galéria</router-link>
        <router-link to="/kapcsolat" @click="isMenuOpen = false">Kapcsolat</router-link>

        <router-link v-if="isLoggedIn" to="/beallitasok" @click="isMenuOpen = false" class="settings-link">
          <i class="pi pi-cog" style="font-size: 1.2rem;"></i>
        </router-link>

        <button v-if="isLoggedIn" @click="handleLogout" class="auth-btn logout">Kilépés</button>
        <router-link v-else to="/login" @click="isMenuOpen = false" class="auth-btn login">Belépés</router-link>
      </nav>
    </div>
  </header>
</template>

<style scoped>
  .app-header {
    background-color: var(--secondary-color);
    color: #fff;
    padding: 0.5rem 0; /* Kicsit kisebb padding, hogy a logó domináljon */
    position: sticky;
    top: 0;
    z-index: 1000;
    box-shadow: 0 2px 10px rgba(0,0,0,0.3);
    border-bottom: 2px solid var(--primary-color);
  }

  .container {
    max-width: 1200px;
    margin: 0 auto;
    padding: 0 1rem;
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  /* LOGÓ TERÜLET */
  .logo-area {
    display: flex;
    align-items: center;
    gap: 15px;
  }

  .logo-link {
    text-decoration: none;
    display: block;
  }

  .text-logo {
    font-size: 1.5rem;
    font-weight: bold;
    color: var(--primary-color);
    font-family: var(--font-family);
  }

  /* ADMIN TOOLBAR (ÚJ) */
  .logo-admin-tools {
    display: flex;
    align-items: center;
    gap: 8px;
    background: rgba(0, 0, 0, 0.6);
    padding: 4px 8px;
    border-radius: 20px;
    border: 1px solid #444;
    /* Animáció megjelenéskor */
    animation: fadeIn 0.3s ease;
  }

  @keyframes fadeIn {
    from {
      opacity: 0;
    }

    to {
      opacity: 1;
    }
  }

  .tool-btn {
    background: transparent;
    border: none;
    color: #ccc;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 4px;
    transition: color 0.2s;
  }

    .tool-btn:hover {
      color: var(--primary-color);
      transform: scale(1.1);
    }

  .slider-wrapper {
    display: flex;
    align-items: center;
  }

  /* Csúszka stílus - kicsi és kompakt */
  input[type=range] {
    -webkit-appearance: none;
    width: 60px; /* Rövidebb, mint a footeré, hogy elférjen a fejlécben */
    height: 4px;
    background: #555;
    border-radius: 2px;
    outline: none;
    cursor: pointer;
  }

    input[type=range]::-webkit-slider-thumb {
      -webkit-appearance: none;
      width: 12px;
      height: 12px;
      border-radius: 50%;
      background: var(--primary-color, #d4af37);
      cursor: pointer;
    }

  /* MENU & NAV */
  nav {
    display: flex;
    gap: 1.5rem;
    align-items: center;
  }

    nav a {
      color: #fff;
      text-decoration: none;
      font-size: 1rem;
      transition: color 0.3s;
    }

      nav a:hover, nav a.router-link-active {
        color: var(--primary-color);
      }

  .settings-link {
    color: var(--primary-color) !important;
    transition: transform 0.3s !important;
  }

    .settings-link:hover {
      transform: rotate(90deg);
    }

  .auth-btn {
    text-decoration: none;
    padding: 5px 12px;
    border-radius: 4px;
    cursor: pointer;
    font-size: 0.9rem;
    border: 1px solid var(--primary-color);
    background: transparent;
    color: var(--primary-color);
  }

    .auth-btn.login {
      background: var(--primary-color);
      color: var(--secondary-color);
      font-weight: bold;
    }

  .menu-toggle {
    display: none;
    background: none;
    border: none;
    color: white;
    font-size: 1.5rem;
    cursor: pointer;
  }

  @media (max-width: 768px) {
    .menu-toggle {
      display: block;
    }

    nav {
      display: none;
      flex-direction: column;
      position: absolute;
      top: 100%;
      left: 0;
      width: 100%;
      background-color: var(--secondary-color);
      padding: 1rem 0;
      text-align: center;
      border-bottom: 1px solid var(--primary-color);
    }

      nav.is-open {
        display: flex;
      }
  }
</style>
