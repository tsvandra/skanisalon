const fs = require('fs');
let content = fs.readFileSync('soluvion-web/src/components/admin/calendar/AppointmentEditorModal.vue', 'utf8');

const methods = "const addExtraProduct = () => {\n  if (newExtraProduct.value) {\n    const prod = allProducts.value.find(p => p.id === newExtraProduct.value);\n    form.value.extraMaterials.push({ productId: prod.id, name: prod.name, quantity: newExtraQty.value, unit: prod.unit, saveAsDefault: newExtraSaveDefault.value });\n    newExtraProduct.value = null; newExtraQty.value = 1; newExtraSaveDefault.value = false; showAddExtraProduct.value = false;\n  }\n};\nconst removeExtraProduct = (idx) => { form.value.extraMaterials.splice(idx, 1); };";

if (!content.includes('addExtraProduct = ()')) {
    content = content.replace("const handleNewItems = (newItems) => {", methods + "\n  const handleNewItems = (newItems) => {");
    fs.writeFileSync('soluvion-web/src/components/admin/calendar/AppointmentEditorModal.vue', content, 'utf8');
    console.log('Fixed methods!');
} else {
    console.log('Methods already present.');
}