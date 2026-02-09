<script setup>
  import { ref, onMounted, inject } from 'vue';
  import { useRouter } from 'vue-router';
  import api from '@/services/api'; // Kell az API híváshoz

  // Injektáljuk a cég adatait (Globális state)
  const company = inject('company');

  const router = useRouter();
  const isMenuOpen = ref(false);
  const isLoggedIn = ref(false);

  // Logó feltöltés state
  const isUploadingLogo = ref(false);
  const logoInputRef = ref(null);

  const toggleMenu = () => {
    isMenuOpen.value = !isMenuOpen.value;
  };

  const handleLogout = () => {
    localStorage.removeItem('salon_token');
    window.location.href = '/';
  };

  // Logó URL segéd (Ha relatív útvonal lenne, bár most már teljes URL jön)
  const getLogoUrl = (path) => {
    if (!path) return null;
    if (path.startsWith('http')) return path;
    const baseUrl = import.meta.env.VITE_API_URL;
    return `${baseUrl}${path}`;
  };

  // --- LOGÓ FELTÖLTÉS ---
  const triggerLogoUpload = () => {
    logoInputRef.value.click();
  };

  const onLogoSelected = async (event) => {
    const file = event.target.files[0];
    if (!file) return;

    isUploadingLogo.value = true;
    const formData = new FormData();
    formData.append('file', file);

    try {
      // POST kérés az új végpontra
      const res = await api.post('/api/Company/upload/logo', formData, {
        headers: { 'Content-Type': undefined }
      });
      // Azonnali frissítés az UI-on (a globális objektumban)
      if (company.value) {
        company.value.logoUrl = res.data.url;
      }
    } catch (err) {
      console.error("Logo upload failed", err);
      alert("Hiba a logó feltöltésekor");
    } finally {
      isUploadingLogo.value = false;
    }
  };

  onMounted(() => {
    isLoggedIn.value = !!localStorage.getItem('salon_token');
  });
</script>

<template>
  <header class="app-header">
    <div class="container">

      <div class="logo">
        <router-link to="/" class="logo-link">
          <img v-if="company?.logoUrl" :src="getLogoUrl(company.logoUrl)" :alt="company?.name" class="logo-img" />

          <span v-else>{{ company?.name || 'Syxyalon' }}</span>

          <div v-if="isLoggedIn" class="logo-edit-wrapper">
            <button @click.prevent="triggerLogoUpload" class="edit-logo-btn" title="Logó cseréje">
              <i v-if="isUploadingLogo" class="pi pi-spin pi-spinner"></i>
              <i v-else class="pi pi-pencil"></i>
            </button>
            <input type="file" ref="logoInputRef" @change="onLogoSelected" accept="image/*" hidden />
          </div>
        </router-link>
      </div>

      <button class="menu-toggle" @click="toggleMenu">☰</button>

      <nav :class="{ 'is-open': isMenuOpen }">
        <router-link to="/" @click="isMenuOpen = false">Kezdőlap</router-link>
        <router-link to="/szolgaltatasok" @click="isMenuOpen = false">Árlista</router-link>
        <router-link to="/galeria" @click="isMenuOpen = false">Galéria</router-link>
        <router-link to="/kapcsolat" @click="isMenuOpen = false">Kapcsolat</router-link>

        <router-link v-if="isLoggedIn"
                     to="/beallitasok"
                     @click="isMenuOpen = false"
                     class="settings-link"
                     title="Beállítások">
          <i class="pi pi-cog" style="font-size: 1.2rem;"></i>
        </router-link>

        <button v-if="isLoggedIn" @click="handleLogout" class="auth-btn logout">
          Kilépés
        </button>

        <router-link v-else to="/login" @click="isMenuOpen = false" class="auth-btn login">
          Belépés
        </router-link>

      </nav>
    </div>
  </header>
</template>

<style scoped>
  .app-header {
    background-color: var(--secondary-color);
    color: #fff;
    padding: 1rem 0;
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

  /* Logó stílus */
  .logo {
    display: flex;
    align-items: center;
  }

  .logo-link {
    font-size: 1.5rem;
    font-weight: bold;
    color: var(--primary-color);
    text-decoration: none;
    font-family: var(--font-family);
    display: flex;
    align-items: center;
    position: relative; /* A szerkesztő gomb pozicionálásához */
  }

  .logo-img {
    max-height: 50px;
    width: auto;
    display: block;
  }

  /* Szerkesztő gomb (Ceruza) */
  .logo-edit-wrapper {
    margin-left: 10px;
  }

  .edit-logo-btn {
    background: rgba(0,0,0,0.5);
    color: #fff;
    border: 1px solid #555;
    border-radius: 50%;
    width: 24px;
    height: 24px;
    font-size: 0.8rem;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    opacity: 0.6;
    transition: all 0.2s;
  }

    .edit-logo-btn:hover {
      opacity: 1;
      background: var(--primary-color);
      color: #000;
    }

  /* Menü linkek */
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

  /* Beállítások ikon specifikus stílus */
  .settings-link {
    display: flex;
    align-items: center;
    color: var(--primary-color) !important;
    transition: transform 0.3s !important;
  }

    .settings-link:hover {
      transform: rotate(90deg);
    }

  /* Auth gombok */
  .auth-btn {
    text-decoration: none;
    padding: 5px 12px;
    border-radius: 4px;
    cursor: pointer;
    font-size: 0.9rem;
    transition: all 0.3s;
    border: 1px solid var(--primary-color);
    background-color: transparent;
  }

    .auth-btn.logout {
      color: var(--primary-color);
    }

      .auth-btn.logout:hover {
        background-color: var(--primary-color);
        color: var(--secondary-color);
      }

    .auth-btn.login {
      background-color: var(--primary-color);
      color: var(--secondary-color);
      font-weight: bold;
    }

      .auth-btn.login:hover {
        background-color: #b5952f;
        color: white;
      }


  .menu-toggle {
    display: none;
    background: none;
    border: none;
    color: white;
    font-size: 1.5rem;
    cursor: pointer;
  }

  /* Mobil nézet */
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
