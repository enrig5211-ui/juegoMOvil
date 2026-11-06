# Armas del valle

En ValleDelRio: F recoge el arma cercana o intercambia la equipada; G la suelta; clic izquierdo dispara hacia el cursor; R recarga mientras estás vivo y sigue sirviendo para reaparecer cuando mueres. El rifle admite mantener pulsado el clic; la pistola dispara una vez por pulsación. Hay dos armas junto al punto inicial y dos junto al camino de la base.

Las armas del suelo flotan y giran con un aro cian. Conservan cargador y reserva al recoger, intercambiar, soltar o morir. No se recogen automáticamente. La recogida exige cercanía y visión sin obstáculos; la recarga se cancela al soltar o morir. La muerte notifica al sistema de armas antes de reaparecer.

Pistola: 12 balas, reserva inicial 60, daño 24. Rifle: 30 balas, reserva 120, daño 14. Los valores, sonidos y tiempos se ajustan en WeaponPickup. No hay munición infinita ni reposición automática de reserva.

Los disparos usan rayos bloqueados por la geometría del escenario, con un trazo visible. Aplica daño a PlayerStats o a las dos dianas de práctica, que se recuperan después de tres segundos al llegar a cero. No hay enemigos con IA todavía. Los sonidos de disparo, equipamiento y recarga proceden de FreeWeaponSFX_v1.1, con bancos separados de Handgun y AssaultRifle. Se conserva Moonspire y el sistema de pasos.

Objetos de escena: ARMAS - Recogibles, Diana de practica 1/2. En player: PlayerWeapon, Arma - soporte, Audio - Armas y Disparo - trazo. Armas guardadas como instancias editables de los prefabs del paquete.

Copia anterior: Assets/Scenes/ValleDelRio_AntesDeArmas.unity.

## Validación

24 comprobaciones en Play completadas, incluyendo recogida por F, caída por G, intercambio sin duplicación, conservación de munición, cancelación de recarga, caída por muerte, animación flotante, disparos, daño a dianas y bloqueo por obstáculos. La prueba de puntería usa coordenadas de pantalla explícitas para no depender de movimientos del ratón físico. Repetir desde Tools > Player > Comprobar armas en Play, con ValleDelRio abierta; el resultado queda en Weapons-play-check.txt. Las dianas y el obstáculo de prueba se mueven/crean solo durante Play, sin guardar sus cambios de prueba en la escena.
