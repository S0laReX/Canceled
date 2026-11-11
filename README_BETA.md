# Cancel Brawl beta local 0.3

Beta jugable de combate en plataformas 3D basada en el GDD beta v0.3. Sirve para comprobar si el salto, el control en profundidad, las expulsiones y los cinco kits se entienden al jugar. El contenido, las cifras y la presentación son provisionales; esta entrega corresponde a los hitos 1 a 4 del GDD en versión local y **no completa el MVP** (faltan online y ranking).

Incluye los **cinco personajes iniciales**: Don Decreto, Lola Loop, Coach Tóxico, Cripto King y Tío Antena. Cada uno tiene su propio especial, recuperación y super. También incluye las **cuatro canchas**: Azotea Neón, Mall del Ego, Campus en Llamas y Núcleo Cripto, construidas con geometría simple. Admite de dos a cinco participantes entre jugadores locales y bots de práctica.

## Cómo jugar

En Windows, abre `Builds/Windows/CancelBrawlBeta.exe`. Conserva el ejecutable junto con sus carpetas y archivos de la misma compilación.

En Unity, abre `Assets/CancelBrawlBeta/Scenes/CancelBrawlBeta.unity` y pulsa **Play**. En el menú:

1. Elige tu luchador (P1). Haz clic otra vez en la tarjeta elegida para cambiar de aspecto.
2. Elige la cancha.
3. Elige de uno a cuatro rivales. Haz clic en un puesto para cambiar su personaje y clic derecho para cambiar su aspecto. Por defecto rotan para que una partida de cinco muestre a todo el elenco. Si dos puestos llevan el mismo personaje y el mismo aspecto, el segundo recibe el siguiente aspecto libre.

Pulsa **Enter** o el botón de entrada. En resultados, **Enter** inicia la revancha.

Por defecto juegas contra IA. Para sumar personas, conecta los mandos antes de empezar y activa **Mandos locales**: P1 usa teclado y ratón, y hasta cuatro mandos controlan P2 a P5. Los puestos restantes los ocupan bots. Los mandos que sobran no participan: el máximo es cinco jugadores, y el menú lo avisa. En modo solo, el primer mando también puede controlar P1 junto con el teclado; no crea otro participante.

## Controles

Las etiquetas del mando corresponden a la distribución Xbox; entre paréntesis, la posición del botón para otros mandos.

| Acción | Teclado y ratón | Mando |
| --- | --- | --- |
| Moverse | WASD | Palanca izquierda |
| Orientar ataques | Ratón | Palanca derecha |
| Saltar y segundo salto | Espacio, pulsar otra vez en el aire | A (botón inferior) |
| Básico y cadena corta | Clic izquierdo, pulsaciones sucesivas | X (botón izquierdo) |
| Ataque fuerte | Clic derecho | Y (botón superior) |
| Especial | E | B (botón derecho) |
| Recuperación aérea | R en el aire (con dirección) | RB en el aire |
| Esquiva | Mayúsculas y dirección | RT y dirección |
| Super con barra llena | Q | LB |
| Caída acelerada | Mantener Control en el aire | Mantener LT en el aire |
| Pausar o reanudar | Esc | Start/Menu |
| Iniciar o revancha | Enter | A desde menú o resultados |
| Salir del ejecutable | Alt + F4 | — |

El ataque básico y el fuerte tienen una ayuda de orientación suave: corrigen errores pequeños hacia un rival visible dentro de un cono corto, nunca a través de obstáculos.

## Elenco

