# Arquitectura

## La idea central

Toda la lógica de decisión es un **cálculo puro**, sin acceso a Windows, a la red ni al sistema de
archivos:

```
(estado del teclado, teclado conectado, perfil de aplicación, ajustes de color) → color por tecla
```

De ahí se siguen dos cosas, y ambas explican por qué el diseño tiene esta forma:

1. La vista previa en pantalla y el teclado real se rellenan con **el mismo código**. Lo que ves
   en la ventana es lo que se enciende.
2. La lógica es enteramente comprobable sin un teclado conectado y sin Synapse instalado.

Todo lo que habla con el mundo exterior vive en adaptadores delgados alrededor de ese núcleo.

## Proyectos

| Proyecto | Contiene | Puede depender de |
|---|---|---|
| `Keylegend.Core` | el teclado conectado, categorías, conjuntos de atajos, el compositor de fotogramas, la máquina de estados de sesión | nada específico de plataforma |
| `Keylegend.Windows` | estado del teclado, resolución de caracteres, ventana en primer plano | API de Windows |
| `Keylegend.Chroma` | cliente REST para el SDK de Chroma, latido | red |
| `Keylegend.Engine` | el bucle que lee el teclado, compone un fotograma y lo envía | Core, Chroma, Windows |
| `Keylegend.App` | interfaz WPF, icono de bandeja, almacenamiento de la configuración | todo lo anterior |

`Keylegend.Core` no debe referenciar nunca a los demás. Si un cambio parece exigirlo, es la
abstracción la que está en el sitio equivocado.

## Leer el estado del teclado

Keylegend **no** instala ningún hook global de teclado. Un hook así es funcionalmente un
registrador de pulsaciones, se sitúa en la cadena de entrada y los sistemas antitrampas lo marcan
con regularidad.

En su lugar se consulta el estado de las teclas que interesan (`GetAsyncKeyState` para los
modificadores mantenidos, `GetKeyState` para los bloqueos) unas sesenta veces por segundo, y solo
se compone un fotograma nuevo cuando algo ha cambiado. Ninguna pulsación se intercepta, se
reenvía, se registra ni se guarda jamás.

### Modificadores izquierdo y derecho

Windows informa de **Alt Gr como Ctrl más Alt derecho**, y en las distribuciones alemanas
Ctrl + Alt izquierdo produce los mismos caracteres que Alt Gr. Se distinguen por el lado:

- **Alt derecho** → capa Alt Gr, que muestra la asignación de caracteres
- **Ctrl + Alt izquierdo** → el conjunto de atajos `Ctrl+Alt`

Las variantes izquierda y derecha deben evaluarse por separado (`VK_LMENU`/`VK_RMENU`, etcétera).

## Determinar qué significa una tecla

En vez de incluir una tabla de distribuciones, Keylegend le pregunta a Windows qué carácter
produciría una tecla en el estado actual del teclado (`ToUnicodeEx`) y deriva la categoría del
carácter obtenido.

Por eso Mayús, Bloq Mayús y Bloq Num no necesitan tratamiento especial: la misma tecla devuelve
sencillamente `A` en lugar de `a` y aterriza por sí sola en la categoría «mayúscula». Y por eso
también funciona cualquier distribución de teclado sin cambios.

### Qué teclado está conectado

Se le pregunta a Razer Synapse, porque ya lo sabe. Escribe una descripción de cada dispositivo
conectado en `…\Razer Chroma SDK\Devices\<guid>.json`: el modelo por su nombre, la distribución
física como número, el tamaño de la matriz y el código de escaneo de cada tecla que el hardware
tiene realmente. `SdkDeviceDescription` lee eso, y del teclado no se deduce nada.

El aspecto del teclado viene de la misma instalación. La interfaz de Synapse es una aplicación web,
y los dibujos que carga para un dispositivo quedan en su caché: rectángulos de teclas con nombre,
la forma de la carcasa con la rueda de volumen y la tira multimedia, y los contornos de los
caracteres impresos en las teclas. `SvgLayoutSource` encuentra el del modelo y la distribución
conectados de forma exacta y no por su forma, porque cada dibujo se entrega junto a un objeto de
configuración que nombra ambos.

Solo se toman medidas y contornos; los colores y el estilo de Razer se ignoran, y nada de ese
material se copia a este repositorio.

Lo único que ninguno de los dos dice es a qué celda de la matriz de iluminación pertenece una
tecla. Eso es `StandardKeyMatrix`, la tabla `RZKEY` del propio protocolo, idéntica en cada modelo.

## Perfiles de aplicación

Un perfil vincula reglas de iluminación a un programa. Se incluyen cerca de noventa, y vale la
pena enunciar las decisiones que hay detrás, porque ninguna de ellas es la respuesta evidente.

### Los perfiles son datos, no código

