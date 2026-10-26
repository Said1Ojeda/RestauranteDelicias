# Restaurante Delicias

Sistema para manejar un restaurante: mesas, órdenes, cocina y cobro, con SQL Server. Hecho en C# con Windows Forms.

Tiene una pantalla para cada quien: las mesas para meseros y cajeros, y una pantalla oscura para la cocina y la barra. Varias cajas y meseros pueden trabajar al mismo tiempo. Todo se guarda en la base al momento, así cada compu ve lo mismo y nada se pierde si se va la luz o se cierra el programa.

## Qué hace

**Diseño**
- Barra lateral con íconos, mesas y platillos en tarjetas, y tablas limpias sin cuadrícula.
- Los íconos salen de la letra de íconos que ya trae Windows 10 y 11, así no se carga ninguna imagen.
- Se ve nítido con el escalado de Windows al 125 % o 150 %, no borroso.
- Los botones y las tarjetas son grandes, se pueden usar en pantalla táctil.

**Usuarios y permisos**
- Se entra con usuario y contraseña, o con un **PIN** en un teclado numérico grande, pensado para la compu que comparten los meseros.
- Roles:

| Rol | Puede |
|---|---|
| Mesero | Tomar órdenes y mandarlas a cocina |
| Cajero | Lo del mesero, más cobrar, abrir y cerrar caja y ver las ventas |
| Admin | Todo, más el menú y lo que está en **Ajustes**: usuarios, bitácora, datos del restaurante y conexión |
| Cocina | Solo la pantalla de cocina |

- Contraseñas y PIN guardados con hash PBKDF2 y sal, nunca en texto. Después de 5 intentos fallidos hay que esperar un minuto.
- "Cambiar usuario" deja la compu en el login sin cerrar el programa.
- Si nadie toca la compu en 5 minutos, regresa sola al login. Así nadie cobra ni anula con el usuario de otro. Los minutos se cambian en Ajustes, y con 0 no se cierra nunca.
- La conexión a la base se configura **una sola vez por compu**. Después cada quien solo entra con su PIN o su clave, sin ver nada del servidor.

**Caja**
- Cada cajero abre su turno con un fondo inicial. Sin caja abierta no se puede cobrar.
- Al cerrar se cuenta el efectivo. El sistema dice cuánto debería haber, si sobra o falta, y lo separa por forma de pago.
- Imprime el corte de caja con espacio para firma.

**Autorizaciones**
- Los descuentos y las anulaciones de lo que ya está en cocina piden la autorización de un admin, con usuario y clave o con PIN.
- Si el que está usando la compu ya es admin, no le pregunta.
- La bitácora guarda quién lo pidió, quién lo autorizó y desde qué compu.

**Cocina**
- Pantalla oscura con un pedido por tarjeta. Cada tarjeta trae la mesa, el mesero, lo que hay que preparar con sus notas y cuánto tiempo lleva.
- Se filtra por **cocina**, **barra** o todo, según la categoría del platillo.
- La franja de arriba va de verde a naranja, y a rojo cuando pasa de 15 minutos.
- **Empezar** marca que ya se está preparando y **Listo** lo quita de la pantalla. **Deshacer** lo regresa por si se tocó mal.
- Lo que se anula después de mandarlo sale tachado en rojo, para que no se prepare.
- Suena un aviso cuando llega un pedido nuevo.
- En las mesas aparece "2 listos" para que el mesero sepa que ya puede llevarlo. En la orden lo marca con **Entregar listos**.

**Mesas**
- Mesas agrupadas por área: salón, terraza o las que se configuren.
- **Verde** si está libre, **roja** si está ocupada, **naranja** si tiene platillos que todavía no se mandaron a cocina y **azul** si cocina ya tiene algo listo.
- Cada mesa ocupada muestra lo consumido, las personas y cuánto tiempo lleva.
- Se refresca sola cada 4 segundos, así se ve lo que hacen las otras cajas.
- **Mover o juntar**: desde la orden se pasa la cuenta a una mesa libre, o se junta con una ocupada. Los platillos, las personas y lo que ya pagaron se van juntos.

**Para llevar y a domicilio**
- El botón **Para llevar** de arriba abre un pedido con el nombre del cliente, el teléfono y, si es a domicilio, la dirección.
- Los pedidos abiertos salen arriba de las mesas, cada uno en su tarjeta, y en cocina con el nombre del cliente.
- El ticket lleva los datos del cliente.

