# Arquitetura

## A ideia central

Toda a lógica de decisão é um **cálculo puro**, sem acesso ao Windows, à rede ou ao sistema de
ficheiros:

```
(estado do teclado, teclado ligado, perfil de aplicação, definições de cor) → cor por tecla
```

Daqui decorrem duas coisas, e ambas explicam por que o desenho tem esta forma:

1. A pré-visualização no ecrã e o teclado real são preenchidos pelo **mesmo código**. O que vês na
   janela é o que se acende.
2. A lógica é inteiramente testável sem teclado ligado e sem o Synapse instalado.

Tudo o que fala com o mundo exterior vive em adaptadores finos à volta desse núcleo.

## Projetos

| Projeto | Contém | Pode depender de |
|---|---|---|
| `Keylegend.Core` | o teclado ligado, categorias, conjuntos de atalhos, o compositor de fotogramas, a máquina de estados da sessão | nada específico de plataforma |
| `Keylegend.Windows` | estado do teclado, resolução de carateres, janela em primeiro plano | APIs do Windows |
| `Keylegend.Chroma` | cliente REST para o SDK Chroma, batimento | rede |
| `Keylegend.Engine` | o ciclo que lê o teclado, compõe um fotograma e o envia | Core, Chroma, Windows |
| `Keylegend.App` | interface WPF, ícone na área de notificação, armazenamento da configuração | tudo o que está acima |

`Keylegend.Core` nunca pode referenciar os outros. Se uma alteração parecer exigi-lo, é a
abstração que está no sítio errado.

## Ler o estado do teclado

O Keylegend **não** instala qualquer hook global de teclado. Um hook desses é funcionalmente um
registador de teclas, coloca-se na cadeia de entrada e é regularmente sinalizado pelos sistemas
anti-batota.

Em vez disso, o estado das teclas que interessam é consultado (`GetAsyncKeyState` para os
modificadores premidos, `GetKeyState` para os bloqueios) cerca de sessenta vezes por segundo, e só
se compõe um novo fotograma quando algo mudou. Nenhuma tecla premida é alguma vez intercetada,
reencaminhada, registada ou guardada.

### Modificadores esquerdo e direito

O Windows reporta **Alt Gr como Ctrl mais Alt direito**, e nos esquemas alemães Ctrl + Alt esquerdo
produz os mesmos carateres que Alt Gr. Distinguem-se pelo lado:

- **Alt direito** → camada Alt Gr, que mostra a atribuição de carateres
- **Ctrl + Alt esquerdo** → o conjunto de atalhos `Ctrl+Alt`

As variantes esquerda e direita têm portanto de ser avaliadas separadamente
(`VK_LMENU`/`VK_RMENU`, e assim por diante).

## Determinar o que uma tecla significa

Em vez de trazer consigo uma tabela de esquemas, o Keylegend pergunta ao Windows que caráter uma
tecla produziria no estado atual do teclado (`ToUnicodeEx`), e deriva a categoria do caráter
obtido.

É por isso que Shift, Caps Lock e Num Lock não precisam de tratamento especial: a mesma tecla
devolve simplesmente `A` em vez de `a` e cai por si na categoria «maiúscula». E é também por isso
que qualquer esquema de teclado funciona sem alterações.

### Que teclado está ligado

Pergunta-se ao Razer Synapse, porque já sabe. Escreve uma descrição de cada dispositivo ligado em
`…\Razer Chroma SDK\Devices\<guid>.json`: o modelo pelo nome, a disposição física como número, o
tamanho da matriz e o código de varrimento de cada tecla que o hardware realmente tem.
`SdkDeviceDescription` lê isso, e nada do teclado é deduzido.

O aspeto do teclado vem da mesma instalação. A interface do Synapse é uma aplicação web, e os
desenhos que carrega para um dispositivo ficam na sua cache: retângulos de teclas com nome, a forma
da caixa com a roda de volume e a faixa multimédia, e os contornos dos caracteres impressos nas
teclas. `SvgLayoutSource` encontra o do modelo e da disposição ligados de forma exata e não pela
forma, porque cada desenho é entregue ao lado de um objeto de configuração que nomeia ambos.

Só se tomam medidas e contornos; as cores e o estilo da Razer são ignorados, e nada desse material
é copiado para este repositório.

A única coisa que nenhum dos dois indica é a que célula da matriz de iluminação pertence uma tecla.
Isso é `StandardKeyMatrix`, a tabela `RZKEY` do próprio protocolo, idêntica em cada modelo.

## Perfis de aplicação

Um perfil liga regras de iluminação a um programa. Vêm incluídos cerca de noventa, e vale a pena
enunciar as decisões por detrás deles, porque nenhuma delas é a resposta óbvia.

### Os perfis são dados, não código

