<template>
  <div>
    <!-- Header -->
    <div class="d-flex justify-space-between align-center mb-4">
      <div>
        <h2 class="text-h6 font-weight-bold">Quadro Kanban</h2>
        <p class="text-body-2 text-medium-emphasis">
          Arraste os cards para atualizar o status
        </p>
      </div>

      <div class="d-flex align-center gap-2">
        <!-- Filtro por técnico -->
        <v-select
          v-model="filtroTecnico"
          :items="tecnicosOpts"
          label="Técnico"
          clearable
          hide-details
          density="compact"
          style="width: 200px"
        />
        <v-btn
          icon="mdi-refresh"
          variant="tonal"
          @click="store.fetchAll()"
          :loading="store.loading"
        />
      </div>
    </div>

    <!-- Sumário rápido -->
    <v-row class="mb-4" dense>
      <v-col v-for="stat in stats" :key="stat.status" cols="auto">
        <v-chip
          :color="stat.color"
          variant="tonal"
          size="small"
          class="font-weight-medium"
        >
          {{ stat.label }}: {{ stat.count }}
        </v-chip>
      </v-col>
    </v-row>

    <!-- Kanban Board -->
    <KanbanBoard
      :colunas="colunasFiltradas"
      @card-click="openDetail"
      @status-change="onStatusChange"
    />

    <!-- ─── Dialog detalhe / atualização ────────────────────── -->
    <v-dialog v-model="detailDialog" max-width="560" persistent>
      <v-card v-if="selected">
        <v-card-title class="pa-4 d-flex align-center gap-3">
          <span>{{ selected.numero }}</span>
          <StatusBadge :status="selected.status" />
          <v-spacer />
          <v-btn icon="mdi-close" variant="text" @click="detailDialog = false" />
        </v-card-title>
        <v-divider />

        <v-card-text class="pa-4">
          <v-list density="compact" lines="two">
            <v-list-item subtitle="Cliente"    :title="selected.nomeCliente" />
            <v-list-item subtitle="Equipamento":title="selected.descricaoEquipamento" />
            <v-list-item subtitle="Técnico"    :title="selected.nomeTecnico || 'Não atribuído'" />
          </v-list>

          <v-divider class="my-3" />

          <!-- Transições disponíveis -->
          <p class="text-subtitle-2 font-weight-bold mb-2">Mover para:</p>
          <div class="d-flex flex-wrap gap-2">
            <v-btn
              v-for="prox in proximosStatus"
              :key="prox.value"
              :color="prox.color"
              variant="tonal"
              size="small"
              :loading="saving && novoStatusAlvo === prox.value"
              @click="moverPara(prox.value)"
            >
              {{ prox.label }}
            </v-btn>
            <span
              v-if="!proximosStatus.length"
              class="text-body-2 text-medium-emphasis"
            >
              OS em estado final.
            </span>
          </div>

          <!-- Observação -->
          <v-textarea
            v-if="proximosStatus.length"
            v-model="obsMovimentacao"
            label="Observação (opcional)"
            rows="2"
            class="mt-4"
          />
        </v-card-text>

        <v-card-actions class="pa-4 pt-0 justify-end">
          <v-btn variant="text" to="/ordens" @click="detailDialog = false">
            Ver detalhes completos
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Confirm drag-drop -->
    <ConfirmDialog
      ref="confirmRef"
      title="Mover Ordem de Serviço"
      :message="`Mover para '${statusLabel(pendingMove?.novoStatus)}'?`"
      confirm-label="Confirmar"
      color="primary"
      icon="mdi-arrow-right-circle-outline"
      @confirm="executarMove"
      @cancel="pendingMove = null"
    />

    <v-snackbar v-model="snack.show" :color="snack.color" timeout="3000">
      {{ snack.text }}
    </v-snackbar>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useOrdensStore, STATUS_LABELS, STATUS_COLORS, STATUS_SEQUENCE } from '@/stores/ordens'
import { useUsuariosStore } from '@/stores/entidades'
import { useAuthStore } from '@/stores/auth'
import KanbanBoard from '@/components/kanban/KanbanBoard.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import ConfirmDialog from '@/components/shared/ConfirmDialog.vue'

