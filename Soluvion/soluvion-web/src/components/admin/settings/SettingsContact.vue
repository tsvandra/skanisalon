<script setup>
  import InputText from 'primevue/inputtext';

  defineProps({
    companyData: {
      type: Object,
      required: true
    }
  });
</script>

<template>
  <div class="settings-contact">
    <div class="form-grid">
      <div class="field">
        <label>Cégnév (Weboldalon megjelenő)</label>
        <InputText v-model="companyData.name" class="w-full" />
      </div>
      <div class="field">
        <label>Email</label>
        <InputText v-model="companyData.email" class="w-full" />
      </div>
      <div class="field">
        <label>Telefonszám</label>
        <InputText v-model="companyData.phone" class="w-full" />
      </div>

      <h3>Cím</h3>
      <div class="field-group">
        <div class="field">
          <label>Irányítószám</label>
          <InputText v-model="companyData.postalCode" class="w-full" />
        </div>
        <div class="field">
          <label>Város</label>
          <InputText v-model="companyData.city" class="w-full" />
        </div>
      </div>
      <div class="field-group">
        <div class="field">
          <label>Utca</label>
          <InputText v-model="companyData.streetName" class="w-full" />
        </div>
        <div class="field">
          <label>Házszám</label>
          <InputText v-model="companyData.houseNumber" class="w-full" />
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
  h3 {
    margin-top: 1.5rem;
    margin-bottom: 0.5rem;
    color: #555;
  }

  .field {
    margin-bottom: 1.5rem;
  }

    .field label {
      display: block;
      margin-bottom: 0.5rem;
      font-weight: bold;
      color: #333;
    }

  .w-full {
    width: 100%;
  }

  .field-group {
    display: flex;
    gap: 1rem;
  }

    .field-group .field {
      flex: 1;
    }
</style>