| Personaje | Papel | Especial | Recuperación | Super |
| --- | --- | --- | --- | --- |
| Don Decreto | Control frontal; resiste más el empuje | **Decreto exprés**: placa recta a altura fija que choca con obstáculos | **Subida en las encuestas**: impulso alto, poco control horizontal | **Mayoría absoluta**: abanico frontal de 150° marcado en el suelo |
| Lola Loop | Movilidad; resiste menos el empuje | **Ola de hate**: tres etiquetas con dirección y altura fijas | **Tendencia ascendente**: gran ajuste lateral, menos altura | **Viralización masiva**: zona circular marcada delante que estalla tras 0,7 s |
| Coach Tóxico | Pelea cercana; el más pesado | **Serie al fallo**: avance corto y comprometido; no atraviesa paredes | **Día de pierna**: salto explosivo con muy poco control lateral | **Preentreno nuclear**: onda por el suelo; solo alcanza a quien está apoyado a una altura parecida |
| Cripto King | Proyectiles y zonas | **Token rebote**: aviso de trayectoria, hasta 3 rebotes, desaparece al primer impacto | **A la luna**: curva amplia que hay que orientar al iniciarla | **Burbuja especulativa**: esfera anunciada que crece 1,2 s y estalla |
| Tío Antena | Trampas y desvíos | **Interferencia**: emisor visible que se arma en 0,35 s y lanza hacia arriba al primer rival; solo uno activo, dura 7 s | **Me están llevando**: haz de abducción lento, gravedad reducida, altura limitada | **La señal definitiva**: barrido circular con dos sectores seguros marcados en blanco a los lados |

Parámetros de partida (se ajustan con pruebas):

| Personaje | Básico (rango) | Fuerte daño / preparación / rango | Especial daño | Super daño / alcance / altura | Resistencia |
| --- | --- | --- | --- | --- | --- |
| Don Decreto | 5–7 % · 2,1 m | 13 % · 0,34 s · 2,9 m | 10 % | 22 % · 6,5 m · 2,5 m | 1,08 |
| Lola Loop | 5–7 % · 1,95 m | 10 % · 0,22 s · 3,0 m | 8 % ×3 proyectiles | 20 % · radio 3,2 m · 2,5 m | 0,90 |
| Coach Tóxico | 5–7 % · 1,75 m | 14 % · 0,42 s · 2,5 m | 12 % | 24 % · 7,5 m · tolerancia 0,8 m | 1,12 |
| Cripto King | 5–7 % · 1,9 m | 12 % · 0,26 s · 3,1 m | 9 % | 22 % · radio 3,6 m (esfera) | 0,97 |
| Tío Antena | 5–7 % · 2,35 m (arco amplio) | 12 % · 0,36 s · 3,2 m | 9 % | 21 % · 7 m · 2,2 m | 1,00 |

El ataque fuerte muestra una flecha en el suelo con su dirección durante la preparación. Todos los supers marcan en el suelo su área horizontal y su alcance vertical.

## Modelos y animación

Los luchadores usan once modelos 3D importados de Meshy (`Assets/CancelBrawlBeta/Art/Characters`). Cada personaje tiene un aspecto por defecto y otros alternativos:

| Personaje | Aspecto por defecto | Alternativos |
| --- | --- | --- |
| Don Decreto | The Dapper Tycoon | Victoria Vexley |
| Lola Loop | Pink Pop Princess | Ruby Starlight, Neon Rebel |
| Coach Tóxico | Turbo Max | Commander Vex |
| Cripto King | Smirking Tycoon | The Regal Duelist |
| Tío Antena | Tin Foil Fred | Neon Mischief |

Los modelos llegaron sin esqueleto ni animaciones. `Runtime/BrawlAutoRig.cs` les genera un esqueleto de 11 huesos al cargar: cadera, pecho, cabeza, brazos y antebrazos, muslos y espinillas. Calcula las articulaciones a partir de la silueta en pose A y los pesos según la distancia a cada extremidad. `BrawlFighterVisual.Model.cs` anima ese esqueleto por código:

- Reposo, carrera y retroceso.
- Salto, caída y aterrizaje con compresión.
- Esquiva y recuperación aérea.
- Golpe recibido.
- Tres golpes de la cadena básica.
- Ataque fuerte por encima de la cabeza.
- Un especial por estilo: lanzamiento, ráfaga, embestida o colocación del emisor.
- Carga y liberación de la super.

Las animaciones son solo visuales: no cambian alcances, tiempos ni colisiones.

