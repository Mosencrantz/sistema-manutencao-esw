<template>
  <div>
    <div class="d-flex justify-space-between align-center mb-6">
      <div>
        <h2 class="text-h6 font-weight-bold">Usuários do Sistema</h2>
        <p class="text-body-2 text-medium-emphasis">Gerenciamento de acessos e perfis</p>
      </div>
      <v-btn color="primary" prepend-icon="mdi-plus" @click="openCreate">Novo Usuário</v-btn>
    </div>

    <v-row dense class="mb-4">
      <v-col cols="12" md="5">
        <v-text-field v-model="search" placeholder="Buscar por nome ou e-mail…"
          prepend-inner-icon="mdi-magnify" clearable hide-details />
      </v-col>
      <v-col cols="12" md="3">
        <v-select v-model="filtroPerfil" :items="perfis" label="Perfil" clearable hide-details />
      </v-col>
    </v-row>

    <!-- Cards resumo por perfil -->
    <v-row class="mb-6" dense>
      <v-col v-for="p in resumo" :key="p.perfil" cols="6" sm="3">
        <v-card class="pa-4 text-center" :color="p.bg" variant="flat"
          @click="filtroPerfil = p.perfil" style="cursor:pointer">
          <v-icon :color="p.color" size="28" class="mb-1">{{ p.icon }}</v-icon>
          <div class="text-h5 font-weight-black" :class="`text-${p.color}`">{{ p.count }}</div>
          <div class="text-caption text-medium-emphasis">{{ p.perfil }}</div>
        </v-card>
      </v-col>
    </v-row>

    <v-card>
      <v-data-table :headers="headers" :items="filtrados" :loading="store.loading" hover>
        <template #item.perfil="{ item }">
          <v-chip :color="corPerfil(item.perfil)" size="small" variant="tonal">
            {{ item.perfil }}
          </v-chip>
        </template>

        <template #item.ativo="{ item }">
          <v-icon :color="item.ativo ? 'success' : 'error'" size="18">
            {{ item.ativo ? 'mdi-check-circle' : 'mdi-close-circle' }}
          </v-icon>
        </template>

        <template #item.criadoEm="{ item }">{{ fmt(item.criadoEm) }}</template>

        <template #item.actions="{ item }">
          <v-btn icon="mdi-pencil-outline" size="small" variant="text" @click="openEdit(item)" />

          <!-- FIX: administradores não podem ser removidos -->
          <v-tooltip
            :text="item.perfil === 'Administrador'
              ? 'Administradores não podem ser removidos'
              : item.ativo ? 'Desativar usuário' : 'Reativar usuário'"
            location="top"
          >
            <template #activator="{ props }">
              <span v-bind="props">
                <v-btn
                  :icon="item.ativo ? 'mdi-account-off-outline' : 'mdi-account-check-outline'"
                  size="small"
                  variant="text"
                  :color="item.perfil === 'Administrador' ? 'grey' : item.ativo ? 'error' : 'success'"
                  :disabled="item.perfil === 'Administrador'"
                  @click="askToggle(item)"
                />
              </span>
            </template>
          </v-tooltip>
        </template>

        <template #no-data>
          <div class="py-8 text-center text-medium-emphasis">
            <v-icon size="48">mdi-account-group-outline</v-icon>
            <p class="mt-2">Nenhum usuário encontrado.</p>
          </div>
        </template>
      </v-data-table>
    </v-card>

    <!-- Dialog create / edit -->
    <v-dialog v-model="dialog" max-width="560" persistent>
      <v-card>
        <v-card-title class="pa-4">{{ editItem ? 'Editar Usuário' : 'Novo Usuário' }}</v-card-title>
        <v-divider />
        <v-card-text class="pa-4">
          <v-form ref="formRef">
            <v-row dense>
              <v-col cols="12">
                <v-text-field v-model="form.nome" label="Nome *" :rules="[r.required]" />
              </v-col>
              <v-col cols="12" sm="6">
                <v-text-field v-model="form.email" label="E-mail *" type="email"
                  :rules="[r.required, r.email]" :disabled="!!editItem" />
              </v-col>
              <v-col cols="12" sm="6">
                <v-text-field v-model="form.telefone" label="Telefone" />
              </v-col>
              <v-col cols="12" sm="6" v-if="!editItem">
                <v-text-field v-model="form.senha" label="Senha *" type="password"
                  :rules="[r.required, r.minLen]" />
              </v-col>
              <v-col cols="12" sm="6">
                <v-select v-model="form.perfil" :items="perfis" label="Perfil *"
                  :rules="[r.required]" :disabled="!!editItem" />
              </v-col>
              <v-col cols="12" v-if="form.perfil === 'Cliente'">
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
      :title="toggleTarget?.ativo ? 'Desativar usuário' : 'Reativar usuário'"
      :message="toggleTarget?.ativo
        ? 'O usuário perderá acesso ao sistema.'
        : 'O usuário voltará a ter acesso ao sistema.'"
      :confirm-label="toggleTarget?.ativo ? 'Desativar' : 'Reativar'"
      :color="toggleTarget?.ativo ? 'error' : 'success'"
      @confirm="confirmToggle"
    />

    <v-snackbar v-model="snack.show" :color="snack.color" timeout="3000">{{ snack.text }}</v-snackbar>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useUsuariosStore } from '@/stores/entidades'
