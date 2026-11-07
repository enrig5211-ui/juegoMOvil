# Plan del juego · Noche en el Chapare

Nombre de trabajo, pendiente de confirmar. Plan armado el 2026-11-06.

**Estado (2026-11-07):** etapas 0, 1 y 2 construidas, pendientes de prueba con personas.
El mapa ya es hexagonal (ver "Cambio de mapa").
La rama de trabajo es `etapa-1-reglas`.

## El juego

Dos bandos en red local, y cada partida es una noche. La DEA debe capturar al líder
de la organización antes del amanecer. La organización debe mantenerlo libre hasta
que amanezca. Se conserva la cámara 3D desde arriba.

## Decisiones confirmadas

- **Líder:** lo controla un jugador; si nadie lo toma, un bot.
- **Captura:** sin vida queda reducido, no muerto. La DEA lo esposa manteniendo una
  tecla unos segundos, y los guardias pueden interrumpirla.
- **Bajas:** refuerzos por oleadas y en número limitado.
- **Escenario:** selva nocturna con campamentos y una pista clandestina.
- **Selección:** pantalla animada estilo arcade. Metal Slug es solo referencia de
  estilo; los personajes y el arte son propios.

## Lo que encontré en el proyecto

1. **El mapa es demasiado pequeño para esconderse.** Mide unas 40 × 35 unidades y se
   cruza en 6 a 14 segundos. Los equipos aparecen a 16 unidades y el rifle alcanza 24.
   Por eso la Etapa 1 necesita un mapa provisional mucho mayor.
2. **Las reglas están repartidas.** La victoria está en `LanSession`, los equipos se
   deducen con `Id/4` en `LanPawn`, y los bots solo persiguen al enemigo más cercano.
3. **La noche no oculta nada todavía.** Nombres y anillos de los rivales siempre se
   ven, y los bots conocen la posición de todos. Hay que limitar la visión de ambos.
4. **WeatherMaker aporta poco aquí.** La versión instalada admite URP 17.4, pero su
   niebla y sombras de nubes no funcionan con cámara ortográfica, y desde esta cámara
   no se ve el cielo. Conviene un script propio de luz.
5. **El amanecer ya queda sincronizado.** `Remaining` viaja en cada instantánea, así
   que cada equipo puede calcular la luz sin tráfico nuevo. El perfil PC usa Forward+,
   apto para muchas luces. El perfil Mobile queda limitado a 4 por objeto.
6. **Entrar a mitad de partida revive.** `HandleClient` llama a `World.Respawn` al
   reemplazar un bot. Con estas reglas revivería a un líder reducido.
7. **No hay control de versiones.** Las copias son escenas `_Antes...`. `Assets` pesa
   4,6 GB, y `Artsystack` ocupa 2,6 GB de ese total.
8. **No hay modelos humanos con esqueleto.** Las piezas actuales no sirven para la
   selección animada.

## Reglas iniciales

Todos los valores se ajustarán al probar.

| Regla | Valor inicial |
|---|---|
| Equipos | 4 agentes contra 3 guardias y el líder |
| Despliegue | 90 s, sin disparos ni daño |
| Noche | 8 min |
| Líder reducido | 20 s sin moverse ni disparar; si nadie lo esposa, vuelve con 40 % de vida |
| Esposar | Mantener F 5 s a 1,5 unidades o menos; recibir daño o alejarse lo cancela |
| Oleadas | Cada 30 s |
| Refuerzos | DEA 12, organización 8; el líder no usa refuerzos |
| Fin anticipado | La organización gana si la DEA se queda sin agentes vivos ni refuerzos |
| Sin refuerzos en la organización | Los guardias caídos no vuelven; la partida sigue |

**Despliegue propuesto, distinto para cada bando:**

- **Organización:** está en su terreno. Camina libre, recoge items y decide dónde
  esconder al líder.
- **DEA:** espera fuera del mapa. Cada agente elige uno de 3 o 4 puntos de entrada
  (río, camino, pista, selva) y ve la elección de sus compañeros. Aparece allí al
  empezar la noche.

## Etapas

### Etapa 0 · Proteger el proyecto

- Crear un repositorio git con el trabajo propio: scripts, escenas, ajustes y
  documentación.
- Excluir `Library`, `Temp`, `Logs`, `UserSettings`, `juegoChapare`, los `.zip` y los
  paquetes de terceros grandes.

**Terminada cuando:** se puede volver a cualquier punto anterior.

### Etapa 1 · Reglas jugables con las fichas actuales

- Bandos DEA y organización, con opción "Ser el líder" en la sala.
- Despliegue como sub-estado de `match`, para no tocar las 14 referencias a esa fase.
- Reglas nuevas en `LanNightRules.cs`, en vez de crecer los archivos actuales.
- Protocolo versión 2: tecla mantenida, líder, punto de entrada, refuerzos y progreso
  de esposas. La versión nueva rechaza a clientes antiguos.
- Reducción, esposas, oleadas y fin de partida. Corregir el revivir del hallazgo 6.
- Bots por rol: guardias cerca del líder, líder que se esconde y huye, DEA que busca
  por zonas y sabe esposar.
- Mapa gris de unas 120 × 100 unidades con `ChapareSetup.cs`: bloques de selva,
  coberturas, navegación y puntos de entrada. Escena nueva `NocheChapare.unity`.
- HUD: "Amanece en…", refuerzos y progreso de esposas. Resultado propio de cada final.
- Actualizar `LanPlayCheck`, `LanBotCheck` y `LanClientCheck`.

