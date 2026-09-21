# ⚽ Penales — Ronda de tiros de castigo en la cancha del barrio

Juego 3D en **tercera persona** hecho en **Unity 6000.5.6f1**. Manejás a un pibe que entra a
la cancha del barrio y, si acepta, arranca una **ronda de 5 tiros de castigo** contra un
**arquero con IA**. El jugador conduce la pelota con los pies, carga la fuerza del disparo con
el clic izquierdo y el arquero **lee el tiro, espera su tiempo de reacción y se lanza** al
punto por donde va a cruzar. El **portón de la cancha se cierra** durante la ronda (con
animación y sonido) y se vuelve a abrir al terminar.

| Ficha técnica | Valor |
|---|---|
| Motor | Unity **6000.5.6f1** (URP 17.5.0) |
| Entrada | **Input System 1.20.0** (nuevo) |
| Escena de juego | `Assets/Scenes/SampleScene.unity` (única en el build) |
| Nombre del ejecutable | `Penales.exe` (product name: *Penales*, versión 0.1.0) |
| Plataforma | Windows 64-bit |
| Scripts | 11 de juego + 5 de editor |
| Sonido / animación | 10 efectos (WAV generados por código) + 12 clips + 6 controladores (incluye el **Blend Tree** del jugador) |
| Optimización | Perfilado antes/después documentado en [`ENTREGABLE-2-Optimizacion.md`](ENTREGABLE-2-Optimizacion.md) |

**Contenido:** 1. De qué trata el juego · 2. Qué necesitás · 3. Cómo ejecutarlo (editor y `.exe`) ·
4. Controles · 5. La ronda paso a paso · 6. Sonido y animación · 7. Estructura del proyecto ·
8. Herramientas del editor · 9. Problemas comunes · 10. Git y entrega · 11. Créditos

---

## 1. De qué trata el juego

**Ambientación.** Una cancha de fútbol de barrio (**36 × 24 m**) con malla perimetral, arco de
**3,60 × 2,44 m** y un portón de reja en la entrada sur. Alrededor hay andenes, tribunas y
edificios: el portón es la única puerta de la cancha.

**Objetivo.** Meter la mayor cantidad de goles en una **ronda de 5 tiros de castigo**. Arriba a
la derecha hay un marcador con **5 círculos de tiros** y **5 de atajadas**: se van pintando en
verde (acierto) o rojo (fallo) a medida que el juez resuelve cada tiro.

**El arquero tiene IA.** No está quieto: se balancea de lado a lado siguiendo la posición de la
pelota, **achica el arco** cuando el balón se acerca, y cuando detecta un disparo **calcula por
dónde va a cruzar su línea**, espera su **retardo de reacción** y se lanza hacia ese punto con
un pequeño error (para que no sea perfecto). Si la pelota rueda lenta hacia su arco, sale a
interceptarla.

**Qué decide el juez de cada tiro** (`JuezTiro`):

| Resultado | Condición |
|---|---|
| **¡GOL!** | La pelota cruza la línea (x = 15,95) **entre los palos** (\|z\| ≤ 1,80) y **por debajo del travesaño** (2,44 m) |
| **¡ATAJADA!** | El arquero llegó a tocarla y no entró |
| **¡FALLADO!** | Ni gol ni atajada (afuera, al palo, se quedó corta…) |

Si el tiro se eterniza (**6 s**) o la pelota queda quieta, el juez lo da por terminado.

**Lo que hace especial al juego** (feedback audiovisual en cada interacción):

- El **portón** de la reja **se hunde** para dejarte entrar y **sube** para cerrar la cancha,
  con sonido de reja metálica corriendo.
- La **patada** suena más fuerte cuanto más cargaste, y hay un **aviso sonoro** al llegar al 100 %.
- El **balón rebota** con sonido 3D en el punto del golpe (suelo, arco o cerco).
- Los mensajes **¡GOL! / ¡ATAJADA! / ¡FALLADO!** entran con un *pop* de escala y su propio sonido.
- Los **círculos del marcador** hacen un pulso y suenan al pintarse.
- El **arquero se inclina** (cuerpo y cabeza) cuando se lanza, y vuelve solo a su pose.

---

## 2. Qué necesitás para ejecutarlo

| Requisito | Detalle |
|---|---|
| **Unity 6000.5.6f1** | Se instala desde el **Unity Hub** (*Installs → Install Editor → 6000.5.6f1*). También sirve una 6.x posterior (Unity avisa que actualiza el proyecto). |
| **Unity Hub** | Para abrir el proyecto y gestionar la versión del editor. |
| **Windows 10/11 de 64 bits** | Es donde se armó y se probó el proyecto. |
| **~5 GB libres en disco** | El proyecto en sí pesa ~30 MB; la carpeta `Library/` que Unity genera sola ronda los 2 GB. |
| **Internet la primera vez** | Unity baja los paquetes declarados en `Packages/manifest.json` (URP, Input System, ProBuilder, uGUI, Timeline).

