<template>
  <div>
    <div class="d-flex justify-space-between align-center mb-6">
      <div>
        <h2 class="text-h6 font-weight-bold">Equipamentos</h2>
        <p class="text-body-2 text-medium-emphasis">
          {{ store.equipamentos.length }} equipamento{{ store.equipamentos.length !== 1 ? 's' : '' }} registrado{{ store.equipamentos.length !== 1 ? 's' : '' }}
        </p>
      </div>
      <v-btn color="primary" prepend-icon="mdi-plus" @click="openCreate">Novo Equipamento</v-btn>
    </div>

    <v-text-field
      v-model="search"
      placeholder="Buscar por cliente, marca, modelo…"
      prepend-inner-icon="mdi-magnify"
      clearable hide-details class="mb-4"
    />

    <v-card>
      <v-data-table
        :headers="headers"
        :items="store.equipamentos"
        :search="search"
        :loading="store.loading"
        hover
      >
        <template #item.criadoEm="{ item }">{{ fmt(item.criadoEm) }}</template>
        <template #item.actions="{ item }">
          <v-btn icon="mdi-pencil-outline" size="small" variant="text" @click="openEdit(item)" />
          <v-btn icon="mdi-delete-outline" size="small" variant="text" color="error" @click="askDelete(item)" />
        </template>
        <template #no-data>
          <div class="py-8 text-center text-medium-emphasis">
            <v-icon size="48">mdi-desktop-classic</v-icon>
            <p class="mt-2">Nenhum equipamento encontrado.</p>
          </div>
        </template>
      </v-data-table>
    </v-card>

    <v-dialog v-model="dialog" max-width="600" persistent>
      <v-card>
        <v-card-title class="pa-4">{{ editItem ? 'Editar Equipamento' : 'Novo Equipamento' }}</v-card-title>
        <v-divider />
        <v-card-text class="pa-4">
          <v-form ref="formRef">
            <v-row dense>
              <v-col cols="12" v-if="!editItem">
                <v-autocomplete
                  v-model="form.clienteId"
                  :items="clientes"
                  item-title="nome" item-value="id"
                  label="Cliente *" :rules="[r.required]"
                />
              </v-col>
              <v-col cols="12" sm="6">
                <v-select v-model="form.tipo" :items="tipos" label="Tipo *" :rules="[r.required]" />
              </v-col>
              <v-col cols="12" sm="6">
                <v-text-field v-model="form.marca" label="Marca *" :rules="[r.required]" />
              </v-col>
              <v-col cols="12" sm="6">
                <v-text-field v-model="form.modelo" label="Modelo *" :rules="[r.required]" />
              </v-col>
              <v-col cols="12" sm="6">
                <v-text-field v-model="form.numeroSerie" label="Número de Série" />
              </v-col>
              <v-col cols="12">
                <v-textarea v-model="form.descricaoProblema" label="Descrição do Problema *"
                  rows="3" :rules="[r.required]" />
              </v-col>
            </v-row>
          </v-form>
        </v-card-text>
        <v-divider />
        <v-card-actions class="pa-4 justify-end gap-2">
          <v-btn variant="text" @click="dialog = false">Cancelar</v-btn>
          <v-btn color="primary" :loading="saving" @click="save">Salvar</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <ConfirmDialog
      ref="confirmRef"
      title="Excluir equipamento"
      message="O equipamento será desativado."
      confirm-label="Excluir"
      @confirm="confirmDelete"
    />

    <v-snackbar v-model="snack.show" :color="snack.color" timeout="3000">{{ snack.text }}</v-snackbar>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useEquipamentosStore, useClientesStore } from '@/stores/entidades'
import ConfirmDialog from '@/components/shared/ConfirmDialog.vue'

const store    = useEquipamentosStore()
const cliStore = useClientesStore()
const dialog   = ref(false)
const saving   = ref(false)
const search   = ref('')
const formRef  = ref(null)
const editItem = ref(null)
const deleteTarget = ref(null)
const confirmRef = ref(null)
const snack = ref({ show: false, text: '', color: 'success' })

const tipos = ['Desktop', 'Notebook', 'All-in-One', 'Servidor', 'Tablet', 'Outro']

onMounted(async () => {
  await store.fetchAll()
  await cliStore.fetchAll()
})

const clientes   = computed(() => cliStore.clientes.filter(c => c.perfil === 'Cliente' && c.ativo))
const blankForm  = () => ({ clienteId: '', tipo: '', marca: '', modelo: '', numeroSerie: '', descricaoProblema: '' })
const form = ref(blankForm())

const r = { required: v => !!v || 'Campo obrigatório.' }

const headers = [
  { title: 'Cliente',   key: 'nomeCliente',      sortable: true },
  { title: 'Tipo',      key: 'tipo',             sortable: true },
  { title: 'Marca',     key: 'marca',            sortable: true },
  { title: 'Modelo',    key: 'modelo',           sortable: false },
  { title: 'Nº Série',  key: 'numeroSerie',      sortable: false },
  { title: 'Cadastro',  key: 'criadoEm',         sortable: true },
  { title: 'Ações',     key: 'actions',          sortable: false, width: 100 }
]

function openCreate() { editItem.value = null; form.value = blankForm(); dialog.value = true }
function openEdit(item) {
  editItem.value = item
  form.value = { clienteId: item.clienteId, tipo: item.tipo, marca: item.marca,
                 modelo: item.modelo, numeroSerie: item.numeroSerie, descricaoProblema: item.descricaoProblema }
  dialog.value = true
}

async function save() {
  const { valid } = await formRef.value.validate()
  if (!valid) return
  saving.value = true
  try {
    if (editItem.value) {
      await store.update(editItem.value.id, form.value)
      notify('Equipamento atualizado.')
    } else {
      await store.create(form.value)
      notify('Equipamento cadastrado.')
    }
    dialog.value = false
  } catch (e) {
    notify(e.message, 'error')
  } finally {
    saving.value = false
  }
}

function askDelete(item) { deleteTarget.value = item; confirmRef.value.open() }
async function confirmDelete() {
  try { await store.delete(deleteTarget.value.id); notify('Equipamento desativado.') }
  catch (e) { notify(e.message, 'error') }
}

function notify(text, color = 'success') { snack.value = { show: true, text, color } }
const fmt = d => d ? new Date(d).toLocaleDateString('pt-BR') : '—'
</script>
