# Multijugador local · Valle del Río

## Jugar

Abre `Assets/Scenes/MenuInicio.unity`, pulsa Play y elige **Multijugador LAN**.

1. Cada participante usa una copia del mismo juego en un ordenador de la misma red.
2. El anfitrión elige nombre, equipo y clase, y pulsa **Crear sala**.
3. Los demás escriben la IP mostrada en esa sala y pulsan **Unirse por IP**.
4. Los invitados pulsan **Estoy listo**. El anfitrión pulsa **Iniciar partida**.

No hace falta tener ocho personas: los espacios vacíos los ocupan bots.
Se puede entrar durante una partida, reemplazando un bot. Un jugador desconectado
se sustituye por un bot. Si sale el anfitrión, termina la conexión para todos;
no hay migración de anfitrión ni reconexión automática.

La red usa TCP 7777. Si el sistema pregunta, permite las conexiones de red local
del juego. Una red de invitados con aislamiento entre dispositivos puede impedir
la conexión aunque ambos estén conectados al mismo Wi-Fi. No requiere cuentas ni
servicios de Internet. La IP se escribe manualmente; no hay buscador de salas.

## Partida

- Azul contra rojo, cuatro puestos por equipo.
- Victoria por 20 bajas o al terminar cinco minutos; empate si los puntos coinciden.
- Sin daño entre compañeros. Los obstáculos bloquean disparos.
- Reaparición automática tras cuatro segundos y dos segundos de protección.
- Cada vida comienza con un rifle. Al morir o pulsar G, se suelta el arma conservando
  la munición; puede recogerse con F. Las armas del suelo flotan y giran.
- Esc muestra opciones; la partida online sigue avanzando.
- Al finalizar, el anfitrión puede volver a la sala y empezar otra ronda.

| Clase | Vida | Estamina | Defensa | Velocidad base | Cargador rifle | Reserva |
|---|---:|---:|---:|---:|---:|---:|
| Explorador | 90 | 130 | 10 | 4,2 | 24 | 90 |
| Guardián | 160 | 85 | 40 | 2,8 | 24 | 90 |
| Artillero | 110 | 100 | 15 | 3,3 | 40 | 210 |

La defensa reduce el daño mediante `daño × 100 / (100 + defensa)`.
Correr multiplica la velocidad por 1,5 y consume estamina. El salto también consume.

## Escenas y edición

- `MenuInicio`: entrada al juego solo y a la sala LAN.
- `ValleDelRio`: experiencia individual, conservada.
- `ValleMultijugador`: variante del valle para el combate LAN, con navegación
  horneada y ocho puntos de aparición en `PARTIDA - 4 contra 4`.
- `Assets/Resources/Multiplayer/LanAssets.asset`: modelos, sonidos y recursos UI.
- `LanProtocol.cs`: valores de clases y formato de mensajes.

El anfitrión simula los ocho personajes, colisiones, daño, inventario y bots.
Los clientes envían controles; no envían vida, munición o daño decidido por ellos.
Los sockets usan colas acotadas y mensajes con longitud explícita. La simulación y
las instantáneas se actualizan a 20 Hz. No se ha implementado predicción local ni
compensación de latencia; está pensado para una LAN de baja latencia.

Los bots buscan enemigos, calculan rutas por NavMesh, disparan si tienen visión,
recargan y reaparecen. No utilizan aprendizaje ni comportamiento táctico avanzado.

## Verificación

- `LAN-play-check.txt`: anfitrión Unity con siete clientes de protocolo por sockets
  reales de loopback; sala, límite de jugadores, preparación, controles, réplicas,
  combate, cobertura, armas, reaparición y bot por desconexión.
- `LAN-transport-check.txt`: fragmentación de cabecera/cuerpo, UTF-8, respuesta y
  rechazo de mensajes demasiado grandes y destinos externos.
- `LAN-client-check.txt`: cliente Unity contra un servidor local de prueba.

Estas pruebas no equivalen a una prueba física entre ocho ordenadores por Wi-Fi.
Para ejecutarlo sin el editor, compila las escenas habilitadas desde Build Profiles
para el sistema operativo de cada ordenador. No se incluye un ejecutable en esta entrega.

Referencias técnicas: [sockets TCP de .NET](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/sockets/tcp-classes)
y [NavMesh Surface de Unity](https://docs.unity.cn/Packages/com.unity.ai.navigation%402.0/manual/NavMeshSurface.html).

Validación final: **54 comprobaciones locales aprobadas**: 30 de anfitrión/combate,
13 del cliente Unity, 4 del transporte y 7 de bots/unión durante la partida.
`LAN-bots-check.txt` registra una simulación de 40 segundos con siete bots: rutas,
disparos, bajas, permanencia en terreno y sustitución al entrar/salir un invitado.
La imagen `LAN-bots.png` muestra esa prueba, sin modificar la escena guardada.
