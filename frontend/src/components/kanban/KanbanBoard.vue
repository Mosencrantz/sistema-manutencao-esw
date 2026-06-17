<template>
  <div class="kanban-wrapper">
    <div class="kanban-scroll">
      <div
        v-for="col in colunas"
        :key="col.status"
        class="kanban-col"
        :class="{ 'drop-target': dragOver === col.status }"
        @dragover.prevent="dragOver = col.status"
        @dragleave="dragOver = null"
        @drop.prevent="onDrop($event, col.status)"
      >
        <!-- Cabeçalho da coluna -->
        <div class="col-header mb-3">
          <div class="d-flex align-center justify-space-between">
            <div class="d-flex align-center gap-2">
              <div
                class="col-dot"
                :style="`background: rgb(var(--v-theme-${col.color}))`"
              />
              <span class="text-body-2 font-weight-bold">{{ col.label }}</span>
            </div>
            <v-chip size="x-small" variant="tonal" :color="col.color">
              {{ col.ordens.length }}
            </v-chip>
          </div>
        </div>

        <!-- Cards -->
        <div class="col-body">
          <KanbanCard
            v-for="os in col.ordens"
            :key="os.id"
            :os="os"
            :color="col.color"
            @click="$emit('card-click', os)"
            @drag-start="activeDrag = os"
            @drag-end="activeDrag = null; dragOver = null"
          />

          <!-- Empty state -->
          <div
            v-if="!col.ordens.length"
            class="empty-col text-center pa-4"
          >
            <v-icon size="28" color="grey-lighten-1">mdi-inbox-outline</v-icon>
            <p class="text-caption text-medium-emphasis mt-1">Vazio</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import KanbanCard from './KanbanCard.vue'

const props = defineProps({
  colunas: { type: Array, required: true }
})

const emit = defineEmits(['card-click', 'status-change'])

const dragOver  = ref(null)
const activeDrag = ref(null)

function onDrop(e, targetStatus) {
  dragOver.value = null
  const osId = e.dataTransfer.getData('osId')
  const fromStatus = e.dataTransfer.getData('osStatus')
  if (osId && fromStatus !== targetStatus) {
    emit('status-change', { osId, novoStatus: targetStatus })
  }
}
</script>

<style scoped>
.kanban-wrapper {
  overflow: hidden;
}

.kanban-scroll {
  display: flex;
  gap: 12px;
  overflow-x: auto;
  padding-bottom: 16px;
  min-height: calc(100vh - 220px);
}

.kanban-col {
  min-width: 240px;
  width: 240px;
  flex-shrink: 0;
  background: rgb(var(--v-theme-background));
  border-radius: 12px;
  padding: 12px;
  border: 2px solid transparent;
  transition: border-color 0.2s;
}

.kanban-col.drop-target {
  border-color: rgb(var(--v-theme-primary));
  background: rgba(var(--v-theme-primary), 0.04);
}

.col-header {
  position: sticky;
  top: 0;
  background: inherit;
  z-index: 1;
}

.col-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  flex-shrink: 0;
}

.col-body {
  min-height: 60px;
}

.empty-col {
  border: 1px dashed rgb(var(--v-border-color));
  border-radius: 8px;
  opacity: 0.6;
}

.kanban-scroll::-webkit-scrollbar {
  height: 6px;
}
.kanban-scroll::-webkit-scrollbar-track {
  background: transparent;
}
.kanban-scroll::-webkit-scrollbar-thumb {
  background: rgba(var(--v-theme-on-surface), 0.2);
  border-radius: 3px;
}
</style>
