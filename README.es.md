# Keylegend

**Iluminación de teclado interactiva para Razer Chroma: tus teclas se encienden según lo que realmente hacen.**

[English](README.md) · [Deutsch](README.de.md) · [Español](README.es.md) · [Français](README.fr.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md) ·
[Polski](README.pl.md) · [Português](README.pt.md) · [Русский](README.ru.md) · [Українська](README.uk.md) · [简体中文](README.zh-cn.md)

> **Versión 1.2.0.** La iluminación, la interfaz, la detección de juegos y los perfiles de
> aplicación funcionan. [Descarga el instalador o la copia portátil](https://github.com/Eistee82/Keylegend/releases/latest),
> o compila desde el código. Véase [CHANGELOG.md](CHANGELOG.md).

![Keylegend colorea las teclas según lo que significan en ese momento y cambia de perfil cuando otra aplicación pasa al primer plano](docs/images/keylegend.png)

---

## Qué hace

La mayoría del software RGB trata el teclado como decoración. Keylegend lo trata como una
**pantalla**.

Cada tecla se colorea según lo que significa *en ese momento*, y ese color cambia en el instante
en que cambia su significado:

- **Los bloqueos de un vistazo.** Bloq Num, Bloq Mayús y Bloq Despl muestran su estado en la
  propia tecla.
- **Un color por clase de carácter.** Dígitos, minúsculas, mayúsculas, símbolos y teclas de
  control tienen cada uno su color.
- **Mantén un modificador y verás su capa.** Pulsa `Alt Gr` y solo siguen encendidas las teclas
  que realmente llevan un carácter Alt Gr. Pulsa `Windows` y se encienden los atajos de Windows,
  agrupados por función. Lo mismo con `Alt`, `Ctrl` y sus combinaciones.
- **Mayús y Bloq Mayús funcionan solos.** Como el carácter que produce cada tecla se consulta a
  Windows en directo, las letras pasan por sí mismas del color «minúscula» al color «mayúscula».
  El teclado numérico se recolorea como navegación cuando Bloq Num está apagado.
- **Los juegos tienen su propio tratamiento.** Se detectan automáticamente —incluidos los que
  van en ventana sin bordes— y WASD, las teclas de alrededor y la fila de números toman colores
  fijos: mientras juegas importa dónde van las manos, no qué letra escribe una tecla.
- **Perfiles por aplicación, cerca de noventa incluidos.** Photoshop, Visual Studio Code, Excel,
  Elden Ring y los demás se aplican en cuanto el programa tiene el foco, y un perfil que nombra
  un programa gana al perfil general de juegos. Edita uno y solo la parte que editaste deja de
  seguir la versión incluida; el resto sigue mejorando con las versiones posteriores.
- **La iluminación puede responder a la escritura.** Ocho efectos a elegir, *ninguno* de forma
  predeterminada: la tecla pulsada se desvanece, destella o sigue brillando, una gota de agua o
  una onda oscura recorre el teclado, las teclas de alrededor tiemblan, saltan chispas, o las
  teclas se calientan con el uso y vuelven a enfriarse. Superpuesto a los colores, no mezclado
  con ellos: cada tecla sigue diciendo lo que significa.
- **Devuelve la iluminación.** Tras un tiempo de inactividad configurable (60 s de forma
  predeterminada), Keylegend libera el teclado y tu efecto de Chroma Studio vuelve a tomar el
  mando.
- **Once idiomas.** Inglés, alemán, español, francés, italiano, neerlandés, polaco, portugués,
  ruso, ucraniano y chino simplificado. La interfaz sigue el idioma de pantalla de Windows y se
  puede cambiar en los ajustes. Las leyendas de las teclas no se ven afectadas: siguen a tu
  teclado, no a los menús.

Como el significado de las teclas viene de la **distribución de teclado activa de Windows** y no
de una tabla fija, Keylegend funciona con cualquier distribución —española, alemana,
estadounidense, Dvorak— sin cambios.

## Cómo funciona

Keylegend pregunta a Windows qué carácter produciría cada tecla en el estado actual del teclado
(`ToUnicodeEx`), deriva de ese carácter una categoría y envía el mapa de colores resultante al
SDK de Razer Chroma a través de su interfaz REST local.

Deliberadamente **no** instala ningún hook global de teclado. Lee *estados* — si una tecla está
pulsada en este momento — y nunca intercepta, reenvía ni registra pulsaciones. Sin efecto de
escritura elegido solo mira el estado de los modificadores y de los bloqueos; un efecto pregunta
además qué teclas de este teclado están pulsadas, y nada más.
Véase [docs/es/architecture.md](docs/es/architecture.md).

## Requisitos

- Windows 10 u 11
- Razer Synapse con el servicio Chroma SDK en marcha
- Un teclado Razer Chroma, conectado (véase más abajo)
- El runtime de .NET 10

## Instalación

```powershell
winget install Eistee82.Keylegend
```

Es la vía más corta: winget se trae el runtime de .NET como dependencia declarada, así que no
queda ningún requisito por instalar a mano. Si no, coge un archivo:

[**Descargar la última versión.**](https://github.com/Eistee82/Keylegend/releases/latest)

| Archivo | Qué es |
|---|---|
| `Keylegend-1.2.0-setup.exe` | Se instala para el usuario actual: sin permisos de administrador. Entrada en el menú Inicio y una desinstalación que también quita la entrada de inicio automático. |
| `Keylegend-1.2.0-portable.zip` | El mismo programa, para descomprimir. Mantén las carpetas de idioma (`de`, `fr`, …) junto al ejecutable; si no, la interfaz aparecerá en inglés. |

Ninguno está firmado, así que Windows dirá que el editor es desconocido: un certificado cuesta al
año más de lo que tiene este proyecto. Cada versión incluye `SHA256SUMS.txt` para comprobar la
descarga, y el registro de compilación que la produjo es público.

## Teclados compatibles

**Cualquier teclado Razer Chroma.** No hay lista ni un archivo por modelo, porque Keylegend no
necesita reconocer tu teclado: lo pregunta. Razer Synapse describe el que está conectado — el modelo
por su nombre, la distribución física como número y las teclas que el hardware tiene realmente. El
propio dibujo de Razer para ese modelo aporta el resto: los tamaños reales de las teclas, la carcasa
con su rueda y sus teclas multimedia, y los contornos de los caracteres impresos, en el idioma
correcto.

Lo único que el dibujo no dice es a qué celda de la matriz de iluminación pertenece cada tecla. Eso
es una constante del protocolo Chroma, idéntica en todos los modelos, y por eso Synapse tampoco
necesita una tabla por modelo. Comprobado contra el único teclado calibrado a mano: las 105 teclas
coinciden.

La **disposición física** describe la *forma* del teclado, no el idioma con el que escribes. Qué carácter
produce una tecla se consulta a Windows en tiempo de ejecución, así que un teclado alemán funciona
correctamente incluso con Windows configurado en US o Dvorak.

**Requiere Razer Synapse**, instalado y en ejecución, con el teclado conectado. Ahí se describe el
teclado y ahí está su dibujo.

## Documentación

| Tema | |
|---|---|
| Arquitectura | cómo se decide el coloreado y por qué no hay ningún hook de teclado |
| Añadir un perfil | coloreado por aplicación |
| Configuración | ajustes, archivo de ajustes, inicio automático |

Disponible en once idiomas:

[English](docs/en/) · [Deutsch](docs/de/) · [Español](docs/es/) · [Français](docs/fr/) ·
[Italiano](docs/it/) · [Nederlands](docs/nl/) · [Polski](docs/pl/) · [Português](docs/pt/) ·
[Русский](docs/ru/) · [Українська](docs/uk/) · [简体中文](docs/zh-cn/)

El inglés y el alemán son los originales mantenidos; donde una traducción los contradiga, el
texto en inglés es el correcto. Las correcciones son bienvenidas, véase
[CONTRIBUTING.md](CONTRIBUTING.md).

## Compilar y ejecutar

```bash
git clone https://github.com/Eistee82/Keylegend.git
cd Keylegend
dotnet build
dotnet test
```

`Keylegend.exe` (`src/Keylegend.App`) es todo el programa: ventana, icono en el área de
notificación, ajustes. El único conmutador que conviene conocer: `--verify` comprueba que una copia
lleve los perfiles incluidos y los once idiomas, escribe lo que encuentra en la ruta indicada a
continuación y responde mediante su código de salida. Es lo que el script de publicación ejecuta
contra una copia empaquetada.

Los ajustes residen en `%APPDATA%\Keylegend\settings.json` y los escribe la aplicación.

## Contribuir

Los informes de error, los perfiles de aplicación y las traducciones son bienvenidos: véase
[CONTRIBUTING.md](CONTRIBUTING.md) y [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).

## Licencia

[MIT](LICENSE). Se exceptúan dos botones de donación de terceros, y aquí no hay código,
encabezados, bibliotecas ni material gráfico de ningún fabricante: véase [NOTICE.md](NOTICE.md).

## Aviso de marcas

Este proyecto **no está afiliado a Razer Inc., ni cuenta con su respaldo o patrocinio.**

RAZER y RAZER CHROMA son marcas comerciales o marcas registradas de Razer Inc. Se emplean aquí
únicamente para identificar el hardware y la interfaz de software con los que trabaja este
proyecto, tal como permite el uso referencial. Keylegend es un proyecto independiente, mantenido
por la comunidad.

Lo mismo vale para cualquier otro nombre de este repositorio. Los perfiles de aplicación y de juego
nombran cerca de noventa programas —Photoshop, Visual Studio Code, Excel, Elden Ring y otros— y la
documentación nombra fabricantes y modelos de teclado. Son marcas de sus respectivos titulares y
aparecen solo para indicar a qué programa o a qué teclado corresponde algo. Keylegend no está
asociado con ninguno de ellos y no contiene su código ni sus recursos gráficos. Véase
[NOTICE.md](NOTICE.md).