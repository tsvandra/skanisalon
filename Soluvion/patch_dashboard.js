const fs = require('fs');
let content = fs.readFileSync('soluvion-web/src/views/DashboardView.vue', 'utf8');

// 1. Add import
if (!content.includes('LowStockPlannerModal')) {
    content = content.replace(
        "import MaterialWrapUpModal from '@/components/admin/dashboard/MaterialWrapUpModal.vue';",
        "import MaterialWrapUpModal from '@/components/admin/dashboard/MaterialWrapUpModal.vue';\nimport LowStockPlannerModal from '@/components/admin/dashboard/LowStockPlannerModal.vue';"
    );
}

// 2. Add refs
if (!content.includes('isPlannerOpen')) {
    content = content.replace(
        "const isWrapUpModalOpen = ref(false);",
        "const isWrapUpModalOpen = ref(false);\nconst isPlannerOpen = ref(false);\nconst selectedLowStockProduct = ref(null);\nconst openPlanner = (p) => { selectedLowStockProduct.value = p; isPlannerOpen.value = true; };"
    );
}

// 3. Add Modal tag
if (!content.includes('<LowStockPlannerModal')) {
    content = content.replace(
        "<!-- Napi Zárás Modal -->",
        "<!-- Készlet Tervező Modal -->\n    <LowStockPlannerModal :is-open=\"isPlannerOpen\" :product=\"selectedLowStockProduct\" :services=\"services\" @close=\"isPlannerOpen = false\" />\n\n    <!-- Napi Zárás Modal -->"
    );
}

// 4. Update the card HTML
const oldCardHTML = <router-link v-for="product in lowStockProducts" :key="product.id" to="/raktar" 
             class="bg-background border border-orange-500/30 p-4 rounded-xl cursor-pointer hover:bg-orange-500/10 hover:border-orange-500/50 transition-all flex items-center justify-between group no-underline">
          <div>
            <div class="font-bold text-text group-hover:text-primary transition-colors flex items-center gap-2">
              <i class="pi pi-box text-text-muted text-sm"></i> {{ product.name }}
            </div>
            <div class="text-xs text-text-muted mt-1 font-medium">
              Készlet: <span class="text-orange-500 font-bold">{{ product.currentStock }} db</span> (Minimum: {{ product.lowStockThreshold }} db)
            </div>
          </div>
          <i class="pi pi-angle-right text-text-muted group-hover:text-orange-500 transition-colors ml-2"></i>
        </router-link>;

const newCardHTML = <div v-for="product in lowStockProducts" :key="product.id" @click="openPlanner(product)"
             class="bg-background border border-orange-500/30 p-4 rounded-xl cursor-pointer hover:bg-orange-500/10 hover:border-orange-500/50 transition-all flex items-center justify-between group">
          <div>
            <div class="font-bold text-text group-hover:text-primary transition-colors flex items-center gap-2">
              <i class="pi pi-box text-text-muted text-sm"></i> {{ product.name }}
            </div>
            <div class="text-xs text-text-muted mt-1 font-medium">
              Készlet: <span class="text-orange-500 font-bold">{{ product.currentStock }} db</span> (Minimum: {{ product.lowStockThreshold }} db)
            </div>
            <div v-if="product.upcoming30DaysUsage !== undefined" class="text-xs font-bold text-orange-500 mt-2 flex items-center gap-1">
              <i class="pi pi-calendar"></i> 30 napos várható fogyás: {{ product.upcoming30DaysUsage }} db
            </div>
          </div>
          <i class="pi pi-angle-right text-text-muted group-hover:text-orange-500 transition-colors ml-2"></i>
        </div>;

content = content.replace(/<router-link v-for="product in lowStockProducts"[\s\S]*?<\/router-link>/, newCardHTML);

// 5. Update fetchLowStockProducts
const fetchReplacement = const fetchLowStockProducts = async () => {
  try {
    const response = await apiClient.get('/api/products');
    const allProducts = response.data || [];
    lowStockProducts.value = allProducts.filter(p => !p.isDeleted && p.currentStock <= p.lowStockThreshold);

    if (lowStockProducts.value.length > 0) {
      try {
        const now = new Date();
        const next30 = new Date();
        next30.setDate(now.getDate() + 30);
        
        const [appsRes, custsRes] = await Promise.all([
          appointmentApi.getAppointments(now, next30),
          apiClient.get('/api/customers')
        ]);
        
        const apps = appsRes.data?. || appsRes.data || [];
        const customers = custsRes.data?. || custsRes.data || [];
        
        const usageMap = {};
        lowStockProducts.value.forEach(p => usageMap[p.id] = 0);
        
        apps.forEach(app => {
          app.items?.forEach(item => {
            const svc = services.value.find(s => s.variants && s.variants.some(v => v.id === item.serviceVariantId));
            if (svc) {
              const variant = svc.variants.find(v => v.id === item.serviceVariantId);
              if (variant && variant.defaultProducts) {
                variant.defaultProducts.forEach(dp => {
                  if (usageMap[dp.productId] !== undefined) {
                    usageMap[dp.productId] += dp.defaultQuantity;
                  }
                });
              }
            }
          });
          
          const customer = customers.find(c => c.id === app.customerId);
          if (customer && customer.attributes && customer.attributes.FormulaList) {
            try {
              const formula = JSON.parse(customer.attributes.FormulaList);
              formula.forEach(fi => {
                if (usageMap[fi.productId] !== undefined) {
                  usageMap[fi.productId] += fi.quantity;
                }
              });
            } catch(e) {}
          }
        });
        
        lowStockProducts.value.forEach(p => {
          const rawQuantity = usageMap[p.id] || 0;
          const pkgSize = (p.packageSize && p.packageSize > 0) ? p.packageSize : 1;
          const piecesNeeded = rawQuantity > 0 ? (rawQuantity / pkgSize) : 0;
          p.upcoming30DaysUsage = Math.ceil(piecesNeeded * 10) / 10;
          p.rawUpcomingUsage = rawQuantity;
        });
      } catch(e) {
         console.error("Hiba a fogyás számolásakor", e);
      }
    }
  } catch (error) {
    console.error(error);
  }
};;

content = content.replace(/const fetchLowStockProducts = async \(\) => \{[\s\S]*?lowStockProducts\.value = allProducts\.filter\(p => !p\.isDeleted && p\.currentStock <= p\.lowStockThreshold\);\n\n    \} catch \(error\) \{\n      console\.error\(error\);\n    \}\n  \};/, fetchReplacement);

fs.writeFileSync('soluvion-web/src/views/DashboardView.vue', content, 'utf8');