const store    = useOrdensStore()
const usrStore = useUsuariosStore()
const auth     = useAuthStore()

const detailDialog   = ref(false)
const selected       = ref(null)
const saving         = ref(false)
const novoStatusAlvo = ref(null)
const obsMovimentacao = ref('')
const filtroTecnico  = ref(null)
const pendingMove    = ref(null)
const confirmRef     = ref(null)
const snack = ref({ show: false, text: '', color: 'success' })

const TRANSITIONS = {
  AguardandoEquipamento: ['EquipamentoRecebido'],
  EquipamentoRecebido:   ['EmAnalise'],
  EmAnalise:             ['AguardandoAprovacao'],
  AguardandoAprovacao:   ['EmManutencao', 'Finalizado'],
  EmManutencao:          ['AguardandoPecas', 'Finalizado'],
  AguardandoPecas:       ['EmManutencao'],
  Finalizado:            ['EntregueAoCliente'],
  EntregueAoCliente:     []
}

onMounted(async () => {
  await Promise.all([store.fetchAll(), usrStore.fetchAll()])
})

const tecnicosOpts = computed(() => [
  { title: 'Todos', value: null },
  ...usrStore.tecnicos.map(t => ({ title: t.nome, value: t.id }))
])

const stats = computed(() => {
  const ativos = store.ordens.filter(o =>
    !['Finalizado', 'EntregueAoCliente'].includes(o.status))
  return [
    { status: 'total',     label: 'Total ativo', color: 'primary',  count: ativos.length },
    { status: 'urgentes',  label: 'Vencidas',    color: 'error',
      count: store.ordens.filter(o => {
        if (!o.previsaoConclusao) return false
        return new Date(o.previsaoConclusao) < new Date() &&
          !['Finalizado','EntregueAoCliente'].includes(o.status)
      }).length
    }
  ]
})

const colunasFiltradas = computed(() => {
  return store.kanbanColunas.map(col => ({
    ...col,
    ordens: col.ordens.filter(o =>
      !filtroTecnico.value || o.tecnicoId === filtroTecnico.value)
  }))
})

const proximosStatus = computed(() => {
  if (!selected.value) return []
  return (TRANSITIONS[selected.value.status] || []).map(s => ({
    value: s,
    label: STATUS_LABELS[s],
    color: STATUS_COLORS[s]
  }))
})

function openDetail(os) {
  selected.value = os
  obsMovimentacao.value = ''
  detailDialog.value = true
}

async function moverPara(status) {
  novoStatusAlvo.value = status
  saving.value = true
  try {
    await store.atualizarStatus(selected.value.id, status, obsMovimentacao.value || null)
    // Refresh selected
    selected.value = store.ordens.find(o => o.id === selected.value.id) || null
    obsMovimentacao.value = ''
    notify(`Movido para "${STATUS_LABELS[status]}"`)
  } catch (e) {
    notify(e.message, 'error')
  } finally {
    saving.value = false
    novoStatusAlvo.value = null
  }
}

function onStatusChange({ osId, novoStatus }) {
  const os = store.ordens.find(o => o.id === osId)
  if (!os) return
  // Verifica se transição é válida antes de confirmar
  const permitidos = TRANSITIONS[os.status] || []
  if (!permitidos.includes(novoStatus)) {
    notify(`Transição ${STATUS_LABELS[os.status]} → ${STATUS_LABELS[novoStatus]} não permitida.`, 'error')
    return
  }
  pendingMove.value = { osId, novoStatus }
  confirmRef.value.open()
}

async function executarMove() {
  if (!pendingMove.value) return
  try {
    await store.atualizarStatus(pendingMove.value.osId, pendingMove.value.novoStatus, null)
    notify(`Status atualizado para "${STATUS_LABELS[pendingMove.value.novoStatus]}"`)
  } catch (e) {
    notify(e.message, 'error')
  } finally {
    pendingMove.value = null
  }
}

const statusLabel = s => s ? STATUS_LABELS[s] : ''

function notify(text, color = 'success') { snack.value = { show: true, text, color } }
</script>