A mesma regra do suporte a dispositivos: acrescentar um perfil é acrescentar um ficheiro JSON sob
`profiles/`, e a compilação apanha-o por carateres universais. Ninguém tem de tocar em C# para
ensinar um programa ao Keylegend, o que significa que um perfil pode ser contribuído, revisto e
corrigido por alguém que apenas conhece o programa. Se suportar uma nova aplicação alguma vez
exigisse código, o formato estaria errado.

### Embutidos no assembly em vez de soltos no disco

Os perfis de aplicação são compilados no assembly em vez de ficarem como ficheiros ao lado do
executável. Três razões, e cada uma bastaria por si. Uma versão em ficheiro único leva-os consigo
sem pasta que se possa perder. Nada no disco pode ser editado por acidente, e é precisamente isso
que dá sentido a «repor a versão incluída» — a versão incluída tem de estar fora de alcance para
valer a pena voltar a ela. E um perfil que não compila torna-se um erro de compilação em vez de um
programa que ficou silenciosamente sem perfis.

### As substituições são por secção

A edição de um utilizador nunca é guardada como cópia do perfil. É guardada como uma substituição
indexada pelo identificador do perfil, contendo apenas as secções tocadas. Decorrem duas coisas:
repor é sequer possível, e uma compilação atualizada ainda pode melhorar um perfil que alguém
editou em parte. O identificador sustenta isto e nunca pode mudar depois de publicado: renomeá-lo
deixa órfãs as edições de alguém.

A granularidade aguenta contra as duas alternativas óbvias:

- **Por campo** parece mais arrumado e produz estados que ninguém configurou. Recoloca a cor de
  `W`, aceita depois uma atualização que acrescenta `Q`, e o resultado é uma mistura que o
  utilizador nunca construiu e não sabe explicar.
- **Por perfil** é a falha oposta. Renomeia uma coisa e o perfil fica congelado para sempre; nunca
  mais vê uma correção.

Uma secção é a granularidade à qual a alteração ainda cabe numa frase: editaste os destaques, logo
os destaques passam a ser teus.

### Um perfil é sobreposto ao conjunto geral, entrada por entrada

Os atalhos são indexados por combinação de modificadores, e as entradas de um perfil assentam sobre
as gerais em vez de tomarem o seu lugar — entrada por entrada, não camada por camada. O Photoshop
sabe o que significa `Ctrl+J` dentro do Photoshop; não sabe nada sobre `Win+E`, que o Windows
atribui a todo o sistema, nem sobre `Ctrl+C`, que vale em qualquer lugar onde haja um cursor de
texto.

Por camada significaria que um perfil que nomeia `Ctrl` para os seus próprios comandos leva a camada
inteira consigo, e a área de transferência é o que isso custa: copiar, colar, cortar, desfazer e
selecionar tudo apagam-se num navegador, num programa de conversação, num terminal — programas em
que pouco mais se faz do que escrever e colar. Por entrada, quem nomeia uma tecla vence para essa
tecla e nada mais se move. Esvaziar uma camada inteira é de propósito impossível.

Um perfil que não nomeia nenhuma camada devolve o catálogo geral inalterado; o caso comum não
reserva, portanto, nada.

### Atalhos e destaques trazem uma etiqueta

A etiqueta diz o que o comando faz — «Duplicar camada», não «Ctrl+J». O hardware nunca a mostra:
os LED trazem cor e mais nada, portanto a etiqueta não custa nada em execução. Paga-se três vezes
noutros sítios. A pré-visualização dentro da aplicação pode mostrá-la, um teste pode encontrar
contradições entre entradas, e a noventa perfis é a única maneira de alguém rever se uma entrada
está certa. `"j": "Editar"` não pode ser confrontado com nada; `"j": "Duplicar camada"` pode.

### Migrar um ficheiro de definições em formato 1

Um ficheiro em formato 1 guarda os perfis inteiros, sem identificador e sem registo da sua
proveniência. Uma substituição precisa de um identificador a que se agarrar, e repor precisa de
saber que existe uma versão incluída à qual voltar: um ficheiro assim não pode, por isso, dizer
quais das suas entradas são as incluídas.

Por isso todas passam a perfis de utilizador. Isso preserva cada edição que alguém fez, ao preço de
o perfil incluído aparecer ao lado da cópia migrada até que um dos dois seja removido — a troca
certa, porque a outra leitura apaga trabalho em silêncio.

### Migrar um ficheiro de definições em formato 2

Um ficheiro em formato 2 lista todas as cores, incluindo as que ninguém tocou, e não pode por isso
dizer quais das suas entradas são decisões e quais valores predefinidos devolvidos. Acatá-las todas
fixa a paleta: uma cor incluída melhorada não chega então a ninguém que já tenha executado o
programa.

O formato 3 escreve apenas o que difere da paleta incluída, pelo que uma entrada no ficheiro
significa que alguém a escolheu. Migrar um ficheiro mais antigo obriga a adivinhar essa distinção, e
o pressuposto é: uma entrada igual à paleta dessa versão é um valor predefinido, qualquer outra é
uma escolha. `PaletteBeforeFormat3` guarda essa paleta como cópia congelada em vez de ler a atual —
essa comparação perde sentido no momento em que a paleta muda de novo, que é exatamente quando é
necessária.

