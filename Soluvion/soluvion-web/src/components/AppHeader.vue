<script setup>
  import { ref, inject, computed } from 'vue';
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
    <div class="max-w-7xl mx-auto px-4 py-2 flex justify-between items-center relative min-h-[64px]">

      <div class="flex items-center">
        <router-link to="/" class="no-underline block hover:opacity-80 transition-opacity flex-shrink-0 min-h-[44px] flex items-center">
          <img v-if="company?.logoUrl" :src="getLogoUrl(company?.logoUrl)" alt="Logo" :style="logoStyle" />
          <span v-else class="text-2xl font-bold text-primary tracking-wider">{{ company?.name || 'Skani Salon' }}</span>
        </router-link>
      </div>

      <button @click="toggleMenu"
              class="md:hidden flex items-center justify-center min-w-[44px] min-h-[44px] text-text hover:text-primary transition-colors"
              aria-label="Menü megnyitása">
        <i :class="isMenuOpen ? 'pi pi-times' : 'pi pi-bars'" class="text-2xl"></i>
      </button>

      <nav class="hidden md:flex items-center gap-6">
        <router-link to="/szolgaltatasok" class="text-text hover:text-primary transition-colors [&.router-link-active]:text-primary font-medium min-h-[44px] flex items-center">
          {{ $t('nav.services') }}
        </router-link>

        <router-link to="/galeria" class="text-text hover:text-primary transition-colors [&.router-link-active]:text-primary font-medium min-h-[44px] flex items-center">
          {{ $t('nav.gallery') }}
        </router-link>

        <router-link to="/foglalas" class="bg-primary text-white font-bold py-2 px-4 rounded-lg hover:brightness-90 transition-all">
          Időpontfoglalás
        </router-link>

        <router-link to="/kapcsolat" class="text-text hover:text-primary transition-colors [&.router-link-active]:text-primary font-medium min-h-[44px] flex items-center">
          {{ $t('nav.contact') }}
        </router-link>

        <LanguageSwitcher :adminMode="isLoggedIn" />

        <router-link v-if="isLoggedIn" to="/beallitasok" class="text-primary hover:rotate-90 transition-transform duration-300 flex items-center justify-center min-w-[44px] min-h-[44px]" :title="$t('common.settings')">
          <i class="pi pi-cog text-xl"></i>
        </router-link>

        <button v-if="isLoggedIn" @click="handleLogout"
                class="px-5 py-2 min-h-[44px] rounded-lg text-sm border border-primary text-primary hover:bg-primary/10 transition-colors font-bold tracking-wide">
          {{ $t('common.logout') }}
        </button>

        <router-link v-else to="/login" class="px-5 py-2 min-h-[44px] rounded-lg text-sm bg-primary text-white hover:bg-primary-emphasis transition-colors font-bold tracking-wide flex items-center">
          {{ $t('common.login') }}
        </router-link>
      </nav>
    </div>

    <nav v-show="isMenuOpen" class="md:hidden absolute top-full left-0 right-0 bg-surface border-b border-primary/20 shadow-xl flex flex-col p-4 gap-2 z-[999]">
      <router-link to="/szolgaltatasok" @click="isMenuOpen = false" class="text-text hover:text-primary transition-colors [&.router-link-active]:text-primary font-bold text-lg p-3 rounded-lg hover:bg-text/5">
        {{ $t('nav.services') }}
      </router-link>

      <router-link to="/galeria" @click="isMenuOpen = false" class="text-text hover:text-primary transition-colors [&.router-link-active]:text-primary font-bold text-lg p-3 rounded-lg hover:bg-text/5">
        {{ $t('nav.gallery') }}
      </router-link>

      <router-link to="/kapcsolat" @click="isMenuOpen = false" class="text-text hover:text-primary transition-colors [&.router-link-active]:text-primary font-bold text-lg p-3 rounded-lg hover:bg-text/5">
        {{ $t('nav.contact') }}
      </router-link>

      <div class="h-px bg-text/10 my-2"></div>

      <div class="flex justify-between items-center p-3">
        <LanguageSwitcher :adminMode="isLoggedIn" />

        <router-link v-if="isLoggedIn" to="/beallitasok" @click="isMenuOpen = false" class="text-primary p-2">
          <i class="pi pi-cog text-2xl"></i>
        </router-link>
      </div>

      <button v-if="isLoggedIn" @click="handleLogout" class="w-full mt-2 min-h-[44px] rounded-lg text-lg border border-primary text-primary font-bold">
        {{ $t('common.logout') }}
      </button>

      <router-link v-else to="/login" @click="isMenuOpen = false" class="w-full mt-2 min-h-[44px] flex items-center justify-center rounded-lg text-lg bg-primary text-white font-bold">
        {{ $t('common.login') }}
      </router-link>
    </nav>
  </header>
</template>
