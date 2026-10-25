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
        readonly Label lblConexion = new Label();
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

            var barra = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                Padding = new Padding(12, 8, 12, 8),
                BackColor = Color.White
            };
            var btnMenu = Estilo.Boton("Menú", Estilo.Gris);
            var btnVentas = Estilo.Boton("Ventas del día", Estilo.Gris);
            var btnConexion = Estilo.Boton("Conexión", Estilo.Gris);
            btnMenu.Click += (s, e) => Dialogo(new FrmMenu());
            btnVentas.Click += (s, e) => Dialogo(new FrmVentas());
            btnConexion.Click += (s, e) => CambiarConexion();
            lblResumen.AutoSize = true;
            lblResumen.Margin = new Padding(20, 10, 0, 0);
            lblResumen.ForeColor = Estilo.Gris;
            lblConexion.AutoSize = true;
            lblConexion.Margin = new Padding(20, 10, 0, 0);
            barra.Controls.AddRange(new Control[] { btnMenu, btnVentas, btnConexion, lblResumen, lblConexion });

            Controls.Add(panelMesas);
            Controls.Add(barra);
            Controls.Add(Estilo.Encabezado(Configuracion.Nombre + " · Mesas"));

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
                lblConexion.Text = "● Conectado a " + Conexion.Describir(Conexion.Cadena);
                lblConexion.ForeColor = Estilo.Libre;
            }
            catch (Exception ex)
            {
                // el timer vuelve a intentar solo, no se llena la pantalla de mensajes
                Log.Error("Refrescar mesas", ex);
                lblConexion.Text = "● Sin conexión, reintentando... (" + Log.Amigable(ex) + ")";
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

        async void CambiarConexion()
        {
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
