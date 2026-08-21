# Formato de perfil de dispositivo

Um perfil de dispositivo descreve um modelo de teclado num esquema físico. É um único ficheiro
numa pasta sob `devices/`, chamada `<fabricante>-<modelo>-<esquema>`:

```
devices/razer-deathstalker-v2-de/
└── device.json     geometria e correspondência dos LED
```

`devices/device-profile.schema.json` descreve o mesmo de forma legível por máquina. Nomeá-lo numa
linha `$schema`, como fazem os perfis incluídos, dá à maioria dos editores conclusão e erros em
linha enquanto escreves.

## device.json

```jsonc
{
  "$schema": "../device-profile.schema.json",
  "formatVersion": 1,
  "name": "Razer DeathStalker V2",
  "vendor": "Razer",
  "model": "DeathStalker V2",
  "physicalLayout": "ISO-DE",
  "canvas":  { "width": 439.5, "height": 135.5 },
  "matrix":  { "rows": 6, "columns": 22 },
  "verified": true,
  "keys": [
    { "id": "Keyboard_Escape", "x": 6, "y": 6, "width": 19, "height": 19,
      "row": 0, "column": 1, "label": "esc" }
  ]
}
```

| Campo | Significado |
|---|---|
| `formatVersion` | Revisão do formato. Atualmente `1`. Uma compilação recusa um perfil numerado acima do que compreende. |
| `name` | O que a interface mostra. |
| `vendor`, `model` | Quem o fabrica e que modelo. `"Generic"` para um perfil que descreve um esquema em vez de um produto. |
| `physicalLayout` | `ANSI-US`, `ISO-DE`, `JIS-JP`, `ABNT2-BR` … — a *disposição* física das teclas, não o esquema de software. |
| `canvas` | O sistema de coordenadas a que todas as posições se referem. Só contam as proporções; os perfis incluídos raciocinam em milímetros. |
| `matrix` | Tamanho da matriz de LED do fabricante. Os teclados Razer são 6 × 22, seja qual for o seu tamanho. |
| `verified` | `true` depois de alguém ter confirmado a correspondência em hardware real. |
| `note` | Texto livre opcional para quem abrir o ficheiro a seguir. |
| `image` | Opcional e neste momento não usado — ver [Imagens](#imagens) mais abaixo. |
| `keys[]` | Uma entrada por tecla. |

### Esquema físico, não esquema de software

`physicalLayout` decide a *forma* do teclado: se o Enter é alto e em L, se há uma tecla adicional à
esquerda do `Z`, se a fila inferior traz as teclas japonesas de conversão.

Não diz nada sobre que carateres essas teclas produzem. Isso o Keylegend pergunta ao Windows em
execução, para o esquema ativo. Um perfil ISO-PT serve portanto um teclado português esteja o
Windows em português, em americano ou em Dvorak — daí haver um perfil por esquema *físico* e não um
por idioma.

### Entradas de tecla

| Campo | Significado |
|---|---|
| `id` | Identificador único. Segue a nomenclatura existente: `Keyboard_A`, `Keyboard_Enter`, `Keyboard_NonUsBackslash`, `Keyboard_Num7`. |
| `x`, `y` | Posição do canto superior esquerdo no plano. |
| `width`, `height` | Tamanho da tecla no plano. |
| `row`, `column` | Célula na matriz de LED do fabricante. Ambos `null` enquanto desconhecidos — um estado válido, e é para isso que serve a calibração. |
| `scanCode` | Substitui o código de varrimento padrão. Só é preciso onde o esquema físico contradiz a nomenclatura americana. |
| `parts` | Mais retângulos pertencentes à mesma tecla, para teclas que não são retangulares. |
| `label` | O que está impresso na tecla, para as teclas que não escrevem nada. |
| `labelSecondary` | Uma segunda linha impressa, por baixo da primeira. |

### As legendas pertencem ao teclado

`label` é o que está *impresso na tecla*, não uma tradução do que ela faz. Um teclado alemão diz
`strg`, um francês `ctrl`, um italiano `bloc maiusc` — e cada um o diz seja qual for o idioma dos
menus do Keylegend. Mudar o idioma da interface nunca muda as legendas.

As teclas que produzem um caráter não trazem `label` nenhum. A sua legenda vem do esquema Windows
ativo e segue portanto por si Shift, Caps Lock e Alt Gr.

### Teclas com mais de um retângulo

O Enter ISO é o caso típico: uma tecla que cobre duas filas.

```jsonc
{
  "id": "Keyboard_Enter",
  "x": 267.25, "y": 72.5, "width": 23.75, "height": 19,
  "row": 3, "column": 14,
  "scanCode": 28,
  "parts": [ { "x": 262.5, "y": 53.5, "width": 28.5, "height": 19 } ],
  "label": "enter"
}
```

O retângulo principal traz a célula; `parts` acrescenta o resto da forma. O `scanCode` explícito
está lá porque a metade de cima ocupa a posição que o ANSI reserva à barra invertida: sem ele, o
topo do Enter seria colorido como se escrevesse `\`.

### Códigos de varrimento de teclas exclusivas de um esquema

A tabela padrão em `Keylegend.Core` cobre o que um teclado americano tem. As teclas que só existem
noutros sítios declaram o seu código no perfil, para que não seja preciso mudar C# por causa de um
esquema:

| Identificador | Tecla | `scanCode` |
|---|---|---|
| `Keyboard_JpYen` | `¥`, à esquerda do Backspace no JIS | `0x7D` |
| `Keyboard_JpRo` | `ろ`, à direita do Shift direito no JIS | `0x73` |
| `Keyboard_JpMuhenkan` | `無変換`, à esquerda da barra de espaços | `0x7B` |
| `Keyboard_JpHenkan` | `変換`, à direita da barra de espaços | `0x79` |
| `Keyboard_JpKana` | `かな` | `0x70` |
| `Keyboard_AbntC1` | a tecla `/?` à direita do Shift direito no ABNT-2 | `0x73` |

## Regras que o validador impõe

São verificadas na integração contínua, portanto um perfil que as quebre não pode ser integrado:

- Os identificadores de tecla são únicos
- Não há duas teclas a reivindicar a mesma célula de matriz
- Não há duas teclas a sobrepor-se no plano
- `row` e `column` estão ambos definidos ou ambos a `null`
- As células caem dentro da matriz declarada
- As teclas caem dentro do plano
- Cada tecla tem um tamanho positivo
- Uma imagem nomeada por `image` existe mesmo

## Nomenclatura e a diferença ISO/ANSI

Os identificadores seguem o esquema americano, porque é o que a própria matriz do fabricante faz.
Num teclado alemão o `Z` físico fica portanto em `Keyboard_Y` e vice-versa. Isto diz respeito só ao
nome: nem a posição nem o comportamento dependem disso, porque o caráter real é perguntado ao
Windows em execução.

Dois identificadores só existem em teclados ISO:

| Identificador | Tecla | Célula Razer |
|---|---|---|
| `Keyboard_NonUsBackslash` | a tecla adicional à esquerda do `Y`/`Z` (`<`, `>`, `\|`) | `RZKEY_EUR_2`, fila 4 coluna 2 |
| `Keyboard_NonUsTilde` | a tecla ao lado do Enter na fila central (`#`, `'`) | `RZKEY_EUR_1`, fila 3 coluna 13 |

Nos teclados ISO o Enter alto abrange duas posições de matriz: a metade de cima onde o ANSI tem a
barra invertida (fila 2, coluna 14), a de baixo em `Keyboard_Enter` (fila 3, coluna 14).

**Se ambas se acendem mesmo depende do modelo.** A tabela do fabricante descreve o que a matriz
consegue *endereçar*, não o que um dado teclado tem *montado*. Na DeathStalker V2, a calibração
mostrou que a célula de cima não aciona LED nenhum — o Enter inteiro é iluminado pela de baixo, e é
por isso que o perfil incluído modela o Enter como uma tecla com dois retângulos e não como duas
teclas.

É exatamente o género de coisa que nenhuma documentação permite deduzir, e a razão pela qual um
perfil não deve ser marcado `verified` enquanto alguém não o tiver percorrido em hardware.

## Imagens

`image` é opcional e neste momento não é usado: a pré-visualização no ecrã é desenhada a partir da
geometria acima. Desenhá-la mantém a pré-visualização nítida em qualquer tamanho de janela e torna
impossível que imagem e perfil se contradigam.

Se ainda assim juntares uma, tem de ser uma imagem que **tu** tiraste ou criaste. Todo este
repositório sai sob a licença MIT, que concede a qualquer pessoa o direito de modificar e
redistribuir o que ele contém — um direito que ninguém pode conceder sobre a fotografia de produto
de um fabricante de teclados. Ver [NOTICE.md](../../NOTICE.md).

## Ver também

- [Adicionar ou corrigir um teclado](adding-a-keyboard.md) — o percurso prático
