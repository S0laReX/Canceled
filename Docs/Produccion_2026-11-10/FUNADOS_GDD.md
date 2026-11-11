# FUNADOS

## Game Design Document principal

**Versión:** 1.0 · **Fecha de diseño:** 10 de noviembre de 2026 · **Idioma de trabajo:** español.

**Género:** lucha de plataformas 3D con movimiento libre sobre arenas y plataformas. **Dirección visual:** cartoon-anime, siluetas exageradas, cel shading de lectura clara. **Plataforma inicial:** Windows, juego local. **Experiencia prioritaria de validación:** partidas de 1 contra 1; la beta documenta hasta cinco participantes locales o bots y esa capacidad debe conservarse mientras se prueba por separado.

**Responsable de producto:** Santiago. **Estado:** base de diseño para producción. Las cifras de combate, presupuestos gráficos y duración de hitos de este documento son hipótesis de trabajo que deben validarse con prototipos. Las instalaciones y versiones efectivas se registran en el informe técnico que acompaña esta entrega.

### Cómo usar este documento

Este GDD es la referencia de intención y aceptación. Trello registra el trabajo; Miro explica relaciones, decisiones y referencias; el repositorio conserva código, datos y documentación versionada. Un cambio aprobado de reglas se refleja en este documento y enlaza la tarjeta que lo implementa. Las decisiones pendientes tienen propietario, prueba y condición de cierre; no se convierten en funcionalidades automáticamente.

El roster conserva los trece arquetipos solicitados. Don Promesio y Lola Viral mantienen los nombres del GDD original elegidos por el usuario. Los demás nombres de trabajo nuevos deben revisarse antes de publicación. La vertical slice de validación concentra pruebas en dos personajes y una arena sin borrar los cinco kits y cuatro arenas que ya documenta la beta.

### Base existente y objetivo de esta versión

El proyecto localizado está en `D:/Funados`. Su `README_BETA.md` describe Cancel Brawl beta local 0.3: combate 3D libre, cinco kits, cuatro arenas, once modelos Meshy con rig automático, teclado/ratón, mandos y bots. Esa documentación describe una prueba histórica de 67 comprobaciones; este GDD no afirma haber repetido esas pruebas ni validado hoy sus mandos, rendimiento o diversión.

La versión del proyecto inventariada es Unity 6000.5.7f1, URP 17.5 e Input System 1.20. La versión efectiva y los cambios realizados se contrastan con el informe técnico de esta entrega. El nombre interno `CancelBrawlBeta` y sus rutas no se renombrarán solo por adoptar el nombre público FUNADOS.

**Decisión vigente del usuario:** conservar movimiento 3D libre. No se planifica una conversión lateral 2.5D. Don Decreto se mapea editorialmente a Don Promesio y Lola Loop a Lola Viral; ese mapa no autoriza renombrar assets, clases o guardados sin una tarea técnica de migración.

Hay diferencias entre documentos: el GDD original proponía 3 stocks/4 minutos y la beta describe 3 stocks/5 minutos. Para la siguiente prueba de diseño se adopta 3 stocks/4 minutos, con valor configurable y sin afirmar que ya se cambió el proyecto. La beta llama a la barra «descontrol»; el diseño público recupera «escándalo», conservando la misma idea de carga individual. Las reglas nuevas de este GDD son objetivos, no una lista de funcionalidades ya implementadas.

## 1. Visión y promesa

### 1.1 Concepto

En FUNADOS, una productora corrupta organiza un reality clandestino que convierte las polémicas en entretenimiento. Trece figuras obsesionadas con la reputación transforman sus privilegios, discursos y contradicciones en armas absurdas. Se enfrentan en arenas 3D que satirizan el espectáculo público: azoteas de neón, centros comerciales de vanidad, estudios televisivos o palacios que siguen viviendo en otro siglo. Cada golpe aumenta la vulnerabilidad al lanzamiento; cada vida termina al cruzar los límites de la arena o caer al vacío.

La diversión debe surgir del movimiento, la lectura del rival y las recuperaciones arriesgadas. La sátira aporta identidad, animaciones y contexto sin convertirse en un requisito para entender las reglas. Un jugador que no reconozca una referencia social debe poder disfrutar del combate por completo.

**Promesa en una frase:** gana la discusión sacando a tu rival del escenario, en una pelea ágil de egos descomunales y consecuencias exageradas.

### 1.2 Pilares de diseño

1. **Lectura antes que ruido.** Debe distinguirse quién golpea, qué zona amenaza y cuándo puede responderse. Los efectos, voces y decoración respetan esa jerarquía.
2. **Fácil entrar, interesante dominar.** Movimiento libre, apuntado comprensible, básico, fuerte, especial, recuperación y super. Profundidad en espaciado tridimensional, esquiva, control aéreo y recuperación; sin secuencias largas obligatorias.
3. **La sátira se juega.** Cada arquetipo expresa su contradicción mediante mecánicas con coste y contrajuego: el político promete, el gurú especula, la censura delimita zonas.
4. **La habilidad decide.** Sin mejoras de estadísticas vendidas, ventajas por progresión, equipamiento competitivo ni aleatoriedad oculta en el combate base.
5. **Una buena partida local primero.** La prioridad es el tacto de dos personas compartiendo pantalla. El online será una inversión posterior con puerta de viabilidad explícita.
6. **Producción sostenible.** Pocos personajes excelentes antes que muchos incompletos. Arte modular, datos reutilizables y herramientas justificadas por una necesidad comprobada.

### 1.3 Objetivos de experiencia

- En el primer minuto, una persona comprende que acumular daño facilita expulsar al rival.
- En cinco minutos puede volver a una plataforma usando salto y recuperación aérea.
- Una derrota deja una causa reconocible: mala posición, ataque fallado, defensa predecible o recuperación castigada.
- Una victoria genera al menos un momento que merece comentarse: un rescate, un castigo, un intercambio al borde o un KO con gran anticipación.
- La identidad se reconoce en silueta, pose y movimiento antes de leer un nombre.

### 1.4 Límites de visión

No se contempla mundo abierto, campaña cinematográfica, editor de personajes, dieciséis tipos de moneda, sistemas de equipo con estadísticas, generación procedural de ataques ni avatares importados por usuarios. No hay compromisos de crossplay, clasificación competitiva o servidores dedicados en el MVP.

## 2. Audiencia, tono y posicionamiento

### 2.1 Audiencia

Público principal propuesto: adolescentes mayores y adultos que disfrutan partidas locales, juegos de lucha accesibles y humor sobre poder, fama y reputación. Público secundario: jugadores competitivos interesados en una base legible y creadores de contenido que valoren personajes expresivos. La clasificación por edad se determinará con el contenido real y los procesos de las plataformas; no se presupone una clasificación oficial.

El lanzamiento inicial necesita funcionar sin conocimiento previo del género. El entrenamiento explica movimiento, daño, lanzamiento, KO, defensa y recuperación de forma gradual. El competitivo aporta objetivos posteriores, sin exigir memorización externa para una primera partida satisfactoria.

### 2.2 Tono satírico

El blanco principal son los comportamientos: corrupción, oportunismo, privilegio, manipulación, culto a la imagen, desinformación y explotación de la atención. Los personajes son ficticios y compuestos. No se usan nombres, voces clonadas, rostros reconocibles, marcas o biografías de personas reales como atajo de diseño.

La influencer no se define por su género o color de pelo, sino por la distancia entre la imagen cuidadosamente fabricada y la necesidad de controlar el relato. El activista performativo satiriza la búsqueda de prestigio por encima de la acción, sin convertir la defensa de derechos en el chiste. El aristócrata cuestiona el privilegio heredado; el magnate, la concentración de poder. La hacker no es moralmente perfecta: confunde control técnico con ausencia de consecuencias.

La violencia es caricaturesca, sin gore realista. Los KO son expulsiones escénicas, no ejecuciones. Las frases tienen versiones sin lenguaje fuerte; el humor nunca es imprescindible para recibir información de combate.

### 2.3 Reglas editoriales

- Cada chiste debe tener una conducta identificable como objetivo.
- La mecánica sigue siendo comprensible al quitar la frase humorística.
- No se recompensa acosar, revelar datos personales o castigar a una comunidad real.
- Antes de cerrar un personaje se revisan nombre, imagen, símbolos, parecido y contexto cultural.
- Una frase que solo funciona humillando un rasgo protegido se reescribe.
- Las descripciones de marketing no presentan las caricaturas como retratos de personas concretas.

## 3. Bucles de juego y estructura de sesión

### 3.1 Bucle de segundos

Leer distancia y estado del rival → elegir movimiento, ataque o defensa → resolver impacto o fallo → ajustar posición → buscar ventaja cerca del borde → intentar expulsión o recuperación. La decisión central es intercambiar seguridad por posibilidad de daño o KO.

### 3.2 Bucle de partida

Unirse localmente → elegir personaje y aspecto → confirmar escenario y reglas → combatir → ver resultado con rematch inmediato → cambiar personaje o repetir. La revancha debe estar disponible en dos acciones de confirmación o menos desde el resultado.

### 3.3 Bucle de aprendizaje

Descubrir una interacción → practicarla en laboratorio → probarla contra otra persona → identificar un fallo → repetir con una meta concreta. El modo entrenamiento mostrará estado y datos útiles sin obligar a utilizarlos en juego casual.

### 3.4 Bucle de progresión

Jugar partidas o completar retos de aprendizaje → obtener contenido cosmético mediante objetivos transparentes → expresar identidad → volver a jugar. La progresión no modifica daño, peso, velocidad ni acceso a información competitiva.

### 3.5 Ritmo esperado

La prueba principal propone tres stocks y cuatro minutos, siguiendo la identidad del GDD original. Se compara con el valor de cinco minutos documentado en la beta antes de cerrar balance. El ritmo alterna neutral, ventaja, borde y reinicio tras stock. Si todas las partidas terminan por tiempo, debe investigarse movilidad, profundidad de la arena y poder de cierre antes de reducir el temporizador.

## 4. Reglas del combate

### 4.1 Espacio de juego 3D

El desplazamiento horizontal ocurre libremente en X/Z; Y representa altura, salto y caída. No hay un eje lateral fijo ni carriles de profundidad. Un luchador puede rodear al rival, cruzar pasarelas y atacar desde distintos ángulos. Movimiento y orientación ofensiva son entradas separadas cuando el dispositivo lo permite.

La cámara local es compartida y elevada, con encuadre oblicuo propio por arena, como base de la beta. La cámara no gira por orden de un solo jugador. Debe mostrar rutas, bordes y alturas; una adaptación limitada de zoom o seguimiento multiobjetivo se adopta solo si mejora lectura sin mareo. Un punto de apoyo, sombra de contacto e indicadores de altura comunican dónde aterrizará cada cuerpo. Las oclusiones se atenúan, sin volver invisible la colisión real.

### 4.2 Condiciones de victoria

Regla estándar propuesta: tres stocks por participante y cuatro minutos. El porcentaje de daño comienza en 0; puede superar 100 y no equivale a salud restante. Un KO ocurre cuando el punto de referencia del luchador sale del volumen exterior válido de la arena o cae bajo su límite inferior. No se presume un KO superior: cualquier techo de expulsión debe definirse expresamente y probarse. Se pierde un stock y el personaje reaparece con daño y escándalo en 0.

