using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using RestauranteDelicias.Datos;
using RestauranteDelicias.Modelos;
using RestauranteDelicias.Servicios;

namespace RestauranteDelicias.Pantallas
{
    public class FrmPrincipal : Form
    {
        // cada cuanto se pregunta a la base como van las mesas, asi se ve lo que hacen las otras cajas
        const int REFRESCO_MS = 4000;

        readonly FlowLayoutPanel panelMesas = new FlowLayoutPanel();
        readonly Dictionary<int, Tarjeta> tarjetas = new Dictionary<int, Tarjeta>();
        readonly List<FlowLayoutPanel> filasDeMesas = new List<FlowLayoutPanel>();
        readonly Panel lateral = new Panel();
        readonly FlowLayoutPanel menuLateral = new FlowLayoutPanel();
        readonly Label lblMarca = new Label();
        readonly Label lblUsuario = new Label();
        readonly Label lblRol = new Label();
        readonly Label lblConexion = new Label();
        readonly Label lblResumen = new Label();
        readonly Label lblFecha = new Label();
        readonly Timer timer = new Timer();
        readonly Timer vigilante = new Timer();
        readonly Boton btnRespaldo = Estilo.Boton("Respaldo pendiente", Estilo.Naranja, Icono.Aviso);
        readonly Panel zonaAviso = new Panel();
        List<MesaEstado> mesas = new List<MesaEstado>();
        bool refrescando;
        bool hayDialogo;

        public FrmPrincipal()
        {
            Estilo.Formulario(this, Configuracion.Nombre, 1000, 640);
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;

            // a la izquierda la barra oscura con el nombre, quien esta usando la compu y las opciones
            lateral.Dock = DockStyle.Left;
            lateral.Width = 236;
            lateral.BackColor = Estilo.Lateral;

            // el alto sale de lo que mide el texto, asi un nombre largo baja de linea en vez de cortarse
            var marca = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(18, 20, 12, 14),
                BackColor = Estilo.Lateral
            };
            lblMarca.AutoSize = true;
            lblMarca.Font = Estilo.Titulo;
            lblMarca.ForeColor = Color.White;
            lblMarca.Margin = new Padding(0, 0, 0, 10);
            lblUsuario.AutoSize = true;
            lblUsuario.Font = Estilo.Negrita;
            lblUsuario.ForeColor = Color.White;
            lblUsuario.Margin = new Padding(0);
            lblRol.AutoSize = true;
            lblRol.Font = Estilo.Chica;
            lblRol.ForeColor = Estilo.LateralTexto;
            lblRol.Margin = new Padding(0, 2, 0, 0);
            marca.Controls.Add(lblMarca);
            marca.Controls.Add(lblUsuario);
            marca.Controls.Add(lblRol);

            menuLateral.Dock = DockStyle.Fill;
            menuLateral.FlowDirection = FlowDirection.TopDown;
            menuLateral.WrapContents = false;
            menuLateral.AutoScroll = true;
            menuLateral.Padding = new Padding(10, 6, 10, 6);
            menuLateral.BackColor = Estilo.Lateral;

            lblConexion.Dock = DockStyle.Bottom;
            lblConexion.Height = 40;
            lblConexion.Padding = new Padding(18, 0, 0, 0);
            lblConexion.TextAlign = ContentAlignment.MiddleLeft;
            lblConexion.Font = Estilo.Chica;
            lblConexion.ForeColor = Estilo.LateralTexto;

            lateral.Controls.Add(menuLateral);
            lateral.Controls.Add(lblConexion);
            lateral.Controls.Add(marca);

            // arriba el titulo con la fecha y el resumen del salon
            var cabeza = new Panel { Dock = DockStyle.Top, Height = 76, BackColor = Color.White, Padding = new Padding(24, 10, 24, 1) };
            cabeza.Paint += (s, e) =>
            {
                using (var p = new Pen(Estilo.Borde)) e.Graphics.DrawLine(p, 0, cabeza.Height - 1, cabeza.Width, cabeza.Height - 1);
            };
            var lblTitulo = new Label { Text = "Mesas", Font = Estilo.Titulo, ForeColor = Estilo.Texto, Dock = DockStyle.Top, Height = 34, BackColor = Color.White };
            lblFecha.Dock = DockStyle.Top;
            lblFecha.Height = 24;
            lblFecha.ForeColor = Estilo.Suave;
            lblFecha.BackColor = Color.White;
            lblResumen.Dock = DockStyle.Right;
            lblResumen.AutoSize = true;
            lblResumen.Padding = new Padding(16, 0, 0, 0);
            lblResumen.TextAlign = ContentAlignment.MiddleRight;
            lblResumen.Font = Estilo.Negrita;
            lblResumen.ForeColor = Estilo.Suave;
            lblResumen.BackColor = Color.White;
            // al admin se le recuerda si hace mas de un dia que no se respalda la base
            btnRespaldo.Contorno = true;
            btnRespaldo.AutoSize = false;
            btnRespaldo.Dock = DockStyle.Fill;
            zonaAviso.Dock = DockStyle.Right;
            zonaAviso.Width = 220;
            zonaAviso.Padding = new Padding(16, 14, 0, 16);
            zonaAviso.BackColor = Color.White;
            zonaAviso.Visible = false;
            zonaAviso.Controls.Add(btnRespaldo);
            btnRespaldo.Click += async (s, e) => { await Dialogo(new FrmRespaldo()); Rearmar(); };
            cabeza.Controls.Add(lblFecha);
            cabeza.Controls.Add(lblTitulo);
            cabeza.Controls.Add(lblResumen);
            cabeza.Controls.Add(zonaAviso);

