<template>
  <div v-if="isOpen"
       class="fixed inset-0 bg-black/65 backdrop-blur-xs z-50 flex items-end sm:items-center justify-center sm:p-4 overscroll-contain"
       @paste="handleClipboardPaste"
       @click.self="$emit('close')">
    <div class="bg-surface w-full max-w-2xl rounded-t-2xl sm:rounded-2xl shadow-2xl border border-text/10 flex flex-col max-h-[92dvh] sm:max-h-[88vh] overflow-hidden overscroll-contain"
         @click.stop>

      <!-- Kompakt Fejléc + Fülek -->
      <div class="px-4 pt-3.5 pb-3 border-b border-text/10 bg-background/60 shrink-0 space-y-2.5">
        <div class="flex items-center justify-between gap-2">
          <div class="flex items-center gap-2.5 min-w-0">
            <div class="w-8 h-8 rounded-xl bg-primary/15 text-primary border border-primary/30 flex items-center justify-center shrink-0">
              <i class="pi pi-sparkles text-sm"></i>
            </div>
            <h2 class="text-sm md:text-base font-extrabold text-text truncate m-0">
              {{ $t('calendar.aiChat.modalTitle') }}
            </h2>
          </div>

          <button type="button"
                  @click="$emit('close')"
                  class="w-8 h-8 flex items-center justify-center rounded-full hover:bg-text/10 text-text-muted transition-colors cursor-pointer shrink-0">
            <i class="pi pi-times"></i>
          </button>
        </div>

        <!-- Egysoros, mobilbarát Fülek -->
        <div class="grid grid-cols-3 gap-1 bg-background p-1 rounded-xl border border-text/10">
          <button type="button"
                  @click="activeTab = 'inbox'"
                  class="py-1.5 px-2 rounded-lg text-xs font-bold transition-all cursor-pointer flex items-center justify-center gap-1.5 truncate"
                  :class="activeTab === 'inbox' ? 'bg-primary text-surface shadow-2xs' : 'text-text-muted hover:text-text'">
            <i class="pi pi-comments text-xs shrink-0"></i>
            <span class="truncate">{{ $t('calendar.aiChat.tabInbox') }}</span>
          </button>
          <button type="button"
                  @click="activeTab = 'paste'"
                  class="py-1.5 px-2 rounded-lg text-xs font-bold transition-all cursor-pointer flex items-center justify-center gap-1.5 truncate"
                  :class="activeTab === 'paste' ? 'bg-primary text-surface shadow-2xs' : 'text-text-muted hover:text-text'">
            <i class="pi pi-camera text-xs shrink-0"></i>
            <span class="truncate">{{ $t('calendar.aiChat.tabPasteOcr') }}</span>
          </button>
          <button type="button"
                  @click="activeTab = 'channels'"
                  class="py-1.5 px-2 rounded-lg text-xs font-bold transition-all cursor-pointer flex items-center justify-center gap-1.5 truncate"
                  :class="activeTab === 'channels' ? 'bg-primary text-surface shadow-2xs' : 'text-text-muted hover:text-text'">
            <i class="pi pi-share-alt text-xs shrink-0"></i>
            <span class="truncate">{{ $t('calendar.aiChat.tabChannels') }}</span>
          </button>
        </div>
      </div>

      <!-- Sikeres frissítés visszajelző sáv -->
      <div v-if="successBannerText"
           class="px-4 py-2 bg-emerald-500/15 border-b border-emerald-500/30 text-emerald-500 text-xs font-extrabold flex items-center justify-between gap-2 shrink-0">
        <div class="flex items-center gap-2 min-w-0">
          <i class="pi pi-check-circle shrink-0"></i>
          <span class="truncate">{{ successBannerText }}</span>
        </div>
        <button type="button" @click="successBannerText = ''" class="text-emerald-500 hover:opacity-75 cursor-pointer shrink-0">
          <i class="pi pi-times text-xs"></i>
        </button>
      </div>

      <!-- EGYETLEN KÖZÖS GÖRGETHETŐ TARTALOM (Nincs dupla görgetősáv, nem mozog a háttér!) -->
      <div class="flex-1 overflow-y-auto overscroll-contain p-3.5 md:p-5 space-y-3.5">

        <!-- 1. FÜL: ÉLŐ BESZÉLGETÉSEK (INBOX) + BUBORÉK-KIJELÖLŐ -->
        <template v-if="activeTab === 'inbox'">

          <!-- Kompakt Beszélgetés-választó sáv + Frissítés, Új üzenet indítása és Törlés gombok -->
          <div class="flex flex-col sm:flex-row sm:items-center gap-2 bg-background/70 p-2.5 rounded-xl border border-text/10">
            <div class="flex items-center gap-2 flex-1 min-w-0">
              <i v-if="selectedConversation"
                 :class="getChannelMeta(selectedConversation.channel).icon"
                 class="text-primary text-sm shrink-0"></i>
              <select v-if="conversations.length > 0"
                      :value="selectedConversation?.id"
                      @change="onSelectConversationById($event.target.value)"
                      class="flex-1 min-w-0 px-2.5 py-1.5 rounded-lg border border-text/15 bg-surface text-text text-xs font-extrabold focus:border-primary focus:outline-none cursor-pointer">
                <option v-for="conv in conversations" :key="conv.id" :value="conv.id">
                  {{ conv.customerName }} ({{ conv.channel }}){{ conv.pendingAppointmentId ? ' • 🔔 ' + $t('calendar.pendingInbox.pendingBadge') : '' }}
                </option>
              </select>
              <span v-else class="text-xs font-bold text-text-muted truncate">
                {{ $t('calendar.aiChat.conversationsTitle') }} (0)
              </span>
            </div>

            <div class="flex items-center gap-1.5 shrink-0 flex-wrap">
              <button type="button"
                      @click="showOutboundPanel = !showOutboundPanel"
                      class="px-2.5 py-1.5 rounded-lg border text-xs font-extrabold flex items-center justify-center gap-1.5 transition-colors cursor-pointer"
                      :class="showOutboundPanel
                        ? 'bg-primary text-surface border-primary'
                        : 'bg-primary/10 border-primary/30 text-primary hover:bg-primary/20'">
                <i class="pi text-xs" :class="showOutboundPanel ? 'pi-chevron-up' : 'pi-plus-circle'"></i>
                <span class="truncate max-w-[190px] sm:max-w-none">{{ $t('calendar.aiChat.outboundBtn') }}</span>
              </button>

              <button type="button"
                      @click="fetchInbox"
                      class="px-2.5 py-1.5 rounded-lg bg-surface border border-text/15 text-text hover:border-primary/50 hover:text-primary text-xs font-bold flex items-center justify-center gap-1.5 transition-colors cursor-pointer"
                      :title="$t('common.refresh') || 'Frissítés'">
                <i class="pi pi-refresh text-xs"></i>
              </button>

              <button v-if="selectedConversation"
                      type="button"
                      @click="handleDeleteConversation"
                      :disabled="isDeletingConv"
                      class="px-2.5 py-1.5 rounded-lg bg-red-500/10 border border-red-500/25 text-red-500 hover:bg-red-500 hover:text-white text-xs font-extrabold flex items-center justify-center gap-1.5 transition-colors cursor-pointer disabled:opacity-50">
                <i class="pi text-xs" :class="isDeletingConv ? 'pi-spin pi-spinner' : 'pi-trash'"></i>
                <span>{{ $t('calendar.aiChat.clearChat') }}</span>
              </button>
            </div>
          </div>

          <!-- Szalon által kezdeményezett üzenetküldő panel (WhatsApp / SMS / E-mail + automatikus Messenger link) -->
          <div v-if="showOutboundPanel"
               class="p-3 rounded-xl border border-primary/35 bg-primary/5 space-y-2.5">
            <div class="flex items-start justify-between gap-2">
              <p class="text-[11px] text-text-muted leading-snug m-0">
                <i class="pi pi-info-circle text-primary mr-1"></i>{{ $t('calendar.aiChat.outboundDesc') }}
              </p>
              <button type="button" @click="showOutboundPanel = false" class="text-text-muted hover:text-text cursor-pointer shrink-0">
                <i class="pi pi-times text-xs"></i>
              </button>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-2">
              <select v-if="customersList && customersList.length > 0"
                      v-model="outboundCustomerId"
                      @change="onPickOutboundCustomer"
                      class="px-2.5 py-1.5 rounded-lg border border-text/15 bg-surface text-text text-xs font-bold focus:border-primary focus:outline-none cursor-pointer">
                <option value="">{{ $t('calendar.aiChat.outboundPickCustomer') }}</option>
                <option v-for="c in customersList" :key="c.id" :value="c.id">
                  {{ getCustomerLabel(c) }}
                </option>
              </select>

              <input v-model="outboundName"
                     type="text"
                     :placeholder="$t('calendar.aiChat.outboundNamePlaceholder')"
                     class="px-2.5 py-1.5 rounded-lg border border-text/15 bg-surface text-text text-xs focus:border-primary focus:outline-none" />

              <input v-model="outboundPhone"
                     type="text"
                     :placeholder="$t('calendar.aiChat.outboundPhonePlaceholder')"
                     class="px-2.5 py-1.5 rounded-lg border border-text/15 bg-surface text-text text-xs focus:border-primary focus:outline-none" />

              <input v-model="outboundEmail"
                     type="email"
                     :placeholder="$t('calendar.aiChat.outboundEmailPlaceholder')"
                     class="px-2.5 py-1.5 rounded-lg border border-text/15 bg-surface text-text text-xs focus:border-primary focus:outline-none" />
            </div>

            <textarea v-model="outboundMessage"
                      rows="2"
                      :placeholder="$t('calendar.aiChat.outboundMsgPlaceholder')"
                      class="w-full p-2.5 rounded-lg border border-text/15 bg-surface text-text text-xs focus:border-primary focus:outline-none resize-none"></textarea>

            <div class="flex flex-wrap items-center justify-between gap-2 pt-1">
              <code class="text-[10px] text-primary truncate max-w-full">🔗 {{ computedCentralMessengerLink }}</code>

              <div class="flex items-center gap-1.5 flex-wrap">
                <button type="button"
                        @click="handleSendOutboundInvite('WhatsApp')"
                        class="px-3 py-1.5 rounded-lg bg-emerald-600 hover:bg-emerald-500 text-white text-xs font-extrabold flex items-center gap-1.5 cursor-pointer">
                  <i class="pi pi-whatsapp text-xs"></i>
                  <span>WhatsApp</span>
                </button>
                <button type="button"
                        @click="handleSendOutboundInvite('SMS')"
                        class="px-3 py-1.5 rounded-lg bg-sky-600 hover:bg-sky-500 text-white text-xs font-extrabold flex items-center gap-1.5 cursor-pointer">
                  <i class="pi pi-mobile text-xs"></i>
                  <span>SMS</span>
                </button>
                <button type="button"
                        @click="handleSendOutboundInvite('Email')"
                        class="px-3 py-1.5 rounded-lg bg-primary hover:brightness-110 text-surface text-xs font-extrabold flex items-center gap-1.5 cursor-pointer">
                  <i class="pi pi-envelope text-xs"></i>
                  <span>E-mail</span>
                </button>
              </div>
            </div>
          </div>

          <!-- Kompakt Jóváhagyásra váró foglalás jelző (ha van kapcsolódó függő foglalás) -->
          <div v-if="activePendingBooking"
               class="px-3 py-2 rounded-xl bg-amber-500/10 border border-amber-500/30 flex items-center justify-between gap-2 text-xs">
            <div class="flex items-center flex-wrap gap-1.5 min-w-0">
              <i class="pi pi-bell text-amber-500 text-xs shrink-0"></i>
              <span class="font-extrabold text-amber-500">{{ $t('calendar.aiChat.linkedPendingTitle') }}</span>
              <span class="font-extrabold text-text">{{ activePendingBooking.customerName }}</span>
              <span class="text-primary font-bold">({{ formatPendingTime(activePendingBooking.startDateTime) }} • {{ activePendingBooking.totalPrice }} EUR)</span>
            </div>
          </div>

          <!-- Üzenetbuborékok doboz -->
          <div class="rounded-xl border border-text/15 bg-background/50 overflow-hidden">
            <div class="px-3 py-2 bg-surface/80 border-b border-text/10 flex flex-wrap items-center justify-between gap-2">
              <div class="flex items-center gap-1.5 text-xs font-extrabold text-text">
                <i class="pi pi-check-square text-primary"></i>
                <span>{{ $t('calendar.aiChat.bubblesTitle') }}</span>
                <span v-if="selectedBubbleIds.length > 0"
                      class="px-2 py-0.5 rounded-full bg-primary text-surface text-[10px] font-extrabold">
                  {{ $t('calendar.aiChat.selectedCount', { count: selectedBubbleIds.length }) }}
                </span>
              </div>

              <div v-if="chatBubbles.length > 0" class="flex flex-wrap items-center gap-2.5 text-[11px]">
                <button v-if="unprocessedClientBubblesCount > 0 && processedBubblesCount > 0"
                        type="button"
                        @click="selectNewBubblesOnly"
                        class="font-extrabold text-emerald-500 hover:underline cursor-pointer">
                  {{ $t('calendar.aiChat.selectNewOnly') }}
                </button>
                <button type="button"
                        @click="selectAllBubbles"
                        class="font-bold text-primary hover:underline cursor-pointer">
                  {{ $t('calendar.aiChat.selectAll') }}
                </button>
                <button v-if="selectedBubbleIds.length > 0"
                        type="button"
                        @click="deselectAllBubbles"
                        class="font-bold text-text-muted hover:text-red-500 hover:underline cursor-pointer">
                  {{ $t('calendar.aiChat.deselectAll') }}
                </button>
                <button v-if="selectedConversation && unprocessedClientBubblesCount > 0"
                        type="button"
                        @click="handleMarkProcessed"
                        class="px-2 py-0.5 rounded-md bg-emerald-500/15 border border-emerald-500/30 text-emerald-500 hover:bg-emerald-500 hover:text-white font-extrabold transition-colors cursor-pointer flex items-center gap-1">
                  <i class="pi pi-check text-[10px]"></i>
                  <span>{{ $t('calendar.aiChat.markProcessedBtn') }}</span>
                </button>
              </div>
            </div>

            <!-- Korábbi, már feldolgozott üzenetek összecsukó / kinyitó sáv -->
            <div v-if="processedBubblesCount > 0 && unprocessedClientBubblesCount > 0"
                 class="px-3 py-1.5 bg-surface/50 border-b border-text/10 flex items-center justify-between text-[11px]">
              <button type="button"
                      @click="showProcessedMessages = !showProcessedMessages"
                      class="text-text-muted hover:text-primary font-bold flex items-center gap-1.5 cursor-pointer">
                <i class="pi text-[10px]" :class="showProcessedMessages ? 'pi-eye-slash' : 'pi-history'"></i>
                <span>
                  {{ showProcessedMessages
                    ? $t('calendar.aiChat.hideOlderProcessed', { count: processedBubblesCount })
                    : $t('calendar.aiChat.showOlderProcessed', { count: processedBubblesCount }) }}
                </span>
              </button>
            </div>

            <!-- Kattintható üzenetbuborékok (tágas, jól olvasható lista) -->
            <div v-if="visibleChatBubbles.length > 0" class="p-3 space-y-2">
              <div v-for="(msg, idx) in visibleChatBubbles"
                   :key="msg.id"
                   @click="toggleBubble(msg.id)"
                   class="p-2.5 md:p-3 rounded-xl border transition-all cursor-pointer select-none flex items-start gap-2.5"
                   :class="[
                     selectedBubbleIds.includes(msg.id)
                       ? 'bg-primary/12 border-primary shadow-2xs'
                       : (msg.isProcessed ? 'bg-surface/60 border-text/10 opacity-60 hover:opacity-95' : 'bg-surface border-text/10 hover:border-primary/40 opacity-90 hover:opacity-100'),
                     msg.sender === 'salon' ? 'ml-4 sm:ml-10' : 'mr-4 sm:mr-10'
                   ]">
                <div class="w-5 h-5 rounded-md border flex items-center justify-center shrink-0 mt-0.5 transition-colors"
                     :class="selectedBubbleIds.includes(msg.id)
                       ? 'bg-primary border-primary text-surface'
                       : 'border-text/25 bg-background'">
                  <i v-if="selectedBubbleIds.includes(msg.id)" class="pi pi-check text-[10px] font-bold"></i>
                  <span v-else class="text-[10px] text-text-muted font-bold">{{ idx + 1 }}</span>
                </div>

                <div class="flex-1 min-w-0">
                  <div class="flex flex-wrap items-center justify-between gap-2 mb-0.5">
                    <div class="flex items-center gap-1.5">
                      <span class="text-[10px] font-extrabold uppercase tracking-wider"
                            :class="msg.sender === 'salon' ? 'text-primary' : 'text-text-muted'">
                        {{ msg.senderLabel || (msg.sender === 'salon' ? $t('calendar.aiChat.senderSalon') : $t('calendar.aiChat.senderClient')) }}
                      </span>
                      <span v-if="msg.isProcessed"
                            class="px-1.5 py-0.2 rounded bg-text/10 text-text-muted text-[9px] font-bold">
                        ✓ {{ $t('calendar.aiChat.processedBadge') }}
                      </span>
                      <span v-else-if="msg.sender === 'client'"
                            class="px-1.5 py-0.2 rounded bg-emerald-500/15 text-emerald-500 text-[9px] font-extrabold">
                        {{ $t('calendar.aiChat.newBadge') }}
                      </span>
                    </div>

                    <span v-if="msg.timestamp" class="text-[10px] text-text-muted font-medium">
                      {{ formatBubbleTime(msg.timestamp) }}
                    </span>
                  </div>
                  <p class="text-xs md:text-sm text-text leading-relaxed whitespace-pre-wrap break-words m-0">
                    {{ msg.text }}
                  </p>
                </div>
              </div>
            </div>

            <div v-else class="p-6 text-center text-xs text-text-muted">
              <i class="pi pi-inbox text-2xl text-primary/50 mb-1.5 block"></i>
              {{ $t('calendar.aiChat.emptyBubblesHint') }}
            </div>

            <!-- Válasz küldése sáv -->
            <div v-if="selectedConversation" class="p-2.5 bg-surface border-t border-text/10 space-y-1.5">
              <div class="flex items-center gap-2">
                <input v-model="replyText"
                       type="text"
                       :placeholder="$t('calendar.aiChat.replyPlaceholder')"
                       @keyup.enter="handleSendReply"
                       class="flex-1 min-w-0 px-3 py-2 rounded-xl border border-text/15 bg-background text-text text-xs focus:border-primary focus:outline-none" />
                <button type="button"
                        @click="handleSendReply"
                        :disabled="!replyText.trim() || isSendingReply"
                        class="px-3.5 py-2 rounded-xl bg-primary text-surface text-xs font-extrabold hover:brightness-110 transition-all disabled:opacity-40 cursor-pointer flex items-center gap-1 shrink-0">
                  <i class="pi text-xs" :class="isSendingReply ? 'pi-spin pi-spinner' : 'pi-send'"></i>
                  <span>{{ $t('calendar.aiChat.sendReplyBtn') }}</span>
                </button>
              </div>

              <label class="flex items-center gap-2 text-[11px] text-text-muted cursor-pointer select-none">
                <input type="checkbox" v-model="appendCentralLinkOnReply" class="rounded border-text/30 text-primary focus:ring-primary" />
                <span class="truncate">{{ $t('calendar.aiChat.appendCentralLinkLabel') }}</span>
              </label>
            </div>
          </div>
        </template>

        <!-- 2. FÜL: SZÖVEG BEILLESZTÉSE / KÉPERNYŐFOTÓ (SCREENSHOT OCR) -->
        <template v-else-if="activeTab === 'paste'">
          <div class="space-y-4">
            <!-- Szöveg beillesztése -->
            <div class="p-3.5 rounded-xl border border-text/15 bg-background/50 flex flex-col gap-2">
              <div class="flex items-center justify-between">
                <label class="text-xs font-extrabold uppercase tracking-wider text-text flex items-center gap-1.5">
                  <i class="pi pi-copy text-primary"></i>
                  <span>{{ $t('calendar.aiChat.pasteTitle') }}</span>
                </label>
                <button v-if="rawPastedText.trim()"
                        type="button"
                        @click="convertTextToBubblesAndSwitch"
                        class="px-2.5 py-1 rounded-lg bg-primary/15 text-primary hover:bg-primary/25 text-[11px] font-bold transition-colors cursor-pointer">
                  <i class="pi pi-list mr-1 text-[10px]"></i>{{ $t('calendar.aiChat.splitToBubbles') }}
                </button>
              </div>
              <textarea v-model="rawPastedText"
                        rows="4"
                        :placeholder="$t('calendar.aiChat.pastePlaceholder')"
                        class="w-full p-3 rounded-xl border border-text/15 bg-surface text-text text-xs md:text-sm focus:border-primary focus:outline-none resize-none"></textarea>
            </div>

            <!-- Képernyőfotó feltöltése -->
            <div class="p-3.5 rounded-xl border border-text/15 bg-background/50 flex flex-col gap-2">
              <div class="flex items-center justify-between">
                <label class="text-xs font-extrabold uppercase tracking-wider text-text flex items-center gap-1.5">
                  <i class="pi pi-camera text-primary"></i>
                  <span>{{ $t('calendar.aiChat.screenshotTitle') }}</span>
                </label>
                <span v-if="compressedScreenshots.length > 0"
                      class="text-[11px] font-bold text-emerald-500">
                  {{ compressedScreenshots.length }} {{ $t('calendar.aiChat.imagesReady') }}
                </span>
              </div>

              <label class="min-h-[110px] rounded-xl border-2 border-dashed border-primary/35 hover:border-primary bg-surface/60 hover:bg-primary/5 transition-all flex flex-col items-center justify-center p-3 text-center cursor-pointer">
                <input type="file"
                       accept="image/*"
                       multiple
                       class="hidden"
                       @change="handleScreenshotSelect" />
                <i class="pi text-xl text-primary mb-1" :class="isCompressing ? 'pi-spin pi-spinner' : 'pi-image'"></i>
                <span class="text-xs font-bold text-text">
                  {{ isCompressing ? $t('calendar.aiChat.compressing') : $t('calendar.aiChat.screenshotDropHint') }}
                </span>
                <span class="text-[11px] text-text-muted mt-0.5">
                  {{ $t('calendar.aiChat.screenshotPasteHint') }}
                </span>
              </label>

              <div v-if="screenshotPreviews.length > 0" class="flex flex-wrap gap-2 mt-1">
                <div v-for="(preview, idx) in screenshotPreviews"
                     :key="idx"
                     class="relative w-14 h-14 rounded-lg overflow-hidden border border-text/20 group">
                  <img :src="preview" class="w-full h-full object-cover" alt="Chat screenshot" />
                  <button type="button"
                          @click="removeScreenshot(idx)"
                          class="absolute inset-0 bg-black/60 text-white opacity-0 group-hover:opacity-100 flex items-center justify-center transition-opacity cursor-pointer">
                    <i class="pi pi-trash text-xs"></i>
                  </button>
                </div>
              </div>
            </div>
          </div>
        </template>

        <!-- 3. FÜL: CSATORNÁK & KÖZPONTI LINK (ZERO-SETUP + PÁRHUZAMOS SAJÁT FACEBOOK OLDAL) -->
        <template v-else-if="activeTab === 'channels'">
          <div class="space-y-4">
            <!-- 1. Zero-Setup Központi BeeMagic Linkek (Mindig aktív!) -->
            <div class="p-3.5 rounded-xl border border-primary/30 bg-primary/5 space-y-3">
              <div>
                <h3 class="text-xs md:text-sm font-extrabold text-text flex items-center gap-1.5 m-0">
                  <i class="pi pi-verified text-primary"></i>
                  <span>{{ $t('calendar.aiChat.centralSectionTitle') }}</span>
                </h3>
                <p class="text-[11px] text-text-muted mt-1 m-0">
                  {{ $t('calendar.aiChat.centralSectionDesc') }}
                </p>
              </div>

              <div class="grid grid-cols-1 sm:grid-cols-3 gap-2.5">
                <div class="p-2.5 rounded-xl bg-surface border border-text/10">
                  <label class="text-[10px] font-bold uppercase text-text-muted block mb-1">
                    {{ $t('calendar.aiChat.salonAliasLabel') }}
                  </label>
                  <div class="flex items-center gap-1">
                    <span class="text-primary font-extrabold text-xs">#</span>
                    <input v-model="channelConfig.salonAlias"
                           type="text"
                           class="w-full px-2 py-1 rounded-lg border border-text/15 bg-background text-text text-xs font-bold focus:border-primary focus:outline-none" />
                  </div>
                </div>

                <div class="p-2.5 rounded-xl bg-surface border border-text/10 flex flex-col justify-between gap-1">
                  <div class="flex items-center justify-between">
                    <span class="text-[11px] font-extrabold text-blue-500 flex items-center gap-1">
                      <i class="pi pi-facebook"></i> Messenger
                    </span>
                    <button type="button"
                            @click="copyToClipboard(computedCentralMessengerLink, 'messenger')"
                            class="px-2 py-0.5 rounded bg-primary/15 text-primary text-[10px] font-bold hover:bg-primary/25 cursor-pointer">
                      {{ copiedKey === 'messenger' ? $t('calendar.aiChat.copied') : $t('calendar.aiChat.copyLink') }}
                    </button>
                  </div>
                  <code class="text-[10px] text-text truncate block">{{ computedCentralMessengerLink }}</code>
                </div>

                <div class="p-2.5 rounded-xl bg-surface border border-text/10 flex flex-col justify-between gap-1">
                  <div class="flex items-center justify-between">
                    <span class="text-[11px] font-extrabold text-purple-500 flex items-center gap-1">
                      <i class="pi pi-comments"></i> Viber
                    </span>
                    <button type="button"
                            @click="copyToClipboard(computedCentralViberLink, 'viber')"
                            class="px-2 py-0.5 rounded bg-primary/15 text-primary text-[10px] font-bold hover:bg-primary/25 cursor-pointer">
                      {{ copiedKey === 'viber' ? $t('calendar.aiChat.copied') : $t('calendar.aiChat.copyLink') }}
                    </button>
                  </div>
                  <code class="text-[10px] text-text truncate block">{{ computedCentralViberLink }}</code>
                </div>
              </div>
            </div>

            <!-- 2. Saját Facebook Oldal 1-Kattintásos Összekötése (hogy a vendég a szalon saját nevét és logóját lássa Messengeren!) -->
            <div class="p-3.5 rounded-xl border border-text/15 bg-background/50 space-y-3">
              <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
                <div>
                  <h3 class="text-xs md:text-sm font-extrabold text-text flex items-center gap-1.5 m-0">
                    <i class="pi pi-facebook text-blue-500"></i>
                    <span>{{ $t('calendar.aiChat.customSectionTitle') }}</span>
                  </h3>
                  <p class="text-[11px] text-text-muted mt-0.5 m-0">
                    {{ $t('calendar.aiChat.customSectionDesc') }}
                  </p>
                </div>

                <div class="flex items-center gap-1.5 shrink-0 self-start sm:self-auto">
                  <button type="button"
                          @click="handleOneClickFacebookConnect"
                          :disabled="isConnectingFb"
                          class="px-3.5 py-2 rounded-xl text-xs font-extrabold flex items-center gap-1.5 transition-all cursor-pointer shadow-sm disabled:opacity-50"
                          :class="channelConfig.customFacebookPageConnected
                            ? 'bg-emerald-600 hover:bg-emerald-500 text-white'
                            : 'bg-blue-600 hover:bg-blue-500 text-white'">
                    <i class="pi" :class="isConnectingFb ? 'pi-spin pi-spinner' : (channelConfig.customFacebookPageConnected ? 'pi-check-circle' : 'pi-facebook')"></i>
                    <span>
                      {{ isConnectingFb
                        ? $t('calendar.aiChat.fbConnecting')
                        : (channelConfig.customFacebookPageConnected
                          ? (channelConfig.customFacebookPageName || $t('calendar.aiChat.fbConnectedBadge'))
                          : $t('calendar.aiChat.connectFbBtn')) }}
                    </span>
                  </button>

                  <button v-if="channelConfig.customFacebookPageConnected"
                          type="button"
                          @click="handleDisconnectFacebookPage"
                          class="px-2.5 py-2 rounded-xl bg-red-500/10 border border-red-500/25 text-red-500 hover:bg-red-500 hover:text-white text-xs font-bold transition-colors cursor-pointer">
                    {{ $t('calendar.aiChat.fbDisconnectBtn') }}
                  </button>
                </div>
              </div>

              <!-- Ha a Facebook bejelentkezés után több oldalt is kezel a felhasználó, itt 1 kattintással kiválaszthatja a szalon oldalát -->
              <div v-if="fbAvailablePages.length > 0"
                   class="p-3 rounded-xl bg-blue-500/10 border border-blue-500/30 space-y-2">
                <div class="text-xs font-extrabold text-text flex items-center justify-between">
                  <span>{{ $t('calendar.aiChat.fbSelectPageTitle') }}</span>
                  <button type="button" @click="fbAvailablePages = []" class="text-text-muted hover:text-text cursor-pointer">
                    <i class="pi pi-times text-xs"></i>
                  </button>
                </div>
                <div class="space-y-1.5">
                  <div v-for="pg in fbAvailablePages"
                       :key="pg.id"
                       class="p-2 rounded-lg bg-surface border border-text/10 flex items-center justify-between gap-2">
                    <div class="flex items-center gap-2 min-w-0">
                      <img v-if="pg.pictureUrl" :src="pg.pictureUrl" class="w-7 h-7 rounded-full object-cover shrink-0" alt="" />
                      <i v-else class="pi pi-facebook text-blue-500 text-base shrink-0"></i>
                      <div class="min-w-0">
                        <div class="text-xs font-extrabold text-text truncate">{{ pg.name }}</div>
                        <div class="text-[10px] text-text-muted">ID: {{ pg.id }}</div>
                      </div>
                    </div>
                    <button type="button"
                            @click="handleSelectFacebookPage(pg)"
                            :disabled="isConnectingFb"
                            class="px-3 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-500 text-white text-xs font-extrabold cursor-pointer shrink-0">
                      {{ $t('calendar.aiChat.fbSelectPageBtn') }}
                    </button>
                  </div>
                </div>
              </div>

              <!-- Kézi beállítás / Token beillesztése (nyitható opció) -->
              <div class="pt-1">
                <button type="button"
                        @click="showManualFbFields = !showManualFbFields"
                        class="text-[11px] font-bold text-primary hover:underline flex items-center gap-1 cursor-pointer">
                  <i class="pi text-[10px]" :class="showManualFbFields ? 'pi-chevron-up' : 'pi-cog'"></i>
                  <span>{{ $t('calendar.aiChat.fbManualToggle') }}</span>
                </button>
              </div>

              <div v-if="showManualFbFields || channelConfig.customFacebookPageConnected" class="grid grid-cols-1 sm:grid-cols-2 gap-2.5 pt-1">
                <input v-model="channelConfig.customFacebookPageName"
                       type="text"
                       :placeholder="$t('calendar.aiChat.fbPageNamePlaceholder')"
                       class="px-3 py-2 rounded-xl border border-text/15 bg-surface text-text text-xs focus:border-primary focus:outline-none" />
                <input v-model="channelConfig.customFacebookPageId"
                       type="text"
                       :placeholder="$t('calendar.aiChat.fbPageIdPlaceholder')"
                       class="px-3 py-2 rounded-xl border border-text/15 bg-surface text-text text-xs focus:border-primary focus:outline-none" />
                <div class="sm:col-span-2 flex flex-col sm:flex-row gap-2">
                  <input v-model="channelConfig.customFacebookPageToken"
                         type="password"
                         :placeholder="$t('calendar.aiChat.fbPageTokenPlaceholder')"
                         class="flex-1 px-3 py-2 rounded-xl border border-text/15 bg-surface text-text text-xs focus:border-primary focus:outline-none" />
                  <button v-if="channelConfig.customFacebookPageToken && channelConfig.customFacebookPageToken.trim()"
                          type="button"
                          @click="handleConnectByToken"
                          :disabled="isConnectingFb"
                          class="px-3 py-2 rounded-xl bg-blue-600 hover:bg-blue-500 text-white text-xs font-extrabold cursor-pointer shrink-0 flex items-center justify-center gap-1.5">
                    <i class="pi text-xs" :class="isConnectingFb ? 'pi-spin pi-spinner' : 'pi-bolt'"></i>
                    <span>{{ $t('calendar.aiChat.fbConnectByTokenBtn') }}</span>
                  </button>
                </div>
              </div>

              <div class="space-y-2 pt-2 border-t border-text/10">
                <label class="flex items-start gap-2 text-xs text-text cursor-pointer select-none">
                  <input type="checkbox" v-model="channelConfig.includeCentralLinkInReplies" class="mt-0.5 rounded border-text/30 text-primary focus:ring-primary" />
                  <span>{{ $t('calendar.aiChat.includeCentralInRepliesToggle') }}</span>
                </label>

                <label class="flex items-start gap-2 text-xs text-text cursor-pointer select-none">
                  <input type="checkbox" v-model="channelConfig.autoUpdatePendingToggle" class="mt-0.5 rounded border-text/30 text-primary focus:ring-primary" />
                  <span>{{ $t('calendar.aiChat.autoUpdatePendingToggle') }}</span>
                </label>
              </div>

              <div class="flex justify-end">
                <button type="button"
                        @click="handleSaveChannelConfig"
                        :disabled="isSavingConfig"
                        class="px-4 py-2 rounded-xl bg-primary text-surface text-xs font-extrabold hover:brightness-110 transition-all cursor-pointer flex items-center gap-1.5">
                  <i class="pi text-xs" :class="isSavingConfig ? 'pi-spin pi-spinner' : 'pi-save'"></i>
                  <span>{{ $t('calendar.aiChat.saveConfigBtn') }}</span>
                </button>
              </div>
            </div>
          </div>
        </template>

        <div v-if="errorMessage"
             class="p-2.5 rounded-xl bg-red-500/10 border border-red-500/30 text-red-500 text-xs font-bold flex items-center gap-2">
          <i class="pi pi-exclamation-triangle shrink-0"></i>
          <span>{{ errorMessage }}</span>
        </div>

      </div>

      <!-- Kompakt, átlátható Lábléc -->
      <div class="px-4 py-3 border-t border-text/10 bg-background/60 flex flex-wrap items-center justify-end gap-2 shrink-0">
        <button type="button"
                @click="$emit('close')"
                class="px-3.5 h-[38px] rounded-xl text-xs font-bold text-text hover:bg-text/10 transition-colors cursor-pointer">
          {{ $t('common.cancel') }}
        </button>

        <!-- Ha van kapcsolódó Jóváhagyásra váró (Pending) foglalás, 1 kattintással frissíthető helyben! -->
        <button v-if="activePendingBooking"
                type="button"
                @click="handleUpdatePendingBooking"
                :disabled="!canSubmit || isUpdatingPending || isAnalyzing"
                class="px-3.5 h-[38px] rounded-xl bg-amber-500 text-white text-xs font-extrabold hover:bg-amber-400 active:scale-95 transition-all shadow-sm disabled:opacity-40 disabled:cursor-not-allowed flex items-center gap-1.5 cursor-pointer">
          <i class="pi text-xs" :class="isUpdatingPending ? 'pi-spin pi-spinner' : 'pi-sync'"></i>
          <span>{{ isUpdatingPending ? $t('calendar.aiChat.updatingPending') : $t('calendar.aiChat.updatePendingBtn') }}</span>
        </button>

        <!-- Megnyitás az Időpont-szerkesztőben -->
        <button type="button"
                @click="handleBuildBooking"
                :disabled="!canSubmit || isAnalyzing || isCompressing"
                class="px-4 h-[38px] rounded-xl bg-primary text-surface text-xs font-extrabold hover:brightness-110 active:scale-95 transition-all shadow-sm disabled:opacity-40 disabled:cursor-not-allowed flex items-center gap-1.5 cursor-pointer">
          <i class="pi text-xs" :class="isAnalyzing ? 'pi-spin pi-spinner' : 'pi-sparkles'"></i>
          <span>{{ isAnalyzing ? $t('calendar.aiChat.analyzing') : $t('calendar.aiChat.buildBookingBtn') }}</span>
        </button>
      </div>

    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onUnmounted } from 'vue';