Gana quien conserve stocks cuando el rival agota los suyos. Al terminar el tiempo, gana quien tenga más stocks; con igualdad, quien tenga menos porcentaje. Si ambos valores empatan, el MVP declara empate y ofrece revancha. El modo casual posterior podrá ofrecer desempate, pero no añade una regla poco probada al prototipo.

Los KO se evalúan después de resolver todos los impactos del mismo tick. Una doble expulsión en el último stock da empate. No se decide por el orden de actualización de objetos.

### 4.3 Reaparición

Se conserva como base la reaparición documentada en la beta: espera de dos segundos y aparición en un punto seguro, evitando rivales, trampas y zonas activas. Dos segundos de protección visible terminan antes al atacar. El modo de destellos reducidos sustituye parpadeo por un contorno o icono estable. El respawn no debe dejar al personaje dentro de geometría ni crear un refugio indefinido.

### 4.4 Estados principales

Idle, caminar/correr, frenado, giro, salto inicial, ascenso, caída, caída rápida, aterrizaje, ataque básico, fuerte, especial, super, esquiva, lanzamiento, hitstop, hitstun, recuperación, KO y reaparición. Cada estado define entradas admitidas, transiciones, invulnerabilidad y recursos consumidos. Guardia, agarres y adhesión a bordes quedan fuera de la vertical slice; no se añaden por convención del género.

Una interrupción nunca debe dejar una hitbox activa, una corutina pendiente o una invulnerabilidad permanente. El estado de combate es la fuente de verdad; la animación representa su resultado.

## 5. Movimiento y recuperación

### 5.1 Base común

Todos los personajes disponen de desplazamiento libre sobre X/Z, salto, un segundo salto aéreo, control aéreo y caída rápida. Las plataformas sólidas son la base; atravesarlas desde arriba solo se incorpora si una arena lo requiere con señal y control específicos. Una dirección de movimiento no equivale a salto ni activa por sí sola un especial.

La orientación depende del ratón o del stick derecho. Sin entrada de apuntado válida, conserva la última orientación; puede usar la dirección de movimiento como respaldo configurable. El giro durante anticipación se limita por ataque; al activar la hitbox, la dirección queda fijada salvo que el movimiento permita seguimiento. No se utiliza root motion para desplazar al luchador.

### 5.2 Valores iniciales del prototipo

Se establece una unidad de mundo cercana a un metro para facilitar el pipeline, sin pretender realismo físico. Referencia para el luchador medio:

| Propiedad | Hipótesis inicial | Prueba de validación |
|---|---:|---|
| Velocidad máxima horizontal X/Z | 7 unidades/s | Cruzar la arena sin perder precisión; normalizar diagonales |
| Aceleración al correr | 42 unidades/s² | Arranque legible y control de frenada |
| Velocidad inicial de salto | 11 unidades/s | Alcanzar plataforma lateral desde reposo |
| Gravedad de ascenso | 28 unidades/s² | Arco expresivo con ventana de decisión |
| Gravedad de caída | 34 unidades/s² | Evitar flotación excesiva |
| Caída rápida | 1,5 veces la caída normal máxima | Control deliberado de aterrizaje |
| Buffer de entradas | 5 ticks a 60 Hz | Consistencia al actuar cerca del aterrizaje |
| Coyote time de salto desde suelo | 3 ticks | Tolerancia al borde sin saltos invisibles largos |
| Salto aéreo disponible | 1 | Recurso visible en entrenamiento |

Estas magnitudes no se copian a ciegas a código existente. Se miden distancia y altura reales, incluyendo aceleración, cancelación de salto, colisión y paso fijo. La geometría del escenario se ajusta a ese resultado.

### 5.3 Recuperación

Cada personaje tiene una recuperación aérea independiente del especial principal. Usarla consume el recurso hasta aterrizar en una superficie válida; se conserva esta regla de la beta. Recibir un impacto no devuelve automáticamente salto ni recuperación. Se define la dirección horizontal inicial con el apuntado o movimiento y se limita el ajuste posterior por personaje.

La recuperación comunica altura máxima, deriva y vulnerabilidad. El HUD de entrenamiento muestra segundo salto y recuperación disponibles. Se prueba regresar desde varios ángulos alrededor de una plataforma, no solo desde izquierda y derecha. El inicio de la recuperación no puede atravesar paredes ni colocar al luchador sobre una plataforma sin comprobar la trayectoria.

### 5.4 Bordes y aterrizaje

La vertical slice no añade agarre automático de borde. El luchador debe ganar altura y aterrizar sobre una superficie válida. Los bordes reciben contraste y una forma coherente; un saliente decorativo no parece una plataforma segura si no tiene colisión.

Se prueban esquinas, encuentros entre rampas, saltos oblicuos y caída junto a paredes. El controlador distingue suelo válido, pared y techo; no restaura recursos al rozar una superficie lateral. Una posible tolerancia de aterrizaje debe ser simétrica y pequeña, y jamás sustituir una prueba de colisión de alta velocidad.

## 6. Ataques, defensa y resolución de impacto

### 6.1 Repertorio común

Se conserva el esquema del juego 3D existente: básico con cadena corta, ataque fuerte, especial principal, recuperación aérea, esquiva y super con barra completa. Las mismas acciones pueden tener ajustes aéreos explícitos, sin multiplicar el moveset antes de validarlo.

El ataque básico facilita contacto y acumulación; el fuerte ofrece lanzamiento a cambio de anticipación; el especial expresa la identidad; la recuperación conserva posición; el super consume un recurso ganado por acertar. No se añaden comandos de arriba/abajo propios de un plano lateral a la entrada de apuntado 3D.

### 6.2 Estructura temporal

Cada ataque define anticipación, ventana activa y recuperación. Las cifras se expresan en ticks de simulación a 60 Hz; un tick equivale aproximadamente a 16,67 ms. Un ejemplo inicial de golpe neutral es 5 ticks de anticipación, 3 activos y 12 de recuperación. No es una promesa de balance ni un valor aplicable a todos los personajes.

Las animaciones muestran la anticipación antes del contacto; los efectos de impacto aparecen cuando la simulación confirma el golpe. Un personaje no cancela libremente su recuperación al fallar. Los cancel específicos se enumeran por movimiento y se prueban para impedir bucles infinitos.

### 6.3 Hitboxes y hurtboxes

La hitbox representa un volumen ofensivo 3D; la hurtbox, la zona 3D que puede recibir daño. Se usan formas simples como esferas, cápsulas, cajas o sectores con tolerancia vertical explícita. Su orientación sigue el vector ofensivo en X/Z y su altura sigue la pose/regla del movimiento. La silueta artística no es una promesa exacta de colisión, pero la diferencia debe ser razonable y visible en depuración.

El collider de movimiento permanece estable y separado. El cabello, las capas, sombreros o antenas no amplían automáticamente la hurtbox. Una pose de esquiva puede reducirla si el diseño lo especifica; la deformación del modelo por sí sola no cambia las reglas.

Cada instancia de ataque tiene identificador; cada objetivo solo recibe el número de impactos autorizado. Un multigolpe define intervalos de reimpacto y golpe final expresamente. Las pruebas cubren ocho direcciones horizontales, objetivos a distinta altura, esquinas, rampas, cambio de estado, aterrizaje e interrupción. Los proyectiles rápidos necesitan barrido de trayectoria o subpasos comprobados; un overlap al final puede saltarse un objetivo.

### 6.4 Daño y knockback

El daño acumulado aumenta la potencia de lanzamiento. Se adopta una fórmula propia sencilla para prototipar, no una reproducción de la fórmula de otro juego:

`K = (B + G × (P_antes + d) / 100) × (100 / W)`

`K` es la velocidad de lanzamiento en unidades/s; `B` es lanzamiento base del movimiento; `G` es crecimiento; `P_antes` es porcentaje previo al impacto; `d` es daño del golpe; `W` es índice de peso, con 100 como referencia. El vector inicial es `K × dirección(ángulo)`. El peso no altera el tamaño del personaje ni se extrae de la masa física del modelo.

Ejemplo verificable: un golpe con B=5, G=12 y d=8 sobre un luchador W=100 a 50% produce K=11,96. A 150% produce K=23,96. Un personaje W=120 recibe K≈9,97 en el primer caso. Los números sirven para comprobar coherencia; se calibran con las dimensiones reales del escenario.

La dirección separa azimut horizontal y elevación. Para una orientación horizontal normalizada `h` y elevación `θ`, el vector de lanzamiento es `K × (cos(θ) × h + sin(θ) × arriba)`. Un ataque puede empujar alejando del atacante o seguir un rumbo propio, pero lo especifica en datos. La simulación controla arrastre y gravedad; un personaje a distinta altura no se considera alcanzado solo por compartir posición proyectada en pantalla.

### 6.5 Hitstop, hitstun e influencia direccional

El hitstop congela brevemente la representación y el avance de las acciones afectadas para comunicar contacto. La partida mantiene su reloj de tick y registra correctamente entradas. Hipótesis inicial: entre 3 y 7 ticks según fuerza, con límite para multigolpes. El sonido y efecto no se repiten por cada tick congelado.

El hitstun impide acciones durante una duración calculada y acotada: propuesta inicial `H = clamp(redondear(K × 1,6), 8, 40)` ticks. Debe ajustarse con pruebas para que existan secuencias interesantes sin combos inevitables excesivos. Los lanzamientos bajos no pueden encerrar al oponente infinitamente junto a una pared.

La influencia direccional es una candidata posterior, no requisito para reinterpretar la beta. Si se añade, rota modestamente el azimut o modifica deriva sin revertir el impulso; elevación y límites se especifican por separado. Primero se valida el lanzamiento 3D sin esta capa. El entrenamiento debe mostrar trayectoria y estado para poder decidir si aporta profundidad comprensible.

### 6.6 Prioridad, intercambios y proyectiles

Primero se reúnen contactos válidos del tick; después se resuelven como un conjunto. Dos golpes simultáneos pueden intercambiarse. La prioridad de un ataque no se decide por lo espectacular de su efecto. El MVP evita un sistema profundo de choque entre hitboxes: si ambos alcanzan una hurtbox vulnerable, ambos impactan salvo regla explícita.

Los proyectiles tienen propietario, vida máxima, comportamiento al tocar plataformas y límite por personaje. Ninguno existe indefinidamente fuera de cámara. Una zona persistente tiene duración y presupuesto activo; se destruye al terminar ronda o cambiar escenario.

### 6.7 Esquiva y defensa

La esquiva es la defensa activa inicial. Usa dirección sobre X/Z, una ventana breve de invulnerabilidad y recuperación castigable. Punto de partida del GDD anterior: 0,15 s de ventana y 1 s de recarga; debe contrastarse con los datos reales de la beta antes de aplicarlo. En el aire hay un uso por fase aérea, restaurado al aterrizar.

No se cancelan libremente ataques con esquiva y no hay invulnerabilidad durante todo el desplazamiento por defecto. Esperar, rodear, saltar, mantener distancia y castigar un ataque fallado también son defensas. Guardia, agarres y parries universales permanecen como candidatos posteriores; requerirían una prueba que demuestre que resuelven un problema real.

