using System;
using System.Collections.Generic;
using System.Drawing;
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
        readonly Dictionary<int, Button> botones = new Dictionary<int, Button>();
        readonly Label lblResumen = new Label();
        readonly Label lblTitulo = new Label();
        readonly Label lblUsuario = new Label();
        readonly FlowLayoutPanel barra = new FlowLayoutPanel();
        readonly Label lblConexion = new Label();
        readonly ContextMenuStrip menuAjustes = new ContextMenuStrip();
        readonly Timer timer = new Timer();
        List<MesaEstado> mesas = new List<MesaEstado>();
        bool refrescando;
        bool hayDialogo;

        public FrmPrincipal()
        {
            Estilo.Formulario(this, Configuracion.Nombre, 1000, 640);
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;

            panelMesas.Dock = DockStyle.Fill;
            panelMesas.Padding = new Padding(16);
            panelMesas.AutoScroll = true;

            barra.Dock = DockStyle.Bottom;
            barra.Height = 56;
            barra.Padding = new Padding(12, 8, 12, 8);
            barra.BackColor = Color.White;
            lblResumen.AutoSize = true;
            lblResumen.Margin = new Padding(20, 10, 0, 0);
            lblResumen.ForeColor = Estilo.Gris;
            lblConexion.AutoSize = true;
            lblConexion.Margin = new Padding(20, 10, 0, 0);
            ArmarAjustes();

            // encabezado con el nombre del restaurante a la izquierda y quien esta usando la compu a la derecha
            var cabeza = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Estilo.Vino };
            lblTitulo.ForeColor = Color.White;
            lblTitulo.Font = Estilo.Titulo;
            lblTitulo.AutoSize = true;
            lblTitulo.Location = new Point(16, 12);
            lblUsuario.Dock = DockStyle.Right;
            lblUsuario.Width = 460;
            lblUsuario.TextAlign = ContentAlignment.MiddleRight;
            lblUsuario.ForeColor = Color.White;
            lblUsuario.Font = Estilo.Negrita;
            lblUsuario.Padding = new Padding(0, 0, 16, 0);
            cabeza.Controls.Add(lblUsuario);
            cabeza.Controls.Add(lblTitulo);

            Controls.Add(panelMesas);
            Controls.Add(barra);
            Controls.Add(cabeza);
            ArmarBarra();

            timer.Interval = REFRESCO_MS;
            timer.Tick += async (s, e) => await Refrescar();
            Shown += async (s, e) => { await Refrescar(); timer.Start(); };
            FormClosed += (s, e) => timer.Stop();
        }

        // la consulta corre en otro hilo para que la pantalla no se congele si la red esta lenta
        async Task Refrescar()
        {
            if (refrescando) return;
            refrescando = true;
            try
            {
                var nuevas = await Task.Run(() => MesaRepo.Estado());
                mesas = nuevas;
                Pintar();
                lblConexion.Text = "● En línea";
                lblConexion.ForeColor = Estilo.Libre;
            }
            catch (Exception ex)
            {
                // el timer vuelve a intentar solo, no se llena la pantalla de mensajes
                Log.Error("Refrescar mesas", ex);
                lblConexion.Text = "● Sin conexión, reintentando...";
                lblConexion.ForeColor = Estilo.Ocupada;
            }
            finally
            {
                refrescando = false;
            }
        }

        void Pintar()
        {
            // solo se arman los botones de nuevo si cambiaron las mesas, si no se actualiza el texto y no parpadea
            var ids = mesas.Select(m => m.Id).ToList();
            if (!ids.SequenceEqual(botones.Keys))
                Armar();

            foreach (var m in mesas)
            {
                var b = botones[m.Id];
                if (!m.Ocupada)
                {
                    b.BackColor = Estilo.Libre;
                    b.Text = "Mesa " + m.Numero + "\nLibre\n" + m.Capacidad + " personas";
                    continue;
                }
                var minutos = (int)(DateTime.Now - m.Apertura.Value).TotalMinutes;
                b.BackColor = m.Pendientes > 0 ? Estilo.Naranja : Estilo.Ocupada;
                b.Text = "Mesa " + m.Numero + "\n" + Ticket.Q(m.Subtotal) + "\n" +
                         m.Personas + " pers · " + Tiempo(minutos) +
                         (m.Pendientes > 0 ? "\nSin enviar a cocina" : "");
            }

            var ocupadas = mesas.Count(m => m.Ocupada);
            lblResumen.Text = ocupadas + " de " + mesas.Count + " mesas ocupadas   ·   en mesas " +
                              Ticket.Q(mesas.Sum(m => m.Subtotal));
        }

        void Armar()
        {
            panelMesas.SuspendLayout();
            panelMesas.Controls.Clear();
            botones.Clear();

            foreach (var area in mesas.GroupBy(m => m.AreaNombre))
            {
                var titulo = new Label
                {
                    Text = area.Key,
                    Font = Estilo.Titulo,
                    ForeColor = Estilo.Vino,
                    AutoSize = true,
                    Margin = new Padding(8, 12, 8, 4)
                };
                panelMesas.Controls.Add(titulo);
                panelMesas.SetFlowBreak(titulo, true);

                Button ultimo = null;
                foreach (var m in area)
                {
                    var mesa = m;
                    var b = new Button
                    {
                        Size = new Size(170, 104),
                        Margin = new Padding(8),
                        FlatStyle = FlatStyle.Flat,
                        ForeColor = Color.White,
                        Font = Estilo.Negrita,
                        Cursor = Cursors.Hand
                    };
                    b.FlatAppearance.BorderSize = 0;
                    b.Click += (s, e) => AbrirMesa(mesa.Id, mesa.Numero);
                    botones[mesa.Id] = b;
                    panelMesas.Controls.Add(b);
                    ultimo = b;
                }
                if (ultimo != null) panelMesas.SetFlowBreak(ultimo, true);
            }
            panelMesas.ResumeLayout();
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

        // cada quien ve solo los botones de lo que puede hacer
        void ArmarBarra()
        {
            barra.SuspendLayout();
            barra.Controls.Clear();

            if (Sesion.PuedeCobrar)
            {
                var caja = Estilo.Boton(Sesion.TurnoId.HasValue ? "Caja" : "Abrir caja", Sesion.TurnoId.HasValue ? Estilo.Libre : Estilo.Naranja);
                caja.Click += async (s, e) => { await Dialogo(new FrmCaja()); ArmarBarra(); };
                barra.Controls.Add(caja);
            }
            if (Sesion.PuedeVerVentas) Agregar("Ventas del día", () => new FrmVentas());
            if (Sesion.EsAdmin)
            {
                Agregar("Menú", () => new FrmMenu());
                // lo de administrar va junto en un menu, asi la barra queda para el trabajo del dia
                var ajustes = Estilo.Boton("Ajustes ▾", Estilo.Gris);
                ajustes.Click += (s, e) => menuAjustes.Show(ajustes, new Point(0, 0), ToolStripDropDownDirection.AboveRight);
                barra.Controls.Add(ajustes);
            }
            var salir = Estilo.Boton("Cambiar usuario", Estilo.Vino);
            salir.Click += (s, e) => CambiarUsuario();
            barra.Controls.Add(salir);
            barra.Controls.Add(lblResumen);
            barra.Controls.Add(lblConexion);
            barra.ResumeLayout();

            Text = Configuracion.Nombre;
            lblTitulo.Text = Configuracion.Nombre + " · Mesas";
            lblUsuario.Text = Sesion.Nombre + " · " + (Sesion.Usuario != null ? Sesion.Usuario.Rol : "") +
                              (Sesion.PuedeCobrar ? (Sesion.TurnoId.HasValue ? " · caja abierta" : " · sin caja") : "");
        }

        void ArmarAjustes()
        {
            menuAjustes.Font = Estilo.Normal;
            menuAjustes.ShowImageMargin = false;
            menuAjustes.Items.Add("Usuarios", null, async (s, e) => await Dialogo(new FrmUsuarios()));
            menuAjustes.Items.Add("Bitácora", null, async (s, e) => await Dialogo(new FrmBitacora()));
            menuAjustes.Items.Add("Datos del restaurante", null, async (s, e) => { await Dialogo(new FrmConfiguracion()); ArmarBarra(); });
            menuAjustes.Items.Add(new ToolStripSeparator());
            menuAjustes.Items.Add("Conexión a la base de datos", null, (s, e) => CambiarConexion());
            foreach (ToolStripItem i in menuAjustes.Items)
                i.Padding = new Padding(8, 6, 8, 6);
        }

        void Agregar(string texto, Func<Form> crear)
        {
            var b = Estilo.Boton(texto, Estilo.Gris);
            b.Click += async (s, e) => await Dialogo(crear());
            barra.Controls.Add(b);
        }

        // la compu queda en el login y el siguiente entra con su usuario, sin cerrar el programa
        async void CambiarUsuario()
        {
            timer.Stop();
            try { Bitacora.Registrar("Salida", Sesion.Nombre); }
            catch (Exception ex) { Log.Error("Registrar salida", ex); }
            Sesion.Cerrar();
            Hide();
            if (!FrmLogin.IniciarSesion(null))
            {
                Close();
                return;
            }
            ArmarBarra();
            Show();
            await Refrescar();
            timer.Start();
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
            botones.Clear();
            await Refrescar();
        }
    }
}
