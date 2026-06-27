# Roadmap de Melhorias — Sistema de Manutenção
> Lista de funcionalidades, casos de uso e melhorias a serem implementadas pelos colaboradores.
> Organizada por prioridade e complexidade estimada.

---

## 🔴 Prioridade Alta — Essencial para produção

### 1. Restaurar Autenticação JWT
**O que é:** O MVP removeu o login para simplificar o desenvolvimento. A autenticação precisa ser reativada antes de qualquer deploy público.

**O que fazer:**
- Reativar os atributos `[Authorize]` e `[Authorize(Roles = "...")]` nos controllers do backend
- Restaurar o interceptor do axios no frontend para injetar o token Bearer
- Restaurar o guard de rotas no Vue Router (`router/index.js`)
- Restaurar a lógica de login na auth store (`stores/auth.js`)
- Implementar refresh de token para não deslogar o usuário a cada 8 horas
- Adicionar tela de "Sessão expirada"

**Arquivos principais:** `Controllers.cs`, `router/index.js`, `stores/auth.js`, `services/api.js`

---

### 2. Deploy em Servidor Real (sair do localhost)
**O que é:** Hospedar o sistema em um servidor acessível pela internet para que clientes e técnicos usem de qualquer lugar.

**Opções recomendadas (gratuitas ou baratas):**

| Parte | Plataforma sugerida | Obs |
|---|---|---|
| Frontend | **Vercel** ou **Netlify** | Deploy em 1 clique via GitHub |
| Backend | **Railway** ou **Render** | Plano gratuito disponível |
| Banco | **MongoDB Atlas** M0 | Já gratuito, apenas configurar URL |

**O que fazer:**
- Configurar variáveis de ambiente na plataforma escolhida (não hardcodar senhas)
- Atualizar o CORS no `Program.cs` com a URL real do frontend
- Atualizar o `VITE_API_URL` no frontend com a URL real do backend
- Configurar HTTPS (as plataformas acima fazem isso automaticamente)
- Criar pipeline de deploy automático via GitHub Actions

---

### 3. Banco de Dados Central Compartilhado
**O que é:** Um único MongoDB Atlas para todos os desenvolvedores e usuários, em vez de cada um rodar o próprio banco local.

**O que fazer:**
- Criar organização no MongoDB Atlas e convidar os colegas
- Criar dois clusters: um para desenvolvimento (`dev`) e um para produção (`prod`)
- Documentar a connection string no README (sem expor a senha — usar variável de ambiente)
- Definir política de acesso: quem pode escrever, quem pode só ler

---

## 🟡 Prioridade Média — Funcionalidades previstas no ERS

### 4. Recuperação de Senha
**O que é:** Fluxo de "Esqueci minha senha" com envio de e-mail.

**O que fazer:**
- Endpoint `POST /api/auth/recuperar-senha` que gera um token temporário
- Endpoint `POST /api/auth/redefinir-senha` que valida o token e atualiza a senha
- Integrar com serviço de e-mail (SendGrid ou Amazon SES — ambos têm plano gratuito)
- Criar tela de recuperação no frontend

---

### 5. Integração com WhatsApp / Telegram
**O que é:** Cliente manda mensagem no WhatsApp → sistema cria OS automaticamente (previsto no ERS original).

**O que fazer:**
- Integrar com a API do WhatsApp Business (Meta) ou Twilio
- Criar webhook que recebe mensagens e abre uma OS com status "Aguardando Equipamento"
- Notificar o cliente automaticamente quando o status da OS mudar
- Alternativa mais simples: botão "Compartilhar por WhatsApp" que manda o link de consulta da OS

---

### 6. Controle de Estoque de Peças
**O que é:** Módulo para gerenciar peças em estoque, dar baixa quando usadas em OS.

**O que fazer:**
- Nova coleção `pecas` no MongoDB com campos: nome, quantidade, preço custo, preço venda, fornecedor
- CRUD completo de peças com tela própria
- Ao adicionar peça em uma OS, deduzir automaticamente do estoque
- Alertas quando estoque abaixo do mínimo configurado
- Relatório de peças mais utilizadas

---

### 7. Gestão de Fornecedores
**O que é:** Cadastro de fornecedores de peças com consulta de preços e prazos (previsto no ERS).

**O que fazer:**
- Nova coleção `fornecedores` com nome, CNPJ, contato, catálogo de peças
- Tela de consulta: "qual fornecedor tem essa peça mais barata?"
- Integração futura com APIs de distribuidores

---

### 8. Geração de Nota Fiscal / Orçamento em PDF
**O que é:** Gerar PDF da OS para enviar ao cliente com discriminação de serviços e peças.

**O que fazer:**
- Instalar biblioteca de PDF no backend (ex: `QuestPDF` para .NET — open source)
- Endpoint `GET /api/ordens/{id}/pdf` que retorna o PDF gerado
- Botão "Baixar PDF" na tela de detalhes da OS
- Layout profissional com logo, dados da empresa, itemização e total

---

### 9. Portal do Cliente com Login
**O que é:** Área autenticada para o cliente ver todas as suas OS, histórico e orçamentos.

