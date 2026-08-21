# Adicionar ou corrigir um teclado

O suporte de um teclado é **dados, não código**. Não precisas de C# nem de ferramentas de
compilação — um editor de texto e o teu próprio teclado chegam.

A maioria de quem chega aqui não tem nada a acrescentar, porque já existe um perfil para o seu
esquema. O que falta a esses perfis é a única coisa que não se pode gerar: alguém com o hardware
que confirme que cada tecla se acende onde o perfil afirma. **É esse o trabalho descrito na
[parte 2](#2-corrigir-um-perfil), e leva uns dez minutos.**

---

## O que um perfil sabe, e com que segurança

Um perfil responde a duas perguntas distintas, e não são igualmente fiáveis:

| Pergunta | De onde vem a resposta | Quão segura |
|---|---|---|
| Onde fica cada tecla e que tamanho tem? | A grelha normalizada de 19,05 mm, que todos os teclados seguem desde o IBM Model M | **Certa.** A geometria decorre do esquema. |
| Que célula da matriz de LED acende essa tecla? | A matriz publicada pelo fabricante, presumindo um teclado padrão | **Uma suposição.** Os modelos mudam teclas de sítio, deixam células por povoar e acrescentam as suas. |

Essa separação é toda a razão de ser do indicador `verified`. Um perfil marcado
`"verified": false` está quase de certeza certo quanto ao desenho e pode muito bem estar errado
quanto à tecla que se acende.

---

## 1. Acrescentar um esquema em falta

Verifica primeiro se falta mesmo: `devices/` já contém perfis de formato completo para ANSI-US,
ISO-DE, ISO-UK, ISO-FR, ISO-ES, ISO-IT, ISO-NORDIC, ISO-PT, ISO-CH, ISO-RU, ISO-PL, JIS-JP e
ABNT2-BR, além de variantes tenkeyless, 75 %, 65 % e 60 %. Se o teu está entre eles, passa à
parte 2.

### O caminho gerado

`tools/make-layout.py` constrói um perfil a partir das dimensões normalizadas. Acrescentar-lhe um
teclado é uma entrada na lista `PROFILES`, no fim do ficheiro:

```python
("generic-fullsize-iso-tr", dict(
    name="Full-size keyboard (Turkish)", vendor="Generic", model="Full-size 105-key",
    physical_layout="ISO-TR", form_factor="fullsize", variant="iso", legends="en")),
```

| Argumento | O que decide |
|---|---|
| `form_factor` | `fullsize`, `tkl`, `75`, `65`, `60`, `fullsize-macro` |
| `variant` | `ansi`, `iso`, `jis` ou `abnt2` — a forma do Enter e que teclas adicionais existem |
| `legends` | Que conjunto de legendas impressas usar: `en`, `de`, `fr`, `es`, `it` |
| `right` | `win` ou `fn` — o que fica entre o Alt direito e a tecla de menu |

Depois executa-o:

```bash
python tools/make-layout.py --only iso-tr
```

Se as legendas do teu teclado não estiverem entre os cinco conjuntos, acrescenta um: copia
`LEGENDS_EN` no mesmo ficheiro, traduz as entradas e regista-o em `LEGEND_SETS`. Só as teclas que
*não* escrevem nada precisam de legenda — as restantes são perguntadas ao Windows em execução, e é
isso que faz um perfil servir todos os esquemas de software no mesmo hardware.

### O caminho manuscrito

Para um teclado que não seja uma variação de um esquema padrão — ortolinear, dividido, com uma
fila de teclas de macro que mais ninguém tem — escreve `device.json` diretamente. A
[descrição do formato](device-profile-format.md) enumera cada campo, e
`devices/device-profile.schema.json` dá à maioria dos editores conclusão e erros em linha.

A primeira passagem não precisa de ser exata. Põe as teclas mais ou menos certas, deixa `row` e
`column` a `null` onde tiveres dúvidas, e deixa a calibração fazer o resto.

---

## 2. Corrigir um perfil

Esta é a parte que precisa do hardware, e a parte que realmente importa.

### Primeiro olhar

Antes de tocares no teclado, examina o desenho:

```bash
python tools/preview-layout.py devices/generic-fullsize-iso-pt/device.json
```

Isso escreve `preview.svg` ao lado do perfil; abre-o em qualquer navegador. Compara-o com o
teclado à tua frente e procura:

- teclas em falta, ou teclas desenhadas que o teu teclado não tem
- um Enter com a forma errada — alto e em L no ISO, largo e plano no ANSI
- uma fila inferior com o número errado de modificadores, que varia mais do que tudo o resto
- **contornos vermelhos**, que assinalam teclas sem célula de matriz. Essas nunca se acenderão.

Corrigir a geometria é aritmética, não adivinhação: a grelha é uma unidade por tecla, e uma unidade
é a `width` que as teclas de letra vulgares têm.

### Depois calibrar

A calibração acende uma tecla de cada vez e nomeia-a, para que possas confirmar que a tecla que
brilha a branco é a que o perfil afirma. É a única maneira de ter certeza: tudo o resto é
inferência a partir de uma tabela do fabricante.

```bash
keylegend-cli --profile devices/<a-tua-pasta>/device.json --calibrate
```

Percorre as teclas mapeadas por ordem de leitura:

| Tecla | O que faz |
|---|---|
| `Enter` ou `→` | esta está certa, seguir para a próxima |
| `F` | acendeu-se a tecla errada — registar |
| `←` | uma tecla atrás |
| `A` | acender todas as teclas mapeadas ao mesmo tempo |
| `S` | saltar para o resumo |
| `Q` ou `Esc` | parar |

Como os identificadores seguem o esquema americano, a indicação mostra também o que cada tecla
escreve de facto na *tua* máquina — num teclado português fala-se-te portanto da «tecla ç» e não
de `Keyboard_SemicolonAndColon`.

Os achados são escritos em `calibration-findings.txt` à medida que avanças, não no fim. Calibrar é
trabalho paciente e uma janela fechada não te pode custar isso.

Enquanto trabalhas ajuda um segundo desenho — este rotula cada tecla com a célula que reivindica em
vez da sua legenda:

```bash
python tools/preview-layout.py devices/<a-tua-pasta>/device.json --cells
```

### Aplicar o que encontraste

`tools/apply-calibration.ps1` escreve-o de volta no perfil, guardando uma cópia `.bak`:

```powershell
tools/apply-calibration.ps1 `
  -ProfilePath devices/<a-tua-pasta>/device.json `
  -Unlit Keyboard_Backslash,Keyboard_PauseBreak `
  -Remap "Keyboard_Enter=3,14"
```

`-Unlit` é para as teclas que não acenderam absolutamente nada: a matriz consegue endereçar a
célula, mas este modelo não tem lá LED. Essas teclas mantêm a sua geometria — a tecla existe, e a
pré-visualização deve desenhá-la — e perdem `row`/`column`, para que nada seja enviado para o
vazio. `-Remap` é para as teclas mapeadas na célula errada.

### O que esperar

Estes são os sítios onde um perfil gerado erra com mais frequência:

| Onde | O que acontece |
|---|---|
| **O Enter ISO** | Abrange duas células. Em muitos teclados só a de baixo tem LED, e a metade de cima é iluminada pela vizinha ou por nada. |
| **A fila inferior** | O número e a largura dos modificadores diferem entre modelos. Os teclados de jogo põem `Fn` onde os de escritório têm uma segunda tecla Windows. |
| **Teclas de macro e multimédia** | Muitas vezes na coluna 0 ou nas colunas exteriores, e muitas vezes em célula nenhuma. |
| **Teclados compactos** | A matriz mantém os seus 6 × 22 completos; um teclado de 60 % simplesmente deixa a maior parte vazia. As células não são renumeradas. |
| **As teclas altas do teclado numérico** | Mais e Enter cobrem duas filas mas respondem a uma só célula, normalmente a de cima. |

Uma tecla que se revele sem LED mantém a sua geometria e perde a sua célula:

```jsonc
{ "id": "Keyboard_Function", "x": 234, "y": 120, "width": 24, "height": 19,
  "row": null, "column": null }
```

Continua a ser desenhada, para que a pré-visualização corresponda ao hardware; simplesmente nunca
se acende. Isso está correto, não é um defeito.

### Marcar como verificado

Quando cada célula bater certo, passa `-MarkVerified` ao mesmo script, ou põe `"verified": true` à
mão, e retira a `note` que diz que o perfil foi gerado. Esse indicador é o que diz à próxima pessoa
com o teu teclado que pode confiar nele.

---

## 3. Testar

```bash
dotnet test
```

Os testes dos perfis incluídos validam todos os perfis sob `devices/`, incluindo o teu. Apanham
identificadores duplicados, duas teclas a reivindicar o mesmo LED, teclas desenhadas umas por cima
das outras, células fora da matriz e geometria que escorregou para fora do plano.

## 4. Abrir uma pull request

Indica que teclado e que esquema físico verificaste, e se percorreste a calibração. Ver
[CONTRIBUTING.md](../../CONTRIBUTING.md).

Perfis com `"verified": false` também são bem-vindos — dão vantagem à próxima pessoa com esse
teclado. Uma correção a um perfil existente vale tanto como um perfil novo.

### Sobre imagens

O campo `image` é opcional e neste momento não é usado: a pré-visualização é desenhada a partir da
geometria, o que a mantém nítida em qualquer tamanho e a impede de contradizer o perfil. Se ainda
assim juntares uma, tem de ser uma imagem que **tu** fotografaste ou desenhaste. Um render de
produto de um fabricante não pode ser publicado sob a licença MIT deste projeto, e a uma pull
request que traga um será pedido que o remova.

## Ver também

- [Formato de perfil de dispositivo](device-profile-format.md) — cada campo, em detalhe
- [Arquitetura](architecture.md) — porque o significado das teclas vem do Windows e não de uma tabela