import ConfirmDialog from '@/components/shared/ConfirmDialog.vue'

const store        = useUsuariosStore()
const dialog       = ref(false)
const saving       = ref(false)
const search       = ref('')
const filtroPerfil = ref(null)
const formRef      = ref(null)
const editItem     = ref(null)
const toggleTarget = ref(null)
const confirmRef   = ref(null)
const snack        = ref({ show: false, text: '', color: 'success' })

const perfis = ['Administrador', 'Funcionario', 'Tecnico', 'Cliente']

onMounted(() => store.fetchAll())

const filtrados = computed(() => {
  let list = store.usuarios
  if (filtroPerfil.value) list = list.filter(u => u.perfil === filtroPerfil.value)
  if (search.value) {
    const q = search.value.toLowerCase()
    list = list.filter(u =>
      u.nome?.toLowerCase().includes(q) || u.email?.toLowerCase().includes(q))
  }
  return list
})

const resumo = computed(() => [
  { perfil: 'Administrador', color: 'error',   bg: 'red-lighten-5',    icon: 'mdi-shield-crown-outline',
    count: store.usuarios.filter(u => u.perfil === 'Administrador').length },
  { perfil: 'Funcionario',   color: 'primary',  bg: 'blue-lighten-5',   icon: 'mdi-account-tie-outline',
    count: store.usuarios.filter(u => u.perfil === 'Funcionario').length },
  { perfil: 'Tecnico',       color: 'purple',   bg: 'purple-lighten-5', icon: 'mdi-wrench-outline',
    count: store.usuarios.filter(u => u.perfil === 'Tecnico').length },
  { perfil: 'Cliente',       color: 'success',  bg: 'green-lighten-5',  icon: 'mdi-account-outline',
    count: store.usuarios.filter(u => u.perfil === 'Cliente').length },
])

const headers = [
  { title: 'Nome',     key: 'nome',     sortable: true },
  { title: 'E-mail',   key: 'email',    sortable: false },
  { title: 'Telefone', key: 'telefone', sortable: false },
  { title: 'Perfil',   key: 'perfil',   sortable: true },
  { title: 'Status',   key: 'ativo',    sortable: false },
  { title: 'Criado',   key: 'criadoEm', sortable: true },
  { title: 'Ações',    key: 'actions',  sortable: false, width: 100 }
]

const blankForm = () => ({ nome: '', email: '', telefone: '', senha: '', perfil: '', endereco: '' })
const form = ref(blankForm())

const r = {
  required: v => !!v || 'Campo obrigatório.',
  email:    v => /.+@.+\..+/.test(v) || 'E-mail inválido.',
  minLen:   v => (v && v.length >= 6) || 'Mínimo 6 caracteres.'
}

function openCreate() { editItem.value = null; form.value = blankForm(); dialog.value = true }
function openEdit(item) {
  editItem.value = item
  form.value = { nome: item.nome, email: item.email, telefone: item.telefone,
                 senha: '', perfil: item.perfil, endereco: item.endereco || '' }
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
      notify('Usuário atualizado.')
    } else {
      await store.create(form.value)
      notify('Usuário criado.')
    }
    dialog.value = false
  } catch (e) {
    notify(e.message, 'error')
  } finally {
    saving.value = false
  }
}

function askToggle(item) {
  // FIX: bloqueia remoção de administradores
  if (item.perfil === 'Administrador') {
    notify('Administradores não podem ser removidos do sistema.', 'error')
    return
  }
  toggleTarget.value = item
  confirmRef.value.open()
}

async function confirmToggle() {
  try {
    await store.delete(toggleTarget.value.id)
    notify(toggleTarget.value.ativo ? 'Usuário desativado.' : 'Usuário reativado.')
  } catch (e) {
    notify(e.message, 'error')
  }
}

const corPerfil = p => ({ Administrador: 'error', Tecnico: 'purple', Funcionario: 'primary', Cliente: 'success' }[p] || 'grey')
const fmt = d => d ? new Date(d).toLocaleDateString('pt-BR') : '—'
function notify(text, color = 'success') { snack.value = { show: true, text, color } }
</script>
