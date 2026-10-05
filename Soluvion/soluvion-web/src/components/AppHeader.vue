<script setup>
  import { ref, inject, computed } from 'vue';
  import { useRouter } from 'vue-router';
  import LanguageSwitcher from './LanguageSwitcher.vue';
  import { useLocalizedRoute } from '@/composables/useLocalizedRoute';

  const company = inject('company');
  const isLoggedIn = inject('isLoggedIn');
  const userRole = inject('userRole'); // <--- JWT-ből dekódolt szerepkör
  const router = useRouter();
  const { to: localized } = useLocalizedRoute();
  const isMenuOpen = ref(false);

  // Jogosultság szintek
  const isEmployee = computed(() => ['Admin', 'Owner', 'Employee'].includes(userRole?.value));
  const isAdmin = computed(() => ['Admin', 'Owner'].includes(userRole?.value));

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
        <router-link :to="localized('home')" class="no-underline block hover:opacity-80 transition-opacity flex-shrink-0 min-h-[44px] flex items-center">
          <img v-if="company?.logoUrl" :src="getLogoUrl(company?.logoUrl)" alt="Logo" :style="logoStyle" />
          <span v-else class="text-2xl font-bold text-primary tracking-wider">{{ company?.name || 'Skani Salon' }}</span>
        </router-link>
      </div>

      <button @click="toggleMenu"
              class="lg:hidden flex items-center justify-center min-w-[44px] min-h-[44px] text-text hover:text-primary transition-colors"
              :aria-label="$t('appShell.openMenu')">
        <i :class="[isMenuOpen ? 'pi pi-times' : 'pi pi-bars', 'text-2xl']"></i>
      </button>

      <!-- ================= ASZTALI NÉZET ================= -->
      <nav class="hidden lg:flex items-center gap-3 xl:gap-6">
        
        <!-- Publikus / Ügyfél nézet -->
        <router-link :to="localized('services')" class="text-text hover:text-primary transition-colors [&.router-link-active]:text-primary font-medium min-h-[44px] flex items-center whitespace-nowrap">
          {{ $t('nav.services') }}
        </router-link>

        <router-link :to="localized('gallery')" class="text-text hover:text-primary transition-colors [&.router-link-active]:text-primary font-medium min-h-[44px] flex items-center whitespace-nowrap">
          {{ $t('nav.gallery') }}
        </router-link>

        <router-link :to="localized('contact')" class="text-text hover:text-primary transition-colors [&.router-link-active]:text-primary font-medium min-h-[44px] flex items-center whitespace-nowrap">
          {{ $t('nav.contact') }}
        </router-link>

        <!-- Foglalás ügyfeleknek (NEM munkásoknak) -->
        <router-link v-if="!isEmployee" :to="localized('booking')"
                     class="bg-primary text-white font-bold py-2 px-4 rounded-lg hover:brightness-90 transition-all min-h-[44px] flex items-center shadow-sm whitespace-nowrap">
          {{ $t('nav.booking') }}
        </router-link>

        <!-- Elválasztó munkásoknak -->
        <div v-if="isEmployee" class="w-px h-6 bg-text/20 mx-1"></div>

        <!-- Munkás nézet -->
        <router-link v-if="isEmployee" :to="localized('dashboard')"
                     class="bg-primary text-white font-bold py-2 px-4 rounded-lg hover:brightness-90 transition-all min-h-[44px] flex items-center shadow-sm whitespace-nowrap">
          {{ $t('nav.dashboard') }}
        </router-link>

        <!-- Elválasztó admin/közös dolgok előtt -->
        <div class="w-px h-6 bg-text/20 mx-1"></div>

        <!-- Közös funkciók (Nyelv, Beállítás, Kijelentkezés) -->
        <LanguageSwitcher :adminMode="isAdmin" />

        <!-- Csak tulaj/főnök láthatja a beállításokat -->
        <router-link v-if="isAdmin" :to="localized('settings')" class="text-primary hover:rotate-90 transition-transform duration-300 flex items-center justify-center min-w-[44px] min-h-[44px]" :title="$t('common.settings')">
          <i class="pi pi-cog text-xl"></i>
        </router-link>

        <button v-if="isLoggedIn" @click="handleLogout"
                class="px-3 xl:px-5 py-2 min-h-[44px] rounded-lg text-sm border border-primary text-primary hover:bg-primary/10 transition-colors font-bold tracking-wide whitespace-nowrap">
          {{ $t('common.logout') }}
        </button>

        <router-link v-else :to="localized('login')" class="px-5 py-2 min-h-[44px] rounded-lg text-sm bg-primary text-white hover:bg-primary-emphasis transition-colors font-bold tracking-wide flex items-center whitespace-nowrap">
          {{ $t('common.login') }}
        </router-link>
      </nav>
    </div>

    <!-- ================= MOBIL NÉZET ================= -->
    <nav v-show="isMenuOpen" class="lg:hidden absolute top-full left-0 right-0 bg-surface border-b border-primary/20 shadow-xl flex flex-col p-4 gap-2 z-[999]">

      <!-- Ügyfél: Foglalás -->
      <router-link v-if="!isEmployee" :to="localized('booking')" @click="isMenuOpen = false"
                   class="bg-primary text-white text-center font-bold text-lg p-3 rounded-lg shadow-sm hover:brightness-95 transition-all mb-2 min-h-[48px] flex justify-center items-center">
        {{ $t('nav.booking') }}
      </router-link>

      <!-- Munkás: Vezérlőpult és modulok -->
      <template v-if="isEmployee">
        <router-link :to="localized('dashboard')" @click="isMenuOpen = false"
                     class="bg-primary text-white text-center font-bold text-lg p-3 rounded-lg shadow-sm hover:brightness-95 transition-all mb-1 min-h-[48px] flex justify-center items-center gap-2">
          <i class="pi pi-th-large"></i> {{ $t('nav.dashboard') }}
        </router-link>
        <div class="grid grid-cols-3 gap-2 mb-2">
          <router-link :to="localized('orders')" @click="isMenuOpen = false"
                       class="bg-surface border border-text/15 text-text hover:border-primary text-center font-semibold text-xs p-2 rounded-lg flex flex-col items-center justify-center gap-1 min-h-[44px]">
            <i class="pi pi-calendar text-primary text-sm"></i>
            <span>{{ $t('nav.orders') }}</span>
          </router-link>
          <router-link v-if="isAdmin" :to="localized('inventory')" @click="isMenuOpen = false"
                       class="bg-surface border border-text/15 text-text hover:border-primary text-center font-semibold text-xs p-2 rounded-lg flex flex-col items-center justify-center gap-1 min-h-[44px]">
            <i class="pi pi-box text-primary text-sm"></i>
            <span>{{ $t('nav.inventory') }}</span>
          </router-link>
          <router-link :to="localized('customers')" @click="isMenuOpen = false"
                       class="bg-surface border border-text/15 text-text hover:border-primary text-center font-semibold text-xs p-2 rounded-lg flex flex-col items-center justify-center gap-1 min-h-[44px]">
            <i class="pi pi-users text-primary text-sm"></i>
            <span>{{ $t('nav.customers') }}</span>
          </router-link>
        </div>
      </template>

      <router-link :to="localized('services')" @click="isMenuOpen = false" class="text-text hover:text-primary transition-colors [&.router-link-active]:text-primary font-bold text-lg p-3 rounded-lg hover:bg-text/5">
        {{ $t('nav.services') }}
      </router-link>

      <router-link :to="localized('gallery')" @click="isMenuOpen = false" class="text-text hover:text-primary transition-colors [&.router-link-active]:text-primary font-bold text-lg p-3 rounded-lg hover:bg-text/5">
        {{ $t('nav.gallery') }}
      </router-link>

      <router-link :to="localized('contact')" @click="isMenuOpen = false" class="text-text hover:text-primary transition-colors [&.router-link-active]:text-primary font-bold text-lg p-3 rounded-lg hover:bg-text/5">
        {{ $t('nav.contact') }}
      </router-link>

      <div class="h-px bg-text/10 my-2"></div>

      <div class="flex justify-between items-center p-3">
        <LanguageSwitcher :adminMode="isAdmin" />

        <router-link v-if="isAdmin" :to="localized('settings')" @click="isMenuOpen = false" class="text-primary p-2 min-h-[44px] min-w-[44px] flex items-center justify-center">
          <i class="pi pi-cog text-2xl"></i>
        </router-link>
      </div>

      <button v-if="isLoggedIn" @click="handleLogout" class="w-full mt-2 min-h-[44px] rounded-lg text-lg border border-primary text-primary font-bold">
        {{ $t('common.logout') }}
      </button>

      <router-link v-else :to="localized('login')" @click="isMenuOpen = false" class="w-full mt-2 min-h-[44px] flex items-center justify-center rounded-lg text-lg bg-primary text-white font-bold">
        {{ $t('common.login') }}
      </router-link>
    </nav>
  </header>
</template>
