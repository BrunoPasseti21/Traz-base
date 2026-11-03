# Padrões de interface

## Botões

Todos os botões interativos da aplicação devem seguir o mesmo padrão visual de **Cancelar operação**. Não criar botões de ação como links sublinhados nem introduzir cores ou tipografias diferentes sem atualizar este documento.

- Fundo azul `#1F6FEB` e texto branco.
- Fonte **Segoe UI Semibold**, tamanho **10 pt**.
- Estilo plano (`FlatStyle`) e sem borda (`BorderSize = 0`).
- Altura de 42 px para ações principais e altura compacta proporcional ao controle para ações auxiliares; manter a mesma cor, fonte e tratamento.
- Usar estados de foco, hover e desabilitado legíveis, sem trocar a identidade visual do botão.
- Botões de navegação com ícones seguem a mesma paleta. A opção selecionada recebe destaque azul e as demais usam o estado neutro da navegação.

Aplica-se, por exemplo, a **Trazer e preparar base**, **Cancelar operação**, **Limpar**, **Procurar** e às ações das caixas de diálogo. Em telas novas, reutilizar este padrão em vez de definir um estilo local diferente.

## Menu lateral

- Recolhido por padrão, mostra os ícones das opções.
- Expandido pelo botão sanduíche, mostra ícone e descrição.
- Manter a fonte das descrições em 12 pt e indicar claramente a opção selecionada.

## Progresso e status

- Ocupar espaço na tela somente enquanto houver status ou progresso a apresentar.
- A barra deve respeitar os limites do painel e refletir o percentual real.
- Manter o texto do status próximo à barra e com contraste suficiente.
