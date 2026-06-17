<template>
  <div>
    <div
      class="drop-zone rounded-lg pa-6 text-center"
      :class="{ dragging }"
      @dragover.prevent="dragging = true"
      @dragleave="dragging = false"
      @drop.prevent="onDrop"
      @click="fileInput.click()"
    >
      <v-icon size="40" color="primary" class="mb-2">mdi-cloud-upload-outline</v-icon>
      <p class="text-body-1 font-weight-medium">
        Arraste arquivos ou <span class="text-primary">clique para selecionar</span>
      </p>
      <p class="text-caption text-medium-emphasis mt-1">
        JPEG, PNG, GIF, MP4 ou PDF — máx. 50 MB por arquivo
      </p>
    </div>

    <input
      ref="fileInput"
      type="file"
      multiple
      accept="image/*,video/mp4,.pdf"
      class="d-none"
      @change="onSelect"
    />

    <v-list v-if="files.length" class="mt-3" density="compact">
      <v-list-item
        v-for="(f, i) in files"
        :key="i"
        :subtitle="formatBytes(f.size)"
      >
        <template #prepend>
          <v-icon color="primary">{{ mimeIcon(f.type) }}</v-icon>
        </template>
        <template #title>
          <span class="text-body-2">{{ f.name }}</span>
        </template>
        <template #append>
          <v-btn icon="mdi-close" size="x-small" variant="text" @click="remove(i)" />
        </template>
      </v-list-item>
    </v-list>

    <v-btn
      v-if="files.length"
      color="primary"
      class="mt-3"
      :loading="uploading"
      @click="upload"
    >
      <v-icon start>mdi-upload</v-icon>
      Enviar {{ files.length }} arquivo{{ files.length > 1 ? 's' : '' }}
    </v-btn>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { arquivosApi } from '@/services/api'

const props = defineProps({ osId: { type: String, required: true } })
const emit  = defineEmits(['uploaded'])

const fileInput = ref(null)
const files     = ref([])
const dragging  = ref(false)
const uploading = ref(false)

const onDrop   = e => { dragging.value = false; addFiles([...e.dataTransfer.files]) }
const onSelect = e => addFiles([...e.target.files])
const addFiles = list => files.value.push(...list)
const remove   = i  => files.value.splice(i, 1)

async function upload() {
  uploading.value = true
  try {
    const results = []
    for (const f of files.value) {
      const { data } = await arquivosApi.upload(props.osId, f)
      results.push(data)
    }
    files.value = []
    emit('uploaded', results)
  } finally {
    uploading.value = false
  }
}

const formatBytes = b => b < 1_048_576
  ? `${(b / 1024).toFixed(1)} KB`
  : `${(b / 1_048_576).toFixed(1)} MB`

const mimeIcon = t =>
  t.startsWith('image') ? 'mdi-image-outline'
  : t.startsWith('video') ? 'mdi-video-outline'
  : 'mdi-file-pdf-box'
</script>

<style scoped>
.drop-zone {
  border: 2px dashed rgba(var(--v-theme-primary), 0.4);
  cursor: pointer;
  transition: all 0.2s;
}
.drop-zone:hover, .drop-zone.dragging {
  border-color: rgb(var(--v-theme-primary));
  background: rgba(var(--v-theme-primary), 0.04);
}
</style>