> No hace falta instalar nada a mano: los paquetes (`com.unity.render-pipelines.universal`,
> `com.unity.inputsystem`, `com.unity.probuilder`, `com.unity.ugui`, etc.) están declarados en
> `Packages/manifest.json` y Unity los resuelve al abrir el proyecto.

---

## 3. Cómo ejecutarlo

### 3.1 En el Editor de Unity (recomendado para probarlo o mostrarlo)

1. Abrí el **Unity Hub** → **Add** → **Add project from disk** → elegí la carpeta del proyecto
   (`Penales`, la que contiene `Assets/`, `Packages/` y `ProjectSettings/`).
2. Si el Hub pide una versión distinta, instalá/abrí con **6000.5.6f1**.
3. Abrí el proyecto. **La primera vez tarda bastante**: importa todos los assets y baja los paquetes.
4. En el panel **Project**, entrá a `Assets/Scenes/` y hacé **doble clic en `SampleScene`**.
   (Es la única escena del juego y ya está marcada en *Build Settings*.)
5. Apretá **▶ Play** (arriba, centrado) y hacé clic en la ventana **Game**.
   El cursor ya arranca capturado, así que el ratón orbita la cámara enseguida.

> **El cursor:** **Esc** alterna entre capturado y libre. Si lo liberaste y no gira la cámara,
> volvé a apretar **Esc** para recapturarlo.

### 3.2 Generando el `.exe` (para jugar sin Unity, ideal para entregar)

1. Con el proyecto abierto: **File → Build Profiles** (en Unity 6; en versiones viejas es
   *File → Build Settings*, atajo **Ctrl + Shift + B**).
2. Elegí **Windows** y la arquitectura **x86_64**.
3. **Build** (o *Build And Run*) y elegí la carpeta de salida, por ejemplo `Builds/Windows`.
4. Se genera **`Penales.exe`** junto con la carpeta `Penales_Data/`. **Se ejecuta el `.exe`
   desde esa carpeta** (no lo muevas solo, necesita el `_Data` al lado).
5. Para cerrar una ronda o salir del juego: **Esc** libera el cursor y luego cerrás la ventana.

> La carpeta de build está ignorada por git (`.gitignore`), así que no se sube al repo: se
> genera con estos pasos o se comparte el `.zip` por separado.

---

## 4. Controles

Están siempre visibles abajo a la derecha en pantalla (guía de comandos del HUD).

| Tecla / botón | Acción | ¿Cuándo? |
|---|---|---|
| **W A S D** o **flechas** | Moverse (relativo a la cámara) | siempre |
| **Shift** (izq. o der.) | Correr | siempre |
| **Espacio** | Saltar | siempre |
| **Ratón** | Girar / orbitar la cámara | siempre |
| **Rueda del ratón** | Zoom | siempre |
| **E** | Acción: iniciar la ronda · confirmar la pregunta de salir | siempre |
| **Q** | "No" / cerrar el aviso de la pregunta | siempre |
| **Clic izquierdo** | **Disparar** (mantener = cargar fuerza, soltar = tirar) | durante la ronda |
| **R** | Reponer la pelota en el centro de la cancha | durante la ronda |
| **E** | Salir de la ronda (previa pregunta de confirmación) | durante la ronda |
| **F** | **Repetir la ronda** (borra el marcador y arranca de nuevo) | solo con los 5 tiros ya lanzados |
| **Esc** | Liberar el cursor del ratón | siempre |

---

## 5. Cómo se juega una ronda (paso a paso)

1. **Entrás a la cancha.** El portón arranca **abierto** (la reja queda hundida, bajo el andén),
   así que la entrada está libre.
2. **Aparece la pregunta** en pantalla: *"¿Quieres iniciar una ronda de tiros de castigo?"*
   con **E** (sí) y **Q** (no), con un sonido de clic.
3. **Apretás E.** Suena el **silbato**, la **reja sube y cierra la cancha** (animación + sonido
   de reja metálica), aparece la **pelota en el centro**, se muestra el **marcador** (arriba a la
   derecha) y el mensaje *"¡Ronda de tiros de castigo lista para jugar!"*.
4. **Te acercás a la pelota.** Cuando la pelota está delante tuyo y dentro de la zona de control,
   **se conduce pegada a los pies**.
5. **Mantenés el clic izquierdo.** Abajo aparece la **barra de fuerza**, que se llena de
   **verde a rojo**; al llegar al 100 % suena un **aviso**. Al **soltar**, el jugador se orienta
   hacia donde tira y sale la **patada** (el sonido es más fuerte cuanto más cargaste).
