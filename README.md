# Restaurante Delicias

Sistema para manejar un restaurante: mesas, órdenes, cocina y cobro, con SQL Server. Hecho en C# con Windows Forms.

Varias cajas y meseros pueden trabajar al mismo tiempo. Todo se guarda en la base al momento, así cada compu ve lo mismo y nada se pierde si se va la luz o se cierra el programa.

## Qué hace

**Mesas**
- Mesas agrupadas por área: salón, terraza o las que se configuren.
- **Verde** si está libre, **roja** si está ocupada y **naranja** si tiene platillos que todavía no se mandaron a cocina.
- Cada mesa ocupada muestra lo consumido, las personas y cuánto tiempo lleva.
- Se refresca sola cada 4 segundos, así se ve lo que hacen las otras cajas.

**Orden**
- Menú por categorías con buscador. Lo que se acabó en el día sale como "agotado" y no se puede pedir.
- Se agrega con cantidad y nota ("sin cebolla", "bien cocido").
- **Enviar a cocina**: lo nuevo se marca como enviado. Si se cierra la mesa sin enviar, el sistema pregunta.
- Lo que no se ha enviado se cambia libremente. Lo que ya está en cocina se **anula con motivo**, y queda en la bitácora.
- Personas por mesa, propina opcional (10 % por defecto) y el IVA incluido desglosado.
- Si otro mesero agrega algo a la misma mesa desde otra compu, aparece solo.

**Cobro**
- Efectivo, tarjeta o transferencia.
- En efectivo calcula el vuelto, con botones rápidos de Q50, Q100, Q200 y exacto.
- En tarjeta o transferencia se anota la referencia.
- Número de ticket correlativo y ticket listo para impresora térmica de 80 mm.
- Si otra caja ya cobró la mesa, o alguien le agregó algo mientras se cobraba, no deja cobrar dos veces y avisa.

**Menú**
- Platillos con categoría, descripción y precio.
- "Hay hoy" para lo que se acabó en el día y "En el menú" para sacarlo sin perder el historial.
- Los cambios de precio quedan en la bitácora.

**Ventas del día**
- Cobros de cualquier fecha, con el total por forma de pago, el consumo, las propinas y los platillos anulados.
- Reimprimir un ticket y exportar a CSV para Excel.

## Instalación

### Lo que se necesita

- **SQL Server Express** 2016 o más nuevo (es gratis).
- **Visual Studio 2019 o 2022** con "Desarrollo de escritorio de .NET". Trae .NET Framework 4.8.
- Al compilar, Visual Studio baja **Dapper** solo por NuGet.

### Primera vez

1. Abre `RestauranteDelicias.sln` y dale F5.
2. Si no encuentra la base, sale la pantalla de **Conexión**. Pon el servidor (en la misma compu es `.\SQLEXPRESS`) y dale a **Crear base de datos**.
3. Se crean las tablas, un menú de ejemplo y 12 mesas en dos áreas. Dale a **Guardar y entrar**.

Si prefieres crear la base a mano, en SSMS corre en orden los scripts de [`BaseDatos/`](BaseDatos).

### Varias compus

La base vive en una sola compu (el servidor) y las demás se conectan a ella:

1. En el servidor, abre **SQL Server Configuration Manager** y activa **TCP/IP** en los protocolos de SQLEXPRESS. Reinicia el servicio.
2. Enciende el servicio **SQL Server Browser**.
3. En el firewall del servidor, permite el puerto **1433 TCP** y el **1434 UDP**.
4. En cada caja, en la pantalla de Conexión, pon `NOMBRE-DEL-SERVIDOR\SQLEXPRESS`.
   - Usa **Autenticación de Windows** si las compus están en un dominio.
   - Si no, crea en SQL Server un usuario con permisos sobre la base y usa **Usuario de SQL Server**.

La conexión de cada compu se guarda cifrada en su `AppData`, no en el código.

## Cómo está hecho

```
BaseDatos/                    scripts de SQL Server
RestauranteDelicias/
├── Datos/                    conexión y consultas con Dapper
├── Modelos/                  categorías, platillos, mesas, órdenes, pagos
├── Servicios/                ticket y registro de errores
└── Pantallas/                los formularios
```

### Base de datos

| Tabla | Para qué |
|---|---|
| `Categoria`, `Platillo` | El menú. Cada categoría dice si va a **cocina** o a la **barra** |
| `Area`, `Mesa` | El salón, con capacidad y posición de cada mesa |
| `Orden`, `OrdenDetalle` | Cada cuenta y lo que se pidió, con el estado en cocina y las anulaciones |
| `Pago` | Cómo se pagó. Una orden puede tener varios pagos |
| `Usuario`, `Turno` | Usuarios con rol y apertura o cierre de caja (fase 2) |
| `Bitacora` | Anulaciones, cambios de precio y quién lo hizo desde qué compu |
| `Configuracion` | Nombre, NIT, dirección, porcentajes de propina e IVA y el pie del ticket |

### Para que varios trabajen al mismo tiempo

- **Lecturas sin bloqueo** (`READ_COMMITTED_SNAPSHOT`). Refrescar las mesas en 10 compus no traba a quien está guardando.
- **Una sola orden abierta por mesa**, garantizado por un índice único. Si dos meseros abren la misma mesa al mismo tiempo, los dos terminan en la misma orden.
- **Cobro en una transacción**: el pago, el número de ticket y el cierre se guardan juntos, o no se guarda nada.
- **Control de versión** (`rowversion`) en la orden. Si alguien la cambia mientras otro cobra, el cobro se detiene y avisa.
- Si dos cajas chocan guardando al mismo tiempo (deadlock), se reintenta solo.
- Los precios se **copian** al pedir. Cambiar el menú no mueve las cuentas abiertas ni los reportes viejos.
- Los totales se **congelan** al cobrar.

### Errores

Todo error queda en `%LOCALAPPDATA%\RestauranteDelicias\logs\`, un archivo por día. Al usuario se le muestra un mensaje entendible y el programa no se cierra.

## Lo que viene

El plan por fases está en [ROADMAP.md](ROADMAP.md).

## Licencia

MIT. Ver [LICENSE](LICENSE).