Para añadir un aspecto, crea `Art/Characters/Nombre/Nombre.fbx` y `Nombre_Albedo.png`, añade `"Nombre"` a `Skins` del kit en `BrawlRoster.cs` y ejecuta **Cancel Brawl → Actualizar modelos de personaje**. **Cancel Brawl → Previsualizar poses de personaje** dibuja cada aspecto en seis poses dentro de `Evidence/characters`. Un modelo nuevo debe estar en pose A o T, mirando a +Z. Si falta un modelo, el luchador usa las primitivas de la 0.2.

## Canchas

| Cancha | Distribución | Decisión |
| --- | --- | --- |
| Azotea Neón | Dos azoteas, cruce central, terrazas elevadas y plataformas bajas de retorno | Disputar el cruce o arriesgar un salto lateral |
| Mall del Ego | Atrio con kiosco central, dos pasarelas a 1,8 m y plataformas laterales a media altura | Rodear el kiosco o buscar ventaja desde arriba |
| Campus en Llamas | Plaza con estatua, paseo frontal, escalinatas y dos terrazas a 2 m accesibles por rampa o salto | Controlar la altura sin perder la escapatoria |
| Núcleo Cripto | Plataforma central elevada, cuatro plataformas suspendidas y conexiones en L desplazadas | Camino largo y seguro, o salto corto junto al vacío |

Las plataformas son fijas, sin destrucción ni peligros aleatorios. "Campus en Llamas" no añade daño por fuego. Cada cancha tiene sus propios límites exteriores e inferior, de cinco a seis puntos de aparición y un encuadre de cámara propio. Cuando un elemento tapa a un luchador, la cámara lo vuelve semitransparente.

## Reglas de esta prueba

- Tres vidas por participante y cinco minutos por partida.
- El daño aumenta el empuje recibido. Llegar a 100 % no elimina: pierdes una vida al salir por los límites exteriores o caer por debajo de la cancha.
- Tienes doble salto y una recuperación por fase aérea. Aterrizar restablece los recursos aéreos.
- La barra de descontrol se carga conectando ataques: 8 puntos por cadena básica y 12 por especial, una vez por activación. Con 100 puntos puedes lanzar un super; consume toda la barra aunque falle, y sus impactos no recargan.
- Perder una vida reinicia daño y barra. La reaparición tarda dos segundos y concede dos segundos de protección visible; atacar la cancela. El punto de aparición evita a los rivales y las zonas o emisores activos.
- Gana el último con vidas. Si acaba el reloj, se ordena por más vidas y luego por menor daño. Los empates exactos comparten puesto.
- Una caída se atribuye al último rival que te golpeó en los 8 segundos anteriores; si no, cuenta como caída propia. Los KO, las caídas propias y el daño infligido aparecen en los resultados como estadísticas: no cambian los puestos.

Al quedar eliminado, puedes observar la continuación de la partida. La IA es simple y sirve para practicar localmente; los bots no representan jugadores online ni generan rating.

## Opciones y accesibilidad

En el menú y en la pausa:

- **Reducir destellos y temblor**: menos partículas, sin parpadeo en la protección y sin temblor de cámara.
- **Volumen de efectos** y **volumen de música**, por separado.

Las opciones se guardan en el equipo. Los avisos esenciales (super listo, expulsión, áreas de super) tienen señal visual además del sonido, y los identificadores P1–P5 complementan los colores.

## Cambios respecto a la 0.2

- Once modelos 3D con esqueleto y animación generados por código. Antes eran primitivas.
- Aspectos seleccionables para P1 y para cada rival, y aspectos distintos automáticos en los duelos espejo.
- Catálogo `Data/CharacterArt.asset` enlazado desde `BetaBalance.asset`.
- Dos comprobaciones nuevas en la prueba automática: los cinco usan su modelo y P1 tiene esqueleto con pesos.

## Cambios respecto a la 0.1

