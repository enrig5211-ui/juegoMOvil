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

- Sin daño entre compañeros. Los obstáculos bloquean disparos.
- Cada vida comienza con un rifle. Al caer o pulsar G, se suelta el arma conservando
  la munición; puede recogerse con F. Las armas del suelo flotan y giran.
- Esc muestra opciones; la partida online sigue avanzando.
- Al finalizar, el anfitrión puede volver a la sala y empezar otra ronda.

Todos estos valores están en `LanNightRules.cs`.

| Clase | Vida | Estamina | Defensa | Velocidad base | Cargador | Reserva |
|---|---:|---:|---:|---:|---:|---:|
| Explorador | 90 | 130 | 10 | 4,2 | 24 | 90 |
| Guardián | 160 | 85 | 40 | 2,8 | 24 | 90 |
| Artillero | 110 | 100 | 15 | 3,3 | 40 | 210 |
| Líder | 120 | 100 | 20 | 3,3 | 12 (pistola) | 60 |

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

Validación del 2026-11-07, en el mapa hexagonal y en el editor de Unity:
**93 comprobaciones aprobadas**, sin errores en consola.

| Archivo | Aprobadas | Qué cubre |
|---|---:|---|
| `LAN-night-check.txt` | 31 | Despliegue, oleadas, reducción, esposas, entrada tardía y los tres finales |
| `LAN-play-check.txt` | 32 | Anfitrión con siete clientes por sockets reales de loopback, combate y bot por desconexión |
| `LAN-client-check.txt` | 13 | Cliente Unity contra un servidor local de prueba |
| `LAN-bots-check.txt` | 8 | Seis noches completas solo con bots, con despliegue real, y entrada tardía |
| `Magic-play-check.txt` | 9 | Efectos visuales durante la partida |

Comandos en **Tools > Multijugador**: *Comprobar reglas de la noche*, *Comprobar noches
con bots* y *Comprobar LAN en Play*.

En el mapa hexagonal, las seis noches con bots terminaron en captura, entre los 45 y los
326 segundos de noche. Los bots de la DEA conocen todos los puntos de búsqueda y todavía es
de día, así que por ahora la DEA está favorecida. La oscuridad de la Etapa 2 debe
equilibrarlo; no mide el equilibrio entre personas.

La prueba acelerada avanza la física en cada paso. Sin eso, disparos y visión usan
posiciones antiguas y los resultados no son válidos.

Capturas del mapa: `NocheChapare-mapa.png`, `NocheChapare-campamento.png` y
`NocheChapare-puente.png`. `LAN-match.png` muestra una partida en el mapa hexagonal. Las
capturas `Noche-*.png` de la interfaz son del mapa gris anterior.

Escala del mapa: 5.156 piezas visibles, unos 1,26 millones de triángulos en total y 1.445
colisiones. La cámara solo ve una pequeña parte a la vez. El rendimiento en un ejecutable
todavía no se ha medido.

**Sin probar todavía:** teclado y ratón reales (mantener F y pulsar los botones de
entrada), y una red física entre varios ordenadores. Las pruebas llaman a las funciones
directamente y usan loopback.

Referencias técnicas: [sockets TCP de .NET](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/sockets/tcp-classes)
y [NavMesh Surface de Unity](https://docs.unity.cn/Packages/com.unity.ai.navigation%402.0/manual/NavMeshSurface.html).
