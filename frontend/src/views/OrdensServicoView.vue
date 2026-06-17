<template>
  <div>
    <div class="d-flex justify-space-between align-center mb-6">
      <div>
        <h2 class="text-h6 font-weight-bold">Ordens de Serviço</h2>
        <p class="text-body-2 text-medium-emphasis">
          {{ store.ordens.length }} OS cadastrada{{ store.ordens.length !== 1 ? 's' : '' }}
        </p>
      </div>
      <v-btn color="primary" prepend-icon="mdi-plus" @click="openCreate">Nova OS</v-btn>
    </div>

    <v-row class="mb-4" dense>
      <v-col cols="12" md="5">
        <v-text-field v-model="search" placeholder="Buscar por número, cliente, técnico…"
          prepend-inner-icon="mdi-magnify" clearable hide-details />
      </v-col>
      <v-col cols="12" md="4">
        <v-select v-model="filterStatus" :items="statusItems"
          label="Filtrar por status" clearable hide-details />
      </v-col>
    </v-row>

    <v-card>
      <v-data-table
        :headers="headers"
        :items="filtered"
        :loading="store.loading"
        hover
        @click:row="(_, { item }) => openDetail(item)"
      >
        <template #item.status="{ item }">
          <StatusBadge :status="item.status" />
        </template>
        <template #item.dataAbertura="{ item }">{{ fmt(item.dataAbertura) }}</template>
        <template #item.valorTotal="{ item }">{{ fmtMoney(item.valorTotal) }}</template>

        <template #item.actions="{ item }">
          <v-btn icon="mdi-eye-outline" size="small" variant="text"
            @click.stop="openDetail(item)" />

          <!-- FIX: esconde botão de status quando OS já foi entregue -->
          <v-btn
            v-if="item.status !== 'EntregueAoCliente'"
            icon="mdi-arrow-right-circle-outline"
            size="small" variant="text" color="primary"
            @click.stop="openStatus(item)"
          />
        </template>

        <template #no-data>
          <div class="py-8 text-center text-medium-emphasis">
            <v-icon size="48">mdi-clipboard-off-outline</v-icon>
            <p class="mt-2">Nenhuma ordem de serviço encontrada.</p>
          </div>
        </template>
      </v-data-table>
    </v-card>

    <!-- ─── Dialog: Nova OS ───────────────────────────────── -->
    <v-dialog v-model="createDialog" max-width="640" persistent>
      <v-card>
        <v-card-title class="pa-4">Nova Ordem de Serviço</v-card-title>
        <v-divider />
        <v-card-text class="pa-4">
          <v-form ref="createForm">
            <v-row dense>
              <v-col cols="12" md="6">
                <v-autocomplete
                  v-model="newOS.clienteId"
                  :items="clientes"
                  item-title="nome" item-value="id"
                  label="Cliente *"
                  :rules="[r.required]"
                />
              </v-col>

              <v-col cols="12" md="6">
                <v-autocomplete
                  v-model="newOS.equipamentoId"
                  :items="equipClienteOpts"
                  item-title="titulo" item-value="id"
                  label="Equipamento *"
                  :rules="[r.required]"
                  :disabled="!newOS.clienteId"
                  :loading="loadingEquips"
                  :no-data-text="noDataText"
                />
              </v-col>

              <v-col cols="12">
                <!-- FIX: descrição herdada do equipamento, editável -->
                <v-textarea
                  v-model="newOS.descricaoProblema"
                  label="Descrição do problema *"
                  rows="3"
                  :rules="[r.required]"
                  :hint="descHint"
                  persistent-hint
                />
              </v-col>

              <v-col cols="12" md="6">
                <v-select
                  v-model="newOS.tipoEntrega"
                  :items="['Balcao', 'Transportadora']"
                  label="Tipo de Entrega *"
                  :rules="[r.required]"
                />
              </v-col>

              <v-col cols="12" md="6">
                <v-text-field
                  v-model="newOS.previsaoConclusao"
                  label="Previsão de Conclusão"
                  type="date"
                />
              </v-col>

              <v-col cols="12">
                <v-autocomplete
                  v-model="newOS.tecnicoId"
                  :items="tecnicos"
                  item-title="nome" item-value="id"
                  label="Técnico Responsável"
                  clearable
                />
              </v-col>
            </v-row>
          </v-form>
        </v-card-text>
        <v-divider />
        <v-card-actions class="pa-4 justify-end gap-2">
          <v-btn variant="text" @click="createDialog = false">Cancelar</v-btn>
          <v-btn color="primary" :loading="saving" @click="saveOS">Criar OS</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- ─── Dialog: Detalhe da OS ─────────────────────────── -->
    <v-dialog v-model="detailDialog" max-width="820" scrollable>
      <v-card v-if="selected">
        <v-card-title class="pa-4 d-flex align-center gap-3">
          <span>OS {{ selected.numero }}</span>
          <StatusBadge :status="selected.status" />
          <v-spacer />
          <v-btn icon="mdi-close" variant="text" @click="detailDialog = false" />
        </v-card-title>
        <v-divider />

        <v-card-text class="pa-4">
          <v-row>
            <v-col cols="12" md="6">
              <v-list density="compact" lines="two">
                <v-list-item subtitle="Cliente"     :title="selected.nomeCliente"          prepend-icon="mdi-account-outline" />
                <v-list-item subtitle="Equipamento" :title="selected.descricaoEquipamento"  prepend-icon="mdi-desktop-classic" />
                <v-list-item subtitle="Técnico"     :title="selected.nomeTecnico || 'Não atribuído'" prepend-icon="mdi-wrench-outline" />
                <v-list-item subtitle="Tipo Entrega":title="selected.tipoEntrega"           prepend-icon="mdi-truck-outline" />
                <v-list-item subtitle="Valor Total" :title="fmtMoney(selected.valorTotal)"  prepend-icon="mdi-currency-usd" />
                <v-list-item subtitle="Abertura"    :title="fmt(selected.dataAbertura)"     prepend-icon="mdi-calendar-outline" />
                <v-list-item v-if="selected.previsaoConclusao"
                  subtitle="Previsão" :title="fmt(selected.previsaoConclusao)"
                  prepend-icon="mdi-calendar-clock" />
              </v-list>
            </v-col>

            <v-col cols="12" md="6">
              <p class="text-subtitle-2 font-weight-bold mb-1">Descrição do Problema</p>
              <p class="text-body-2 mb-4">{{ selected.descricaoProblema }}</p>
              <p class="text-subtitle-2 font-weight-bold mb-1">Observações</p>
              <p class="text-body-2 text-medium-emphasis">
                {{ selected.observacoes || 'Nenhuma observação registrada.' }}
              </p>
            </v-col>

            <!-- Peças utilizadas -->
            <v-col cols="12" v-if="selected.pecasUtilizadas?.length">
              <p class="text-subtitle-2 font-weight-bold mb-2">Peças e Serviços</p>
              <v-table density="compact">
                <thead>
                  <tr>
                    <th>Item</th>
                    <th class="text-center">Qtd.</th>
                    <th class="text-right">Unit.</th>
                    <th class="text-right">Total</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="p in selected.pecasUtilizadas" :key="p.nome">
                    <td>{{ p.nome }}</td>
                    <td class="text-center">{{ p.quantidade }}</td>
                    <td class="text-right">{{ fmtMoney(p.valorUnitario) }}</td>
                    <td class="text-right">{{ fmtMoney(p.quantidade * p.valorUnitario) }}</td>
                  </tr>
                </tbody>
              </v-table>
            </v-col>

            <!-- Registrar peça -->
            <v-col cols="12"
              v-if="!['Finalizado','EntregueAoCliente'].includes(selected.status)">
              <v-divider class="my-2" />
              <p class="text-subtitle-2 font-weight-bold mb-3">Registrar Peça</p>
              <v-row dense>
                <v-col cols="12" md="5">
                  <v-text-field v-model="peca.nome" label="Nome da peça"
                    :rules="[r.required]" hide-details />
                </v-col>
                <v-col cols="4" md="2">
                  <!-- FIX: quantidade mínima 1 -->
                  <v-text-field v-model.number="peca.quantidade" label="Qtd."
                    type="number" min="1"
                    :rules="[v => v >= 1 || 'Mín. 1']"
                    hide-details />
                </v-col>
                <v-col cols="5" md="3">
                  <!-- FIX: valor mínimo 0 -->
                  <v-text-field v-model.number="peca.valorUnitario" label="Valor unit. (R$)"
                    type="number" min="0" step="0.01"
                    :rules="[v => v >= 0 || 'Deve ser ≥ 0']"
                    hide-details />
                </v-col>
                <v-col cols="3" md="2" class="d-flex align-center">
                  <v-btn color="primary" :loading="pecaSaving" @click="adicionarPeca" block>
                    Adicionar
                  </v-btn>
                </v-col>
              </v-row>
            </v-col>

            <!-- Upload -->
            <v-col cols="12">
              <v-divider class="my-2" />
              <p class="text-subtitle-2 font-weight-bold mb-3">Arquivos Anexados</p>
              <FileUpload :os-id="selected.id" @uploaded="onUploaded" />
            </v-col>

            <!-- Histórico -->
            <v-col cols="12">
              <v-divider class="my-2" />
              <p class="text-subtitle-2 font-weight-bold mb-3">Histórico</p>
              <v-timeline density="compact" side="end">
                <v-timeline-item v-for="h in historico" :key="h.id"
                  size="x-small" dot-color="primary">
                  <div class="text-body-2">{{ h.descricaoAlteracao }}</div>
                  <div class="text-caption text-medium-emphasis">{{ fmtFull(h.dataAlteracao) }}</div>
                </v-timeline-item>
                <v-timeline-item v-if="!historico.length" size="x-small" dot-color="grey">
                  <span class="text-body-2 text-medium-emphasis">Sem histórico.</span>
                </v-timeline-item>
              </v-timeline>
            </v-col>
          </v-row>
        </v-card-text>
      </v-card>
    </v-dialog>

    <!-- ─── Dialog: Atualizar Status ──────────────────────── -->
    <v-dialog v-model="statusDialog" max-width="480" persistent>
      <v-card v-if="selected">
        <v-card-title class="pa-4">Atualizar Status</v-card-title>
        <v-divider />
        <v-card-text class="pa-4">
          <p class="text-body-2 mb-4">
            Status atual: <StatusBadge :status="selected.status" />
          </p>

          <v-select
            v-model="novoStatus"
            :items="statusDisponiveis"
            label="Novo Status *"
            class="mb-3"
          />

          <!-- FIX: campo de valor do serviço ao finalizar -->
          <v-expand-transition>
            <div v-if="novoStatus === 'Finalizado'">
              <v-divider class="mb-4" />
              <p class="text-subtitle-2 font-weight-bold mb-1">
                <v-icon size="16" color="success" class="mr-1">mdi-currency-usd</v-icon>
                Valor do Serviço (Mão de obra)
              </p>
              <p class="text-caption text-medium-emphasis mb-3">
                Informe o valor cobrado pela mão de obra. Será somado ao custo das peças.
              </p>
              <v-text-field
                v-model.number="valorServico"
                label="Valor do serviço (R$)"
                type="number"
                min="0"
                step="0.01"
                prefix="R$"
                :rules="[v => v >= 0 || 'Deve ser maior ou igual a 0']"
              />
              <v-alert v-if="valorServico > 0" type="success" variant="tonal" density="compact">
                Total final: {{ fmtMoney((selected.valorTotal || 0) + valorServico) }}
                (peças: {{ fmtMoney(selected.valorTotal || 0) }} + serviço: {{ fmtMoney(valorServico) }})
              </v-alert>
            </div>
          </v-expand-transition>

          <v-textarea
            v-model="statusObs"
            label="Observação"
            rows="2"
            class="mt-3"
          />
        </v-card-text>
        <v-divider />
        <v-card-actions class="pa-4 justify-end gap-2">
          <v-btn variant="text" @click="statusDialog = false">Cancelar</v-btn>
          <v-btn color="primary" :loading="saving" :disabled="!novoStatus" @click="saveStatus">
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
import { ref, computed, onMounted, watch } from 'vue'
import { useOrdensStore, STATUS_LABELS, STATUS_SEQUENCE } from '@/stores/ordens'
import { useClientesStore, useEquipamentosStore, useUsuariosStore } from '@/stores/entidades'
import { equipamentosApi, ordensApi } from '@/services/api'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import FileUpload  from '@/components/shared/FileUpload.vue'

