# Multijugador local · Noche en el Chapare

El plan completo del juego está en `PlanNocheChapare.md`.

## Jugar

Abre `Assets/Scenes/MenuInicio.unity`, pulsa Play y elige **Multijugador LAN**.

1. Cada participante usa una copia del mismo juego en un ordenador de la misma red.
2. El anfitrión elige nombre, bando y clase, y pulsa **Crear sala**.
3. Los demás escriben la IP mostrada en esa sala y pulsan **Unirse por IP**.
4. El anfitrión elige con **−** y **+** cuántos hay en cada bando: de 2 a 8 por lado (hasta 16
   en total). Los puestos libres son bots. En la organización, un jugador puede pulsar **Ser el líder**. Si nadie lo hace, el líder
   es un bot.
5. Los invitados pulsan **Estoy listo**. El anfitrión pulsa **Iniciar partida**.
6. **Selección (20 s).** Cada jugador elige uno de los ocho personajes de su bando con
   las flechas o clic, y confirma con ENTER o **¡LISTO!**. Ve en vivo qué miran sus compañeros y
   quién confirmó; del otro bando solo sabe cuántos están listos. Los bots eligen solos y
   el líder no elige. La noche empieza cuando todos confirman o se acaba el tiempo.

No hace falta llenar todos los puestos: los espacios vacíos los ocupan bots.
Se puede entrar durante una partida, reemplazando a un guardia o agente bot en su
estado actual: entrar no revive a nadie. Un jugador desconectado se sustituye por un
bot, también si era el líder. Si sale el anfitrión, termina la conexión para todos;
no hay migración de anfitrión ni reconexión automática.

La red usa TCP 7777. Si el sistema pregunta, permite las conexiones de red local
del juego. Una red de invitados con aislamiento entre dispositivos puede impedir
la conexión aunque ambos estén conectados al mismo Wi-Fi. No requiere cuentas ni
servicios de Internet. La IP se escribe manualmente; no hay buscador de salas.

## Partida

Cada partida es una noche. Por defecto, 4 agentes de la DEA contra 3 guardias y el líder; el
anfitrión puede elegir de 2 a 8 por bando.

1. **Despliegue (60 s).** Nadie dispara. La organización camina libre y esconde al líder.
   La DEA espera fuera del mapa en el **plan de despliegue**: un mapa táctico con
   cuadrícula, barrido de radar y el campamento enemigo marcado. Cada agente elige un punto
   de entrada (Río, Camino, Pista o Selva) con clic en el mapa o en su tarjeta, y ve en vivo
   por dónde entran sus compañeros y la ruta del helicóptero. Ningún bando ve al otro.
2. **Noche (8 min).** El reloj corre hacia el amanecer.
3. **Final.** Hay tres formas de terminar:
   - La DEA esposa al líder: gana la DEA.
   - Amanece con el líder libre: gana la organización.
   - La DEA se queda sin agentes vivos ni refuerzos: gana la organización.

**Líder.** Sin vida queda reducido 20 s, sin moverse ni disparar. Un agente lo esposa
manteniendo F durante 5 s a 1,5 unidades o menos, y no puede disparar mientras lo hace.
Recibir daño o alejarse reinicia el progreso. Mientras lo esposan, el líder no se recupera.
Si nadie lo esposa a tiempo, vuelve con el 40 % de su vida. Usa pistola y su propia clase.

**Refuerzos.** Los caídos esperan a la oleada, que llega cada 30 s. La DEA tiene 3 refuerzos
por agente y la organización 2 por integrante (12 y 8 en 4 contra 4). El líder no gasta refuerzos.

**Noche y visión.** El despliegue es el atardecer, la noche es oscura y en los últimos
90 s amanece poco a poco. De noche:

- Sin luz se ve a 5 unidades.
- La linterna (**T**) deja ver hasta 13 dentro de su cono, pero quien la lleva se ve desde 20.
- Disparar delata durante un instante.
- Los faroles del campamento, las bengalas, el fuego y el humo rojo del bombardeo iluminan
  su zona. Un farol se puede apagar a tiros o con una explosión.

