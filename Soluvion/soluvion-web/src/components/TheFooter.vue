<script setup>
  import { inject, computed } from 'vue';

  const company = inject('company');
  const isLoggedIn = inject('isLoggedIn');
  const userRole = inject('userRole');

  const footerStyle = computed(() => {
    const height = company.value?.footerHeight || 250;

    const baseStyle = {
      minHeight: `${height}px`,
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

    // Eltávolítottuk a fix #2c2c2c-t, hogy a Tailwind bg-surface érvényesüljön!
    return baseStyle;
  });
</script>

<template>
  <footer class="relative flex items-end justify-center mt-auto overflow-hidden bg-surface text-text shadow-inner" :style="footerStyle">

    <div class="absolute inset-0 bg-gradient-to-t from-black/95 to-black/10 pointer-events-none"></div>

    <div class="relative z-10 text-center w-full px-4 pt-6 pb-24 md:pb-6 h-full flex flex-col justify-end items-center">
      <div>
        <h3 class="text-primary mb-2 text-2xl font-bold drop-shadow-md tracking-wide m-0">
          {{ company?.name || 'Skani Salon' }}
        </h3>

        <p class="text-text-muted text-sm font-medium tracking-wider m-0">
          &copy; {{ new Date().getFullYear() }} {{ company?.name || 'Skani Salon' }}. {{ $t('footer.rights') }}
        </p>

        <p v-if="isLoggedIn && userRole" class="text-text-muted text-xs mt-2 m-0 flex items-center justify-center gap-1.5">
          <span>{{ $t('footer.role') }}:</span>
          <span class="text-primary font-bold">{{ userRole }}</span>
        </p>
      </div>
    </div>
  </footer>
</template>