            // cada area va en su propia fila que se acomoda al ancho de la pantalla
            panelMesas.Dock = DockStyle.Fill;
            panelMesas.Padding = new Padding(18, 6, 18, 18);
            panelMesas.AutoScroll = true;
            panelMesas.FlowDirection = FlowDirection.TopDown;
            panelMesas.WrapContents = false;
            panelMesas.BackColor = Estilo.Crema;
            panelMesas.Resize += (s, e) => AcomodarMesas();

            Controls.Add(panelMesas);
            Controls.Add(cabeza);
            Controls.Add(lateral);

            // la barra se arma ya escalada, por eso va en el load y no aca
            Load += (s, e) => ArmarBarra();

            timer.Interval = REFRESCO_MS;
            timer.Tick += async (s, e) => await Refrescar();
            Shown += async (s, e) => { await Refrescar(); timer.Start(); };
            FormClosed += (s, e) => { timer.Stop(); vigilante.Stop(); };

            // si nadie toca la compu un rato se regresa al login, asi nadie trabaja con el usuario de otro
            vigilante.Interval = 15000;
            vigilante.Tick += (s, e) => RevisarInactividad();
            vigilante.Start();
            KeyPreview = true;
            KeyDown += async (s, e) => { if (e.KeyCode == Keys.F5) await Refrescar(); };
        }

        // la consulta corre en otro hilo para que la pantalla no se congele si la red esta lenta
        async Task Refrescar()
        {
            if (refrescando) return;
            refrescando = true;
            try
            {
                var nuevas = await Task.Run(() => MesaRepo.Estado());
                if (IsDisposed) return;
                mesas = nuevas;
                Pintar();
                lblConexion.Text = "●  En línea";
                lblConexion.ForeColor = Estilo.Mezclar(Estilo.Libre, Color.White, 0.25f);
            }
            catch (Exception ex)
            {
                // el timer vuelve a intentar solo, no se llena la pantalla de mensajes
                Log.Error("Refrescar mesas", ex);
                if (IsDisposed) return;
                lblConexion.Text = "●  Sin conexión, reintentando...";
                lblConexion.ForeColor = Estilo.Mezclar(Estilo.Ocupada, Color.White, 0.2f);
            }
            finally
            {
                refrescando = false;
            }
            if (!IsDisposed) lblFecha.Text = Fecha();
        }

        static string Fecha()
        {
            try
            {
                var texto = DateTime.Now.ToString("dddd d 'de' MMMM, HH:mm", new CultureInfo("es-GT"));
                return char.ToUpper(texto[0]) + texto.Substring(1);
            }
            catch
            {
                return DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            }
        }

        void Pintar()
        {
            // solo se arman las tarjetas de nuevo si cambiaron las mesas, si no se cambia el texto y no parpadea
            var ids = mesas.Select(m => m.Id).ToList();
            if (!ids.SequenceEqual(tarjetas.Keys))
                Armar();

            foreach (var m in mesas)
            {
                var t = tarjetas[m.Id];
                if (!m.Ocupada)
                {
                    t.Acento = Estilo.Libre;
                    t.Etiqueta = "Libre";
                    t.Detalle = m.Capacidad + " personas";
                    t.Pie = "";
                    continue;
                }
                var minutos = (int)(DateTime.Now - m.Apertura.Value).TotalMinutes;
                // lo que cocina ya tiene listo es lo mas urgente, despues lo que no se ha mandado
                if (m.Listos > 0)
                {
                    t.Acento = Estilo.Azul;
                    t.Etiqueta = m.Listos == 1 ? "1 listo" : m.Listos + " listos";
                }
                else
                {
                    t.Acento = m.Pendientes > 0 ? Estilo.Naranja : Estilo.Ocupada;
                    t.Etiqueta = m.Pendientes > 0 ? "Sin enviar" : "Ocupada";
                }
                t.Detalle = m.Personas + " pers · " + Tiempo(minutos);
                t.Pie = Ticket.Q(m.Subtotal);
            }

            var ocupadas = mesas.Count(m => m.Ocupada);
            lblResumen.Text = (mesas.Count - ocupadas) + " libres     " + ocupadas + " ocupadas     " +
                              Ticket.Q(mesas.Sum(m => m.Subtotal)) + " en mesas";
        }