Los rivales, sus nombres y sus anillos solo se dibujan si alguien de tu equipo los ve.
Los bots siguen las mismas reglas.

**Items.** Hay 18 puntos repartidos por el mapa y cada oleada repone dos vacíos. Se lleva
uno a la vez: **F** recoge o cambia, **C** usa y caer lo suelta.

| Item | Efecto |
|---|---|
| Botiquín | +45 de vida; no se gasta con la vida llena |
| Munición | Recarga la reserva del arma que llevas y suma 2 granadas (máximo 6) |
| Granadas | Suma 3 granadas (máximo 6) |
| Visor nocturno | 25 s viendo a 12 unidades sin linterna |
| Bengala | Se lanza 8 unidades al frente e ilumina 10 durante 20 s |
| Alarma | Se coloca a tus pies y avisa a tu equipo cuando pasa un rival |

Los guardias ven en pantalla dónde está su líder cuando sale de la vista.

- Sin daño entre compañeros. Los obstáculos bloquean disparos.
- Cada vida comienza con un rifle y granadas. **1** elige el arma y **2** la granada (con la
  granada en la mano, el clic la lanza; **G** la lanza siempre). Al caer o pulsar X, se suelta
  el arma conservando la munición; puede recogerse con F. Las armas del suelo flotan y giran.
- **M** abre el mapa grande; arriba a la izquierda siempre está el minimapa.
- Esc muestra opciones; la partida online sigue avanzando.
- Al finalizar, el anfitrión puede volver a la sala y empezar otra ronda.

Todos estos valores están en `LanNightRules.cs`.

**Personajes.** Cada uno tiene una habilidad con **clic derecho** y su tiempo de recarga.

| Bando | Personaje | Vida | Defensa | Velocidad | Cargador / reserva | Habilidad (recarga) |
|---|---|---:|---:|---:|---:|---|
| DEA | Asaltante | 90 | 10 | 4,2 | 24 / 90 | Suministro aéreo: una avioneta suelta una caja en paracaídas con botiquín, munición, granadas y un rifle (45 s) |
| DEA | Pesado | 160 | 40 | 2,8 | 24 / 90 | Escudo frontal 4 s y cúpula donde los aliados reciben la mitad durante 6 s (16 s) |
| DEA | Rastreador | 110 | 15 | 3,3 | 40 / 210 | Rastreo: muestra a la DEA dónde está el líder, y él lo sabe. Uno por minuto para el equipo |
| DEA | Paramédico | 100 | 15 | 3,6 | 24 / 90 | Estación médica: cura 30 de vida por segundo en 4 unidades durante 40 s; 150 de vida, se puede destruir (45 s) |
| DEA | Granadero | 120 | 25 | 3,1 | 24 / 120 | Andanada: durante 10 s lanza una granada por segundo hacia donde apunta, alternando explosivas y aturdidoras (30 s) |
| DEA | Explorador nocturno | 90 | 10 | 3,9 | 24 / 90 | Bombardeo: humo rojo, 3,5 s de aviso y 5 explosiones (50 s) |
| DEA | Francotirador | 90 | 10 | 3,4 | 10 / 60 | Pasiva: ×2,2 de daño y 36 unidades de alcance, dispara algo más lento. Helicóptero: llega al punto marcado, persigue y ametralla a los rivales cercanos (sin alejarse más de 30 unidades) durante 18 s, deja dos soldados de la DEA y se retira (60 s) |
| DEA | Operador de dron | 100 | 15 | 3,6 | 24 / 90 | Enjambre: base en el suelo que lanza 40 drones kamikaze, 6 a la vez. Recorren el mapa; cuando uno encuentra a un rival, los demás van a esa ubicación. Se estrellan contra ellos: 50 de daño y los deja marcados (90 s) |
| Org | Pistolero | 90 | 10 | 4,2 | 24 / 90 | Ráfaga: dispara el doble de rápido durante 6 s (8 s) |
| Org | Guardaespaldas | 160 | 40 | 2,8 | 24 / 90 | Muralla de sacos (15 s). Pasiva: escolta al líder (−30 % de daño) |
| Org | Reclutador | 110 | 15 | 3,3 | 40 / 210 | Reclutar: 6 comuneros con machete; cada uno con 30 % de ser kamikaze (30 s) |
| Org | Cocalero | 110 | 15 | 3,8 | 24 / 90 | Frenesí: doble daño durante 8 s y va dejando hasta 10 hojas de coca. Quien las consume (C) hace ×1,5 de daño durante 6 s (25 s) |
| Org | Curandera | 100 | 10 | 3,6 | 24 / 90 | Fogata: cura 15 por segundo en 3,5 unidades sin límite de tiempo hasta que la destruyan (100 de vida); ilumina y delata (20 s, la nueva reemplaza a la anterior) |
| Org | Trampero | 100 | 10 | 3,9 | 24 / 90 | Mina que estalla y marca (15 s) |
| Org | Sicario | 100 | 10 | 4 | 24 / 90 | Chutos: convoy de tres camionetas reales (con colisión, destructibles) que llega a toda velocidad por los caminos al punto marcado. La primera, artillada, entra disparando con dos NPC a bordo, que saltan a pelear si la destruyen. Las otras dos frenan derrapando y de cada una bajan dos pistoleros con 2 granadas; después la organización puede manejarlas (40 s) |
| Org | Contrabandista | 110 | 20 | 3,6 | 24 / 120 | Bomba de humo: tapa la vista 10 s (18 s) |
| Org | Líder | 150 | 20 | 3,3 | 12 / 60 (pistola) | Lanceros: 6 defensores durante 25 s y hasta 3 escoltas armados que lo siguen (60 s). Chaleco contra explosiones |

