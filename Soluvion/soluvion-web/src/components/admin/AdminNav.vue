<template>
  <nav aria-label="Admin Navigation" class="bg-surface border-b border-text/10 shadow-xs relative z-30">
    <div class="max-w-7xl mx-auto px-4 py-2 flex items-center justify-between gap-3">
      
      <!-- Fő navigációs fülek (Vezérlőpult, Megrendelések, Raktár, Ügyfelek) -->
      <div class="flex items-center gap-1.5 md:gap-2 overflow-x-auto no-scrollbar py-0.5 w-full sm:w-auto">
        <router-link
          :to="localized('dashboard')"
          class="px-3 md:px-4 py-2 rounded-xl text-xs md:text-sm font-bold flex items-center gap-2 whitespace-nowrap transition-all border border-text/10 bg-background text-text hover:border-primary/50 hover:text-primary [&.router-link-active]:bg-primary [&.router-link-active]:text-white [&.router-link-active]:border-primary [&.router-link-active]:shadow-sm"
        >
          <i class="pi pi-th-large text-sm"></i>
          <span>{{ $t('nav.dashboard') }}</span>
        </router-link>

        <router-link
          :to="localized('orders')"
          class="px-3 md:px-4 py-2 rounded-xl text-xs md:text-sm font-bold flex items-center gap-2 whitespace-nowrap transition-all border border-text/10 bg-background text-text hover:border-primary/50 hover:text-primary [&.router-link-active]:bg-primary [&.router-link-active]:text-white [&.router-link-active]:border-primary [&.router-link-active]:shadow-sm"
        >
          <i class="pi pi-calendar text-sm"></i>
          <span>{{ $t('nav.orders') }}</span>
        </router-link>

        <router-link
          v-if="isAdmin"
          :to="localized('inventory')"
          class="px-3 md:px-4 py-2 rounded-xl text-xs md:text-sm font-bold flex items-center gap-2 whitespace-nowrap transition-all border border-text/10 bg-background text-text hover:border-primary/50 hover:text-primary [&.router-link-active]:bg-primary [&.router-link-active]:text-white [&.router-link-active]:border-primary [&.router-link-active]:shadow-sm"
        >
          <i class="pi pi-box text-sm"></i>
          <span>{{ $t('nav.inventory') }}</span>
        </router-link>

        <router-link
          :to="localized('customers')"
          class="px-3 md:px-4 py-2 rounded-xl text-xs md:text-sm font-bold flex items-center gap-2 whitespace-nowrap transition-all border border-text/10 bg-background text-text hover:border-primary/50 hover:text-primary [&.router-link-active]:bg-primary [&.router-link-active]:text-white [&.router-link-active]:border-primary [&.router-link-active]:shadow-sm"
        >
          <i class="pi pi-users text-sm"></i>
          <span>{{ $t('nav.customers') }}</span>
        </router-link>
      </div>

      <!-- Jobb oldal: Beállítások gyorsgomb (csak admin/owner számára) -->
      <div v-if="isAdmin" class="hidden md:flex items-center">
        <router-link
          :to="localized('settings')"
          class="px-3 py-1.5 rounded-lg text-xs font-semibold text-text-muted hover:text-primary hover:bg-text/5 transition-colors flex items-center gap-1.5 [&.router-link-active]:text-primary [&.router-link-active]:font-bold"
        >
          <i class="pi pi-cog text-xs"></i>
          <span>{{ $t('common.settings') }}</span>
        </router-link>
      </div>

    </div>
  </nav>
</template>

<script setup>
  import { inject, computed } from 'vue';
  import { useLocalizedRoute } from '@/composables/useLocalizedRoute';

  const userRole = inject('userRole');
  const isAdmin = computed(() => ['Admin', 'Owner'].includes(userRole?.value));
  const { to: localized } = useLocalizedRoute();
</script>
