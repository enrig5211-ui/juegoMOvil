# Multijugador local · Noche en el Chapare

El plan completo del juego está en `PlanNocheChapare.md`.

## Jugar

Abre `Assets/Scenes/MenuInicio.unity`, pulsa Play y elige **Multijugador LAN**.

1. Cada participante usa una copia del mismo juego en un ordenador de la misma red.
2. El anfitrión elige nombre, bando y clase, y pulsa **Crear sala**.
3. Los demás escriben la IP mostrada en esa sala y pulsan **Unirse por IP**.
4. En la organización, un jugador puede pulsar **Ser el líder**. Si nadie lo hace, el líder
   es un bot.
5. Los invitados pulsan **Estoy listo**. El anfitrión pulsa **Iniciar partida**.

No hace falta tener ocho personas: los espacios vacíos los ocupan bots.
Se puede entrar durante una partida, reemplazando a un guardia o agente bot en su
estado actual: entrar no revive a nadie. Un jugador desconectado se sustituye por un
bot, también si era el líder. Si sale el anfitrión, termina la conexión para todos;
no hay migración de anfitrión ni reconexión automática.

La red usa TCP 7777. Si el sistema pregunta, permite las conexiones de red local
del juego. Una red de invitados con aislamiento entre dispositivos puede impedir
la conexión aunque ambos estén conectados al mismo Wi-Fi. No requiere cuentas ni
servicios de Internet. La IP se escribe manualmente; no hay buscador de salas.

## Partida

Cada partida es una noche: 4 agentes de la DEA contra 3 guardias y el líder.

1. **Despliegue (90 s).** Nadie dispara. La organización camina libre y esconde al líder.
   La DEA espera fuera del mapa, ve el mapa completo y cada agente elige un punto de
   entrada: Río, Camino, Pista o Selva. Ningún bando ve al otro.
2. **Noche (8 min).** El reloj corre hacia el amanecer.
3. **Final.** Hay tres formas de terminar:
   - La DEA esposa al líder: gana la DEA.
   - Amanece con el líder libre: gana la organización.
   - La DEA se queda sin agentes vivos ni refuerzos: gana la organización.

**Líder.** Sin vida queda reducido 20 s, sin moverse ni disparar. Un agente lo esposa
manteniendo F durante 5 s a 1,5 unidades o menos, y no puede disparar mientras lo hace.
Recibir daño o alejarse reinicia el progreso. Mientras lo esposan, el líder no se recupera.
Si nadie lo esposa a tiempo, vuelve con el 40 % de su vida. Usa pistola y su propia clase.

**Refuerzos.** Los caídos esperan a la oleada, que llega cada 30 s. La DEA tiene 12
refuerzos y la organización 8. El líder no gasta refuerzos.

**Noche y visión.** El despliegue es el atardecer, la noche es oscura y en los últimos
90 s amanece poco a poco. De noche:

- Sin luz se ve a 5 unidades.
- La linterna (**T**) deja ver hasta 13 dentro de su cono, pero quien la lleva se ve desde 20.
- Disparar delata durante un instante.
- Los faroles del campamento y las bengalas iluminan su zona.

Los rivales, sus nombres y sus anillos solo se dibujan si alguien de tu equipo los ve.
Los bots siguen las mismas reglas.

**Items.** Hay 18 puntos repartidos por el mapa y cada oleada repone dos vacíos. Se lleva
uno a la vez: **F** recoge o cambia, **C** usa y caer lo suelta.

| Item | Efecto |
|---|---|
| Botiquín | +45 de vida; no se gasta con la vida llena |
| Munición | Recarga la reserva del arma que llevas |
| Visor nocturno | 25 s viendo a 12 unidades sin linterna |
| Bengala | Se lanza 8 unidades al frente e ilumina 10 durante 20 s |
| Alarma | Se coloca a tus pies y avisa a tu equipo cuando pasa un rival |

Los guardias ven en pantalla dónde está su líder cuando sale de la vista.

- Sin daño entre compañeros. Los obstáculos bloquean disparos.
- Cada vida comienza con un rifle. Al caer o pulsar G, se suelta el arma conservando
  la munición; puede recogerse con F. Las armas del suelo flotan y giran.
- Esc muestra opciones; la partida online sigue avanzando.
- Al finalizar, el anfitrión puede volver a la sala y empezar otra ronda.

Todos estos valores están en `LanNightRules.cs`.

**Personajes.** Cada uno tiene una habilidad con **clic derecho** y su tiempo de recarga.

| Bando | Personaje | Base | Vida | Defensa | Velocidad | Cargador / reserva | Habilidad (recarga) |
|---|---|---|---:|---:|---:|---:|---|
| DEA | Asaltante | Explorador | 90 | 10 | 4,2 | 24 / 90 | Avance: carrera de 0,35 s a 3,2× velocidad (8 s) |
| DEA | Pesado | Guardián | 160 | 40 | 2,8 | 24 / 90 | Escudo: −80 % de daño de frente durante 4 s (14 s) |
| DEA | Rastreador | Artillero | 110 | 15 | 3,3 | 40 / 210 | Pulso: marca 4 s a los rivales a 18 unidades, visibles tras paredes (16 s) |
| Org | Pistolero | Explorador | 90 | 10 | 4,2 | 24 / 90 | Ráfaga: dispara el doble de rápido durante 3 s (12 s) |
| Org | Guardaespaldas | Guardián | 160 | 40 | 2,8 | 24 / 90 | Proteger: el líder a 6 unidades recibe −60 % de daño durante 5 s (15 s) |
| Org | Vigía | Artillero | 110 | 15 | 3,3 | 40 / 210 | Coloca una alarma sin gastar items (20 s) |
| Org | Líder | Propio | 120 | 20 | 3,3 | 12 / 60 (pistola) | Refuerzos: trae a los guardias caídos gastando refuerzos (60 s) |

