# Valle del Río

Abre `Assets/Scenes/ValleDelRio.unity`. Los 201 prefabs están guardados en la escena y se pueden editar sin ejecutar el juego.

La raíz `VALLE DEL RIO` contiene bases, río, bosques, montañas, rocas y vegetación, y puente. Hay 91 celdas de terreno, de las que 13 son río.

La rejilla usa coordenadas axiales: X = 2q + r, Z = √3 r. La superficie está en Y = 0; el agua, en Y = -0.1. Los giros de los tramos de río son múltiplos de 60°. No cambies la escala de una base individual si quieres mantener las uniones.

La escena original se conserva en SampleScene; AntesDelValle guarda una copia del estado previo. La cámara de ValleDelRio está preparada para mostrar el escenario completo en Game.

Tools > Escenario > Crear Valle del Rio permite crear el mismo diseño en otra escena. El comando no duplica el valle si ya existe la raíz.

Validación: 91 posiciones distintas y las 12 uniones consecutivas del río verificadas contra los vértices de agua de los modelos. Vista revisada en Unity.

## Ampliación con lago

La misma escena ahora contiene 271 hexágonos (antes 91), 516 renderizadores y 105.851 triángulos de geometría en total. El valle central se conserva y el río desemboca en un lago con 7 piezas de agua y 11 de costa. Los grupos 07 y 08 contienen el lago y su decoración.

Se marcó la geometría inmóvil para static batching y se desactivaron las sombras de 103 detalles pequeños. Árboles, montañas y puente conservan sus sombras. Se mantienen mallas y materiales compartidos y el SRP Batcher existente; no se añaden scripts de actualización por fotograma.

Si conviertes un objeto del escenario en un elemento móvil, quita su marca Batching Static. La copia previa a esta ampliación está en Assets/Scenes/ValleDelRio_AntesDeAmpliar.unity.

Las 271 bases planas tampoco emiten sombras; siguen recibiéndolas. Esto conserva las sombras visibles de árboles, montañas y puente. La verificación en Play confirmó que los 516 renderizadores participan en static batching. Las cifras de UnityStats incluyen el trabajo del Editor y no representan FPS garantizados en un ejecutable o en otro dispositivo.

En dos muestras de Play a 807×596, las llamadas de dibujo reportadas por el Editor bajaron de 1275 a 1122 al desactivar las sombras de bases planas (aproximadamente 12%). No se midió una build independiente ni se estableció una garantía de FPS.

## Base rústica Yellow

Mapa ampliado a 331 hexágonos, con un poblado de 13 edificios yellow al sur del río. Incluye casas, ayuntamiento, taberna, mercado, pozo, taller, aserradero, establos, molino, herrería y torre de vigilancia. El grupo `09 - Base rustica Yellow` organiza edificios, senderos, accesorios y árboles.

Se añadieron barriles, cajas, leña, heno, un abrevadero y cercas de madera. El camino de tierra conduce al puente. Todos los objetos nuevos conservan sus vínculos a prefabs y la marca para agrupación estática; las bases y detalles pequeños no emiten sombras. La escena anterior está guardada como `ValleDelRio_AntesDeBase.unity`.

## Caminos del paquete

Se eliminaron los building_dirt de la base. El acceso utiliza tiles/roads en sustitución de las bases de césped, sin apilar dos bases. Incluye curvas, una bifurcación y un cruce de río bajo el puente, con giro del puente alineado al paso. Los caminos están en `09 - Base rustica Yellow / 03 - Caminos hexagonales`. Se conserva la escena previa como `ValleDelRio_AntesDeCaminos.unity`.
