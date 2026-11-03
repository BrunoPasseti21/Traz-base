# Riscos e decisões em aberto

## Riscos observados no script legado

- **Exclusão de conteúdo:** o fluxo de cópia pode excluir recursivamente o destino existente antes de copiar a solicitação.
- **Credencial embutida:** comandos Firebird usam `SYSDBA` e senha fixa `masterkey`.
- **Caminhos posicionais:** `Caminhos.txt` depende de uma ordem fixa de linhas e de barras finais.
- **Diretório de trabalho:** vários comandos usam caminhos relativos ou `cd`, tornando o resultado dependente do diretório atual.
- **Detecção de processo:** o BAT procura processos pelo nome global; outra instância pode atrasar ou liberar a próxima etapa incorretamente.
- **Erros escondidos:** há redirecionamentos para `nul`, que podem ocultar diagnóstico.
- **Seleção/renomeação:** a entrada digitada é usada em caminhos e nomes sem validação suficiente.
- **Restauração concorrente:** processos `gbak` são iniciados em segundo plano e o fluxo pode precisar verificar cada retorno individualmente.
- **Distribuição:** o arquivo tem marcadores de conversão BAT-to-EXE; confirmar se esse invólucro ainda faz parte do modo de distribuição.

## Decisões a tomar antes da implementação

1. A aplicação será usada somente em Windows e em quais versões?
2. O diretório de origem de SL é sempre `\\192.168.1.34\solicitacoes` ou varia por ambiente?
3. `HKCU\SIC\CONEXAO` é a chave correta em todos os computadores? Quais valores são obrigatórios?
4. A senha `masterkey` é a credencial real de todos os ambientes? Qual mecanismo aprovado deve substituí-la ou protegê-la?
5. Os scripts SQL do BAT devem ser executados em todas as preparações? Como o operador escolhe versões/variações?
6. Confirmar se os arquivos RAR usados no ambiente incluem arquivos multipartes ou protegidos por senha; esses formatos devem ser validados com exemplos reais.
7. Ao existir a pasta da SL, a regra é substituir tudo, mesclar ou criar uma cópia com outro nome?
8. Na restauração, os `.FDB` existentes devem ser preservados, movidos ou substituídos após confirmação?
9. O executável será distribuído por instalador, pasta compartilhada ou atualização interna?

## Recomendação para decisões de risco

Não transportar automaticamente as exclusões e credenciais do BAT para a nova interface. Confirmar a regra de negócio e definir uma ação explícita, contextual e auditável antes de implementar qualquer operação que apague/sobrescreva arquivos ou conecte ao banco.