Una alarma que salta marca al intruso durante 4 s. Los valores están en `LanAbilities.cs`.

La defensa reduce el daño mediante `daño × 100 / (100 + defensa)`.
Correr multiplica la velocidad por 1,5 y consume estamina. El salto también consume.

## Escenas y edición

- `MenuInicio`: entrada al juego solo y a la sala LAN.
- `ValleDelRio`: experiencia individual, conservada.
- `NocheChapare`: mapa hexagonal hecho con las piezas de KayKit. Tiene radio 30
  (unas 120 × 105 unidades) y 3.367 hexágonos, con montañas como borde. Incluye río con
  3 puentes, campamento rojo, caminos, pista de tierra, selva (36 %) y 6 refugios entre los
  árboles. Se regenera con **Tools > Multijugador > Crear mapa del Chapare**, que usa
  `ChapareSetup.cs`. Las piezas son prefabs editables; las colisiones son cilindros y cajas
  simples dentro de `COLISIONES`.
- El río no se cruza caminando, solo por los puentes. Sus colisiones usan la capa *Water*,
  que la vista y las balas ignoran, así que se puede disparar a la otra orilla.
- `ValleMultijugador`: arena anterior. Ya no se carga ni entra en la exportación.
- `Assets/Resources/Multiplayer/LanAssets.asset`: modelos, sonidos y recursos UI.
- `LanNightRules.cs`: valores y reglas de la noche.
- `LanBots.cs`: comportamiento de los bots según su rol.
- `LanProtocol.cs`: clases y formato de mensajes, en versión 2. Las copias anteriores
  del juego no pueden conectarse.

El anfitrión simula los ocho personajes, colisiones, daño, inventario, reglas y bots.
Los clientes envían controles; no envían vida, munición o daño decidido por ellos.
Los sockets usan colas acotadas y mensajes con longitud explícita. La simulación y
las instantáneas se actualizan a 20 Hz. No se ha implementado predicción local ni
compensación de latencia; está pensado para una LAN de baja latencia.

Los bots ven hasta 14 unidades y con línea de visión. El líder va a un refugio y huye al
más lejano si ve enemigos. Los guardias se quedan cerca de él y atacan amenazas
cercanas. Los agentes recorren zonas de búsqueda, recuerdan al último enemigo visto y
esposan al líder reducido.

## Verificación

Validación del 2026-11-07, en el mapa hexagonal:
**152 comprobaciones aprobadas**, sin errores en consola.

| Archivo | Aprobadas | Qué cubre |
|---|---:|---|
| `LAN-night-check.txt` | 31 | Despliegue, oleadas, reducción, esposas, entrada tardía y los tres finales |
| `LAN-play-check.txt` | 32 | Anfitrión con siete clientes por sockets reales de loopback, combate y bot por desconexión |
| `LAN-client-check.txt` | 13 | Cliente Unity contra un servidor local de prueba |
| `LAN-bots-check.txt` | 8 | Seis noches completas solo con bots, con despliegue real, y entrada tardía |
| `Magic-play-check.txt` | 9 | Efectos visuales durante la partida |
| `LAN-vision-check.txt` | 38 | Luz, visión, linternas por red, items, bengalas, alarmas y qué se dibuja |
| `LAN-ability-check.txt` | 21 | Personajes por bando, cada habilidad medida, activación por red y uso por bots |

Comandos en **Tools > Multijugador**: *Comprobar reglas de la noche*, *Comprobar noches
con bots*, *Comprobar noche, vision e items*, *Comprobar habilidades* y *Comprobar LAN en Play*.

Con noche y habilidades, dos tandas de seis noches con bots terminaron así: 8 capturas,
2 amaneceres y 2 derrotas de la DEA por agotar refuerzos. Sin oscuridad la DEA ganaba 6 de 6.
No mide el equilibrio entre personas.

Mediciones de `LAN-ability-check.txt`: el escudo deja 2,3 de daño de frente frente a 11,4 sin
él, el líder protegido recibe 5,3 en vez de 13,3, y el Avance recorre 4 unidades.

La prueba acelerada avanza la física en cada paso. Sin eso, disparos y visión usan
posiciones antiguas y los resultados no son válidos.

Capturas del mapa: `NocheChapare-mapa.png`, `NocheChapare-campamento.png` y
`NocheChapare-puente.png`. `LAN-match.png` muestra una partida en el mapa hexagonal, y
`Noche-linterna.png` y `Noche-bengala.png` muestran la noche con linterna y faroles.
`Personajes-sala.png` y `Personajes-hud.png` muestran los personajes en la sala y la
habilidad en el HUD. Las
capturas `Noche-*.png` de la interfaz son del mapa gris anterior.

Escala del mapa: 5.156 piezas visibles, unos 1,26 millones de triángulos en total y 1.445
colisiones. La cámara solo ve una pequeña parte a la vez. El rendimiento en un ejecutable
todavía no se ha medido.

**Sin probar todavía:** teclado y ratón reales (mantener F y pulsar los botones de
entrada), y una red física entre varios ordenadores. Las pruebas llaman a las funciones
directamente y usan loopback.

Referencias técnicas: [sockets TCP de .NET](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/sockets/tcp-classes)
y [NavMesh Surface de Unity](https://docs.unity.cn/Packages/com.unity.ai.navigation%402.0/manual/NavMeshSurface.html).