**Orden**
- Menú en tarjetas con el color de su categoría, filtros por categoría y buscador. Lo que se acabó en el día sale como "agotado" y no se puede pedir.
- Se agrega con un toque, con cantidad y nota ("sin cebolla", "bien cocido"). En el buscador, Enter agrega el primero que sale.
- **Enviar a cocina**: lo nuevo se marca como enviado. Si se cierra la mesa sin enviar, el sistema pregunta.
- Lo que no se ha enviado se cambia libremente. Lo que ya está en cocina se **anula con motivo**, y queda en la bitácora.
- Personas por mesa, propina opcional (10 % por defecto) y el IVA incluido desglosado.
- Descuento en monto (25) o en porcentaje (10%), con motivo.
- Cada orden guarda qué mesero la atendió.
- Si otro mesero agrega algo a la misma mesa desde otra compu, aparece solo.

**Cobro**
- Se cobra **todo**, en **partes iguales**, **por platillos** u **otro monto**. Cada pago puede ser en una forma distinta, por ejemplo mitad en efectivo y mitad con tarjeta.
- Por platillos se escoge cuántos de cada uno paga cada quien, y a cada parte le toca su proporción de la propina y del descuento.
- Cada pago parcial imprime su comprobante con el saldo que queda. El último pago cierra la cuenta y saca el ticket con número, con todos los pagos.
- Lo que ya se pagó no se puede anular, y ningún cambio puede dejar la cuenta en menos de lo que ya pagaron.
- Efectivo, tarjeta o transferencia.
- En efectivo calcula el vuelto, con botones rápidos de Q50, Q100, Q200 y exacto.
- En tarjeta o transferencia se anota la referencia.
- Número de ticket correlativo. El ticket se ve antes de imprimir tal como va a salir, listo para impresora térmica de 80 mm.
- Si otra caja ya cobró la mesa, o alguien le agregó algo mientras se cobraba, no deja cobrar dos veces y avisa.

**Menú**
- Platillos con categoría, descripción y precio.
- "Hay hoy" para lo que se acabó en el día y "En el menú" para sacarlo sin perder el historial.
- Los cambios de precio quedan en la bitácora.

**Ventas del día**
- Cobros de cualquier fecha, con el mesero, quién cobró, el total por forma de pago, el consumo, las propinas y los platillos anulados.
- Reimprimir un ticket y exportar a CSV para Excel.

**Atajos de teclado**

| Tecla | En la orden |
|---|---|
| F2 | Enviar a cocina |
| F3 o Ctrl+F | Buscar platillo |
| F4 | Descuento |
| F9 | Cobrar |
| + y - | Sumar o quitar uno del platillo marcado |
| Supr | Quitar el platillo marcado |
| Esc | Limpiar la búsqueda o volver a las mesas |

En las mesas, F5 refresca.

**Respaldo**
- En **Ajustes → Respaldo de la base** se hace un respaldo completo con un clic. SQL Server Express no los hace solo.
- Si pasa más de un día sin respaldo, al admin le sale el aviso arriba en las mesas.
- El archivo `.bak` queda en la compu del servidor. Hay que copiarlo de vez en cuando a una USB o a la nube.

## Instalación

### Lo que se necesita

- **SQL Server Express** 2016 o más nuevo (es gratis).
- **Visual Studio 2019 o 2022** con "Desarrollo de escritorio de .NET". Trae .NET Framework 4.8.
- Al compilar, Visual Studio baja **Dapper** solo por NuGet.

### Primera vez

1. Abre `RestauranteDelicias.sln` y dale F5.
2. Si no encuentra la base, sale la pantalla de **Conexión**. Pon el servidor (en la misma compu es `.\SQLEXPRESS`) y dale a **Crear base de datos**.
3. Se crean las tablas, un menú de ejemplo y 12 mesas en dos áreas. Dale a **Guardar y entrar**.
4. Como no hay usuarios todavía, te pide crear el **administrador**. Con él entras y creas a los demás desde **Ajustes → Usuarios**.

### Actualizar a una versión nueva

Si la base es de una versión anterior, al abrir el programa avisa y la actualiza sola. Solo agrega tablas y columnas, nunca borra datos. La versión de la base queda guardada en la tabla `Configuracion`.

Si prefieres crear la base a mano, en SSMS corre en orden los scripts de [`BaseDatos/`](BaseDatos).

### Varias compus

La base vive en una sola compu (el servidor) y las demás se conectan a ella:

1. En el servidor, abre **SQL Server Configuration Manager** y activa **TCP/IP** en los protocolos de SQLEXPRESS. Reinicia el servicio.
2. Enciende el servicio **SQL Server Browser**.
3. En el firewall del servidor, permite el puerto **1433 TCP** y el **1434 UDP**.
4. En cada caja, la primera vez que se abre el programa sale la pantalla de Conexión. Ahí pon `NOMBRE-DEL-SERVIDOR\SQLEXPRESS`.
   - Usa **Autenticación de Windows** si las compus están en un dominio.
   - Si no, crea en SQL Server un usuario con permisos sobre la base y usa **Usuario de SQL Server**.

