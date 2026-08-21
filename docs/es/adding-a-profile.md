# Añadir un perfil

Un perfil de aplicación es **un dato, no código**. No necesitas C# ni herramientas de compilación:
bastan un editor de texto y conocimiento real del programa, y esta segunda parte es la difícil.

Si solo quieres un perfil para ti, hazlo en la interfaz: se guarda en `settings.json` y no
necesita nada de esto. Un archivo bajo `profiles/` es la forma de que un perfil se distribuya con
la aplicación para todo el mundo.

## 1. Crear el archivo

```
profiles/apps/<id>.json      programas
profiles/games/<id>.json     juegos
```

El nombre del archivo debe coincidir con el `id` que contiene. Minúsculas, `a-z0-9-`. La
compilación incrusta por comodín todos los archivos de estas dos carpetas, así que no hay ningún
archivo de proyecto que editar.

Un identificador es permanente. Los reemplazos del usuario y las entradas de perfiles ocultos se
indexan por él, de modo que renombrarlo en una versión posterior deja huérfanas las ediciones de
alguien. Elige un nombre que siga siendo correcto después de que el programa cambie de marca:
`adobe-photoshop`, no `photoshop-2026`.

## 2. Rellenarlo

Los campos, las tres secciones, los grupos de funciones, las combinaciones de modificadores y los
convenios de color se describen en [profiles/FORMAT.md](../../profiles/FORMAT.md). Léelo primero;
es la referencia y esta página no la repite.

Lo que sigue es la parte que sale mal incluso cuando se ha leído el formato.

## 3. Posiciones y caracteres no son lo mismo

Los identificadores de tecla vienen del perfil de dispositivo y nombran **posiciones americanas**.
`Keyboard_Y` es la tecla física que escribe `Y` en un teclado americano; en uno alemán, esa tecla
escribe `Z`. El formato ofrece por tanto dos maneras de nombrar una tecla, y elegir la equivocada
produce un perfil visiblemente erróneo en cualquier distribución no americana mientras parece
perfecto en la máquina donde se escribió.

La pregunta que hay que hacerse en cada entrada es de qué trata realmente:

- **Dónde está la mano → posición.** Un resaltado para WASD trata de la forma que hacen tus dedos,
  no de las letras. `Keyboard_W`, `Keyboard_A`, `Keyboard_S`, `Keyboard_D` son las teclas
  correctas en todas partes.
- **Cuál es el comando → carácter.** `Ctrl+Z` significa «la tecla que escribe z». Escrito como
  posición, deshacer y rehacer aparecen intercambiados en un teclado alemán.
- **Teclas que no escriben nada → posición otra vez.** Esc, Tab, Intro, Retroceso, las flechas y
  las teclas de función no tienen carácter, así que `shortcuts.keys` las nombra por identificador
  sin ambigüedad.

### Para los resaltados depende de cómo lee el teclado el programa

QWERTZ y QWERTY difieren exactamente en dos sitios, así que `Keyboard_Y` y `Keyboard_Z` son los
únicos identificadores donde esto puede salir mal. Y sale mal en silencio.

El identificador de un resaltado es siempre una **posición física**. La pregunta es qué tecla
física quiere decir el programa, y eso se deduce de cómo lee el teclado:

| El programa se enlaza a | Ejemplos | `Z` en su documentación significa |
|---|---|---|
| el **carácter** (códigos de tecla virtual de Windows, que siguen la distribución) | Photoshop, Blender, GIMP, Krita — las aplicaciones en general | `Keyboard_Y` — la tecla de la fila superior, que escribe `Z` en un teclado alemán |
| la **posición** (códigos de rastreo, como usan casi todos los motores de juego, para que WASD no se mueva) | los juegos en general | `Keyboard_Z` — la tecla de la fila inferior |

Si no consigues establecer de qué manera lee el teclado un programa concreto, deja fuera las
entradas de `Y` y `Z`. Ninguna otra letra se ve afectada.

## 4. Deja fuera aquello de lo que no estés seguro

Un atajo equivocado es peor que uno ausente. Una entrada ausente deja una tecla apagada y no
cuesta nada; una equivocada hace que el teclado afirme algo falso, y el usuario no tiene manera de
saber que es falso. La etiqueta hace explícita la afirmación; no la hace correcta.

Así que:

- Escribe solo aquello de lo que estés seguro de que es la asignación **predeterminada** del
  programa, recién instalado. Tu propia instalación no es una fuente; probablemente has cambiado
  cosas y lo has olvidado.
- Compruébalo con la documentación del programa, o con el programa mismo sin tocar los ajustes.
- Donde los valores predeterminados difieran entre versiones, sigue la actual.
- No inventes. Si un programa no tiene un atajo bien conocido para algo, no lleva entrada.

Doce atajos correctos valen más que treinta de los que cuatro están mal. Lo mismo vale para las
etiquetas de los resaltados: si no sabes decir qué hace una tecla, eso es señal de que la entrada
todavía no pertenece al perfil.

## 5. Pruébalo

```bash
dotnet test
```

Los tests de perfiles comprueban cada archivo bajo `profiles/`: el identificador es único y
coincide con el nombre del archivo, `kind` coincide con la carpeta, cada identificador de tecla
existe en un perfil de dispositivo incluido, los colores se interpretan, los grupos y las
combinaciones de modificadores son válidos y están escritos en forma canónica, cada atajo lleva
etiqueta, ninguna tecla de letra está bajo `shortcuts.keys` (su sitio es `characters`), ningún
perfil está vacío, y no hay dos perfiles que reclamen un mismo ejecutable sin distinguirse
mediante `titleContains`.

Una cosa **no** se comprueba deliberadamente: la misma etiqueta apareciendo dos veces bajo un
mismo modificador. Parecía una manera de detectar descuidos de copiar y pegar, y detectaba en
cambio alias reales: los navegadores cierran una pestaña tanto con `Ctrl+W` como con `Ctrl+F4`.
Una comprobación que salta con datos correctos es peor que ninguna.

Lo que ningún test puede comprobar es si un atajo es *cierto*. Para eso está la revisión, y por
eso cada entrada lleva una etiqueta que revisar.

## 6. Pruébalo contra el programa

Inicia Keylegend, trae el programa al primer plano y mantén pulsados los modificadores que define
tu perfil. La vista previa muestra lo mismo que el teclado, así que para esto basta un portátil
sin hardware Chroma. Compárala con los menús del propio programa: un comando cuya etiqueta no
encuentres en el programa es lo primero que hay que quitar.

## 7. Abre una pull request

Indica contra qué programa y qué versión lo comprobaste, y cómo verificaste las asignaciones: la
documentación del programa, el programa mismo, o ambos. Véase
[CONTRIBUTING.md](../../CONTRIBUTING.md).

Un perfil pequeño y seguro es una buena aportación. Uno grande y recordado a medias, no.