6. **El arquero reacciona.** Se acomoda, lee el disparo, **espera su retardo** y se **lanza
   inclinando el cuerpo** hacia el punto por donde va a cruzar.
7. **El juez resuelve el tiro:** aparece el mensaje **¡GOL! / ¡ATAJADA! / ¡FALLADO!** con su
   *pop* y su sonido, y se **pinta el círculo del marcador** con un pulso y un *ding*.
   Después de ~2,2 s la pelota **vuelve al centro** para el siguiente tiro.
8. **Al terminar los 5 tiros:** aparece **"RONDA TERMINADA"** con los goles y las atajadas.
   Ahí podés:
   - **F** → **repetir la ronda** (borra el marcador y arranca de nuevo), o
   - **E** → confirmar y **salir de la ronda**: la reja se **hunde** y ya podés salir de la cancha.

> **Extra:** durante la ronda, **R** repone la pelota al centro si se te fue lejos.

---

## 6. Sonido y animación de cada interactivo

No se usaron assets externos: **los 10 sonidos son WAV sintetizados por código**
(`Penales → Generar sonidos (WAV)`) y **las 12 animaciones + 6 controladores se generan por
script** (`Penales → Crear animaciones`): 9 clips para los interactivos y 3 para el **Blend Tree**
de locomoción del jugador (ver `ENTREGABLE-2-Optimizacion.md`, sección 9).

| Interactivo | Animación | Sonido |
|---|---|---|
| **Portón** de la entrada | La reja **se hunde** para abrir y **sube** para cerrar (0,90 s) | Reja metálica corrediza con traqueteo |
| **Jugador** (locomoción) | **Blend Tree** 1D: quieto / caminar / correr mezclados por la velocidad real | — |
| **Disparo del jugador** | El jugador se orienta hacia el tiro; el balón sale con física real | **Patada** (volumen según la carga) + aviso al 100 % |
| **Pelota** | Rueda y rebota con física | **Bote** 3D en el punto del golpe (suelo, arco, cerco) |
| **Arquero** | **Se inclina** (cuerpo y cabeza, ±30°) al lanzarse y vuelve solo | (el resultado del tiro lo sonifica el juez) |
| **Mensaje de resultado** | Entra con *pop* de escala (0,72 → 1,12 → 1) | **Gol** / **atajada** / **fallado** |
| **Círculos del marcador** | **Pulso** de escala (1,00 → 1,45) al pintarse | *Ding* al pintar |
| **Panel de pregunta (E/Q)** | Entrada suave (0,90 → 1,04 → 1) | Clic de interfaz |
| **Inicio / fin de ronda** | — | **Silbato** de árbitro |

---

## 7. Estructura del proyecto

```text
Penales/
├─ Assets/
│  ├─ Scenes/SampleScene.unity      ← la escena del juego (única del build)
│  ├─ Scripts/                      ← 11 scripts de juego (C#)
│  ├─ Editor/                       ← 5 herramientas de menú (generan escena/UI/audio/animación)
│  ├─ Audio/                        ← 10 WAV (sfx_patada, sfx_gol, sfx_porton, …)
│  ├─ Animations/                   ← 9 clips .anim + 5 controladores .controller
│  ├─ Materials/ · Meshes/ · Textures/ · Physics/  ← geometría y materiales de la cancha
│  └─ Settings/                     ← assets de URP (render pipeline)
├─ Packages/manifest.json           ← paquetes (URP, Input System, ProBuilder, uGUI…)
├─ ProjectSettings/                 ← configuración del proyecto (versión, input, tags…)
├─ README.md                        ← este archivo
└─ .gitignore                       ← ignora Library/, Temp/, builds y archivos de IDE
```

**Scripts de juego** (`Assets/Scripts/`):

| Script | Qué hace |
|---|---|
| `JugadorTerceraPersona` | Movimiento y salto con `CharacterController` (WASD relativo a la cámara, Shift, Espacio) |
| `CamaraTerceraPersona` | Cámara en tercera persona: órbita con el ratón, zoom y evita atravesar obstáculos |
| `AvisoEntradaCancha` | Detecta la entrada a la cancha, pregunta con E/Q, **abre y cierra el portón** y arranca/termina la ronda |
| `ConductorPelota` | Conduce la pelota a los pies y **dispara** según la carga del clic |
| `Pelota` | Aparece/desaparece del punto de castigo y expone velocidad; suena al rebotar |
| `Arquero` | **IA del arquero**: acomodo, achique, predicción del cruce, retardo y lanzamiento |
| `JuezTiro` | Decide **gol / atajada / fallado**, controla los tiempos de la ronda y los mensajes |
| `IndicadorRonda` | Marcador de 5 tiros + 5 atajadas (pinta los círculos y los hace pulsar) |
| `BarraFuerza` | Barra de carga del disparo (verde → rojo) |
| `GuiaComandos` | Guía de comandos del HUD (cambia según estés o no en la ronda) |
| `GestorAudio` | Reproductor central: `GestorAudio.Sonar("gol")` reparte los 10 sonidos (2D y 3D) |