**Apoyo NPC.** Los soldados del helicóptero, los pistoleros de los chutos y los escoltas del líder:

- **Acompañan:** van con su dueño o con el aliado más cercano.
- **Atacan:** a los rivales que ven ellos o que tiene cerca un compañero (a 8 unidades, o 16 si está iluminado o se
  delata).
- **Se mueven con inteligencia:** van por la navegación hasta tener ángulo de tiro y se apartan de granadas, fuego y
  bombardeos.
- **Duran y caen:** duran 45 s y se pueden abatir.
- **Granadas:** los pistoleros lanzan sus dos a media distancia.
- **Drones:** los del enjambre vuelan sobre los obstáculos.

Las habilidades de la organización recargan más rápido que antes. Las alarmas (item) marcan al intruso durante 4 s. Los valores están en `LanAbilities.cs`.

**Granadas, bombardeo y fuego** (`LanWorld.Explosives.cs`).

- **G** lanza una granada hacia el cursor, hasta 12 unidades, sin atravesar paredes. Vuela,
  rebota y parpadea; explota a los 1,8 s. Daña hasta 95 en 3,8 unidades, y tras una
  cobertura, solo la cuarta parte. Deja fuego 4,5 s.
- Cada vida empieza con granadas: cada agente de la DEA 5, la organización 2 y el líder 1.
  La munición suma 2 y el item Granadas 3, con un máximo de 6.
- El bombardeo lanza 5 cargas de 65 de daño y 3,2 unidades de radio, que dejan fuego 5 s.
- El fuego quema 12 por segundo a quien se queda dentro e ilumina alrededor, así que delata.
- Como las balas, los explosivos no dañan a compañeros. Los barriles explotan sin bando y
  dañan a todos.

**Vehículos manejables** (`LanWorld.Vehicles.cs`, modelos low poly propios). La DEA tiene dos blindados, en las
entradas Camino y Pista; la organización, dos **chutos** (camioneta doble cabina con ametralladora atrás, 420 de vida y
más rápida) en el camino este del campamento. Todos se manejan igual:

- **Una sola plaza:** se sube con **V**. Se conduce con WASD y la torreta apunta al ratón;
  el clic dispara la ametralladora. **V** otra vez baja por el costado.
