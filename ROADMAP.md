# Roadmap

El sistema se va armando por fases. Cada una se puede usar sola, sin esperar a las siguientes.

## Fase 1: base de datos y cimientos ✅

- [x] Base en SQL Server con scripts y un asistente que la crea desde el programa
- [x] Conexión configurable por compu, guardada cifrada
- [x] Consultas con Dapper, siempre con parámetros
- [x] Órdenes guardadas al momento: no se pierden si se cierra el programa
- [x] Varias cajas al mismo tiempo: una orden por mesa, cobro en transacción, control de versión y reintento en deadlocks
- [x] Mesas por área que se refrescan solas
- [x] Enviar a cocina y anular con motivo lo que ya se envió
- [x] Cobro en efectivo, tarjeta o transferencia, con número de ticket correlativo
- [x] Menú con disponibilidad del día y bitácora de cambios de precio
- [x] Ventas del día con totales por forma de pago, reimpresión y CSV
- [x] Registro de errores en archivo

## Fase 2: usuarios y caja ✅

- [x] Login con roles: **Admin**, **Cajero**, **Mesero** y **Cocina**
- [x] Contraseñas y PIN con hash y sal (PBKDF2), y bloqueo después de varios intentos
- [x] PIN corto para que el mesero entre rápido en la compu compartida
- [x] Cada orden guarda qué mesero la atendió y cada pago quién lo cobró
- [x] **Apertura de caja** con el fondo inicial y **cierre** con conteo de efectivo, diferencia y corte impreso
- [x] Anulaciones y descuentos con autorización de un admin
- [x] Pantallas de usuarios, bitácora y datos del restaurante
- [x] La base se actualiza sola al instalar una versión nueva

## Fase 3: operación del restaurante

- [x] **Pantalla de cocina**: una por estación (cocina y barra), con los pedidos en tarjetas, el tiempo que llevan y los botones "Empezar" y "Listo"
- [x] La tarjeta cambia de color si un pedido pasa de 15 minutos, y suena cuando llega uno nuevo
- [x] El mesero ve en la mesa qué ya está listo para llevar
- [x] Respaldo de la base desde el programa, con aviso si pasa un día sin hacerlo
- [x] Regreso al login si nadie usa la compu un rato
- [x] Atajos de teclado para tomar la orden y cobrar
- [ ] Opcional: ticket de comanda impreso en cocina, para quien prefiera papel
- [ ] Mover una orden de mesa y juntar mesas
- [ ] **Dividir la cuenta** por platillos o en partes iguales, cada parte con su pago
- [ ] Varios pagos en una cuenta (mitad efectivo, mitad tarjeta)
- [ ] Modificadores con precio: término de la carne, extra queso +Q5, sin hielo
- [ ] Combos y platillo del día
- [ ] Pedidos para llevar y a domicilio, con nombre, teléfono y dirección
- [ ] Configurar áreas y mesas desde el sistema

## Fase 4: dejarlo lindísimo

- [x] Diseño propio: barra lateral, tarjetas, iconos y paleta del restaurante
- [x] Modo oscuro para la cocina
- [x] Que se vea bien con el escalado de Windows al 125 % o 150 %
- [x] Ticket con vista previa como va a salir en el papel
- [ ] **Plano del salón** con la forma y posición real de cada mesa
- [ ] Menú con foto de cada platillo

## Fase 5: reportes y control

- [ ] Ventas por día, semana y mes, con gráficas
- [ ] Platillos más y menos vendidos
- [ ] Ventas y propinas por mesero
- [ ] Horas pico y tiempo promedio por mesa
- [ ] Tiempo promedio de cocina por platillo
- [ ] Anulaciones por motivo y por usuario
- [ ] Exportar a Excel y PDF
- [ ] Inventario de insumos con recetas: al vender un pepián se descuenta el pollo, y avisa cuando algo se va a acabar
- [ ] Clientes frecuentes y reservaciones

## Fase 6, opcional: facturación electrónica

- [ ] Factura FEL con NIT del cliente por medio de un certificador autorizado
- [ ] Notas de crédito para devoluciones
