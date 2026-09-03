# Configuração

As definições residem em `%APPDATA%\Keylegend\` e editam-se pela interface. No primeiro arranque é
escrita uma configuração predefinida completa.

## Cores

Uma cor por categoria:

| Categoria | Aplica-se a |
|---|---|
| Algarismo | `1`, `7`, e o teclado numérico enquanto o Num Lock está ligado |
| Minúscula | `a`, `ã` |
| Maiúscula | `A`, `Ã` |
| Símbolo | `+`, `#`, `€`, `\|`, e os operadores do teclado numérico |
| Tecla de controlo | Esc, Tab, Enter, Backspace, modificadores, setas, bloco de navegação, e o teclado numérico enquanto o Num Lock está desligado |
| Tecla de função | F1 a F12 |
| Tecla morta | `^`, `´`, `` ` `` — teclas que precisam de uma segunda pressão para produzir um caráter |
| Sem atribuição | teclas sem significado no contexto atual; apagadas por omissão. A tecla central do teclado numérico com o Num Lock desligado é o exemplo mais claro |

As teclas de bloqueio têm duas cores cada uma — uma para ligada, outra para desligada.

## Conjuntos de atalhos

Um conjunto de atalhos associa teclas a **grupos de funções** e é escolhido conforme os
modificadores mantidos premidos. Conjuntos incluídos: `Win`, `Win+Shift`, `Win+Ctrl`, `Alt`,
`Ctrl`, `Ctrl+Shift`, `Ctrl+Alt`.

Cada grupo tem a sua cor, de modo que comandos relacionados se leem como um bloco — por exemplo a
edição (`X`/`C`/`V`/`Z`/`Y`/`A`) numa cor e as operações com ficheiros (`N`/`O`/`S`/`P`/`W`)
noutra.

Os atalhos do Windows estão fixados a nível de sistema e são por isso sempre exatos. Os atalhos de
Ctrl variam entre programas; o conjunto incluído cobre as convenções habituais do Windows.

## Perfis de aplicação

Um perfil descreve o que o teclado deve mostrar enquanto um determinado programa está à frente.
Vêm cerca de noventa com a aplicação — programas como o Photoshop, o Visual Studio Code ou o Excel,
e jogos como o Elden Ring ou o Counter-Strike 2. Aplicam-se sozinhos: assim que a janela
correspondente tem o foco o perfil entra em vigor, e quando o foco passa adiante voltam os
conjuntos predefinidos. Onde nenhum perfil corresponde, nada muda.

O reconhecimento é pelo nome do executável. Quando corresponde mais do que um perfil, ganha o que
nomeia o programa — um jogo com perfil próprio mantém-no portanto mesmo que a deteção de jogos
também dispare. A prioridade só desempata o que sobra.

Um perfil é sobreposto ao conjunto geral, entrada por entrada. O Photoshop diz o que ali significa
`Ctrl+J`; `Ctrl+C` continua a copiar, porque um perfil que nomeia a camada Ctrl não está a afirmar
que Ctrl não significa mais nada. E `Win+E` continua a abrir o Explorador, porque o Windows atribui
essa combinação a todo o sistema e ela vale independentemente do que está à frente.

### O que um perfil contém

| Secção | Conteúdo |
|---|---|
| Correspondência | A que programas o perfil se aplica: nomes de executáveis, se cobre os jogos detetados em geral, e a prioridade |
| Destaques | Teclas fixadas numa cor independentemente do caráter que produzem — WASD num jogo, as teclas de ferramenta de um editor de imagem |
| Atalhos | Substituições de camadas de modificadores individuais: que tecla leva que comando sob `Ctrl`, colorida por grupo de funções |

Destaques e atalhos trazem também uma etiqueta que diz o que o comando faz — «Duplicar camada»,
«Saltar». Nada disso é visível no teclado; os LED só mostram cor. A etiqueta aparece na
pré-visualização dentro da aplicação, e a noventa perfis é a única maneira de verificar se uma
entrada está sequer certa.

### Editar e repor

As três secções são substituídas separadamente. Edita os destaques de um perfil incluído e a partir
daí os destaques são teus: ficam congelados e deixam de seguir a versão incluída. A correspondência
e os atalhos continuam a segui-la e apanham as melhorias que uma nova versão traz.

Só a secção que mudaste é guardada, sob o identificador do perfil — nunca uma cópia do perfil
inteiro. É precisamente por isso que a reposição existe, e por isso que uma atualização ainda pode
melhorar um perfil que editaste em parte.

A reposição funciona portanto também por secção: devolver os atalhos mantendo os teus próprios
destaques é possível. Repor o perfil inteiro recupera todas as secções, mais um nome alterado e um
estado oculto.

Os perfis incluídos podem ser **ocultados mas não eliminados**. Vivem dentro do ficheiro do
programa; eliminar um duraria apenas até ao arranque seguinte. Um perfil oculto é saltado quando se
escolhe um perfil, mas mantém-se na lista e pode voltar a ser mostrado.

### Os teus próprios perfis

Um perfil que crias tu é guardado por inteiro em `settings.json`, porque não há nada com que o
comparar. Por isso não pode ser reposto, apenas eliminado. De resto comporta-se como um incluído:
as mesmas três secções, a mesma regra de escolha.

Se um perfil devesse valer para toda a gente e não só para ti, o lugar dele é no projeto, como
ficheiro — ver [Adicionar um perfil](adding-a-profile.md).

### Formato do ficheiro de definições

`settings.json` traz `formatVersion` 3. Ficheiros mais antigos são migrados ao carregar.

Um ficheiro da versão 1 não conhece identificadores nem a proveniência de um perfil, e por isso não
pode dizer quais das suas entradas são as incluídas. Todas passam a perfis de utilizador. Nada se
perde, mas os perfis incluídos aparecem ao lado, portanto pode haver ao início duas entradas para o
mesmo programa; a que sobra pode ser eliminada ou ocultada.

Um ficheiro da versão 2 lista todas as cores, incluindo as que ninguém tocou, e com isso fixa a
paleta: uma cor de origem melhorada não chega a ninguém que já tenha executado o programa. Uma cor
igual à paleta dessa versão é por isso lida como valor predefinido e descartada na migração; todo o
resto é a sua escolha e mantém-se.

## Comportamento

| Definição | Significado |
|---|---|
| Devolver a iluminação quando inativo | Se é devolvida sequer. Desligado, o Keylegend mantém o teclado até o pausares ou fechares — e toma-o no arranque em vez de esperar por uma tecla. |
| Período de inatividade | Segundos sem atividade de teclado antes da devolução. 60 por omissão — recuperá-la custa um a dois segundos, portanto um período curto transforma isso numa interrupção constante. O valor é mantido enquanto a devolução está desligada. |
| Brilho | Fator global de 0 a 100 %, aplicado a cada cor enquanto o fotograma é composto. |
| Usar perfis de aplicação | Se os perfis são sequer consultados. Desligado, os conjuntos predefinidos valem em todo o lado, esteja o que estiver à frente. |
| Arrancar com o Windows | Regista a aplicação na chave `Run`, com a opção `--minimized`. Arrancado assim, o Keylegend aparece na área de notificação: sem janela, sem balão. Arrancado à mão mostra sempre a janela. Uma entrada escrita por uma versão anterior é atualizada no arranque seguinte. |
| Efeito ao escrever | Como a iluminação responde a uma tecla premida, *nenhum* por predefinição. Um de cada vez; os oito estão descritos abaixo. Sem efeito, o Keylegend nunca olha para que teclas concretas estão premidas, apenas se alguém está a escrever. |

### Efeitos ao escrever

Cada efeito é uma curva sobre o tempo decorrido desde que uma tecla foi premida ou largada,
sobreposta à imagem terminada em vez de misturada na decisão sobre o que uma tecla significa: as
cores continuam a dizer o que diziam, e o teclado na janela mostra o mesmo que o da secretária.
Um efeito que clareia uma tecla fá-lo misturando branco, até ao branco puro na intensidade
máxima — cada cor incluída já leva um canal a 255, por isso não há um azul mais brilhante. Os
efeitos que viajam recebem a distância de um canto do teclado ao outro, de modo que uma onda
atravessa o teclado inteiro, seja ele qual for.

| Efeito | O que acontece |
|---|---|
| Desvanecer | A tecla premida apaga-se enquanto é mantida e recupera a sua cor num segundo depois de largada. |
| Clarão | A tecla premida fica branca na intensidade máxima e cai de imediato na sua própria cor, em menos de um quinto de segundo. |
| Brilho residual | A tecla premida mantém-se brilhante enquanto é mantida e apaga-se ao longo de quase um segundo depois de largada — o rasto que a escrita deixa. |
| Impacto | A tecla premida acende-se, e as teclas à volta, até duas alturas e meia de tecla, respondem um instante depois, as mais afastadas ainda mais tarde — como se a tecla tivesse abanado o teclado. Termina num quinto de segundo. |
| Gota de água | Um anel de luz estreito parte da tecla premida para fora e desvanece-se pelo caminho; atravessa o teclado em menos de um segundo. |
| Onda escura | O mesmo anel, escuro: o teclado afasta-se à volta da tecla em vez de se acender com ela. |
| Faíscas | Uma tecla premida lança até três faíscas para teclas próximas, nunca para a própria tecla premida. Brilham quentes e apagam-se em meio segundo. Onde caem é acaso. |
| Calor | As teclas aquecem a cada toque e voltam a arrefecer, perdendo metade do calor a cada quatro segundos; uma tecla usada muitas vezes brilha mais quente do que uma premida uma vez. O único efeito que guarda algo entre toques, e guarda-o apenas em memória: um número por tecla que decai e desaparece assim que a tecla está fria. |

A escolha fica em `settings.json` sob `Effect`, por nome — `None`, `Fade`, `Flash`,
`Afterglow`, `Impact`, `Ripple`, `DarkWave`, `Sparks` ou `Heat`. Um nome que o programa não
conhece significa nenhum efeito.

## Idioma

A interface segue o idioma de visualização do Windows e está disponível em onze: inglês, alemão,
espanhol, francês, italiano, neerlandês, polaco, português, russo, ucraniano e chinês
simplificado. **Definições → Idioma** substitui isso; a mudança tem efeito imediato, sem
reiniciar.

Cada idioma nomeia-se a si próprio nessa lista em vez de ser traduzido. Traduzi-la significaria que
cada um dos onze levasse dez nomes para os outros, e quem encontrasse a interface num idioma que
não sabe ler teria de procurar o seu num idioma que também não sabe ler.

A escolha é guardada em `settings.json` sob `language` como `Automatic`, `English`, `German`,
`Spanish`, `French`, `Italian`, `Dutch`, `Polish`, `Portuguese`, `Russian`, `Ukrainian` ou
`ChineseSimplified`. Um valor desconhecido recai em `Automatic` em vez de recusar arrancar, que é o
que um ficheiro editado à mão com toda a probabilidade quer.

O que está traduzido são os menus e as explicações. Duas coisas **não** estão, ambas de propósito:

- **As legendas das teclas** no teclado representado. Vêm do desenho da Razer e têm de corresponder ao teclado à tua frente, não ao idioma dos menus — um teclado ISO alemão mostra
  `strg` e `entf`, esteja a interface em inglês ou não.
- **Os nomes dos modificadores** (Shift, Ctrl, Alt, Alt Gr, Num Lock …). Esses mesmos nomes são
  produzidos pela maquinaria dos atalhos para as listas de camadas, que fica fora da tradução; meia
  tradução leria-se pior do que nenhuma.

Tudo o que não tem tradução recai no inglês, portanto um ficheiro de idioma por acabar custa as
linhas que lhe faltam e não a interface inteira.

## Se o Synapse ainda não está em execução

Ao iniciar sessão, o software da Razer e o Keylegend arrancam ao mesmo tempo, e a descrição do
teclado ligado só existe quando a parte da Razer estiver terminada. O Keylegend não toma isso por
uma falha. O seu ícone está na área de notificação antes sequer de olhar, e depois continua a olhar:
de dois em dois segundos enquanto nenhum teclado é nomeado, e com uma pausa crescente até meio
minuto enquanto só falta o desenho. A iluminação começa sozinha assim que há algo para iluminar.

Um arranque a partir da lista de arranque do Windows não abre nenhuma janela para isto: o teclado à
sua frente mostra se funciona, e entretanto di-lo a dica na área de notificação. Um arranque à mão
mostra uma pequena janela assim que o primeiro olhar sai vazio, dizendo o que falta e quando tentou
pela última vez. Fechar essa janela não muda nada — a procura continua e o Keylegend permanece na
área de notificação.

## Se a iluminação não funciona

A conversa com o serviço Chroma pode falhar: o serviço está parado, o Synapse foi fechado, outro
programa detém a sessão. O Keylegend continua a tentar, com uma pausa crescente entre as
tentativas, e enquanto o faz diz o que está errado:

- a linha de estado no fundo da janela traz o motivo, em âmbar em vez do cinzento habitual
- a área de notificação di-lo na sua dica, para que uma janela fechada não o esconda
- um balão anuncia-o, uma vez por falha e não uma vez por tentativa

Os três desaparecem assim que passa de novo um fotograma. Se não aparecer nada e o teclado continuar
sem acender, o programa não está em execução: procure o seu ícone na área de notificação.

## Se se acendem as teclas erradas

O teclado na janela é o teclado na mesa: ambos são preenchidos pelo mesmo código, portanto a janela
mostra como o hardware deve estar. A verificação é ter os dois lado a lado.

A que célula da matriz de iluminação pertence uma tecla é a única coisa que nem o Synapse nem o
desenho indicam: vem da tabela do próprio protocolo Chroma. Se no hardware se acender uma tecla
diferente da que está acesa na janela, essa tabela está errada para o seu modelo. Vale a pena abrir
uma questão a dizer que teclado e que tecla.