---

## 8. Herramientas del editor (menú **Penales**)

El proyecto trae scripts de editor que **regeneran todo lo que hay en la escena**, por si algo se
rompe o si querés rehacerlo desde cero. Están en la barra de menú superior, en **Penales**.

| Menú | Qué hace |
|---|---|
| `Penales ▸ Crear UI de juego` | Arma el HUD (panel de pregunta, marcador de la ronda, barra de fuerza, mensaje de resultado, guía de comandos) y lo conecta a los scripts. |
| `Penales ▸ Crear arquero y juez de tiro` | Crea el **arquero**, la **pelota** y el **juez de tiro**, y los conecta entre sí. |
| `Penales ▸ Generar sonidos (WAV)` | **Sintetiza los 10 sonidos** (patada, gol, atajada, fallado, silbato, portón, clic, ding, bote, carga) y los guarda como `.wav` en `Assets/Audio`. |
| `Penales ▸ Crear audio del juego` | Crea el objeto **`Audio_Juego`** (con `GestorAudio` + 3 `AudioSource`) y le asigna los WAV. |
| `Penales ▸ Crear animaciones` | Crea los **9 clips `.anim` y los 5 controladores** y les pone el componente **`Animator`** al portón, al mensaje, al panel de aviso, a los 10 círculos del marcador y al arquero. |

**Orden recomendado si querés rehacerlo todo:** Crear UI de juego → Crear arquero y juez de tiro
→ Generar sonidos (WAV) → Crear audio del juego → **Crear animaciones** (esta última al final,
porque necesita el portón y los paneles ya creados). Después guardá la escena (**Ctrl + S**).

---

## 9. Problemas comunes

| Síntoma | Causa y solución |
|---|---|
| **Los controles no responden** | El proyecto usa el **Input System nuevo**. Verificá `Project Settings ▸ Player ▸ Active Input Handling = Input System` (ya viene configurado así). |
| **No se escucha nada** | El `AudioListener` va en la **cámara principal** y el objeto `Audio_Juego` tiene el `GestorAudio`. Si faltan los WAV: `Penales ▸ Generar sonidos (WAV)` y después `Penales ▸ Crear audio del juego`. |
| **La cancha no se cierra ni se abre** | El portón necesita su `Animator` con el controlador `Porton`: corré `Penales ▸ Crear animaciones`. |
| **No aparece el HUD o el marcador no pinta** | `Penales ▸ Crear UI de juego`. |
| **No aparece la pelota ni el arquero** | `Penales ▸ Crear arquero y juez de tiro`. |
| **El ratón no gira la cámara** | **Esc** para recapturar el cursor (el ratón solo orbita con el cursor capturado). |
| **Unity pide otra versión** | Instalá **6000.5.6f1** desde el Hub. Si abrís con una 6.x más nueva, Unity va a pedir confirmar la actualización del proyecto. |
| **La primera apertura tarda mucho** | Es normal: Unity importa los assets y descarga los paquetes (~2 GB en `Library/`). |

---

## 10. Git y entrega

- **Versionado:** la carpeta todavía **no es un repositorio**. Para inicializarlo:
  ```bash
  git init
  git add .
  git commit -m "Penales: juego de tiros de castigo"
  ```
  El `.gitignore` ya deja afuera `Library/`, `Temp/`, `Logs/`, `UserSettings/`, las carpetas de
  build, los `.exe` y los archivos de IDE, así que **el repo pesa ~30 MB**.
- **Si comprimís el proyecto para entregarlo, excluí `Library/`**: el `.zip` baja de ~2 GB a ~30 MB.
- **El ejecutable** se genera con `File ▸ Build Profiles ▸ Windows ▸ x86_64 ▸ Build` y se comparte
  la carpeta que contiene `Penales.exe` + `Penales_Data/`.

---

## 11. Contexto y créditos

- **Trabajo:** Actividad de Construcción Aplicada (ACA) — juego *Penales* (cancha de barrio).
- **Integrantes:** Johan Andres Castelblanco Utria - Luis Enrique Castelblanco Utria - Brayan Steven Castelblanco Utria
- **Cómo se hizo:** modelado de la cancha en Unity (Mesh/ProBuilder), lógica en C# (jugador,
  pelota, arquero con IA y juez), HUD con uGUI, **sonido sintetizado por código** (10 WAV) y
  **animaciones generadas por script** (9 clips + 5 controladores).

| Fecha | Cambio |
|---|---|
| 2026-09-15 | Versión inicial del README: descripción del juego, requisitos, cómo ejecutarlo, controles, guía de la ronda, sonido/animación, estructura y solución de problemas. |
