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
            <v-list-item subtitle="Cliente"     :title="selected.nomeCliente" />
            <v-list-item subtitle="Equipamento" :title="selected.descricaoEquipamento" />
            <v-list-item subtitle="Técnico"     :title="selected.nomeTecnico || 'Não atribuído'" />
          </v-list>

          <template v-if="selected.observacoes && selected.observacoes.trim()">
            <v-divider class="my-3" />
            <p class="text-caption font-weight-bold text-medium-emphasis mb-1">
              OBSERVAÇÕES REGISTRADAS
            </p>
            <p class="text-body-2 text-medium-emphasis" style="white-space: pre-line">
              {{ selected.observacoes.trim() }}
            </p>
          </template>

          <v-divider class="my-3" />

          <!-- Botões de transição disponíveis -->
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

          <v-textarea
            v-if="proximosStatus.length"
            v-model="obsMovimentacao"
            label="Observação (opcional)"
            rows="2"
            class="mt-4"
            hint="Salva no histórico da OS e visível aqui após a movimentação"
            persistent-hint
          />
        </v-card-text>

        <v-card-actions class="pa-4 pt-0 justify-end">
          <v-btn variant="text" to="/ordens" @click="detailDialog = false">
            Ver detalhes completos
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="finalizarDialog" max-width="480" persistent>
      <v-card>
        <v-card-title class="pa-4 d-flex align-center gap-2">
          <v-icon color="success" size="24">mdi-check-circle-outline</v-icon>
          Finalizar Ordem de Serviço
        </v-card-title>
        <v-divider />

        <v-card-text class="pa-4">
          <p class="text-body-2 mb-4">
            OS: <strong>{{ selected?.numero }}</strong> —
            <span class="text-medium-emphasis">{{ selected?.nomeCliente }}</span>
          </p>

          <!-- Valor do serviço (mão de obra) -->
          <p class="text-subtitle-2 font-weight-bold mb-1">
            <v-icon size="16" color="success" class="mr-1">mdi-currency-usd</v-icon>
            Valor do Serviço (Mão de obra)
          </p>
          <p class="text-caption text-medium-emphasis mb-3">
            Informe o valor da mão de obra. Será somado ao custo das peças já registradas.
          </p>
          <v-text-field
            v-model.number="valorServico"
            label="Valor do serviço (R$)"
            type="number"
            min="0"
            step="0.01"
            prefix="R$"
            class="mb-2"
          />

          <v-alert
            v-if="valorServico > 0"
            type="success"
            variant="tonal"
            density="compact"
            class="mb-4"
          >
            Mão de obra: {{ fmtMoney(valorServico) }}
          </v-alert>

          <!-- Observação de finalização -->
          <v-textarea
            v-model="finalizarObs"
            label="Observação de finalização (opcional)"
            rows="2"
            hint="Será registrada no histórico da OS"
            persistent-hint
          />
        </v-card-text>

        <v-divider />
        <v-card-actions class="pa-4 justify-end gap-2">
          <v-btn variant="text" @click="cancelarFinalizar">Cancelar</v-btn>
          <v-btn color="success" :loading="saving" @click="confirmarFinalizar">
            <v-icon start>mdi-check</v-icon>
            Confirmar Finalização
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="moveDialog" max-width="440" persistent>
      <v-card v-if="pendingMove && selected">
        <v-card-title class="pa-4 d-flex align-center gap-2">
          <v-icon color="primary">mdi-arrow-right-circle-outline</v-icon>
          Mover Ordem de Serviço
        </v-card-title>
        <v-divider />

        <v-card-text class="pa-4">
          <!-- Resumo da OS -->
          <v-list density="compact" lines="two" class="mb-3">
            <v-list-item subtitle="OS"      :title="selected.numero" />
            <v-list-item subtitle="Cliente" :title="selected.nomeCliente" />
          </v-list>

          <!-- Visualização da transição -->
          <div class="d-flex align-center gap-2 mb-4">
            <StatusBadge :status="selected.status" />
            <v-icon size="18" color="medium-emphasis">mdi-arrow-right</v-icon>
            <StatusBadge :status="pendingMove.novoStatus" />
          </div>

          <!-- Campo de observação (Fix 3) -->
          <v-textarea
            v-model="moveObs"
            label="Observação (opcional)"
            rows="2"
            hint="Será registrada no histórico da OS"
            persistent-hint
          />
        </v-card-text>

        <v-divider />
        <v-card-actions class="pa-4 justify-end gap-2">
          <v-btn variant="text" @click="cancelarMover">Cancelar</v-btn>
          <v-btn color="primary" :loading="saving" @click="confirmarMoverDrag">
            Confirmar
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-snackbar v-model="snack.show" :color="snack.color" timeout="3000">
      {{ snack.text }}
    </v-snackbar>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useOrdensStore, STATUS_LABELS, STATUS_COLORS, STATUS_SEQUENCE } from '@/stores/ordens'
