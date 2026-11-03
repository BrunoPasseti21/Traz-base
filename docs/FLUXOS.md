# Fluxos de operação

Este documento traduz os caminhos principais do script legado em fluxos de interface. A sequência final deve ser confirmada com quem usa o processo no dia a dia.

## Tela inicial

Apresentar ações principais:

1. Trazer base por número de SL.
2. Trocar a base ativa.
3. Redefinir/preparar base para uma SL.
4. Restaurar base a partir de backup.
5. Configurações e diagnóstico de dependências.

Mostrar a base atualmente selecionada, o caminho e o estado das dependências configuradas.

## Trazer base por SL

1. Usuário informa o número da solicitação.
2. Aplicação valida o número e confirma origem/destino.
3. Aplicação verifica se o destino já existe; se existir, informa o conteúdo que será substituído e pede confirmação específica.
4. Copia a pasta da origem de rede para o destino, mostrando andamento e permitindo consultar o log.
5. Detecta arquivos RAR/ZIP. Se houver vários, mostra uma lista selecionável; se houver um, permite extraí-lo diretamente após confirmação/configuração.
6. Valida arquivos da base e encaminha o usuário para selecionar ou preparar a base.

## Trocar base

1. Lista as bases existentes no diretório configurado.
2. Usuário seleciona uma base e confere o caminho completo.
3. Aplicação valida o diretório e atualiza os valores configurados em `HKCU\SIC\CONEXAO`.
4. Apresenta o resultado de cada valor alterado e opção para abrir a pasta.

## Redefinir/preparar base

1. Usuário informa a SL ou seleciona uma pasta já copiada.
2. Aplicação valida arquivos necessários (`ARQEMP.FDB`, `ARQSIST.FDB`, `ARQOCOR.FDB`) e informa os ausentes.
3. Se não houver `ARQSIST.FDB`, mostra arquivos candidatos para o operador escolher; não renomeia automaticamente um arquivo arbitrário.
4. Se arquivos de apoio precisarem ser copiados, informa origem e destino e pede confirmação.
5. Executa os scripts SQL habilitados, em ordem definida, exibindo saída e código de retorno de cada chamada.
6. Consulta e mostra a versão quando prevista para o fluxo.

## Restaurar base

1. Usuário escolhe a pasta da base.
2. Aplicação lista arquivos `.FBK` encontrados na subpasta `BKP` e identifica possíveis destinos pelo nome.
3. Para cada backup reconhecido, apresenta origem, destino `.FDB` e opção de continuar.
4. Executa `gbak`, acompanha o processo e registra seu resultado.
5. Após restauração bem-sucedida, oferece atualizar a base ativa no Registro.

## Estados comuns de uma operação

`Aguardando entrada → Validando → Confirmando alterações → Executando → Concluída ou Falhou/Cancelada`

Cada etapa deve produzir uma mensagem visível e um registro técnico. Ao falhar, a aplicação deve preservar a saída útil do processo e não avançar silenciosamente para a etapa seguinte.
