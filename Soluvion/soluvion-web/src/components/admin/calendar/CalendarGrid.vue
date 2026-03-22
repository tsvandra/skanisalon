<template>
  <div class="flex flex-col h-full bg-background text-text">

    <div class="sticky top-0 z-20 bg-surface border-b border-text/10 p-3 md:p-4 shadow-sm">
      <div class="max-w-7xl mx-auto w-full flex flex-col xl:flex-row justify-between items-start xl:items-center gap-2 md:gap-4">

        <div class="flex items-baseline gap-2 md:gap-4 w-full xl:w-auto">
          <h2 class="text-xl md:text-2xl font-bold text-text drop-shadow-sm">{{ $t('calendar.dashboardTitle') || 'Vezérlőpult' }}</h2>
          <span class="text-xs md:text-sm text-text-muted">{{ $t('calendar.role') || 'Szerepkör' }}: <strong class="text-primary">{{ store.userRole }}</strong></span>
        </div>

        <div class="flex flex-col md:flex-row items-center gap-2 w-full xl:w-auto mt-1 md:mt-0">

          <div class="flex flex-row w-full md:w-auto gap-2">
            <button @click="goToday" class="flex-1 md:flex-none px-3 md:px-5 min-h-[44px] font-bold text-sm bg-background border border-text/10 hover:border-primary/50 transition-colors rounded-xl text-text shadow-sm flex items-center justify-center gap-1.5">
              <i class="pi pi-calendar-times text-primary"></i> <span>{{ $t('calendar.today') || 'Ma' }}</span>
            </button>

            <div class="flex flex-1 md:flex-none bg-background rounded-xl p-1 shadow-inner border border-text/5">
              <button v-for="view in views" :key="view.id"
                      @click="currentView = view.id"
                      class="flex-1 min-h-[36px] md:min-h-[44px] px-2 md:px-4 rounded-lg font-bold text-xs md:text-sm flex items-center justify-center gap-1 md:gap-2 transition-all"
                      :class="currentView === view.id ? 'bg-surface text-primary shadow-sm ring-1 ring-text/10' : 'text-text-muted hover:text-text hover:bg-text/5'">
                <i :class="view.icon"></i>
                <span class="hidden sm:inline">{{ $t(`calendar.${view.id}`) || view.label }}</span>
              </button>
            </div>
          </div>

          <div class="flex items-center bg-background rounded-xl p-1 shadow-inner border border-text/5 w-full md:w-auto justify-between md:justify-center">
            <button @click="goPrev" class="min-w-[44px] min-h-[36px] md:min-h-[44px] flex items-center justify-center rounded-lg hover:bg-text/5 transition-colors text-text">
              <i class="pi pi-chevron-left font-bold text-base md:text-lg"></i>
            </button>

            <div class="px-2 md:px-4 font-black text-text uppercase tracking-wider text-xs md:text-base text-center w-full md:min-w-[220px] truncate">
              {{ pagerText }}
            </div>

            <button @click="goNext" class="min-w-[44px] min-h-[36px] md:min-h-[44px] flex items-center justify-center rounded-lg hover:bg-text/5 transition-colors text-text">
              <i class="pi pi-chevron-right font-bold text-base md:text-lg"></i>
            </button>
          </div>

        </div>
      </div>
    </div>

    <div class="flex-1 overflow-y-auto p-2 md:p-6 w-full">
      <div class="max-w-7xl mx-auto w-full">

        <div v-if="currentView === 'month'" class="space-y-6 md:space-y-8">

          <div class="max-w-5xl mx-auto bg-surface p-2 md:p-5 rounded-2xl border border-text/10 shadow-sm">
            <div class="grid grid-cols-7 gap-1 md:gap-2">
              <div v-for="day in dynamicWeekDays" :key="day" class="text-center text-[10px] md:text-xs font-bold text-text-muted uppercase mb-1 tracking-wider">
                {{ day }}
              </div>

              <div v-for="(dayObj, index) in calendarDays" :key="index"
                   @click="onDayClick(dayObj)"
                   class="relative min-h-[50px] md:min-h-[80px] p-1 md:p-2 rounded-lg transition-all cursor-pointer flex flex-col items-center justify-between border"
                   :class="[
                     dayObj.isCurrentMonth ? 'bg-white border-gray-300 shadow-sm hover:border-primary/60' : 'bg-gray-100 border-transparent opacity-50',
                     dayObj.isToday ? 'ring-2 ring-primary ring-offset-1 md:ring-offset-2 ring-offset-surface' : ''
                   ]">

                <div v-if="dayObj.isCurrentMonth" class="absolute top-0 left-0 right-0 h-1 bg-gray-200 rounded-t-lg overflow-hidden">
                  <div class="h-full bg-primary transition-all" :style="{ width: `${dayObj.loadPercentage}%` }"></div>
                </div>

                <span class="font-black text-xs md:text-base mt-1" :class="dayObj.isToday ? 'text-primary drop-shadow-sm' : 'text-gray-800'">
                  {{ dayObj.date.getDate() }}
                </span>

                <div class="flex gap-0.5 mt-auto pb-0.5 flex-wrap justify-center items-center">
                  <span v-for="n in Math.min(dayObj.appointmentCount, 4)" :key="n"
                        class="w-1.5 h-1.5 rounded-full shadow-sm"
                        :class="dayObj.hasPending ? 'bg-red-500' : 'bg-green-500'"></span>
                  <span v-if="dayObj.appointmentCount > 4" class="text-[8px] md:text-[9px] text-gray-600 font-black ml-0.5">+</span>
                </div>
              </div>
            </div>
          </div>

          <div class="max-w-5xl mx-auto mt-6 md:mt-8">
            <h3 class="font-bold text-lg md:text-xl text-text border-b border-text/10 pb-2 mb-3 md:mb-4 flex items-center gap-2">
              <i class="pi pi-forward text-primary"></i> {{ $t('calendar.upcomingAppointments') || 'Következő várható foglalások' }}
            </h3>

            <div v-if="upcomingAppointments.length > 0" class="grid grid-cols-1 md:grid-cols-2 gap-3 md:gap-4">
              <div v-for="app in upcomingAppointments" :key="'upc-'+app.id"
                   @click="openAppointmentDetails(app)"
                   class="bg-surface rounded-xl p-3 md:p-4 shadow-sm border border-text/10 flex flex-col gap-2 md:gap-3 hover:border-primary/50 cursor-pointer transition-colors relative"
                   :style="{ borderLeftWidth: '5px', borderLeftColor: getCustomerColorDarker(app.customerId) }">

                <div class="flex justify-between items-start">
                  <div class="flex items-center gap-2 md:gap-3">
                    <div class="flex items-center justify-center w-7 h-7 md:w-9 md:h-9 rounded-full font-bold text-gray-800 text-[10px] md:text-xs border border-text/10 shadow-sm"
                         :style="{ backgroundColor: getCustomerColor(app.customerId) }">
                      {{ getCustomerInitials(app.customerId) }}
                    </div>
                    <div class="flex flex-col">
                      <div class="flex items-center gap-2">
                        <h4 class="text-sm md:text-md font-bold text-text">{{ getCustomerName(app.customerId) }}</h4>
                        <div class="w-2 h-2 rounded-full shadow-sm" :class="app.status === 0 ? 'bg-red-500' : 'bg-green-500'" :title="app.status === 0 ? 'Függőben' : 'Jóváhagyva'"></div>
                      </div>
                    </div>
                  </div>
                  <div class="text-[10px] md:text-xs bg-background px-2 py-1 rounded text-text-muted font-bold capitalize">
                    {{ getDayNameShort(new Date(app.startDateTime)) }}, {{ formatDateShort(app.startDateTime) }}
                  </div>
                </div>

                <div class="flex items-center gap-3 md:gap-4 text-text-muted text-[10px] md:text-xs font-bold pl-1">
                  <div class="flex items-center gap-1"><i class="pi pi-clock text-primary"></i> {{ formatTime(app.startDateTime) }}</div>
                  <div class="flex items-center gap-1"><i class="pi pi-hourglass text-primary"></i> {{ getDurationMinutes(app) }} p</div>
                </div>

                <div class="flex gap-1 md:gap-1.5 mt-1 pl-1">
                  <div v-for="item in app.items" :key="item.id"
                       class="w-6 h-6 md:w-8 md:h-8 rounded-full bg-primary/10 text-primary flex items-center justify-center text-[10px] md:text-xs font-black border border-primary/20 cursor-help"
                       :title="getVariantFullName(item.serviceVariantId)">
                    {{ getInitials(getVariantFullName(item.serviceVariantId)) }}
                  </div>
                </div>
              </div>
            </div>
            <div v-else class="text-center py-6 md:py-8 bg-surface rounded-xl border border-dashed border-text/10 text-text-muted text-sm md:text-base">
              {{ $t('calendar.noUpcomingAppointments') || 'Nincsenek közelgő foglalások.' }}
            </div>
          </div>
        </div>

        <div v-else-if="currentView === 'week'" class="space-y-6 md:space-y-8">
          <div class="bg-surface p-2 md:p-5 rounded-2xl border border-text/10 shadow-sm">
            <div class="grid grid-cols-7 gap-1 md:gap-2">
              <div v-for="dayObj in currentWeekDays" :key="dayObj.date.toISOString()"
                   @click="onDayClick(dayObj)"
                   class="relative min-h-[70px] md:min-h-[100px] p-1.5 md:p-2 rounded-xl border border-gray-300 bg-white hover:border-primary/50 cursor-pointer flex flex-col items-center justify-center transition-all shadow-sm"
                   :class="{ 'ring-2 ring-primary ring-offset-1 md:ring-offset-2 ring-offset-surface': dayObj.isToday }">
                <div class="absolute top-0 left-0 right-0 h-1 bg-gray-200 rounded-t-xl overflow-hidden">
                  <div class="h-full bg-primary" :style="{ width: `${dayObj.loadPercentage}%` }"></div>
                </div>
                <span class="text-[10px] md:text-xs text-gray-500 font-bold mt-1 uppercase">{{ getDayNameShort(dayObj.date) }}</span>
                <span class="font-black text-lg md:text-2xl" :class="dayObj.isToday ? 'text-primary' : 'text-gray-800'">{{ dayObj.date.getDate() }}</span>
                <div class="flex gap-0.5 md:gap-1 mt-1 md:mt-2">
                  <span v-for="n in Math.min(dayObj.appointmentCount, 3)" :key="n" class="w-1.5 h-1.5 rounded-full bg-primary shadow-sm"></span>
                </div>
              </div>
            </div>
          </div>

          <div class="max-w-5xl mx-auto mt-6 md:mt-8">
            <h3 class="font-bold text-lg md:text-xl text-text border-b border-text/10 pb-2 mb-3 md:mb-4 flex items-center gap-2">
              <i class="pi pi-calendar-clock text-primary"></i> {{ $t('calendar.remainingWeekAppointments') || 'A hét hátralévő foglalásai' }}
            </h3>

            <div v-if="currentWeekUpcomingAppointments.length > 0" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3 md:gap-4">
              <div v-for="app in currentWeekUpcomingAppointments" :key="'week-'+app.id"
                   @click="openAppointmentDetails(app)"
                   class="bg-surface rounded-xl p-3 md:p-4 shadow-sm border border-text/10 flex flex-col gap-2 md:gap-3 hover:border-primary/50 cursor-pointer transition-colors relative"
                   :style="{ borderLeftWidth: '5px', borderLeftColor: getCustomerColorDarker(app.customerId) }">

                <div class="flex justify-between items-start">
                  <div class="flex items-center gap-2 md:gap-3">
                    <div class="flex items-center justify-center w-7 h-7 md:w-9 md:h-9 rounded-full font-bold text-gray-800 text-[10px] md:text-xs border border-text/10 shadow-sm"
                         :style="{ backgroundColor: getCustomerColor(app.customerId) }">
                      {{ getCustomerInitials(app.customerId) }}
                    </div>
                    <div class="flex flex-col">
                      <div class="flex items-center gap-1.5">
                        <h4 class="text-sm md:text-md font-bold text-text">{{ getCustomerName(app.customerId) }}</h4>
                        <div class="w-2 h-2 rounded-full shadow-sm" :class="app.status === 0 ? 'bg-red-500' : 'bg-green-500'" :title="app.status === 0 ? 'Függőben' : 'Jóváhagyva'"></div>
                      </div>
                    </div>
                  </div>
                  <div class="text-[10px] md:text-xs bg-background px-2 py-1 rounded text-text-muted font-bold capitalize">
                    {{ getDayNameShort(new Date(app.startDateTime)) }}
                  </div>
                </div>

                <div class="flex items-center gap-3 md:gap-4 text-text-muted text-[10px] md:text-xs font-bold pl-1">
                  <div class="flex items-center gap-1"><i class="pi pi-clock text-primary"></i> {{ formatTime(app.startDateTime) }}</div>
                  <div class="flex items-center gap-1"><i class="pi pi-hourglass text-primary"></i> {{ getDurationMinutes(app) }} p</div>
                </div>

                <div class="flex gap-1 md:gap-1.5 mt-1 pl-1">
                  <div v-for="item in app.items" :key="item.id"
                       class="w-6 h-6 md:w-8 md:h-8 rounded-full bg-primary/10 text-primary flex items-center justify-center text-[10px] md:text-xs font-black border border-primary/20 cursor-help"
                       :title="getVariantFullName(item.serviceVariantId)">
                    {{ getInitials(getVariantFullName(item.serviceVariantId)) }}
                  </div>
                </div>
              </div>
            </div>

            <div v-else class="text-center py-6 md:py-8 bg-surface rounded-xl border border-dashed border-text/10 text-text-muted text-sm md:text-base">
              {{ $t('calendar.noRemainingWeekAppointments') || 'Nincsenek hátralévő foglalások erre a hétre.' }}
            </div>
          </div>
        </div>

        <div v-else-if="currentView === 'day'" class="space-y-4 md:space-y-8 bg-surface p-3 md:p-8 rounded-2xl md:rounded-3xl border border-text/10 shadow-sm">

          <div class="flex flex-row items-baseline gap-2 md:gap-3 border-b border-text/10 pb-2 md:pb-4 flex-wrap">
            <h3 class="text-xl md:text-2xl font-black text-primary capitalize">{{ getDayNameLong(currentDate) }}</h3>
            <p class="text-sm md:text-base text-text font-medium">{{ formatDateLong(currentDate) }}</p>
          </div>

          <div class="relative w-full mt-6 md:mt-10 mb-8 md:mb-12 select-none">

            <div v-for="(gap, index) in timelineData.gaps" :key="'gap-'+index"
                 class="absolute -top-6 md:-top-7 text-[10px] md:text-sm font-bold text-gray-800 flex justify-center items-center bg-white px-1.5 md:px-3 py-0.5 md:py-1 rounded border border-gray-300 shadow-sm whitespace-nowrap"
                 :style="{ left: gap.left + '%', width: gap.width + '%' }">
              {{ formatDurationStr(gap.durationMinutes) }}
            </div>

            <div class="w-full h-8 md:h-12 bg-white rounded-lg md:rounded-xl shadow-inner relative border border-gray-300 overflow-hidden">
              <div v-for="app in timelineData.appointments" :key="'tl-'+app.id"
                   @click="openAppointmentDetails(app)"
                   class="absolute top-0 bottom-0 h-full border-x border-white/50 cursor-pointer transition-transform hover:scale-y-105 flex items-center justify-center overflow-hidden shadow-sm"
                   :style="{ left: app.left + '%', width: app.width + '%', backgroundColor: getCustomerColor(app.customerId) }"
                   :title="`${getCustomerName(app.customerId)}: ${formatTime(app.startDateTime)} - ${formatTime(app.endDateTime)}`">

                <div class="absolute top-1 right-1 w-2 h-2 rounded-full shadow-sm border border-white/50" :class="app.status === 0 ? 'bg-red-500' : 'bg-green-500'"></div>

                <span v-if="app.width >= 3" class="text-gray-800 text-[10px] md:text-xs font-bold truncate px-1 drop-shadow-sm">{{ getCustomerInitials(app.customerId) }}</span>
              </div>
            </div>

            <div v-for="hour in timelineData.hours" :key="'hr-'+hour.val"
                 class="absolute top-9 md:top-14 text-[10px] md:text-sm font-bold text-text-muted -translate-x-1/2 flex flex-col items-center"
                 :style="{ left: hour.left + '%' }">
              <div class="w-0.5 h-1.5 md:h-2 bg-text/20 mb-0.5 md:mb-1 rounded-full"></div>
              {{ hour.val }}
            </div>
          </div>

          <div class="mt-8 md:mt-16 space-y-3 md:space-y-4">
            <h3 class="font-bold text-lg md:text-xl text-text border-b border-text/10 pb-2 flex items-center gap-2">
              <i class="pi pi-list"></i> {{ $t('calendar.dailyAppointmentsDetails') || 'Napi Foglalások Részletei' }}
            </h3>

            <div v-for="app in currentDayAppointments" :key="'card-'+app.id"
                 @click="openAppointmentDetails(app)"
                 class="bg-background rounded-xl md:rounded-2xl p-3 md:p-6 shadow-sm border border-text/10 flex flex-col gap-3 md:gap-4 hover:border-primary/30 transition-colors cursor-pointer relative"
                 :style="{ borderLeftWidth: '6px', borderLeftColor: getCustomerColorDarker(app.customerId) }">

              <div class="flex justify-between items-start">
                <div class="flex items-center gap-2 md:gap-3">
                  <div class="flex items-center justify-center w-8 h-8 md:w-12 md:h-12 rounded-full font-bold text-gray-800 text-xs md:text-lg border border-text/10 shadow-sm"
                       :style="{ backgroundColor: getCustomerColor(app.customerId) }">
                    {{ getCustomerInitials(app.customerId) }}
                  </div>
                  <div class="flex flex-col">
                    <div class="flex items-center gap-2">
                      <h4 class="text-base md:text-xl font-bold text-text">{{ getCustomerName(app.customerId) }}</h4>
                      <div class="w-2.5 h-2.5 md:w-3 md:h-3 rounded-full shadow-sm" :class="app.status === 0 ? 'bg-red-500' : 'bg-green-500'" :title="app.status === 0 ? 'Függőben' : 'Jóváhagyva'"></div>
                    </div>
                  </div>
                </div>
                <div class="flex items-center gap-2 md:gap-3 text-text-muted text-[10px] md:text-sm font-bold bg-surface px-2 md:px-3 py-1 md:py-1.5 rounded-lg border border-text/5">
                  <div class="flex items-center gap-1"><i class="pi pi-clock text-primary"></i> {{ formatTime(app.startDateTime) }}</div>
                  <span class="text-text/20">|</span>
                  <div class="flex items-center gap-1"><i class="pi pi-hourglass text-primary"></i> {{ getDurationMinutes(app) }} p</div>
                </div>
              </div>

              <div class="flex flex-wrap gap-1.5 md:gap-2 mt-1 md:mt-2 pl-2">
                <div v-for="item in app.items" :key="item.id"
                     class="w-8 h-8 md:w-12 md:h-12 rounded-full bg-primary/10 text-primary flex items-center justify-center text-xs md:text-base font-black border border-primary/20 cursor-help hover:bg-primary hover:text-white transition-colors"
                     :title="getVariantFullName(item.serviceVariantId)">
                  {{ getInitials(getVariantFullName(item.serviceVariantId)) }}
                </div>
              </div>
            </div>

            <div v-if="currentDayAppointments.length === 0" class="text-center py-8 md:py-12 bg-background rounded-xl md:rounded-2xl border border-dashed border-text/10 text-text-muted">
              <p class="font-medium text-sm md:text-base">{{ $t('calendar.noAppointmentsForDay') || 'Nincs foglalás erre a napra.' }}</p>
            </div>

            <button @click="openNewAppointment" class="w-full min-h-[44px] md:min-h-[56px] mt-4 md:mt-6 flex items-center justify-center gap-2 bg-primary text-white rounded-lg md:rounded-xl font-bold text-sm md:text-lg hover:brightness-110 transition-all active:scale-95 shadow-md">
              <i class="pi pi-plus"></i> {{ $t('calendar.addNewAppointment') || 'Új foglalás hozzáadása' }}
            </button>
          </div>
        </div>

      </div>
    </div>

    <div v-if="isModalOpen" class="fixed inset-0 bg-black/60 backdrop-blur-sm z-50 flex items-center justify-center p-2 md:p-4">
      <div class="bg-surface w-full max-w-2xl rounded-2xl shadow-2xl overflow-hidden border border-text/10 flex flex-col max-h-[95vh] md:max-h-[90vh]">

        <div class="p-4 md:p-6 border-b border-text/10 flex justify-between items-center bg-background/50">
          <h2 class="text-lg md:text-xl font-bold text-text flex items-center gap-2">
            <i class="pi" :class="isEditing ? 'pi-pencil text-primary' : 'pi-plus-circle text-primary'"></i>
            {{ isEditing ? 'Foglalás szerkesztése' : 'Új foglalás rögzítése' }}
          </h2>
          <button @click="closeModal" class="w-8 h-8 flex items-center justify-center rounded-full hover:bg-text/10 text-text-muted transition-colors">
            <i class="pi pi-times"></i>
          </button>
        </div>

        <div class="p-4 md:p-6 overflow-y-auto space-y-6">

          <div>
            <label class="block text-[10px] md:text-xs font-bold text-text-muted mb-1.5 uppercase flex items-center gap-1"><i class="pi pi-user"></i> 1. Ügyfél</label>
            <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div class="relative">
                <select v-model="form.customerId" @change="handleCustomerSelect" class="w-full h-[44px] bg-background border border-text/20 rounded-lg px-3 text-sm text-text font-bold focus:outline-none focus:border-primary appearance-none cursor-pointer">
                  <option value="" disabled>Válassz a listából...</option>
                  <option value="new" class="text-primary font-bold">+ Új ügyfél rögzítése</option>
                  <option v-for="c in customersList" :key="c.id" :value="c.id">{{ c.name }}</option>
                </select>
                <i class="pi pi-chevron-down absolute right-3 top-1/2 -translate-y-1/2 text-text-muted pointer-events-none text-sm"></i>
              </div>

              <div v-if="form.customerId === 'new'" class="flex flex-col gap-2">
                <input type="text" v-model="form.customerFullName" placeholder="Teljes név (opcionális)..." class="w-full h-[44px] bg-background border border-text/20 rounded-lg px-3 text-sm text-text focus:outline-none focus:border-primary">
                <input type="tel" v-model="form.customerPhone" placeholder="Telefonszám (opcionális)..." class="w-full h-[44px] bg-background border border-text/20 rounded-lg px-3 text-sm text-text focus:outline-none focus:border-primary">
                <span class="text-[10px] text-text-muted leading-tight">Legalább az egyik mező kitöltése kötelező!</span>
              </div>
              <div v-else-if="form.customerId">
                <input type="text" v-model="form.customerFullName" disabled class="w-full h-[44px] bg-background border border-text/20 rounded-lg px-3 text-sm text-text focus:outline-none focus:border-primary opacity-50 cursor-not-allowed">
              </div>
            </div>
          </div>

          <div class="grid grid-cols-2 gap-3 border-t border-text/10 pt-4">
            <div>
              <label class="block text-[10px] md:text-xs font-bold text-text-muted mb-1.5 uppercase flex items-center gap-1"><i class="pi pi-calendar"></i> Dátum</label>
              <input type="date" v-model="form.date" class="w-full h-[44px] bg-background border border-text/20 rounded-lg px-3 text-sm text-text font-bold focus:outline-none focus:border-primary">
            </div>
            <div>
              <label class="block text-[10px] md:text-xs font-bold text-text-muted mb-1.5 uppercase flex items-center gap-1"><i class="pi pi-clock"></i> Kezdés</label>
              <input type="time" v-model="form.time" class="w-full h-[44px] bg-background border border-text/20 rounded-lg px-3 text-sm text-text font-bold focus:outline-none focus:border-primary">
            </div>
          </div>

          <div class="border-t border-text/10 pt-4">
            <label class="block text-[10px] md:text-xs font-bold text-text-muted mb-2 uppercase flex items-center gap-1"><i class="pi pi-sparkles"></i> Szolgáltatások</label>

            <div class="bg-primary/5 p-3 md:p-4 rounded-xl border border-primary/20 space-y-3 mb-4">
              <h4 class="text-sm font-bold text-primary flex items-center gap-2"><i class="pi pi-plus-circle"></i> Tétel hozzáadása</h4>

              <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
                <div class="relative">
                  <select v-model="builder.category" @change="resetBuilder(1)" class="w-full h-[44px] bg-white dark:bg-black border border-text/10 rounded-lg px-3 text-text focus:outline-none focus:border-primary appearance-none text-xs md:text-sm">
                    <option value="" disabled>1. Kategória...</option>
                    <option v-for="cat in availableCategories" :key="cat" :value="cat">{{ cat }}</option>
                  </select>
                  <i class="pi pi-chevron-down absolute right-3 top-1/2 -translate-y-1/2 text-text-muted text-xs pointer-events-none"></i>
                </div>

                <div class="relative">
                  <select v-model="builder.serviceId" @change="resetBuilder(2)" :disabled="!builder.category" class="w-full h-[44px] bg-white dark:bg-black border border-text/10 rounded-lg px-3 text-text focus:outline-none focus:border-primary appearance-none text-xs md:text-sm disabled:opacity-50">
                    <option value="" disabled>2. Szolgáltatás...</option>
                    <option v-for="srv in availableServicesInCategory" :key="srv.id" :value="srv.id">{{ getLocText(srv.name) }}</option>
                  </select>
                  <i class="pi pi-chevron-down absolute right-3 top-1/2 -translate-y-1/2 text-text-muted text-xs pointer-events-none"></i>
                </div>
              </div>

              <div v-if="builder.serviceId" class="relative">
                <select v-model="builder.variantId" @change="onVariantSelected" class="w-full h-[44px] bg-white dark:bg-black border border-primary/30 rounded-lg px-3 text-text font-bold focus:outline-none focus:border-primary appearance-none text-xs md:text-sm">
                  <option value="" disabled>3. Pontos Variáns kiválasztása...</option>
                  <option v-for="v in availableVariantsInService" :key="v.id" :value="v.id">{{ getLocText(v.variantName) }} ({{ v.price }} EUR)</option>
                </select>
                <i class="pi pi-chevron-down absolute right-3 top-1/2 -translate-y-1/2 text-primary text-xs pointer-events-none"></i>
              </div>

              <div v-if="builder.variantId" class="flex flex-col md:flex-row md:items-end gap-3 pt-2">
                <div class="w-full md:w-1/3 flex flex-col">
                  <label class="text-[10px] font-bold text-text-muted uppercase mb-1">Időtartam (Húzd)</label>
                  <ScrubbableInput v-model="builder.duration" :min="5" :max="480" :step="5" :sensitivity="10" suffix="perc" class="h-[44px]" />
                </div>

                <div class="w-full md:w-2/3">
                  <button @click="addServiceToForm" class="w-full h-[44px] bg-primary text-white font-bold rounded-lg shadow-sm hover:brightness-110 active:scale-95 transition-all flex items-center justify-center gap-2">
                    <i class="pi pi-check"></i> Listához ad
                  </button>
                </div>
              </div>
            </div>

            <div v-if="form.items.length > 0" class="space-y-2 mt-2">
              <div v-for="(item, idx) in form.items" :key="idx" class="flex items-center justify-between bg-surface border border-text/10 p-2 md:p-3 rounded-lg shadow-sm">
                <div class="flex flex-col">
                  <span class="font-bold text-xs md:text-sm text-text">{{ item.name }}</span>
                  <span class="text-[10px] md:text-xs text-text-muted font-medium">{{ item.duration }} perc • {{ item.price }} EUR</span>
                </div>
                <button @click="removeFormItem(idx)" class="w-8 h-8 flex items-center justify-center bg-red-500/10 text-red-500 rounded-full hover:bg-red-500 hover:text-white transition-colors">
                  <i class="pi pi-trash text-xs"></i>
                </button>
              </div>
            </div>
            <div v-else class="text-[10px] md:text-xs text-center py-2 text-text-muted border border-dashed border-text/10 rounded-lg">
              Még nincs szolgáltatás hozzáadva.
            </div>

          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4 border-t border-text/10 pt-4">
            <div>
              <label class="block text-[10px] md:text-xs font-bold text-text-muted mb-2 uppercase">Állapot</label>
              <div class="flex gap-2">
                <button @click="form.status = 0" class="flex-1 h-[44px] rounded-lg font-bold text-xs md:text-sm border transition-all" :class="form.status === 0 ? 'bg-red-500/10 border-red-500 text-red-500 shadow-sm' : 'border-text/20 text-text hover:bg-text/5'">Függőben</button>
                <button @click="form.status = 1" class="flex-1 h-[44px] rounded-lg font-bold text-xs md:text-sm border transition-all" :class="form.status === 1 ? 'bg-green-500/10 border-green-500 text-green-500 shadow-sm' : 'border-text/20 text-text hover:bg-text/5'">Jóváhagyva</button>
              </div>
            </div>
            <div>
              <label class="block text-[10px] md:text-xs font-bold text-text-muted mb-1.5 uppercase">Belső Megjegyzés</label>
              <textarea v-model="form.notes" rows="2" placeholder="Opcionális feljegyzés..." class="w-full bg-background border border-text/20 rounded-lg p-2.5 text-xs md:text-sm text-text focus:outline-none focus:border-primary resize-none"></textarea>
            </div>
          </div>

        </div>

        <div class="p-3 md:p-4 border-t border-text/10 bg-background/50 flex justify-between gap-2 md:gap-3 mt-auto">
          <button v-if="isEditing" @click="handleDelete" class="px-3 md:px-4 h-[44px] text-red-500 font-bold text-sm md:text-base rounded-lg border border-red-500/30 hover:bg-red-500/10 transition-colors">
            Törlés
          </button>
          <div v-else></div>

          <div class="flex gap-2">
            <button @click="closeModal" class="px-3 md:px-4 h-[44px] text-text text-sm md:text-base font-bold rounded-lg hover:bg-text/10 transition-colors">
              Mégsem
            </button>
            <button @click="handleSave" :disabled="!isFormValid" class="px-4 md:px-6 h-[44px] bg-primary text-white text-sm md:text-base font-bold rounded-lg hover:brightness-110 shadow-md transition-transform active:scale-95 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-1 md:gap-2">
              <i class="pi pi-save"></i> Mentés
            </button>
          </div>
        </div>
      </div>
    </div>

  </div>
