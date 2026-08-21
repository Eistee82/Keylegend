# Formato de perfil de dispositivo

Un perfil de dispositivo describe un modelo de teclado en una distribución física. Es un único
archivo en una carpeta bajo `devices/`, nombrada `<fabricante>-<modelo>-<distribución>`:

```
devices/razer-deathstalker-v2-de/
└── device.json     geometría y correspondencia de LED
```

`devices/device-profile.schema.json` describe lo mismo de forma legible por máquina. Nombrarlo en
una línea `$schema`, como hacen los perfiles incluidos, da a casi todos los editores
autocompletado y errores en línea mientras escribes.

## device.json

```jsonc
{
  "$schema": "../device-profile.schema.json",
  "formatVersion": 1,
  "name": "Razer DeathStalker V2",
  "vendor": "Razer",
  "model": "DeathStalker V2",
  "physicalLayout": "ISO-DE",
  "canvas":  { "width": 439.5, "height": 135.5 },
  "matrix":  { "rows": 6, "columns": 22 },
  "verified": true,
  "keys": [
    { "id": "Keyboard_Escape", "x": 6, "y": 6, "width": 19, "height": 19,
      "row": 0, "column": 1, "label": "esc" }
  ]
}
```

| Campo | Significado |
|---|---|
| `formatVersion` | Revisión del formato. Actualmente `1`. Una compilación rechaza un perfil con número mayor del que entiende. |
| `name` | Lo que muestra la interfaz. |
| `vendor`, `model` | Quién lo fabrica y qué modelo. `"Generic"` para un perfil que describe una distribución en lugar de un producto. |
| `physicalLayout` | `ANSI-US`, `ISO-DE`, `JIS-JP`, `ABNT2-BR` … — la *disposición* física de las teclas, no la distribución de software. |
| `canvas` | El sistema de coordenadas al que se refieren todas las posiciones. Solo importan las proporciones; los perfiles incluidos usan milímetros. |
| `matrix` | Tamaño de la matriz de LED del fabricante. Los teclados Razer son 6 × 22, sea cual sea su tamaño. |
| `verified` | `true` una vez que alguien ha confirmado la correspondencia en hardware real. |
| `note` | Texto libre opcional para quien abra el archivo a continuación. |
| `image` | Opcional, y actualmente sin uso — véase [Imágenes](#imágenes) más abajo. |
| `keys[]` | Una entrada por tecla. |

### Distribución física, no de software

`physicalLayout` decide la *forma* del teclado: si el Intro es alto y en forma de L, si hay una
tecla adicional a la izquierda de la `Z`, si la fila inferior lleva las teclas japonesas de
conversión.

No dice nada sobre qué caracteres producen esas teclas. Eso Keylegend se lo pregunta a Windows en
tiempo de ejecución, para la distribución activa. Un perfil ISO-ES sirve por tanto para un teclado
español tanto si Windows está en español como en inglés o en Dvorak — de ahí que haya un perfil
por distribución *física* y no uno por idioma.

### Entradas de tecla

| Campo | Significado |
|---|---|
| `id` | Identificador único. Sigue la nomenclatura existente: `Keyboard_A`, `Keyboard_Enter`, `Keyboard_NonUsBackslash`, `Keyboard_Num7`. |
| `x`, `y` | Posición de la esquina superior izquierda en el lienzo. |
| `width`, `height` | Tamaño de la tecla en el lienzo. |
| `row`, `column` | Celda de la matriz de LED del fabricante. Ambos `null` mientras se desconozcan: un estado válido, y para lo que sirve la calibración. |
| `scanCode` | Sustituye al código de rastreo estándar. Solo hace falta donde la distribución física contradice la nomenclatura americana. |
| `parts` | Rectángulos adicionales de la misma tecla, para teclas que no son rectangulares. |
| `label` | Lo que va impreso en la tecla, para las teclas que no escriben nada. |
| `labelSecondary` | Una segunda línea impresa, bajo la primera. |

### Las leyendas pertenecen al teclado

`label` es lo que está *impreso en la tecla*, no una traducción de lo que hace. Un teclado alemán
dice `strg`, uno francés `ctrl`, uno italiano `bloc maiusc` — y cada uno lo dice sin importar el
idioma en que estén los menús de Keylegend. Cambiar el idioma de la interfaz no cambia nunca las
leyendas.

Las teclas que producen un carácter no llevan `label` alguno. Su leyenda viene de la distribución
activa de Windows, de modo que sigue por sí sola a Mayús, Bloq Mayús y Alt Gr.

### Teclas con más de un rectángulo

El Intro ISO es el caso típico: una tecla que abarca dos filas.

```jsonc
{
  "id": "Keyboard_Enter",
  "x": 267.25, "y": 72.5, "width": 23.75, "height": 19,
  "row": 3, "column": 14,
  "scanCode": 28,
  "parts": [ { "x": 262.5, "y": 53.5, "width": 28.5, "height": 19 } ],
  "label": "enter"
}
```

El rectángulo principal lleva la celda; `parts` añade el resto de la forma. El `scanCode` explícito
está ahí porque la mitad superior ocupa la posición que ANSI reserva a la barra invertida: sin él,
la parte de arriba del Intro se colorearía como si escribiera `\`.

### Códigos de rastreo de teclas propias de una sola distribución

La tabla estándar de `Keylegend.Core` cubre lo que tiene un teclado americano. Las teclas que solo
existen en otros sitios declaran su código en el perfil, para que no haya que cambiar C# por una
distribución:

| Identificador | Tecla | `scanCode` |
|---|---|---|
| `Keyboard_JpYen` | `¥`, a la izquierda del Retroceso en JIS | `0x7D` |
| `Keyboard_JpRo` | `ろ`, a la derecha del Mayús derecho en JIS | `0x73` |
| `Keyboard_JpMuhenkan` | `無変換`, a la izquierda del espaciador | `0x7B` |
| `Keyboard_JpHenkan` | `変換`, a la derecha del espaciador | `0x79` |
| `Keyboard_JpKana` | `かな` | `0x70` |
| `Keyboard_AbntC1` | la tecla `/?` a la derecha del Mayús derecho en ABNT-2 | `0x73` |

## Reglas que impone el validador

Se comprueban en integración continua, así que un perfil que las incumpla no puede fusionarse:

- Los identificadores de tecla son únicos
- No hay dos teclas que reclamen la misma celda de matriz
- No hay dos teclas que se solapen en el lienzo
- `row` y `column` están ambos puestos o ambos en `null`
- Las celdas caen dentro de la matriz declarada
- Las teclas caen dentro del lienzo
- Cada tecla tiene un tamaño positivo
- Una imagen nombrada por `image` existe realmente

## Nomenclatura y la diferencia ISO/ANSI

Los identificadores siguen la distribución americana, porque es lo que hace la propia matriz del
fabricante. En un teclado alemán la `Z` física está por tanto en `Keyboard_Y` y viceversa. Esto
afecta solo al nombre: ni a la posición ni al comportamiento, porque el carácter real se le
pregunta a Windows en tiempo de ejecución.

Dos identificadores existen solo en teclados ISO:

| Identificador | Tecla | Celda Razer |
|---|---|---|
| `Keyboard_NonUsBackslash` | la tecla adicional a la izquierda de `Y`/`Z` (`<`, `>`, `\|`) | `RZKEY_EUR_2`, fila 4 columna 2 |
| `Keyboard_NonUsTilde` | la tecla junto al Intro en la fila central (`#`, `'`) | `RZKEY_EUR_1`, fila 3 columna 13 |

En los teclados ISO el Intro alto abarca dos posiciones de matriz: la mitad superior donde ANSI
tiene la barra invertida (fila 2, columna 14), la inferior en `Keyboard_Enter` (fila 3,
columna 14).

**Que ambas se enciendan de verdad depende del modelo.** La tabla del fabricante describe lo que
la matriz puede *direccionar*, no lo que un teclado concreto lleva *montado*. En la
DeathStalker V2, la calibración mostró que la celda superior no acciona ningún LED: todo el Intro
lo ilumina la inferior, y por eso el perfil incluido modela el Intro como una tecla con dos
rectángulos y no como dos teclas.

Esto es exactamente lo que ninguna documentación permite deducir, y la razón por la que un perfil
no debería marcarse `verified` hasta que alguien lo haya recorrido sobre hardware.

## Imágenes

`image` es opcional y ahora mismo no se usa: la vista previa en pantalla se dibuja a partir de la
geometría de arriba. Dibujarla mantiene la vista previa nítida a cualquier tamaño de ventana y
hace imposible que imagen y perfil se contradigan.

Si aun así adjuntas una, tiene que ser una imagen que **tú** hayas tomado o creado. Todo este
repositorio aparece bajo la licencia MIT, que concede a cualquiera el derecho a modificar y
redistribuir lo que contiene — un derecho que nadie puede conceder sobre la fotografía de producto
de un fabricante de teclados. Véase [NOTICE.md](../../NOTICE.md).

## Véase también

- [Añadir o corregir un teclado](adding-a-keyboard.md) — el recorrido práctico
