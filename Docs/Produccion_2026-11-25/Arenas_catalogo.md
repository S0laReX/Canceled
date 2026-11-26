# Cuatro arenas restantes del catálogo (CIERRE-460/461)

Versión 0.7.7, 25/11/2026. Con estas cuatro, el juego de producción en 2.5D tiene ocho arenas. Las cuatro nuevas son **greybox jugable**: su geometría, límites, apariciones, cámara, música y fondo están definidos en datos (`BrawlArena.SideLayouts`, ranuras 4 a 7). Todavía no tienen arte de producción ni prefab propio.

## Revisión de nombres e ID

| Ranura | Nombre | Origen | Conflicto revisado |
|---|---|---|---|
| 4 | Mall del Ego | Arena 3D de la beta (`BuildMall`), adaptada al plano lateral | El nombre ya existía en la beta histórica, ranura 1. Se conserva a propósito: es la misma arena llevada a 2.5D. La beta 3D sigue con sus cuatro arenas y sus ID. |
| 5 | Cumbre de la Liquidez | GDD 1.0, ST04 | Comparte tema cripto con «Núcleo Cripto» de la beta. Son arenas distintas: Núcleo Cripto solo existe en la regresión 3D. |
| 6 | Palacio del Privilegio | GDD 1.0, ST05 (Palacio del privilegio perpetuo) | Nombre abreviado para que quepa en el menú. |
| 7 | Centro de Cumplimiento | GDD 1.0, ST06 | Sin conflicto. |

Descartadas:
- **Hemiciclo / El Gran Debate (ST02):** ya está cubierto por Estudio de Debate.
- **Azotea de la relevancia (ST03):** ya está cubierta por Azotea Influencer.
- **Campus en Llamas (beta):** su tema, un campus en llamas, se presta a leerse como violencia real.

## Distribuciones (medidas contra salto ≈ 2,3 m y doble salto ≈ 4,3 m)

| Arena | Suelo | Plataformas atravesables | Foso | Decisión que plantea |
|---|---|---|---|---|
| Mall del Ego | 18 m | Kiosco central bajo (cara a 1,5 m, 3,2 m de ancho) y dos entresuelos a ±6,4 m (cara a 3,2 m) | — | Tres alturas: el kiosco se toma con un salto y los entresuelos piden doble salto. Se rota entre niveles. |
| Cumbre de la Liquidez | 19 m, partido | Pasarela central alta sobre el foso (cara a 3,3 m, 4 m de ancho) y dos plataformas a ±6 m (cara a 2,4 m) | 3,5 m en el centro | Cruzar por arriba o pelear a un lado. Caer al foso exige recuperarse o cuesta una vida. |
| Palacio del Privilegio | 13 m | Una sola tarima alta central (cara a 3,0 m, 5,2 m de ancho) | — | Dominar el centro alto. Los bordes están cerca y cualquier lanzamiento lateral amenaza. |
| Centro de Cumplimiento | 20 m | Archivo alto central (cara a 3,7 m, 3,6 m de ancho) y dos ventanillas bajas a ±7 m (cara a 2,25 m) | — | Cuesta más sacar al rival y gana quien controla la altura. |

Reglas comunes a las ocho arenas:
- Límites de KO a ±22 m, −10 m abajo y +20 m arriba. Ninguno en profundidad.
- Cinco apariciones en el plano, separadas 2 m o más, sobre suelo real o sobre la plataforma central.
- La cámara encuadra siempre todo el suelo y todas las plataformas (`PlayBounds`).
- Variante competitiva: sin peligros, sin piezas móviles y sin destrucción.

## Fondo y lectura (GDD §9)

- **Paletas:** suelos y fondos apagados. El color saturado queda solo en los cantos de las plataformas, que son finos.
- **Decorado:**
  - Mall del Ego: escaparates.
  - Cumbre de la Liquidez: barras de gráficas y un cohete de cartón.
  - Palacio del Privilegio: tapices y un reloj detenido.
  - Centro de Cumplimiento: ventanillas, carpetas gigantes y un archivador.
- **Colisión del decorado:** todo el decorado va detrás del plano y sin colisión. Ningún objeto continúa la línea de una plataforma.
- **Fachada bajo el suelo:** marca dónde acaba el suelo pisable.
- **Ajustes por `-read-smoke`:**
  - Paredes y fondos en grises medios para Cumbre, Centro y Mall: los luchadores oscuros, como Commander Vex, se perdían sobre paredes casi negras.
  - Fondo oliva apagado y tapices pizarra en Palacio, en vez de tonos cálidos.
  - Pilares de la tarima y archivador en piedra media: el contorno oscuro de los luchadores se fundía con los pilares negros.
- **Atenuado del fondo:** las ranuras 4 a 7 no usan el atenuado de `BrawlBackgroundGrade`, porque no tienen texturas de escenario. `-read-smoke` mide el §9 igual que en las cuatro arenas con arte.

## Música

Una pista sintetizada por arena, sin melodías reconocibles ni voces:

| Arena | Pista | Estilo | Tempo |
|---|---|---|---|
| Mall del Ego | «Liquidación total» | disco | 112 BPM |
| Cumbre de la Liquidez | «Mercado alcista» | trance barato | 136 BPM |
| Palacio del Privilegio | «Reloj detenido» | barroco sintético | 92 BPM |
| Centro de Cumplimiento | «Ventanilla única» | lounge de oficina | 86 BPM |

## Verificación automática

Las suites que recorren arenas cubren ahora las ocho:
- `-arena-smoke`: límites exactos, apariciones y plataformas en su sitio. También comprueba que el kit de menor alcance sube a cada plataforma y lo que pasa al bajar. En Cumbre de la Liquidez comprueba además que el foso no tiene suelo y sus bordes sí.
- `-camera-smoke`
- `-side-smoke`
- `-read-smoke`
- `-arena-stress`: 20 partidas por arena.
- `-perf-smoke`
- `-toon-smoke`
- `-music-smoke`

## Pendiente

- **Arte de producción:** modelado con Meshy o a mano, más el prefab con la misma huella. Generar con Meshy consume créditos y requiere aprobación previa del coste.
- **Calibración con personas:** dificultad de cada distribución, sobre todo del foso de la Cumbre y del suelo corto del Palacio.
