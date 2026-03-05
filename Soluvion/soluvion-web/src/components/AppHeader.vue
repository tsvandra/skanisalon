<script setup>
  import { ref, onMounted, inject, computed } from 'vue';
  import { useRouter } from 'vue-router';
  import LanguageSwitcher from './LanguageSwitcher.vue';

  const company = inject('company');
  const isLoggedIn = inject('isLoggedIn');
  const router = useRouter();
  const isMenuOpen = ref(false);

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

  const logoStyle = computed(() => {
    const height = company.value?.logoHeight || 50;
    return {
      height: `${height}px`,
      width: 'auto',
      display: 'block'
    };
  });
</script>

<template>
  <header class="bg-surface text-text sticky top-0 z-[1000] shadow-md border-b-2 border-primary transition-colors duration-300">
    <div class="max-w-[1200px] mx-auto px-4 py-2 flex justify-between items-center relative">

      <div class="flex items-center">
        <router-link to="/" class="no-underline block hover:opacity-80 transition-opacity">
          <img v-if="company?.logoUrl"
               :src="getLogoUrl(company.logoUrl)"
               :alt="company?.name"
               :style="logoStyle" />
          <span v-else class="text-2xl font-bold text-primary font-sans tracking-wide">
            {{ company?.name || 'Szalon' }}
          </span>
        </router-link>
      </div>

      <button class="md:hidden text-text text-3xl focus:outline-none hover:text-primary transition-colors p-1" @click="toggleMenu">
        <i :class="isMenuOpen ? 'pi pi-times' : 'pi pi-bars'"></i>
      </button>

      <nav :class="[
        'items-center gap-6 font-medium transition-all duration-300',
        isMenuOpen ? 'flex flex-col absolute top-full left-0 w-full bg-surface py-6 border-b border-primary shadow-xl z-50' : 'hidden',
        'md:flex md:static md:flex-row md:w-auto md:bg-transparent md:border-none md:p-0 md:shadow-none'
      ]">

        <router-link to="/" @click="isMenuOpen = false" class="text-text hover:text-primary transition-colors [&.router-link-active]:text-primary">
          {{ $t('nav.home') }}
        </router-link>

        <router-link to="/szolgaltatasok" @click="isMenuOpen = false" class="text-text hover:text-primary transition-colors [&.router-link-active]:text-primary">
          {{ $t('nav.services') }}
        </router-link>

        <router-link to="/galeria" @click="isMenuOpen = false" class="text-text hover:text-primary transition-colors [&.router-link-active]:text-primary">
          {{ $t('nav.gallery') }}
        </router-link>

        <router-link to="/kapcsolat" @click="isMenuOpen = false" class="text-text hover:text-primary transition-colors [&.router-link-active]:text-primary">
          {{ $t('nav.contact') }}
        </router-link>

        <div class="my-2 md:my-0 flex justify-center">
          <LanguageSwitcher :adminMode="isLoggedIn" />
        </div>

        <router-link v-if="isLoggedIn" to="/beallitasok" @click="isMenuOpen = false" class="text-primary hover:rotate-90 transition-transform duration-300 inline-block p-1" :title="$t('common.settings')">
          <i class="pi pi-cog text-xl"></i>
        </router-link>

        <button v-if="isLoggedIn" @click="handleLogout"
                class="px-5 py-2 rounded-lg text-sm border border-primary text-primary hover:bg-primary/10 transition-colors font-bold tracking-wide">
          {{ $t('common.logout') }}
        </button>

        <router-link v-else to="/login" @click="isMenuOpen = false"
                     class="px-5 py-2 rounded-lg text-sm bg-primary text-black font-bold hover:brightness-110 hover:scale-105 transition-all tracking-wide shadow-md">
          {{ $t('common.login') }}
        </router-link>

      </nav>
    </div>
  </header>
</template>
