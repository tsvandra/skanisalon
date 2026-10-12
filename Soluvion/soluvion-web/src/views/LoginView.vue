<script setup>
  import { ref, computed } from 'vue';
  import { useRouter, useRoute } from 'vue-router';
  import { useI18n } from 'vue-i18n';
  import api from '@/services/api';
  import { useAuthStore } from '@/stores/authStore';
  import { useCompanyStore } from '@/stores/companyStore';
  import { useLocalizedRoute } from '@/composables/useLocalizedRoute';

  const { t } = useI18n();
  const router = useRouter();
  const route = useRoute();
  const authStore = useAuthStore();
  const companyStore = useCompanyStore();
  const { go } = useLocalizedRoute();

  const companyName = computed(() => companyStore.company?.name || 'BeeMagic Salon');
  const username = ref('');
  const password = ref('');
  const errorMsg = ref('');
  const isLoading = ref(false);

  const handleLogin = async () => {
    errorMsg.value = '';
    isLoading.value = true;

    try {
      const res = await api.post('/api/Auth/login', {
        username: username.value,
        password: password.value
      });

      const token = res.data.token;
      authStore.setToken(token);

      const redirectPath = route.query.redirect;
      if (redirectPath) {
        router.push(redirectPath);
      } else {
        const queryParams = route.query.forceTenant ? { forceTenant: route.query.forceTenant } : undefined;
        go('dashboard', { query: queryParams });
      }
    } catch (err) {
      console.error("Hiba történt:", err);
      errorMsg.value = err.response?.data || t('login.invalidCredentials');
    } finally {
      isLoading.value = false;
    }
  };
</script>

<template>
  <div class="min-h-[80vh] flex justify-center items-center bg-background p-4">

    <div class="bg-surface p-8 rounded-xl shadow-xl w-full max-w-md text-center border-t-4 border-primary">
      <h2 class="text-text text-2xl font-bold mb-2">{{ $t('login.title') }}</h2>
      <p class="text-text-muted text-sm mb-8">{{ companyName }} Management</p>

      <form @submit.prevent="handleLogin">

        <div class="mb-6 text-left">
          <label for="username" class="block mb-2 font-bold text-text">{{ $t('login.username') }}</label>
          <input id="username"
                 name="username"
                 type="text"
                 v-model="username"
                 required
                 :placeholder="$t('login.usernamePlaceholder')"
                 class="w-full p-3 border border-text/20 rounded-lg text-base bg-background text-text focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary transition-colors" />
        </div>

        <div class="mb-6 text-left">
          <label for="password" class="block mb-2 font-bold text-text">{{ $t('login.password') }}</label>
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
          {{ isLoading ? $t('login.submitting') : $t('login.submit') }}
        </button>

      </form>
    </div>

  </div>
</template>