import { useUsuariosStore } from '@/stores/entidades'
import KanbanBoard from '@/components/kanban/KanbanBoard.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
// ConfirmDialog REMOVIDO — substituído pelos dialogs moveDialog e finalizarDialog

const store    = useOrdensStore()
const usrStore = useUsuariosStore()

// ─── Dialog 1: Detalhe do card (clique) ──────────────────────────────────────
const detailDialog    = ref(false)
const selected        = ref(null)
const saving          = ref(false)
const novoStatusAlvo  = ref(null)
const obsMovimentacao = ref('')        // observação digitada no dialog de detalhe

// ─── Dialog 2: Finalizar OS (FIX 1) ──────────────────────────────────────────
const finalizarDialog  = ref(false)
const finalizarObs     = ref('')       // observação do dialog de finalização
const valorServico     = ref(0)
// source: 'detail' = veio do clique no card | 'drag' = veio do drag-drop
const pendingFinalizar = ref(null)     // { osId, source }

// ─── Dialog 3: Movimentação drag-drop (FIX 3) ────────────────────────────────
const moveDialog  = ref(false)
const moveObs     = ref('')            // observação do drag-drop
const pendingMove = ref(null)          // { osId, novoStatus }

// ─── UI ──────────────────────────────────────────────────────────────────────
const filtroTecnico = ref(null)
const snack = ref({ show: false, text: '', color: 'success' })

// Mapa de transições válidas (igual ao backend + OrdensServicoView)
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

// ─── Computeds ───────────────────────────────────────────────────────────────

const tecnicosOpts = computed(() => [
  { title: 'Todos', value: null },
  ...usrStore.tecnicos.map(t => ({ title: t.nome, value: t.id }))
])

const stats = computed(() => {
  const ativos = store.ordens.filter(o =>
    !['Finalizado', 'EntregueAoCliente'].includes(o.status))
  return [
    {
      status: 'total',
      label:  'Total ativo',
      color:  'primary',
      count:  ativos.length
    },
    {
      status: 'urgentes',
      label:  'Vencidas',
      color:  'error',
      count:  store.ordens.filter(o => {
        if (!o.previsaoConclusao) return false
        return new Date(o.previsaoConclusao) < new Date() &&
          !['Finalizado', 'EntregueAoCliente'].includes(o.status)
      }).length
    }
  ]
})

const colunasFiltradas = computed(() =>
  store.kanbanColunas.map(col => ({
    ...col,
    ordens: col.ordens.filter(o =>
      !filtroTecnico.value || o.tecnicoId === filtroTecnico.value)
  }))
)

const proximosStatus = computed(() => {
  if (!selected.value) return []
  return (TRANSITIONS[selected.value.status] || []).map(s => ({
    value: s,
    label: STATUS_LABELS[s],
    color: STATUS_COLORS[s]
  }))
})

// ─── Ações: clique no card ────────────────────────────────────────────────────

function openDetail(os) {
  selected.value        = os
  obsMovimentacao.value = ''
  detailDialog.value    = true
}