### 6.8 Escándalo y superataques

Cada luchador tiene barra individual de 0 a 100. Conectar una activación de cadena básica genera 8 puntos; conectar un especial genera 12, una sola vez por activación aunque golpee varias veces o a varios rivales. El fuerte no añade carga extra hasta que se defina y pruebe una regla explícita; no se le asigna carga por accidente. Ataques fallados, recibir daño y supers no cargan barra.

Con 100 puntos se habilita el super. Activarlo consume todo aun si falla o se interrumpe después de confirmar la activación. La preparación muestra área horizontal, tolerancia vertical y tiempo suficiente para reaccionar. Perder stock reinicia barra; no se obtiene carga atacando durante protección de reaparición. La barra completa tiene señal visual y sonora que puede entenderse sin parpadeos.

Las supers no deben expulsar de forma garantizada desde cualquier lugar. Contra varios objetivos, la carga por activación continúa siendo única. Una prueba específica comprueba que los impactos del super no generan otro super ni duplican recompensas.

### 6.9 Reglas de balance

- Un movimiento poderoso debe pagar con anticipación, recuperación, recurso, rango comprometido o riesgo de posición.
- Un proyectil no gana automáticamente a un acercamiento que lo leyó correctamente.
- Recuperación excelente, gran peso y gran poder de KO no coinciden sin una debilidad fuerte y medible.
- La dificultad técnica no justifica que un personaje carezca de contrajuego.
- Las modificaciones de daño se evalúan junto a trayectorias, stocks y duración de partida.
- Se registra cada cambio con hipótesis, versión, prueba y resultado; no se balancea solo por una derrota memorable.

## 7. Controles y dispositivos

### 7.1 Mando recomendado

Se conserva la distribución documentada en la beta, con objetivo de reasignación antes de la versión pública:

| Acción | Entrada predeterminada |
|---|---|
| Movimiento X/Z | Stick izquierdo |
| Orientar ataques | Stick derecho |
| Salto y segundo salto | A |
| Ataque básico y cadena corta | X |
| Ataque fuerte | Y |
| Especial principal | B |
| Recuperación aérea | RB en el aire, con dirección |
| Esquiva | RT con dirección |
| Super con escándalo completo | LB |
| Caída rápida | Mantener LT en el aire |
| Pausa / confirmar menú de partida | Menu / Start |
| Cancelar menú | B |

El stick derecho define azimut sobre el suelo, no una dirección de salto. Su deadzone evita giros involuntarios; al soltarlo se conserva orientación. Una ayuda suave puede corregir errores pequeños hacia un rival dentro de un cono corto y visible, como documenta la beta. No atraviesa obstáculos, no cambia de objetivo a mitad del golpe ni garantiza impactos. La intensidad debe ser configurable y probarse con mandos reales.

### 7.2 Teclado

Perfil documentado en beta: WASD movimiento; ratón orientación; Espacio salto; clic izquierdo básico; clic derecho fuerte; E especial; R recuperación aérea; Mayúsculas+dirección esquiva; Q super; Control caída rápida; Esc pausa; Enter iniciar o revancha. La navegación completa por teclado es objetivo de accesibilidad y debe verificarse, sin asumirla por la existencia de estos controles.

El apuntado del ratón proyecta una dirección sobre el plano horizontal a la altura de referencia del luchador, con respaldo cuando el rayo no encuentra plano válido. El cursor no puede saltar de orientación por tocar el cielo, una pared o una UI. Una sombra/retícula discreta muestra la dirección; la selección de altura de ataques pertenece al movimiento, no a un lock-on vertical oculto.

La beta documenta P1 con teclado/ratón y hasta cuatro mandos para P2–P5, con bots en puestos libres. En solitario el primer mando puede compartir P1 con teclado. Se preserva ese comportamiento hasta definir una mejora; la validación prioriza primero teclado/ratón+mando y dos mandos. Dos personas compartiendo teclado no resuelve el apuntado independiente y queda fuera de la base.

### 7.3 Unión y pérdida de dispositivo

Objetivo de mejora: cada participante reclama un dispositivo libre con confirmar. Un mando no controla dos puestos a la vez; compartir P1 en modo solo es una regla explícita. Desconexión en partida local pausa y permite reconectar o reasignar sin perder stocks. Conectar un mando adicional no roba el control del teclado. Estas mejoras se prueban antes de describirlas como ya implementadas.

Se guardan perfiles por esquema, deadzones, vibración y preferencias de salto. La reasignación detecta conflictos y permite restaurar valores. Las acciones esenciales deben poder completarse sin pulsar dos botones pequeños simultáneamente.

## 8. Roster: trece arquetipos

### 8.1 Matriz de producción y nombres

Don Promesio y Lola Viral son los nombres de diseño vigentes. La beta los identifica como Don Decreto y Lola Loop: conservar esos IDs internos permite reutilizar trabajo sin una migración precipitada. Coach Tóxico, Cripto King y Tío Antena ya figuran en la beta; los otros ocho personajes son visión de expansión, no contenido implementado.

| ID de diseño | Nombre público de trabajo | Arquetipo | Rol | Estado documental |
|---|---|---|---|---|
| CH01 | Don Promesio | Político corrupto | Control frontal y resistencia | Beta: Don Decreto; foco validación |
| CH02 | Lola Viral | Influencer rubia | Movilidad y hostigamiento | Beta: Lola Loop; foco validación |
| CH03 | Cripto King | Gurú cripto | Proyectiles y zonas | Kit documentado en beta |
| CH04 | Vera Cláusula | Directora de RRHH | Castigo y control de espacio | Diseño futuro |
| CH05 | Gloria Bis | Diva exestrella | Alcance y ritmo | Diseño futuro |
| CH06 | El Moderador | Censor supremo | Negación temporal de zonas | Diseño futuro |
| CH07 | Tío Antena | Conspiranoico | Trampas y lecturas | Kit documentado en beta |
| CH08 | Coach Tóxico | Coach motivacional | Presión cercana y compromiso | Kit documentado en beta |
| CH09 | Gala Causa | Activista performativo | Movilidad y señuelo | Diseño futuro |
| CH10 | Don Monopolio | Magnate anciano | Peso y control lento | Diseño futuro |
| CH11 | Rolo Directo | Presentador TV | Zoning lineal | Diseño futuro |
| CH12 | Don Íñigo del Ayer | Aristócrata español del siglo XVII | Espaciado con arma | Diseño futuro |
| CH13 | Null | Hacker joven | Técnica y reposicionamiento | Diseño futuro |

Cada ficha define fuerte, especial, recuperación y super; el básico pertenece al esquema común y adopta poses propias. No son cuatro especiales direccionales laterales. Las propuestas para kits existentes conservan sus funciones; cambiar valores o nombres visibles requerirá una tarjeta y prueba, no una afirmación de que la beta ya fue modificada.

### 8.2 CH01 — Don Promesio, político corrupto

**Fantasía:** todo problema se resuelve con otra promesa, una firma y alguien más pagando la cuenta. Traje azul, corbata llamativa y silueta rectangular. **Rol:** control frontal, resistencia al empuje algo mayor y movilidad aérea limitada. **Básico:** Manotazo electoral, cadena corta de contacto.

- **Fuerte: Golpe de mayoría.** Golpe frontal con anticipación visible y dirección fijada. Alcance superior al básico, recuperación castigable y tolerancia vertical limitada.
- **Especial: Promesa vacía.** Placa/cartel que viaja en línea recta a altura fija, golpea y desaparece. Reutiliza el concepto de Decreto exprés de la beta; no atraviesa paredes ni tapa la pantalla real.
- **Recuperación: Subida en las encuestas.** Impulso alto con poco control horizontal, una vez por fase aérea. Su trayectoria es predecible y no genera daño obligatorio en la primera validación.
- **Super: Cadena nacional.** Abanico frontal anunciado sobre el suelo. Se toma como referencia la Mayoría absoluta de la beta: 150 grados, alcance y altura editables. No afecta a quien esté fuera del volumen vertical por compartir la proyección en pantalla.

**Fortalezas:** castigar aproximaciones frontales, sostener centro y cerrar stocks tras un error grande. **Debilidades:** giro comprometido, persecución aérea discreta y recuperación lateral reducida. **Contrajuego:** rodear durante anticipación, forzar disparo de cartel y entrar por un ángulo nuevo, llevarlo hacia el borde. **Aceptación:** el cartel, la dirección del fuerte y el abanico del super se leen desde la cámara elevada sin texto explicativo. Frase de trabajo: «Mi compromiso es seguir comprometiéndome».

### 8.3 CH02 — Lola Viral, influencer rubia

**Fantasía:** transforma toda caída en contenido y toda crisis en una colaboración. Cabello rubio de masa gráfica simple, teléfono, ropa fucsia y silueta ligera. **Rol:** movilidad y presión a media distancia, menor resistencia al empuje. **Básico:** Selfie de impacto, cadena corta con poses legibles.

- **Fuerte: Encuadre perfecto.** Golpe de teléfono con alcance medio, anticipación más corta que Don Promesio y lanzamiento menor. No gira libremente después de activarse.
- **Especial: Ola de hate.** Tres etiquetas sucesivas con dirección y altura fijadas al inicio; desaparecen al impactar u obstáculo. El daño por etiqueta se calibra: el GDD antiguo proponía 3% cada una, mientras la beta documenta 8%; no se mezclan ambas cifras como balance validado.
- **Recuperación: Impulso de tendencia.** Ascenso con reacciones y buen ajuste horizontal, menor altura que Don Promesio. Mapea Tendencia ascendente de la beta; conserva un uso por fase aérea.
- **Super: Funada masiva.** Marca una zona circular delante de ella y estalla tras advertencia, nunca de manera instantánea alrededor del cuerpo. La beta documenta 0,7 s de preparación para Viralización masiva, referencia a probar antes de cambiar.

**Fortalezas:** reposicionamiento, presionar rutas y volver desde ángulos laterales. **Debilidades:** peso bajo, cierres de stock comprometidos y exposición tras ráfaga. **Contrajuego:** cambiar altura o rodear etiquetas, mantener centro y salir de la zona del super antes del estallido. **Aceptación:** los destellos tienen versión reducida sin perder aviso; la etiqueta ofensiva se distingue de decoración. Frase de trabajo: «No tengo contexto, pero tengo una opinión».

### 8.4 CH03 — Cripto King, gurú cripto

**Fantasía:** vende certeza mientras intenta escapar de sus propias gráficas. Traje brillante, gafas opacas, monedas ficticias. **Rol:** proyectiles, rebotes y preparación de zonas. **Básico:** gestos cortos con un dispositivo de cotizaciones, sin marcas.

- **Fuerte: Cierre de mercado.** Golpe frontal de buen rango, con flecha de dirección y recuperación suficiente para castigar un fallo.
- **Especial: Token rebote.** Proyectil con aviso de trayectoria y hasta tres rebotes, desaparece al primer impacto válido con luchador. No debe rebotar infinitamente entre superficies ni saltarse colisiones rápidas.
- **Recuperación: A la luna.** Curva amplia orientada al iniciarla. Buena distancia, trayectoria comprometida; la cámara y sombra comunican la ruta de aterrizaje.
- **Super: Burbuja especulativa.** Esfera anunciada que crece durante una ventana visible y explota. La beta cita 1,2 s; el volumen es realmente 3D, no un disco infinito en altura.