La conexión de cada compu se guarda cifrada en su `AppData`, no en el código. Si después cambia el servidor, el admin la cambia en **Ajustes → Conexión a la base de datos**.

## Cómo está hecho

```
BaseDatos/                    scripts de SQL Server
RestauranteDelicias/
├── Datos/                    conexión y consultas con Dapper
├── Modelos/                  categorías, platillos, mesas, órdenes, pagos, comandas
├── Servicios/                ticket, sesión y registro de errores
└── Pantallas/                las pantallas, los botones y tarjetas del diseño
```

### Base de datos

| Tabla | Para qué |
|---|---|
| `Categoria`, `Platillo` | El menú. Cada categoría dice si va a **cocina** o a la **barra** |
| `Area`, `Mesa` | El salón, con capacidad y posición de cada mesa |
| `Orden`, `OrdenDetalle` | Cada cuenta y lo que se pidió, con el estado en cocina y las anulaciones |
| `Pago` | Cómo se pagó. Una orden puede tener varios pagos |
| `Usuario`, `Turno` | Usuarios con rol, contraseña y PIN cifrados, y los turnos de caja con su cuadre |
| `Bitacora` | Anulaciones, cambios de precio y quién lo hizo desde qué compu |
| `Configuracion` | Nombre, NIT, dirección, porcentajes de propina e IVA y el pie del ticket |

### Para que varios trabajen al mismo tiempo

- **Lecturas sin bloqueo** (`READ_COMMITTED_SNAPSHOT`). Refrescar las mesas en 10 compus no traba a quien está guardando.
- **Una sola orden abierta por mesa**, garantizado por un índice único. Si dos meseros abren la misma mesa al mismo tiempo, los dos terminan en la misma orden.
- **Cobro en una transacción**: el pago, el número de ticket y el cierre se guardan juntos, o no se guarda nada.
- **Control de versión** (`rowversion`) en la orden, y el total se compara otra vez al cobrar. Si alguien la cambia mientras otro cobra, el cobro se detiene y avisa.
- **Candado de la orden**: agregar, quitar, anular o descontar toman el mismo candado que el cobro. Un platillo nunca se cuela en una cuenta que se está cobrando en otra caja.
- **Candado de la caja**: no se puede cerrar una caja mientras se está cobrando en ella, así el corte siempre cuadra.
- Si dos cajas chocan guardando al mismo tiempo (deadlock), se reintenta solo.
- Los precios se **copian** al pedir. Cambiar el menú no mueve las cuentas abiertas ni los reportes viejos.
- Los totales se **congelan** al cobrar.

### Seguridad

- Contraseñas y PIN con PBKDF2 y sal. Los PIN se revisan en paralelo, así entrar es rápido aunque haya muchos usuarios.
- Los intentos fallidos se guardan en la base por compu. Después de 5 hay que esperar un minuto, y cada fallo de más dobla la espera, hasta 15 minutos. Cerrar el programa no lo reinicia.
- Los permisos se revisan dos veces: la pantalla esconde lo que no te toca y la capa de datos lo vuelve a revisar antes de guardar.
- Quien autoriza un descuento o una anulación se valida contra la base, tiene que ser un admin activo.
- Al exportar a CSV, un texto que empieza con `=`, `+`, `-` o `@` se guarda como texto, para que Excel no lo corra como fórmula.
- Todas las consultas van con parámetros.

**Recomendado en producción:** las cajas no deberían entrar a SQL Server con un usuario administrador. Crea un usuario solo para el programa:

```sql
CREATE LOGIN restaurante_app WITH PASSWORD = 'una-clave-larga-y-unica';
USE RestauranteDelicias;
CREATE USER restaurante_app FOR LOGIN restaurante_app;
ALTER ROLE db_datareader ADD MEMBER restaurante_app;
ALTER ROLE db_datawriter ADD MEMBER restaurante_app;
GRANT UPDATE ON dbo.SecTicket TO restaurante_app;
```

Las actualizaciones de la base y los respaldos se hacen desde la compu del admin, con un usuario que sí tenga permisos.

### Errores

Todo error queda en `%LOCALAPPDATA%\RestauranteDelicias\logs\`, un archivo por día. Al usuario se le muestra un mensaje entendible y el programa no se cierra.

## Lo que viene

El plan por fases está en [ROADMAP.md](ROADMAP.md).

## Licencia

MIT. Ver [LICENSE](LICENSE).
