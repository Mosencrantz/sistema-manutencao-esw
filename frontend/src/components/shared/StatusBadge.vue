<template>
  <v-chip
    :color="color"
    size="small"
    variant="tonal"
    class="font-weight-medium"
  >
    <v-icon start size="12">{{ icon }}</v-icon>
    {{ label }}
  </v-chip>
</template>

<script setup>
import { computed } from 'vue'
import { STATUS_LABELS, STATUS_COLORS } from '@/stores/ordens'

const props = defineProps({
  status: { type: String, required: true }
})

const label = computed(() => STATUS_LABELS[props.status] || props.status)
const color = computed(() => STATUS_COLORS[props.status] || 'grey')
const icon  = computed(() => ({
  AguardandoEquipamento: 'mdi-truck-outline',
  EquipamentoRecebido:   'mdi-package-variant-closed',
  EmAnalise:             'mdi-magnify',
  AguardandoAprovacao:   'mdi-clock-outline',
  EmManutencao:          'mdi-wrench',
  AguardandoPecas:       'mdi-cog-refresh',
  Finalizado:            'mdi-check-circle-outline',
  EntregueAoCliente:     'mdi-hand-coin-outline'
}[props.status] || 'mdi-circle'))
</script>