const store      = useOrdensStore()
const cliStore   = useClientesStore()
const equipStore = useEquipamentosStore()
const usrStore   = useUsuariosStore()

const search        = ref('')
const filterStatus  = ref(null)
const createDialog  = ref(false)
const detailDialog  = ref(false)
const statusDialog  = ref(false)
const saving        = ref(false)
const pecaSaving    = ref(false)
const loadingEquips = ref(false)
const selected      = ref(null)
const historico     = ref([])
const createForm    = ref(null)
const snack         = ref({ show: false, text: '', color: 'success' })

const equipCliente  = ref([])
const newOS         = ref(blankOS())
const descHint      = ref('')   // dica quando a descrição é herdada

const peca          = ref({ nome: '', quantidade: 1, valorUnitario: 0 })
const novoStatus    = ref('')
const statusObs     = ref('')
const valorServico  = ref(0)    // FIX: valor da mão de obra ao finalizar

const r = {
  required: v => !!v || 'Campo obrigatório.',
  naoNeg:   v => (v !== '' && v >= 0) || 'Deve ser ≥ 0',
  minUm:    v => (v >= 1) || 'Mínimo 1'
}

const clientes  = computed(() => cliStore.clientes.filter(c => c.perfil === 'Cliente' && c.ativo))
const tecnicos  = computed(() => usrStore.tecnicos.filter(t => t.ativo))

