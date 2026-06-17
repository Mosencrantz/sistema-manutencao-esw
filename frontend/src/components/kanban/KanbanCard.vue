<template>
  <v-card
    class="kanban-card mb-2"
    :elevation="dragging ? 6 : 1"
    rounded="lg"
    draggable="true"
    @dragstart="onDragStart"
    @dragend="$emit('drag-end')"
    @click="$emit('click', os)"
  >
    <!-- Cabeçalho colorido -->
    <div class="card-accent" :style="`background: rgb(var(--v-theme-${color}))`" />

    <v-card-text class="pa-3">
      <!-- Número + prioridade -->
      <div class="d-flex justify-space-between align-center mb-2">
        <span class="text-caption font-weight-bold text-medium-emphasis">
          {{ os.numero }}
        </span>
        <v-icon v-if="isUrgente" color="error" size="16" title="Previsão vencida">
          mdi-alert-circle
        </v-icon>
      </div>

      <!-- Cliente -->
      <p class="text-body-2 font-weight-medium text-truncate mb-1">
        {{ os.nomeCliente }}
      </p>

      <!-- Equipamento -->
      <p class="text-caption text-medium-emphasis text-truncate mb-2">
        <v-icon size="12" class="mr-1">mdi-desktop-classic</v-icon>
        {{ os.descricaoEquipamento }}
      </p>

      <!-- Rodapé -->
      <div class="d-flex align-center justify-space-between mt-2">
        <!-- Técnico avatar -->
        <v-tooltip :text="os.nomeTecnico || 'Sem técnico'" location="top">
          <template #activator="{ props }">
            <v-avatar v-bind="props" size="22" :color="os.nomeTecnico ? 'primary' : 'grey-lighten-2'">
              <span class="text-caption" style="font-size: 9px !important">
                {{ initials }}
              </span>
            </v-avatar>
          </template>
        </v-tooltip>

        <!-- Data de abertura -->
        <span class="text-caption text-medium-emphasis">
          {{ fmt(os.dataAbertura) }}
        </span>
      </div>
    </v-card-text>
  </v-card>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  os:    { type: Object, required: true },
  color: { type: String, default: 'primary' }
})

const emit = defineEmits(['click', 'drag-start', 'drag-end'])
const dragging = false

const initials = computed(() => {
  const nome = props.os.nomeTecnico || ''
  return nome ? nome.split(' ').slice(0, 2).map(n => n[0]).join('').toUpperCase() : '?'
})

const isUrgente = computed(() => {
  if (!props.os.previsaoConclusao) return false
  return new Date(props.os.previsaoConclusao) < new Date() &&
    !['Finalizado', 'EntregueAoCliente'].includes(props.os.status)
})

function onDragStart(e) {
  e.dataTransfer.setData('osId', props.os.id)
  e.dataTransfer.setData('osStatus', props.os.status)
  e.dataTransfer.effectAllowed = 'move'
  emit('drag-start', props.os)
}

const fmt = d => d ? new Date(d).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' }) : '—'
</script>

<style scoped>
.kanban-card {
  cursor: grab;
  transition: transform 0.15s, box-shadow 0.15s;
  position: relative;
  overflow: hidden;
}
.kanban-card:hover {
  transform: translateY(-2px);
}
.kanban-card:active {
  cursor: grabbing;
}
.card-accent {
  position: absolute;
  top: 0; left: 0; right: 0;
  height: 3px;
}
</style>