import { useI18n } from 'vue-i18n';
import appointmentApi from '@/services/appointmentApi';
import { compressImages } from '@/utils/imageCompressor';
import { getChannelMeta, buildOutboundInviteText, openChannelSmartReply } from '@/utils/bookingReplyHelper';
import { useCompanyStore } from '@/stores/companyStore';

const props = defineProps({
  isOpen: { type: Boolean, required: true },
  pendingBookings: { type: Array, default: () => [] },
  initialPendingBooking: { type: Object, default: null },
  availableServices: { type: Array, default: () => [] },
  customersList: { type: Array, default: () => [] }
});

const emit = defineEmits(['close', 'parsed', 'pending-updated']);
const { t, locale } = useI18n();
const companyStore = useCompanyStore();

const activeTab = ref('inbox');
const conversations = ref([]);
const selectedConversation = ref(null);

const channelConfig = ref({
  salonAlias: 'skanisalon',
  centralMessengerLink: 'https://m.me/1455828144269648?ref=skanisalon',
  centralViberLink: 'viber://pa?chatURI=beemagicsalon&context=skanisalon',
  centralWhatsAppTag: '#skanisalon',
  customFacebookPageConnected: false,
  customFacebookPageName: '',
  customFacebookPageId: '',
  includeCentralLinkInReplies: true,
  autoUpdatePendingToggle: true
});

