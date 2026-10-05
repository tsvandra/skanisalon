const fs = require('fs');
let content = fs.readFileSync('soluvion-web/src/views/CustomersView.vue', 'utf8');

if(!content.includes('useRoute')) {
    content = content.replace(/import \{ ref, computed, onMounted \} from 'vue';/, "import { ref, computed, onMounted, watch } from 'vue';\n  import { useRoute, useRouter } from 'vue-router';");
}

if(!content.includes('const route = useRoute();')) {
    content = content.replace(/const customers = ref\(\[\]\);/, "const route = useRoute();\n  const router = useRouter();\n  const customers = ref([]);");
}

content = content.replace(
    /const closeHistoryModal = \(\) => \{\r?\n\s*isHistoryModalOpen\.value = false;\r?\n\s*selectedCustomerHistory\.value = null;\r?\n\s*\};/,
    "const closeHistoryModal = () => {\n    isHistoryModalOpen.value = false;\n    selectedCustomerHistory.value = null;\n    if (route.query.customerId) {\n      const q = { ...route.query };\n      delete q.customerId;\n      router.replace({ query: q });\n    }\n  };"
);

content = content.replace(
    /companyAttributes\.value = allAttrs\.filter\(a => a\.isActive\);\r?\n\r?\n\s*\} catch \(error\) \{/,
    "companyAttributes.value = allAttrs.filter(a => a.isActive);\n\n      if (route.query.customerId) {\n        const c = customers.value.find(x => x.id == route.query.customerId);\n        if (c) openHistoryModal(c);\n      }\n    } catch (error) {"
);

fs.writeFileSync('soluvion-web/src/views/CustomersView.vue', content, 'utf8');