</template>

<script setup>
  import { ref, computed, onMounted, watch } from 'vue';
  import { useI18n } from 'vue-i18n';
  import { useAppointmentStore } from '@/stores/appointmentStore';
  import bookingApi from '@/services/bookingApi';
  import ScrubbableInput from '@/components/common/ScrubbableInput.vue';
  import { getCustomerColor, getCustomerColorDarker } from '@/utils/colorUtils';

  const store = useAppointmentStore();
  const { locale } = useI18n();
  const currentLang = computed(() => locale.value || 'hu-HU');

  // --- ALAP NÉZET ÉS NAPTÁR LOGIKA ---
  const views = [{ id: 'day', icon: 'pi pi-clock', label: 'Napi' }, { id: 'week', icon: 'pi pi-list', label: 'Heti' }, { id: 'month', icon: 'pi pi-calendar', label: 'Havi' }];
  const currentView = ref('month');
  const currentDate = ref(new Date());

  const currentMonthName = computed(() => currentDate.value.toLocaleString(currentLang.value, { month: 'long' }).toUpperCase());
  const currentYear = computed(() => currentDate.value.getFullYear());

  const pagerText = computed(() => {
    if (currentView.value === 'month') return `${currentMonthName.value} ${currentYear.value}`;
    if (currentView.value === 'day') {
      const d = currentDate.value;
      const month = d.toLocaleString(currentLang.value, { month: 'long' }).toUpperCase();
      return `${d.getFullYear()}. ${month} ${d.getDate()}.`;
    }
    if (currentView.value === 'week') {
      if (currentWeekDays.value.length === 0) return `${currentMonthName.value} ${currentYear.value}`;
      const start = currentWeekDays.value[0].date; const end = currentWeekDays.value[6].date;
      const startMonth = start.toLocaleString(currentLang.value, { month: 'short' }).toUpperCase();
      const endMonth = end.toLocaleString(currentLang.value, { month: 'short' }).toUpperCase();
      if (start.getMonth() === end.getMonth()) return `${start.getFullYear()}. ${startMonth} ${start.getDate()} - ${end.getDate()}.`;
      return `${start.getFullYear()}. ${startMonth} ${start.getDate()} - ${endMonth} ${end.getDate()}.`;
    } return '';
  });

  const getDayNameShort = (date) => date.toLocaleString(currentLang.value, { weekday: 'short' });
  const getDayNameLong = (date) => date.toLocaleString(currentLang.value, { weekday: 'long' });
  const formatDateLong = (date) => date.toLocaleDateString(currentLang.value, { year: 'numeric', month: 'long', day: 'numeric' });
  const formatDateShort = (iso) => iso ? new Date(iso).toLocaleDateString(currentLang.value, { month: 'short', day: 'numeric' }) : '';
  const formatTime = (iso) => iso ? new Date(iso).toLocaleTimeString(currentLang.value, { hour: '2-digit', minute: '2-digit' }) : '';
  const formatDurationStr = (m) => m >= 60 ? `${Math.floor(m / 60)}:${(m % 60).toString().padStart(2, '0')}` : `${m}p`;
  const getDurationMinutes = (app) => Math.round((new Date(app.endDateTime) - new Date(app.startDateTime)) / 60000);

  const dynamicWeekDays = computed(() => {
    const days = []; const baseDate = new Date(2024, 0, 1);
    for (let i = 0; i < 7; i++) { const d = new Date(baseDate); d.setDate(d.getDate() + i); days.push(getDayNameShort(d)); }
    return days;
  });

  const goPrev = () => {
    const d = new Date(currentDate.value);
    if (currentView.value === 'month') d.setMonth(d.getMonth() - 1);
    else if (currentView.value === 'week') d.setDate(d.getDate() - 7); else d.setDate(d.getDate() - 1);
    currentDate.value = d;
  };
  const goNext = () => {
    const d = new Date(currentDate.value);
    if (currentView.value === 'month') d.setMonth(d.getMonth() + 1);
    else if (currentView.value === 'week') d.setDate(d.getDate() + 7); else d.setDate(d.getDate() + 1);
    currentDate.value = d;
  };
  const goToday = () => { currentDate.value = new Date(); };

  const displayAppointments = computed(() => store.appointments || []);

  const calendarDays = computed(() => {
    const year = currentDate.value.getFullYear(); const month = currentDate.value.getMonth();
    const firstDay = new Date(year, month, 1); let startingDay = firstDay.getDay() || 7; startingDay--;
    const days = [];
    for (let i = 0; i < startingDay; i++) days.push(createDayObject(new Date(year, month, -startingDay + i + 1), false));
    for (let i = 1; i <= new Date(year, month + 1, 0).getDate(); i++) days.push(createDayObject(new Date(year, month, i), true));
    const remainingCells = 42 - days.length;
    for (let i = 1; i <= remainingCells; i++) days.push(createDayObject(new Date(year, month + 1, i), false));
    return days;
  });

  const currentWeekDays = computed(() => {
    const targetStr = currentDate.value.toDateString(); const idx = calendarDays.value.findIndex(d => d.date.toDateString() === targetStr);
    if (idx === -1) return []; const start = idx - (idx % 7); return calendarDays.value.slice(start, start + 7);
  });

  const createDayObject = (date, isCurrentMonth) => {
    const isToday = date.toDateString() === new Date().toDateString();
    const targetStr = date.toDateString();
    const dayApps = displayAppointments.value.filter(a => new Date(a.startDateTime).toDateString() === targetStr);
    return { date, isCurrentMonth, isToday, hasAppointments: dayApps.length > 0, appointmentCount: dayApps.length, hasPending: dayApps.some(a => a.status === 0 || a.status === 'Pending'), loadPercentage: dayApps.length > 0 ? (dayApps.length * 15) : 0 };
  };

  const onDayClick = (dayObj) => { currentDate.value = dayObj.date; currentView.value = 'day'; };

  const upcomingAppointments = computed(() => {
    const now = new Date(); now.setHours(0, 0, 0, 0);
    return displayAppointments.value.filter(app => new Date(app.startDateTime) >= now).sort((a, b) => new Date(a.startDateTime) - new Date(b.startDateTime)).slice(0, 5);
  });

  const currentWeekUpcomingAppointments = computed(() => {
    if (currentWeekDays.value.length === 0) return [];
    const now = new Date(); now.setHours(0, 0, 0, 0);
    const weekStart = new Date(currentWeekDays.value[0].date); weekStart.setHours(0, 0, 0, 0);
    const weekEnd = new Date(currentWeekDays.value[6].date); weekEnd.setHours(23, 59, 59, 999);
    const filterStart = now > weekStart ? now : weekStart;
    return displayAppointments.value.filter(app => { const appDate = new Date(app.startDateTime); return appDate >= filterStart && appDate <= weekEnd; }).sort((a, b) => new Date(a.startDateTime) - new Date(b.startDateTime));
  });

  const currentDayAppointments = computed(() => {
    const targetStr = currentDate.value.toDateString();
    return displayAppointments.value.filter(app => new Date(app.startDateTime).toDateString() === targetStr).sort((a, b) => new Date(a.startDateTime) - new Date(b.startDateTime));
  });

  const timelineData = computed(() => {
    const apps = currentDayAppointments.value;
    let minMinutes = 8 * 60; let maxMinutes = 15 * 60;
    if (apps.length > 0) {
      const actualMin = Math.min(...apps.map(a => { const d = new Date(a.startDateTime); return isNaN(d) ? 8 * 60 : d.getHours() * 60 + d.getMinutes(); }));
      const actualMax = Math.max(...apps.map(a => { const d = new Date(a.endDateTime); return isNaN(d) ? 15 * 60 : d.getHours() * 60 + d.getMinutes(); }));
      if (actualMin < minMinutes) minMinutes = Math.floor(actualMin / 60) * 60; if (actualMax > maxMinutes) maxMinutes = Math.ceil(actualMax / 60) * 60;
    }
    if (minMinutes >= maxMinutes) maxMinutes = minMinutes + 60;
    const totalMinutes = maxMinutes - minMinutes;
    const hours = []; for (let m = minMinutes; m <= maxMinutes; m += 60) hours.push({ val: m / 60, left: ((m - minMinutes) / totalMinutes) * 100 });
    const mappedApps = [];
    apps.forEach(app => {
      const sDate = new Date(app.startDateTime); const eDate = new Date(app.endDateTime); if (isNaN(sDate) || isNaN(eDate)) return;
      const startM = sDate.getHours() * 60 + sDate.getMinutes(); const endM = eDate.getHours() * 60 + eDate.getMinutes();
      let width = ((endM - startM) / totalMinutes) * 100; if (width <= 0) width = 2;
      mappedApps.push({ ...app, startM, endM, left: ((startM - minMinutes) / totalMinutes) * 100, width: width });
    });
    mappedApps.sort((a, b) => a.startM - b.startM);
    const gaps = [];
    for (let i = 0; i < mappedApps.length - 1; i++) {
      const diff = mappedApps[i + 1].startM - mappedApps[i].endM;
      if (diff > 0) gaps.push({ durationMinutes: diff, left: ((mappedApps[i].endM - minMinutes) / totalMinutes) * 100, width: (diff / totalMinutes) * 100 });
    }
    return { minMinutes, maxMinutes, totalMinutes, hours, appointments: mappedApps, gaps };
  });

  const fetchDataForCurrentView = () => {
    const d = new Date(currentDate.value);
    const start = new Date(d.getFullYear(), d.getMonth() - 1, 1);
    const end = new Date(d.getFullYear(), d.getMonth() + 2, 0);
    store.fetchAppointments(start, end);
  };

  // ============================================================================
  // ÚJ: SZÍNEK GENERÁLÁSA ÜGYFELEKHEZ
  // ============================================================================
  const getCustomerColor = (customerId) => {
    if (!customerId) return 'hsl(0, 0%, 90%)';
    const str = String(customerId);
    let hash = 0;
    for (let i = 0; i < str.length; i++) {
      hash = str.charCodeAt(i) + ((hash << 5) - hash);
    }
    const hue = Math.abs(hash) % 360;
    // Világos pasztell árnyalat (S: 70%, L: 85%)
    return `hsl(${hue}, 70%, 85%)`;
  };

  const getCustomerColorDarker = (customerId) => {
    if (!customerId) return 'hsl(0, 0%, 70%)';
    const str = String(customerId);
    let hash = 0;
    for (let i = 0; i < str.length; i++) {
      hash = str.charCodeAt(i) + ((hash << 5) - hash);
    }
    const hue = Math.abs(hash) % 360;
    // Kicsit sötétebb árnyalat a keretekhez (S: 60%, L: 65%)
    return `hsl(${hue}, 60%, 65%)`;
  };

  // ============================================================================
  // ÚJ: VALÓS ÜGYFELEK ÉS FOGLALÁS ŰRLAP
  // ============================================================================
  const availableServices = ref([]);
  const customersList = ref([]);

  const isModalOpen = ref(false);
  const isEditing = ref(false);

  const form = ref({
    id: null, customerId: '', customerFullName: '', customerPhone: '', employeeId: 1,
    date: '', time: '08:00', status: 1, notes: '', items: []
  });

  const builder = ref({ category: '', serviceId: '', variantId: '', duration: 0 });

  const fetchServicesForAdmin = async () => {
    try {
      const response = await bookingApi.getPublicServices();
      const rawServices = response.data.$values || response.data || [];

      // Szűrés: Csak aminek van ára (>0), és ahol a kategória emiatt nem ürül ki
      const filteredServices = rawServices.map(s => {
        const vars = s.variants?.$values || s.variants || [];
        return {
          ...s,
          variants: vars.filter(v => v.price != null && v.price > 0)
        };
      }).filter(s => s.variants.length > 0);

      availableServices.value = filteredServices;
    } catch (error) { console.error('Hiba a szolgáltatások betöltésekor:', error); }
  };

  const fetchCustomers = async () => {
    try {
      const response = await bookingApi.getCustomers();
      customersList.value = response.data.$values || response.data || [];
    } catch (error) { console.error('Hiba az ügyfelek betöltésekor:', error); }
  };

  const getLocText = (dict) => dict ? (dict[currentLang.value] || dict['hu'] || '') : '';
  const getInitials = (name) => name ? name.split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase() : '?';

  const getCustomerName = (id) => {
    const c = customersList.value.find(x => x.id === id);
    return c && c.name && c.name !== 'Ismeretlen Vendég' ? c.name : `Vendég #${id}`;
  };

  const getCustomerInitials = (id) => {
    const name = getCustomerName(id);
    if (name.startsWith('Vendég #')) return `#${id}`;
    return getInitials(name);
  };

  const getVariantFullName = (variantId) => {
    for (const s of availableServices.value) {
      const v = s.variants?.find(vx => vx.id === variantId);
      if (v) return `${getLocText(s.name)} - ${getLocText(v.variantName)}`;
    } return 'Ismeretlen';
  };

  const availableCategories = computed(() => {
    const cats = new Set();
    availableServices.value.forEach(s => cats.add(getLocText(s.category) || 'Egyéb'));
    return Array.from(cats).sort();
  });

  const availableServicesInCategory = computed(() => {
    return availableServices.value.filter(s => (getLocText(s.category) || 'Egyéb') === builder.value.category);
  });

  const availableVariantsInService = computed(() => {
    const s = availableServices.value.find(s => s.id === builder.value.serviceId);
    return s ? s.variants : [];
  });

  const resetBuilder = (level) => {
    if (level === 1) {
      builder.value.serviceId = '';
      builder.value.variantId = '';
      builder.value.duration = 0;

      const services = availableServicesInCategory.value;
      if (services.length === 1) {
        builder.value.serviceId = services[0].id;
        resetBuilder(2);
      }
    }

    if (level === 2) {
      builder.value.variantId = '';
      builder.value.duration = 0;

      const variants = availableVariantsInService.value;
      if (variants.length === 1) {
        builder.value.variantId = variants[0].id;
        onVariantSelected();
      }
    }
  };

  const onVariantSelected = () => {
    const v = availableVariantsInService.value.find(vx => vx.id === builder.value.variantId);
    if (v) builder.value.duration = v.duration || 30;
  };

  const addServiceToForm = () => {
    const s = availableServices.value.find(x => x.id === builder.value.serviceId);
    const v = availableVariantsInService.value.find(x => x.id === builder.value.variantId);
    if (s && v) {
      form.value.items.push({
        variantId: v.id,
        name: `${getLocText(s.name)} - ${getLocText(v.variantName)}`,
        duration: builder.value.duration,
        price: v.price
      });
      builder.value = { category: '', serviceId: '', variantId: '', duration: 0 };
    }
  };

  const removeFormItem = (idx) => form.value.items.splice(idx, 1);

  const handleCustomerSelect = () => {
    if (form.value.customerId === 'new') {
      form.value.customerFullName = '';
      form.value.customerPhone = '';
    } else {
      const c = customersList.value.find(x => x.id === form.value.customerId);
      if (c) form.value.customerFullName = c.name;
    }
  };

  const openNewAppointment = () => {
    isEditing.value = false;
    const d = new Date(currentDate.value.getTime() - (currentDate.value.getTimezoneOffset() * 60000));
    form.value = { id: null, customerId: '', customerFullName: '', customerPhone: '', employeeId: 1, date: d.toISOString().split('T')[0], time: '08:00', status: 1, notes: '', items: [] };
    builder.value = { category: '', serviceId: '', variantId: '', duration: 0 };
    isModalOpen.value = true;
  };

  const openAppointmentDetails = (app) => {
    isEditing.value = true;
    const d = new Date(app.startDateTime);
    const mappedItems = app.items?.map(i => ({
      variantId: i.serviceVariantId, name: getVariantFullName(i.serviceVariantId), duration: i.calculatedDurationMinutes || 30, price: i.price
    })) || [];

    const c = customersList.value.find(x => x.id === app.customerId);

    form.value = {
      id: app.id, customerId: app.customerId, customerFullName: c ? c.name : '', customerPhone: '', employeeId: app.employeeId,
      date: new Date(d.getTime() - (d.getTimezoneOffset() * 60000)).toISOString().split('T')[0],
      time: d.toTimeString().substring(0, 5),
      status: (app.status === 0 || app.status === 'Pending') ? 0 : 1,
      notes: app.notes || '', items: mappedItems
    };
    builder.value = { category: '', serviceId: '', variantId: '', duration: 0 };
    isModalOpen.value = true;
  };

  const closeModal = () => { isModalOpen.value = false; };

  const isFormValid = computed(() => {
    if (form.value.items.length === 0) return false;
    if (!form.value.customerId) return false;

    if (form.value.customerId === 'new') {
      const hasName = form.value.customerFullName && form.value.customerFullName.trim() !== '';
      const hasPhone = form.value.customerPhone && form.value.customerPhone.trim() !== '';
      if (!hasName && !hasPhone) return false;
    }
    return true;
  });

  const handleSave = async () => {
    try {
      let finalCustId = form.value.customerId;

      if (form.value.customerId === 'new') {
        const nameVal = form.value.customerFullName?.trim() || '';
        const phoneVal = form.value.customerPhone?.trim() || '';

        const newCustomerResponse = await bookingApi.createCustomer({
          fullName: nameVal,
          phone: phoneVal
        });
        finalCustId = newCustomerResponse.data.id;
        customersList.value.push(newCustomerResponse.data);
      } else {
        finalCustId = parseInt(form.value.customerId);
      }

      const startDateTime = new Date(`${form.value.date}T${form.value.time}:00`).toISOString();
      const mappedItems = form.value.items.map(i => ({
        serviceVariantId: i.variantId,
        durationMinutes: i.duration
      }));

      const payload = {
        customerId: finalCustId,
        employeeId: parseInt(form.value.employeeId),
        startDateTime: startDateTime,
        items: mappedItems,
        status: parseInt(form.value.status),
        notes: form.value.notes,
        force: false
      }; 

      await store.saveAppointment(payload, form.value.id);
      closeModal(); fetchDataForCurrentView();
    } catch (error) {
      alert("Hiba mentéskor: " + (error.response?.data || error.message));
    }
  };

  const handleDelete = async () => {
    if (confirm("Biztosan véglegesen törlöd ezt a foglalást?")) {
      try {
        if (store.deleteAppointment) await store.deleteAppointment(form.value.id);
        closeModal(); fetchDataForCurrentView();
      } catch (error) { alert("Hiba történt a törlés során."); }
    }
  };

  onMounted(() => {
    store.initUserPermissions();
    fetchDataForCurrentView();
    fetchServicesForAdmin();
    fetchCustomers();
  });
  watch(currentDate, () => fetchDataForCurrentView());
</script>
