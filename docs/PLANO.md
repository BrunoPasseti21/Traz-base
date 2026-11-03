# Plano de implementação

## Fase 1 — Confirmar o comportamento

- Validar com operadores o significado de cada opção do menu legado.
- Confirmar caminhos de origem, destino, apoio e temporário usados atualmente.
- Confirmar quando os scripts SQL devem rodar e quais bases cada um altera.
- Confirmar comportamento esperado para múltiplos arquivos compactados e arquivos `.FBK` não reconhecidos.
- Definir política de credenciais Firebird e sobrescrita/restauração.

## Fase 2 — Fundação da aplicação

- Criar solução WinForms em C#/.NET para Windows.
- Criar configuração nomeada e tela de diagnóstico das dependências.
- Implementar log local e captura de exceções.
- Implementar serviço central de execução de processos com saída, código de retorno, cancelamento e timeout configurável.

## Fase 3 — Operações locais

- Implementar listagem e seleção de bases.
- Implementar validação estrutural de pastas e arquivos esperados.
- Implementar atualização das chaves do Registro com confirmação e relatório do resultado.

## Fase 4 — Cópia e extração

- Implementar cópia nativa de uma SL da rede com progresso por bytes e cancelamento.
- Implementar detecção e seleção de arquivos ZIP/RAR.
- Implementar extração monitorada e validação dos arquivos resultantes.

## Fase 5 — Firebird

- Implementar execução configurável dos scripts SQL por conexão direta com provider ADO.NET do Firebird.
- Implementar consulta de versão.
- Implementar restauração `.FBK` para `.FDB` com `gbak`, incluindo confirmação do destino.
- Apresentar falhas e saída de diagnóstico sem avançar etapas automaticamente.

## Fase 6 — Preparação para uso

- Revisar mensagens e acessibilidade básica da interface.
- Definir publicação/instalação e documentação para operadores.
- Fazer validação manual com cópias descartáveis de bases e ambientes sem cada dependência.
- Comparar os resultados com o script legado antes de aposentar qualquer opção do BAT.

## Entregáveis esperados

- Aplicação Windows e código-fonte.
- Configuração inicial documentada.
- Log de operações e instruções para diagnóstico.
- Guia de instalação/atualização para os computadores operadores.
- Matriz de validação manual por fluxo.
