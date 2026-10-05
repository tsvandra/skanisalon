import re
with open('soluvion-web/src/components/admin/customers/CustomerHistoryModal.vue', 'r', encoding='utf-8') as f:
    content = f.read()

replacement = '''          <!-- Bal: Karton (Jegyzetek) -->
          <div class="flex flex-col h-full gap-4">
            
            <div class="flex flex-col flex-1">
              <div class="flex items-center justify-between mb-3">
                <h3 class="text-sm font-black text-text uppercase tracking-wider flex items-center gap-2">
                  <i class="pi pi-align-left text-primary"></i> Általános Jegyzetek
                </h3>
                <button @click="saveNotes" :disabled="savingNotes || !isNotesChanged" class="text-xs font-bold px-3 py-1.5 rounded-lg transition-colors" :class="isNotesChanged ? 'bg-primary text-white hover:brightness-110' : 'bg-surface-200 text-text-muted'">
                  <i class="pi" :class="savingNotes ? 'pi-spinner pi-spin' : 'pi-save'"></i> Mentés
                </button>
              </div>
              <textarea 
                v-model="notes" 
                class="w-full flex-1 min-h-[120px] bg-background border border-text/20 rounded-xl p-4 text-sm font-medium focus:outline-none focus:border-primary resize-none" 
                placeholder="Ide írhatod a vendég személyes preferenciáit, allergiákat stb...">
              </textarea>
            </div>

            <div class="flex flex-col flex-1">
              <div class="flex items-center justify-between mb-3">
                <h3 class="text-sm font-black text-text uppercase tracking-wider flex items-center gap-2">
                  <i class="pi pi-palette text-primary"></i> Használt Anyagok / Formula
                </h3>
              </div>
              <textarea 
                v-model="formula" 
                class="w-full flex-1 min-h-[100px] bg-background border border-text/20 rounded-xl p-4 text-sm font-medium focus:outline-none focus:border-primary resize-none" 
                placeholder="Hajfesték formulák (kódok, arányok)... (A jövoben ide automatikusan bekerülnek a napi zárás adatai is)">
              </textarea>
            </div>

          </div>'''

content = re.sub(r'<!-- Bal: Karton \(Jegyzetek\) -->.*?</div>\s*<!-- Jobb: Foglal', replacement + '\n\n          <!-- Jobb: Foglal', content, flags=re.DOTALL)

with open('soluvion-web/src/components/admin/customers/CustomerHistoryModal.vue', 'w', encoding='utf-8') as f:
    f.write(content)
