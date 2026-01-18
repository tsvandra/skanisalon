<script setup>
  import { RouterLink, RouterView } from 'vue-router'
  import AppHeader from './components/Appheader.vue'
</script>

<template>
  <header>
    <AppHeader />
  </header>

  <main>
    <RouterView />
  </main>

  <footer style="text-align:center; padding: 20px; background: #f4f4f4; margin-top: 50px;">
    <p>&copy; 2026 Skani Salon. Minden jog fentartva.</p>
  </footer>
</template>

<style>
  body {
      margin: 0;
      font-family: var(--font-family);
  }
</style>

<style scoped>
  .container {
    font-family: Arial, sans-serif;
    max-width: 800px;
    margin: 0 auto;
    padding: 20px;
  }

  header {
    background-color: #ff98a6;
    color: white;
    padding: 20px;
    text-align: center;
    border-radius: 8px;
    margin-bottom: 20px;
  }

  h1 {
    margin: 0;
  }

  footer {
    margin-top: 50px;
    text-align: center;
    font-size: 0.8em;
    color: #666;
  }
</style>
