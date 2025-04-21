<div>
  
<div align="center">

# 🛩️ Unity Flight Simulator Multiplayer

Conviértete en piloto y compite o colabora en combates aéreos multijugador.  
Este proyecto es una conversión del simulador de vuelo original de **Vazgriz (YouTube)**, adaptado a **multijugador online con Photon Fusion 2**.</br>
[FlightSim por Vazgriz](https://github.com/vazgriz/FlightSim)

</div>

---

## 📌 Descripción del Proyecto

Este simulador fue originalmente desarrollado como un juego **Single Player** con físicas de vuelo realistas, disparo de cañones y misiles, sistema de daño, efectos visuales, y un completo **HUD** de vuelo.

El objetivo de este proyecto ha sido **convertirlo en un juego multijugador online** con sincronización completa de aviones, proyectiles y daño, usando **Photon Fusion 2** como motor de red.

Actualmente permite a dos jugadores:

- Spawnear en distintas bases.
- Controlar sus propios aviones en tiempo real.
- Disparar cañones.
- Ver el daño recibido en el HUD.
- Mostrar lock de misiles, flechas HUD y warnings visuales.

---

## 🏛 Estructura del Proyecto

### ✈️ Control de Vuelo
- `Plane.cs` → Lógica de físicas de vuelo, daño, armas y sincronización de red.
- `PlaneAnimation.cs` → Animaciones de alerones, flaps, misiles y postcombustión.
- `PlaneCamera.cs` → Cámara dinámica de seguimiento del avión.
- `PlaneInputHandler.cs` / `PlaneNetworkInput.cs` → Captura y envío de inputs multijugador.

### 🧠 HUD & Interfaz
- `PlaneHUD.cs` → Muestra datos en pantalla: altitud, velocidad, salud, misil lock, etc.
- `Compass.cs`, `PitchLadder.cs`, `Bar.cs` → Elementos del HUD personalizados.
- `Target.cs` → Gestiona posición y amenazas entrantes (misiles enemigos).

### 🔫 Armas y Combate
- `Bullet.cs` → Sincronización de balas y daño por Raycast.

### 🌐 Multijugador con Photon Fusion
- `PlayerSpawner.cs` → Generación de aviones según equipo (bases).
- `PlaneNetworkController.cs` → Control centralizado de entrada y HUD.
- `StateAuthorityRecovery.cs` → Sistema automático para recuperación de autoridad de red.

### 🛠️ Utilidades
- `Utilities.cs` → Funciones matemáticas para interceptaciones, físicas y escalados.

---

## 📸 Capturas de pantalla

<p align="center">
  <img src="docs/screenshots/screenshot_1.png" width="400" />
  <img src="docs/screenshots/screenshot_2.png" width="400" />
</p>

<div align="center">
  
---

### 🛠️ Tecnologías y Herramientas 🛠️

</br>

<img alt="github" src="https://user-images.githubusercontent.com/25181517/192108374-8da61ba1-99ec-41d7-80b8-fb2f7c0a4948.png" width="80"/>  
<img alt="unity" src="https://raw.githubusercontent.com/marwin1991/profile-technology-icons/refs/heads/main/icons/unity.png" width="80"/>
<img alt="visualstudio" src="https://images.icon-icons.com/112/PNG/512/visual_studio_18908.png" width="80"/>
<img alt="c#" src="https://raw.githubusercontent.com/marwin1991/profile-technology-icons/refs/heads/main/icons/c%23.png" width="80"/>
<br>

</div>

---

<table align="center">
  <tr>
    <td>
      <table align="center">
        <tr>
          <td align="center">
            <a href="https://github.com/LeanEmanuel">
              <img src="https://github.com/LeanEmanuel/Images/blob/main/Leandro.png" alt="Mini Leandro" width="80">
            </a>
          </td>
        </tr>
        <tr>
          <td>
            <a href="https://github.com/LeanEmanuel">
              <img src="https://img.shields.io/badge/LeanEmanuel-Git?style=flat&logo=github&logoColor=white&labelColor=black&color=50e520&label=GitHub" alt="Badge">
            </a>
          </td>
        </tr>
    </td>
  </tr>
</table>

</div>