**Fortalezas:** condicionar rutas y castigar rivales que se agrupan. **Debilidades:** necesita espacio, sufre presión cercana y su recuperación puede leerse. **Contrajuego:** aproximarse en diagonales después del rebote, salir de la esfera y castigar anticipación. **Restricción:** no se integra moneda, NFT o inversión real. **Aceptación:** el jugador identifica trayectoria actual, rebotes restantes y área de explosión sin depender solo del color. Se conserva el kit existente antes de añadir una segunda economía de recursos.

### 8.5 CH04 — Vera Cláusula, directora de RRHH

**Fantasía:** convierte toda conversación en un procedimiento y todo procedimiento en una amenaza cortés. Traje geométrico, carpeta grande y silueta vertical. **Rol:** castigo, zonas pequeñas y control de rutas; diseño futuro.

- **Fuerte: Reestructuración.** Barrido de carpeta en sector frontal. Amplio en horizontal, altura limitada y anticipación que permite rodearlo.
- **Especial: Periodo de prueba.** Coloca un rectángulo pequeño que, tras aviso, emite una única ráfaga de papeles si entra un rival. Máximo uno y destruible; no bloquea controles ni simula un contrato real.
- **Recuperación: Promoción horizontal.** Impulso diagonal con una caja de oficina. Dos inclinaciones iniciales posibles, deriva posterior reducida; menos altura a cambio de distancia.
- **Super: Salida por acuerdo.** Marca dos sectores delante y detrás con corredor lateral seguro; después produce dos ráfagas de lanzamiento. La zona completa y su altura aparecen antes del impacto.

**Fortalezas:** ocupar pasos estrechos y castigar entradas predecibles. **Debilidades:** reacción aérea discreta y pérdida de presión cuando la obligan a abandonar su marca. **Contrajuego:** rodear la trampa, destruirla desde distancia o saltar fuera de su tolerancia vertical; atacar durante colocación. **Aceptación:** el rectángulo armado se distingue de una zona que ya está atacando; ninguna hoja decorativa tiene colisión inesperada. El primer prototipo reutiliza un solo tipo de zona y no necesita un editor de reglas complejo.

### 8.6 CH05 — Gloria Bis, diva exestrella

**Fantasía:** el escenario entero sigue debiéndole un aplauso. Silueta de abanico, hombreras, capa simple y micrófono ficticio. **Rol:** alcance y control de ritmo, ataques anunciados; diseño futuro.

- **Fuerte: Gira de despedida.** Giro con capa que cubre un sector amplio alrededor. Tiene hueco durante arranque y gran recuperación; no ofrece invulnerabilidad completa.
- **Especial: Nota sostenida.** Cono corto de voz dirigido en X/Z. Mantenerlo brevemente compromete movimiento; no ciega ni exige volumen alto para comprender el ataque.
- **Recuperación: Último bis.** Ascenso sobre un pedestal de luz ficticio, con deriva pequeña y final expuesto. El pedestal es presentación, no plataforma persistente.
- **Super: Ovación obligatoria.** Anillo de sonido que se expande a una altura delimitada y anuncia su frente de impacto. Puede evitarse tomando altura o abandonando el radio antes del avance.

**Fortalezas:** disuadir agrupaciones y castigar acercamientos frontales. **Debilidades:** anticipación larga, cuerpo visible y vulnerabilidad desde ángulos no cubiertos. **Contrajuego:** entrar por el hueco del cono, esperar el giro y castigar; usar altura ante el anillo. **Aceptación:** toda amenaza sonora tiene señal equivalente; la capa no amplía la hurtbox por accidente ni oculta el borde. Su voz se limita en mezcla para evitar fatiga.

### 8.7 CH06 — El Moderador, censor supremo

**Fantasía:** considera que toda discrepancia es un error de formato. Máscara rectangular, sellos y barras ficticias. **Rol:** negación temporal de rutas; diseño futuro.

- **Fuerte: Fuera de contexto.** Sello frontal de alcance medio y compromiso alto. La dirección se fija tras anticipación y deja opciones de rodeo.
- **Especial: Área restringida.** Zona pequeña que pulsa una vez tras aviso; máximo una, duración corta y salida clara. No desactiva botones, pantalla, sonido ni ajustes de accesibilidad.
- **Recuperación: Revisión superior.** Elevación sobre un marco gráfico, altura media e inicio débil. La silueta permanece visible desde arriba.
- **Super: Archivo definitivo.** Dos paneles de energía anuncian un corredor y lo barren una vez. Hay zonas seguras reconocibles y tolerancia vertical limitada; los paneles no son paredes físicas permanentes.

**Fortalezas:** obligar a elegir ruta y castigar indecisión en un área concreta. **Debilidades:** poca velocidad, preparación visible y dificultad para seguir cambios rápidos de altura. **Contrajuego:** abandonar la marca, entrar desde un ángulo lateral y castigar al finalizar el barrido. **Aceptación:** la fantasía de censura no elimina información del jugador; barras y paneles se dibujan sin tapar el HUD ni ocultar otras amenazas. Se prototipa con geometría simple antes de producir efectos complejos.

### 8.8 CH07 — Tío Antena, conspiranoico

**Fantasía:** todos sus planes están conectados, aunque él no recuerde cómo. Abrigo grande y antena de papel metálico; el humor apunta a razonamientos, no a condiciones de salud. **Rol:** trampas y desvíos; kit documentado en beta.

- **Fuerte: Prueba concluyente.** Barrido frontal amplio con dispositivo improvisado; alcance algo mayor a cambio de preparación legible.
- **Especial: Interferencia.** Emisor visible, uno activo, armado con demora y duración limitada. La beta cita 0,35 s de armado y 7 s de vida; lanza hacia arriba al primer rival válido y se consume.
- **Recuperación: Me están llevando.** Haz de abducción lento, gravedad reducida y altura limitada. No ofrece suspensión indefinida ni libertad total de dirección.
- **Super: La señal definitiva.** Barrido circular con dos sectores seguros señalados en blanco a los lados, como documenta la beta. El blanco se complementa con forma/patrón; altura y radio están delimitados.

**Fortalezas:** condicionar suelo y hacer dudar en cruces. **Debilidades:** colocación lenta, presión directa discreta y recuperación interceptable. **Contrajuego:** provocar o destruir emisor según regla de durabilidad, entrar tras su consumo y buscar el sector seguro. **Aceptación:** la antena y sus cables no parecen plataformas o colisión sólida; el emisor se distingue de props y comunica armado, activo y consumido. Se conserva el límite de una trampa en enfrentamientos de cinco participantes.

### 8.9 CH08 — Coach Tóxico, coach motivacional

**Fantasía:** convierte cada derrota en un seminario sobre por qué la culpa era del público. Hombros anchos, zapatillas grandes y pizarra portátil. **Rol:** presión cercana, resistencia alta y compromiso; kit documentado en beta.

- **Fuerte: Objetivo innegociable.** Golpe potente de corto-medio alcance, preparación mayor y recuperación grande si falla. No sigue al objetivo después de fijar dirección.
- **Especial: Serie al fallo.** Avance corto con golpe final; no atraviesa paredes. El controlador barre el recorrido y se detiene ante obstáculos válidos.
- **Recuperación: Día de pierna.** Salto explosivo con muy poco control lateral. Buena altura, ruta clara y recurso único por fase aérea.
- **Super: Preentreno nuclear.** Onda por el suelo que solo alcanza rivales apoyados y a una altura parecida. La beta cita tolerancia de 0,8 m; ese criterio se comprueba en rampas y plataformas para evitar impactos invisibles.

**Fortalezas:** perseguir retiradas cortas, castigar errores cercanos y resistir intercambios. **Debilidades:** alcance limitado, anticipación larga y pocas opciones al quedar lejos de una plataforma. **Contrajuego:** variar ritmo, rodear embestida, saltar sobre la onda y castigar su fin. **Aceptación:** su energía se expresa con poses; no exige machacar botones ni movimientos físicos agotadores. Su mayor resistencia no puede venir acompañada de la mejor recuperación y el mejor alcance a la vez.

### 8.10 CH09 — Gala Causa, activista performativo

**Fantasía:** siempre llega a tiempo para la foto y tarde para desmontar el escenario. Megáfono, pancarta reversible y símbolos ficticios. **Rol:** movilidad y señuelos; diseño futuro.

- **Fuerte: Comunicado definitivo.** Golpe de pancarta con alcance medio, sector y recuperación claros.
- **Especial: Foto de archivo.** Coloca un recorte destructible que dura poco. Puede interceptar un proyectil débil mediante una regla explícita, pero no imita el indicador de jugador ni copia ataques.
- **Recuperación: Elevación del discurso.** Ascenso con buena deriva y altura moderada, sin golpe fuerte protector.
- **Super: Presencia total.** Marca tres puntos de flashes que se activan en secuencia anunciada. Hay rutas entre ellos; número, orden y tiempo permanecen visibles sin depender de destellos intensos.

**Fortalezas:** provocar fallos y cambiar de ángulo mientras prepara espacio. **Debilidades:** peso bajo, poco cierre directo y dependencia del señuelo. **Contrajuego:** observar el identificador real, destruir el recorte y mantener rutas seguras durante el super. **Aceptación editorial:** el objetivo es usar causas como marca personal, no ridiculizar derechos o comunidades. **Aceptación técnica:** máximo un recorte, sin IA ni mallas generadas dinámicamente. Si los tres puntos del super añaden demasiada carga visual, se reduce a una sola secuencia de dos zonas.

### 8.11 CH10 — Don Monopolio, magnate anciano

**Fantasía:** considera que la arena es suya porque una cláusula olvidada lo dice. Abrigo, bastón y asiento móvil simplificado. **Rol:** súper pesado lento con amenaza cercana; diseño futuro.

- **Fuerte: Adquisición hostil.** Empuje de bastón con avance corto y gran recuperación. Poco alcance vertical y ruta horizontal fija.
- **Especial: Oferta irrechazable.** Moneda ficticia pesada en arco corto. Una activa; buena para castigar una ruta, mala para llenar toda la arena.
- **Recuperación: Rescate privado.** Paraguas corporativo lo eleva lentamente, deriva modesta y vulnerabilidad marcada. Nunca permite vuelo ilimitado.
- **Super: Compra del entorno.** Marca un sector ancho frontal y lo golpea con una caída caricaturesca de contratos. Aviso largo y centro seguro detrás; no compra ni transforma realmente la geometría.

**Fortalezas:** resistencia y castigo de mala distancia. **Debilidades:** tamaño, persecución vertical, giro lento y recuperación expuesta. **Contrajuego:** alternar altura, rodear al iniciar el fuerte y llevarlo hacia el vacío. **Aceptación:** el asiento forma parte de una sola entidad; no hay dos cuerpos con reglas divergentes. La edad no es el chiste ni una burla corporal. Su resistencia se ajusta junto a la duración de partida y a la dificultad real de sacarlo de la arena.

### 8.12 CH11 — Rolo Directo, presentador TV

