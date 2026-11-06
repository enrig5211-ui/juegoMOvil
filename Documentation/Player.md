# Player del valle

Abre Assets/Scenes/ValleDelRio.unity y pulsa Play. Haz clic en Game para dar foco al teclado.

- WASD o flechas: movimiento relativo a la cámara.
- Shift izquierdo: correr.
- Espacio: saltar (incluye una breve anticipación).
- Q / E: girar la cámara, inclinada 45 grados.
- R: reaparecer después de morir; al caer fuera del mapa reaparece automáticamente.

El componente PlayerStats permite ajustar vida máxima (100), estamina máxima (100), defensa (15) y ataque (10). Correr consume 18 estamina/segundo; saltar consume 12. La recuperación comienza tras 0,8 segundos sin gasto y recupera 20/segundo. La defensa usa daño efectivo = daño bruto × 100 / (100 + defensa). Ataque es un dato inicial: todavía no se ha implementado combate ni enemigos.

PlayerMotor controla el CharacterController; el hijo Visual conserva el prefab y recibe balanceo, rebote, compresión y estiramiento procedural. No se necesita un esqueleto ni clips externos. Las deformaciones no alteran el collider. PlayerCamera sigue al personaje y PlayerHUD muestra las estadísticas.

Se agregaron colliders estáticos para terreno, puente, edificios y obstáculos. Por ahora se puede caminar sobre la superficie del agua: no hay natación. La cámara aún no incluye ocultación automática de obstáculos.

En Play, el menú contextual del componente PlayerStats permite probar 25 de daño. Tools > Player > Comprobar en Play ejecuta las comprobaciones automatizadas y sale de Play al finalizar; el resultado se guarda en Documentation/Player-play-check.txt. Ejecutar con la escena del valle abierta.

Referencias de estilo: https://www.animationmentor.com/blog/anticipation-the-12-basic-principles-of-animation/ y https://www.animationmentor.com/blog/tutorial-12-principles-of-animation/ (anticipación, squash/stretch, exageración). Implementación procedural original, sin descargar animaciones.

Copia del estado previo: Assets/Scenes/ValleDelRio_AntesDelPlayer.unity.

## Audio

`PlayerFootsteps` escucha los contactos de la animación del PlayerMotor: no reproduce pasos quieto ni en el aire, y añade un golpe suave al aterrizar. Usa ocho clips por superficie de SingleSteps (césped, tierra, concreto para piedra y agua); alterna variantes sin repetir inmediatamente y varía ligeramente el tono. Los caminos usan tierra y el puente usa piedra. La selección se hace mediante la malla bajo los pies.

En el componente PlayerFootsteps, Volume controla los pasos (0,32). El AudioSource hijo `Audio - Pasos` usa reproducción 2D para que la cámara elevada no reduzca la audibilidad del jugador.

`AMBIENTE - Moonspire` reproduce `Loops/1. Moonspire.wav`, en bucle, a volumen 0,12 y con entrada suave de tres segundos. Ajusta Volume y Fade Seconds en AmbientMusic. La pista usa streaming Vorbis para no cargar sus tres minutos completos como audio descomprimido en memoria.

Copia previa al audio: `Assets/Scenes/ValleDelRio_AntesDeAudio.unity`.
