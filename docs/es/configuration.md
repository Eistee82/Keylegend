# Configuración

Los ajustes residen en `%APPDATA%\Keylegend\` y se editan desde la interfaz. En el primer inicio
se escribe una configuración predeterminada completa.

## Colores

Un color por categoría:

| Categoría | Se aplica a |
|---|---|
| Dígito | `1`, `7`, y el teclado numérico mientras Bloq Num está activo |
| Minúscula | `a`, `ñ` |
| Mayúscula | `A`, `Ñ` |
| Símbolo | `+`, `#`, `€`, `\|`, y los operadores del teclado numérico |
| Tecla de control | Esc, Tab, Intro, Retroceso, modificadores, flechas, bloque de navegación, y el teclado numérico mientras Bloq Num está apagado |
| Tecla de función | F1 a F12 |
| Tecla muerta | `^`, `´`, `` ` `` — teclas que necesitan una segunda pulsación para producir un carácter |
| Sin asignar | teclas sin significado en el contexto actual; apagadas por omisión. La tecla central del teclado numérico con Bloq Num apagado es el ejemplo más claro |

Las teclas de bloqueo tienen dos colores cada una: uno para activada, otro para desactivada.

## Conjuntos de atajos

Un conjunto de atajos asigna teclas a **grupos de funciones** y se elige según los modificadores
que se mantengan pulsados. Conjuntos incluidos: `Win`, `Win+Shift`, `Win+Ctrl`, `Alt`, `Ctrl`,
`Ctrl+Shift`, `Ctrl+Alt`.

Cada grupo tiene su color, de modo que los comandos relacionados se leen como un bloque — por
ejemplo la edición (`X`/`C`/`V`/`Z`/`Y`/`A`) en un color y las operaciones con archivos
(`N`/`O`/`S`/`P`/`W`) en otro.

Los atajos de Windows están fijados a nivel de sistema y por tanto son siempre exactos. Los atajos
de Ctrl varían entre programas; el conjunto incluido cubre los convenios habituales de Windows.

## Perfiles de aplicación

Un perfil describe qué debe mostrar el teclado mientras un programa concreto está delante. Con la
aplicación vienen cerca de noventa: programas como Photoshop, Visual Studio Code o Excel, y juegos
como Elden Ring o Counter-Strike 2. Se aplican solos: en cuanto la ventana correspondiente tiene
el foco, el perfil se aplica, y cuando el foco pasa a otra cosa vuelven los conjuntos
predeterminados. Donde no coincide ningún perfil, nada cambia.

El reconocimiento es por nombre de ejecutable. Cuando coincide más de un perfil, gana el que
nombra el programa — un juego con su propio perfil lo conserva por tanto aunque la detección de
juegos también se dispare. La prioridad solo deshace los empates restantes.

Un perfil sustituye únicamente las capas de modificadores que él mismo nombra. Photoshop sustituye
la capa Ctrl, porque allí Ctrl significa otros comandos; `Win+E` sigue abriendo el Explorador,
porque Windows asigna esa combinación a nivel de sistema y vale sea lo que sea lo que esté
delante.

### Qué contiene un perfil

| Sección | Contenido |
|---|---|
| Coincidencia | A qué programas se aplica el perfil: nombres de ejecutables, si cubre los juegos detectados en general, y la prioridad |
| Resaltados | Teclas fijadas a un color con independencia del carácter que produzcan — WASD en un juego, las teclas de herramienta de un editor de imágenes |
| Atajos | Sustituciones de capas de modificadores concretas: qué tecla lleva qué comando bajo `Ctrl`, coloreada por grupo de funciones |

Resaltados y atajos llevan además una etiqueta que dice qué hace el comando: «Duplicar capa»,
«Saltar». Nada de eso es visible en el teclado; los LED solo muestran color. La etiqueta aparece
en la vista previa dentro de la aplicación, y con noventa perfiles es la única manera de comprobar
si una entrada es correcta siquiera.

### Editar y restablecer

Las tres secciones se sustituyen por separado. Edita los resaltados de un perfil incluido y los
resaltados son tuyos desde entonces: quedan congelados y ya no siguen a la versión incluida. La
coincidencia y los atajos siguen siguiéndola y recogen las mejoras que traiga una versión nueva.

Solo se guarda la sección que cambiaste, bajo el identificador del perfil — nunca una copia del
perfil entero. Precisamente por eso existe el restablecimiento, y por eso una actualización aún
puede mejorar un perfil que has editado en parte.

El restablecimiento funciona por tanto también por secciones: devolver los atajos conservando tus
propios resaltados es posible. Restablecer el perfil entero recupera todas las secciones, más un
nombre cambiado y un estado oculto.

Los perfiles incluidos se pueden **ocultar pero no borrar**. Viven dentro del archivo del
programa; borrar uno duraría solo hasta el siguiente inicio. Un perfil oculto se omite al elegir
perfil, pero permanece en la lista y puede volver a mostrarse.

### Tus propios perfiles

Un perfil que creas tú se guarda entero en `settings.json`, porque no hay nada con lo que
compararlo. Por eso no se puede restablecer, solo borrar. Por lo demás se comporta como uno
incluido: las mismas tres secciones, la misma regla de selección.

Si un perfil debería valer para todo el mundo y no solo para ti, su sitio está en el proyecto como
archivo — véase [Añadir un perfil](adding-a-profile.md).

### Formato del archivo de ajustes

`settings.json` lleva `formatVersion` 2. Los archivos más antiguos se migran al cargarlos: la
versión 1 no conocía ni identificadores ni la procedencia de un perfil, y por eso no puede decir
cuáles de sus entradas fueron alguna vez incluidas. Todas pasan a ser perfiles de usuario. No se
pierde nada, pero los perfiles incluidos aparecen junto a ellos, así que al principio puede haber
dos entradas para el mismo programa; la sobrante se puede borrar u ocultar.

## Comportamiento

| Ajuste | Significado |
|---|---|
| Devolver la iluminación al quedar inactivo | Si se devuelve en absoluto. Desactivado, Keylegend conserva el teclado hasta que lo pauses o lo cierres, y lo toma al arrancar en lugar de esperar una pulsación. |
| Tiempo de inactividad | Segundos sin actividad de teclado antes de la devolución. 60 por omisión: recuperarlo cuesta uno o dos segundos, así que un tiempo corto convierte eso en una interrupción constante. El valor se conserva mientras la devolución está desactivada. |
| Brillo | Factor global de 0 a 100 %, aplicado a cada color al componer el fotograma. |
| Usar perfiles de aplicación | Si se consultan los perfiles siquiera. Desactivado, los conjuntos predeterminados valen en todas partes, esté delante lo que esté. |
| Iniciar con Windows | Registra la aplicación en la clave `Run`, con el modificador `--minimized`. Iniciada así, Keylegend aparece en el área de notificación: sin ventana y sin globo. Iniciada a mano, siempre muestra su ventana. Una entrada escrita por una versión anterior se actualiza en el siguiente inicio. |

## Idioma

La interfaz sigue el idioma de pantalla de Windows y está disponible en once: inglés, alemán,
español, francés, italiano, neerlandés, polaco, portugués, ruso, ucraniano y chino simplificado.
**Ajustes → Idioma** lo sobrescribe; el cambio surte efecto de inmediato, sin reiniciar.

Cada idioma se nombra a sí mismo en esa lista en vez de traducirse. Traducirla significaría que
cada uno de los once llevara diez nombres para los demás, y quien se encontrara la interfaz en un
idioma que no sabe leer tendría que buscar el suyo en un idioma que tampoco sabe leer.

La elección se guarda en `settings.json` bajo `language` como `Automatic`, `English`, `German`,
`Spanish`, `French`, `Italian`, `Dutch`, `Polish`, `Portuguese`, `Russian`, `Ukrainian` o
`ChineseSimplified`. Un valor desconocido recae en `Automatic` en lugar de negarse a arrancar, que
es lo que un archivo editado a mano quiere con toda probabilidad.

Lo que está traducido son los menús y las explicaciones. Dos cosas **no** lo están, ambas a
propósito:

- **Las leyendas de las teclas** del teclado dibujado. Vienen del perfil de dispositivo y tienen
  que coincidir con el teclado que tienes delante, no con el idioma de los menús: un teclado ISO
  alemán muestra `strg` y `entf` esté la interfaz en inglés o no.
- **Los nombres de los modificadores** (Shift, Ctrl, Alt, Alt Gr, Bloq Num …). Esos mismos nombres
  los produce la maquinaria de atajos para las listas de capas, que queda fuera de la traducción;
  media traducción se leería peor que ninguna.

Todo lo que no tenga traducción recae en el inglés, así que un archivo de idioma sin terminar
cuesta las líneas que le faltan y no la interfaz entera.

## Calibración

La calibración es un modo de línea de comandos, no una página de ajustes:

```bash
keylegend-cli --profile devices/<carpeta>/device.json --calibrate
```

Enciende una tecla cada vez y la nombra, para que un perfil de dispositivo pueda comprobarse
contra hardware real. Los hallazgos se escriben sobre la marcha en `calibration-findings.txt`, y
`tools/apply-calibration.ps1` los devuelve al perfil. Véase
[Añadir o corregir un teclado](adding-a-keyboard.md).
