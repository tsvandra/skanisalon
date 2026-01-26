<script setup>
  import { ref, onMounted } from 'vue';
  import { useRouter } from 'vue-router';

  const router = useRouter();
  const isMenuOpen = ref(false);
  const isLoggedIn = ref(false);

  const toggleMenu = () => {
    isMenuOpen.value = !isMenuOpen.value;
  };

  // Kilépés funkció
  const handleLogout = () => {
    // Töröljük a tokent
    localStorage.removeItem('salon_token');
    // Újratöltjük az oldalt, hogy minden visszaálljon alaphelyzetbe
    window.location.reload();
  };

  onMounted(() => {
    // Ellenőrizzük, van-e token
    isLoggedIn.value = !!localStorage.getItem('salon_token');
  });
</script>

<template>
  <header class="app-header">
    <div class="container">
      <div class="logo">
        <router-link to="/">Skani Salon</router-link>
      </div>

      <button class="menu-toggle" @click="toggleMenu">
        ☰
      </button>

      <nav :class="{ 'is-open': isMenuOpen }">
        <router-link to="/" @click="isMenuOpen = false">Kezdőlap</router-link>
        <router-link to="/szolgaltatasok" @click="isMenuOpen = false">Árlista</router-link>
        <router-link to="/galeria" @click="isMenuOpen = false">Galéria</router-link>
        <router-link to="/kapcsolat" @click="isMenuOpen = false">Kapcsolat</router-link>

        <button v-if="isLoggedIn" @click="handleLogout" class="logout-btn">
          Kilépés
        </button>
      </nav>
    </div>
  </header>
</template>

<style scoped>
  .app-header {
    background-color: #1a1a1a; /* Sötét háttér */
    color: #fff;
    padding: 1rem 0;
    position: sticky;
    top: 0;
    z-index: 1000;
    box-shadow: 0 2px 10px rgba(0,0,0,0.3);
  }

  .container {
    max-width: 1200px;
    margin: 0 auto;
    padding: 0 1rem;
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  .logo a {
    font-size: 1.5rem;
    font-weight: bold;
    color: #d4af37; /* Arany szín */
    text-decoration: none;
    font-family: 'Playfair Display', serif; /* Elegáns betűtípus */
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
        color: #d4af37;
      }

  /* Kilépés gomb stílusa */
  .logout-btn {
    background-color: transparent;
    border: 1px solid #d4af37;
    color: #d4af37;
    padding: 5px 12px;
    border-radius: 4px;
    cursor: pointer;
    font-size: 0.9rem;
    transition: all 0.3s;
  }

    .logout-btn:hover {
      background-color: #d4af37;
      color: #1a1a1a;
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
      background-color: #1a1a1a;
      padding: 1rem 0;
      text-align: center;
    }

      nav.is-open {
        display: flex;
      }
  }
</style>
