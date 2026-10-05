const fs = require('fs');
let content = fs.readFileSync('soluvion-web/src/components/admin/dashboard/MaterialWrapUpModal.vue', 'utf8');

const injection = "    // Foglalás extra anyagainak betöltése\n    if (props.appointment.extraMaterials) {\n      try {\n        const extraItems = typeof props.appointment.extraMaterials === 'string' ? JSON.parse(props.appointment.extraMaterials) : props.appointment.extraMaterials;\n        extraItems.forEach(ei => {\n          const existing = productMap.get(ei.productId);\n          if (existing) {\n            existing.quantity += ei.quantity;\n          } else {\n            productMap.set(ei.productId, {\n              productId: ei.productId,\n              productName: ei.name || 'Ismeretlen termék',\n              quantity: ei.quantity,\n              costPrice: 0\n            });\n          }\n        });\n      } catch(e) { console.error('Hiba az extra anyagok betöltésekor', e); }\n    }\n\n";

content = content.replace(
      /if \(custRes && custRes\.data && custRes\.data\.attributes && custRes\.data\.attributes\.FormulaList\)/,
      injection + "if (custRes && custRes.data && custRes.data.attributes && custRes.data.attributes.FormulaList)"
);

fs.writeFileSync('soluvion-web/src/components/admin/dashboard/MaterialWrapUpModal.vue', content, 'utf8');