La misma regla que para los dispositivos: añadir un perfil es añadir un archivo JSON bajo
`profiles/`, y la compilación lo recoge por comodín. Nadie tiene que tocar C# para enseñarle un
programa a Keylegend, lo que significa que un perfil puede aportarlo, revisarlo y corregirlo
alguien que solo conozca el programa. Si dar soporte a una aplicación nueva necesitara código, el
formato estaría mal.

### Incrustados en el ensamblado en vez de sueltos en disco

Los perfiles de aplicación se compilan dentro del ensamblado en vez de quedar como archivos junto
al ejecutable. Tres razones, y cada una bastaría por sí sola. Una versión en un solo archivo los
lleva consigo sin carpeta que perder. Nada en disco puede editarse por accidente, que es
precisamente lo que hace que «restablecer a la versión incluida» signifique algo — la versión
incluida tiene que estar fuera de alcance para merecer que se vuelva a ella. Y un perfil que no
compila se convierte en un error de compilación en lugar de en un programa que calladamente se
queda sin perfiles.

### Los reemplazos son por sección

La edición de un usuario nunca se guarda como copia del perfil. Se guarda como un reemplazo
indexado por el identificador del perfil, que contiene solo las secciones tocadas. Se siguen dos
cosas: restablecer es posible siquiera, y una compilación actualizada aún puede mejorar un perfil
que alguien ha editado en parte. El identificador es la pieza que sostiene esto y no debe cambiar
nunca una vez publicado: renombrarlo deja huérfanas las ediciones de alguien.

La granularidad se sostiene frente a las dos alternativas evidentes:

- **Por campo** parece más pulcro y produce estados que nadie configuró. Recolorea `W`, acepta
  luego una actualización que añade `Q`, y el resultado es una mezcla que el usuario nunca
  construyó y no sabe explicar.
- **Por perfil** es el fallo opuesto. Renombra una cosa y el perfil queda congelado para siempre;
  no vuelve a ver una corrección.

Una sección es la granularidad a la que el cambio todavía cabe en una frase: editaste los
resaltados, así que los resaltados son tuyos a partir de ahora.

### Un perfil se superpone al conjunto general, entrada por entrada

Los atajos se indexan por combinación de modificadores, y las entradas de un perfil se colocan
sobre las generales en vez de en su lugar — entrada por entrada, no capa por capa. Photoshop sabe
qué significa `Ctrl+J` dentro de Photoshop; no sabe nada de `Win+E`, que Windows asigna a nivel de
sistema, ni de `Ctrl+C`, que vale en cualquier sitio donde haya un cursor de texto.

Por capa significaría que un perfil que nombra `Ctrl` para sus propios comandos se lleva la capa
entera, y el portapapeles es lo que eso cuesta: copiar, pegar, cortar, deshacer y seleccionar todo
se apagan en un navegador, en un cliente de chat, en un terminal — programas en los que apenas se
hace otra cosa que escribir y pegar. Por entrada, quien nombra una tecla gana para esa tecla y nada
más se mueve. Vaciar una capa entera no es posible a propósito.

Un perfil que no nombra ninguna capa devuelve el catálogo general sin cambios, con lo que el caso
común no reserva nada.

### Los atajos y los resaltados llevan una etiqueta

La etiqueta dice qué hace el comando: «Duplicar capa», no «Ctrl+J». El hardware no la muestra
nunca: los LED llevan color y nada más, así que la etiqueta no cuesta nada en ejecución. Se paga
tres veces en otros sitios. La vista previa dentro de la aplicación puede mostrarla, un test puede
encontrar contradicciones entre entradas, y con noventa perfiles es la única manera de que alguien
revise si una entrada es correcta. `"j": "Editar"` no se puede contrastar con nada;
`"j": "Duplicar capa"` sí.

### Migrar un archivo de ajustes de formato 1

Un archivo de formato 1 guarda los perfiles enteros, sin identificador y sin dejar constancia de su
procedencia. Un reemplazo necesita un identificador al que engancharse, y restablecer necesita saber
que hay una versión incluida a la que volver, de modo que un archivo así no puede decir cuáles de
sus entradas son las incluidas.

Por eso todas pasan a ser perfiles de usuario. Eso conserva cada edición que alguien hiciera, al
precio de que el perfil incluido aparezca junto a la copia migrada hasta que se quite uno de los dos
— el intercambio correcto, porque la otra lectura borra trabajo en silencio.

### Migrar un archivo de ajustes de formato 2

Un archivo de formato 2 enumera todos los colores, incluidos los que nadie tocó, así que no puede
decir cuáles de sus entradas son decisiones y cuáles valores por defecto devueltos. Acatarlos todos
fija la paleta: un color incluido mejorado no llega entonces a nadie que haya ejecutado el programa
alguna vez.

El formato 3 escribe solo lo que difiere de la paleta incluida, de modo que una entrada en el
archivo significa que alguien la eligió. Migrar un archivo más antiguo obliga a adivinar esa
distinción, y la suposición es: una entrada igual a la paleta de aquella versión es un valor por
defecto, cualquier otra es una elección. `PaletteBeforeFormat3` guarda esa paleta como copia
congelada en vez de leer la actual — esa comparación queda sin sentido en el momento en que la
paleta vuelve a cambiar, que es justo cuando se necesita.

