# Requisitos do projeto

## Objetivo do produto

Substituir o fluxo interativo do `Traz-base-sl.bat` por uma aplicação desktop Windows que ajude a preparar e selecionar bases de solicitações para uso local com Firebird.

## Usuários e ambiente

- Usuário operador de suporte/desenvolvimento em Windows.
- Bases de solicitações acessíveis por compartilhamento de rede.
- Pastas locais para bases de trabalho, bases de apoio e arquivos temporários.
- Serviço Firebird local instalado e em execução. A extração de RAR e ZIP é fornecida pelo próprio aplicativo e suas bibliotecas.

## Requisitos funcionais

### Configuração

- **RF-01** Permitir configurar e validar o diretório local de trabalho das bases.
- **RF-02** Permitir configurar o diretório de bases de apoio.
- **RF-03** Permitir configurar o diretório dos utilitários Firebird e o diretório temporário.
- **RF-04** Permitir configurar o caminho de origem das solicitações na rede.
- **RF-05** Persistir configuração em formato versionável e legível, sem depender da ordem das linhas de `Caminhos.txt`.

### Operações de base

- **RF-06** Buscar/obter uma base informando o número da solicitação e copiar seu conteúdo da origem de rede para o diretório local.
- **RF-07** Mostrar andamento, saída relevante e resultado da cópia.
- **RF-08** Detectar arquivos compactados RAR/ZIP e permitir escolher quais extrair quando houver mais de um.
- **RF-09** Permitir selecionar uma base local existente.
- **RF-10** Trocar os caminhos das bases usados pela aplicação alvo, atualizando as chaves necessárias em `HKCU\SIC\CONEXAO`.
- **RF-11** Redefinir uma base para uma solicitação informada, validando presença dos arquivos Firebird esperados e permitindo identificar/renomear o arquivo de sistema quando necessário.
- **RF-12** Restaurar os arquivos `.FBK` reconhecidos (`ARQEMP`, `ARQOCOR`, `ARQSIST`) para arquivos `.FDB` com `gbak`.
- **RF-13** Executar os scripts SQL configurados diretamente em `ARQEMP.FDB` e `ARQSIST.FDB` usando o provider ADO.NET do Firebird, sem iniciar `isql.exe`.
- **RF-14** Apresentar a versão do script/banco consultada em `CONTROLE.NRO_SCRIPT` quando a operação a solicitar.
- **RF-15** Permitir cancelar operações longas de forma controlada quando o processo externo suportar cancelamento seguro.
- **RF-16** Registrar histórico local das operações, com data/hora, SL/base, etapa e resultado, sem gravar senhas.

## Requisitos não funcionais

- **RNF-01** Interface em português, com nomes e mensagens claros.
- **RNF-02** Operações demoradas não podem congelar a janela; exibir progresso e etapa atual.
- **RNF-03** Validar caminhos, permissões, espaço disponível, ferramentas e arquivos esperados antes de iniciar ações relevantes.
- **RNF-04** Tratar espaços e caracteres especiais em caminhos e argumentos de processos externos.
- **RNF-05** Não apagar ou sobrescrever bases sem informar precisamente o caminho afetado e solicitar confirmação.
- **RNF-06** Manter um log técnico útil para diagnóstico, com segredos e dados sensíveis omitidos.
- **RNF-07** A aplicação deve explicar quando uma ação requer acesso ao compartilhamento ou ao Registro do usuário.
- **RNF-08** Separar interface, regras de operação, configuração e integração com processos/Registro para facilitar manutenção.
- **RNF-09** Se o operador optar por salvar a senha Firebird, protegê-la com DPAPI para o usuário Windows atual; nunca armazená-la em texto puro nem registrá-la em logs.
- **RNF-10** Todos os botões interativos devem seguir o padrão visual documentado em [Padrões de interface](PADROES-DE-INTERFACE.md), usando a mesma paleta, tipografia e estilo plano.

## Fora do escopo inicial

- Alterar os próprios scripts SQL sem decisão explícita do responsável pelo banco.
- Atualização automática do Firebird ou da aplicação alvo.
- Operação remota em outros computadores.
- Gerenciamento de usuários ou senhas do Firebird.

## Critérios gerais de aceite

- O operador consegue configurar e validar dependências sem editar arquivos manualmente.
- Cada operação mostra claramente a base e o caminho que serão usados antes de executar.
- Falhas de cópia, extração, conexão SQL, `gbak` ou Registro aparecem com etapa, código de saída e orientação para diagnóstico.
- Operações destrutivas têm confirmação contextual e não são iniciadas quando a validação prévia falha.
