const fs = require('fs');
let content = fs.readFileSync('soluvion-web/src/components/admin/customers/CustomerHistoryModal.vue', 'utf8');

const template = <template>
  <div class="fixed inset-0 z-[100] flex items-center justify-center p-4 bg-background/80 backdrop-blur-sm" @mousedown.self="closeModal">
    <div class="bg-surface rounded-2xl shadow-2xl w-full max-w-4xl max-h-[90vh] flex flex-col overflow-hidden border border-text/10 animate-fade-in-up">
      
      <!-- Header -->
      <div class="p-5 border-b border-text/10 flex items-center justify-between bg-surface-50 shrink-0">
        <div>
          <h2 class="text-xl font-black text-text flex items-center gap-2">
            <i class="pi pi-book text-primary"></i> 
            {{ customerData?.name }} - Karton & Történet
          </h2>
          <p class="text-sm font-bold text-text-muted mt-1" v-if="customerData?.phone || customerData?.email">
            {{ customerData?.phone }} <span v-if="customerData?.phone && customerData?.email">|</span> {{ customerData?.email }}
          </p>
        </div>
        <button @click="closeModal" class="w-8 h-8 flex items-center justify-center rounded-full hover:bg-text/10 text-text-muted transition-colors">
          <i class="pi pi-times"></i>
        </button>
      </div>

      <!-- Body -->
      <div class="flex-1 overflow-y-auto p-5">
        <div class="grid grid-cols-1 md:grid-cols-2 gap-6 h-full">
          
          <!-- Bal: Karton (Jegyzetek) -->
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
              <textarea v-model="notes" class="w-full flex-1 min-h-[120px] bg-background border border-text/20 rounded-xl p-4 text-sm font-medium focus:outline-none focus:border-primary resize-none" placeholder="Ide írhatod a vendég személyes preferenciáit, allergiákat stb..."></textarea>
            </div>

            <div class="flex flex-col flex-1">
              <div class="flex items-center justify-between mb-3">
                <h3 class="text-sm font-black text-text uppercase tracking-wider flex items-center gap-2">
                  <i class="pi pi-palette text-primary"></i> Használt Anyagok / Formula
                </h3>
                <button @click="showAddProduct = true" class="text-xs font-bold text-primary hover:brightness-110 flex items-center gap-1">
                  <i class="pi pi-plus"></i> Új termék
                </button>
              </div>
              
              <div class="flex-1 bg-background border border-text/20 rounded-xl p-4 overflow-y-auto max-h-[250px] space-y-2">
                <div v-if="formulaItems.length === 0 && !showAddProduct" class="text-center text-text-muted italic text-sm mt-4">
                  Nincs még termék hozzáadva.
                </div>
                
                <div v-for="(item, index) in formulaItems" :key="index" class="flex items-center justify-between p-2 bg-surface border border-text/10 rounded-lg shadow-sm">
                  <div class="font-bold text-sm text-text">{{ item.productName }}</div>
                  <div class="flex items-center gap-3">
                    <span class="text-sm font-black text-primary">{{ item.quantity }} <span class="text-xs text-text-muted">{{ getUnit(item.productId) }}</span></span>
                    <button @click="removeProduct(index)" class="w-6 h-6 flex justify-center items-center rounded-full text-red-500 hover:bg-red-500/10 transition-colors">
                      <i class="pi pi-trash text-xs"></i>
                    </button>
                  </div>
                </div>

                <!-- Új termék dropdown -->
                <div v-if="showAddProduct" class="p-2 border border-primary/30 bg-primary/5 rounded-lg flex flex-col gap-2 mt-2">
                  <Dropdown v-model="selectedNewProduct" :options="allProducts" optionLabel="name" placeholder="Válassz terméket..." filter class="w-full bg-background border border-text/20 rounded-lg focus:outline-none focus:border-primary px-3 py-1.5 flex items-center h-[36px]" />
                  <div class="flex items-center gap-2 justify-between">
                    <div class="flex items-center gap-2">
                      <InputNumber v-model="newQuantity" :min="0" :maxFractionDigits="2" class="w-20 h-[36px]" inputClass="w-full h-full text-center text-sm" placeholder="Menny." />
                      <span class="text-xs font-bold text-text-muted w-6 text-center">{{ selectedNewProduct ? getUnit(selectedNewProduct.id) : '-' }}</span>
                    </div>
                    <div class="flex gap-1">
                      <button @click="showAddProduct = false" class="px-3 py-1.5 rounded text-xs font-bold text-text-muted hover:bg-text/10">Mégsem</button>
                      <button @click="addProduct" class="px-3 py-1.5 rounded text-xs font-bold bg-primary text-white hover:brightness-110">Hozzáadás</button>
                    </div>
                  </div>
                </div>
              </div>
            </div>

          </div>

          <!-- Jobb: Foglalások története -->
          <div class="flex flex-col h-full">
            <h3 class="text-sm font-black text-text uppercase tracking-wider mb-3 flex items-center gap-2">
              <i class="pi pi-history text-primary"></i> Foglalások története
            </h3>
            
            <div class="flex-1 bg-background border border-text/10 rounded-xl overflow-y-auto p-2">
              <div v-if="loadingHistory" class="flex justify-center items-center h-32">
                <i class="pi pi-spinner pi-spin text-primary text-2xl"></i>
              </div>
              <div v-else-if="appointments.length === 0" class="flex flex-col items-center justify-center h-48 text-center px-4">
                <i class="pi pi-calendar-times text-text-muted text-4xl mb-3 opacity-50"></i>
                <p class="text-text-muted font-bold">Még nem volt egyetlen foglalása sem.</p>
              </div>
              <div v-else class="flex flex-col gap-2">
                <!-- Appointment kártya -->
                <div v-for="app in appointments" :key="app.id" @click="goToCalendar(app.startDateTime)" class="p-3 bg-surface border border-text/5 rounded-lg hover:border-primary hover:shadow-sm cursor-pointer transition-all">
                  <div class="flex justify-between items-start mb-2">
                    <div>
                      <span class="text-xs font-bold text-primary bg-primary/10 px-2 py-0.5 rounded-md">{{ formatDate(app.startDateTime) }}</span>
                      <div class="text-xs font-bold text-text-muted mt-1"><i class="pi pi-user text-[10px]"></i> {{ app.employeeName }}</div>
                    </div>
                    <div class="text-sm font-black text-text">
                      {{ formatPrice(app.totalPrice) }}
                    </div>
                  </div>
                  
                  <div class="flex flex-col gap-1 mt-2">
                    <div v-for="(item, idx) in app.items" :key="idx" class="text-sm font-bold text-text flex items-center gap-1.5">
                      <div class="w-1.5 h-1.5 rounded-full bg-primary/50"></div>
                      {{ item.serviceName }}
                    </div>
                  </div>

                  <div v-if="app.customerNotes" class="mt-2 text-xs text-text-muted italic border-l-2 border-text/20 pl-2">
                    "{{ app.customerNotes }}"
                  </div>
                </div>
              </div>
            </div>
          </div>

        </div>
      </div>

    </div>
  </div>
</template>;

content = content.replace(/<template>[\s\S]*?<\/template>/, template);
fs.writeFileSync('soluvion-web/src/components/admin/customers/CustomerHistoryModal.vue', content, 'utf8');
console.log("CustomerHistoryModal template fixed");