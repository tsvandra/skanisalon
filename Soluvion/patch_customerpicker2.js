const fs = require('fs');
let content = fs.readFileSync('soluvion-web/src/components/admin/calendar/CustomerPicker.vue', 'utf8');
const targetRegex = /<label class="block text-\[10px\] md:text-xs font-bold text-text-muted mb-1\.5 uppercase flex items-center gap-1">[\s\S]*?<\/label>/;
const replacement = <label class="block text-[10px] md:text-xs font-bold text-text-muted mb-1.5 uppercase flex items-center justify-between">\n      <div class="flex items-center gap-1"><i class="pi pi-user"></i> {{ \('calendar.editor.client') }}</div>\n      <button v-if="modelValue && modelValue !== 'new'" @click.stop="goToCustomer" class="text-primary hover:text-primary/70 transition-colors flex items-center gap-1" title="Ugrás az ügyfél kartonjára">\n        <i class="pi pi-external-link"></i> Karton\n      </button>\n    </label>;

content = content.replace(targetRegex, replacement);
fs.writeFileSync('soluvion-web/src/components/admin/calendar/CustomerPicker.vue', content, 'utf8');