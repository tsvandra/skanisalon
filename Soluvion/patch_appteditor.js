const fs = require('fs');
let content = fs.readFileSync('soluvion-web/src/components/admin/calendar/AppointmentEditorModal.vue', 'utf8');

// 1. Add API imports
content = content.replace(
    "import bookingApi from '@/services/bookingApi';",
    "import bookingApi from '@/services/bookingApi';\nimport productApi from '@/services/productApi';\nimport apiClient from '@/services/api';"
);

// 2. Add extraMaterials state
if (!content.includes('extraMaterials')) {
    content = content.replace(
        "notes: '',",
        "notes: '',\n  extraMaterials: [],"
    );
}

// 3. Add products array state
if (!content.includes('allProducts')) {
    content = content.replace(
        "const loading = ref(false);",
        "const loading = ref(false);\nconst allProducts = ref([]);\nconst showAddExtraProduct = ref(false);\nconst newExtraProduct = ref(null);\nconst newExtraQty = ref(1);\nconst newExtraSaveDefault = ref(false);"
    );
}

// 4. Load products on mount
if (!content.includes('getAllProducts')) {
    content = content.replace(
        "const loadInitialData = async () => {",
        "const loadInitialData = async () => {\n  productApi.getAllProducts().then(res => allProducts.value = res.data || []).catch(e => console.error(e));"
    );
}

// 5. Populate extraMaterials on edit
if (!content.includes('form.value.extraMaterials = ')) {
    content = content.replace(
        "form.value.notes = props.appointmentData.customerNotes || '';",
        "form.value.notes = props.appointmentData.customerNotes || '';\n    if(props.appointmentData.extraMaterials) {\n      try { form.value.extraMaterials = JSON.parse(props.appointmentData.extraMaterials); } catch(e){ form.value.extraMaterials = []; }\n    } else form.value.extraMaterials = [];"
    );
}

// 6. Provide methods to add/remove
const methodsToAdd = 
const addExtraProduct = () => {
  if (newExtraProduct.value) {
    const prod = allProducts.value.find(p => p.id === newExtraProduct.value);
    form.value.extraMaterials.push({
      productId: prod.id,
      name: prod.name,
      quantity: newExtraQty.value,
      unit: prod.unit,
      saveAsDefault: newExtraSaveDefault.value
    });
    newExtraProduct.value = null;
    newExtraQty.value = 1;
    newExtraSaveDefault.value = false;
    showAddExtraProduct.value = false;
  }
};
const removeExtraProduct = (idx) => { form.value.extraMaterials.splice(idx, 1); };
;

content = content.replace(
    "const handleNewItems = (items) => {",
    methodsToAdd + "\nconst handleNewItems = (items) => {"
);

// 7. Inject UI
const uiTemplate = 
        <div class="border-t border-text/10 pt-4">
          <label class="block text-[10px] md:text-xs font-bold text-text-muted mb-2 uppercase flex items-center gap-1"><i class="pi pi-box"></i> További felhasznált anyagok</label>
          <div class="bg-background border border-text/10 rounded-xl p-3 flex flex-col gap-2 mb-4">
            <div v-for="(em, idx) in form.extraMaterials" :key="idx" class="flex justify-between items-center bg-surface p-2 rounded-lg border border-text/5">
              <div class="flex flex-col">
                <span class="text-xs font-bold text-text">{{ em.name }}</span>
                <span v-if="em.saveAsDefault" class="text-[10px] text-green-500 font-bold"><i class="pi pi-check"></i> Alapértelmezettként mentve az ügyfélhez</span>
              </div>
              <div class="flex items-center gap-3">
                <span class="text-sm font-black text-orange-500">{{ em.quantity }} {{ em.unit }}</span>
                <button @click="removeExtraProduct(idx)" class="text-red-500 hover:bg-red-500/10 w-6 h-6 rounded flex items-center justify-center transition-colors"><i class="pi pi-trash text-xs"></i></button>
              </div>
            </div>
            
            <div v-if="showAddExtraProduct" class="bg-surface p-3 rounded-lg border border-primary/20 flex flex-col gap-3 mt-2">
              <select v-model="newExtraProduct" class="w-full h-[40px] bg-background border border-text/20 rounded-lg px-2 text-sm">
                <option :value="null" disabled>Válassz terméket...</option>
                <option v-for="p in allProducts" :key="p.id" :value="p.id">{{ p.name }} ({{ p.currentStock }} {{ p.unit }} raktáron)</option>
              </select>
              <div class="flex items-center gap-2">
                <input type="number" v-model="newExtraQty" class="w-20 h-[40px] bg-background border border-text/20 rounded-lg px-2 text-sm text-center" min="1">
                <span class="text-xs text-text-muted">mennyiség</span>
              </div>
              <label class="flex items-center gap-2 cursor-pointer mt-1">
                <input type="checkbox" v-model="newExtraSaveDefault" class="w-4 h-4 text-primary rounded border-text/30 focus:ring-primary">
                <span class="text-xs font-bold text-text">Termék hozzáadása az ügyfél alapértelmezett anyagaihoz is</span>
              </label>
              <div class="flex justify-end gap-2 mt-2">
                <button @click="showAddExtraProduct = false" class="px-3 h-[32px] rounded-lg text-xs font-bold text-text hover:bg-text/10">Mégsem</button>
                <button @click="addExtraProduct" :disabled="!newExtraProduct" class="px-4 h-[32px] rounded-lg text-xs font-bold bg-primary text-white hover:brightness-110 disabled:opacity-50">Hozzáad</button>
              </div>
            </div>
            <button v-else @click="showAddExtraProduct = true" class="text-xs font-bold text-primary hover:brightness-110 flex items-center justify-center gap-1 py-2 border border-dashed border-primary/30 rounded-lg hover:bg-primary/5 transition-colors">
              <i class="pi pi-plus"></i> Új anyag hozzáadása
            </button>
          </div>
        </div>
