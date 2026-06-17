<template>
  <v-container fluid class="fill-height consulta-bg">
    <v-row align="center" justify="center">
      <v-col cols="12" sm="9" md="6" lg="5">

        <!-- Card principal -->
        <v-card elevation="8" class="pa-6 rounded-xl">
          <div class="text-center mb-6">
            <v-icon size="52" color="primary">mdi-clipboard-search-outline</v-icon>
            <h1 class="text-h6 font-weight-bold mt-2">Consulta de Ordem de Serviço</h1>
            <p class="text-body-2 text-medium-emphasis mt-1">
              Acompanhe o andamento da sua manutenção
            </p>
          </div>

          <!-- Formulário de busca -->
          <v-text-field
            v-model="numero"
            label="Número da OS"
            placeholder="Ex.: OS-2024-0001"
            prepend-inner-icon="mdi-magnify"
            :rules="[r.required]"
            @keyup.enter="buscar"
            clearable
          />

          <v-btn
            color="primary"
            block
            size="large"
            :loading="loading"
            :disabled="!numero"
            class="mb-4"
            @click="buscar"
          >
            Consultar
          </v-btn>

          <!-- Erro -->
          <v-alert v-if="erro" type="error" variant="tonal" class="mb-4">
            {{ erro }}
          </v-alert>

          <!-- Resultado -->
          <template v-if="resultado">
            <v-divider class="my-4" />

            <!-- Status destacado -->
            <div class="text-center mb-4">
              <StatusBadge :status="resultado.status" />
              <p class="text-h6 font-weight-bold mt-2">{{ resultado.numero }}</p>
            </div>

            <!-- Stepper visual do fluxo -->
            <div class="status-timeline mb-4">
              <div
                v-for="(s, i) in timeline"
                :key="s.status"
                class="timeline-step"
                :class="{
                  'step-done':   s.order < currentOrder,
                  'step-active': s.order === currentOrder,
                  'step-future': s.order > currentOrder
                }"
              >
                <div class="step-dot">
                  <v-icon size="14" v-if="s.order < currentOrder">mdi-check</v-icon>
                  <v-icon size="14" v-else-if="s.order === currentOrder">mdi-circle-medium</v-icon>
                </div>
                <div class="step-label text-caption">{{ s.label }}</div>
                <div v-if="i < timeline.length - 1" class="step-line" />
              </div>
            </div>

            <!-- Detalhes -->
            <v-list density="compact" lines="two" class="mb-2">
              <v-list-item subtitle="Equipamento" :title="resultado.descricaoEquipamento"
                prepend-icon="mdi-desktop-classic" />
              <v-list-item subtitle="Técnico Responsável"
                :title="resultado.nomeTecnico || 'Não atribuído'"
                prepend-icon="mdi-wrench-outline" />
              <v-list-item v-if="resultado.previsaoConclusao"
                subtitle="Previsão de Conclusão"
                :title="fmt(resultado.previsaoConclusao)"
                prepend-icon="mdi-calendar-clock" />
              <v-list-item
                subtitle="Valor Total"
                :title="fmtMoney(resultado.valorTotal)"
                prepend-icon="mdi-currency-usd"
              />
            </v-list>

            <p v-if="resultado.observacoes" class="text-body-2 text-medium-emphasis mt-2 px-2">
              <v-icon size="14" class="mr-1">mdi-information-outline</v-icon>
              {{ resultado.observacoes }}
            </p>
          </template>

          <v-divider class="my-4" />
          <p class="text-center text-caption text-medium-emphasis">
            Possui conta?
            <RouterLink to="/login" class="text-primary font-weight-medium">
              Entrar no sistema
            </RouterLink>
          </p>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
import { ref, computed } from 'vue'
import { ordensApi } from '@/services/api'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import { STATUS_LABELS, STATUS_SEQUENCE } from '@/stores/ordens'

const numero   = ref('')
const loading  = ref(false)
const erro     = ref(null)
const resultado = ref(null)

const r = { required: v => !!v || 'Informe o número da OS.' }

// Exclui "AguardandoPecas" do timeline linear (é retorno)
const timelineStatuses = STATUS_SEQUENCE.filter(s => s !== 'AguardandoPecas')

const timeline = timelineStatuses.map((s, i) => ({
  status: s,
  label: STATUS_LABELS[s],
  order: i
}))

const currentOrder = computed(() => {
  if (!resultado.value) return -1
  const idx = timelineStatuses.indexOf(resultado.value.status)
  return idx === -1 ? 0 : idx
})

async function buscar() {
  if (!numero.value) return
  loading.value = true
  erro.value = null
  resultado.value = null
  try {
    const { data } = await ordensApi.getByNumero(numero.value.trim().toUpperCase())
    resultado.value = data
  } catch (e) {
    erro.value = 'OS não encontrada. Verifique o número e tente novamente.'
  } finally {
    loading.value = false
  }
}

const fmt = d => d ? new Date(d).toLocaleDateString('pt-BR') : '—'
const fmtMoney = v => (v || 0).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
</script>

<style scoped>
.consulta-bg {
  background: linear-gradient(135deg, #1E2937 0%, #1A56DB 100%);
  min-height: 100vh;
}

.status-timeline {
  display: flex;
  align-items: flex-start;
  overflow-x: auto;
  padding: 8px 4px;
  gap: 0;
}

.timeline-step {
  display: flex;
  flex-direction: column;
  align-items: center;
  position: relative;
  flex: 1;
  min-width: 60px;
}

.step-dot {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1;
  transition: all 0.2s;
}

.step-done .step-dot {
  background: rgb(var(--v-theme-success));
  color: white;
}

.step-active .step-dot {
  background: rgb(var(--v-theme-primary));
  color: white;
  box-shadow: 0 0 0 4px rgba(var(--v-theme-primary), 0.2);
}

.step-future .step-dot {
  background: rgb(var(--v-border-color));
}

.step-line {
  position: absolute;
  top: 12px;
  left: 50%;
  width: 100%;
  height: 2px;
  background: rgb(var(--v-border-color));
  z-index: 0;
}

.step-done .step-line { background: rgb(var(--v-theme-success)); }
.step-active .step-line { background: rgb(var(--v-theme-primary)); }

.step-label {
  margin-top: 6px;
  text-align: center;
  word-break: break-word;
  max-width: 56px;
  line-height: 1.2;
}
</style>