async function moverPara(status) {
  if (status === 'Finalizado') {
    finalizarObs.value     = obsMovimentacao.value  // transfere observação já digitada
    obsMovimentacao.value  = ''                     // limpa o campo do dialog de detalhe
    valorServico.value     = 0
    pendingFinalizar.value = { osId: selected.value.id, source: 'detail' }
    finalizarDialog.value  = true
    return
  }

  novoStatusAlvo.value = status
  saving.value = true
  try {
    await store.atualizarStatus(selected.value.id, status, obsMovimentacao.value || null)


    selected.value        = store.ordens.find(o => o.id === selected.value.id) || null
    obsMovimentacao.value = ''
    notify(`Movido para "${STATUS_LABELS[status]}"`)
  } catch (e) {
    notify(e.message, 'error')
  } finally {
    saving.value         = false
    novoStatusAlvo.value = null
  }
}

// ─── Ações: drag-drop ─────────────────────────────────────────────────────────

function onStatusChange({ osId, novoStatus }) {
  const os = store.ordens.find(o => o.id === osId)
  if (!os) return

  const permitidos = TRANSITIONS[os.status] || []
  if (!permitidos.includes(novoStatus)) {
    notify(
      `Transição ${STATUS_LABELS[os.status]} → ${STATUS_LABELS[novoStatus]} não permitida.`,
      'error'
    )
    return
  }

  // FIX 1: drag-drop para Finalizado → abre dialog de finalização
  if (novoStatus === 'Finalizado') {
    selected.value         = os
    finalizarObs.value     = ''
    valorServico.value     = 0
    pendingFinalizar.value = { osId, source: 'drag' }
    finalizarDialog.value  = true
    return
  }

  // FIX 3: drag-drop para outros status → abre dialog com observação
  selected.value    = os
  pendingMove.value = { osId, novoStatus }
  moveObs.value     = ''
  moveDialog.value  = true
}

async function confirmarMoverDrag() {
  if (!pendingMove.value) return
  saving.value = true
  try {
    await store.atualizarStatus(
      pendingMove.value.osId,
      pendingMove.value.novoStatus,
      moveObs.value || null
    )
    notify(`Status atualizado para "${STATUS_LABELS[pendingMove.value.novoStatus]}"`)
    moveObs.value    = ''
    moveDialog.value = false
  } catch (e) {
    notify(e.message, 'error')
  } finally {
    saving.value      = false
    pendingMove.value = null
  }
}

function cancelarMover() {
  moveDialog.value  = false
  moveObs.value     = ''
  pendingMove.value = null
}

// ─── Ações: finalização (Fix 1) ───────────────────────────────────────────────

async function confirmarFinalizar() {
  if (!pendingFinalizar.value) return
  const { osId, source } = pendingFinalizar.value
  saving.value = true
  try {
    // Adiciona mão de obra se informada
    if (valorServico.value > 0) {
      await store.adicionarPeca(osId, {
        nome:          'Mão de obra',
        quantidade:    1,
        valorUnitario: valorServico.value
      })
    }

    // Atualiza status com a observação
    await store.atualizarStatus(osId, 'Finalizado', finalizarObs.value || null)

    // Atualiza selected para refletir o novo status no dialog de detalhe
    if (source === 'detail' && detailDialog.value) {
      selected.value = store.ordens.find(o => o.id === osId) || null
    }

    finalizarDialog.value  = false
    notify('OS finalizada com sucesso.')
  } catch (e) {
    notify(e.message, 'error')
  } finally {
    saving.value           = false
    pendingFinalizar.value = null
    finalizarObs.value     = ''
    valorServico.value     = 0
  }
}

function cancelarFinalizar() {
  if (pendingFinalizar.value?.source === 'detail') {
    obsMovimentacao.value = finalizarObs.value
  }
  finalizarDialog.value  = false
  finalizarObs.value     = ''
  valorServico.value     = 0
  pendingFinalizar.value = null
}

// ─── Helpers ─────────────────────────────────────────────────────────────────

function notify(text, color = 'success') {
  snack.value = { show: true, text, color }
}

const fmtMoney = v =>
  (v || 0).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
</script>