El precio es que quien eligió a propósito uno de esos colores lo pierde. Es la dirección correcta:
una persona vuelve a elegir un color, frente a todos los usuarios conservando una paleta que nadie
eligió.

## Hablar con el teclado

Al SDK de Chroma se le habla por su interfaz REST local. Los colores son enteros codificados en
BGR; el teclado entero se escribe como una matriz de 6 × 22. Una sesión debe mantenerse viva con
un latido.

Medido en la máquina de desarrollo: crear una sesión tarda de 60 a 125 ms, el primer fotograma
tras tomar el mando de un efecto de Chroma Studio en marcha unos 500 ms, y cada fotograma
posterior alrededor de 2 ms.

### Cada respuesta dice 200, así que decide el cuerpo

El servicio responde a **todo** con HTTP 200, también a las peticiones que ha desechado. Un
fotograma con el tamaño de matriz equivocado vuelve así:

```json
{"error":"expecting a 2 dimensional array of 6 (rows) x 22 (columns) elements with integer values","result":87}
```

con estado 200. Comprobar solo el código de estado informa por tanto de éxito para fotogramas que
el teclado nunca ha mostrado: un fallo silencioso, indistinguible de que la iluminación
simplemente no cambie.

Por eso decide `result` en el cuerpo: cero es éxito, cualquier otra cosa es un rechazo. Donde el
servicio aporta un `error` en lenguaje claro se conserva tal cual, porque nombra el defecto real
mejor que cualquier formulación inventada aquí. Los códigos con los que un usuario puede hacer
algo se traducen:

| Código | Significado |
|---|---|
| 4309 | Chroma está desactivado para este dispositivo en Synapse |
| 1152 | otra aplicación tiene la sesión |
| 1167 | no hay ningún dispositivo Chroma conectado |
| 5 | se denegó el acceso |
| 87 | la petición era incorrecta |
| 50 | la petición no es compatible |

Un inicio de sesión correcto no lleva `result` en absoluto —devuelve los datos de la sesión—, así
que su ausencia cuenta como éxito.

### Con qué frecuencia se envían los fotogramas

Parece un detalle y no lo es; las dos respuestas evidentes son erróneas.

**Enviar solo al cambiar** deja sin alimentar la toma de control. Una pulsación corriente no
cambia el estado del teclado —solo lo hacen los modificadores y los bloqueos—, así que una toma de
control produce exactamente un fotograma. Chroma descarta fotogramas mientras aún está tomando el
control, e informa de éxito por ellos, de modo que ese único fotograma puede desvanecerse y dejar
el teclado congelado en el efecto anterior hasta que el usuario pulse por casualidad un
modificador.

**Enviar tan rápido como se pueda** arruina la respuesta. Los fotogramas se encolan dentro de la
interfaz, y un cambio de estado espera entonces detrás de todo lo ya enviado: pulsar Mayús tarda
un segundo o dos, visiblemente, en mostrarse.

Lo que funciona es enviar por tres motivos distintos a tres ritmos distintos:

| Motivo | Ritmo |
|---|---|
| El estado del teclado ha cambiado | de inmediato — medido en 1 ms de extremo a extremo |
| Dentro de los tres segundos siguientes a una toma de control | cada 120 ms, hasta que el traspaso se asienta |
| En caso contrario | cada 750 ms, puramente como seguro ante un fotograma perdido |

## Gestión de la sesión

| Estado | Comportamiento |
|---|---|
| **En reposo** | Sin sesión. Chroma Studio dirige la iluminación. Solo corre el sondeo barato de actividad. |
| **Activo** | Sesión abierta, latido en marcha, un fotograma nuevo en cada cambio de estado. |
| **En pausa** | Iluminación liberada hasta que se reanude. |

Keylegend toma el mando en la primera pulsación y libera el teclado tras un tiempo de inactividad
configurable, de modo que tu propio efecto de Chroma Studio vuelve. El coste de despertar de unos
500 ms se paga por tanto solo tras una pausa real, nunca mientras escribes.

Solo una copia de Keylegend gobierna el teclado. Dos abrirían dos sesiones para el mismo
dispositivo; el servicio se lo da a una de ellas, y la otra no ilumina nada mientras sigue
informando de éxito, que es exactamente el aspecto de un programa que ha dejado de funcionar en
silencio. Lo que hace un segundo inicio depende de lo que ya esté en marcha. El mismo programa desde
el mismo sitio significa que alguien hizo doble clic en el icono mientras estaba en el área de
notificación: aparece su ventana y el segundo inicio se retira, así que no se cierra nada y la
iluminación no parpadea. Cualquier otra cosa —una versión anterior, o la misma desde otra carpeta—
queda sustituida: se le pide que se cierre, devuelve su sesión, y solo se termina sin más si no
responde en dos segundos.
