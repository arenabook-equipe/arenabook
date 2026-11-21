# Testes do ArenaBook

O projeto possui 12 testes NUnit: 10 unitarios e 2 de integracao.

## Como entender um teste

Cada teste segue tres passos:
1. Preparar: montar os dados necessarios para o cenario.
2. Executar: chamar a operacao que estamos verificando.
3. Conferir: usar Assert.That ou Assert.ThrowsAsync para comparar o resultado com o esperado.

Um Assert e uma verificacao. Um teste pode ter varios Asserts sobre o mesmo cenario.
Nao contamos cada Assert como um teste.

## Testes unitarios (sem banco)

| Arquivo | Teste | O que comprova |
| --- | --- | --- |
| Unit/ReservaServiceTests.cs | R01 | Duas horas a R$80 resultam em R$160 e reserva Pendente, que e um estado ativo. |
| Unit/ReservaServiceTests.cs | R02 | Futsal das 18h as 20h impede volei das 19h as 21h na mesma quadra; a reserva original continua Confirmada. |
| Unit/ReservaServiceTests.cs | R03 | Cancelar libera a disponibilidade e preserva a reserva no historico. |
| Unit/ReservaServiceTests.cs | R04 | As sugestoes respeitam o funcionamento e a ocupacao, permitindo horarios consecutivos. |
| Unit/QuadraServiceTests.cs | Q01 | Cadastro valido salva uma quadra com as modalidades e o preco informados. |
| Unit/QuadraServiceTests.cs | Q02 | Preco zero e recusado e nenhuma quadra e salva. |
| Unit/ClienteServiceTests.cs | U01 | Cadastro valido salva o cliente e permite consulta pelo identificador retornado. |
| Unit/ClienteServiceTests.cs | U02 | Nome obrigatorio em branco e recusado e nenhum cliente e salvo. |
| Unit/HorarioFuncionamentoServiceTests.cs | H01 | Cadastrar domingo das 8h as 22h permite consultar esses horarios. |
| Unit/HorarioFuncionamentoServiceTests.cs | H02 | Alteracao com fechamento anterior a abertura e recusada, preservando o funcionamento anterior. |

Os repositorios Fake ou EmMemoria guardam os dados em listas durante o teste.
Eles substituem o acesso ao banco para testar apenas as regras do Service.
Eles nao comprovam o funcionamento do SQL Server ou do Repository real.

## Testes de integracao

Arquivo: Integration/ReservaApiTests.cs.

- I01 cria uma reserva por HTTP e consulta a agenda, conferindo identificador, estado e valor.
- I02 cria e cancela por HTTP, consulta a agenda e confere no banco que a reserva continua salva como Cancelada.

O fluxo executado e: requisicao HTTP -> Controller -> Service -> Repository -> SQL Server.
WebApplicationFactory inicia a API dentro do teste. Nao e necessario abrir a API no Visual Studio.
O pacote Microsoft.AspNetCore.Mvc.Testing fornece essa ferramenta; nao altera o funcionamento da aplicacao.

Preparar, marcado com SetUp, cria dados ficticios exclusivos antes de cada teste.
Limpar, marcado com TearDown, remove apenas os dados desse teste ao terminar, inclusive se uma verificacao falhar.
A preparacao e a limpeza usam transacoes e respeitam as chaves estrangeiras.

I01/I02 usam o banco existente e ConnectionStrings:ArenaBook dos User Secrets da API.
O SQL Server precisa estar disponivel e configurado na maquina de quem executar.
Nao sao criados banco, tabelas ou migrations. As credenciais nao ficam no codigo.
Se a preparacao falhar, o teste falha; ele nao e marcado como aprovado ou ignorado.

## Executar

Na pasta da Solution:

~~~powershell
dotnet build ArenaBook.slnx
dotnet test ArenaBook.slnx --no-build
~~~

Somente os 10 unitarios:

~~~powershell
dotnet test ArenaBook.slnx --no-build --filter "TestCategory=Unit"
~~~

Somente I01/I02:

~~~powershell
dotnet test ArenaBook.slnx --no-build --filter "TestCategory=Integration"
~~~

No Visual Studio, use o Gerenciador de Testes para executar todos ou selecionar um teste.

## Limite desta selecao

Os 12 testes cobrem os cenarios escolhidos para o trabalho, nao todas as regras e combinacoes do sistema.
A reducao removeu os cenarios adicionais, os testes de configuracao/mapeamento e o teste de exemplo.
As verificacoes manuais anteriores complementam esta selecao, mas nao sao novos testes NUnit.

