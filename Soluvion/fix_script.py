import re

with open('soluvion-web/src/components/admin/customers/CustomerHistoryModal.vue', 'r', encoding='utf-8') as f:
    content = f.read()

# Replace variables
content = re.sub(r'const notes = ref\(""\); const formula = ref\(""\);', 'const notes = ref("");\nconst formulaItems = ref([]);\nconst allProducts = ref([]);\nconst showAddProduct = ref(false);\nconst selectedNewProduct = ref(null);\nconst newQuantity = ref(0);', content)

content = re.sub(r'const originalNotes = ref\(""\); const originalFormula = ref\(""\);', 'const originalNotes = ref("");\nconst originalFormulaItems = ref([]);', content)

# Replace computed
computed_repl = '''const isNotesChanged = computed(() => {
  return notes.value !== originalNotes.value || JSON.stringify(formulaItems.value) !== JSON.stringify(originalFormulaItems.value);
});

const unitMap = { 0: 'db', 1: 'ml', 2: 'g', 3: 'm', 4: 'cm' };
const getUnit = (productId) => {
  const p = allProducts.value.find(x => x.id === productId);
  return p ? unitMap[p.unitOfMeasure] || '-' : '-';
};

const addProduct = () => {
  if (!selectedNewProduct.value || newQuantity.value <= 0) return;
  formulaItems.value.push({
    productId: selectedNewProduct.value.id,
    productName: selectedNewProduct.value.name,
    quantity: newQuantity.value
  });
  selectedNewProduct.value = null;
  newQuantity.value = 0;
  showAddProduct.value = false;
};

const removeProduct = (idx) => {
  formulaItems.value.splice(idx, 1);
};
'''
content = re.sub(r'const isNotesChanged = computed\(\(\) => notes\.value !== originalNotes\.value \|\| formula\.value !== originalFormula\.value\);', computed_repl, content)

# Replace saveNotes
savenotes_repl = '''const saveNotes = async () => {
  if (!props.customerData?.id) return;
  savingNotes.value = true;
  try {
    const updatedAttributes = { ...(props.customerData.attributes || {}) };
    updatedAttributes.FormulaList = JSON.stringify(formulaItems.value);

    const payload = {
      fullName: props.customerData.name,
      phone: props.customerData.phone,
      email: props.customerData.email,
      attributes: updatedAttributes,
      notes: notes.value
    };

    await bookingApi.updateCustomer(props.customerData.id, payload);
    originalNotes.value = notes.value;
    originalFormulaItems.value = JSON.parse(JSON.stringify(formulaItems.value));
    emit('updated');
  } catch (error) {
    console.error("Hiba a karton mentésekor:", error);
    alert("Hiba történt a mentés során.");
  } finally {
    savingNotes.value = false;
  }
};'''
content = re.sub(r'const saveNotes = async \(\) => \{.*?\n\s*savingNotes\.value = false;\n\s*\}\n\};', savenotes_repl, content, flags=re.DOTALL)

# Replace onMounted
onmounted_repl = '''const loadProducts = async () => {
  try {
    const res = await productApi.getAllProducts();
    allProducts.value = res.data. || res.data || [];
  } catch(e) {}
};

onMounted(() => {
  if (props.customerData) {
    notes.value = props.customerData.notes || '';
    originalNotes.value = notes.value;
    
    try {
      const flist = props.customerData.attributes?.FormulaList;
      if (flist) {
        formulaItems.value = JSON.parse(flist);
      } else {
        formulaItems.value = [];
      }
    } catch(e) {
        formulaItems.value = [];
    }
    originalFormulaItems.value = JSON.parse(JSON.stringify(formulaItems.value));
    
    loadHistory();
    loadProducts();
  }
});'''
content = re.sub(r'onMounted\(\(\) => \{.*?loadHistory\(\);\n\s*\}\n\}\);', onmounted_repl, content, flags=re.DOTALL)


with open('soluvion-web/src/components/admin/customers/CustomerHistoryModal.vue', 'w', encoding='utf-8') as f:
    f.write(content)