**Fantasía:** no conoce el resultado, pero ya sabe quién debe parecer culpable. Tarjetas, micrófono y sonrisa gráfica. **Rol:** zoning lineal y control de ritmo; diseño futuro.

- **Fuerte: Corte publicitario.** Micrófono con avance y remate frontal. No corta la imagen real; deja recuperación suficiente tras fallar.
- **Especial: Última hora.** Tarjeta recta de velocidad media y daño bajo, máximo dos y vida breve. Choca con obstáculos; su color no imita un aviso del sistema.
- **Recuperación: Conexión en directo.** Cable ficticio impulsa hacia una dirección inicial. Alcance fijo y colisión comprobada, sin enganchar dinámicamente cualquier malla.
- **Super: Debate amañado.** Zona rectangular anunciada con dos oleadas separadas por una ventana visible. Ambas comparten la misma activación y no cargan escándalo.

**Fortalezas:** controlar pasarelas y castigar aproximaciones rectas. **Debilidades:** lateralidad marcada, vulnerabilidad al rodeo y recuperación predecible. **Contrajuego:** cambiar ángulo, tomar altura y avanzar después de una tarjeta fallada. **Aceptación:** rótulos satíricos no tapan porcentajes, rutas o siluetas; la segunda oleada está anunciada antes de que el jugador la descubra por perder un stock. El kit inicial evita invitados invocados e IA adicional.

### 8.13 CH12 — Don Íñigo del Ayer, aristócrata español del siglo XVII

**Fantasía:** el duelo moderno le parece vulgar y la gravedad una ofensa personal. Capa triangular, gola, sombrero y espada estilizada. **Rol:** espaciado, precisión frontal y alcance; diseño futuro.

- **Fuerte: Exijo satisfacción.** Estocada de alcance largo, punta con mayor lanzamiento y escasa cobertura lateral. Preparación visible, dirección fija y peor resultado demasiado cerca.
- **Especial: Paso de salón.** Dos pasos elegantes y corte. Es desplazamiento visible, sin teleport ni invulnerabilidad universal, y se detiene ante paredes.
- **Recuperación: Linaje ascendente.** Salto con corte vertical, poca distancia horizontal y final comprometido.
- **Super: Cuestión de linaje.** Estocada larga en corredor estrecho marcado en suelo, con altura limitada. La amenaza es enorme de frente, pero permite rodear antes de liberar.

**Fortalezas:** controlar un ángulo, castigar ataques fuera de rango y forzar respeto en pasarelas. **Debilidades:** rivales que rodean, alcance mínimo y recuperación lateral. **Contrajuego:** entrar después de la punta, variar altura y atacar por flanco. **Aceptación:** el arma se ve desde cámara elevada; una espada muy fina no basta como aviso. Sus frases admiten subtítulo claro y el humor no depende de conocimiento histórico preciso. El modelo mantiene vestimenta deformable sin simulación de tela obligatoria.

### 8.14 CH13 — Null, hacker joven

**Fantasía:** cree poder salir de cualquier problema cambiando el sistema hasta que alguien golpea el teclado. Capucha abierta, mochila y teclado plegable. **Rol:** reposicionamiento técnico, ligera; diseño futuro.

- **Fuerte: Error fatal.** Golpe de dispositivo con anticipación visible y alcance modesto; cierre de stock que exige acertar una lectura.
- **Especial: Punto de restauración.** Coloca marcador; otra activación temprana vuelve si el espacio está libre. Mantiene daño y recursos gastados; no cancela hitstun, KO o recuperación comprometida. Se consume y desaparece al perder stock.
- **Recuperación: Escalada de privilegios.** Impulso diagonal orientado al inicio; buena distancia, poca protección ofensiva y aterrizaje comprometido.
- **Super: Reinicio del relato.** Marca una esfera pequeña en una posición visible y la hace estallar tras advertencia. No revierte tiempo, guardados, stocks ni entradas del rival.

**Fortalezas:** rutas preparadas y engaño posicional. **Debilidades:** peso bajo, poco poder directo y necesidad de colocar marcador con seguridad. **Contrajuego:** vigilar marcador, ocupar la zona de retorno y castigar anticipación. **Gate técnico:** retorno y colisiones deben resolver ocupación, rampas, bordes y recursos sin duplicar entidad; si falla, se cambia por dash simple. Su fantasía informática se representa mediante efectos del juego, sin integrar acceso real a equipos o cuentas.

## 9. Modos y progresión

### 9.1 MVP

Versus local 1v1, selección de dos personajes, un escenario sin peligros, regla de stocks, pausa, resultado y revancha. Entrenamiento mínimo con reinicio de posiciones, porcentaje editable, rival inmóvil o controlado por segundo dispositivo y visualización de hitboxes/hurtboxes. No necesita un bot inteligente para validar el combate entre personas.

Un tutorial mínimo usa tres ejercicios: moverse/saltar, golpear y expulsar, volver al escenario. Cada ejercicio admite reinicio inmediato y texto breve. Puede implementarse como tarjetas y una sala de práctica antes de producir una campaña didáctica.

### 9.2 Versión local posterior

Hasta cuatro participantes, equipos opcionales, reglas configurables, selección de escenarios, entrenamiento con grabación/reproducción de entradas y retos de personaje. Un bot básico puede incorporarse cuando exista una necesidad clara de práctica en solitario; no requiere navegación de mundo abierto para un plano de combate pequeño.

Las reglas casuales con peligros de escenario se separan de variantes de escenario estables. Las variantes comparten arte, pero su geometría y reglas se identifican claramente. La configuración guardada de una sesión no cambia en silencio al iniciar otra.

### 9.3 Progresión ética

Todos los movimientos y estadísticas están disponibles en igualdad de condiciones. Propuesta base: roster jugable adquirido junto con la versión del juego correspondiente; colores, poses, iconos y galería se desbloquean jugando. La venta eventual de cosméticos requiere precios claros, vista previa y compra directa.

No hay cajas de botín pagadas, apuestas, energía, ventajas de pago, tareas diarias obligatorias o moneda diseñada para ocultar el precio. No se prometen pases de batalla. Los cosméticos conservan silueta, identificación y señales de ataque; un aspecto no amplía ni oculta hitboxes.

Los retos enseñan acciones útiles: recuperar diez veces, bloquear y castigar, jugar varios personajes. Evitan premiar conductas molestas como prolongar partidas innecesariamente. La progresión local se guarda de forma recuperable y puede restablecerse desde una opción explícita.

### 9.4 Estrategia online posterior

El online comienza como investigación, no como casilla de marketing. Gate de entrada: combate local estable, estado de simulación bien delimitado, pruebas repetibles, presupuesto y capacidad de mantenimiento. Primera meta: 1v1 con invitación y rollback de prueba. Matchmaking, clasificación, espectadores y crossplay requieren hitos separados.

La simulación debe poder guardar/restaurar estado y reproducir una secuencia de entradas. Física de motor, Animator, partículas y audio no se asumen deterministas. Si se utilizan consultas de física, debe comprobarse qué información puede reproducirse y qué divergencias obligan a sustituirla por lógica propia. No se promete determinismo entre equipos solo por usar paso fijo.

Evaluaciones propuestas: latencia simulada de 80/150/250 ms, jitter, pérdida, desconexión, entradas tardías y diferencias de frecuencia de renderizado. La cifra de rollback aceptable se decide con pruebas de tacto y arte, no copiando un número de otro proyecto. Si el prototipo falla, se mantiene el valor del juego local y se reduce o pospone online.

La elección de librería, servicios, relays, plataforma de distribución y costes se realiza en esa puerta con fuentes vigentes. No se instala una pila de red por adelantado ni se mezclan varias soluciones para el mismo problema.

## 10. Accesibilidad y experiencia de uso

### 10.1 Accesibilidad esencial

- Reasignación de entradas y navegación completa por teclado/mando.
- Deadzones ajustables; alternativa de cruceta; vibración regulable y desactivable.
- Subtítulos de voces relevantes, tamaño de texto ajustable y alto contraste para HUD.
- Identificación por forma, número y color; las parejas de colores se prueban con simulaciones de deficiencia cromática.
- Reducción de destellos, sacudida, zoom brusco, posprocesado y movimiento de fondo.
- Volúmenes independientes de música, efectos, voces y UI; señales visuales para eventos sonoros críticos.
- Sin acciones obligatorias de pulsación rápida repetida; mantener puede sustituir acciones repetitivas fuera de combate.
- Tutorial sin límite de tiempo, pausa local y posibilidad de repetir instrucciones.
- Fuentes legibles, texto no integrado en texturas y traducción prevista desde el principio.

La opción de destellos reducidos modifica la presentación de ataques como Flash de relevancia; no elimina sus señales mecánicas ni cambia su tiempo activo. El modo de baja intensidad visual conserva contornos, hit sparks mínimos e indicadores de borde.

### 10.2 Flujo de pantallas

Inicio → aviso de pulsar botón → menú principal → Versus / Entrenamiento / Opciones / Créditos → unión de dispositivos → personaje → escenario y reglas → carga breve → combate → resultado → revancha o selección.

En el MVP, el escenario único puede mostrarse ya seleccionado, conservando el punto de extensión sin obligar a una pantalla vacía. La pausa local ofrece continuar, controles, reiniciar y salir, con confirmación para abandonar una partida activa.

### 10.3 HUD de combate

Por participante: retrato o icono, nombre corto, número, porcentaje grande, stocks y estado de desconexión si procede. Temporizador arriba al centro. La información es legible a resolución 720p como prueba de estrés, aunque el objetivo visual sea 1080p. Fuera de cámara aparece indicador direccional con número y forma.

No se muestran medidores decorativos que parezcan recursos de combate. La interfaz de entrenamiento puede revelar recursos de recuperación, estado, tick de acción, daño del último golpe y knockback, siempre como herramientas opcionales.

### 10.4 Guardado y errores

Se guardan controles, audio, vídeo, accesibilidad y progreso cosmético. El formato incluye versión, validación y copia recuperable; no sobrescribe un archivo válido con contenido incompleto. Un archivo de preferencias dañado restaura valores seguros y avisa con lenguaje claro. Las partidas del MVP no requieren cuenta ni conexión.

## 11. Dirección de arte

### 11.1 Lenguaje visual

Proporciones de aproximadamente cinco a seis cabezas, manos y accesorios expresivos, superficies amplias, detalle concentrado en cara y elemento de identidad. Una silueta debe reconocerse a tamaño pequeño y en poses frontal, posterior, lateral y de tres cuartos. Las diferencias de tamaño artístico no deben crear ventajas de lectura injustas.

Los materiales usan pocos niveles de luz, transiciones de sombra controladas, color local claro y especular moderado. Contorno opcional, ajustable para evitar parpadeo. La dirección de luz ayuda a describir volumen sin convertir cada borde en ruido. El look se evalúa en movimiento, con dos personajes solapados y fondos reales.

### 11.2 Jerarquía visual

Primer nivel: personajes, ataques y peligros. Segundo: superficie jugable y bordes. Tercero: identidad escénica. Cuarto: público, pantallas y gags ambientales. Los fondos tienen menor contraste y saturación que los luchadores cuando estos se aproximan a un solapamiento crítico.