O preço é que quem escolheu de propósito uma dessas cores a perde. É o sentido certo: uma pessoa
volta a escolher uma cor, contra todos os utilizadores a ficar com uma paleta que ninguém escolheu.

## Falar com o teclado

O SDK Chroma é acedido pela sua interface REST local. As cores são inteiros codificados em BGR; o
teclado inteiro escreve-se como uma matriz de 6 × 22. Uma sessão tem de ser mantida viva com um
batimento.

Medido na máquina de desenvolvimento: criar uma sessão demora 60 a 125 ms, o primeiro fotograma
depois de assumir o comando de um efeito do Chroma Studio em curso cerca de 500 ms, e cada
fotograma seguinte à volta de 2 ms.

### Cada resposta diz 200, por isso decide o corpo

O serviço responde a **tudo** com HTTP 200, incluindo pedidos que deitou fora. Um fotograma com o
tamanho de matriz errado volta assim:

```json
{"error":"expecting a 2 dimensional array of 6 (rows) x 22 (columns) elements with integer values","result":87}
```

com estado 200. Verificar apenas o código de estado comunica portanto sucesso para fotogramas que o
teclado nunca mostrou: uma falha silenciosa, indistinguível de a iluminação simplesmente não mudar.

Por isso decide o `result` no corpo: zero é sucesso, tudo o resto é uma recusa. Onde o serviço
fornece um `error` em linguagem clara, este é mantido tal como está, porque nomeia o defeito real
melhor do que qualquer formulação inventada aqui. Os códigos com que um utilizador pode fazer
alguma coisa são traduzidos:

| Código | Significado |
|---|---|
| 4309 | o Chroma está desligado para este dispositivo no Synapse |
| 1152 | outra aplicação detém a sessão |
| 1167 | não há nenhum dispositivo Chroma ligado |
| 5 | o acesso foi negado |
| 87 | o pedido estava malformado |
| 50 | o pedido não é suportado |

Um início de sessão bem-sucedido não traz `result` nenhum — devolve antes os dados da sessão —, por
isso a sua ausência conta como sucesso.

### Com que frequência os fotogramas são enviados

Isto parece um pormenor e não é; ambas as respostas óbvias estão erradas.

**Enviar só quando muda** deixa a tomada de controlo a seco. Uma tecla premida vulgar não muda o
estado do teclado — só os modificadores e os bloqueios o fazem — pelo que uma tomada de controlo
produz exatamente um fotograma. O Chroma descarta fotogramas enquanto ainda está a assumir o
controlo, e reporta sucesso para eles, de modo que esse único fotograma pode desvanecer-se e deixar
o teclado congelado no efeito anterior até o utilizador premir por acaso um modificador.

**Enviar o mais depressa possível** arruína a capacidade de resposta. Os fotogramas ficam em fila
dentro da interface, e uma mudança de estado espera então atrás de tudo o que já foi enviado:
premir Shift demora um segundo ou dois, visivelmente, a aparecer.

O que funciona é enviar por três razões distintas a três ritmos diferentes:

| Razão | Ritmo |
|---|---|
| O estado do teclado mudou | de imediato — medido em 1 ms de ponta a ponta |
| Dentro de três segundos após uma tomada de controlo | a cada 120 ms, até a transição assentar |
| Caso contrário | a cada 750 ms, puramente como seguro contra um fotograma perdido |

## Gestão da sessão

| Estado | Comportamento |
|---|---|
| **Inativo** | Sem sessão. O Chroma Studio conduz a iluminação. Só corre a barata sondagem de atividade. |
| **Ativo** | Sessão aberta, batimento a correr, um novo fotograma a cada mudança de estado. |
| **Em pausa** | Iluminação libertada até se retomar. |

O Keylegend assume ao primeiro toque e liberta o teclado após um período de inatividade
configurável, para que o teu próprio efeito do Chroma Studio regresse. O custo de despertar de
cerca de 500 ms é portanto pago apenas após uma pausa verdadeira, nunca enquanto se escreve.

Apenas uma cópia do Keylegend comanda o teclado. Duas abririam duas sessões para o mesmo
dispositivo; o serviço dá-o a uma delas, e a outra não ilumina nada enquanto continua a comunicar
sucesso — que é exatamente o aspeto de um programa que deixou de funcionar em silêncio. O que um
segundo arranque faz depende do que já está a correr. O mesmo programa a partir do mesmo sítio
significa que alguém fez duplo clique no ícone enquanto estava na área de notificação: aparece a
janela dessa cópia e o segundo arranque retira-se, portanto nada é terminado e a iluminação não
pisca. Tudo o resto — uma versão anterior, ou a mesma a partir de outra pasta — é substituído:
pede-se-lhe que saia, devolve a sua sessão, e só é terminada de imediato se não responder em dois
segundos.
