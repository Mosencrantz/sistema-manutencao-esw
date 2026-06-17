<template>
  <div>
    <div class="d-flex justify-space-between align-center mb-6">
      <div>
        <h2 class="text-h6 font-weight-bold">Clientes</h2>
        <p class="text-body-2 text-medium-emphasis">
          {{ soClientes.length }} cliente{{ soClientes.length !== 1 ? 's' : '' }} cadastrado{{ soClientes.length !== 1 ? 's' : '' }}
        </p>
      </div>
      <v-btn color="primary" prepend-icon="mdi-plus" @click="openCreate">Novo Cliente</v-btn>
    </div>

    <v-text-field
      v-model="search"
      placeholder="Buscar por nome, e-mail ou telefone…"
      prepend-inner-icon="mdi-magnify"
      clearable hide-details class="mb-4"
    />

    <v-card>
      <v-data-table
        :headers="headers"
        :items="soClientes"
        :search="search"
        :loading="store.loading"
        hover
      >
        <template #item.ativo="{ item }">
          <v-chip :color="item.ativo ? 'success' : 'error'" size="x-small" variant="tonal">
            {{ item.ativo ? 'Ativo' : 'Inativo' }}
          </v-chip>
        </template>
        <template #item.criadoEm="{ item }">{{ fmt(item.criadoEm) }}</template>
        <template #item.actions="{ item }">
          <v-btn icon="mdi-pencil-outline" size="small" variant="text" @click="openEdit(item)" />
          <v-btn icon="mdi-delete-outline" size="small" variant="text" color="error" @click="askDelete(item)" />
        </template>
        <template #no-data>
          <div class="py-8 text-center text-medium-emphasis">
            <v-icon size="48">mdi-account-off-outline</v-icon>
            <p class="mt-2">Nenhum cliente encontrado.</p>
          </div>
        </template>
      </v-data-table>
    </v-card>

    <v-dialog v-model="dialog" max-width="560" persistent>
      <v-card>
        <v-card-title class="pa-4">{{ editItem ? 'Editar Cliente' : 'Novo Cliente' }}</v-card-title>
        <v-divider />
        <v-card-text class="pa-4">
          <v-form ref="formRef">
            <v-row dense>
              <v-col cols="12">
                <v-text-field v-model="form.nome" label="Nome completo *" :rules="[r.required]" />
              </v-col>
              <v-col cols="12" sm="6">
                <v-text-field v-model="form.email" label="E-mail *" type="email"
                  :rules="[r.required, r.email]" :disabled="!!editItem" />
              </v-col>
              <v-col cols="12" sm="6">
                <v-text-field v-model="form.telefone" label="Telefone" />
              </v-col>
              <v-col cols="12" v-if="!editItem">
                <v-text-field v-model="form.senha" label="Senha *" type="password"
                  :rules="[r.required, r.minLen]" />
              </v-col>
              <v-col cols="12">
                <v-text-field v-model="form.endereco" label="Endereço" />
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
      title="Excluir cliente"
      message="O cliente será desativado."
      confirm-label="Excluir"
      @confirm="confirmDelete"
    />

    <v-snackbar v-model="snack.show" :color="snack.color" timeout="3000">{{ snack.text }}</v-snackbar>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useClientesStore } from '@/stores/entidades'
import ConfirmDialog from '@/components/shared/ConfirmDialog.vue'

const store  = useClientesStore()
const dialog = ref(false)
const saving = ref(false)
const search = ref('')
const formRef = ref(null)
const editItem = ref(null)
const deleteTarget = ref(null)
const confirmRef = ref(null)
const snack = ref({ show: false, text: '', color: 'success' })

onMounted(() => store.fetchAll())

const soClientes = computed(() => store.clientes.filter(u => u.perfil === 'Cliente'))
const blankForm  = () => ({ nome: '', email: '', telefone: '', senha: '', endereco: '' })
const form = ref(blankForm())

const r = {
  required: v => !!v || 'Campo obrigatório.',
  email:    v => /.+@.+\..+/.test(v) || 'E-mail inválido.',
  minLen:   v => (v && v.length >= 6) || 'Mínimo 6 caracteres.'
}

const headers = [
  { title: 'Nome',     key: 'nome',     sortable: true },
  { title: 'E-mail',   key: 'email',    sortable: false },
  { title: 'Telefone', key: 'telefone', sortable: false },
  { title: 'Status',   key: 'ativo',    sortable: false },
  { title: 'Cadastro', key: 'criadoEm', sortable: true },
  { title: 'Ações',    key: 'actions',  sortable: false, width: 100 }
]

function openCreate() { editItem.value = null; form.value = blankForm(); dialog.value = true }
function openEdit(item) {
  editItem.value = item
  form.value = { nome: item.nome, email: item.email, telefone: item.telefone,
                 senha: '', endereco: item.endereco || '' }
  dialog.value = true
}

async function save() {
  const { valid } = await formRef.value.validate()
  if (!valid) return
  saving.value = true
  try {
    if (editItem.value) {
      await store.update(editItem.value.id, {
        nome: form.value.nome, telefone: form.value.telefone, endereco: form.value.endereco
      })
      notify('Cliente atualizado.')
    } else {
      await store.create(form.value)
      notify('Cliente cadastrado.')
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
  try { await store.delete(deleteTarget.value.id); notify('Cliente desativado.') }
  catch (e) { notify(e.message, 'error') }
}

function notify(text, color = 'success') { snack.value = { show: true, text, color } }
const fmt = d => d ? new Date(d).toLocaleDateString('pt-BR') : '—'
</script>
