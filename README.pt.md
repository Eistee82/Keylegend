# Keylegend

**Iluminação de teclado interativa para Razer Chroma — as tuas teclas acendem-se conforme o que realmente fazem.**

[English](README.md) · [Deutsch](README.de.md) · [Español](README.es.md) · [Français](README.fr.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md) ·
[Polski](README.pl.md) · [Português](README.pt.md) · [Русский](README.ru.md) · [Українська](README.uk.md) · [简体中文](README.zh-cn.md)

> **Versão 1.0.0.** A iluminação, a interface, a deteção de jogos e os perfis de aplicação
> funcionam. [Descarrega o instalador ou a cópia portátil](https://github.com/Eistee82/Keylegend/releases/latest),
> ou compila a partir do código. Ver [CHANGELOG.md](CHANGELOG.md).

![O Keylegend colore as teclas conforme o que significam naquele momento e muda de perfil quando outra aplicação passa para primeiro plano](docs/images/keylegend.png)

---

## O que faz

A maior parte do software RGB trata o teclado como decoração. O Keylegend trata-o como um
**mostrador**.

Cada tecla é colorida conforme o que significa *naquele momento* — e essa cor muda no instante em
que o seu significado muda:

- **Os bloqueios num relance.** Num Lock, Caps Lock e Scroll Lock mostram o seu estado na própria
  tecla.
- **Uma cor por classe de caráter.** Algarismos, minúsculas, maiúsculas, símbolos e teclas de
  controlo têm cada um a sua cor.
- **Mantém um modificador e vês a camada.** Carrega em `Alt Gr` e só ficam acesas as teclas que
  realmente têm um caráter Alt Gr. Carrega em `Windows` e acendem-se os atalhos do Windows,
  agrupados por função. O mesmo para `Alt`, `Ctrl` e as suas combinações.
- **Shift e Caps Lock funcionam sozinhos.** Como o caráter produzido por cada tecla é perguntado
  ao Windows em direto, as letras passam por si da cor «minúscula» para a cor «maiúscula». O
  teclado numérico muda para as cores de navegação quando o Num Lock está desligado.
- **Os jogos têm tratamento próprio.** São detetados automaticamente — inclusive em janela sem
  margens — e WASD, as teclas à volta e a linha dos números assumem cores fixas: a jogar, o que
  conta é onde vão as mãos, não que letra uma tecla escreve.
- **Perfis por aplicação, cerca de noventa incluídos.** Photoshop, Visual Studio Code, Excel,
  Elden Ring e os restantes aplicam-se assim que o programa tem o foco, e um perfil que nomeia um
  programa prevalece sobre o perfil de jogo geral. Edita um e só a parte editada deixa de seguir
  a versão incluída; o resto continua a melhorar com as versões seguintes.
- **Devolve a iluminação.** Passado um período de inatividade configurável (60 s por
  predefinição), o Keylegend liberta o teclado e o teu efeito do Chroma Studio volta a assumir.
- **Onze idiomas.** Inglês, alemão, espanhol, francês, italiano, neerlandês, polaco, português,
  russo, ucraniano e chinês simplificado. A interface segue o idioma de visualização do Windows e
  pode ser mudada nas definições. As legendas das teclas não são afetadas: seguem o teu teclado,
  não os menus.

Como o significado das teclas vem do **esquema de teclado ativo do Windows** e não de uma tabela
fixa, o Keylegend funciona com qualquer esquema — português, alemão, americano, Dvorak — sem
alterações.

## Como funciona

O Keylegend pergunta ao Windows que caráter cada tecla produziria no estado atual do teclado
(`ToUnicodeEx`), deriva daí uma categoria e envia o mapa de cores resultante para o SDK Razer
Chroma através da sua interface REST local.

Deliberadamente **não** instala qualquer hook global de teclado. Lê apenas o *estado* dos
modificadores e dos bloqueios; nunca interceta, reencaminha ou regista uma tecla premida. Ver
[docs/pt/architecture.md](docs/pt/architecture.md).

## Requisitos

- Windows 10 ou 11
- Razer Synapse com o serviço Chroma SDK em execução
- Um teclado compatível com Chroma que tenha um perfil de dispositivo (ver abaixo)
- O runtime .NET 10

## Instalação

```powershell
winget install Eistee82.Keylegend
```

É o caminho mais curto: o winget traz o runtime .NET como dependência declarada, portanto não fica
nada por instalar à mão. Caso contrário, escolhe um ficheiro:

[**Descarregar a versão mais recente.**](https://github.com/Eistee82/Keylegend/releases/latest)

| Ficheiro | O que é |
|---|---|
| `Keylegend-1.0.0-setup.exe` | Instala para o utilizador atual — sem direitos de administrador. Entrada no menu Iniciar, e uma desinstalação que remove também a entrada de arranque automático. |
| `Keylegend-1.0.0-portable.zip` | O mesmo programa, para extrair. Mantém a pasta `devices` ao lado do executável. |

Nenhum está assinado, por isso o Windows dirá que o editor é desconhecido — um certificado custa
por ano mais do que este projeto tem. Cada versão traz `SHA256SUMS.txt` para verificar a
transferência, e o registo de compilação que a produziu é público.

## Teclados suportados

O suporte de um teclado é **dados, não código**. Um teclado é um ficheiro em `devices/`:
`device.json`, com a geometria das teclas e a correspondência entre teclas e células da matriz
Chroma.

São incluídos trinta e dois perfis. Um deles foi percorrido em hardware real; os restantes são
gerados a partir das dimensões normalizadas, o que torna a sua geometria exata e a sua
correspondência de LED uma suposição fundamentada.

| Teclado | Esquema | Estado |
|---|---|---|
| Razer DeathStalker V2 | ISO-DE | **verificado no hardware** |
| Razer DeathStalker V2, BlackWidow V4, Huntsman V3 Pro, Ornata V3 | ANSI-US, ISO-DE | gerado |
| Formato completo, 105/104 teclas | ANSI-US, ISO-DE, ISO-UK, ISO-FR, ISO-ES, ISO-IT, ISO-NORDIC, ISO-PT, ISO-CH, ISO-RU, ISO-PL, JIS-JP, ABNT2-BR | gerado |
| Tenkeyless | ANSI-US, ISO-DE, ISO-UK, ISO-FR | gerado |
| 75 %, 65 %, 60 % | ANSI-US, ISO-DE | gerado |

`physicalLayout` descreve a *forma* do teclado, não o idioma em que escreves. Que caráter uma
tecla produz é perguntado ao Windows no momento, por isso um perfil ISO-PT serve um teclado
português quer o Windows esteja em português, em americano ou em Dvorak.

**Acendem-se as teclas erradas?** É exatamente isso que «gerado» significa, e corrigi-lo não exige
programação — cerca de dez minutos com o modo de calibração. Ver
[docs/pt/adding-a-keyboard.md](docs/pt/adding-a-keyboard.md). As correções são tão bem-vindas
como os perfis novos e transformam uma suposição num perfil `verified` para todos os que tenham
esse teclado.

## Documentação

| Tema | |
|---|---|
| Arquitetura | como a coloração é decidida, e porque não existe qualquer hook de teclado |
| Adicionar ou corrigir um teclado | perfis de dispositivo, calibração, e que fazer quando se acendem as teclas erradas |
| Adicionar um perfil | coloração por aplicação |
| Formato de perfil de dispositivo | cada campo, em detalhe |
| Configuração | definições, ficheiro de definições, arranque automático |

Disponível em onze idiomas:

[English](docs/en/) · [Deutsch](docs/de/) · [Español](docs/es/) · [Français](docs/fr/) ·
[Italiano](docs/it/) · [Nederlands](docs/nl/) · [Polski](docs/pl/) · [Português](docs/pt/) ·
[Русский](docs/ru/) · [Українська](docs/uk/) · [简体中文](docs/zh-cn/)

O inglês e o alemão são os originais mantidos; onde uma tradução os contradiga, é o texto inglês
que está certo. As correções são bem-vindas, ver [CONTRIBUTING.md](CONTRIBUTING.md).

## Compilar e executar

```bash
git clone https://github.com/Eistee82/Keylegend.git
cd keylegend
dotnet build
dotnet test
```

Saem dois programas. **`Keylegend.exe`** (`src/Keylegend.App`) é a aplicação: janela, ícone na
área de notificação, definições. É o que queres para uso normal.

**`keylegend-cli.exe`** (`src/Keylegend.Host`) é um controlador de consola com os diagnósticos:

| Comando | O que faz |
|---|---|
| `keylegend-cli` | Executa a iluminação. Assume ao primeiro toque, devolve após 10 s de inatividade. |
| `keylegend-cli --idle 30` | O mesmo, com um tempo de inatividade de 30 segundos. |
| `keylegend-cli --once 10` | Pinta o estado atual uma vez e mantém-no dez segundos. Boa primeira verificação. |
| `keylegend-cli --calibrate` | Acende uma tecla de cada vez para verificar um perfil de dispositivo. |
| `keylegend-cli --dump-layout` | Imprime aquilo em que cada tecla resulta: normal / Shift / Alt Gr. |
| `keylegend-cli --watch-foreground` | Relata o que a deteção de jogos vê à medida que as janelas mudam. |
| `keylegend-cli --profile <caminho>` | Usa um `device.json` específico. |

As definições residem em `%APPDATA%\Keylegend\settings.json` e são escritas pela aplicação.

## Contribuir

Relatos de erros, perfis de dispositivo e traduções são todos bem-vindos — ver
[CONTRIBUTING.md](CONTRIBUTING.md) e [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).

## Licença

[MIT](LICENSE). Excetuam-se dois botões de donativo de terceiros, e aqui não há código,
cabeçalhos, bibliotecas nem material gráfico de qualquer fabricante — ver [NOTICE.md](NOTICE.md).

## Aviso de marcas

Este projeto **não está afiliado à Razer Inc., nem é por ela apoiado ou patrocinado.**

RAZER e RAZER CHROMA são marcas comerciais ou marcas registadas da Razer Inc. São aqui usadas
unicamente para identificar o hardware e a interface de software com que este projeto trabalha,
tal como o uso referencial permite. O Keylegend é um projeto independente, mantido pela
comunidade.

O mesmo se aplica a qualquer outro nome neste repositório. Os perfis de aplicação e de jogo
nomeiam cerca de noventa programas — Photoshop, Visual Studio Code, Excel, Elden Ring e outros —
e os perfis de dispositivo nomeiam fabricantes e modelos de teclado. São marcas dos respetivos
titulares e aparecem apenas para dizer a que programa ou a que teclado algo se destina. O
Keylegend não está associado a nenhum deles e não contém nem o seu código nem os seus materiais.
Ver [NOTICE.md](NOTICE.md).