const equipClienteOpts = computed(() =>
  equipCliente.value.map(e => ({
    id:    e.id    || e.Id    || '',
    titulo:`${e.marca || e.Marca || ''} ${e.modelo || e.Modelo || ''}`.trim() || 'Equipamento sem descrição'
  }))
)

const noDataText = computed(() =>
  !newOS.value.clienteId       ? 'Selecione um cliente primeiro'
  : loadingEquips.value        ? 'Carregando…'
  : 'Nenhum equipamento cadastrado para este cliente'
)

const statusItems = computed(() =>
  STATUS_SEQUENCE.map(s => ({ title: STATUS_LABELS[s], value: s })))

const headers = [
  { title: 'Número',   key: 'numero',       sortable: true },
  { title: 'Cliente',  key: 'nomeCliente',  sortable: true },
  { title: 'Técnico',  key: 'nomeTecnico',  sortable: false },
  { title: 'Status',   key: 'status',       sortable: false },
  { title: 'Abertura', key: 'dataAbertura', sortable: true },
  { title: 'Valor',    key: 'valorTotal',   sortable: true },
  { title: 'Ações',    key: 'actions',      sortable: false, width: 100 }
]

const filtered = computed(() => {
  let list = store.ordens
  if (filterStatus.value) list = list.filter(o => o.status === filterStatus.value)
  if (search.value) {
    const q = search.value.toLowerCase()
    list = list.filter(o =>
      o.numero?.toLowerCase().includes(q) ||
      o.nomeCliente?.toLowerCase().includes(q) ||
      o.nomeTecnico?.toLowerCase().includes(q))
  }
  return list
})

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

