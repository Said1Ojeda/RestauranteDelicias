-- datos para arrancar, el menu y las mesas se pueden cambiar despues desde el sistema

INSERT INTO dbo.Configuracion (Clave, Valor) VALUES
    (N'Restaurante.Nombre',     N'Restaurante Delicias'),
    (N'Restaurante.Lema',       N'Comida chapina'),
    (N'Restaurante.Direccion',  N'Ciudad de Guatemala'),
    (N'Restaurante.Telefono',   N''),
    (N'Restaurante.Nit',        N'CF'),
    (N'Propina.Porcentaje',     N'10'),
    (N'Iva.Porcentaje',         N'12'),
    (N'Ticket.Pie',             N'Gracias por su visita');
GO

INSERT INTO dbo.Categoria (Nombre, Orden, Estacion, Color) VALUES
    (N'Entradas',       1, N'Cocina', '#E8A33D'),
    (N'Platos fuertes', 2, N'Cocina', '#C4453A'),
    (N'Bebidas',        3, N'Barra',  '#3A8FC4'),
    (N'Postres',        4, N'Cocina', '#B5579E');
GO

DECLARE @entradas INT = (SELECT Id FROM dbo.Categoria WHERE Nombre = N'Entradas');
DECLARE @fuertes  INT = (SELECT Id FROM dbo.Categoria WHERE Nombre = N'Platos fuertes');
DECLARE @bebidas  INT = (SELECT Id FROM dbo.Categoria WHERE Nombre = N'Bebidas');
DECLARE @postres  INT = (SELECT Id FROM dbo.Categoria WHERE Nombre = N'Postres');

INSERT INTO dbo.Platillo (CategoriaId, Nombre, Descripcion, Precio) VALUES
    (@entradas, N'Guacamol con totopos',      N'Aguacate, tomate, cebolla y limón',          35.00),
    (@entradas, N'Chiles rellenos',           N'Con carne, verduras y salsa de tomate',      40.00),
    (@entradas, N'Tostadas de frijol',        N'Tres tostadas con frijol, queso y perejil',  25.00),
    (@entradas, N'Chuchitos',                 N'Dos chuchitos de cerdo con salsa',           24.00),
    (@fuertes,  N'Pepián de pollo',           N'Con arroz y tortillas',                      75.00),
    (@fuertes,  N'Hilachas',                  N'Res deshilachada en salsa de tomate',        70.00),
    (@fuertes,  N'Churrasco con chirmol',     N'Puyazo, chirmol, frijol y guacamol',         95.00),
    (@fuertes,  N'Pollo en jocón',            N'Salsa verde de miltomate y cilantro',        72.00),
    (@fuertes,  N'Kak''ik',                   N'Caldo de chunto con chile cobanero',         85.00),
    (@fuertes,  N'Desayuno chapín',           N'Huevos, frijol, plátano, crema y queso',     45.00),
    (@bebidas,  N'Fresco de rosa de jamaica', N'Vaso de 16 onzas',                           15.00),
    (@bebidas,  N'Horchata',                  N'Vaso de 16 onzas',                           15.00),
    (@bebidas,  N'Café de olla',              N'Con canela y panela',                        12.00),
    (@bebidas,  N'Atol de elote',             N'Taza grande',                                14.00),
    (@bebidas,  N'Agua pura',                 N'Botella de 600 ml',                           8.00),
    (@bebidas,  N'Gaseosa',                   N'Lata',                                       12.00),
    (@postres,  N'Rellenitos de plátano',     N'Tres rellenitos con frijol dulce',           20.00),
    (@postres,  N'Mole de plátano',           N'Plátano en salsa de chocolate',              22.00),
    (@postres,  N'Champurradas con café',     N'Dos champurradas',                           18.00),
    (@postres,  N'Buñuelos en miel',          N'Cuatro buñuelos',                            20.00);
GO

INSERT INTO dbo.Area (Nombre, Orden) VALUES (N'Salón', 1), (N'Terraza', 2);
GO

DECLARE @salon INT = (SELECT Id FROM dbo.Area WHERE Nombre = N'Salón');
DECLARE @terraza INT = (SELECT Id FROM dbo.Area WHERE Nombre = N'Terraza');

INSERT INTO dbo.Mesa (AreaId, Numero, Capacidad, PosX, PosY, Forma) VALUES
    (@salon, 1, 4, 0, 0, N'Cuadrada'), (@salon, 2, 4, 1, 0, N'Cuadrada'),
    (@salon, 3, 4, 2, 0, N'Cuadrada'), (@salon, 4, 6, 3, 0, N'Redonda'),
    (@salon, 5, 2, 0, 1, N'Cuadrada'), (@salon, 6, 2, 1, 1, N'Cuadrada'),
    (@salon, 7, 4, 2, 1, N'Cuadrada'), (@salon, 8, 8, 3, 1, N'Redonda'),
    (@terraza, 9, 4, 0, 0, N'Redonda'), (@terraza, 10, 4, 1, 0, N'Redonda'),
    (@terraza, 11, 4, 2, 0, N'Redonda'), (@terraza, 12, 6, 3, 0, N'Cuadrada');
GO
