<script setup>
  import { ref } from 'vue';
  import { useRouter } from 'vue-router';
  import api from '@/services/api';

  const router = useRouter();
  const username = ref('');
  const password = ref('');
  const errorMsg = ref('');
  const isLoading = ref(false);

  const handleLogin = async () => {
    errorMsg.value = '';
    isLoading.value = true;

    console.log("Bejelentkezés megkezdése...", username.value); // Debug log

    try {
      // Az API controller Query paramétereket vár (url?username=...&password=...)
      // Az axios ezt a 'params' objektummal kezeli elegánsan
      const res = await api.post('/api/Auth/login', {
        username: username.value,
        password: password.value
      });
      // A backend sima szövegként (string) adja vissza a tokent,
      // az axios ezt is a .data-ba teszi
      console.log("Sikeres válasz:", res.data);
      const token = res.data;

      localStorage.setItem('salon_token', token);

      router.push('/');
      setTimeout(() => window.location.reload(), 100);

    } catch (err) {
      console.error("Hiba történt:", err);
      errorMsg.value = err.response?.data || 'Hibás felhasználónév vagy jelszó!';
    } finally {
      isLoading.value = false;
    }
  };
</script>

<template>
  <div class="min-h-[80vh] flex justify-center items-center bg-background p-4">

    <div class="bg-surface p-8 rounded-xl shadow-xl w-full max-w-md text-center border-t-4 border-primary">
      <h2 class="text-text text-2xl font-bold mb-2">Admin Belépés</h2>
      <p class="text-text-muted text-sm mb-8">Skani Salon Management</p>

      <form @submit.prevent="handleLogin">

        <div class="mb-6 text-left">
          <label for="username" class="block mb-2 font-bold text-text">Felhasználónév</label>
          <input id="username"
                 name="username"
                 type="text"
                 v-model="username"
                 required
                 placeholder="Írd be a neved..."
                 class="w-full p-3 border border-text/20 rounded-lg text-base bg-background text-text focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary transition-colors" />
        </div>

        <div class="mb-6 text-left">
          <label for="password" class="block mb-2 font-bold text-text">Jelszó</label>
          <input id="password"
                 name="password"
                 type="password"
                 v-model="password"
                 required
                 placeholder="••••••••"
                 class="w-full p-3 border border-text/20 rounded-lg text-base bg-background text-text focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary transition-colors" />
        </div>

        <p v-if="errorMsg" class="text-red-500 mb-4 text-sm font-medium">{{ errorMsg }}</p>

        <button type="submit"
                :disabled="isLoading"
                class="w-full p-3 bg-primary text-black font-bold rounded-lg cursor-pointer transition-all duration-300 hover:brightness-90 disabled:bg-gray-400 disabled:cursor-not-allowed shadow-md">
          <i v-if="isLoading" class="pi pi-spin pi-spinner mr-2"></i>
          {{ isLoading ? 'Belépés folyamatban...' : 'Belépés' }}
        </button>

      </form>
    </div>

  </div>
</template>
