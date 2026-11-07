> **Estilo actual:** el menú usa el estilo *Noche en el Chapare* (fuente en negrita, botones
> planos y fondo nocturno), aplicado con Tools > Interfaz > Aplicar estilo Noche en el Chapare.
> Volver a ejecutar *Crear menu y HUD* recuperaría el estilo de fantasía anterior.

# Interfaz del valle

Inicio: Assets/Scenes/MenuInicio.unity. Primera escena en Build Settings.
Jugar carga ValleDelRio desde cero; no hay guardado de partida todavía.
Controles abre una guía breve. Salir cierra el juego (detiene Play en Editor).

El HUD usa Canvas, Text e Image con referencias editables en `UI - Valle`.
Vida y estamina arriba a la izquierda, defensa debajo, munición abajo a la derecha
solo al equipar un arma. F aparece únicamente cerca de un objeto recogible.
Esc abre la pausa, desde donde se consultan las teclas o se vuelve al inicio.

Se usan la fuente Kurale y el botón Fantasy RPG del paquete Artsystack.
La imagen de inicio es una captura renderizada del propio valle, sin cargar otro mapa
completo durante el menú. Moonspire mantiene su volumen suave.

Respaldo previo: ValleDelRio_AntesDeInterfaz.unity.
Herramienta de autoría: Tools > Interfaz > Crear menu y HUD.

Validación: 15 comprobaciones en Play aprobadas (UI-play-check.txt): botones,
transiciones, pausa, controles, salud/estamina, muerte/reaparición y un solo listener.
Capturas revisadas: MenuInicio.png y HUD.png.
Para repetir: Tools > Interfaz > Comprobar en Play.
