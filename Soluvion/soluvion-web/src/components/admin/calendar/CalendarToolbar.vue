<template>
  <div class="sticky top-0 z-20 bg-surface border-b border-text/10 p-3 md:p-4 shadow-sm">
    <div class="max-w-7xl mx-auto w-full flex flex-col xl:flex-row justify-between items-start xl:items-center gap-2 md:gap-4">

      <div class="flex items-baseline gap-2 md:gap-4 w-full xl:w-auto">
        <h2 class="text-xl md:text-2xl font-bold text-text drop-shadow-sm">{{ $t('calendar.dashboardTitle') || 'Vezérlőpult' }}</h2>
        <span class="text-xs md:text-sm text-text-muted">{{ $t('calendar.role') || 'Szerepkör' }}: <strong class="text-primary">{{ userRole }}</strong></span>
      </div>

      <div class="flex flex-col md:flex-row items-center gap-2 w-full xl:w-auto mt-1 md:mt-0">

        <div class="flex flex-row w-full md:w-auto gap-2">
          <button @click="$emit('today')" class="flex-1 md:flex-none px-3 md:px-5 min-h-[44px] font-bold text-sm bg-background border border-text/10 hover:border-primary/50 transition-colors rounded-xl text-text shadow-sm flex items-center justify-center gap-1.5">
            <i class="pi pi-calendar-times text-primary"></i> <span>{{ $t('calendar.today') || 'Ma' }}</span>
          </button>

          <div class="flex flex-1 md:flex-none bg-background rounded-xl p-1 shadow-inner border border-text/5">
            <button v-for="view in views" :key="view.id"
                    @click="$emit('update:currentView', view.id)"
                    class="flex-1 min-h-[36px] md:min-h-[44px] px-2 md:px-4 rounded-lg font-bold text-xs md:text-sm flex items-center justify-center gap-1 md:gap-2 transition-all"
                    :class="currentView === view.id ? 'bg-surface text-primary shadow-sm ring-1 ring-text/10' : 'text-text-muted hover:text-text hover:bg-text/5'">
              <i :class="view.icon"></i>
              <span class="hidden sm:inline">{{ $t(`calendar.${view.id}`) || view.label }}</span>
            </button>
          </div>
        </div>

        <div class="flex items-center bg-background rounded-xl p-1 shadow-inner border border-text/5 w-full md:w-auto justify-between md:justify-center">
          <button @click="$emit('prev')" class="min-w-[44px] min-h-[36px] md:min-h-[44px] flex items-center justify-center rounded-lg hover:bg-text/5 transition-colors text-text">
            <i class="pi pi-chevron-left font-bold text-base md:text-lg"></i>
          </button>

          <div class="px-2 md:px-4 font-black text-text uppercase tracking-wider text-xs md:text-base text-center w-full md:min-w-[220px] truncate">
            {{ pagerText }}
          </div>

          <button @click="$emit('next')" class="min-w-[44px] min-h-[36px] md:min-h-[44px] flex items-center justify-center rounded-lg hover:bg-text/5 transition-colors text-text">
            <i class="pi pi-chevron-right font-bold text-base md:text-lg"></i>
          </button>
        </div>

      </div>
    </div>
  </div>
</template>

<script setup>
  defineProps({
    currentView: {
      type: String,
      required: true
    },
    pagerText: {
      type: String,
      required: true
    },
    userRole: {
      type: String,
      default: 'Admin'
    }
  });

  defineEmits(['update:currentView', 'prev', 'next', 'today']);

  const views = [
    { id: 'day', icon: 'pi pi-clock', label: 'Napi' },
    { id: 'week', icon: 'pi pi-list', label: 'Heti' },
    { id: 'month', icon: 'pi pi-calendar', label: 'Havi' }
  ];
</script>
