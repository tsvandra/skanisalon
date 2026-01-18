<script setup>
  import { ref } from "vue";
  import Menubar from 'primevue/menubar';
  import { useRouter } from 'vue-router';

  const router = useRouter();

  const items = ref([
    {
      label: 'Kezdolap',
      icon: 'pi pi-home',
      command: () => { router.push(''); }
    },
    {
      label: 'Arlista',
      icon: 'pi pi-list',
      command: () => { router.push('/szolgaltatasok') }
    },
    {
      label: 'Galéria',
      icon: 'pi pi-images',
      command: () => { router.push('/galeria'); }
    },
    {
      label: 'Kapcsolat',
      icon: 'pi pi-envelope',
      command: () => { router.push('/kapcsolat'); }
    }
  ]);
</script>

<template>
  <div class="card">
    <Menubar :model="items">
      <template #start>
        <span style="font-weight: bold; font-size: 1.2rem; margin-right: 1rem;">
          SKANI SALON
        </span>
      </template>
    </Menubar>
  </div>
</template>
