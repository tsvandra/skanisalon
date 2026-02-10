<script setup>
  import { inject, computed } from 'vue';

  const company = inject('company');

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
    return { ...baseStyle, background: '#2c2c2c' };
  });
</script>

<template>
  <footer class="app-footer" :style="footerStyle">
    <div class="overlay"></div>

    <div class="content">
      <div class="footer-info">
        <h3>{{ company?.name || 'Soluvion Salon' }}</h3>
        <p>&copy; {{ new Date().getFullYear() }} Minden jog fenntartva.</p>
      </div>
    </div>
  </footer>
</template>

<style scoped>
  .app-footer {
    position: relative;
    display: flex;
    align-items: flex-end;
    justify-content: center;
    color: #fff;
    margin-top: auto;
    background-color: #2c2c2c;
    overflow: hidden;
  }

  .overlay {
    position: absolute;
    inset: 0;
    background: linear-gradient(to top, rgba(0,0,0,0.95), rgba(0,0,0,0.1));
    pointer-events: none;
  }

  .content {
    position: relative;
    z-index: 2;
    text-align: center;
    width: 100%;
    padding: 1.5rem;
    height: 100%;
    display: flex;
    flex-direction: column;
    justify-content: flex-end;
    align-items: center;
  }

  .footer-info h3 {
    color: var(--primary-color, #d4af37);
    margin-bottom: 0.3rem;
    font-size: 1.4rem;
    text-shadow: 0 2px 4px rgba(0,0,0,0.8);
    margin-top: 0;
  }

  .footer-info p {
    color: #aaa;
    font-size: 0.85rem;
    margin: 0;
  }
</style>