No se texturiza cada botón del traje. Texturas de letras, interfaces y multitudes se simplifican para evitar moiré y ruido. El texto diegético es breve y reemplazable para localización. La UI se genera con texto real del motor.

### 11.3 Animación

Anticipación clara, poses extremas, contacto definido y recuperación que comunique vulnerabilidad. Los personajes comparten un esqueleto base cuando sus proporciones lo permitan, con accesorios separados. Se evita añadir huesos que solo mejoran un plano cinematográfico que el juego nunca mostrará.

El locomotor tiene arranque, frenada, giro, ascenso, caída y aterrizaje. El prototipo puede usar animaciones sencillas con poses clave antes de pulir transiciones. Las animaciones no deciden cuándo golpea: esa información reside en los datos de combate sincronizados con el tick.

Las expresiones faciales usan soluciones simples y coherentes. Pelo y ropa pueden animarse mediante huesos o movimientos secundarios controlados; la simulación de tela en tiempo real queda fuera del MVP.

### 11.4 Toon shading y herramientas

La base prevista es URP con un material toon sencillo compatible con la versión real del proyecto. Shader Graph puede cubrir bandas de iluminación y variaciones controladas; un shader externo solo se adopta tras comprobar licencia, mantenimiento, soporte de la versión URP, sombras, instancing y coste con cuatro luchadores.

No se mezclan varios pipelines de renderizado ni varios paquetes de toon a la vez. Se crea una escena de prueba de materiales y se valida con sombras, animación, outlines, dos calidades y el ejecutable real. La elección final y sus versiones se fijan en el registro técnico.

## 12. Escenarios y producción asistida por IA

### 12.1 Regla de construcción

Primero se miden salto, carrera, recuperación y alcance. Después se construye un greybox jugable con primitivas o ProBuilder. Se prueban superficie central, alturas de plataformas, bordes y blast zones. El arte definitivo llega cuando la geometría demuestra que produce decisiones interesantes.

Un escenario no es un fondo bonito con colisión añadida al final. La superficie jugable, las condiciones de recuperación y borde y los límites se definen independientemente del decorado. Ningún balcón decorativo debe parecer una plataforma si no puede pisarse.

### 12.2 ST01 — Azotea Neón como arena piloto

La primera validación reutiliza Azotea Neón de la beta, sin borrar sus otras tres arenas. Dos azoteas, cruce central, terrazas y plataformas bajas permiten probar decisiones en profundidad y altura. Primero se mide la geometría existente; no se reconstruye por coincidir con un dibujo lateral previo.

Cada superficie se registra como volumen 3D, con anchura X, profundidad Z y altura Y. Un CombatLab aislado puede usar una plataforma de 20 × 14 m, dos plataformas de 5 × 4 m a 3 m de altura y posiciones opuestas también en Z. Son valores de experimento, sujetos al controlador real. Los límites de expulsión deben cubrir frente, fondo, izquierda, derecha, techo y caída; como punto de partida del laboratorio, X=±25, Z=±20, Y superior=18 e inferior=-10. No se aplican automáticamente a la arena existente.

Variante estable: superficies fijas, sin peligros aleatorios, decoración que no oculte luchadores y oclusores tratados por la cámara. Una vista elevada mantiene orientación estable para el apuntado. La separación en profundidad debe verse mediante sombras de contacto, bases o indicadores claros.

**Aceptación:** desplazamiento y ataques en ocho direcciones sin ventaja diagonal; saltos y recuperación desde cada borde; límites de KO inequívocos en todos los ejes; cien reinicios sin aparición dentro de geometría; cámara legible con separación en profundidad y altura. Se prueban los cinco participantes que ya admite la beta para conservar esa capacidad, aunque la evaluación inicial de calidad se concentre en 1v1.

### 12.3 Escenarios posteriores

- **ST02 — El Gran Debate o Hemiciclo del espectáculo.** Mesa central y dos pasarelas, pantallas grandes de bajo contraste y focos suaves. Variante casual con barrido de cámara ficticia que se anuncia y no cambia la cámara de juego. Riesgo: ruido de rótulos y parecido con programas reales.
- **ST03 — Azotea de la relevancia.** Terraza al atardecer, aros de luz y carteles ficticios en segundo plano. Geometría con dos alturas y zonas de aterrizaje claras. Riesgo: transparencia y neón que compiten con efectos.
- **ST04 — Cumbre de la liquidez.** Auditorio futurista barato con gráficas absurdas, suelo modular y una pantalla de cohete de cartón. Variante competitiva fija. Riesgo: animaciones de gráficas demasiado intensas y exceso de elementos brillantes.
- **ST05 — Palacio del privilegio perpetuo.** Salón estilizado con tapices, balcones decorativos y un gran reloj detenido. Geometría de pasarela central y plataforma alta única. Riesgo: superficies decorativas que parezcan jugables y materiales demasiado detallados.
- **ST06 — Centro de cumplimiento.** Oficina de RRHH y moderación, carpetas gigantes y ventanillas ficticias. Plataformas sobrias para lectura excelente. Riesgo: estética visual demasiado plana; resolver con profundidad y escala, sin contaminar combate.

La selección de escenarios de producción depende de pruebas y presupuesto. Tres escenarios terminados aportan más valor que seis sin revisión de geometría, audio y rendimiento.

### 12.4 Uso de IA para escenarios

La IA se usa para explorar composición, paleta, props y variaciones, manteniendo una ficha de restricciones. Puede generar imágenes de referencia o mallas de utilería no críticas. No decide por sí sola las dimensiones jugables ni produce automáticamente niveles listos para publicar.

Cada escenario comienza con: nombre, objetivo jugable, planta X/Z y sección vertical Y de colisión, escala de personaje, tres referencias de material, paleta, objetos modulares y presupuesto. Se crea una imagen objetivo y se comprueba a tamaño de pantalla antes de generar familias completas de assets.

**Prompt de concepto reutilizable:** “Arena ficticia de lucha de plataformas 3D con movimiento libre, vista elevada de tres cuartos y planta complementaria, [tema], superficies anchas que permitan rodear al rival, plataformas con profundidad y rutas de retorno, personajes de referencia de 1,8 m como escala, cartoon-anime con sombras de dos bandas, formas amplias y pocos detalles, separación visual de alturas, fondo de contraste moderado, espacio visible alrededor de todos los bordes, sin logos ni personajes reales, sin texto legible, sin objetos delante del área jugable”.

**Prompt de prop para modelado:** “Un único [atril/butaca/carpeta/columna] de mundo satírico ficticio, forma estilizada simple, vista tres cuartos con toda la silueta visible, material mate, pocos elementos finos, base estable, sin texto ni marca, separación clara de piezas principales, consistente con [referencia aprobada]”.

Estos prompts son briefs, no garantías de topología, escala o derechos. Cada resultado pasa limpieza, revisión de licencia y prueba en Unity. La geometría de colisión se mantiene como primitivas propias aunque el prop generado cambie.

## 13. Pipeline Meshy → Blender → Unity

### 13.1 Fases y entregables

1. **Brief y concepto.** Definir personaje/prop, silueta desde varias vistas, paleta, proporciones, rango de movimiento y restricciones. Registrar autoría de referencias y condición de uso.
2. **Exploración en Meshy, si procede.** Generar candidatos con referencias autorizadas. Exportar solo tras comprobar condiciones vigentes del plan, privacidad, atribución y permiso de explotación comercial. Si no encaja con un flujo gratuito, usarlo solo como exploración permitida o modelar directamente en Blender.
3. **Selección.** Elegir por silueta y función, no solo por render atractivo. Rechazar manos fusionadas, accesorios imposibles de separar y asimetrías no deseadas.
4. **Limpieza en Blender.** Corregir escala, orientación, transformaciones, normales, piezas internas, geometría duplicada y agujeros. Conservar el original sin modificar en un archivo de origen versionado o archivado.
5. **Retopología y UV.** Crear topología deformable en hombros, codos, caderas, rodillas y manos. UV sin solapamientos accidentales; reservar espacio coherente por importancia visual. No confundir decimación con retopología apta para animación.
6. **Rig.** A/T pose coherente, jerarquía documentada, root en origen y escalas limpias. Definir rig Humanoid o Generic según proporciones y necesidad de retargeting; no cambiar a mitad de producción sin medir impacto.
7. **Pesado y animación de prueba.** Probar sentadilla, elevación de brazos, giro, patada y poses extremas. Corregir deformaciones antes de producir todo el moveset.
8. **Materiales y LOD.** Convertir texturas al estilo toon, reducir materiales, comprobar alfa y crear niveles de detalle cuando exista ahorro real. Mantener los colores de identificación fuera de las sombras profundas.
9. **Exportación.** Usar FBX con ajustes documentados para personajes/animación como base; GLB/glTF solo con importador compatible y justificado para props. Validar un asset de prueba antes de adoptar un formato en lote. No depender de abrir `.blend` directamente en el motor como pipeline de publicación.
10. **Importación Unity.** Confirmar escala de 1 unidad cercana a 1 m, avatar, clips, compresión, límites, materiales y textura. Separar prefabs de arte y combate. Configurar shaders de acuerdo con URP real.
11. **Validación en contexto.** Vista de dos y cuatro personajes, cámara final, sombras, efectos, animación y perfil de rendimiento. Un asset no está aprobado solo por verse bien en Blender.
12. **Registro de procedencia.** Guardar herramientas/versiones, fecha, cuenta/plan de generación, fuente de referencias, licencia, cambios humanos y aprobación. Conservar copia de términos aplicables cuando sea razonable.

### 13.2 Convenciones de archivos

Propuesta: `CH01_DonPromesio_Model_v001.blend`, `CH01_Body_LOD0.fbx`, `CH01_AttackNeutral_v001.fbx`, `CH01_Body_BaseColor.png`. En Unity: prefijo de área y tipo, sin nombres ambiguos como `final_final2`.

Carpetas propuestas: `Assets/FUNADOS/Art/Characters`, `Art/Stages`, `Animation`, `Audio`, `Data`, `Prefabs`, `Scenes`, `Scripts`, `UI` y `Tests`. Las fuentes de alta resolución y backups pesados pueden vivir fuera de `Assets`, en estructura controlada. No se mueve contenido existente sin inventario y respaldo.

### 13.3 Ficha mínima de asset

ID; categoría; propietario; estado; origen; referencia; herramienta/versión; licencia/condiciones; número de triángulos; materiales; dimensiones; texturas; rig; LOD; colisión; ruta de fuente; ruta de exportación; prefab consumidor; prueba de rendimiento; aprobación editorial.

Estados: brief → exploración → seleccionado → limpieza → rig/UV → integrado → probado → aprobado. “Generado” es un paso de origen, no un estado equivalente a terminado.

### 13.4 Dependencias mínimas de herramientas

Blender es el DCC base. Unity y VS Code existentes se conservan. URP, Input System, cámara, animación y greybox se resuelven con versiones compatibles con el editor real. Cinemachine se usa si reduce trabajo de encuadre y límites; Animation Rigging solo si un problema de pose o ajuste lo requiere; AI Navigation ya existe en la beta; las rutas terrestres pueden aprovecharlo, pero saltos, recuperación y knockback necesitan lógica propia; un sistema de comportamiento complejo no se instala para un rival inmóvil de entrenamiento.

