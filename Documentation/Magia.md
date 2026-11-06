# Efectos mágicos PixPlays

Se añadieron las variantes URP incluidas en los paquetes originales, conservando
los archivos Built-in y las fuentes del proveedor. Las variantes de uso en el
juego están en Assets/Resources/Effects y se asignan en MagicPalette.asset.

- Explorador: aura breve de viento al aparecer/reaparecer.
- Guardián: aura de tierra.
- Artillero: aura de fuego.
- Multijugador: escudo de agua mientras dura la protección de reaparición.
- Impactos confirmados de las armas: destello arcano pequeño.
- Modo individual: reaparición de viento e impactos; no muestra escudo porque no
  existe la mecánica de protección de reaparición en ese modo.

Los efectos son visuales: no cambian daño, munición, clases ni habilidades.
En LAN se reproducen a partir de la vida, protección e impactos del estado del
anfitrión. No crean proyectiles dañinos ni añaden tráfico por partícula.

Límite de 24 efectos reutilizables, hasta 128 partículas por sistema, sin luces,
sombras ni colisiones propias. Las emisiones terminan y se liberan para reutilizar.
No se modifican los materiales originales del paquete. Las variantes eliminan
los scripts de demostración del proveedor para evitar controladores duplicados.

La revisión gráfica y funcional está en Magic-play-check.txt y Magic-in-game.png.
Para regenerar las variantes: Tools > Efectos > Preparar magia PixPlays.
