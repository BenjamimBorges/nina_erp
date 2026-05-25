# 🚀 Guia de Compilação Local - ERP Nina

## ✅ Modificações Realizadas

### 1. **Desktop / UI Login** (ERP.Desktop)
- **MainWindow.xaml**: Interface XAML bem estruturada
  - Campo Username (TextBox vinculado)
  - Campo Senha (PasswordBox)
  - Botão Entrar com click handler
  - TextBlock para mensagem de status
  - Dimensões: 520x360, centralizada

- **MainViewModel.cs**: Lógica MVVM com INotifyPropertyChanged
  - Properties: Username, Password, StatusMessage com validação
  - Método `LoginAsync()` com chamada HTTP à API
  - Tratamento de exceções e feedback visual
  - URL API: `https://localhost:7029/api/auth/login` (para desenvolvimento local)

### 2. **Backend / API** (ERP.API)
- **appsettings.json**: Conecta ao PostgreSQL
  - `Host=db;Port=5432;Database=nina_erp;Username=erpuser;Password=erp_password`
  - **⚠️ Nota**: Para desenvolvimento local, mudar para `Host=localhost`

- **Program.cs**: Configuração de DI completa
  - DbContext com UseNpgsql
  - Repositories registrados (User, Client, Product, Stock)
  - UnitOfWork e AuthService configurados
  - Swagger habilitado em Development

---

## 📦 Instalação do .NET SDK

### Linux (Ubuntu/Debian)
```bash
# Adicionar repositório Microsoft
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update

# Instalar .NET 8 SDK
sudo apt-get install -y dotnet-sdk-8.0
```

### Verificar instalação
```bash
dotnet --version
# Esperado: 8.0.x ou superior
```

---

## 🔨 Compilação Local

### 1. **Criar arquivo appsettings.Development.json**

```bash
cat > /home/benborges/Documentos/projetos/erp/nina_erp/ERP.API/appsettings.Development.json << 'EOF'
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=nina_erp;Username=erpuser;Password=erp_password"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
EOF
```

### 2. **Restaurar dependências**
```bash
cd /home/benborges/Documentos/projetos/erp/nina_erp
dotnet restore
```

### 3. **Compilar solução**
```bash
# Debug (para desenvolvimento)
dotnet build

# Release (otimizado)
dotnet build -c Release
```

### 4. **Executar API localmente**
```bash
cd ERP.API
dotnet run
# Esperado: "Application started. Press Ctrl+C to shut down."
# API disponível em: https://localhost:5001 ou http://localhost:5000
```

### 5. **Executar Desktop (em outro terminal)**
```bash
cd ERP.Desktop
dotnet run
# Janela de login abre com UI XAML
```

---

## 🗄️ Banco de Dados LOCAL

### Opção 1: PostgreSQL em Docker (recomendado)
```bash
docker run -d \
  --name nina_erp_db \
  -e POSTGRES_DB=nina_erp \
  -e POSTGRES_USER=erpuser \
  -e POSTGRES_PASSWORD=erp_password \
  -p 5432:5432 \
  postgres:16
```

### Opção 2: PostgreSQL instalado localmente
```bash
sudo apt-get install -y postgresql postgresql-contrib
# Criar banco e usuário
sudo -u postgres psql << EOF
CREATE DATABASE nina_erp;
CREATE USER erpuser WITH PASSWORD 'erp_password';
GRANT ALL PRIVILEGES ON DATABASE nina_erp TO erpuser;
EOF
```

### Aplicar schema SQL
```bash
psql -U postgres -d nina_erp < ERP.Database/migrations/001_initial_schema.sql
# OU com usuário erpuser (após GRANT ALL):
psql -U erpuser -d nina_erp -h localhost < ERP.Database/migrations/001_initial_schema.sql
```

---

## 🌐 Endpoints Disponíveis (Desenvolvimento Local)

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| POST | `http://localhost:5000/api/auth/login` | Autenticação |
| GET | `http://localhost:5000/api/health` | Status |
| GET | `http://localhost:5000/api/clients` | Listar clientes |
| POST | `http://localhost:5000/api/clients` | Criar cliente |
| PUT | `http://localhost:5000/api/clients/{id}` | Atualizar cliente |
| DELETE | `http://localhost:5000/api/clients/{id}` | Deletar cliente |
| GET | `http://localhost:5000/swagger/ui` | **Documentação Swagger** |

---

## 🔧 Estrutura do Projeto

```
NinaERP.sln
├── ERP.Core/                 # Entidades, interfaces, enums
├── ERP.Shared/               # DTOs, modelos compartilhados
├── ERP.Infrastructure/       # DbContext, repositories, services
├── ERP.API/                  # ASP.NET Core REST API
├── ERP.Desktop/              # WPF (Windows only)
└── ERP.Database/             # Migrations SQL
```

### Dependências Principais
- **Target**: .NET 8.0
- **ORM**: Entity Framework Core 8.0 + Npgsql
- **UI Desktop**: WPF (Windows Presentation Foundation)
- **API**: ASP.NET Core
- **Documentação**: Swashbuckle.AspNetCore (Swagger)

---

## 🐛 Troubleshooting

### Erro: "Host is down"
```
Solução: Garantir que PostgreSQL está rodando
docker start nina_erp_db
# ou
sudo systemctl start postgresql
```

### Erro: "Build failed"
```bash
# Limpar cache de build
dotnet clean
dotnet restore
dotnet build
```

### Desktop não inicia
```
Nota: WPF só funciona no Windows. 
Em Linux, use apenas a API e teste via Swagger.
```

---

## ✨ Próximos Passos

1. **Instalar .NET 8 SDK**
2. **Criar appsettings.Development.json** com host=localhost
3. **Iniciar PostgreSQL** (Docker ou local)
4. **Executar `dotnet restore` && `dotnet build`**
5. **Rodar API**: `cd ERP.API && dotnet run`
6. **Testar Swagger**: http://localhost:5000/swagger/ui
7. **Compilar Desktop** (se Windows): `cd ERP.Desktop && dotnet run`

---

## 📝 Notas Importantes

- ⚠️ **appsettings.json** atual é para Docker (`Host=db`)
- ✅ **appsettings.Development.json** será local (`Host=localhost`)
- 🔐 Senhas em texto plano são apenas para desenvolvimento
- 🖥️ Desktop (WPF) requer Windows ou WSL2
- 📊 API funciona em qualquer plataforma (Windows, Linux, macOS)
