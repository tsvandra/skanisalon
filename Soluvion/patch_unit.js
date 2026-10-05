const fs = require('fs');
let content = fs.readFileSync('soluvion-web/src/components/admin/customers/CustomerHistoryModal.vue', 'utf8');

const target1 = "const unitMap = { 0: 'db', 1: 'ml', 2: 'g', 3: 'm', 4: 'cm' };\nconst getUnit = (productId) => {\n  const p = allProducts.value.find(x => x.id === productId);\n  return p ? unitMap[p.unitOfMeasure] || '-' : '-';\n};";
const target2 = "const unitMap = { 0: 'db', 1: 'ml', 2: 'g', 3: 'm', 4: 'cm' };\r\nconst getUnit = (productId) => {\r\n  const p = allProducts.value.find(x => x.id === productId);\r\n  return p ? unitMap[p.unitOfMeasure] || '-' : '-';\r\n};";
const replacement = "const unitMap = { 0: 'ml', 1: 'g', 2: 'db', 3: 'm', 4: 'cm' };\nconst getUnit = (productId) => {\n  const p = allProducts.value.find(x => x.id === productId);\n  return p ? unitMap[p.unit] || '-' : '-';\n};";

if (content.includes(target1)) content = content.replace(target1, replacement);
else if (content.includes(target2)) content = content.replace(target2, replacement);
else {
  // Regex fallback
  content = content.replace(/const unitMap = \{.*?\};\r?\nconst getUnit = \(productId\) => \{\r?\n.*?const p = allProducts\.value\.find\(x => x\.id === productId\);\r?\n.*?return p \? unitMap\[p\.unitOfMeasure\] \|\| '-' : '-';\r?\n\};/, replacement);
}

fs.writeFileSync('soluvion-web/src/components/admin/customers/CustomerHistoryModal.vue', content, 'utf8');