;

content = content.replace(
    '<AppointmentCart :items="form.items"\n                           @remove="removeFormItem" />\n        </div>',
    '<AppointmentCart :items="form.items"\n                           @remove="removeFormItem" />\n        </div>\n' + uiTemplate
);
content = content.replace(
    '<AppointmentCart :items="form.items"\r\n                           @remove="removeFormItem" />\r\n        </div>',
    '<AppointmentCart :items="form.items"\r\n                           @remove="removeFormItem" />\r\n        </div>\r\n' + uiTemplate
);

// 8. On Save, attach extraMaterials to payload and also update customer FormulaList if needed!
const saveCode = 
      // Update customer default formulas if requested
      const defaultsToAdd = form.value.extraMaterials.filter(em => em.saveAsDefault);
      if (defaultsToAdd.length > 0 && form.value.customerId && form.value.customerId !== 'new') {
        try {
          const custRes = await bookingApi.getCustomerById(form.value.customerId);
          const customer = custRes.data;
          let formula = [];
          if (customer.attributes && customer.attributes.FormulaList) {
             try { formula = JSON.parse(customer.attributes.FormulaList); } catch(e){}
          }
          
          defaultsToAdd.forEach(d => {
             const existing = formula.find(f => f.productId === d.productId);
             if (existing) { existing.quantity += d.quantity; }
             else { formula.push({ productId: d.productId, quantity: d.quantity, notes: 'Hozzáadva foglalás szerkesztésből' }); }
          });
          
          if (!customer.attributes) customer.attributes = {};
          customer.attributes.FormulaList = JSON.stringify(formula);
          await apiClient.put('/api/CompanyAttributes/customer-attributes/' + customer.id, customer.attributes);
        } catch(err) {
          console.error("Nem sikerült menteni az ügyfél alapértelmezett anyagait", err);
        }
      }

      const payload = {
        customerId: customerId,
        startDateTime: startIso,
        endDateTime: endIso,
        customerNotes: form.value.notes,
        extraMaterials: JSON.stringify(form.value.extraMaterials.map(em => ({ productId: em.productId, quantity: em.quantity, name: em.name }))),
        items: form.value.items.map(i => ({ serviceVariantId: i.variantId, employeeId: i.employeeId }))
      };
;

content = content.replace(
    /const payload = \{\r?\n\s*customerId: customerId,\r?\n\s*startDateTime: startIso,\r?\n\s*endDateTime: endIso,\r?\n\s*customerNotes: form\.value\.notes,\r?\n\s*items: form\.value\.items\.map\(i => \(\{ serviceVariantId: i\.variantId, employeeId: i\.employeeId \}\)\)\r?\n\s*\};/,
    saveCode
);

fs.writeFileSync('soluvion-web/src/components/admin/calendar/AppointmentEditorModal.vue', content, 'utf8');