- Elenco completo de cinco kits definidos como datos en `Runtime/BrawlRoster.cs`. Añadir un luchador nuevo consiste en añadir una entrada a esa lista, más su modelo en `BrawlFighterVisual`.
- Cuatro canchas con límites, apariciones y encuadre propios (`Runtime/BrawlArena.cs`). Antes los límites eran globales.
- La super de Lola ya no estalla alrededor de ella: marca una zona a distancia, como pide el GDD.
- Aviso de dirección del ataque fuerte y ayuda de orientación suave.
- Atribución de expulsiones con ventana de 8 s y estadísticas en resultados.
- Atenuación de obstáculos que tapan a un luchador.
- Música, volúmenes separados y opción de efectos reducidos.
- IA genérica: salta huecos cuando hay aterrizaje alcanzable, rodea por puntos seguros y usa doble salto y recuperación para volver.
- Aviso de mandos sobrantes por encima de cinco jugadores.

## Alcance pendiente

Quedan pendientes, por orden del GDD:

- **Online y ranking persistente** (hito 5): salas, sincronización, servidor que valide resultados, rating y rangos. No hay nada implementado ni simulado; la partida es siempre libre y local.
- **Remapeo de controles** y tamaño de interfaz ajustable.
- **Arte y animación final**: los modelos son de Meshy y el esqueleto es automático. Al levantar mucho los brazos, la ropa de hombros y axilas se estira, y algún dedo puede deformarse. Para la versión final conviene un rig real (Mixamo, el rigging de Meshy o un artista) y clips de animación con Animator. Los aspectos no tienen todavía accesorios propios (mazo, teléfono o antena) ni retrato en el menú.
- **Pruebas con personas y mandos físicos** en grupos de dos a cinco, y medición de rendimiento en el PC de referencia que acuerde el equipo.
- **Equilibrio**: los valores de daño, empuje y alcance son de partida.

El balance global se ajusta en `Assets/CancelBrawlBeta/Data/BetaBalance.asset` (vidas, tiempo, movimiento, daño base, empuje y ventana de atribución). Los ajustes propios de cada kit están en `BrawlRoster.cs`. El material de atenuación es `Assets/CancelBrawlBeta/Data/OccluderFade.mat`.

Copias recuperables:

- Proyecto inicial: `Backups/TemplateBeforeBeta.zip`.
- Beta 0.1 antes de esta revisión: `Backups/Beta0.1BeforeV0.2.zip`.

La escena `SampleScene` se conserva.

## Evaluación y evidencias

La prueba del ejecutable 0.3 completó **67 comprobaciones** sin fallos, registradas en `Evidence/smoke-report.json`. Además de las de la 0.1, cubre:

- El especial y la super de cada personaje, el rebote limitado y el emisor único.
- El sector seguro de Tío Antena y que el super no recargue la barra.
- La atribución de KO.
- Las apariciones de las cuatro canchas.
- Que los cinco aterricen sin caer y que una partida de cinco muestre a todo el elenco.
- La atenuación de obstáculos.
- Nueve segundos de IA en cada cancha.

Las capturas `arena-0.png` a `arena-3.png` muestran cada cancha durante esa prueba.

Las pruebas de gameplay usan entradas sintéticas y bots: no validan hardware de mando ni la diversión. La IA cae sola de vez en cuando en Núcleo Cripto; el informe lo registra en `notes`.

Para repetir la prueba, abre PowerShell en la carpeta del proyecto y ejecuta:

```powershell
Start-Process -FilePath '.\Builds\Windows\CancelBrawlBeta.exe' -ArgumentList '-beta-smoke -force-d3d11 -screen-width 1280 -screen-height 720 -screen-fullscreen 0' -WorkingDirectory (Get-Location).Path -Wait
```

La prueba dura unos dos minutos y medio, cierra el juego al terminar y escribe las evidencias en `Evidence`. Para regenerar el ejecutable desde Unity, usa **Cancel Brawl → Generar beta Windows**. Si la escena ya existe, la reutiliza y no toca la escena abierta.