Para UI y pequeños efectos, se elige una sola herramienta de tween compatible si hace falta; no dos librerías que hagan lo mismo. El control del combate usa lógica explícita, no una secuencia de tweens. Los agentes de Unity se evalúan por soporte del editor, permisos y duplicación de conexiones; su adopción se documenta en el informe técnico.

## 14. Arquitectura de producción y datos

### 14.1 Separación de responsabilidades

**Entradas:** convierten dispositivos en acciones con jugador y tick. **Simulación:** estados, movimiento, recursos, ataques, colisiones e impactos. **Presentación:** modelos, cámara, audio, partículas y UI. **Partida:** reglas, stocks, reloj, pausas, resultados y reinicios. **Datos:** personajes, movimientos y escenarios con validación de rangos.

Los cambios de personaje no deben exigir copiar todo el controlador. Datos compartidos describen reglas comunes; comportamientos especiales se añaden solo donde hay una diferencia real. Se evita diseñar un editor universal antes de producir los dos personajes del MVP.

### 14.2 Datos de movimiento

Cada movimiento registra ID, versión, entrada, estados desde los que se permite, anticipación/activo/recuperación, trayectoria de hitbox, daño, empuje base y crecimiento, azimut y elevación, hitstop, vulnerabilidad, límites de impactos, sonido/VFX, cancelaciones y condiciones de finalización. La ficha incluye un vídeo o captura de la pose y el contrajuego esperado.

La herramienta de depuración puede avanzar un tick, reiniciar, mostrar volúmenes, registrar última transición y reproducir entradas. Esta inversión permite localizar errores de reglas y vale más que agregar un quinto efecto visual prematuramente.

### 14.3 Escenarios como datos

Cada escenario define colisión, plataformas unidireccionales, bordes, spawns, blast zones, límites de cámara, fondo, música y reglas de peligros. La malla visual no se utiliza como fuente accidental de condiciones de victoria. Las variantes estables/casuales tienen IDs distintos y comparten recursos visuales cuando convenga.

### 14.4 Control de versiones y respaldo

Antes de alterar un proyecto existente se conserva una copia recuperable o un commit comprobado. Se inspecciona el estado de Git para no mezclar trabajo ajeno. Se versionan `Assets`, `Packages`, `ProjectSettings` y archivos `.meta`; se excluyen cachés regenerables siguiendo la configuración apropiada. Archivos grandes requieren evaluar Git LFS y cuotas del alojamiento, sin asumir que ya está configurado.

Cada actualización de paquete se hace de forma separada, con lockfile revisado y prueba de apertura/compilación. No se actualiza Unity y todos los paquetes durante un sprint de contenido. Un backup no sustituye al historial de código, y un repositorio sin fuentes de arte no sustituye al backup de esas fuentes.

## 15. Rendimiento y calidad técnica

### 15.1 Objetivo provisional

Prioridad: 60 imágenes/s sostenidas y simulación a 60 Hz en el equipo mínimo que se defina tras el inventario y las primeras mediciones. Resolución de referencia: 1080p; 720p como prueba de legibilidad y escalado. No se declara soporte para un hardware mínimo concreto antes de medir un ejecutable.

El presupuesto de frame a 60 Hz es 16,67 ms. Meta de margen en la escena de cuatro participantes: CPU principal y GPU por debajo de 13,5 ms por separado en condiciones representativas; no se suman como si fueran siempre trabajo secuencial. Se registran mediana, percentil 95 y picos, con configuración, build y escenario.

### 15.2 Presupuestos de arte iniciales

| Recurso | Techo inicial orientativo | Observación |
|---|---:|---|
| Personaje visible LOD0 | 20.000–35.000 triángulos | Ajustar por silueta y coste de skinning real |
| Materiales por personaje | 2–3 | Evitar una ranura por accesorio |
| Huesos deformantes | 60–80 | Medir según plataforma y rig |
| Texturas principales | Hasta 2048 px | Bajar si el tamaño de pantalla lo permite |
| Luces con sombras en combate | 1 principal | Luces adicionales solo con presupuesto medido |
| Proyectiles/zonas por luchador | Según movimiento, límites pequeños | Sin creación ilimitada |
| Contorno | Una solución común | Comparar coste y estabilidad temporal |

Son límites para iniciar pruebas, no objetivos que deban consumirse. Un accesorio de 5.000 triángulos no necesita elevarse a 20.000 para “cumplir calidad”. Se revisa memoria de texturas, draw calls, transparencias y skinning con los cuatro luchadores antes de producir todo el roster.

### 15.3 Estrategia de optimización

Medir en build, identificar cuello de botella, cambiar una causa y repetir la misma escena. Pooling para efectos/proyectiles si el perfil muestra picos de creación; evitar asignaciones frecuentes en el bucle de combate. Sombras, transparencias, contornos y posprocesado tienen niveles de calidad.

Se prueba una partida prolongada y ciclos de revancha para detectar crecimiento de memoria, audio sin liberar y entidades huérfanas. El modo de estrés fuerza ataques, VFX y cámara extrema. La optimización no se basa solo en el contador de FPS del editor.

### 15.4 Matriz de pruebas

- Dos mandos; teclado+mando; pérdida y recuperación de dispositivo.
- 30/60/120 Hz de render con simulación fija para comprobar reglas y presentación.
- Ventana, pantalla completa, distintas relaciones de aspecto y escala de UI.
- Reinicios repetidos, KO simultáneo, pausa durante ataque y salida a menú.
- Ataques en ambos lados, borde, plataformas, reaparición y cambio de stock.
- Opciones de destellos reducidos, audio mínimo y alto contraste.
- Build limpio desde estado versionado y ejecución sin depender de rutas locales de un artista.

## 16. Audio y música

### 16.1 Dirección sonora

Efectos cartoon de contacto con capas diferenciadas para golpe ligero, fuerte, esquiva, lanzamiento, recuperación y KO. Los sonidos comunican fuerza y resultado. Una esquiva y un impacto confirmado tienen señales diferentes. Los proyectiles y trampas avisan de activación sin saturar la mezcla.

La música combina energía de combate con instrumentos o gestos que refuercen el escenario. El hemiciclo puede usar percusión de marcha y metales juguetones, evitando himnos o melodías reconocibles sin licencia. La intensidad puede subir al último stock después de validar que no distrae.

### 16.2 Mezcla

Buses separados: Master, Música, SFX, Voces y UI. Se reservan niveles cómodos y margen para picos; se evita normalizar cada sonido al máximo. Los KO bajan brevemente música y ambiente si mejora la lectura. La vibración acompaña con opción de desactivación.

Límites de voces: una frase principal por personaje a la vez, prioridad al KO y a señales relevantes. Los ataques repetidos alternan variantes sin cambiar tiempos de gameplay. Se puede reducir o silenciar el parloteo manteniendo señales de combate.

### 16.3 Producción y derechos

Cada archivo registra autor, fuente y licencia. La música, voces generadas o samples pasan el mismo control de procedencia que el arte. No se clonan voces reales sin autorización. El MVP puede usar sonidos temporales gratuitos compatibles, identificados como reemplazables, antes de encargar una identidad sonora propia.

**Aceptación:** con música al 100% y voces al 0%, se distinguen impacto, esquiva y KO; con audio apagado siguen existiendo señales visuales suficientes; no hay clipping audible en la escena de estrés.

## 17. MVP y estrategia de alcance

### 17.1 Contenido comprometido para validar

- Piloto de calidad con Don Promesio y Lola Viral, reutilizando los kits internos de Don Decreto y Lola Loop sin renombrar código o assets en esta preparación.
- Básico, fuerte, especial, recuperación y super por luchador; esquiva, doble salto, apuntado y lanzamiento vectorial 3D. No se exigen cuatro especiales direccionales nuevos ni agarres o escudo.
- Azotea Neón como primera arena de evaluación, con geometría fija. El Gran Debate queda como propuesta de escenario posterior.
- Versus local 1v1, tres stocks y cuatro minutos como preset de diseño configurable; la beta conserva cinco minutos hasta que una tarjeta cambie y pruebe ese dato.
- Entrenamiento mínimo, inspección de hitboxes y reinicio rápido; dispositivos, pausa, resultados y revancha.
- HUD legible, remapeo esencial, volumen por categoría, reducción visual y procedencia de assets documentada.
- Build Windows reproducible y pruebas de regresión de los cinco kits y cuatro arenas ya existentes.

### 17.2 Contenido pospuesto

Los ocho arquetipos aún sin kit en la beta, arte final de los trece, escenarios nuevos, equipos, bots avanzados, online público, ranking, tienda cosmética, cinemáticas y doblaje extenso. Los cinco kits y cuatro arenas existentes se preservan; una vertical slice 2/1 es una concentración de calidad, no una eliminación de contenido ni una afirmación de que el proyecto empieza vacío.

### 17.3 Calidad mínima del MVP

El arte puede ser reducido, pero los personajes deben tener siluetas distintas, poses de ataque legibles y ausencia de deformaciones graves. Los sistemas críticos no pueden depender de botones ocultos de depuración. Se admite una pantalla de selección simple; no se admite perder control al reconectar un mando o atravesar una plataforma por una animación.

### 17.4 Política de recorte

Si un hito supera el presupuesto, se reduce primero contenido accesorio, número de efectos, complejidad del fondo y variaciones de animación. Se conserva la claridad del combate, la estabilidad de entrada y los criterios de aceptación. Un especial complicado se simplifica antes de añadir una dependencia pesada para sostenerlo.

## 18. Roadmap por hitos y puertas de aceptación

Los plazos orientan a una persona con dedicación parcial. Cada sprint de dos semanas se ajusta a horas reales; el alcance manda sobre la fecha. Se comienza verificando lo existente, sin reimplementar sistemas por seguir una plantilla.

### H0 — Base preservada y diagnóstico de producción

Duración inicial: 2–4 días de trabajo. Inventario, respaldo verificable, dependencias resueltas y decisión 3D libre registrada. Aceptación: identificar build, rutas, paquetes, controles, fuentes/licencias y limitaciones; repetir la prueba histórica en una carpeta de evidencia nueva y distinguir resultados nuevos de históricos. Primer sprint: cerrar tres problemas demostrados.

### H1 — Movimiento 3D cámara y escenario piloto

Rango: 1–2 semanas después del diagnóstico. Medir controlador existente, diagonales, apuntado, salto, recuperación y bordes de Azotea Neón. Aceptación: cien ciclos KO/reaparición, reconexión recuperable y cámara que muestre separación en X/Z/Y. Conservar regresión de cinco participantes. Tres personas completan desplazamiento y vuelta a la arena tras una instrucción breve.

### H2 — Calidad del combate de dos kits

Rango: 2–4 semanas. Evaluar básico, fuerte, especial, recuperación y super de Don Promesio/Lola Viral; calibrar knockback vectorial y esquiva. Aceptación: impactos sin duplicación accidental, KO simultáneo consistente, volúmenes inspeccionables y veinte partidas sin estados atascados. Cinco personas prueban parejas con distintos niveles; se registran duración, caídas propias, contrajuego entendido y movimientos ignorados. No se exige un nuevo sistema de cuatro especiales, agarres o escudo.

### H3 — Arte audio accesibilidad y pipeline