**O que fazer:**
- Página `/minha-conta` acessível com login (e-mail + senha)
- Listar todas as OS do cliente logado
- Permitir upload de fotos do defeito diretamente pelo portal
- Botão de aprovação de orçamento (muda status para "Em Manutenção" ou "Finalizado" com recusa)
- Notificação por e-mail quando status mudar

---

### 10. Notificações por E-mail
**O que é:** E-mails automáticos para cliente e técnico quando eventos importantes ocorrem.

**Eventos sugeridos:**
- OS criada → e-mail para o cliente com número e link de consulta
- Status mudou → e-mail para o cliente
- Orçamento enviado → e-mail com valor e link de aprovação
- OS finalizada → e-mail com resumo e total

**O que fazer:**
- Integrar SendGrid ou Brevo (ambos grátis até 300 e-mails/dia)
- Criar templates HTML de e-mail
- Disparar nos eventos de mudança de status em `OrdemServicoService`

---

## 🟢 Prioridade Baixa — Melhorias e refinamentos

### 11. Relatórios e Métricas
**Relatórios úteis:**
- OS por período (dia/semana/mês)
- Tempo médio de resolução por tipo de equipamento
- Receita total por período
- Técnico mais produtivo
- Peças mais utilizadas
- Clientes com mais OS

**O que fazer:**
- Criar endpoints de agregação no MongoDB
- Adicionar gráficos no Dashboard com a biblioteca Recharts (já disponível) ou Chart.js
- Exportar relatórios em Excel ou PDF

---

### 12. Histórico de Equipamentos
**O que é:** Ver todas as OS anteriores de um equipamento específico.

**O que fazer:**
- Na tela de equipamentos, botão "Ver histórico de OS"
- Filtrar OS por `equipamentoId`
- Útil para identificar equipamentos com problemas recorrentes

---

### 13. Atribuição Automática de Técnico
**O que é:** Sistema sugere ou atribui automaticamente o técnico com menos OS abertas.

**O que fazer:**
- Endpoint que retorna o técnico com menor carga atual
- Opção de ativar/desativar atribuição automática nas configurações

---

### 14. Modo Escuro
**O que é:** Alternância entre tema claro e escuro.

**O que fazer:**
- O Vuetify 3 já suporta nativamente — adicionar toggle no AppLayout
- Salvar preferência no `localStorage`

---

### 15. Aplicativo Mobile (PWA)
**O que é:** Transformar o sistema em um Progressive Web App instalável no celular.

**O que fazer:**
- Adicionar plugin `vite-plugin-pwa` ao frontend
- Configurar `manifest.json` com ícone e nome do app
- Implementar cache offline para consulta de OS sem internet
- O sistema já é responsivo — apenas ajustes de layout mobile

---

### 16. Testes Automatizados
**O que é:** Garantir que o sistema não quebra quando alguém faz mudanças.

**O que fazer no backend:**
- Testes unitários com xUnit para os Services (especialmente a máquina de estados da OS)
- Testes de integração para os Controllers

**O que fazer no frontend:**
- Testes de componentes com Vitest + Vue Test Utils
- Testes E2E com Playwright ou Cypress

---

### 17. Logs e Monitoramento
**O que é:** Saber quando e por que o sistema está falhando em produção.

**O que fazer:**
- Integrar Serilog no backend para logs estruturados
- Integrar Sentry para captura de erros em tempo real (plano gratuito disponível)
- Dashboard de uptime com UptimeRobot (gratuito)

---

### 18. CI/CD com GitHub Actions
**O que é:** A cada `git push`, o sistema testa e faz deploy automaticamente.

**O que fazer:**
- Criar workflow `.github/workflows/deploy.yml`
- Rodar os testes automaticamente em cada Pull Request
- Fazer deploy automático para Railway/Vercel quando aprovar merge na branch `main`

---

## 📋 Tabela de estimativa de esforço

| # | Melhoria | Complexidade | Horas estimadas |
|---|---|---|---|
| 1 | Restaurar autenticação JWT | Média | 4–6h |
| 2 | Deploy em servidor real | Baixa | 2–4h |
| 3 | Banco central compartilhado | Baixa | 1–2h |
| 4 | Recuperação de senha | Média | 4–6h |
| 5 | Integração WhatsApp | Alta | 10–16h |
| 6 | Controle de estoque | Média | 8–12h |
| 7 | Gestão de fornecedores | Média | 6–8h |
| 8 | PDF de orçamento/OS | Média | 4–6h |
| 9 | Portal do cliente | Alta | 12–16h |
| 10 | Notificações por e-mail | Média | 4–6h |
| 11 | Relatórios e métricas | Alta | 10–14h |
| 12 | Histórico de equipamentos | Baixa | 2–3h |
| 13 | Atribuição automática | Baixa | 3–4h |
| 14 | Modo escuro | Baixa | 1–2h |
| 15 | PWA mobile | Média | 4–6h |
| 16 | Testes automatizados | Alta | 12–20h |
| 17 | Logs e monitoramento | Baixa | 2–3h |
| 18 | CI/CD GitHub Actions | Média | 4–6h |
