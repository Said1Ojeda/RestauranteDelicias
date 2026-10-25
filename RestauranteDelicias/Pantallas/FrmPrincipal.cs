using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RestauranteDelicias.Datos;
using RestauranteDelicias.Modelos;
using RestauranteDelicias.Servicios;

namespace RestauranteDelicias.Pantallas
{
    public class FrmPrincipal : Form
    {
        const int MESAS = 12;

        List<Platillo> menu;

        // las ordenes abiertas viven en memoria, por eso se avisa antes de cerrar con mesas ocupadas
        readonly Dictionary<int, Orden> abiertas = new Dictionary<int, Orden>();
        readonly Dictionary<int, Button> botones = new Dictionary<int, Button>();

        readonly Label lblResumen = new Label();

        public FrmPrincipal()
        {
            Estilo.Formulario(this, "Restaurante Delicias", 900, 600);
            StartPosition = FormStartPosition.CenterScreen;

            menu = Almacen.CargarMenu();

            var mesas = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                AutoScroll = true
            };
            for (int i = 1; i <= MESAS; i++)
            {
                int numero = i;
                var b = new Button
                {
                    Size = new Size(190, 110),
                    Margin = new Padding(10),
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = Color.White,
                    Font = Estilo.Negrita,
                    Cursor = Cursors.Hand
                };
                b.FlatAppearance.BorderSize = 0;
                b.Click += (s, e) => AbrirMesa(numero);
                botones[numero] = b;
                mesas.Controls.Add(b);
            }

            var barra = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                Padding = new Padding(12, 8, 12, 8),
                BackColor = Color.White
            };
            var btnMenu = Estilo.Boton("Menú", Estilo.Gris);
            var btnVentas = Estilo.Boton("Ventas del día", Estilo.Gris);
            btnMenu.Click += (s, e) => EditarMenu();
            btnVentas.Click += (s, e) =>
            {
                using (var f = new FrmVentas()) f.ShowDialog(this);
            };
            lblResumen.AutoSize = true;
            lblResumen.Margin = new Padding(20, 10, 0, 0);
            lblResumen.ForeColor = Estilo.Gris;
            barra.Controls.AddRange(new Control[] { btnMenu, btnVentas, lblResumen });

            Controls.Add(mesas);
            Controls.Add(barra);
            Controls.Add(Estilo.Encabezado("Restaurante Delicias · Mesas"));

            FormClosing += AlCerrar;
            Refrescar();
        }

        void AbrirMesa(int numero)
        {
            Orden orden;
            if (!abiertas.TryGetValue(numero, out orden))
                orden = new Orden(numero);

            using (var f = new FrmOrden(orden, menu.Where(p => p.Disponible).ToList()))
            {
                f.ShowDialog(this);

                if (f.Cobrada || orden.Vacia)
                    abiertas.Remove(numero);
                else
                    abiertas[numero] = orden;
            }
            Refrescar();
        }

        void EditarMenu()
        {
            using (var f = new FrmMenu(menu))
            {
                if (f.ShowDialog(this) == DialogResult.OK)
                    menu = Almacen.CargarMenu();
            }
        }

        void Refrescar()
        {
            foreach (var par in botones)
            {
                Orden orden;
                bool ocupada = abiertas.TryGetValue(par.Key, out orden);
                par.Value.BackColor = ocupada ? Estilo.Ocupada : Estilo.Libre;
                par.Value.Text = "Mesa " + par.Key + Environment.NewLine +
                    (ocupada ? Ticket.Q(orden.Total) + Environment.NewLine + "desde " + orden.Apertura.ToString("HH:mm") : "Libre");
            }

            var hoy = Almacen.VentasDelDia(DateTime.Today);
            lblResumen.Text = string.Format("{0} de {1} mesas ocupadas   ·   Hoy {2} ventas por {3}",
                abiertas.Count, MESAS, hoy.Count, Ticket.Q(hoy.Sum(v => v.Total)));
        }

        void AlCerrar(object sender, FormClosingEventArgs e)
        {
            if (abiertas.Count == 0) return;

            var r = MessageBox.Show(this,
                "Hay " + abiertas.Count + " mesa(s) con orden abierta. Si cierras, esas órdenes se pierden.\n\n¿Cerrar de todos modos?",
                "Mesas ocupadas", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r == DialogResult.No) e.Cancel = true;
        }
    }
}