- **Escala:** mide unas 1,75 de largo y 1 de ancho, cerca de dos personajes.
- **Ametralladora:** 12 de daño, 10 disparos por segundo, 28 unidades de alcance.
- **Blindaje:** 700 de vida. Las balas y explosiones dañan al vehículo, no al conductor, y
  las explosiones le hacen un 30 % más.
- **Destrucción:** el conductor sale herido, el vehículo estalla y vuelve a los 90 s.
- **Visibilidad:** el motor y los faros lo delatan a 20 unidades.
- **Bots:** no lo usan.

**Pista clandestina.** Tiene línea central y franjas de umbral pintadas, luces a los costados,
manga de viento, 3 bidones explosivos y una avioneta estacionada que se puede destruir: estalla
y queda como chatarra quemada. Los modelos se crean con **Tools > Multijugador > Crear modelos
low poly** (`LanModels.cs`, `ModelSetup.cs`) en `Assets/Art/Modelos`.

**Entorno destructible** (`LanWorld.Props.cs`). El mapa tiene 1.115 objetos destructibles:
952 hexágonos de selva, 53 árboles, 51 matorrales, 27 rocas, 14 cajas, 7 barriles, 6 tiendas,
4 faroles y la avioneta.

- **Explosiones:**
  - Las granadas tumban árboles, matorrales, cajas, rocas y tiendas.
  - La selva solo cae con el bombardeo.
  - Al caer un objeto desaparece su colisión, así que se abre paso y vista.
- **Balas:** rompen cajas, barriles, tiendas, faroles y la avioneta.
- **Restos:**
  - Los árboles caen quemados, dejan tocones y arden un rato.
  - Las rocas se parten, y cajas y tiendas sueltan tablas.
  - Cada explosión deja una quemadura en el suelo.
- **Red:** el anfitrión decide qué se rompe y envía la lista de rotos. Quien entra a mitad de
  partida ve el mapa ya dañado, sin animación.
- **Límite conocido:** la navegación de los bots no cambia. No usan los huecos nuevos, pero sí
  rodean las murallas.

**Mapas y avisos** (`LanUI.Map.cs`). La imagen táctica (`MapaTactico.png`) se genera junto
con el mapa. El diseño da el fondo y una foto cenital aporta río, caminos y árboles sueltos.

- **Minimapa:** gira con la cámara y muestra:
  - Tú y tus aliados; los guardias ven al líder como rombo dorado.
  - Rivales a la vista.
  - Fuego, murallas, granadas cercanas, la zona del bombardeo, el rastreo, la cúpula y el suministro.
  - El blindado; la organización ve además a sus comuneros, con los kamikazes en rojo.
- **Disparos rivales** (estilo Call of Duty): los que se oyen a 45 unidades aparecen en el
  minimapa como un punto rojo con onda. Si el tirador está fuera de pantalla, una flecha roja
  en el borde indica la dirección.
- **Daño recibido:** un arco rojo alrededor del centro apunta hacia el origen (bala,
  explosión, fuego o lancero) y la pantalla se tiñe un instante.
- **Avisos centrales:**
  - «¡GRANADA!» con una flecha hacia la granada.
  - «¡BOMBARDEO! SAL DE LA ZONA».
  - «RASTREO · LÍDER UBICADO» para la DEA, con una flecha si queda fuera de pantalla.
  - «¡TE UBICARON! CAMBIA DE ESCONDITE» para el líder, y para sus guardias «¡RASTREARON A TU
    LÍDER!».

**Aspecto.** Los agentes de la DEA visten pantalón táctico negro y chaqueta azul marino con
«DEA» en letras amarillas en la espalda y en el pecho; según el puesto, llevan gorra negra con
DEA, casco táctico, chaleco, placa, auricular, escudo balístico, sombrero boonie o mando de dron
(`Agentes-DEA.png`). Los personajes son las unidades de KayKit (azul para la DEA, rojo para la
organización) con piezas del paquete y accesorios 3D propios:

| Personaje | Aspecto |
|---|---|
| Asaltante | Gorra y chaleco con franja amarilla |
| Pesado | Casco y escudo de KayKit, chaleco |
| Rastreador | Gorra, chaleco y mochila con antena de radio |
| Pistolero | Pañuelo y gafas oscuras |
| Guardaespaldas | Traje negro con corbata y gafas oscuras |
| Vigía | Estandarte de KayKit y binoculares |
| Líder | Sombrero y cadena de oro |

