const fs = require('fs');
let content = fs.readFileSync('soluvion-web/src/components/admin/calendar/AppointmentEditorModal.vue', 'utf8');

// Inject refs
const tRefs = '  const isEditing = ref(false);';
const rRefs = '  const isEditing = ref(false);\n  const allProducts = ref([]);\n  const showAddExtraProduct = ref(false);\n  const newExtraProduct = ref(null);\n  const newExtraQty = ref(1);\n  const newExtraSaveDefault = ref(false);';
if (!content.includes('allProducts = ref')) {
    content = content.replace(tRefs, rRefs);
}

// Inject onMounted
const tOn = '  onMounted(() => {\n    fetchServicesForAdmin();';
const tOn2 = '  onMounted(() => {\r\n    fetchServicesForAdmin();';
const rOn = '  onMounted(() => {\n    productApi.getAllProducts().then(res => allProducts.value = res.data || []).catch(e => console.error(e));\n    fetchServicesForAdmin();';
if (!content.includes('productApi.getAllProducts()')) {
    if (content.includes(tOn)) content = content.replace(tOn, rOn);
    else if (content.includes(tOn2)) content = content.replace(tOn2, rOn);
}

// Ensure extraMaterials is in form init
const tForm = '    notes: \'\',\n  });';
const tForm2 = '    notes: \'\',\r\n  });';
const rForm = '    notes: \'\',\n    extraMaterials: []\n  });';
if (!content.includes('extraMaterials: []')) {
    if (content.includes(tForm)) content = content.replace(tForm, rForm);
    else if (content.includes(tForm2)) content = content.replace(tForm2, rForm);
}

// When editData is loaded, parse extraMaterials
const tLoad = '      notes: props.editData.customerNotes || \'\'\n    };';
const tLoad2 = '      notes: props.editData.customerNotes || \'\'\r\n    };';
const rLoad = '      notes: props.editData.customerNotes || \'\',\n      extraMaterials: props.editData.extraMaterials ? (() => { try { return typeof props.editData.extraMaterials === "string" ? JSON.parse(props.editData.extraMaterials) : props.editData.extraMaterials; } catch(e) { return []; } })() : []\n    };';
if (!content.includes('props.editData.extraMaterials')) {
    if (content.includes(tLoad)) content = content.replace(tLoad, rLoad);
    else if (content.includes(tLoad2)) content = content.replace(tLoad2, rLoad);
}

fs.writeFileSync('soluvion-web/src/components/admin/calendar/AppointmentEditorModal.vue', content, 'utf8');
console.log('Fixed refs!');