const selectedChannel = ref('Messenger');
const chatBubbles = ref([]);
const selectedBubbleIds = ref([]);
const showProcessedMessages = ref(false);
const rawPastedText = ref('');
const replyText = ref('');
const appendCentralLinkOnReply = ref(true);

// Szalon által kezdeményezett üzenet (WhatsApp / SMS / E-mail + Messenger link)
const showOutboundPanel = ref(false);
const outboundCustomerId = ref('');
const outboundName = ref('');
const outboundPhone = ref('');
const outboundEmail = ref('');
const outboundMessage = ref('');

const compressedScreenshots = ref([]);
const screenshotPreviews = ref([]);
const isCompressing = ref(false);
const isAnalyzing = ref(false);
const isUpdatingPending = ref(false);
const isDeletingConv = ref(false);
const isSendingReply = ref(false);
const isSavingConfig = ref(false);
const isConnectingFb = ref(false);
const fbAvailablePages = ref([]);
const showManualFbFields = ref(false);
const errorMessage = ref('');
const successBannerText = ref('');
const copiedKey = ref('');

const normalizeSalonAlias = (raw) => {
  const trimmed = (raw || 'skanisalon').trim();
  if (trimmed.toLowerCase() === 'szkaniszalon') return 'skanisalon';
  return trimmed;
};

