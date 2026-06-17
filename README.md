# Sistema de Manutenção de Computadores
**Universidade Federal de Sergipe — Departamento de Computação**

Sistema web para gerenciamento de ordens de serviço de manutenção de computadores com fluxo Kanban.

## Tecnologias

| Camada | Tecnologia |
|--------|-----------|
| Frontend | Vue.js 3 + Vuetify 3 |
| Backend  | ASP.NET Core (.NET 8) |
| Banco    | MongoDB (Atlas ou Docker) |

---

## Como rodar o projeto

### Pré-requisitos
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org)
- Conta no [MongoDB Atlas](https://cloud.mongodb.com) (gratuita) **ou** Docker instalado

### 1. Clonar o repositório
```bash
git clone https://github.com/SEU_USUARIO/SEU_REPOSITORIO.git
cd sistema-manutencao
```

### 2. Configurar o banco de dados

**Opção A — MongoDB Atlas (recomendado):**
1. Crie uma conta gratuita em https://cloud.mongodb.com
2. Crie um cluster M0 (gratuito)
3. Crie um usuário e libere o IP `0.0.0.0/0`
4. Copie a connection string

**Opção B — Docker:**
```bash
docker run -d --name mongodb -p 27017:27017 mongo:7
```

### 3. Configurar o backend
```bash
cd backend/SistemaManutencao.API

# Crie o arquivo de configuração local (não sobe para o git)
# Copie o conteúdo abaixo e ajuste com sua connection string
```

Crie o arquivo `appsettings.Development.json`:
```json
{
  "MongoDbSettings": {
    "ConnectionString": "SUA_CONNECTION_STRING_AQUI",
    "DatabaseName": "SistemaManutencaoDev"
  }
}
```

```bash
dotnet restore
dotnet run
```
API disponível em: `http://localhost:5000`

### 4. Configurar o frontend
```bash
cd frontend
copy .env.example .env   # Windows
# cp .env.example .env   # Linux/Mac

# Abra o .env e ajuste a porta se necessário (padrão: 5000)
npm install
npm run dev
```
Frontend disponível em: `http://localhost:5173`

---

## Credenciais padrão
Na primeira execução, o sistema cria automaticamente:

| Campo | Valor |
|-------|-------|
| E-mail | admin@sistema.com |
| Senha  | Admin@123 |

---

## Estrutura do projeto
```
sistema-manutencao/
├── backend/
│   └── SistemaManutencao.API/
│       ├── Controllers/   # Endpoints da API
│       ├── Models/        # Entidades do domínio
│       ├── Services/      # Regras de negócio
│       ├── Repositories/  # Acesso ao MongoDB
│       └── DTOs/          # Objetos de transferência
└── frontend/
    └── src/
        ├── views/         # Páginas
        ├── components/    # Componentes reutilizáveis
        ├── stores/        # Estado global (Pinia)
        └── services/      # Chamadas à API
```
