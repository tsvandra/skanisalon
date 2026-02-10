<script setup>
  import { ref, onMounted, inject, computed } from 'vue';
  import { useRouter } from 'vue-router';

  // Csak a megjelenítéshez szükséges dolgok maradnak
  const company = inject('company');
  const router = useRouter();
  const isMenuOpen = ref(false);
  const isLoggedIn = ref(false);

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

  // A stílus marad, hogy reagáljon a Settings-ben történő változásokra
  const logoStyle = computed(() => {
    const height = company.value?.logoHeight || 50;
    return {
      height: `${height}px`,
      width: 'auto',
      display: 'block'
    };
  });

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
    padding: 0.5rem 0;
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

  .logo-area {
    display: flex;
    align-items: center;
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
