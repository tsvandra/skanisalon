const fs = require('fs');
let content = fs.readFileSync('soluvion-web/src/views/CustomersView.vue', 'utf8');

const targetHtml = <span v-for="(val, key) in customer.attributes" :key="key" class="text-[10px] bg-primary/10 text-primary px-2 py-0.5 rounded-md font-bold border border-primary/20" :title="key">\n              {{ getAttributeLabel(key) }}: {{ val }}\n            </span>;
const newHtml = <template v-for="(val, key) in customer.attributes" :key="key">\n              <span v-if="key !== 'FormulaList'" class="text-[10px] bg-primary/10 text-primary px-2 py-0.5 rounded-md font-bold border border-primary/20" :title="key">\n                {{ getAttributeLabel(key) }}: {{ val }}\n              </span>\n            </template>;

content = content.replace(targetHtml, newHtml);

fs.writeFileSync('soluvion-web/src/views/CustomersView.vue', content, 'utf8');