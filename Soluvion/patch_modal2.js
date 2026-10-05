const fs = require('fs');
let content = fs.readFileSync('soluvion-web/src/components/admin/dashboard/LowStockPlannerModal.vue', 'utf8');

if (!content.includes('useRouter')) {
    content = content.replace(
        "import { ref, watch } from 'vue';",
        "import { ref, watch } from 'vue';\nimport { useRouter } from 'vue-router';"
    );
}

if (!content.includes('goToCalendar')) {
    content = content.replace(
        "const emit = defineEmits(['close']);",
        "const emit = defineEmits(['close']);\nconst router = useRouter();\n\nconst goToCalendar = (dateStr) => {\n  emit('close');\n  router.push({ path: '/megrendelesek', query: { date: dateStr } });\n};"
    );
}

if (!content.includes('goToCalendar(app.startDateTime)')) {
    content = content.replace(
        /class="p-3 bg-background border border-text\/10 rounded-xl flex flex-col sm:flex-row justify-between items-start sm:items-center gap-2 hover:border-primary\/50 transition-colors"/,
        '@click="goToCalendar(app.startDateTime)" class="p-3 bg-background border border-text/10 rounded-xl flex flex-col sm:flex-row justify-between items-start sm:items-center gap-2 hover:border-orange-500/50 cursor-pointer transition-colors"'
    );
}

fs.writeFileSync('soluvion-web/src/components/admin/dashboard/LowStockPlannerModal.vue', content, 'utf8');