Se generan con **Tools > Multijugador > Crear personajes** (`CharacterSetup.cs`). Los
accesorios están en `Assets/Art/Personajes` y los personajes en
`Assets/Resources/Multiplayer/Personajes`.

**Animación tipo caricatura** (`LanCartoon.cs`). Como las unidades no tienen esqueleto,
todo se hace moviendo, girando y deformando el modelo:

- Respira en reposo y rebota al correr, estirándose y aplastándose.
- Retrocede al disparar, se sacude al recibir daño y se estira al usar el Avance.
- Aparece con un salto elástico, cae de bruces al morir y luego se encoge.
- El líder reducido queda tumbado y tambaleándose.
- Al terminar, el bando ganador salta girando y el perdedor se desanima.

Todo sale del estado de red, así que se ve igual en cada pantalla. Los retratos de la
selección se dibujan en baja resolución, con aire de pixel art arcade (`LanPortraits.cs`).

**Efectos y sonido.** No hay efectos mágicos: los disparos tienen fogonazo con destello de luz,
los impactos sueltan chispas y polvo, aparecer, morir y usar habilidades levanta polvo, la
protección al aparecer hace parpadear el anillo, y el escudo del Pesado es un arco azul al
frente (`LanFx.cs`).

El sonido (`LanAudio.cs`) incluye:

- Grillos y viento que se apagan al amanecer, y música de tensión.
- Helicóptero cuando entra la DEA.
- Radio en cada oleada y cuando cae el líder.
- Esposas al empezar la captura y alarma para el equipo que la colocó.
- Siseo de las bengalas y sonidos propios de la selección.

También suenan el motor del blindado, la avioneta del suministro, explosiones, el silbido del bombardeo, el pitido del rastreo (para los dos
bandos), los sacos de la muralla, el fuego, el rebote de la granada y la madera al romperse.
Las explosiones se oyen hasta 70 unidades y sacuden la cámara si están cerca.

Todos los sonidos son originales, generados con `Tools/generar_sonidos.py` en
`Assets/Audio/Generado`. El fuego de las explosiones combina partículas propias con dos
efectos de fuego de PixPlays (`FireBlast` y `FireAoeVFX`), asignados con
**Tools > Interfaz > Asignar recursos de combate**.

**Estilo.** Paneles azul noche, ámbar y botones planos en negrita. El menú principal usa
una foto nocturna del campamento y conserva el título del juego. Se aplica con
**Tools > Interfaz > Aplicar estilo Noche en el Chapare** (`StyleSetup.cs`).

La defensa reduce el daño mediante `daño × 100 / (100 + defensa)`.
Correr multiplica la velocidad por 1,5 y consume estamina. El salto también consume.

## Escenas y edición

- `MenuInicio`: entrada al juego solo y a la sala LAN.
- `ValleDelRio`: experiencia individual, conservada.
- `NocheChapare`: mapa hexagonal hecho con las piezas de KayKit. Tiene radio 30
  (unas 120 × 105 unidades) y 3.367 hexágonos, con montañas como borde. Incluye río con
  3 puentes, campamento rojo, caminos, pista de tierra, selva (36 %) y 6 refugios entre los
  árboles. Se regenera con **Tools > Multijugador > Crear mapa del Chapare**, que usa
  `ChapareSetup.cs`. Las piezas son prefabs editables. Las colisiones son cilindros y cajas
  simples dentro de `COLISIONES`; las de objetos destructibles llevan `LanProp`. El mismo
  comando genera la imagen táctica, y **Rehacer mapa táctico** la rehace sola.
- El río no se cruza caminando, solo por los puentes. Sus colisiones usan la capa *Water*,
  que la vista y las balas ignoran, así que se puede disparar a la otra orilla.
