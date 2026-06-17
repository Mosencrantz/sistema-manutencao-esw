<template>
  <div>
    <h2 class="text-h5 font-weight-bold mb-6">
      Bom dia, {{ primeiroNome }} 👋
    </h2>

    <!-- ─── KPI cards ─────────────────────────────────────── -->
    <v-row class="mb-6">
      <v-col v-for="kpi in kpis" :key="kpi.label" cols="12" sm="6" md="3">
        <v-card class="pa-5" :color="kpi.bg" variant="flat">
          <div class="d-flex align-center justify-space-between">
            <div>
              <div class="text-h4 font-weight-black" :class="`text-${kpi.color}`">
                {{ kpi.value }}
              </div>
              <div class="text-body-2 text-medium-emphasis mt-1">{{ kpi.label }}</div>
            </div>
            <v-icon :color="kpi.color" size="40">{{ kpi.icon }}</v-icon>
          </div>
        </v-card>
      </v-col>
    </v-row>

    <!-- ─── OS recentes ───────────────────────────────────── -->
    <v-row>
      <v-col cols="12" md="7">
        <v-card>
          <v-card-title class="pa-4 pb-2 d-flex justify-space-between align-center">
            <span>Últimas Ordens de Serviço</span>
            <v-btn variant="text" size="small" to="/ordens" color="primary">
              Ver todas
            </v-btn>
          </v-card-title>

          <v-data-table
            :headers="headers"
            :items="ultimas"
            :loading="loading"
            density="compact"
            hide-default-footer
            :items-per-page="6"
          >
            <template #item.status="{ item }">
              <StatusBadge :status="item.status" />
            </template>
            <template #item.dataAbertura="{ item }">
              {{ formatDate(item.dataAbertura) }}
            </template>
            <template #item.actions="{ item }">
              <v-btn icon="mdi-arrow-right" size="small" variant="text" :to="`/ordens`" />
            </template>
          </v-data-table>
        </v-card>
      </v-col>

      <!-- ─── Distribuição por status ─────────────────────── -->
      <v-col cols="12" md="5">
        <v-card class="pa-4">
          <v-card-title class="pa-0 pb-4">Por Status</v-card-title>
          <div
            v-for="(item, i) in porStatus"
            :key="item.status"
            class="mb-3"
          >
            <div class="d-flex justify-space-between text-body-2 mb-1">
              <span>{{ item.label }}</span>
              <strong>{{ item.count }}</strong>
            </div>
            <v-progress-linear
              :model-value="item.pct"
              :color="item.color"
              bg-color="grey-lighten-3"
              rounded
              height="8"
            />
          </div>
        </v-card>
      </v-col>
    </v-row>
  </div>
</template>

<script setup>
import { computed, onMounted } from 'vue'
import { useOrdensStore, STATUS_LABELS, STATUS_COLORS, STATUS_SEQUENCE } from '@/stores/ordens'
import { useAuthStore } from '@/stores/auth'
import StatusBadge from '@/components/shared/StatusBadge.vue'

const auth   = useAuthStore()
const ordens = useOrdensStore()
const loading = computed(() => ordens.loading)

onMounted(() => ordens.fetchAll())

const primeiroNome = computed(() =>
  auth.usuario?.nome?.split(' ')[0] || 'usuário')

const total     = computed(() => ordens.ordens.length)
const emAberto  = computed(() =>
  ordens.ordens.filter(o => !['Finalizado','EntregueAoCliente'].includes(o.status)).length)
const finalizadas = computed(() =>
  ordens.ordens.filter(o => o.status === 'Finalizado').length)
const aguardando = computed(() =>
  ordens.ordens.filter(o => o.status === 'AguardandoAprovacao').length)

const kpis = computed(() => [
  { label: 'Total de OS',         value: total.value,      icon: 'mdi-clipboard-list', color: 'primary',  bg: 'blue-lighten-5' },
  { label: 'Em Andamento',        value: emAberto.value,   icon: 'mdi-progress-wrench', color: 'purple',   bg: 'purple-lighten-5' },
  { label: 'Ag. Aprovação',       value: aguardando.value, icon: 'mdi-clock-outline',   color: 'warning',  bg: 'amber-lighten-5' },
  { label: 'Finalizadas',         value: finalizadas.value,icon: 'mdi-check-circle',    color: 'success',  bg: 'green-lighten-5' },
])

const headers = [
  { title: 'Número',  key: 'numero',       sortable: false },
  { title: 'Cliente', key: 'nomeCliente',  sortable: false },
  { title: 'Status',  key: 'status',       sortable: false },
  { title: 'Abertura',key: 'dataAbertura', sortable: false },
  { title: '',        key: 'actions',      sortable: false, width: 50 }
]

const ultimas = computed(() =>
  [...ordens.ordens].sort((a, b) =>
    new Date(b.dataAbertura) - new Date(a.dataAbertura)).slice(0, 6))

const porStatus = computed(() => {
  const t = total.value || 1
  return STATUS_SEQUENCE.map(s => ({
    status: s,
    label: STATUS_LABELS[s],
    color: STATUS_COLORS[s],
    count: ordens.ordens.filter(o => o.status === s).length,
    pct: Math.round(ordens.ordens.filter(o => o.status === s).length / t * 100)
  })).filter(x => x.count > 0)
})

const formatDate = d => d ? new Date(d).toLocaleDateString('pt-BR') : '—'
</script>
