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
  <div class="p-2 md:p-4 animate-fade-in">

    <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
      <div class="md:col-span-2">
        <label class="block mb-2 font-bold text-text-muted text-xs uppercase tracking-wider">Cégnév (Weboldalon megjelenő)</label>
        <InputText v-model="companyData.name"
                   class="w-full !bg-background !border-text/20 !text-text hover:!border-primary focus:!border-primary focus:!ring-1 focus:!ring-primary transition-colors rounded-lg p-3 shadow-sm" />
      </div>

      <div>
        <label class="block mb-2 font-bold text-text-muted text-xs uppercase tracking-wider">Email cím</label>
        <InputText v-model="companyData.email"
                   type="email"
                   class="w-full !bg-background !border-text/20 !text-text hover:!border-primary focus:!border-primary focus:!ring-1 focus:!ring-primary transition-colors rounded-lg p-3 shadow-sm" />
      </div>

      <div>
        <label class="block mb-2 font-bold text-text-muted text-xs uppercase tracking-wider">Telefonszám</label>
        <InputText v-model="companyData.phone"
                   class="w-full !bg-background !border-text/20 !text-text hover:!border-primary focus:!border-primary focus:!ring-1 focus:!ring-primary transition-colors rounded-lg p-3 shadow-sm" />
      </div>
    </div>

    <h3 class="text-lg font-light text-primary mb-6 mt-10 uppercase tracking-widest border-b border-text/10 pb-2">Címadatok</h3>

    <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
      <div>
        <label class="block mb-2 font-bold text-text-muted text-xs uppercase tracking-wider">Irányítószám</label>
        <InputText v-model="companyData.postalCode"
                   class="w-full !bg-background !border-text/20 !text-text hover:!border-primary focus:!border-primary focus:!ring-1 focus:!ring-primary transition-colors rounded-lg p-3 shadow-sm" />
      </div>

      <div>
        <label class="block mb-2 font-bold text-text-muted text-xs uppercase tracking-wider">Város</label>
        <InputText v-model="companyData.city"
                   class="w-full !bg-background !border-text/20 !text-text hover:!border-primary focus:!border-primary focus:!ring-1 focus:!ring-primary transition-colors rounded-lg p-3 shadow-sm" />
      </div>

      <div class="md:col-span-2 grid grid-cols-4 gap-6">
        <div class="col-span-3">
          <label class="block mb-2 font-bold text-text-muted text-xs uppercase tracking-wider">Utca</label>
          <InputText v-model="companyData.streetName"
                     class="w-full !bg-background !border-text/20 !text-text hover:!border-primary focus:!border-primary focus:!ring-1 focus:!ring-primary transition-colors rounded-lg p-3 shadow-sm" />
        </div>
        <div class="col-span-1">
          <label class="block mb-2 font-bold text-text-muted text-xs uppercase tracking-wider">Házszám</label>
          <InputText v-model="companyData.houseNumber"
                     class="w-full !bg-background !border-text/20 !text-text hover:!border-primary focus:!border-primary focus:!ring-1 focus:!ring-primary transition-colors rounded-lg p-3 shadow-sm" />
        </div>
      </div>
    </div>

  </div>
</template>