- `ValleMultijugador`: arena anterior. Ya no se carga ni entra en la exportación.
- `Assets/Resources/Multiplayer/LanAssets.asset`: modelos, sonidos y recursos UI.
- `LanNightRules.cs`: valores y reglas de la noche.
- `LanBots.cs`: comportamiento de los bots según su rol.
- `LanProtocol.cs`: clases y formato de mensajes, en versión 7. Las copias anteriores
  del juego no pueden conectarse.

El anfitrión simula los ocho personajes, colisiones, daño, inventario, reglas y bots.
Los clientes envían controles; no envían vida, munición o daño decidido por ellos.
Los sockets usan colas acotadas y mensajes con longitud explícita. La simulación y
las instantáneas se actualizan a 20 Hz. No se ha implementado predicción local ni
compensación de latencia; está pensado para una LAN de baja latencia.

Los bots ven hasta 14 unidades y con línea de visión. El líder va a un refugio y huye al
más lejano si ve enemigos, y cambia de refugio cuando lo rastrean. Los guardias se quedan
cerca de él y atacan amenazas cercanas. Los agentes recorren zonas de búsqueda, van a
donde marcó el último rastreo, recuerdan al último enemigo visto y esposan al líder
reducido. Todos lanzan granadas a rivales a media distancia y se apartan de granadas,
bombardeos y fuego del otro bando.

## Verificación

Validación del 2026-11-08 (madrugada), en el editor del usuario: **248 comprobaciones aprobadas**,
sin errores en consola.

| Archivo | Aprobadas | Qué cubre |
|---|---:|---|
| `LAN-night-check.txt` | 31 | Despliegue, oleadas, reducción, esposas, entrada tardía y los tres finales |
| `LAN-play-check.txt` | 32 | Anfitrión con siete clientes por sockets reales de loopback, combate y bot por desconexión |
| `LAN-client-check.txt` | 13 | Cliente Unity contra un servidor local de prueba |
| `LAN-bots-check.txt` | 8 | Seis noches completas solo con bots, con despliegue real, y entrada tardía |
| `LAN-fx-check.txt` | 12 | Efectos sin magia, sonidos de cada evento y estilo de la interfaz |
| `LAN-vision-check.txt` | 38 | Luz, visión, linternas por red, items, bengalas, alarmas y qué se dibuja |
| `LAN-ability-check.txt` | 54 | Personajes, cada habilidad medida (suministro, cúpula, rastreo, muralla, reclutar con kamikazes, mina, bombardeo, estación médica, andanada, helicóptero, enjambre, frenesí y coca, fogata, chutos y humo), activación por red y uso por bots |
| `LAN-select-check.txt` | 15 | Selección arcade por red, bots que eligen solos y modelos de cada personaje |
| `LAN-combat-check.txt` | 45 | Bandos de 8 contra 8 elegidos en la sala, granadas por red y en la mano, cobertura, fuego, destrucción y su envío a clientes, plan de despliegue, minimapa, mapa grande, avisos y el blindado de una plaza (subir, conducir, disparar, bajar, daño y destrucción) |

Comandos en **Tools > Multijugador**: *Comprobar reglas de la noche*, *Comprobar noches
con bots*, *Comprobar noche, vision e items*, *Comprobar habilidades*, *Comprobar seleccion y personajes*, *Comprobar efectos y sonido*, *Comprobar explosivos y mapas* y *Comprobar LAN en Play*.

Equilibrio con bots tras los explosivos. Las primeras tandas dieron 6 a 0 para la DEA: la
habilidad Proteger ya no existía, y la luz de las explosiones delataba al líder. Se ajustó:

- La selva solo cae con bombardeo.
- Las explosiones iluminan menos.
- El Guardaespaldas protege al líder de forma pasiva.
- El líder tiene 150 de vida y chaleco contra explosiones.
- La DEA empieza con 1 granada.
- El bombardeo tarda más en volver.
- El rastreo es por equipo, desde el primer minuto de noche.

Con los valores de la tanda anterior, dos tandas dieron 6 a 0 y 2 a 4 (8 de 12 para la DEA).
Con el despliegue de 60 s, 5 granadas por agente, reclutas, minas y recargas más cortas para
la organización, la tanda del 2026-11-08 dio 4 capturas, 1 amanecer y 1 derrota de la DEA. Casi
todos los derribos del líder son por bala. Seis noches por tanda varían mucho, así que el
ajuste fino queda para partidas con personas.