const statusDisponiveis = computed(() => {
  if (!selected.value) return []
  return (TRANSITIONS[selected.value.status] || [])
    .map(s => ({ title: STATUS_LABELS[s], value: s }))
})

// ─── Watch: recarrega equipamentos quando cliente muda ────────────────────────
watch(
  () => newOS.value.clienteId,
  async (clienteId) => {
    newOS.value.equipamentoId   = ''
    newOS.value.descricaoProblema = ''
    descHint.value              = ''
    equipCliente.value          = []
    if (!clienteId) return

    loadingEquips.value = true
    try {
      const { data } = await equipamentosApi.getByCliente(clienteId)
      equipCliente.value = Array.isArray(data) ? data : []
      if (!equipCliente.value.length)
        notify('Nenhum equipamento encontrado para este cliente. Cadastre um primeiro.', 'warning')
    } catch (e) {
      notify('Erro ao carregar equipamentos: ' + e.message, 'error')
    } finally {
      loadingEquips.value = false
    }
  }
)

// FIX: herda descrição do equipamento quando selecionado
watch(
  () => newOS.value.equipamentoId,
  (equipId) => {
    if (!equipId) { descHint.value = ''; return }

    const equip = equipCliente.value.find(e =>
      (e.id || e.Id || '') === equipId
    )
    if (equip) {
      const desc = equip.descricaoProblema || equip.DescricaoProblema || ''
      if (desc) {
        newOS.value.descricaoProblema = desc
        descHint.value = 'Descrição herdada do equipamento — edite se necessário.'
      }
    }
  }
)

