# ArenaBook

Sistema de reservas de quadras esportivas desenvolvido para o trabalho de **Testes Automatizados**, da disciplina de Gestão da Qualidade.

O objetivo é aplicar testes automatizados em regras de negócio utilizando C# e NUnit.

## Funcionalidades

- Cadastrar, consultar, editar e excluir clientes, preservando o histórico de reservas.
- Cadastrar, consultar, editar e desativar quadras.
- Vincular modalidades esportivas às quadras.
- Configurar os horários de funcionamento.
- Registrar reservas e calcular o valor pela duração × preço por hora.
- Impedir reservas sobrepostas na mesma quadra, independentemente da modalidade.
- Consultar a agenda, cancelar reservas e sugerir horários disponíveis.

As reservas devem começar e terminar em horas inteiras, no mesmo dia e dentro do funcionamento. Horários consecutivos são permitidos.

## Tecnologias

- C# e .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- NUnit
- Swagger/OpenAPI

## Organização do projeto

```text
ArenaBook.Api             Endpoints, contratos e configuração da aplicação
ArenaBook.Core            Entidades, interfaces e regras de negócio
ArenaBook.Infrastructure  Acesso ao banco e mapeamentos do Entity Framework
ArenaBook.Test            Testes unitários e de integração
database                  Script SQL do banco
```

O fluxo das operações segue:

```text
Controller → Service → Repository → SQL Server
```

O Controller recebe as requisições, o Service aplica as regras de negócio e o Repository acessa o banco.

## Executar a aplicação

### Requisitos

- SDK do .NET 10.
- SQL Server disponível.
- Banco criado com o script `database/ArenaBook.sql`.

Para uma instalação nova, execute o script no SQL Server. Se o banco já existir, não execute novamente os comandos de criação.

Na pasta que contém `ArenaBook.slnx`, configure a conexão nos User Secrets da API, substituindo os valores do exemplo:

```powershell
dotnet user-secrets set "ConnectionStrings:ArenaBook" "Server=SEU_SERVIDOR;Database=ArenaBook;Integrated Security=True;TrustServerCertificate=True" --project ArenaBook.Api
```

A conexão deve usar a autenticação disponível no seu SQL Server. Não coloque credenciais em arquivos versionados.

Compile e inicie a API:

```powershell
dotnet restore ArenaBook.slnx
dotnet build ArenaBook.slnx
dotnet run --project ArenaBook.Api --launch-profile http
```

Acesse o Swagger em:

```text
http://localhost:5095/swagger
```

## Testes automatizados

O projeto utiliza **NUnit** e possui **12 testes: 10 unitários e 2 de integração**.

Os testes unitários usam repositórios em memória para verificar as regras sem acessar o banco. Os testes de integração executam requisições à API e utilizam o SQL Server existente.

| Teste | Cenário | Resultado esperado |
|---|---|---|
| R01 | Reservar das 18h às 20h por R$ 80/h | Reserva ativa com valor de R$ 160 |
| R02 | Reservar vôlei das 19h às 21h em quadra ocupada por futsal das 18h às 20h | Recusa e preservação da reserva original |
| R03 | Cancelar uma reserva ativa | Horário liberado e histórico preservado |
| R04 | Solicitar sugestões de uma hora em uma quadra parcialmente ocupada | Retorno apenas dos intervalos livres |
| Q01 | Cadastrar quadra com nome, modalidades e preço válidos | Cadastro realizado com os dados informados |
| Q02 | Cadastrar quadra com preço zero | Recusa sem inclusão |
| U01 | Cadastrar cliente com dados válidos e consultar pelo identificador | Retorno do cliente cadastrado |
| U02 | Cadastrar cliente com nome em branco | Recusa sem inclusão |
| H01 | Configurar domingo das 8h às 22h | Consulta retorna o funcionamento cadastrado |
| H02 | Alterar funcionamento para fechar antes de abrir | Recusa e preservação da configuração anterior |
| I01 | Criar reserva pela API e consultar a agenda | Reserva exibida com identificador e valor corretos |
| I02 | Cancelar reserva pela API e consultar a agenda | Reserva removida dos horários ocupados e preservada no histórico |

Cada teste prepara as entradas, executa a operação e utiliza asserções para conferir os resultados ou as exceções esperadas.

### Executar todos os testes

```powershell
dotnet build ArenaBook.slnx
dotnet test ArenaBook.slnx --no-build
```

### Executar apenas os testes unitários

```powershell
dotnet test ArenaBook.slnx --no-build --filter "TestCategory=Unit"
```

### Executar apenas os testes de integração

```powershell
dotnet test ArenaBook.slnx --no-build --filter "TestCategory=Integration"
```

I01 e I02 precisam do SQL Server disponível e da conexão configurada nos User Secrets da API. Eles iniciam a API automaticamente, criam dados fictícios exclusivos e removem esses dados ao finalizar. Não criam banco, tabelas ou migrations.

## Versionamento

O projeto segue Gitflow:

- `main`: versão estável.
- `develop`: integração das funcionalidades.
- `feature/*`: desenvolvimento de funcionalidades.
- `release/*`: preparação da versão final.
- `hotfix/*`: correções emergenciais da versão estável.

As funcionalidades são desenvolvidas em branches próprias e encaminhadas por Pull Request para revisão e integração.
