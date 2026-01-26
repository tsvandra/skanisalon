<script setup>
  import { ref } from 'vue';
  import { useRouter } from 'vue-router';

  const router = useRouter();
  const username = ref('');
  const password = ref('');
  const errorMsg = ref('');
  const isLoading = ref(false);

  const handleLogin = async () => {
    errorMsg.value = '';
    isLoading.value = true;

    try {
      const url = new URL('https://localhost:7113/api/Auth/login');
      url.searchParams.append('username', username.value);
      url.searchParams.append('password', password.value);

      const res = await fetch(url, {
        method: 'POST'
      });

      if (!res.ok) {
        throw new Error('Hibás felhasználónév vagy jelszó!');
      }

      const token = await res.text();

      localStorage.setItem('salon_token', token);

      router.push('/');

      setTimeout(() => window.location.reload(), 100);

    } catch (err) {
      errorMsg.value = err.message;
    } finally {
      isLoading.value = false;
    }
  };
</script>

<template>
  <div class="login-container">
    <div class="login-card">
      <h2>Admin Belépés</h2>
      <p class="subtitle">Skani Salon Management</p>

      <form @submit.prevent="handleLogin">
        <div class="form-group">
          <label>Felhasználónév</label>
          <input type="text" v-model="username" required placeholder="Írd be a neved..." />
        </div>

        <div class="from-group">
             <label>Jelszó</label>
             <input type="password" v-model="password" required placeholder="••••••••" />
        </div>

        <p v-if="errorMsg" class="error">{{ errorMsg }}</p>

        <button type="submit" :disabled="isLoading">
          {{ isLoading ? 'Belépés folyamatban...' : 'Belépés' }}
        </button>
      </form>
    </div>
  </div>
</template>

<style scoped>
  .login-container {
    display: flex;
    justify-content: center;
    align-items: center;
    min-height: 80vh; /* Majdnem teljes képernyő magasság */
    background-color: #f8f9fa;
  }

  .login-card {
    background: white;
    padding: 2rem;
    border-radius: 12px;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
    width: 100%;
    max-width: 400px;
    text-align: center;
    border-top: 5px solid #d4af37; /* Arany csík a tetején */
  }

  h2 {
    color: #333;
    margin-bottom: 0.5rem;
  }

  .subtitle {
    color: #666;
    margin-bottom: 2rem;
    font-size: 0.9rem;
  }

  .form-group {
    margin-bottom: 1.5rem;
    text-align: left;
  }

  label {
    display: block;
    margin-bottom: 0.5rem;
    color: #333;
    font-weight: bold;
  }

  input {
    width: 100%;
    padding: 10px;
    border: 1px solid #ddd;
    border-radius: 6px;
    font-size: 1rem;
  }

    input:focus {
      outline: none;
      border-color: #d4af37; /* Arany keret fókuszkor */
    }

  button {
    width: 100%;
    padding: 12px;
    background-color: #d4af37; /* Arany gomb */
    color: white;
    border: none;
    border-radius: 6px;
    font-size: 1rem;
    font-weight: bold;
    cursor: pointer;
    transition: background 0.3s;
  }

    button:hover {
      background-color: #b5952f; /* Sötétebb arany */
    }

    button:disabled {
      background-color: #ccc;
      cursor: not-allowed;
    }

  .error {
    color: red;
    margin-bottom: 1rem;
    font-size: 0.9rem;
  }
</style>