**Terminada cuando:** una partida solo con bots termina tanto por captura como por
amanecer, un bot reemplaza a un líder desconectado, entrar a mitad de partida no
revive a nadie y las comprobaciones pasan.

Resultado: 93 comprobaciones aprobadas. Detalle en `MultijugadorLAN.md`.

Ajustes respecto a lo previsto:

- Un jugador humano puede dejar el rol de líder antes de empezar, y el mapa táctico de la
  DEA es la propia escena vista desde arriba, sin los rivales.
- El agente que esposa no puede disparar.
- El líder usa pistola desde esta etapa.

Para ajustar en la Etapa 2: los bots terminan la mayoría de las noches entre el minuto 3 y
el 6, sobre todo porque la DEA agota sus refuerzos. Falta ver si pasa igual con personas y
si 8 minutos es la duración correcta.

### Cambio de mapa (2026-11-07)

A pedido del usuario, el mapa gris se reemplazó por un mapa hexagonal amplio hecho con las
piezas de KayKit. Conserva el trazado probado: río al oeste con puentes, campamento
central, caminos, pista al este, selva y refugios. Adelanta parte de la Etapa 5, aunque el
estilo sigue siendo el medieval del paquete.

### Etapa 2 · Noche, visión e items

- `NightCycle`: luna, ambiente oscuro y amanecer gradual según `Remaining`.
- Linterna para cada jugador. Ayuda a ver, pero delata a quien la lleva. Solo la
  propia proyecta sombras, para cuidar el rendimiento.
- Rivales, nombres y anillos ocultos fuera de la visión. Visión limitada en los bots.
- Generalizar `LanDrop` para que no sean solo armas. Un espacio de item y tecla C.
- Items: botiquín, munición, visor nocturno, bengala y alarma.

**Terminada cuando:** al menos 3 personas juegan por LAN real y responden si la
partida es divertida. **Es el punto de decisión antes de invertir en arte.**

**Estado (2026-11-07):** construida y verificada en el editor (131 comprobaciones). Falta
la prueba con al menos 3 personas por LAN real, que es el punto de decisión. Con bots, la
oscuridad bajó las victorias de la DEA de 6 a 4 de cada 6 noches.

### Etapa 3 · Personajes con habilidades

- Sistema genérico de habilidad en el anfitrión: efecto, duración y recarga. Se activa
  con clic derecho, porque Q y E ya giran la cámara.
- Reutilizar las estadísticas de las 3 clases actuales.
- DEA: Rastreador (marca enemigos cercanos), Pesado (escudo frontal) y Asaltante
  (avance rápido).
- Organización: Vigía (coloca alarmas), Guardaespaldas (protege al líder cercano) y
  Pistolero (ráfaga).
- Líder: personaje único, con pistola y una habilidad para adelantar una oleada.

**Terminada cuando:** cada habilidad funciona en red, también usada por bots, y ninguna
domina la partida.

### Etapa 4 · Modelos animados y selección arcade

- Elegir e importar personajes con esqueleto.
- Animaciones de reposo, correr, disparar, caer y celebrar, derivadas del estado de red.
- Fase `select` entre sala y partida: retratos, cursores de todos en vivo, pose al
  confirmar, destello, sonido y cuenta atrás. Los bots eligen solos.
- Probar el aspecto pixelado renderizando el 3D a baja resolución.

**Terminada cuando:** los 8 puestos eligen a tiempo y cada jugador aparece con su
personaje animado en partida.

Va aquí porque esta pantalla depende de los modelos. Si se adelanta, usaría imágenes
provisionales que luego habría que rehacer.

### Etapa 5 · Vestir el Chapare

- Sustituir los bloques grises por selva, río, campamentos y pista, conservando el
  trazado ya probado.
- Menús con identidad propia, en lugar de la interfaz de fantasía.
- Sonido nocturno, radio, helicóptero y música nueva.
- Reemplazar los efectos de magia por disparos, polvo y chispas.

**Terminada cuando:** ya no queda nada medieval ni mágico en la partida.

### Etapa 6 · Cierre

- Ajustar valores con partidas reales.
- Retirar paquetes y escenas que ya no se usen, con revisión previa.
- Exportar y comprobar en Windows y Mac. La documentación de exportación está
  desactualizada: el módulo de Windows ya está instalado.
- Probar con los 8 equipos reales. Esa prueba física nunca se ha hecho.

**Terminada cuando:** una partida completa de 8 personas funciona en una red real.

## Pendientes

- **Verificación:** Unity está abierto sobre este proyecto, y eso impide ejecutarlo en
  segundo plano. Falta comprobar si las pruebas corren sin abrirlo.
- **Personajes (antes de la Etapa 4):** [Quaternius Universal Base Characters](https://quaternius.com/packs/universalbasecharacters.html)
  es CC0, trae 26 modelos animados y FBX, pero no son militares.
  [Low Poly Combat Character Pack](https://superhivemarket.com/products/low-poly-combat-character-pack)
  trae militares, policías y enemigos; falta revisar precio y compatibilidad.
- **Selva (antes de la Etapa 5):** no apareció un paquete gratuito y CC0 claro.
  [Low Poly Aztec Jungle URP](https://rg-poly.itch.io/low-poly-aztec-jungle-urp) es CC0
  desde 25 USD y [Tropical Island Lite](https://justcreate3d.itch.io/low-poly-tropical-island-lite)
  es gratuito con 12 piezas. Revisar la licencia de cada uno antes de usarlo.
- **Nombre:** definir el nombre final del juego.
