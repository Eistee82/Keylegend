# Adicionar um perfil

Um perfil de aplicação é **dados, não código**. Não precisas de C# nem de ferramentas de
compilação — um editor de texto e conhecimento real do programa chegam, e essa segunda parte é a
mais difícil.

Se só queres um perfil para ti, faz-o na interface: fica guardado em `settings.json` e não precisa
de nada disto. Um ficheiro sob `profiles/` é a forma de um perfil seguir com a aplicação para toda
a gente.

## 1. Criar o ficheiro

```
profiles/apps/<id>.json      programas
profiles/games/<id>.json     jogos
```

O nome do ficheiro tem de ser igual ao `id` lá dentro. Minúsculas, `a-z0-9-`. A compilação embute
por carateres universais todos os ficheiros destas duas pastas, portanto não há qualquer ficheiro
de projeto para editar.

Um identificador é permanente. As substituições do utilizador e as entradas de perfis ocultos
agarram-se a ele, por isso renomeá-lo numa versão posterior deixa órfãs as edições de alguém.
Escolhe um nome que continue certo depois de o programa mudar de marca — `adobe-photoshop`, não
`photoshop-2026`.

## 2. Preenchê-lo

Os campos, as três secções, os grupos de funções, as combinações de modificadores e as convenções
de cor estão descritos em [profiles/FORMAT.md](../../profiles/FORMAT.md). Lê isso primeiro; é a
referência e esta página não a repete.

O que se segue é a parte que corre mal mesmo depois de o formato ter sido lido.

## 3. Posições e carateres não são a mesma coisa

Os identificadores de tecla vêm do perfil de dispositivo e nomeiam **posições americanas**.
`Keyboard_Y` é a tecla física que escreve `Y` num teclado americano — num alemão, essa tecla
escreve `Z`. O formato tem portanto duas maneiras de nomear uma tecla, e escolher a errada produz
um perfil visivelmente errado em qualquer esquema não americano, parecendo perfeito na máquina onde
foi escrito.

A pergunta a fazer em cada entrada é sobre o que ela é realmente:

- **Onde está a mão → posição.** Um destaque para WASD é sobre a forma que os teus dedos fazem, não
  sobre as letras. `Keyboard_W`, `Keyboard_A`, `Keyboard_S`, `Keyboard_D` são as teclas certas em
  toda a parte.
- **Qual é o comando → caráter.** `Ctrl+Z` quer dizer «a tecla que escreve z». Escrito como
  posição, anular e refazer aparecem trocados num teclado alemão.
- **Teclas que não escrevem nada → posição outra vez.** Esc, Tab, Enter, Backspace, as setas e as
  teclas de função não têm caráter, por isso `shortcuts.keys` nomeia-as por identificador sem
  ambiguidade.

### Para os destaques, depende de como o programa lê o teclado

QWERTZ e QWERTY diferem em exatamente dois sítios, por isso `Keyboard_Y` e `Keyboard_Z` são os
únicos identificadores onde isto pode correr mal. E corre mal em silêncio.

O identificador de um destaque é sempre uma **posição física**. A questão é que tecla física o
programa quer dizer, e isso decorre de como ele lê o teclado:

| O programa liga-se ao | Exemplos | `Z` na documentação dele significa |
|---|---|---|
| **caráter** (códigos de tecla virtuais do Windows, que seguem o esquema) | Photoshop, Blender, GIMP, Krita — as aplicações em geral | `Keyboard_Y` — a tecla da fila de cima, que escreve `Z` num teclado alemão |
| **posição** (códigos de varrimento, como a maioria dos motores de jogo, para que WASD não saia do sítio) | os jogos em geral | `Keyboard_Z` — a tecla da fila de baixo |

Se não conseguires estabelecer de que maneira um dado programa lê o teclado, deixa de fora as
entradas `Y` e `Z`. Todas as outras letras ficam a salvo.

## 4. Deixa de fora aquilo de que não tens a certeza

Um atalho errado é pior do que um em falta. Uma entrada em falta deixa uma tecla apagada e não
custa nada; uma errada faz o teclado afirmar algo falso, e o utilizador não tem maneira de saber que
é falso. A etiqueta torna a afirmação explícita — não a torna correta.

Portanto:

- Escreve apenas aquilo de que tens a certeza que é a atribuição **predefinida** do programa,
  acabado de instalar. A tua instalação não é uma fonte; provavelmente mudaste coisas e
  esqueceste-te.
- Confirma na documentação do programa, ou no próprio programa com as definições intactas.
- Onde os valores predefinidos diferem entre versões, segue a atual.
- Não inventes. Se um programa não tem um atalho bem conhecido para alguma coisa, não leva entrada.

Doze atalhos corretos valem mais do que trinta dos quais quatro estão errados. O mesmo vale para as
etiquetas dos destaques: se não souberes dizer o que uma tecla faz, isso é sinal de que a entrada
ainda não pertence ao perfil.

## 5. Testar

```bash
dotnet test
```

Os testes de perfis verificam cada ficheiro sob `profiles/`: o identificador é único e coincide com
o nome do ficheiro, `kind` coincide com a pasta, cada identificador de tecla existe num perfil de
dispositivo incluído, as cores são interpretáveis, os grupos e as combinações de modificadores são
válidos e escritos na forma canónica, cada atalho traz etiqueta, nenhuma tecla de letra está sob
`shortcuts.keys` (o lugar dela é `characters`), nenhum perfil está vazio, e não há dois perfis a
reivindicar o mesmo executável sem se distinguirem por `titleContains`.

Uma coisa **não** é verificada de propósito: a mesma etiqueta a aparecer duas vezes sob um mesmo
modificador. Parecia uma maneira de apanhar descuidos de copiar e colar e apanhava, em vez disso,
verdadeiros sinónimos — os navegadores fecham um separador tanto com `Ctrl+W` como com `Ctrl+F4`.
Uma verificação que dispara com dados corretos é pior do que nenhuma.

O que nenhum teste consegue verificar é se um atalho é *verdadeiro*. É para isso que serve a
revisão, e a razão pela qual cada entrada traz uma etiqueta para rever.

## 6. Experimentar contra o programa

Inicia o Keylegend, traz o programa para primeiro plano e mantém premidos os modificadores que o
teu perfil define. A pré-visualização mostra o mesmo que o teclado, portanto para isto basta um
portátil sem hardware Chroma. Compara com os menus do próprio programa — um comando cuja etiqueta
não encontres no programa é a primeira coisa a retirar.

## 7. Abrir uma pull request

Indica contra que programa e que versão verificaste, e como confirmaste as atribuições: a
documentação do programa, o próprio programa, ou ambos. Ver
[CONTRIBUTING.md](../../CONTRIBUTING.md).

Um perfil pequeno e seguro é uma boa contribuição. Um grande e meio recordado não é.