        void Armar()
        {
            panelMesas.SuspendLayout();
            Estilo.Vaciar(panelMesas);
            tarjetas.Clear();
            filasDeMesas.Clear();

            foreach (var area in mesas.GroupBy(m => m.AreaNombre))
            {
                panelMesas.Controls.Add(new Label
                {
                    Text = area.Key.ToUpper(),
                    Font = Estilo.Negrita,
                    ForeColor = Estilo.Suave,
                    AutoSize = true,
                    Margin = new Padding(Estilo.Px(8), Estilo.Px(18), Estilo.Px(8), Estilo.Px(2))
                });

                var fila = new FlowLayoutPanel
                {
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    WrapContents = true,
                    Margin = new Padding(0),
                    BackColor = Estilo.Crema
                };
                foreach (var m in area)
                {
                    var mesa = m;
                    var t = new Tarjeta
                    {
                        Titulo = "Mesa " + mesa.Numero,
                        Size = new Size(Estilo.Px(200), Estilo.Px(124)),
                        Margin = new Padding(Estilo.Px(7))
                    };
                    t.Click += (s, e) => AbrirMesa(mesa.Id, mesa.Numero);
                    tarjetas[mesa.Id] = t;
                    fila.Controls.Add(t);
                }
                filasDeMesas.Add(fila);
                panelMesas.Controls.Add(fila);
            }
            AcomodarMesas();
            panelMesas.ResumeLayout();
        }

        // el ancho que queda se reparte entre las tarjetas, en pantalla grande caben mas por fila y en chica bajan
        void AcomodarMesas()
        {
            var disponible = panelMesas.ClientSize.Width - panelMesas.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth;
            if (disponible <= 0) return;
            foreach (var fila in filasDeMesas)
            {
                fila.MaximumSize = new Size(disponible, 0);
                fila.MinimumSize = new Size(disponible, 0);
                Estilo.RepartirTarjetas(fila, disponible, Estilo.Px(190), Estilo.Px(260));
            }
        }

        static string Tiempo(int minutos)
        {
            return minutos < 60 ? minutos + " min" : (minutos / 60) + " h " + (minutos % 60) + " min";
        }

        async void AbrirMesa(int mesaId, int numero)
        {
            await Dialogo(new FrmOrden(mesaId, numero));
        }

        // mientras hay una ventana abierta el timer sigue, pero se refresca al cerrar para ver el cambio al tiro
        async Task Dialogo(Form f)
        {
            if (hayDialogo) return;
            hayDialogo = true;
            try
            {
                using (f) f.ShowDialog(this);
            }
            finally
            {
                hayDialogo = false;
            }
            await Refrescar();
        }

        // cada quien ve solo lo que puede hacer
        void ArmarBarra()
        {
            menuLateral.SuspendLayout();
            Estilo.Vaciar(menuLateral);

            var mesasItem = Opcion("Mesas", Icono.Mesas, null);
            mesasItem.BackColor = Estilo.Mezclar(Estilo.Lateral, Color.White, 0.1f);
            mesasItem.ForeColor = Color.White;

            if (Sesion.PuedeCobrar)
            {
                var abierta = Sesion.TurnoId.HasValue;
                var caja = Opcion(abierta ? "Caja abierta" : "Abrir caja", Icono.Caja, async () => { await Dialogo(new FrmCaja()); Rearmar(); });
                if (!abierta) caja.ForeColor = Estilo.Mezclar(Estilo.Naranja, Color.White, 0.2f);
            }
            if (Sesion.PuedeVerVentas) Opcion("Ventas del día", Icono.Ventas, () => Dialogo(new FrmVentas()));
            if (Sesion.EsAdmin)
            {
                Opcion("Menú", Icono.Menu, () => Dialogo(new FrmMenu()));
                Opcion("Cocina", Icono.Cocina, () => Dialogo(new FrmCocina(true)));
                // lo de administrar va junto en una sola pantalla, asi la barra queda para el trabajo del dia
                Opcion("Ajustes", Icono.Ajustes, AbrirAjustes);
            }

            var separador = new Panel { Height = Estilo.Px(1), Width = AnchoOpcion, BackColor = Estilo.Mezclar(Estilo.Lateral, Color.White, 0.12f), Margin = new Padding(Estilo.Px(4), Estilo.Px(10), Estilo.Px(4), Estilo.Px(10)) };
            menuLateral.Controls.Add(separador);
            Opcion("Cambiar usuario", Icono.Cambiar, () => { CambiarUsuario(); return Task.FromResult(0); });
            menuLateral.ResumeLayout();

            zonaAviso.Visible = Sesion.EsAdmin && FrmRespaldo.Pendiente;
            Text = Configuracion.Nombre;
            lblMarca.Text = Configuracion.Nombre;
            lblMarca.MaximumSize = lblUsuario.MaximumSize = lblRol.MaximumSize = new Size(lateral.Width - Estilo.Px(32), 0);
            lblUsuario.Text = Sesion.Nombre;
            lblRol.Text = (Sesion.Usuario != null ? Sesion.Usuario.Rol : "") +
                          (Sesion.PuedeCobrar ? (Sesion.TurnoId.HasValue ? " · caja abierta" : " · sin caja") : "");
        }

