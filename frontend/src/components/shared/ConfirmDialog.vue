<template>
  <v-dialog v-model="visible" max-width="420" persistent>
    <v-card>
      <v-card-title class="pa-4 d-flex align-center gap-3">
        <v-icon :color="color" size="28">{{ icon }}</v-icon>
        {{ title }}
      </v-card-title>
      <v-card-text class="pa-4 pt-0 text-medium-emphasis">
        {{ message }}
      </v-card-text>
      <v-card-actions class="pa-4 pt-0 gap-2 justify-end">
        <v-btn variant="text" @click="cancel">{{ cancelLabel }}</v-btn>
        <v-btn :color="color" @click="confirm">{{ confirmLabel }}</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup>
import { ref } from 'vue'

defineProps({
  title:        { type: String, default: 'Confirmar' },
  message:      { type: String, default: 'Tem certeza?' },
  confirmLabel: { type: String, default: 'Confirmar' },
  cancelLabel:  { type: String, default: 'Cancelar' },
  color:        { type: String, default: 'error' },
  icon:         { type: String, default: 'mdi-alert-circle-outline' }
})

const emit    = defineEmits(['confirm', 'cancel'])
const visible = ref(false)

const open    = () => { visible.value = true }
const cancel  = () => { visible.value = false; emit('cancel') }
const confirm = () => { visible.value = false; emit('confirm') }

defineExpose({ open })
</script>