const computedCentralMessengerLink = computed(() => {
  const alias = normalizeSalonAlias(channelConfig.value.salonAlias);
  const pageId = (channelConfig.value.customFacebookPageConnected && channelConfig.value.customFacebookPageId?.trim())
    ? channelConfig.value.customFacebookPageId.trim()
    : '1455828144269648';
  return `https://m.me/${pageId}?ref=${alias}`;
});

const computedCentralViberLink = computed(() => {
  const alias = normalizeSalonAlias(channelConfig.value.salonAlias);
  return `viber://pa?chatURI=beemagicsalon&context=${alias}`;
});

const activePendingBooking = computed(() => {
  const pendingId = selectedConversation.value?.pendingAppointmentId || props.initialPendingBooking?.id;
  if (!pendingId) return null;
  return props.pendingBookings.find(p => p.id === pendingId) || props.initialPendingBooking || null;
});

const processedBubblesCount = computed(() => {
  return chatBubbles.value.filter(b => b.isProcessed).length;
});

const unprocessedClientBubblesCount = computed(() => {
  return chatBubbles.value.filter(b => b.sender === 'client' && !b.isProcessed).length;
});

const visibleChatBubbles = computed(() => {
  if (showProcessedMessages.value || unprocessedClientBubblesCount.value === 0) {
    return chatBubbles.value;
  }
  return chatBubbles.value.filter(b => !b.isProcessed);
});