        int AnchoOpcion
        {
            get { return Math.Max(Estilo.Px(120), lateral.Width - menuLateral.Padding.Horizontal - Estilo.Px(4)); }
        }

        // el boton que se toco se borra al rearmar, por eso se deja para cuando termine su click
        void Rearmar()
        {
            if (IsHandleCreated) BeginInvoke(new Action(ArmarBarra));
            else ArmarBarra();
        }

        Boton Opcion(string texto, string icono, Func<Task> accion)
        {
            var b = new Boton
            {
                Text = texto,
                Icono = icono,
                Izquierda = true,
                AutoSize = false,
                BackColor = Estilo.Lateral,
                ForeColor = Estilo.LateralTexto,
                Font = Estilo.Normal,
                Width = AnchoOpcion,
                Height = Estilo.Px(42),
                Margin = new Padding(0, Estilo.Px(2), 0, Estilo.Px(2)),
                Padding = new Padding(Estilo.Px(14), 0, Estilo.Px(8), 0)
            };
            if (accion != null) b.Click += async (s, e) => await accion();
            menuLateral.Controls.Add(b);
            return b;
        }

        async Task AbrirAjustes()
        {
            string eleccion;
            using (var f = new FrmAjustes())
            {
                if (f.ShowDialog(this) != DialogResult.OK) return;
                eleccion = f.Eleccion;
            }
            switch (eleccion)
            {
                case FrmAjustes.USUARIOS: await Dialogo(new FrmUsuarios()); break;
                case FrmAjustes.BITACORA: await Dialogo(new FrmBitacora()); break;
                case FrmAjustes.DATOS: await Dialogo(new FrmConfiguracion()); Rearmar(); break;
                case FrmAjustes.RESPALDO: await Dialogo(new FrmRespaldo()); Rearmar(); break;
                case FrmAjustes.CONEXION: CambiarConexion(); break;
            }
        }

        // la ventana se cierra y el programa vuelve al login para que entre el siguiente
        void CambiarUsuario()
        {
            CambiarUsuario("Salida");
        }

        void CambiarUsuario(string motivo)
        {
            timer.Stop();
            vigilante.Stop();
            try { Bitacora.Registrar(motivo, Sesion.Nombre); }
            catch (Exception ex) { Log.Error("Registrar salida", ex); }
            Sesion.Cerrar();
            Sesion.OtroUsuario = true;
            Close();
        }

        void RevisarInactividad()
        {
            var minutos = (int)Configuracion.Numero("Sesion.MinutosInactivo", 5);
            // si hay algo abierto encima, una orden o un mensaje, se espera a que lo cierren
            if (minutos <= 0 || hayDialogo || Application.OpenForms.Count > 1 || Form.ActiveForm != this) return;
            if (Actividad.SinUso.TotalMinutes < minutos) return;
            Log.Info("Salida por inactividad de " + Sesion.Nombre);
            CambiarUsuario("Salida por inactividad");
        }

        // si se cambia mal esta compu deja de ver las mesas, por eso se pregunta antes
        async void CambiarConexion()
        {
            if (!Ui.Confirmar(this, "Esto cambia a qué servidor se conecta esta compu. Solo se usa si el servidor cambió de nombre o de lugar.\n\n¿Seguir?", "Conexión a la base de datos"))
                return;
            using (var f = new FrmConexion())
            {
                if (f.ShowDialog(this) != DialogResult.OK) return;
            }
            Ui.Seguro(this, Configuracion.Cargar);
            tarjetas.Clear();
            await Refrescar();
        }
    }
}
