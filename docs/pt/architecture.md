# Arquitetura

## A ideia central

Toda a lógica de decisão é um **cálculo puro**, sem acesso ao Windows, à rede ou ao sistema de
ficheiros:

```
(estado do teclado, perfil de dispositivo, perfil de aplicação, definições de cor) → cor por tecla
```

Daqui decorrem duas coisas, e ambas explicam por que o desenho tem esta forma:

1. A pré-visualização no ecrã e o teclado real são preenchidos pelo **mesmo código**. O que vês na
   janela é o que se acende.
2. A lógica é inteiramente testável sem teclado ligado e sem o Synapse instalado.

Tudo o que fala com o mundo exterior vive em adaptadores finos à volta desse núcleo.

## Projetos

| Projeto | Contém | Pode depender de |
|---|---|---|
| `Keylegend.Core` | perfis de dispositivo, categorias, conjuntos de atalhos, o compositor de fotogramas, a máquina de estados da sessão | nada específico de plataforma |
| `Keylegend.Windows` | estado do teclado, resolução de carateres, janela em primeiro plano | APIs do Windows |
| `Keylegend.Chroma` | cliente REST para o SDK Chroma, batimento | rede |
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

## Perfis de aplicação

Um perfil liga regras de iluminação a um programa. Vêm incluídos cerca de noventa, e vale a pena
enunciar as decisões por detrás deles, porque cada uma foi a segunda resposta e não a primeira.

### Os perfis são dados, não código

A mesma regra do suporte a dispositivos: acrescentar um perfil é acrescentar um ficheiro JSON sob
`profiles/`, e a compilação apanha-o por carateres universais. Ninguém tem de tocar em C# para
ensinar um programa ao Keylegend, o que significa que um perfil pode ser contribuído, revisto e
corrigido por alguém que apenas conhece o programa. Se suportar uma nova aplicação alguma vez
exigisse código, o formato estaria errado.

### Embutidos no assembly em vez de soltos no disco

Os perfis de dispositivo ficam ao lado do executável; os de aplicação não. Três razões, e cada uma
bastaria por si. Uma versão em ficheiro único leva-os consigo sem pasta que se possa perder. Nada
no disco pode ser editado por acidente, e é precisamente isso que dá sentido a «repor a versão
incluída» — a versão incluída tem de estar fora de alcance para valer a pena voltar a ela. E um
perfil que não compila torna-se um erro de compilação em vez de um programa que ficou
silenciosamente sem perfis.

### As substituições são por secção

A edição de um utilizador nunca é guardada como cópia do perfil. É guardada como uma substituição
indexada pelo identificador do perfil, contendo apenas as secções tocadas. Decorrem duas coisas:
repor é sequer possível, e uma compilação atualizada ainda pode melhorar um perfil que alguém
editou em parte. O identificador sustenta isto e nunca pode mudar depois de publicado: renomeá-lo
deixa órfãs as edições de alguém.

A granularidade foi escolhida contra as duas alternativas óbvias:

- **Por campo** parece mais arrumado e produz estados que ninguém configurou. Recoloca a cor de
  `W`, aceita depois uma atualização que acrescenta `Q`, e o resultado é uma mistura que o
  utilizador nunca construiu e não sabe explicar.
- **Por perfil** é a falha oposta. Renomeia uma coisa e o perfil fica congelado para sempre; nunca
  mais vê uma correção.

Uma secção é a granularidade à qual a alteração ainda cabe numa frase: editaste os destaques, logo
os destaques passam a ser teus.

### Um perfil substitui apenas as camadas que nomeia

Os atalhos são indexados por combinação de modificadores e sobrepostos ao catálogo geral, não
substituídos a ele. O Photoshop sabe o que `Ctrl` significa dentro do Photoshop; não sabe nada de
`Win+E`, que o Windows atribui a nível de sistema e que é verdade haja o que houver à frente.
Substituir o catálogo inteiro tornaria um perfil responsável por factos sobre os quais não tem
opinião. Um perfil que não nomeia camada nenhuma devolve o catálogo geral inalterado, pelo que o
caso comum não aloca nada.

### Atalhos e destaques trazem uma etiqueta

A etiqueta diz o que o comando faz — «Duplicar camada», não «Ctrl+J». O hardware nunca a mostra:
os LED trazem cor e mais nada, portanto a etiqueta não custa nada em execução. Paga-se três vezes
noutros sítios. A pré-visualização dentro da aplicação pode mostrá-la, um teste pode encontrar
contradições entre entradas, e a noventa perfis é a única maneira de alguém rever se uma entrada
está certa. `"j": "Editar"` não pode ser confrontado com nada; `"j": "Duplicar camada"` pode.

### Migrar um ficheiro de definições em formato 1

O formato 1 guardava os perfis inteiros, sem identificador e sem registo da sua proveniência. É
exatamente isso que o novo formato corrige: uma substituição precisa de um identificador a que se
agarrar, e repor precisa de saber que existe uma versão incluída à qual voltar.

A consequência para a migração é que um ficheiro antigo não pode dizer quais das suas entradas
foram outrora incluídas. Por isso todas passam a perfis de utilizador. Isso preserva cada edição
que alguém fez, ao preço de o perfil incluído aparecer ao lado da cópia migrada até que um dos dois
seja removido — e é a troca certa, porque a outra leitura apagaria trabalho em silêncio.

## Falar com o teclado

O SDK Chroma é acedido pela sua interface REST local. As cores são inteiros codificados em BGR; o
teclado inteiro escreve-se como uma matriz de 6 × 22. Uma sessão tem de ser mantida viva com um
batimento.

Medido na máquina de desenvolvimento: criar uma sessão demora 60 a 125 ms, o primeiro fotograma
depois de assumir o comando de um efeito do Chroma Studio em curso cerca de 500 ms, e cada
fotograma seguinte à volta de 2 ms.

### Com que frequência os fotogramas são enviados

Isto parece um pormenor e não é; ambas as respostas óbvias estão erradas, e ambas foram
experimentadas.

**Enviar só quando muda** deixa a tomada de controlo a seco. Uma tecla premida vulgar não muda o
estado do teclado — só os modificadores e os bloqueios o fazem — pelo que uma tomada de controlo
produzia exatamente um fotograma. O Chroma descarta fotogramas enquanto ainda está a assumir o
controlo, e reporta sucesso para eles, de modo que esse único fotograma podia desvanecer-se e
deixar o teclado congelado no efeito anterior até o utilizador premir por acaso um modificador.

**Enviar o mais depressa possível** arruína a capacidade de resposta. Os fotogramas ficam em fila
dentro da interface, e uma mudança de estado espera então atrás de tudo o que já foi enviado:
premir Shift demorava um segundo ou dois, visivelmente, a aparecer.

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
