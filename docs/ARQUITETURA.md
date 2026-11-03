# Arquitetura proposta

## Plataforma

- C# com .NET para Windows.
- WinForms para a primeira interface, adequado ao fluxo de formulários, seleção de pastas e progresso.
- Aplicação inicialmente distribuída como executável Windows; definir depois se será dependente do runtime instalado ou publicada como self-contained.

## Organização sugerida

Manter a solução simples no início, com responsabilidades separadas:

- **UI**: formulários, validação de entrada, confirmação e apresentação do progresso.
- **Application**: coordenação dos casos de uso (copiar SL, trocar base, preparar, restaurar).
- **Infrastructure**: sistema de arquivos, Registro, provider Firebird ADO.NET, `gbak` para restauração futura e extração.
- **Configuration**: leitura, validação e gravação das opções locais.
- **Logging**: histórico de operações e detalhes técnicos.

Essas responsabilidades podem começar em projetos/pastas de uma única solução e ser divididas em assemblies quando houver necessidade real.

As telas devem seguir os [padrões de interface](PADROES-DE-INTERFACE.md), incluindo o estilo compartilhado para botões, menu lateral e progresso.

## Integrações

### Processos externos

Executar utilitários com `ProcessStartInfo` e argumentos separados, sem concatenar comandos de shell. Ler `stdout`/`stderr`, guardar código de saída, permitir cancelamento quando seguro e sempre aguardar o processo específico iniciado pela aplicação. Não detectar conclusão procurando qualquer processo do mesmo nome no sistema.

Integrações do fluxo:

- Cópia da solicitação feita em C# por fluxos `FileStream` em blocos, com progresso por bytes e cancelamento cooperativo; não depende do `robocopy`.
- SharpCompress (biblioteca gerenciada C#) para leitura e extração RAR, sem chamar WinRAR ou outro executável.
- `ZipArchive`/`FileStream` do .NET para extração ZIP, com cancelamento entre blocos.
- `FirebirdSql.Data.FirebirdClient` (`FbConnection`, `FbCommand`) para conectar diretamente aos arquivos `.FDB`, executar scripts e consultar a versão; depende do serviço Firebird local, não do executável `isql.exe`.
- `gbak.exe` para restauração dos arquivos de backup.

O botão Cancelar cancela as cópias/extrações que suportam `CancellationToken` e encerra a árvore do processo Firebird externo que estiver ativo, aguardando sua saída antes de finalizar o fluxo.

### Registro do Windows

Usar APIs de Registro do .NET, limitadas ao hive do usuário (`HKCU`) e à chave `SIC\CONEXAO`. Antes de gravar, exibir a base escolhida, registrar os valores anteriores e tratar falhas de acesso. Evitar gravar caminhos sem aspas ou validação conforme o formato esperado pela aplicação alvo.

### Configuração

Armazenar caminhos nomeados em JSON na pasta de configuração do usuário, validar na inicialização e oferecer uma tela para corrigir cada item. A senha é opcional; quando o operador marca “Salvar senha”, armazená-la protegida pelo Windows DPAPI para a conta atual, nunca em texto puro. Os scripts SQL existentes podem ser selecionados/configurados por caminho e validados antes da execução.

### Acesso SQL direto

Abrir conexões `FbConnection` com o caminho absoluto do banco da pasta transferida. Processar os scripts com `FbScript`/`FbBatchExecution`; consultar `CONTROLE.NRO_SCRIPT` com `FbCommand` diretamente em `ARQSIST.FDB`. Não invocar `isql.exe` nem registrar a senha em logs ou mensagens. A conexão usa o serviço Firebird local.

## Segurança e integridade operacional

- Validar destino e exibir caminho completo antes de exclusão, substituição ou restauração.
- Nunca reutilizar uma variável de caminho sem normalizá-la e validar a pasta final.
- Evitar depender do diretório corrente do processo; usar caminhos absolutos.
- Não assumir que nome de arquivo identifica conteúdo válido de backup/base.
- Fazer cópias/backup quando a operação puder sobrescrever uma base existente, se o fluxo de negócio permitir.
- Não ocultar erros de processo com redirecionamento para `nul`.