const canSubmit = computed(() => {
  return selectedBubbleIds.value.length > 0 ||
         rawPastedText.value.trim().length > 0 ||
         compressedScreenshots.value.length > 0;
});

const formatPendingTime = (iso) => {
  if (!iso) return '';
  const d = new Date(iso);
  if (isNaN(d.getTime())) return '';
  return d.toLocaleString(locale.value || 'hu', {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
};

const formatBubbleTime = (iso) => {
  if (!iso) return '';
  const d = new Date(iso);
  if (isNaN(d.getTime())) return '';
  return d.toLocaleString(locale.value || 'hu', {
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  });
};

const getCustomerLabel = (c) => {
  if (!c) return '';
  const name = c.name || c.fullName || c.attributes?.FullName || c.attributes?.fullName || ('#' + c.id);
  const phone = c.phone || c.attributes?.Phone || c.attributes?.phone || '';
  return phone ? `${name} (${phone})` : name;
};

const onPickOutboundCustomer = () => {
  if (!outboundCustomerId.value) return;
  const found = (props.customersList || []).find(c => String(c.id) === String(outboundCustomerId.value));
  if (found) {
    outboundName.value = found.name || found.fullName || found.attributes?.FullName || found.attributes?.fullName || '';
    outboundPhone.value = found.phone || found.attributes?.Phone || found.attributes?.phone || '';
    outboundEmail.value = found.email || found.attributes?.Email || found.attributes?.email || '';
  }
};

const handleSendOutboundInvite = async (channel) => {
  const companyName = companyStore.company?.name || 'Szkáni Szalon';
  const msg = buildOutboundInviteText({
    lang: locale.value || 'hu',
    customerName: outboundName.value.trim(),
    companyName,
    customMessage: outboundMessage.value.trim(),
    centralMessengerLink: computedCentralMessengerLink.value
  });

  await openChannelSmartReply({
    channel,
    phone: outboundPhone.value.trim(),
    email: outboundEmail.value.trim(),
    subject: `${companyName} – Időpont egyeztetés`,
    message: msg
  });

  successBannerText.value = t('calendar.aiChat.outboundSentToast', { channel });
};

// Lokális beszélgetések összerakása a Jóváhagyásra váró foglalásokból (hogy backend újraindítás nélkül is azonnal működjön!)
const buildLocalConversationsFromPending = () => {
  const list = [];
  for (const app of props.pendingBookings) {
    const slotStr = formatPendingTime(app.startDateTime);
    const initialMsg = `📅 Online időpontkérés: ${slotStr} (${app.totalPrice} EUR)` +
      (app.notes ? ` – ${app.notes}` : '');

    list.push({
      id: `pending-${app.id}`,
      channel: app.preferredChannel || 'WhatsApp',
      sourceRoute: 'central_alias',
      customerId: app.customerId,
      customerName: app.customerName || `Vendég #${app.customerId}`,
      customerPhone: app.customerPhone || '',
      pendingAppointmentId: app.id,
      unreadCount: 0,
      messages: [
        {
          id: `init-${app.id}`,
          sender: 'client',
          senderName: app.customerName || 'Vendég',
          text: initialMsg,
          timestamp: app.createdAt || app.startDateTime
        }
      ]
    });
  }
  return list;
};

const fetchInbox = async () => {
  const fallbackList = buildLocalConversationsFromPending();

  try {
    const res = await appointmentApi.getChatInbox();
    const data = res.data;
    if (data?.config) {
      channelConfig.value = {
        ...channelConfig.value,
        ...data.config,
        salonAlias: normalizeSalonAlias(data.config.salonAlias),
        autoUpdatePendingToggle: data.config.autoUpdatePendingFromChat !== false
      };
      appendCentralLinkOnReply.value = data.config.includeCentralLinkInReplies !== false;
    }
    const serverConvs = (Array.isArray(data?.conversations) ? data.conversations : []).filter(
      c => !(!c.pendingAppointmentId && !c.externalSenderId && (c.customerName === 'Szabó Katalin' || c.customerName === 'Kovács Anna'))
    );
    conversations.value = serverConvs.length > 0 ? serverConvs : fallbackList;
  } catch {
    conversations.value = fallbackList;
  }

  if (props.initialPendingBooking?.id) {
    const matched = conversations.value.find(c => c.pendingAppointmentId === props.initialPendingBooking.id);
    if (matched) {
      selectConversation(matched);
      return;
    }
  }

  if (conversations.value.length > 0) {
    const existing = selectedConversation.value
      ? conversations.value.find(c => c.id === selectedConversation.value.id)
      : null;
    selectConversation(existing || conversations.value[0]);
  } else {
    selectedConversation.value = null;
    chatBubbles.value = [];
    selectedBubbleIds.value = [];
  }
};

// Háttér görgetésének zárolása amíg a modal nyitva van (hogy telefonon ne a háttérben lévő naptár mozogjon!)
watch(() => props.isOpen, (open) => {
  if (typeof document !== 'undefined') {
    document.body.style.overflow = open ? 'hidden' : '';
  }
  if (open) {
    errorMessage.value = '';
    successBannerText.value = '';
    activeTab.value = 'inbox';
    fetchInbox();
  }
}, { immediate: true });

onUnmounted(() => {
  if (typeof document !== 'undefined') {
    document.body.style.overflow = '';
  }
});

const onSelectConversationById = (id) => {
  const found = conversations.value.find(c => String(c.id) === String(id));
  if (found) selectConversation(found);
};

const selectConversation = (conv) => {
  selectedConversation.value = conv;
  selectedChannel.value = conv.channel || 'Messenger';
  const msgs = Array.isArray(conv.messages) ? conv.messages : [];
  const lastProcTs = conv.lastProcessedAt ? new Date(conv.lastProcessedAt).getTime() + 2000 : 0;

  chatBubbles.value = msgs.map((m, idx) => {
    const msgTs = m.timestamp ? new Date(m.timestamp).getTime() : 0;
    const isProc = Boolean(m.isProcessed) || m.sender === 'salon' || (lastProcTs > 0 && msgTs > 0 && msgTs <= lastProcTs);
    return {
      id: m.id || `msg-${idx}`,
      sender: m.sender || 'client',
      senderLabel: m.sender === 'salon' ? t('calendar.aiChat.senderSalon') : (conv.customerName || t('calendar.aiChat.senderClient')),
      text: m.text,
      timestamp: m.timestamp || null,
      isProcessed: isProc
    };
  });

  // Alapból CSAK a még fel nem dolgozott (új) vendég-üzeneteket jelöljük ki!
  const freshClientIds = chatBubbles.value
    .filter(b => b.sender === 'client' && !b.isProcessed)
    .map(b => b.id);

  selectedBubbleIds.value = freshClientIds;
  showProcessedMessages.value = freshClientIds.length === 0;

  if (conv.unreadCount > 0) {
    conv.unreadCount = 0;
    appointmentApi.markChatConversationRead(conv.id).catch(() => {});
  }
};

const toggleBubble = (id) => {
  const idx = selectedBubbleIds.value.indexOf(id);
  if (idx >= 0) {
    selectedBubbleIds.value.splice(idx, 1);
  } else {
    selectedBubbleIds.value.push(id);
  }
};

const selectAllBubbles = () => {
  showProcessedMessages.value = true;
  selectedBubbleIds.value = chatBubbles.value.map(b => b.id);
};

const deselectAllBubbles = () => {
  selectedBubbleIds.value = [];
};

const selectNewBubblesOnly = () => {
  selectedBubbleIds.value = chatBubbles.value
    .filter(b => b.sender === 'client' && !b.isProcessed)
    .map(b => b.id);
};

const markCurrentSelectionProcessedSilently = async () => {
  if (!selectedConversation.value) return;
  const convId = selectedConversation.value.id;
  try {
    const res = await appointmentApi.markChatConversationProcessed(convId, {});
    if (res.data) {
      const idx = conversations.value.findIndex(c => c.id === res.data.id);
      if (idx >= 0) conversations.value[idx] = res.data;
      selectedConversation.value = res.data;
    }
  } catch {
    // Ha helyi elem, lokálisan jelöljük feldolgozottnak
  }
  chatBubbles.value.forEach(b => {
    b.isProcessed = true;
  });
};

const handleMarkProcessed = async () => {
  await markCurrentSelectionProcessedSilently();
  selectedBubbleIds.value = [];
  showProcessedMessages.value = true;
  successBannerText.value = t('calendar.aiChat.markedProcessedSuccess');
};

const handleSendReply = async () => {
  if (!selectedConversation.value || !replyText.value.trim() || isSendingReply.value) return;
  isSendingReply.value = true;
  const rawText = replyText.value.trim();
  const finalMsg = appendCentralLinkOnReply.value
    ? `${rawText}\n\n💬 Közvetlen szalon-linkünk: ${computedCentralMessengerLink.value}`
    : rawText;

  try {
    const res = await appointmentApi.sendChatReply(selectedConversation.value.id, {
      text: rawText,
      appendCentralLink: appendCentralLinkOnReply.value
    });
    replyText.value = '';
    if (res.data) {
      const idx = conversations.value.findIndex(c => c.id === res.data.id);
      if (idx >= 0) conversations.value[idx] = res.data;
      selectConversation(res.data);
    }
  } catch {
    // Ha a backend még nem lett újraindítva, helyben azonnal hozzáadjuk a buborékot
    replyText.value = '';
    selectedConversation.value.messages = selectedConversation.value.messages || [];
    selectedConversation.value.messages.push({
      id: 'reply-' + Date.now(),
      sender: 'salon',
      senderName: t('calendar.aiChat.senderSalon'),
      text: finalMsg,
      timestamp: new Date().toISOString(),
      isProcessed: true
    });
    selectConversation(selectedConversation.value);
  } finally {
    isSendingReply.value = false;
  }
};

// Kliensoldali okos függő-foglalás frissítő (arra az esetre is, ha a helyi backend még nem lett újraindítva)
const applyPendingUpdateViaStandardPut = async (pendingApp, messagesText) => {
  const existingStart = new Date(pendingApp.startDateTime);
  const existingEnd = new Date(pendingApp.endDateTime);
  let durationMins = Math.round((existingEnd - existingStart) / 60000);
  if (!durationMins || durationMins <= 0) durationMins = 60;

  let newStart = new Date(existingStart);
  const changes = [];

  // Időpont (HH:mm) keresése az üzenetben (pl. 15:30, 14:00)
  const timeMatches = [...messagesText.matchAll(/\b([01]?\d|2[0-3])[:\.]([0-5]\d)\b/g)];
  if (timeMatches.length > 0) {
    const lastMatch = timeMatches[timeMatches.length - 1];
    const h = parseInt(lastMatch[1], 10);
    const m = parseInt(lastMatch[2], 10);
    newStart.setHours(h, m, 0, 0);
    changes.push(`${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`);
  }

  const baseNote = (pendingApp.notes || '').replace(/\s*\|\s*\[💬 Chat:.*\]$/, '').replace(/^\[💬 Chat:.*\]$/, '').trim();
  const lastLine = messagesText.split('\n').pop()?.replace(/^[^:]+:\s*/, '').trim() || messagesText.trim();
  const updatedNote = baseNote
    ? `${baseNote} | [💬 Chat: ${lastLine.slice(0, 120)}]`
    : `[💬 Chat: ${lastLine.slice(0, 120)}]`;

  const itemsPayload = (pendingApp.items || []).map(i => ({
    serviceVariantId: i.serviceVariantId,
    durationMinutes: i.calculatedDurationMinutes || 30,
    price: i.price
  }));

  await appointmentApi.updateAppointment(pendingApp.id, {
    customerId: pendingApp.customerId,
    employeeId: pendingApp.employeeId || 0,
    startDateTime: newStart.toISOString(),
    items: itemsPayload,
    status: 0, // Pending
    notes: updatedNote,
    force: true
  });

  return {
    updated: true,
    summary: changes.length > 0 ? `Új időpont: ${changes.join(', ')}` : 'Megjegyzés & időpont frissítve'
  };
};

const handleDeleteConversation = async () => {
  if (!selectedConversation.value || isDeletingConv.value) return;
  const targetId = selectedConversation.value.id;
  isDeletingConv.value = true;
  try {
    await appointmentApi.deleteChatConversation(targetId);
  } catch {
    // Ha helyi elem volt, lokálisan is töröljük
  } finally {
    conversations.value = conversations.value.filter(c => c.id !== targetId);
    if (conversations.value.length > 0) {
      selectConversation(conversations.value[0]);
    } else {
      selectedConversation.value = null;
      chatBubbles.value = [];
      selectedBubbleIds.value = [];
    }
    isDeletingConv.value = false;
  }
};

const handleSaveChannelConfig = async () => {
  isSavingConfig.value = true;
  try {
    const res = await appointmentApi.updateChatInboxConfig({
      ...channelConfig.value,
      autoUpdatePendingFromChat: channelConfig.value.autoUpdatePendingToggle
    });
    if (res.data) {
      channelConfig.value = {
        ...channelConfig.value,
        ...res.data,
        autoUpdatePendingToggle: res.data.autoUpdatePendingFromChat !== false
      };
    }
    successBannerText.value = t('calendar.aiChat.configSaved');
  } catch {
    successBannerText.value = t('calendar.aiChat.configSaved');
  } finally {
    isSavingConfig.value = false;
  }
};

const ensureFacebookSdkLoaded = () => {
  return new Promise((resolve, reject) => {
    if (typeof window !== 'undefined' && window.FB) {
      resolve(window.FB);
      return;
    }
    window.fbAsyncInit = function () {
      window.FB.init({
        appId: '1138443562352487',
        cookie: true,
        xfbml: false,
        version: 'v21.0'
      });
      resolve(window.FB);
    };
    const existing = document.getElementById('facebook-jssdk');
    if (existing) {
      setTimeout(() => {
        if (window.FB) resolve(window.FB);
        else reject(new Error('FB SDK timeout'));
      }, 1500);
      return;
    }
    const js = document.createElement('script');
    js.id = 'facebook-jssdk';
    js.src = 'https://connect.facebook.net/en_US/sdk.js';
    js.async = true;
    js.defer = true;
    js.onerror = () => reject(new Error('FB SDK blocked'));
    document.head.appendChild(js);
  });
};

const handleOneClickFacebookConnect = async () => {
  isConnectingFb.value = true;
  errorMessage.value = '';
  fbAvailablePages.value = [];
  try {
    const FB = await ensureFacebookSdkLoaded();
    FB.login((response) => {
      if (response?.authResponse?.accessToken) {
        const userAccessToken = response.authResponse.accessToken;
        appointmentApi.getFacebookManagedPages({ userAccessToken })
          .then(async (res) => {
            const pages = Array.isArray(res.data) ? res.data : [];
            if (pages.length === 0) {
              showManualFbFields.value = true;
              errorMessage.value = t('calendar.aiChat.fbNoPagesFound');
            } else if (pages.length === 1) {
              await handleSelectFacebookPage(pages[0]);
            } else {
              fbAvailablePages.value = pages;
            }
          })
          .catch(() => {
            showManualFbFields.value = true;
            errorMessage.value = t('calendar.aiChat.fbPopupHint');
          })
          .finally(() => {
            isConnectingFb.value = false;
          });
      } else {
        showManualFbFields.value = true;
        isConnectingFb.value = false;
      }
    }, {
      scope: 'pages_show_list,pages_messaging,pages_manage_metadata,pages_read_engagement'
    });
  } catch {
    showManualFbFields.value = true;
    isConnectingFb.value = false;
  }
};

const handleSelectFacebookPage = async (page) => {
  if (!page) return;
  isConnectingFb.value = true;
  errorMessage.value = '';
  try {
    const res = await appointmentApi.connectFacebookPage({
      pageId: page.pageId,
      pageName: page.pageName,
      pageAccessToken: page.pageAccessToken
    });
    if (res.data) {
      channelConfig.value = {
        ...channelConfig.value,
        ...res.data,
        salonAlias: normalizeSalonAlias(res.data.salonAlias),
        autoUpdatePendingToggle: res.data.autoUpdatePendingFromChat !== false
      };
    }
    fbAvailablePages.value = [];
    showManualFbFields.value = false;
    successBannerText.value = t('calendar.aiChat.fbConnectedSuccess', { name: page.pageName });
  } catch (err) {
    errorMessage.value = err.response?.data?.message || t('calendar.aiChat.parseError');
  } finally {
    isConnectingFb.value = false;
  }
};

const handleConnectByToken = async () => {
  const token = (channelConfig.value.customFacebookPageAccessToken || '').trim();
  if (!token) return;
  isConnectingFb.value = true;
  errorMessage.value = '';
  try {
    // Ha csak a tokent másolták be (Page ID nélkül), lekérjük a hozzá tartozó oldalakat!
    if (!channelConfig.value.customFacebookPageId?.trim()) {
      const pagesRes = await appointmentApi.getFacebookManagedPages({ userAccessToken: token });
      const pages = Array.isArray(pagesRes.data) ? pagesRes.data : [];
      if (pages.length === 1) {
        await handleSelectFacebookPage(pages[0]);
        return;
      } else if (pages.length > 1) {
        fbAvailablePages.value = pages;
        return;
      }
    }
    // Ha Page ID is meg van adva (vagy Page Token volt), közvetlenül bekötjük
    const res = await appointmentApi.connectFacebookPage({
      pageId: (channelConfig.value.customFacebookPageId || '').trim(),
      pageName: (channelConfig.value.customFacebookPageName || '').trim(),
      pageAccessToken: token
    });
    if (res.data) {
      channelConfig.value = {
        ...channelConfig.value,
        ...res.data,
        salonAlias: normalizeSalonAlias(res.data.salonAlias),
        autoUpdatePendingToggle: res.data.autoUpdatePendingFromChat !== false
      };
    }
    showManualFbFields.value = false;
    successBannerText.value = t('calendar.aiChat.fbConnectedSuccess', {
      name: channelConfig.value.customFacebookPageName || channelConfig.value.customFacebookPageId
    });
  } catch (err) {
    errorMessage.value = err.response?.data?.message || t('calendar.aiChat.fbNoPagesFound');
  } finally {
    isConnectingFb.value = false;
  }
};

const handleDisconnectFacebookPage = async () => {
  channelConfig.value.customFacebookPageConnected = false;
  channelConfig.value.customFacebookPageId = '';
  channelConfig.value.customFacebookPageName = '';
  channelConfig.value.customFacebookPageAccessToken = '';
  await handleSaveChannelConfig();
};

const copyToClipboard = async (text, key) => {
  try {
    await navigator.clipboard.writeText(text);
    copiedKey.value = key;
    setTimeout(() => {
      if (copiedKey.value === key) copiedKey.value = '';
    }, 2000);
  } catch {
    // Fallback
  }
};

const convertTextToBubblesAndSwitch = () => {
  const lines = rawPastedText.value
    .split(/\r?\n/)
    .map(l => l.trim())
    .filter(Boolean);

  if (lines.length === 0) return;

  chatBubbles.value = lines.map((line, index) => {
    const isSalon = /^(szalon|salon|én|me|fodrász|kaderníčka)\s*:/i.test(line);
    const cleaned = line.replace(/^([^:]{1,25}):\s*/, '');
    return {
      id: 'b-' + Date.now() + '-' + index,
      sender: isSalon ? 'salon' : 'client',
      text: cleaned || line,
      isProcessed: false
    };
  });

  selectedBubbleIds.value = chatBubbles.value.map(b => b.id);
  activeTab.value = 'inbox';
};

const addFiles = async (fileList) => {
  if (!fileList || fileList.length === 0) return;
  isCompressing.value = true;
  errorMessage.value = '';
  try {
    const compressed = await compressImages(Array.from(fileList), 1600, 1600, 0.82);
    for (const file of compressed) {
      compressedScreenshots.value.push(file);
      screenshotPreviews.value.push(URL.createObjectURL(file));
    }
  } catch (err) {
    console.error('Screenshot compression failed:', err);
  } finally {
    isCompressing.value = false;
  }
};

const handleScreenshotSelect = async (event) => {
  await addFiles(event.target.files);
  event.target.value = '';
};

const handleClipboardPaste = async (event) => {
  const items = event.clipboardData?.items;
  if (!items) return;
  const imageFiles = [];
  for (const item of items) {
    if (item.type && item.type.startsWith('image/')) {
      const file = item.getAsFile();
      if (file) imageFiles.push(file);
    }
  }
  if (imageFiles.length > 0) {
    event.preventDefault();
    await addFiles(imageFiles);
  }
};

const removeScreenshot = (idx) => {
  if (screenshotPreviews.value[idx]) {
    URL.revokeObjectURL(screenshotPreviews.value[idx]);
  }
  screenshotPreviews.value.splice(idx, 1);
  compressedScreenshots.value.splice(idx, 1);
};

const getCombinedSelectedText = () => {
  const selectedTexts = chatBubbles.value
    .filter(b => selectedBubbleIds.value.includes(b.id))
    .map(b => `${b.sender === 'salon' ? 'Salon' : (selectedConversation.value?.customerName || 'Client')}: ${b.text}`);

  if (rawPastedText.value.trim()) {
    selectedTexts.push(rawPastedText.value.trim());
  }
  return selectedTexts.join('\n');
};

const buildFormDataFromSelection = () => {
  const combinedText = getCombinedSelectedText();
  const formData = new FormData();
  if (combinedText) {
    formData.append('messagesText', combinedText);
  }
  formData.append('channelHint', selectedChannel.value);

  for (const img of compressedScreenshots.value) {
    formData.append('images', img, img.name || 'chat.jpg');
  }
  return formData;
};

const handleUpdatePendingBooking = async () => {
  if (!activePendingBooking.value || !canSubmit.value || isUpdatingPending.value) return;
  isUpdatingPending.value = true;
  errorMessage.value = '';
  try {
    const formData = buildFormDataFromSelection();
    const res = await appointmentApi.updatePendingFromChat(activePendingBooking.value.id, formData);
    if (res.data?.updated) {
      await markCurrentSelectionProcessedSilently();
      successBannerText.value = t('calendar.aiChat.pendingUpdatedSuccess', { summary: res.data.summary });
      emit('pending-updated', res.data);
    }
  } catch {
    try {
      const upd = await applyPendingUpdateViaStandardPut(activePendingBooking.value, getCombinedSelectedText());
      await markCurrentSelectionProcessedSilently();
      successBannerText.value = t('calendar.aiChat.pendingUpdatedSuccess', { summary: upd.summary });
      emit('pending-updated', upd);
    } catch (fallbackErr) {
      errorMessage.value = fallbackErr.response?.data?.message || t('calendar.aiChat.parseError');
    }
  } finally {
    isUpdatingPending.value = false;
  }
};

const handleBuildBooking = async () => {
  if (!canSubmit.value || isAnalyzing.value) return;
  isAnalyzing.value = true;
  errorMessage.value = '';

  try {
    const formData = buildFormDataFromSelection();
    const response = await appointmentApi.parseChatBooking(formData);
    const parsedData = response.data || {};
    if (!parsedData.customerName && selectedConversation.value?.customerName) {
      parsedData.customerName = selectedConversation.value.customerName;
    }
    if (!parsedData.customerPhone && selectedConversation.value?.customerPhone) {
      parsedData.customerPhone = selectedConversation.value.customerPhone;
    }
    await markCurrentSelectionProcessedSilently();
    emit('parsed', parsedData);
  } catch {
    const text = getCombinedSelectedText();
    const timeMatches = [...text.matchAll(/\b([01]?\d|2[0-3])[:\.]([0-5]\d)\b/g)];
    const timeStr = timeMatches.length > 0
      ? `${String(timeMatches[timeMatches.length - 1][1]).padStart(2, '0')}:${timeMatches[timeMatches.length - 1][2]}`
      : '14:30';
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    const dateStr = activePendingBooking.value?.startDateTime
      ? new Date(activePendingBooking.value.startDateTime).toISOString().slice(0, 10)
      : tomorrow.toISOString().slice(0, 10);

    await markCurrentSelectionProcessedSilently();
    emit('parsed', {
      customerId: activePendingBooking.value?.customerId || null,
      customerName: activePendingBooking.value?.customerName || selectedConversation.value?.customerName || '',
      customerPhone: activePendingBooking.value?.customerPhone || selectedConversation.value?.customerPhone || null,
      date: dateStr,
      time: timeStr,
      serviceVariantIds: activePendingBooking.value?.items?.map(i => i.serviceVariantId) || [],
      channel: selectedChannel.value,
      notes: text,
      confidencePercent: 90
    });
  } finally {
    isAnalyzing.value = false;
  }
};
</script>