Rango: 2–4 semanas. Terminar un rig deformable y un personaje de referencia antes del segundo; integrar toon, una sección de arena y mezcla. Aceptación: materiales correctos en build, poses extremas sin deformación grave, lectura 360 grados, controles/opciones recuperables y procedencia completa. Aprobar el coste real por asset antes de fabricar variantes.

### H4 — Vertical slice local

Rango: 2–3 semanas. Completar inicio, selección, tutorial breve, partida, pausa, resultados y revancha. Aceptación: persona nueva inicia sin ayuda, revancha en dos confirmaciones, sesión de treinta minutos sin bloqueo y rendimiento medido en equipo identificado. La revisión decide continuar, ajustar o reducir, con evidencia; conserva pruebas de los cinco kits y cuatro arenas de la beta.

### H5 — Consolidación local ampliada

Se estima tras H4. Elevar a calidad coherente los otros tres kits y arenas existentes antes de ampliar el roster. Verificar dos a cinco participantes, cámara/oclusiones, mandos físicos y coste de efectos. No prometer fecha de trece personajes hasta medir producción de dos terminados.

### H6 — Investigación online

Experimento de 2–4 semanas, después de aprobar el juego local. Captura/restauración de estado, repetición de entradas y red simulada. Aceptación: divergencias reproducibles, estrategia para física no determinista, desconexión y presentación de correcciones, costes operativos estimados. Salida: decisión escrita continuar o posponer. Ningún framework garantiza rollback por instalarlo.

### H7 — Producción beta y lanzamiento

Solo tras puertas anteriores: QA externo, accesibilidad, licencias, localización, distribución y soporte. El lanzamiento puede ser local. Online, ranking y expansión se anuncian únicamente cuando estén validados y financiados.

## 19. Riesgos y respuesta

| Riesgo | Señal temprana | Respuesta y condición de cierre |
|---|---|---|
| Roster de trece desborda capacidad | Segundo personaje tarda igual o más que el primero | Congelar expansión; medir coste del MVP y priorizar 4–6 para alpha |
| Arte IA inconsistente o sin derechos claros | Variaciones de cara, UV pobres, términos incompatibles | Target aprobado, limpieza humana y procedencia; sustituir asset si no cumple |
| Combate parece flotante | Caídas largas sin decisiones, KO solo por tiempo | Medir arcos, arrastre y cobertura; ajustar una variable por prueba |
| Personajes ilegibles con efectos | No se reconoce quién golpeó | Reducir VFX/fondo y reforzar poses/colores identificadores |
| Paquetes incompatibles | Errores nuevos al abrir o compilar | Volver a respaldo; una dependencia por cambio; versión fijada |
| Online obliga a rehacer toda la lógica | Estado vive en Animator, física y corutinas dispersas | Separar simulación/presentación desde MVP y gate técnico antes de compromiso |
| Sátira se interpreta como ataque personal | Parecido reconocible o chiste sin conducta objetivo | Revisión editorial, arquetipos compuestos, rediseño de nombre/voz/imagen |
| Rendimiento cae con cuatro luchadores | Picos de skinning, transparencias o contorno | Presupuesto por asset y escena de estrés desde arte de referencia |
| El jugador no vuelve al escenario | Muertes frecuentes sin presión del rival | Tutorial y trayectorias legibles; revisar recursos y geometría |
| El proyecto pierde trazabilidad | Fuentes sin licencia, archivos “final2”, cambios mezclados | Convenciones, manifiesto de assets, commits pequeños y backup comprobado |

Cada riesgo se convierte en tarjeta con responsable, prioridad y fecha de revisión. No basta etiquetarlo; debe existir una prueba que permita cerrarlo o aceptarlo conscientemente.

## 20. Métricas, pruebas de juego y criterios de aceptación

### 20.1 Protocolo de playtest

Preparar build numerada, reglas fijas, controles impresos y hoja breve. Pedir que la persona juegue sin explicación larga; observar antes de intervenir. Registrar experiencia previa, dispositivo, personaje, duración, KO, caídas propias y comentarios literales. Evitar preguntas dirigidas como “¿te gustó lo divertido que es?”.

Preguntas de salida: ¿qué intentabas hacer?, ¿por qué crees que perdiste ese stock?, ¿qué movimiento no entendiste?, ¿qué personaje elegirías otra vez?, ¿qué te impediría jugar otra partida? La observación de una causa repetida pesa más que una sugerencia aislada de añadir contenido.

### 20.2 Metas iniciales de validación

- Al menos 4 de 5 participantes de una prueba exploratoria explican la relación daño/lanzamiento después del tutorial.
- Al menos 4 de 5 realizan una recuperación básica tras practicar brevemente.
- La mayoría de partidas observadas termina por stocks dentro del rango esperado; si la muestra es pequeña se registra sin inferir conclusiones definitivas.
- Ningún movimiento imprescindible solo se entiende consultando a quien lo programó.
- Don Promesio y Lola Viral reciben descripciones de rol distintas sin mostrar su ficha.
- Cero errores bloqueantes en la sesión de aceptación y ningún crecimiento sostenido de entidades tras revancha repetida.

Estas metas son puertas prácticas, no resultados estadísticos de mercado. Deben repetirse con más participantes antes de usar afirmaciones públicas sobre facilidad o balance.

### 20.3 Definition of Done de una funcionalidad

Comportamiento y caso de uso definidos; implementado sin errores nuevos; datos y límites validados; probado con ambos jugadores y lados; entrada, pausa y reinicio correctos; accesibilidad relevante comprobada; build o evidencia adjunta; documentación actualizada; fuente/licencia registrada si incorpora asset; cambio versionado y revisado; limitaciones conocidas descritas.

Para un movimiento: además, anticipación/activo/recuperación visibles, hitbox coherente, impacto único correcto, interrupción segura, contrajuego identificable y ausencia de estados permanentes. Para un asset: deformación, importación, materiales, presupuesto y procedencia aprobados en motor.

### 20.4 Aceptación global del MVP

1. Dos personas pueden conectar dispositivos, elegir luchador y terminar una partida 1v1.
2. Daño, knockback, stocks, empate, pausa, reaparición y rematch siguen las reglas documentadas.
3. Los dos personajes tienen básico, fuerte, especial, recuperación y super funcionales, con diferencias legibles y contrajuego.
4. Hitboxes y hurtboxes respetan posición y altura 3D; apuntado y knockback no pierden profundidad; la presentación y el root motion no compiten con el controlador.
5. Una build Windows limpia ejecuta el flujo completo con ajustes y guardado recuperable.
6. Se alcanzan objetivos de rendimiento en hardware de prueba identificado, con registro reproducible.
7. Opciones esenciales de entrada, audio y reducción visual funcionan.
8. Los assets publicables tienen procedencia y condiciones de uso compatibles documentadas.
9. El respaldo inicial y el estado final del proyecto pueden localizarse y recuperarse.
10. La prueba externa produce una decisión explícita sobre la siguiente inversión de tiempo.

## 21. Decisiones que deben cerrarse durante producción

### D01 — Cámara ortográfica o perspectiva leve

**Propietario:** diseño/arte. **Prueba:** misma partida y geometría con ambos encuadres. **Cierre:** elegir la opción con mejor lectura de profundidad, altura, bordes y apuntado, con límites de zoom y tratamiento de oclusiones. Resolver en H1 antes de cerrar arte.

### D02 — Colisión y futura reproducibilidad online

**Propietario:** programación. **Prueba:** repetir entradas y comparar estados con velocidades altas, plataformas y bordes. **Cierre:** arquitectura local fiable y límites conocidos de determinismo. No bloquear el MVP por una red futura, pero impedir que la animación sea fuente de reglas.

### D03 — Look toon definitivo

**Propietario:** arte/técnica. **Prueba:** material de referencia con cuatro modelos, sombras y efectos en build. **Cierre:** aspecto aprobado, licencia compatible, coste medido y versiones fijadas. Resolver en H3.

### D04 — Uso de Meshy según términos y presupuesto

**Propietario:** producción. **Prueba:** revisar condiciones vigentes del plan y limpiar un candidato completo. **Cierre:** coste real y derechos documentados; si no encaja, pipeline Blender directo viable. Ningún asset pasa a publicación por la mera existencia de un botón Exportar.

### D05 — Ampliación de roster

**Propietario:** producto. **Prueba:** medir tiempo de CH01/CH02 y resultados de H4. **Cierre:** seleccionar próximos dos personajes por diversidad de mecánicas y coste, con criterio de éxito. No producir once rigs en paralelo.

### D06 — Online como inversión

**Propietario:** producto/programación. **Prueba:** H6 y estimación operativa. **Cierre:** continuar con alcance/costes aceptados o posponer manteniendo una versión local completa. La decisión se comunica en materiales de producto antes de venta.

## 22. Primeras acciones de producción

1. Revisar el informe técnico y fijar la versión real del editor y los paquetes compatibles.
2. Abrir el proyecto conservado o crear un proyecto nuevo únicamente si se confirma que no existe uno utilizable.
3. Medir Azotea Neón existente y crear `CombatLab` aislado solo para casos que no puedan reproducirse de forma controlada en la beta.
4. Validar unión de dos dispositivos, movimiento, salto y recuperación antes de importar personajes complejos.
5. Crear un ataque neutral de datos con hitbox visible y un único impacto por objetivo.
6. Añadir fórmula de lanzamiento, KO, stock y reinicio; probar ambos lados y KO simultáneo.
7. Probar la primera partida local con modelos provisionales y registrar tres problemas observables.
8. Crear un único personaje visual de referencia y un prop de escenario para validar Blender→Unity.
9. Elegir tres tarjetas pequeñas del siguiente sprint a partir de problemas demostrados.
10. Mantener el GDD, las tarjetas y los mapas de Miro enlazados a la versión de build revisada.

## 23. Glosario

**3D libre:** movimiento horizontal X/Z, altura Y y volúmenes de combate tridimensionales. **Stock:** vida disponible. **Porcentaje:** daño acumulado que influye en lanzamiento. **Knockback:** impulso de expulsión. **Hitbox:** zona ofensiva. **Hurtbox:** zona vulnerable. **Hitstop:** pausa breve de impacto. **Hitstun:** tiempo sin actuar tras recibir golpe. **Neutral:** situación sin ventaja consolidada. **Edgeguard:** presión para impedir recuperación. **Recovery:** acciones para volver al escenario. **Greybox:** geometría de prueba sin arte final. **Vertical slice:** fragmento completo que demuestra calidad y flujo de producción. **Gate:** condición verificable para autorizar el siguiente hito. **Rollback:** técnica que restaura y vuelve a simular estados cuando llegan entradas de red tardías.

## 24. Registro de cambios

**1.0 — 10/11/2026:** visión, reglas y arquitectura de base; trece arquetipos con fuerte, especial, recuperación y super propuestos; MVP local de dos luchadores; pipeline 3D, escenarios, accesibilidad, presupuestos provisionales, riesgos y puertas de aceptación.

Para versiones siguientes registrar: fecha, responsable, decisión, razón, evidencia y tarjetas afectadas. Los cambios de valores de balance pueden residir en datos del proyecto; el GDD conserva las reglas y la intención que permiten evaluarlos.