Mediciones:

- `LAN-ability-check.txt`:
  - El escudo deja 2,3 de daño de frente, frente a 11,4 sin él.
  - Tras la muralla el líder recibe 0, frente a 13,3 sin ella.
  - El Avance recorre 4 unidades.
- `LAN-combat-check.txt`:
  - Una granada hace 52 a un agente expuesto y 12,5 a otro igual de cerca tras una caja.
  - La ametralladora del blindado deja sin vida a un guardia de 90 en un segundo.
  - Tres balas y una granada le quitan unos 150 de vida al blindado, sin herir al conductor.

La prueba acelerada avanza la física en cada paso. Sin eso, disparos y visión usan
posiciones antiguas y los resultados no son válidos.

Capturas del mapa: `NocheChapare-mapa.png`, `NocheChapare-campamento.png` y
`NocheChapare-puente.png`. `LAN-match.png` muestra una partida en el mapa hexagonal, y
`Noche-linterna.png` y `Noche-bengala.png` muestran la noche con linterna y faroles.
`Personajes-sala.png` y `Personajes-hud.png` muestran los personajes en la sala y la
habilidad en el HUD. `Seleccion.png` muestra la selección arcade y `Personajes-partida.png`,
los modelos en partida. `Menu-principal.png` muestra el menú con el estilo nuevo.
`Despliegue.png` muestra el plan de despliegue y `Mapa-grande.png` el mapa con M.
`Blindado.png` muestra el blindado con los faros y `Pista.png`, la pista decorada con el
suministro cayendo. `Combate.png` y `Explosion.png` muestran el minimapa, el aviso de bombardeo, el arco de daño,
el fuego y una explosión. Las capturas `Noche-*.png` de la interfaz son del mapa gris anterior.

Escala del mapa: 5.156 piezas visibles, unos 1,26 millones de triángulos en total y 1.445
colisiones. La cámara solo ve una pequeña parte a la vez. El rendimiento en un ejecutable
todavía no se ha medido.

**Sin probar todavía:** teclado y ratón reales (mantener F y pulsar los botones de
entrada), y una red física entre varios ordenadores. Las pruebas llaman a las funciones
directamente y usan loopback.