onMounted(async () => {
  await Promise.all([
    store.fetchAll(),
    cliStore.fetchAll(),
    usrStore.fetchAll()
  ])
})

function blankOS() {
  return { clienteId: '', equipamentoId: '', descricaoProblema: '',
           tipoEntrega: 'Balcao', previsaoConclusao: null, tecnicoId: null }
}

function openCreate() {
  newOS.value        = blankOS()
  equipCliente.value = []
  descHint.value     = ''
  createDialog.value = true
}

async function saveOS() {
  const { valid } = await createForm.value.validate()
  if (!valid) return
  saving.value = true
  try {
    await store.create({ ...newOS.value, previsaoConclusao: newOS.value.previsaoConclusao || null })
    createDialog.value = false
    notify('Ordem de serviço criada com sucesso.')
  } catch (e) {
    notify(e.message, 'error')
  } finally {
    saving.value = false
  }
}

async function openDetail(item) {
  selected.value     = item
  detailDialog.value = true
  peca.value         = { nome: '', quantidade: 1, valorUnitario: 0 }
  historico.value    = await store.fetchHistorico(item.id)
}

function openStatus(item) {
  selected.value   = item
  novoStatus.value = ''
  statusObs.value  = ''
  valorServico.value = 0
  statusDialog.value = true
}

async function saveStatus() {
  if (!novoStatus.value) return
  saving.value = true
  try {
    // FIX: se finalizando com valor de serviço, adiciona como "Mão de obra"
    if (novoStatus.value === 'Finalizado' && valorServico.value > 0) {
      await store.adicionarPeca(selected.value.id, {
        nome: 'Mão de obra',
        quantidade: 1,
        valorUnitario: valorServico.value
      })
    }

    await store.atualizarStatus(selected.value.id, novoStatus.value, statusObs.value)
    statusDialog.value = false
    valorServico.value = 0
    notify('Status atualizado com sucesso.')
  } catch (e) {
    notify(e.message, 'error')
  } finally {
    saving.value = false
  }
}

async function adicionarPeca() {
  // FIX: valida preço >= 0 e quantidade >= 1
  if (!peca.value.nome)               return notify('Informe o nome da peça.', 'warning')
  if (peca.value.quantidade < 1)      return notify('Quantidade mínima é 1.', 'warning')
  if (peca.value.valorUnitario < 0)   return notify('Valor deve ser maior ou igual a 0.', 'warning')

  pecaSaving.value = true
  try {
    const updated = await store.adicionarPeca(selected.value.id, peca.value)
    selected.value = updated
    peca.value = { nome: '', quantidade: 1, valorUnitario: 0 }
    notify('Peça registrada.')
  } catch (e) {
    notify(e.message, 'error')
  } finally {
    pecaSaving.value = false
  }
}

function onUploaded() { notify('Arquivo(s) enviado(s) com sucesso.') }

function notify(text, color = 'success') { snack.value = { show: true, text, color } }
const fmt      = d => d ? new Date(d).toLocaleDateString('pt-BR') : '—'
const fmtFull  = d => d ? new Date(d).toLocaleString('pt-BR')     : '—'
const fmtMoney = v => (v || 0).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
</script>
