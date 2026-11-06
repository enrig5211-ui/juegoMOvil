# Exportar Valle del Río para Windows desde este Mac

En la instalación actual (Unity 6000.4.0f1) solo está presente MacStandaloneSupport.
No se ha generado todavía un archivo .exe.

1. Abre Unity Hub → Installs / Instalaciones → engranaje o menú de Unity 6000.4.0f1
   → Add modules / Añadir módulos → **Windows Build Support (Mono)** → Install.
2. Vuelve al proyecto. Puedes usar **Tools → Exportar → Windows x64**: configura
   Mono, incluye MenuInicio, ValleDelRio y ValleMultijugador y escribe
   `Builds/Windows/ValleDelRio.exe`.
3. Alternativa manual: **File → Build Profiles → Add Build Profile → Windows**,
   arquitectura **Intel 64-bit / x86_64**, activa el perfil y usa scripting backend
   **Mono** en Player Settings. MenuInicio debe ser la primera escena, seguida por
   ValleDelRio y ValleMultijugador. Pulsa **Build** y elige una carpeta vacía.
4. Comprime y comparte **toda la carpeta de salida**, no únicamente el .exe:
   necesita la carpeta `ValleDelRio_Data`, UnityPlayer.dll, MonoBleedingEdge y los
   demás archivos que Unity genere. En Windows se descomprime y se ejecuta el .exe.
5. Para LAN, todos deben usar la misma versión del juego. Permite al ejecutable
   conexiones en redes privadas cuando lo pida el firewall. Una persona crea la
   sala y las demás se unen usando su IP local (TCP 7777).

Desde este Mac se prepara el build Windows con Mono; no se ha validado la ejecución
real en un PC Windows. El .exe de Windows no se ejecuta nativamente en macOS.

Referencias oficiales:
- https://docs.unity.com/en-us/engine/6000.3/manual/building-and-publishing/build-settings/create-build-profile
- https://docs.unity.com/en-us/engine/6000.5/manual/platform-specific/windows/standalone-binaries