Referencias técnicas: [sockets TCP de .NET](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/sockets/tcp-classes)
y [NavMesh Surface de Unity](https://docs.unity.cn/Packages/com.unity.ai.navigation%402.0/manual/NavMeshSurface.html).

## Cambios del 2026-11-07 (tarde)

- **Seis personajes por bando.** DEA: Paramédico (cura a aliados), Granadero (aturdidora) y
  Explorador nocturno (bengala). Organización, con aspecto local (sombrero de paja, aguayo,
  chuspa, machete, poncho, chullo, botas de goma): Cocalero (emboscada), Curandera (cura) y
  Trampero (trampa que aturde y marca).
- **Lanceros del líder:** 5 defensores, 35 de vida cada uno, golpe de 12 cuerpo a cuerpo,
  duran 25 s; recarga de 90 s. Interrumpen las esposas.
- **Equilibrio:** la DEA ya no busca en los refugios ni en los puntos cercanos a ellos, y el
  Pulso alcanza 14 unidades con recarga de 22 s. Con bots, la última tanda quedó 3 a 3:
  3 capturas, 1 amanecer y 2 derrotas de la DEA por agotar refuerzos.
- **Caminata a saltitos sin temblor**, retratos de selección nítidos, más naturaleza en el
  mapa y menú con vitrina, luciérnagas y fondo en movimiento.

## Cambios del 2026-11-07 (noche)

- **Plan de despliegue nuevo:** reemplaza la vista cenital amarilla por una pantalla táctica.
- **Minimapa, mapa grande (M) y avisos de disparos y daño**, al estilo de los shooters modernos.
- **Granadas (G)** con fuego. Soltar arma pasa a **X**.
- **Habilidades:**
  - Rastreo global del líder, que se entera.
  - Bombardeo del Explorador nocturno.
  - Muralla de sacos del Guardaespaldas.
- **Entorno destructible:** selva, árboles, rocas, cajas, barriles que explotan, tiendas y faroles.
- **Material de líneas:** las líneas brillantes usan uno nuevo que respeta su color. Antes
  todas salían celestes, también las alarmas y las trampas.
- **Protocolo:** versión 7.

## Cambios del 2026-11-08

- **Despliegue:** dura 60 s.
- **Granadas:** cada agente de la DEA lleva 5. Con 1 y 2 se cambia entre arma y granada.
- **DEA:**
  - Asaltante: pide un suministro aéreo, en vez del Avance.
  - Pesado: suma una cúpula para el equipo.
- **Organización:**
  - Reclutador (antes Vigía): comuneros, con 30 % de kamikazes.
  - Trampero: mina con recarga de 15 s.
  - Todas sus habilidades recargan más rápido.
- **Blindado de la DEA:** de una plaza, a escala de los personajes; el conductor también dispara.
- **Bombardeo:** recarga de 50 s.
- **Pista decorada** con modelos low poly propios.
- **Protocolo:** versión 9.

## Cambios del 2026-11-08 (tarde)

- **Tamaño de los bandos:** el anfitrión elige de 2 a 8 por bando (hasta 16 en total). Las
  personas conservan su bando al cambiarlo, y los refuerzos escalan con el tamaño.
- **Ocho personajes por bando:**
  - DEA: Francotirador (mira) y Operador de dron (dron).
  - Organización: Sicario (silenciador) y Contrabandista (bomba de humo).
  - El Vigía ya no existe; su lugar es el Reclutador.
- **Aspecto de agentes de la DEA:** chaqueta con DEA, pantalón y equipo táctico.
- **Bots:** con los 8 personajes, la tanda de 4 contra 4 dio 2 capturas y 4 derrotas de la
  DEA por agotar refuerzos (antes, 4 a 2 para la DEA). Con 6 noches por tanda varía mucho.
- **Protocolo:** versión 10; el tamaño de la sala cambia con el mensaje «seat».

## Cambios del 2026-11-08 (noche)

- **Vehículos:** un segundo blindado para la DEA y dos chutos manejables para la organización.
- **Campamento boliviano:** los edificios medievales se cambiaron por arquitectura boliviana a escala de los
  personajes, todos mirando al centro (`Campamento-boliviano.png`):
  - Casa de tejas.
  - Tienda de barrio.
  - Casa de adobe con calamina y piedras en el techo.
  - Dos palafitos con techo de jatata.
  - Casa encalada.
  - Tanque de agua elevado.
  - Pozo.
- **Habilidades nuevas:**
  - DEA: estación médica, andanada, helicóptero con soldados (el Francotirador pasa a tener alcance y daño
    pasivos) y enjambre de drones.
  - Organización: Ráfaga de 6 s, 6 reclutas, Frenesí con hojas de coca, fogata, y chutos con pistoleros (el
    Sicario pasa a tener silenciador pasivo).
- **Retirados:** Curar, Aturdidora, Emboscada, Mira, Dron y el láser del francotirador.
- **Bots:** la tanda de 4 contra 4 quedó 3 capturas y 3 derrotas de la DEA por agotar refuerzos.
- **Protocolo:** versión 11.

## Cambios del 2026-11-08 (madrugada)

- **Sicario:**
  - Sin pasiva.
  - El convoy de chutos ahora son vehículos reales que entran como en película: bocina, polvo, faros y derrape al
    frenar, con una camioneta artillada (`Convoy.png`).
  - Recarga de 40 s.
- **Helicóptero:** persigue a los rivales hasta que termina su misión.
- **NPC:** acompañan y apoyan a sus aliados.
- **Drones:** van en grupo al rival que encuentra cualquiera de ellos.
- **Líder:** suma escoltas armados.
- **Bots:** la tanda quedó 3 a 3.
- **Protocolo:** versión 12.
