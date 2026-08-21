# Arquitectura

## La idea central

Toda la lógica de decisión es un **cálculo puro**, sin acceso a Windows, a la red ni al sistema de
archivos:

```
(estado del teclado, perfil de dispositivo, perfil de aplicación, ajustes de color) → color por tecla
```

De ahí se siguen dos cosas, y ambas explican por qué el diseño tiene esta forma:

1. La vista previa en pantalla y el teclado real se rellenan con **el mismo código**. Lo que ves
   en la ventana es lo que se enciende.
2. La lógica es enteramente comprobable sin un teclado conectado y sin Synapse instalado.

Todo lo que habla con el mundo exterior vive en adaptadores delgados alrededor de ese núcleo.

## Proyectos

| Proyecto | Contiene | Puede depender de |
|---|---|---|
| `Keylegend.Core` | perfiles de dispositivo, categorías, conjuntos de atajos, el compositor de fotogramas, la máquina de estados de sesión | nada específico de plataforma |
| `Keylegend.Windows` | estado del teclado, resolución de caracteres, ventana en primer plano | API de Windows |
| `Keylegend.Chroma` | cliente REST para el SDK de Chroma, latido | red |
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

## Perfiles de aplicación

Un perfil vincula reglas de iluminación a un programa. Se incluyen cerca de noventa, y vale la
pena enunciar las decisiones que hay detrás, porque cada una fue la segunda respuesta y no la
primera.

### Los perfiles son datos, no código

La misma regla que para los dispositivos: añadir un perfil es añadir un archivo JSON bajo
`profiles/`, y la compilación lo recoge por comodín. Nadie tiene que tocar C# para enseñarle un
programa a Keylegend, lo que significa que un perfil puede aportarlo, revisarlo y corregirlo
alguien que solo conozca el programa. Si dar soporte a una aplicación nueva necesitara código, el
formato estaría mal.

### Incrustados en el ensamblado en vez de sueltos en disco

Los perfiles de dispositivo están junto al ejecutable; los de aplicación no. Tres razones, y cada
una bastaría por sí sola. Una versión en un solo archivo los lleva consigo sin carpeta que perder.
Nada en disco puede editarse por accidente, que es precisamente lo que hace que «restablecer a la
versión incluida» signifique algo — la versión incluida tiene que estar fuera de alcance para
merecer que se vuelva a ella. Y un perfil que no compila se convierte en un error de compilación
en lugar de en un programa que calladamente se queda sin perfiles.

### Los reemplazos son por sección

La edición de un usuario nunca se guarda como copia del perfil. Se guarda como un reemplazo
indexado por el identificador del perfil, que contiene solo las secciones tocadas. Se siguen dos
cosas: restablecer es posible siquiera, y una compilación actualizada aún puede mejorar un perfil
que alguien ha editado en parte. El identificador es la pieza que sostiene esto y no debe cambiar
nunca una vez publicado: renombrarlo deja huérfanas las ediciones de alguien.

La granularidad se eligió frente a las dos alternativas evidentes:

- **Por campo** parece más pulcro y produce estados que nadie configuró. Recolorea `W`, acepta
  luego una actualización que añade `Q`, y el resultado es una mezcla que el usuario nunca
  construyó y no sabe explicar.
- **Por perfil** es el fallo opuesto. Renombra una cosa y el perfil queda congelado para siempre;
  no vuelve a ver una corrección.

Una sección es la granularidad a la que el cambio todavía cabe en una frase: editaste los
resaltados, así que los resaltados son tuyos a partir de ahora.

### Un perfil solo reemplaza las capas que nombra

Los atajos se indexan por combinación de modificadores y se superponen al catálogo general, no lo
sustituyen. Photoshop sabe qué significa `Ctrl` dentro de Photoshop; no sabe nada de `Win+E`, que
Windows asigna a nivel de sistema y que es cierto tenga lo que tenga delante. Sustituir el
catálogo entero haría a un perfil responsable de hechos sobre los que no tiene opinión. Un perfil
que no nombra ninguna capa devuelve el catálogo general sin cambios, con lo que el caso común no
reserva nada.

### Los atajos y los resaltados llevan una etiqueta

La etiqueta dice qué hace el comando: «Duplicar capa», no «Ctrl+J». El hardware no la muestra
nunca: los LED llevan color y nada más, así que la etiqueta no cuesta nada en ejecución. Se paga
tres veces en otros sitios. La vista previa dentro de la aplicación puede mostrarla, un test puede
encontrar contradicciones entre entradas, y con noventa perfiles es la única manera de que alguien
revise si una entrada es correcta. `"j": "Editar"` no se puede contrastar con nada;
`"j": "Duplicar capa"` sí.

### Migrar un archivo de ajustes de formato 1

El formato 1 guardaba los perfiles enteros, sin identificador y sin dejar constancia de su
procedencia. Eso es exactamente lo que corrige el formato nuevo: un reemplazo necesita un
identificador al que engancharse, y restablecer necesita saber que hay una versión incluida a la
que volver.

La consecuencia para la migración es que un archivo antiguo no puede decir cuáles de sus entradas
fueron alguna vez incluidas. Así que todas pasan a ser perfiles de usuario. Eso conserva cada
edición que alguien hiciera, al precio de que el perfil incluido aparezca junto a la copia migrada
hasta que se quite uno de los dos — y es el intercambio correcto, porque la otra lectura borraría
trabajo en silencio.

## Hablar con el teclado

Al SDK de Chroma se le habla por su interfaz REST local. Los colores son enteros codificados en
BGR; el teclado entero se escribe como una matriz de 6 × 22. Una sesión debe mantenerse viva con
un latido.

Medido en la máquina de desarrollo: crear una sesión tarda de 60 a 125 ms, el primer fotograma
tras tomar el mando de un efecto de Chroma Studio en marcha unos 500 ms, y cada fotograma
posterior alrededor de 2 ms.

### Con qué frecuencia se envían los fotogramas

Parece un detalle y no lo es; las dos respuestas evidentes son erróneas, y ambas se probaron.

**Enviar solo al cambiar** deja sin alimentar la toma de control. Una pulsación corriente no
cambia el estado del teclado —solo lo hacen los modificadores y los bloqueos—, así que una toma de
control producía exactamente un fotograma. Chroma descarta fotogramas mientras aún está tomando el
control, e informa de éxito por ellos, de modo que ese único fotograma podía desvanecerse y dejar
el teclado congelado en el efecto anterior hasta que el usuario pulsara por casualidad un
modificador.

**Enviar tan rápido como se pueda** arruina la respuesta. Los fotogramas se encolan dentro de la
interfaz, y un cambio de estado espera entonces detrás de todo lo ya enviado: pulsar Mayús tardaba
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
