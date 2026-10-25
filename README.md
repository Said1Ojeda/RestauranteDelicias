# Restaurante Delicias

Sistema de escritorio para llevar las mesas, las órdenes y el cobro de un restaurante pequeño de comida chapina. Hecho en C# con Windows Forms.

La idea es que el mesero vea de un vistazo qué mesas están ocupadas, tome la orden en la misma pantalla, cobre y saque el ticket, y que al final del día se pueda ver cuánto se vendió.

## Qué hace

**Mesas**
- 12 mesas en la pantalla principal: verde si está libre, roja si está ocupada.
- Cada mesa ocupada muestra lo que lleva consumido y desde qué hora está abierta.
- Abajo se ve cuántas mesas hay ocupadas y lo vendido en el día.

**Orden**
- Menú filtrado por categoría: entradas, platos fuertes, bebidas y postres.
- Se agrega con cantidad y una nota opcional ("sin cebolla", "bien cocido").
- Si se pide otra vez el mismo platillo con la misma nota, se suma a la línea que ya estaba.
- Botones para subir, bajar o quitar.
- Subtotal, propina del 10 % (se puede quitar) y total, con el IVA incluido desglosado.

**Cobro y ticket**
- Efectivo o tarjeta. En efectivo calcula el vuelto y no deja cobrar si el monto no alcanza.
- Vista previa del ticket a 40 columnas, lista para impresora térmica de 80 mm o una normal.
- Al cobrar, la mesa queda libre de nuevo.

**Menú**
- Agregar platillos, cambiar nombre, categoría y precio.
- Un platillo no se borra, se desactiva, para no perder el historial de ventas.

**Ventas del día**
- Lista de cobros de cualquier fecha.
- Totales en efectivo, con tarjeta y de propinas por separado.
- Exportar a CSV para abrirlo en Excel.

## Cómo correrlo

Necesitas Visual Studio 2019 o 2022 con el paquete "Desarrollo de escritorio de .NET" (trae .NET Framework 4.8).

1. Clona el repo o descárgalo en ZIP.
2. Abre `RestauranteDelicias.sln`.
3. Dale F5.

No usa paquetes NuGet ni base de datos, así que no hay nada que instalar ni configurar.

La primera vez que abre crea un menú de ejemplo con platillos guatemaltecos. Lo puedes cambiar todo desde el botón **Menú**.

## Dónde se guardan los datos

En `%LOCALAPPDATA%\RestauranteDelicias\`:

| Archivo | Qué tiene |
|---|---|
| `menu.csv` | Los platillos con su categoría, precio y si están disponibles |
| `ventas.csv` | Cada cobro: número, fecha, mesa, artículos, subtotal, propina, total y forma de pago |

Son CSV normales y se pueden abrir con Excel. Para empezar de cero basta con borrar la carpeta.

## Cómo está armado

```
RestauranteDelicias/
├── Modelos/       Platillo, Orden, LineaOrden, Venta
├── Datos/         lectura y escritura de los CSV
├── Servicios/     el ticket (texto e impresión)
├── Pantallas/     los formularios y los estilos
└── Program.cs
```

Las pantallas están armadas en código, con paneles que se acomodan solos (`Dock`, `FlowLayoutPanel`, `TableLayoutPanel`). Así se ven bien aunque se cambie el tamaño de la ventana o el escalado de Windows.

### Algunas decisiones

- **El IVA va incluido** en el precio, como se acostumbra en Guatemala. El ticket solo lo desglosa (12 %).
- **La propina no lleva IVA** y en el reporte del día va aparte, porque no es venta del restaurante.
- **El precio se copia al agregar** a la orden. Si alguien cambia el precio en el menú, la mesa que ya estaba abierta no cambia.
- **El menú se guarda en un archivo temporal** y después se reemplaza, para que no quede vacío si se apaga la compu a medio guardar.
- **Los números se guardan con punto decimal**, sin importar la configuración regional de Windows.

## Lo que falta

- Guardar las órdenes abiertas. Ahorita viven en memoria, y si se cierra el programa con mesas ocupadas pide confirmación.
- Meseros con usuario, para saber quién atendió cada mesa.
- Comanda para cocina al agregar platillos.
- Pasar los datos a SQL Server o SQLite.
- Juntar o dividir cuentas.

## Licencia

MIT. Ver [LICENSE](LICENSE).
