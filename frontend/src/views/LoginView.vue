<template>
  <v-container fluid class="fill-height login-bg">
    <v-row align="center" justify="center" class="fill-height">
      <v-col cols="12" sm="8" md="5" lg="4">

        <!-- Card central -->
        <v-card elevation="8" class="pa-8 rounded-xl">
          <div class="text-center mb-8">
            <v-icon size="56" color="primary">mdi-wrench-cog</v-icon>
            <h1 class="text-h5 font-weight-bold mt-3">Sistema de Manutenção</h1>
            <p class="text-body-2 text-medium-emphasis mt-1">
              Gestão de Ordens de Serviço
            </p>
          </div>

          <!-- Alerta de erro -->
          <v-alert
            v-if="auth.error"
            type="error"
            variant="tonal"
            closable
            class="mb-4"
            @click:close="auth.error = null"
          >
            {{ auth.error }}
          </v-alert>

          <v-form @submit.prevent="submit" ref="formRef">
            <v-text-field
              v-model="email"
              label="E-mail"
              type="email"
              prepend-inner-icon="mdi-email-outline"
              :rules="[r.required, r.email]"
              autofocus
              class="mb-3"
            />

            <v-text-field
              v-model="senha"
              label="Senha"
              :type="showPass ? 'text' : 'password'"
              prepend-inner-icon="mdi-lock-outline"
              :append-inner-icon="showPass ? 'mdi-eye-off' : 'mdi-eye'"
              :rules="[r.required]"
              class="mb-6"
              @click:append-inner="showPass = !showPass"
            />

            <v-btn
              type="submit"
              color="primary"
              block
              size="large"
              :loading="auth.loading"
            >
              Entrar
            </v-btn>
          </v-form>

          <v-divider class="my-6" />

          <p class="text-center text-body-2 text-medium-emphasis">
            Deseja consultar uma OS?
            <RouterLink to="/consulta" class="text-primary font-weight-medium">
              Consulta pública
            </RouterLink>
          </p>
        </v-card>

      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router   = useRouter()
const route    = useRoute()
const auth     = useAuthStore()
const formRef  = ref(null)
const email    = ref('')
const senha    = ref('')
const showPass = ref(false)

const r = {
  required: v => !!v || 'Campo obrigatório.',
  email: v => /.+@.+\..+/.test(v) || 'E-mail inválido.'
}

// DIAGNÓSTICO: se o interceptor da API nos jogou de volta pra cá após um 401,
// o motivo exato vem na query string ?erro=... — mostramos isso na tela
// em vez de deixar o usuário sem nenhuma explicação.
onMounted(() => {
  if (route.query.erro) {
    auth.error = decodeURIComponent(route.query.erro)
    // limpa a query da URL sem recarregar a página, pra não ficar feio se atualizar
    router.replace({ path: '/login' })
  }
})

async function submit() {
  const { valid } = await formRef.value.validate()
  if (!valid) return
  const ok = await auth.login(email.value, senha.value)
  if (ok) router.push('/dashboard')
}
</script>

<style scoped>
.login-bg {
  background: linear-gradient(135deg, #1E2937 0%, #1A56DB 100%);
  min-height: 100vh;
}
</style>
