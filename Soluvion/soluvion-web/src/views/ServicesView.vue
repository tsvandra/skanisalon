<script setup>
  import { ref, onMounted, computed, inject, watch } from 'vue';
  import InputNumber from 'primevue/inputnumber';
  import apiClient from '@/services/api';
  import { getCompanyIdFromToken } from '@/utils/jwt';
  import { DEFAULT_COMPANY_ID } from '@/config';

  const services = ref([]);
  const loading = ref(true);
  const isLoggedIn = ref(false);

  const company = inject('company', ref(null));

  /* --- SEGÉDFÜGGVÉNYEK --- */

  const sortVariants = (variants) => {
    if (!variants) return [];
    return variants.sort((a, b) => {
      if (a.id === 0 && b.id === 0) return 0;
      if (a.id === 0) return 1;
      if (b.id === 0) return -1;
      return a.id - b.id;
    });
  };

  const formatCurrency = (val) => {
    if (val === null || val === undefined || val === 0) return '';
    return val.toLocaleString('hu-HU', { style: 'currency', currency: 'EUR', maximumFractionDigits: 0 });
  };

  /* --- ADATBETÖLTÉS --- */

  const fetchServices = async () => {
    const targetCompanyId = company?.value?.id || DEFAULT_COMPANY_ID;
    loading.value = true;
    try {
      const response = await apiClient.get('/api/Service', {
        params: { companyId: targetCompanyId }
      });

      const rawServices = response.data;

      rawServices.forEach(service => {
        if (service.variants) {
          service.variants = sortVariants(service.variants);
          service.variants.forEach(v => {
            if (v.price === 0) v.price = null;
          });
        }
        if (!service.category) service.category = "Egyéb";
      });

      // Fontos: Itt is rendezzük OrderIndex szerint, bár a backend is megteszi
      rawServices.sort((a, b) => a.orderIndex - b.orderIndex);

      services.value = rawServices;

    } catch (error) {
      console.error('Hiba a betolteskor:', error);
    } finally {
      loading.value = false;
    }
  };

  watch(
    () => company?.value?.id,
    (newId) => {
      if (newId) fetchServices();
    },
    { immediate: true }
  );

  /* --- CSOPORTOSÍTÁS LOGIKA --- */

  const groupedServices = computed(() => {
    if (!services.value) return [];

    const groups = [];
    let currentGroup = null;

    services.value.forEach(service => {
      const catName = service.category || "Egyéb";

      // Ha a sor egy MEGJEGYZÉS (nincs variánsa), az nem töri meg a csoport fejléc logikáját,
      // de a megjelenítésnél külön kezeljük.

      if (!currentGroup || currentGroup.categoryName !== catName) {

        // Keresünk egy olyan elemet a csoportban, ami NEM megjegyzés, hogy abból vegyük a fejlécet
        // (Ha az első elem megjegyzés, akkor várunk a következőre)
        let headerSource = service.variants && service.variants.length > 0 ? service : null;

        currentGroup = {
          categoryName: catName,
          headerVariants: headerSource ? [...headerSource.variants] : [],
          items: []
        };
        groups.push(currentGroup);
      }

      // Ha még nincs header beállítva (mert az első elem megjegyzés volt), de ez most egy normál service
      if (currentGroup.headerVariants.length === 0 && service.variants && service.variants.length > 0) {
        currentGroup.headerVariants = [...service.variants];
      }

      currentGroup.items.push(service);
    });

    return groups;
  });

  /* --- SZERKESZTÉS & MENTÉS --- */

  const saveService = async (serviceItem) => {
    try {
      const payload = JSON.parse(JSON.stringify(serviceItem));
      if (payload.variants) {
        payload.variants.forEach(v => {
          if (v.price === null || v.price === undefined) v.price = 0;
        });
      }

      const response = await apiClient.put(`/api/Service/${serviceItem.id}`, payload);

      if (response.status === 200 && response.data) {
        const index = services.value.findIndex(s => s.id === serviceItem.id);
        if (index !== -1) {
          const updated = response.data;
          if (updated.variants) {
            updated.variants = sortVariants(updated.variants);
            updated.variants.forEach(v => { if (v.price === 0) v.price = null; });
          }
          if (!updated.category) updated.category = "Egyéb";

          services.value[index] = { ...services.value[index], ...updated };
        }
      }
    } catch (err) {
