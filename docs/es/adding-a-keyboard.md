# Añadir o corregir un teclado

La compatibilidad con un teclado es **un dato, no código**. No necesitas C# ni herramientas de
compilación: bastan un editor de texto y tu propio teclado.

La mayoría de quienes llegan aquí no tienen nada que añadir, porque ya existe un perfil para su
distribución. Lo que a esos perfiles les falta es lo único que no se puede generar: alguien con el
hardware que confirme que cada tecla se enciende donde el perfil afirma. **Ese es el trabajo
descrito en la [parte 2](#2-corregir-un-perfil), y lleva unos diez minutos.**

---

## Qué sabe un perfil, y con cuánta seguridad

Un perfil responde a dos preguntas distintas, y no son igual de fiables:

| Pregunta | De dónde sale la respuesta | Cuán segura |
|---|---|---|
| ¿Dónde está cada tecla y qué tamaño tiene? | La retícula normalizada de 19,05 mm, que todo teclado sigue desde el IBM Model M | **Segura.** La geometría se deduce de la distribución. |
| ¿Qué celda de la matriz de LED enciende esa tecla? | La matriz publicada por el fabricante, suponiendo un teclado estándar | **Una conjetura.** Los modelos mueven teclas, dejan celdas sin poblar y añaden las suyas. |

Esa separación es toda la razón de ser del indicador `verified`. Un perfil marcado
`"verified": false` casi con seguridad acierta con el dibujo y bien puede equivocarse con la tecla
que se enciende.

---

## 1. Añadir una distribución que falta

Comprueba primero que de verdad falta: `devices/` ya contiene perfiles de formato completo para
ANSI-US, ISO-DE, ISO-UK, ISO-FR, ISO-ES, ISO-IT, ISO-NORDIC, ISO-PT, ISO-CH, ISO-RU, ISO-PL,
JIS-JP y ABNT2-BR, además de variantes tenkeyless, 75 %, 65 % y 60 %. Si la tuya está entre ellas,
pasa a la parte 2.

### La vía generada

`tools/make-layout.py` construye un perfil a partir de las dimensiones normalizadas. Añadirle un
teclado es una entrada en la lista `PROFILES`, al final del archivo:

```python
("generic-fullsize-iso-tr", dict(
    name="Full-size keyboard (Turkish)", vendor="Generic", model="Full-size 105-key",
    physical_layout="ISO-TR", form_factor="fullsize", variant="iso", legends="en")),
```

| Argumento | Qué decide |
|---|---|
| `form_factor` | `fullsize`, `tkl`, `75`, `65`, `60`, `fullsize-macro` |
| `variant` | `ansi`, `iso`, `jis` o `abnt2` — la forma del Intro y qué teclas adicionales existen |
| `legends` | Qué conjunto de leyendas impresas usar: `en`, `de`, `fr`, `es`, `it` |
| `right` | `win` o `fn` — qué hay entre el Alt derecho y la tecla de menú |

Después ejecútalo:

```bash
python tools/make-layout.py --only iso-tr
```

Si las leyendas de tu teclado no están entre los cinco conjuntos, añade uno: copia `LEGENDS_EN` en
el mismo archivo, traduce las entradas y regístralo en `LEGEND_SETS`. Solo las teclas que *no*
escriben nada necesitan leyenda; las demás se le preguntan a Windows en tiempo de ejecución, que
es lo que permite que un perfil sirva para todas las distribuciones de software sobre el mismo
hardware.

### La vía manuscrita

Para un teclado que no sea una variación de una distribución estándar —ortolineal, partido, con
una fila de teclas macro que nadie más tiene— escribe `device.json` directamente. La
[descripción del formato](device-profile-format.md) enumera cada campo, y
`devices/device-profile.schema.json` da a casi todos los editores autocompletado y errores en
línea.

No hace falta ser exacto en la primera pasada. Coloca las teclas más o menos bien, deja `row` y
`column` en `null` donde dudes, y que la calibración haga el resto.

---

## 2. Corregir un perfil

Esta es la parte que necesita el hardware, y la que de verdad importa.

### Míralo primero

Antes de tocar el teclado, examina el dibujo:

```bash
python tools/preview-layout.py devices/generic-fullsize-iso-es/device.json
```

Eso escribe `preview.svg` junto al perfil; ábrelo en cualquier navegador. Compáralo con el teclado
que tienes delante y busca:

- teclas que falten, o teclas dibujadas que tu teclado no tiene
- un Intro con la forma equivocada: alto y en forma de L en ISO, ancho y plano en ANSI
- una fila inferior con el número equivocado de modificadores, que varía más que ninguna otra cosa
- **contornos rojos**, que marcan teclas sin celda de matriz. Esas no se encenderán nunca.

Corregir la geometría es aritmética, no adivinanza: la retícula es una unidad por tecla, y una
unidad es el `width` que tienen las teclas de letra corrientes.

### Después calibra

La calibración enciende una tecla cada vez y la nombra, para que puedas confirmar que la tecla que
brilla en blanco es la que el perfil afirma. Es la única manera de estar seguro: todo lo demás es
inferencia a partir de una tabla del fabricante.

```bash
keylegend-cli --profile devices/<tu-carpeta>/device.json --calibrate
```

Recorre las teclas asignadas en orden de lectura:

| Tecla | Qué hace |
|---|---|
| `Intro` o `→` | esta es correcta, sigue con la siguiente |
| `F` | se encendió la tecla equivocada — anotarlo |
| `←` | una tecla atrás |
| `A` | encender todas las teclas asignadas a la vez |
| `S` | saltar al resumen |
| `Q` o `Esc` | detener |

Como los identificadores siguen la distribución americana, el indicador muestra además lo que cada
tecla escribe realmente en *tu* máquina: en un teclado español se te habla de «la tecla ñ» y no de
`Keyboard_SemicolonAndColon`.

Los hallazgos se escriben en `calibration-findings.txt` sobre la marcha, no al final. Calibrar es
un trabajo paciente y cerrar la ventana no debe costártelo.

Mientras trabajas ayuda un segundo dibujo: este etiqueta cada tecla con la celda que reclama en
lugar de con su leyenda:

```bash
python tools/preview-layout.py devices/<tu-carpeta>/device.json --cells
```

### Aplica lo que hayas encontrado

`tools/apply-calibration.ps1` lo devuelve al perfil y guarda una copia `.bak`:

```powershell
tools/apply-calibration.ps1 `
  -ProfilePath devices/<tu-carpeta>/device.json `
  -Unlit Keyboard_Backslash,Keyboard_PauseBreak `
  -Remap "Keyboard_Enter=3,14"
```

`-Unlit` es para las teclas que no encendieron nada en absoluto: la matriz puede direccionar la
celda, pero ese modelo no tiene LED ahí. Esas teclas conservan su geometría —la tecla existe, y la
vista previa debe dibujarla— y pierden su `row`/`column`, para que no se envíe nada al vacío.
`-Remap` es para las teclas asignadas a la celda equivocada.

### Qué cabe esperar

Estos son los sitios donde un perfil generado se equivoca con más frecuencia:

| Dónde | Qué ocurre |
|---|---|
| **El Intro ISO** | Abarca dos celdas. En muchos teclados solo la inferior lleva LED, y la mitad superior la ilumina su vecina o nada. |
| **La fila inferior** | El número y la anchura de los modificadores difieren entre modelos. Los teclados de juego ponen `Fn` donde los de oficina tienen una segunda tecla Windows. |
| **Teclas macro y multimedia** | A menudo en la columna 0 o en las columnas exteriores, y a menudo en ninguna celda. |
| **Teclados compactos** | La matriz conserva sus 6 × 22 completos; un teclado del 60 % simplemente deja vacía la mayor parte. Las celdas no se renumeran. |
| **Las teclas altas del teclado numérico** | Más e Intro cubren dos filas pero responden a una sola celda, normalmente la superior. |

Una tecla que resulte no tener LED conserva su geometría y pierde su celda:

```jsonc
{ "id": "Keyboard_Function", "x": 234, "y": 120, "width": 24, "height": 19,
  "row": null, "column": null }
```

Se sigue dibujando, de modo que la vista previa coincide con el hardware; simplemente no se
enciende nunca. Eso es correcto, no un defecto.

### Márcalo como verificado

Cuando cada celda coincida, pasa `-MarkVerified` al mismo script, o pon `"verified": true` a mano,
y quita la `note` que dice que el perfil fue generado. Ese indicador es lo que le dice a la
siguiente persona con tu teclado que puede fiarse.

---

## 3. Pruébalo

```bash
dotnet test
```

Los tests de perfiles incluidos validan todos los perfiles bajo `devices/`, también el tuyo.
Detectan identificadores duplicados, dos teclas reclamando el mismo LED, teclas dibujadas una
sobre otra, celdas fuera de la matriz y geometría que se ha salido del lienzo.

## 4. Abre una pull request

Indica qué teclado y qué distribución física comprobaste, y si recorriste la calibración. Véase
[CONTRIBUTING.md](../../CONTRIBUTING.md).

Los perfiles con `"verified": false` también son bienvenidos: le dan ventaja a la siguiente
persona con ese teclado. Una corrección a un perfil existente vale tanto como uno nuevo.

### Sobre las imágenes

El campo `image` es opcional y ahora mismo no se usa: la vista previa se dibuja a partir de la
geometría, con lo que se mantiene nítida a cualquier tamaño y no puede contradecir al perfil. Si
aun así adjuntas una, tiene que ser una imagen que **tú** hayas fotografiado o dibujado. Un render
de producto de un fabricante no puede publicarse bajo la licencia MIT de este proyecto, y a una
pull request que lleve uno se le pedirá que lo retire.

## Véase también

- [Formato de perfil de dispositivo](device-profile-format.md) — cada campo, en detalle
- [Arquitectura](architecture.md) — por qué el significado de las teclas viene de Windows y no de una tabla
